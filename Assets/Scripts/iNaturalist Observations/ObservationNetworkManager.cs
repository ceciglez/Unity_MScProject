using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObservationNetworkManager : MonoBehaviour
{
    [Header("Network Settings")]
    public float maxConnectionDistance = 2000f;
    public float minConnectionDistance = 5f;
    public int maxConnectionsPerObservation = 8;
    public bool connectSameSpeciesOnly = true;
    
    [Header("Player-Centric Settings")]
    [Tooltip("Maximum distance from player to include observations")]
    public float playerProximityRange = 1000f;
    
    [Tooltip("Maximum observations to process (nearest to player)")]
    public int maxObservationsToProcess = 25;
    
    [Header("Visual Settings")]
    public Material connectionMaterial;
    public float lineWidth = 4f; // Thicker for better visibility
    public Color defaultConnectionColor = Color.cyan; // Different color from minimap (yellow)
    
    [Header("Performance")]
    public float updateInterval = 2f;
    public int maxTotalConnections = 200;
    
    [Header("Update Optimization")]
    [Tooltip("Minimum distance player must move before updating network")]
    public float playerMovementThreshold = 25f;
    
    [Tooltip("Update network when player moves this distance")]
    public bool enableAutomaticUpdates = true;
    
    [Tooltip("Reduce terrain raycasts for better performance (web-friendly)")]
    public bool optimizeForWeb = true;
    
    [Header("UI References")]
    [Tooltip("Your manually created network UI panel prefab")]
    public GameObject networkUIPrefab;
    
    [Tooltip("The specific Canvas where network UI should be placed")]
    public Canvas targetCanvas;
    
    [Tooltip("Parent transform for network UI (if null, uses targetCanvas)")]
    public Transform uiParent;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Internal state
    private List<ObservationDisplay> allObservations = new List<ObservationDisplay>();
    private List<NetworkConnection> activeConnections = new List<NetworkConnection>();
    private Dictionary<string, bool> categoryFilterStates = new Dictionary<string, bool>();
    private HashSet<string> enabledCategories = new HashSet<string>();
    
    private INaturalistMapController mapController;
    private ObservationNetworkUI networkUI;
    private float lastUpdateTime;
    private bool networkEnabled = false;
    
    // Player movement tracking
    private Vector3 lastPlayerPosition;
    private Transform playerTransform;
    private bool hasValidPlayerPosition = false;
    
    // Connection pooling
    private Queue<NetworkConnection> connectionPool = new Queue<NetworkConnection>();
    private Transform connectionContainer;
    
    void Start()
    {
        // Find map controller
        mapController = FindObjectOfType<INaturalistMapController>();
        if (mapController == null)
        {
            Debug.LogError("[ObservationNetworkManager] INaturalistMapController not found!");
            enabled = false;
            return;
        }
        
        // Create connection container
        GameObject container = new GameObject("NetworkConnections");
        container.transform.SetParent(transform);
        connectionContainer = container.transform;
        
        // DISABLED: Create UI (but don't require it for testing)
        // CreateNetworkUI();
        
        // Initialize connection pool
        InitializeConnectionPool();
        
        // Initialize all categories as enabled
        InitializeCategoryFilters();
        
        // Initialize player tracking
        InitializePlayerTracking();
        
        // TESTING: Auto-enable network and force connections
        networkEnabled = enableAutomaticUpdates; // Enable if automatic updates are on
        connectSameSpeciesOnly = false; // Connect everything for testing
        
        if (showDebugInfo)
        {
            Debug.Log("[ObservationNetworkManager] Initialized network manager - WAITING FOR PLAYER INTERACTION");
        }
        
        // DISABLED: Create a simple test line to verify LineRenderer works
        // CreateTestLine();
        
        // Also create a GameObject-based test line for comparison
        // CreateGameObjectTestLine(); // DISABLED - no more spheres
        
        // Auto-testing disabled - now triggered by player interaction
        // Invoke("TestConnections", 8f);
    }
    
    private int testAttempts = 0;
    private const int maxTestAttempts = 5;
    
    void Update()
    {
        // Manual trigger for testing - press N key to trigger connections
        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("[ObservationNetworkManager] N key pressed - manually triggering connections!");
            ManualTriggerConnections();
        }
        
        // Automatic updates based on player movement
        if (enableAutomaticUpdates && networkEnabled && hasValidPlayerPosition)
        {
            CheckPlayerMovementAndUpdate();
        }
    }
    
    private void CreateTestLine()
    {
        GameObject testLine = new GameObject("TestLine");
        testLine.transform.SetParent(connectionContainer);
        
        LineRenderer lr = testLine.AddComponent<LineRenderer>();
        
        // Use a simple material and set color through the material
        Material testMaterial = new Material(Shader.Find("Unlit/Color"));
        testMaterial.color = Color.red;
        lr.material = testMaterial;
        lr.startWidth = 5f;  // Make it thick so we can see it
        lr.endWidth = 5f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.allowOcclusionWhenDynamic = false;
        
        // Create a line in world space where we should be able to see it
        Vector3 start = new Vector3(0, 20, 0);  // High up
        Vector3 end = new Vector3(50, 20, 50);  // Far away and high up
        
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        Debug.Log($"[ObservationNetworkManager] Created test line from {start} to {end} with material: {lr.material.name}");
    }
    
    private void CreateGameObjectTestLine()
    {
        // Create a series of small spheres to visualize a line
        GameObject lineParent = new GameObject("GameObjectTestLine");
        lineParent.transform.SetParent(connectionContainer);
        
        Vector3 start = new Vector3(0, 25, 0);
        Vector3 end = new Vector3(60, 25, 60);
        
        int sphereCount = 20;
        for (int i = 0; i < sphereCount; i++)
        {
            float t = i / (float)(sphereCount - 1);
            Vector3 position = Vector3.Lerp(start, end, t);
            
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"LineSphere_{i}";
            sphere.transform.SetParent(lineParent.transform);
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * 2f;
            
            // Make it bright yellow
            Renderer renderer = sphere.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.yellow;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.5f);
            renderer.material = material;
        }
        
        Debug.Log($"[ObservationNetworkManager] Created GameObject test line with {sphereCount} spheres from {start} to {end}");
        
        // Check camera and layer setup
        CheckCameraAndLayers();
    }
    
    private void CheckCameraAndLayers()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();
        Debug.Log($"[ObservationNetworkManager] Found {cameras.Length} cameras in scene:");
        
        foreach (Camera cam in cameras)
        {
            Debug.Log($"  Camera: {cam.name}, Culling Mask: {cam.cullingMask}, Clear Flags: {cam.clearFlags}");
            
            // Check if camera can see default layer (layer 0)
            bool canSeeDefaultLayer = (cam.cullingMask & (1 << 0)) != 0;
            Debug.Log($"  Can see Default layer (0): {canSeeDefaultLayer}");
        }
        
        // Check what layer our connection container is on
        if (connectionContainer != null)
        {
            Debug.Log($"[ObservationNetworkManager] Connection container layer: {connectionContainer.gameObject.layer}");
        }
    }
    
    private void CreateNetworkUI()
    {
        if (networkUIPrefab == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[ObservationNetworkManager] No network UI prefab assigned");
            }
            return;
        }
        
        if (targetCanvas == null)
        {
            Debug.LogError("[ObservationNetworkManager] No target Canvas assigned! Please assign the Canvas in the Inspector.");
            return;
        }
        
        // Use manually assigned Canvas and parent
        Transform parent = uiParent != null ? uiParent : targetCanvas.transform;
        
        GameObject uiInstance = Instantiate(networkUIPrefab, parent);
        networkUI = uiInstance.GetComponent<ObservationNetworkUI>();
        
        if (networkUI == null)
        {
            networkUI = uiInstance.AddComponent<ObservationNetworkUI>();
        }
        
        networkUI.Initialize(this);
        
        if (showDebugInfo)
        {
            Debug.Log($"[ObservationNetworkManager] Network UI created on Canvas: {targetCanvas.name}");
        }
    }
    
    private void InitializeConnectionPool()
    {
        for (int i = 0; i < 50; i++)
        {
            CreatePooledConnection();
        }
    }
    
    private void CreatePooledConnection()
    {
        GameObject connectionObj = new GameObject("NetworkConnection");
        connectionObj.transform.SetParent(connectionContainer);
        
        NetworkConnection connection = connectionObj.AddComponent<NetworkConnection>();
        connection.SetActive(false);
        
        connectionPool.Enqueue(connection);
        
        Debug.Log($"[ObservationNetworkManager] Created pooled connection: {connectionObj.name}, active: {connectionObj.activeInHierarchy}");
    }
    
    private void InitializeCategoryFilters()
    {
        // Initialize common iNaturalist categories
        string[] categories = { "Plantae", "Animalia", "Aves", "Fungi", "Insecta", "Arachnida", "unknown" };
        
        foreach (string category in categories)
        {
            categoryFilterStates[category] = true;
            enabledCategories.Add(category);
        }
    }
    
    private void InitializePlayerTracking()
    {
        // Try to find player transform
        playerTransform = GetPlayerTransform();
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
            hasValidPlayerPosition = true;
            Debug.Log($"[ObservationNetworkManager] Player tracking initialized at {lastPlayerPosition}");
        }
        else
        {
            Debug.LogWarning("[ObservationNetworkManager] Player not found - network updates disabled");
            hasValidPlayerPosition = false;
        }
    }
    
    private void CheckPlayerMovementAndUpdate()
    {
        if (playerTransform == null)
        {
            // Try to find player again
            playerTransform = GetPlayerTransform();
            if (playerTransform == null) return;
        }
        
        Vector3 currentPlayerPosition = playerTransform.position;
        float distanceMoved = Vector3.Distance(lastPlayerPosition, currentPlayerPosition);
        
        // Update if player moved enough distance AND enough time has passed
        if (distanceMoved >= playerMovementThreshold && Time.time - lastUpdateTime >= updateInterval)
        {
            Debug.Log($"[ObservationNetworkManager] Player moved {distanceMoved:F1}m - updating network connections");
            
            lastPlayerPosition = currentPlayerPosition;
            lastUpdateTime = Time.time;
            
            // Update network connections
            UpdateObservationsList();
            UpdateConnections();
        }
    }
    
    private void UpdateObservationsList()
    {
        allObservations.Clear();
        ObservationDisplay[] foundObservations = FindObjectsOfType<ObservationDisplay>();
        
        Debug.Log($"[ObservationNetworkManager] Found {foundObservations.Length} ObservationDisplay objects in scene");
        
        foreach (var obs in foundObservations)
        {
            Debug.Log($"[ObservationNetworkManager] Checking observation: {obs.name} at {obs.transform.position}");
            
            var data = obs.GetData();
            if (data != null)
            {
                allObservations.Add(obs);
                string displayName = data.taxon?.preferred_common_name ?? data.taxon?.name ?? "Unknown Species";
                Debug.Log($"[ObservationNetworkManager] ✓ Added observation: {displayName} at {obs.transform.position}");
            }
            else
            {
                Debug.Log($"[ObservationNetworkManager] ✗ Observation found but no data yet: {obs.name} at {obs.transform.position}");
            }
        }
        
        Debug.Log($"[ObservationNetworkManager] Total observations with data: {allObservations.Count}/{foundObservations.Length}");
        
        // Update category states based on current observations
        UpdateCategoryStates();
    }
    
    private void UpdateCategoryStates()
    {
        HashSet<string> currentCategories = new HashSet<string>();
        
        foreach (var obs in allObservations)
        {
            var data = obs.GetData();
            if (data?.taxon != null)
            {
                string category = data.taxon.iconic_taxon_name ?? "unknown";
                currentCategories.Add(category);
                
                // Add to filter states if not present
                if (!categoryFilterStates.ContainsKey(category))
                {
                    categoryFilterStates[category] = true;
                    enabledCategories.Add(category);
                }
            }
        }
        
        // Notify UI of category changes
        if (networkUI != null)
        {
            // Convert to species format for UI compatibility
            Dictionary<string, bool> speciesStates = new Dictionary<string, bool>();
            foreach (var kvp in categoryFilterStates)
            {
                if (currentCategories.Contains(kvp.Key))
                {
                    speciesStates[kvp.Key] = kvp.Value;
                }
            }
            networkUI.UpdateSpeciesList(speciesStates);
        }
    }
    
    private void UpdateConnections()
    {
        // Clear existing connections
        ClearConnections();
        
        if (allObservations.Count < 2)
        {
            return;
        }
        
        List<ObservationDisplay> filteredObservations = GetFilteredObservations();
        
        if (filteredObservations.Count < 2)
        {
            return;
        }
        
        // Create new connections
        CreateConnections(filteredObservations);
        
        if (showDebugInfo)
        {
            Debug.Log($"[ObservationNetworkManager] Updated {activeConnections.Count} connections between {filteredObservations.Count} observations");
        }
    }
    
    private List<ObservationDisplay> GetFilteredObservations()
    {
        List<ObservationDisplay> filtered = new List<ObservationDisplay>();
        
        foreach (var obs in allObservations)
        {
            var data = obs.GetData();
            if (data?.taxon != null)
            {
                string category = data.taxon.iconic_taxon_name ?? "unknown";
                if (enabledCategories.Contains(category))
                {
                    filtered.Add(obs);
                }
            }
        }
        
        return filtered;
    }
    
    private void CreateConnections(List<ObservationDisplay> observations)
    {
        Debug.Log($"[ObservationNetworkManager] CreateConnections called with {observations.Count} observations");
        
        // Find player position
        Transform player = GetPlayerTransform();
        Vector3 playerPos = player != null ? player.position : Vector3.zero;
        
        // Filter observations by proximity to player
        var playerNearbyObs = observations
            .Where(obs => {
                float distToPlayer = Vector3.Distance(obs.transform.position, playerPos);
                return distToPlayer <= playerProximityRange;
            })
            .OrderBy(obs => Vector3.Distance(obs.transform.position, playerPos))
            .Take(maxObservationsToProcess)
            .ToList();
            
        Debug.Log($"[ObservationNetworkManager] Filtered to {playerNearbyObs.Count} observations near player at {playerPos}");
        
        int connectionsCreated = 0;
        
        for (int i = 0; i < playerNearbyObs.Count && connectionsCreated < maxTotalConnections; i++)
        {
            var obsA = playerNearbyObs[i];
            var dataA = obsA.GetData();
            
            Debug.Log($"[ObservationNetworkManager] Processing observation {i}: {obsA.name} at {obsA.transform.position}");
            
            var nearbyObservations = GetNearbyObservations(obsA, playerNearbyObs);
            int connectionsForThis = 0;
            
            Debug.Log($"[ObservationNetworkManager] Found {nearbyObservations.Count} nearby observations for {obsA.name}");
            
            foreach (var obsB in nearbyObservations)
            {
                if (connectionsForThis >= maxConnectionsPerObservation || connectionsCreated >= maxTotalConnections)
                    break;
                
                var dataB = obsB.GetData();
                
                if (ShouldCreateConnection(dataA, dataB))
                {
                    Debug.Log($"[ObservationNetworkManager] Attempting to create connection between {obsA.name} and {obsB.name}");
                    
                    NetworkConnection connection = GetPooledConnection();
                    if (connection != null)
                    {
                        Debug.Log($"[ObservationNetworkManager] Got pooled connection: {connection.name}");
                        
                        connection.SetConnection(obsA.transform.position, obsB.transform.position, defaultConnectionColor);
                        connection.SetActive(true);
                        
                        activeConnections.Add(connection);
                        connectionsCreated++;
                        connectionsForThis++;
                        
                        Debug.Log($"[ObservationNetworkManager] Connection created! Total: {connectionsCreated}");
                    }
                    else
                    {
                        Debug.LogError("[ObservationNetworkManager] Failed to get pooled connection!");
                    }
                }
                else
                {
                    Debug.Log($"[ObservationNetworkManager] Should NOT create connection between {obsA.name} and {obsB.name}");
                }
            }
        }
        
        Debug.Log($"[ObservationNetworkManager] CreateConnections finished. Created {connectionsCreated} connections total.");
        
        // DISABLED: DEBUGGING: Create a visible test line at the same position as first connection
        // if (activeConnections.Count > 0 && activeConnections[0] != null)
        // {
        //     Vector3[] points = activeConnections[0].GetConnectionPoints();
        //     if (points.Length >= 2)
        //     {
        //         CreateVisibleTestLineAtPosition(points[0], points[1]);
        //         Debug.Log($"[ObservationNetworkManager] Created visible test line at connection position: {points[0]} to {points[1]}");
        //     }
        // }
    }
    
    private List<ObservationDisplay> GetNearbyObservations(ObservationDisplay centerObs, List<ObservationDisplay> allObs)
    {
        var nearby = new List<(ObservationDisplay obs, float distance)>();
        
        foreach (var obs in allObs)
        {
            if (obs == centerObs) continue;
            
            float distance = Vector3.Distance(centerObs.transform.position, obs.transform.position);
            
            if (distance >= minConnectionDistance && distance <= maxConnectionDistance)
            {
                nearby.Add((obs, distance));
            }
        }
        
        return nearby.OrderBy(x => x.distance)
                    .Take(maxConnectionsPerObservation)
                    .Select(x => x.obs)
                    .ToList();
    }
    
    private bool ShouldCreateConnection(ObservationData dataA, ObservationData dataB)
    {
        if (dataA == null || dataB == null) return false;
        
        string categoryA = dataA.taxon?.iconic_taxon_name ?? "unknown";
        string categoryB = dataB.taxon?.iconic_taxon_name ?? "unknown";
        
        if (connectSameSpeciesOnly)
        {
            return categoryA == categoryB;
        }
        
        return true; // Connect all enabled categories
    }
    
    private Transform GetPlayerTransform()
    {
        // Try finding by tag first
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }
        
        // Try finding by KinematicCharacterController
        var characterController = FindObjectOfType<KinematicCharacterController.KinematicCharacterMotor>();
        if (characterController != null)
        {
            return characterController.transform;
        }
        
        Debug.LogWarning("[ObservationNetworkManager] Could not find player transform");
        return null;
    }
    
    private NetworkConnection GetPooledConnection()
    {
        Debug.Log($"[ObservationNetworkManager] GetPooledConnection: Pool has {connectionPool.Count} connections");
        
        if (connectionPool.Count > 0)
        {
            var connection = connectionPool.Dequeue();
            Debug.Log($"[ObservationNetworkManager] Dequeued connection: {connection?.name}");
            return connection;
        }
        
        Debug.Log("[ObservationNetworkManager] Pool empty, creating new connection");
        CreatePooledConnection();
        return connectionPool.Count > 0 ? connectionPool.Dequeue() : null;
    }
    
    private void ClearConnections()
    {
        foreach (var connection in activeConnections)
        {
            connection.SetActive(false);
            connectionPool.Enqueue(connection);
        }
        activeConnections.Clear();
    }
    
    // Public methods for UI interaction
    public void SetNetworkEnabled(bool enabled)
    {
        networkEnabled = enabled;
        if (!enabled)
        {
            ClearConnections();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[ObservationNetworkManager] Network {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    public void SetCategoryFilter(string category, bool enabled)
    {
        if (categoryFilterStates.ContainsKey(category))
        {
            categoryFilterStates[category] = enabled;
            
            if (enabled)
            {
                enabledCategories.Add(category);
            }
            else
            {
                enabledCategories.Remove(category);
            }
            
            // Force immediate update if network is enabled
            if (networkEnabled)
            {
                UpdateConnections();
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[ObservationNetworkManager] Category '{category}' {(enabled ? "enabled" : "disabled")}");
            }
        }
    }
    
    public void EnableAllSpecies()
    {
        foreach (var category in categoryFilterStates.Keys.ToList())
        {
            categoryFilterStates[category] = true;
            enabledCategories.Add(category);
        }
        
        if (networkEnabled)
        {
            UpdateConnections();
        }
    }
    
    public void DisableAllSpecies()
    {
        foreach (var category in categoryFilterStates.Keys.ToList())
        {
            categoryFilterStates[category] = false;
        }
        enabledCategories.Clear();
        
        if (networkEnabled)
        {
            UpdateConnections();
        }
    }
    
    public Dictionary<string, bool> GetSpeciesFilterStates()
    {
        return new Dictionary<string, bool>(categoryFilterStates);
    }
    
    private void CreateVisibleTestLineAtPosition(Vector3 start, Vector3 end)
    {
        // Create the same type of test line as our working one, but at connection position
        GameObject testLine = new GameObject("ConnectionPositionTestLine");
        testLine.transform.SetParent(connectionContainer);
        
        LineRenderer lr = testLine.AddComponent<LineRenderer>();
        
        // Use exact same setup as working test line
        Material testMaterial = new Material(Shader.Find("Unlit/Color"));
        testMaterial.color = Color.green; // Use green to distinguish from red test line
        lr.material = testMaterial;
        lr.startWidth = 5f;
        lr.endWidth = 5f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.allowOcclusionWhenDynamic = false;
        
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        Debug.Log($"[ObservationNetworkManager] Created green test line from {start} to {end}");
    }
    
    // PUBLIC METHOD: Trigger connections when player approaches an observation
    public void TriggerConnectionsFromObservation(ObservationDisplay triggeringObservation)
    {
        Debug.Log($"[ObservationNetworkManager] TRIGGERED: Creating connections from {triggeringObservation.name}");
        
        // Clear existing connections first
        ClearConnections();
        
        // Force update observations list
        UpdateObservationsList();
        
        Debug.Log($"[ObservationNetworkManager] TRIGGERED: Found {allObservations.Count} observations with data");
        
        if (allObservations.Count >= 2)
        {
            // Enable network and force all categories
            networkEnabled = true;
            enabledCategories.Clear();
            enabledCategories.Add("Plantae");
            enabledCategories.Add("Animalia");
            enabledCategories.Add("Aves");
            enabledCategories.Add("Fungi");
            enabledCategories.Add("Insecta");
            enabledCategories.Add("Arachnida");
            enabledCategories.Add("unknown");
            
            // Create connections
            CreateConnections(allObservations);
            
            Debug.Log($"[ObservationNetworkManager] TRIGGERED: Created {activeConnections.Count} connections total!");
            
            // Create a guaranteed visible test line between first two observations
            if (allObservations.Count >= 2)
            {
                Vector3 pos1 = allObservations[0].transform.position + Vector3.up * 10f;
                Vector3 pos2 = allObservations[1].transform.position + Vector3.up * 10f;
                CreateVisibleTestLineAtPosition(pos1, pos2);
                Debug.Log($"[ObservationNetworkManager] TRIGGERED: Created guaranteed test line between observations!");
            }
        }
        else
        {
            Debug.LogWarning($"[ObservationNetworkManager] TRIGGERED: Not enough observations ({allObservations.Count}) to create connections");
        }
    }
    
    // TESTING METHOD - Force connections to see lines
    private void TestConnections()
    {
        testAttempts++;
        Debug.Log($"[ObservationNetworkManager] TESTING: Test attempt #{testAttempts} - Force updating connections...");
        
        UpdateObservationsList();
        
        Debug.Log($"[ObservationNetworkManager] TESTING: Found {allObservations.Count} observations");
        
        if (allObservations.Count >= 2)
        {
            // Force all categories enabled
            enabledCategories.Clear();
            enabledCategories.Add("Plantae");
            enabledCategories.Add("Animalia");
            enabledCategories.Add("Aves");
            enabledCategories.Add("Fungi");
            enabledCategories.Add("Insecta");
            enabledCategories.Add("Arachnida");
            enabledCategories.Add("unknown");
            
            UpdateConnections();
            
            Debug.Log($"[ObservationNetworkManager] TESTING: Created {activeConnections.Count} connections");
        }
        else if (testAttempts < maxTestAttempts)
        {
            Debug.Log($"[ObservationNetworkManager] TESTING: Not enough observations yet, retrying in 3 seconds...");
            Invoke("TestConnections", 3f);
        }
        else
        {
            Debug.LogWarning($"[ObservationNetworkManager] TESTING: Failed to find observations after {maxTestAttempts} attempts. Check if iNaturalist data is loading properly.");
            
            // Try creating connections between ANY ObservationDisplay objects, even without data
            ForceTestConnectionsWithoutData();
        }
    }
    
    private void ForceTestConnectionsWithoutData()
    {
        Debug.Log("[ObservationNetworkManager] FORCE TEST: Creating connections between any ObservationDisplay objects found...");
        
        ObservationDisplay[] allDisplays = FindObjectsOfType<ObservationDisplay>();
        Debug.Log($"[ObservationNetworkManager] FORCE TEST: Found {allDisplays.Length} ObservationDisplay objects total");
        
        if (allDisplays.Length >= 2)
        {
            // Create a simple connection between first two observations
            Vector3 pos1 = allDisplays[0].transform.position;
            Vector3 pos2 = allDisplays[1].transform.position;
            
            Debug.Log($"[ObservationNetworkManager] FORCE TEST: Connecting {allDisplays[0].name} at {pos1} to {allDisplays[1].name} at {pos2}");
            
            // Get a pooled connection
            NetworkConnection connection = GetPooledConnection();
            if (connection != null)
            {
                connection.SetConnection(pos1, pos2, Color.cyan);
                connection.SetActive(true);
                activeConnections.Add(connection);
                
                Debug.Log($"[ObservationNetworkManager] FORCE TEST: Connection created successfully! Active connections: {activeConnections.Count}");
            }
            else
            {
                Debug.LogError("[ObservationNetworkManager] FORCE TEST: Failed to get pooled connection!");
            }
        }
        else
        {
            Debug.LogWarning("[ObservationNetworkManager] FORCE TEST: Not enough ObservationDisplay objects found for connections");
        }
    }
    
    // Debug visualization using Gizmos
    void OnDrawGizmos()
    {
        if (!networkEnabled || !showDebugInfo) return;
        
        // Draw all active connections as debug lines
        Gizmos.color = Color.cyan;
        foreach (var connection in activeConnections)
        {
            if (connection != null && connection.IsActive())
            {
                Vector3[] points = connection.GetConnectionPoints();
                if (points.Length >= 2)
                {
                    Gizmos.DrawLine(points[0], points[1]);
                    Gizmos.DrawWireSphere(points[0], 1f);
                    Gizmos.DrawWireSphere(points[1], 1f);
                }
            }
        }
        
        // Also draw lines between all observations for comparison
        if (allObservations.Count >= 2)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < allObservations.Count; i++)
            {
                for (int j = i + 1; j < Mathf.Min(i + 4, allObservations.Count); j++) // Limit to avoid too many lines
                {
                    Vector3 start = allObservations[i].transform.position + Vector3.up * 10f;
                    Vector3 end = allObservations[j].transform.position + Vector3.up * 10f;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
    
    // MANUAL TRIGGER: Press T key to test connections
    public void ManualTriggerConnections()
    {
        Debug.Log("[ObservationNetworkManager] MANUAL TRIGGER: Creating connections...");
        
        // Clear existing connections first
        ClearConnections();
        
        // Force update observations list
        UpdateObservationsList();
        
        Debug.Log($"[ObservationNetworkManager] MANUAL TRIGGER: Found {allObservations.Count} observations with data");
        
        if (allObservations.Count >= 2)
        {
            // Enable network and force all categories
            networkEnabled = true;
            enabledCategories.Clear();
            enabledCategories.Add("Plantae");
            enabledCategories.Add("Animalia");
            enabledCategories.Add("Aves");
            enabledCategories.Add("Fungi");
            enabledCategories.Add("Insecta");
            enabledCategories.Add("Arachnida");
            enabledCategories.Add("unknown");
            
            // Create connections
            CreateConnections(allObservations);
            
            Debug.Log($"[ObservationNetworkManager] MANUAL TRIGGER: Created {activeConnections.Count} connections total!");
            
            // Create a guaranteed visible test line between first two observations
            Vector3 pos1 = allObservations[0].transform.position + Vector3.up * 10f;
            Vector3 pos2 = allObservations[1].transform.position + Vector3.up * 10f;
            // DISABLED: CreateVisibleTestLineAtPosition(pos1, pos2);
            Debug.Log($"[ObservationNetworkManager] MANUAL TRIGGER: Test line creation disabled - using real network connections only!");
        }
        else
        {
            Debug.LogWarning($"[ObservationNetworkManager] MANUAL TRIGGER: Not enough observations ({allObservations.Count}) to create connections");
        }
    }
    
    /// <summary>
    /// Get count of currently active connections for UI display
    /// </summary>
    public int GetActiveConnectionCount()
    {
        return activeConnections.Count(c => c != null && c.IsActive());
    }
    
    /// <summary>
    /// Clear all network connections (for returning to main menu)
    /// </summary>
    public void ClearAllConnections()
    {
        foreach (var connection in activeConnections)
        {
            if (connection != null)
            {
                connection.SetActive(false);
                connectionPool.Enqueue(connection);
            }
        }
        
        activeConnections.Clear();
        Debug.Log("[ObservationNetworkManager] All connections cleared");
    }
}
