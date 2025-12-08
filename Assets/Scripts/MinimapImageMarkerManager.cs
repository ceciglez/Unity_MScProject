using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mapbox.Utils;

/// <summary>
/// Manages image-based markers on the minimap for iNaturalist observations.
/// Uses sprites/images for proper emoji/icon display with color.
/// </summary>
public class MinimapImageMarkerManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the iNaturalist observation controller")]
    public INaturalistMapController observationController;
    
    [Tooltip("Reference to the minimap")]
    public StaticMapMinimap minimap;
    
    [Tooltip("Container for all minimap markers (should be child of minimap canvas)")]
    public RectTransform markerContainer;
    
    [Header("Marker Settings")]
    [Tooltip("Size of markers in pixels")]
    [Range(16f, 64f)]
    public float markerSize = 32f;
    
    [Tooltip("Update marker positions every frame (disable to save performance)")]
    public bool updateEveryFrame = true;
    
    [Header("Icon Mapping")]
    [Tooltip("Map taxon categories to sprites")]
    public List<TaxonIconMapping> iconMappings = new List<TaxonIconMapping>();
    
    [Tooltip("Default icon if no mapping found")]
    public Sprite defaultIcon;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    [Header("2D Connections")]
    [Tooltip("Enable 2D line connections between markers")]
    public bool enableConnections = true;
    
    [Tooltip("Material for 2D connection lines")]
    public Material connectionLineMaterial;
    
    [Tooltip("Maximum distance for connections (in world units)")]
    public float maxConnectionDistance = 2000f;
    
    [Tooltip("Line width for connections")]
    public float connectionLineWidth = 3f;
    
    [Tooltip("Maximum number of nearest observations to show connections for")]
    public int maxNearestObservations = 15;
    
    [Tooltip("Player transform (will auto-find if null)")]
    public Transform playerTransform;
    
    [Tooltip("Distance player must move before updating connections")]
    public float playerMoveThreshold = 25f;
    
    [Tooltip("Auto-update connections when player moves")]
    public bool autoUpdateOnPlayerMove = true;
    
    [Tooltip("Minimum time between connection updates (seconds)")]
    public float updateCooldown = 1f;
    
    // Private variables
    private Dictionary<int, MinimapImageMarker> activeMarkers = new Dictionary<int, MinimapImageMarker>();
    private Dictionary<string, TaxonIconData> iconMap;
    private GameObject markerPrefab;
    private List<GameObject> connectionLines = new List<GameObject>();
    private Vector3 lastPlayerPosition;
    private bool hasConnectionsActive = false;
    private float lastUpdateTime = 0f;
    
    void Start()
    {
        Debug.Log("[MinimapImageMarkerManager] Starting initialization...");
        
        // Validate references
        if (observationController == null)
        {
            observationController = FindObjectOfType<INaturalistMapController>();
            if (observationController == null)
            {
                Debug.LogError("[MinimapImageMarkerManager] INaturalistMapController not found!");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"[MinimapImageMarkerManager] Found INaturalistMapController: {observationController.gameObject.name}");
            }
        }
        
        if (minimap == null)
        {
            minimap = FindObjectOfType<StaticMapMinimap>();
            if (minimap == null)
            {
                Debug.LogError("[MinimapImageMarkerManager] StaticMapMinimap not found!");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"[MinimapImageMarkerManager] Found StaticMapMinimap: {minimap.gameObject.name}");
            }
        }
        
        // Create marker container if not assigned
        if (markerContainer == null)
        {
            GameObject containerObj = new GameObject("ObservationMarkers");
            markerContainer = containerObj.AddComponent<RectTransform>();
            
            if (minimap.minimapImage != null)
            {
                markerContainer.SetParent(minimap.minimapImage.transform, false);
            }
            else
            {
                markerContainer.SetParent(minimap.transform, false);
            }
            
            markerContainer.anchorMin = Vector2.zero;
            markerContainer.anchorMax = Vector2.one;
            markerContainer.offsetMin = Vector2.zero;
            markerContainer.offsetMax = Vector2.zero;
        }
        
        // Create marker prefab
        markerPrefab = CreateMarkerPrefab();
        
        // Initialize icon mapping
        InitializeIconMap();
        
        // Start updating markers
        StartCoroutine(UpdateMarkersCoroutine());
        
        Debug.Log("[MinimapImageMarkerManager] Initialization complete!");
    }
    
    void Update()
    {
        if (updateEveryFrame && minimap != null && minimap.minimapImage != null)
        {
            UpdateAllMarkerPositions();
        }
        
        // Check for player movement and auto-update connections
        if (autoUpdateOnPlayerMove && hasConnectionsActive)
        {
            CheckPlayerMovement();
        }
        
        // Manual trigger for 2D connections - press L key
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("[MinimapImageMarkerManager] L key pressed - creating/reloading 2D connections!");
            CreateMinimapConnections();
        }
    }
    
    /// <summary>
    /// Initialize the icon mapping dictionary
    /// </summary>
    private void InitializeIconMap()
    {
        iconMap = new Dictionary<string, TaxonIconData>();
        
        // Default color scheme
        Dictionary<string, Color> defaultColors = new Dictionary<string, Color>()
        {
            { "Fungi", new Color(0.6f, 0.4f, 0.2f) },        // Brown
            { "Plantae", new Color(0.2f, 0.8f, 0.3f) },      // Green
            { "Animalia", new Color(0.9f, 0.6f, 0.2f) },     // Orange
            { "Insecta", new Color(1f, 0.8f, 0.2f) },        // Yellow
            { "Arachnida", new Color(0.5f, 0.3f, 0.2f) },    // Dark brown
            { "Aves", new Color(0.3f, 0.6f, 1f) },           // Blue
            { "Mammalia", new Color(0.9f, 0.5f, 0.3f) },     // Orange-red
            { "Reptilia", new Color(0.4f, 0.7f, 0.3f) },     // Light green
            { "Amphibia", new Color(0.3f, 0.8f, 0.6f) },     // Teal
            { "Actinopterygii", new Color(0.2f, 0.5f, 0.9f) }, // Blue
            { "Mollusca", new Color(0.8f, 0.6f, 0.7f) },     // Pink
            { "Unknown", Color.gray }
        };
        
        // Apply custom mappings from inspector
        int mappingsAdded = 0;
        foreach (var mapping in iconMappings)
        {
            if (!string.IsNullOrEmpty(mapping.taxonName))
            {
                Color color = mapping.useCustomColor ? mapping.customColor : 
                              (defaultColors.ContainsKey(mapping.taxonName) ? defaultColors[mapping.taxonName] : Color.white);
                
                iconMap[mapping.taxonName] = new TaxonIconData
                {
                    sprite = mapping.icon,
                    color = color
                };
                
                if (showDebugInfo)
                {
                    Debug.Log($"[MinimapImageMarkerManager] Added mapping: '{mapping.taxonName}' -> Sprite: {(mapping.icon != null ? mapping.icon.name : "NULL")}, Color: {color}");
                }
                mappingsAdded++;
            }
        }
        
        // Add defaults for any missing mappings
        foreach (var kvp in defaultColors)
        {
            if (!iconMap.ContainsKey(kvp.Key))
            {
                iconMap[kvp.Key] = new TaxonIconData
                {
                    sprite = defaultIcon,
                    color = kvp.Value
                };
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Loaded {iconMap.Count} total icon mappings ({mappingsAdded} from Inspector, {iconMap.Count - mappingsAdded} defaults)");
            Debug.Log($"[MinimapImageMarkerManager] Default icon: {(defaultIcon != null ? defaultIcon.name : "NULL")}");
        }
    }
    
    /// <summary>
    /// Get icon data for a taxon category
    /// </summary>
    private TaxonIconData GetIconDataForTaxon(string iconicTaxonName)
    {
        if (string.IsNullOrEmpty(iconicTaxonName))
        {
            if (showDebugInfo)
                Debug.LogWarning($"[MinimapImageMarkerManager] Empty taxon name, using default");
            return iconMap.GetValueOrDefault("Unknown", new TaxonIconData 
            { 
                sprite = defaultIcon, 
                color = Color.gray 
            });
        }
        
        // Try exact match
        if (iconMap.ContainsKey(iconicTaxonName))
        {
            if (showDebugInfo)
                Debug.Log($"[MinimapImageMarkerManager] Found exact match for '{iconicTaxonName}', sprite: {iconMap[iconicTaxonName].sprite?.name}");
            return iconMap[iconicTaxonName];
        }
        
        // Try partial match
        foreach (var kvp in iconMap)
        {
            if (iconicTaxonName.Contains(kvp.Key))
            {
                if (showDebugInfo)
                    Debug.Log($"[MinimapImageMarkerManager] Found partial match: '{iconicTaxonName}' contains '{kvp.Key}', sprite: {kvp.Value.sprite?.name}");
                return kvp.Value;
            }
        }
        
        // Fallback
        if (showDebugInfo)
            Debug.LogWarning($"[MinimapImageMarkerManager] No match found for '{iconicTaxonName}', using default. Available keys: {string.Join(", ", iconMap.Keys)}");
        return new TaxonIconData { sprite = defaultIcon, color = Color.white };
    }
    
    /// <summary>
    /// Coroutine that periodically checks for new/removed observations
    /// </summary>
    private System.Collections.IEnumerator UpdateMarkersCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (observationController != null)
            {
                UpdateMarkers();
            }
        }
    }
    
    /// <summary>
    /// Sync minimap markers with current observations
    /// </summary>
    public void UpdateMarkers()
    {
        if (observationController == null) return;
        
        // Access observations via reflection
        var observationsField = typeof(INaturalistMapController).GetField("observations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (observationsField == null) return;
        
        List<ObservationData> observations = observationsField.GetValue(observationController) as List<ObservationData>;
        
        if (observations == null || observations.Count == 0)
        {
            ClearAllMarkers();
            return;
        }
        
        HashSet<int> currentObservationIds = new HashSet<int>();
        
        foreach (var obs in observations)
        {
            currentObservationIds.Add(obs.id);
            
            if (!activeMarkers.ContainsKey(obs.id))
            {
                CreateMarkerForObservation(obs);
            }
        }
        
        // Remove old markers
        List<int> idsToRemove = new List<int>();
        foreach (var kvp in activeMarkers)
        {
            if (!currentObservationIds.Contains(kvp.Key))
            {
                idsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int id in idsToRemove)
        {
            RemoveMarker(id);
        }
        
        UpdateAllMarkerPositions();
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Active markers: {activeMarkers.Count}");
        }
    }
    
    /// <summary>
    /// Create a new minimap marker for an observation
    /// </summary>
    private void CreateMarkerForObservation(ObservationData obs)
    {
        if (markerPrefab == null || markerContainer == null) return;
        
        GameObject markerObj = Instantiate(markerPrefab, markerContainer);
        RectTransform markerRect = markerObj.GetComponent<RectTransform>();
        
        markerRect.sizeDelta = new Vector2(markerSize, markerSize);
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        
        MinimapImageMarker marker = markerObj.GetComponent<MinimapImageMarker>();
        
        string taxonName = obs.taxon?.iconic_taxon_name ?? "Unknown";
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Creating marker for taxon: '{taxonName}' - Common name: {obs.taxon?.preferred_common_name}");
        }
        
        TaxonIconData iconData = GetIconDataForTaxon(taxonName);
        
        // CRITICAL: If sprite is null, use default icon
        if (iconData.sprite == null && defaultIcon != null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[MinimapImageMarkerManager] Sprite is NULL for '{taxonName}', using default icon");
            }
            iconData.sprite = defaultIcon;
        }
        
        marker.Initialize(obs, minimap, iconData.sprite, iconData.color);
        
        activeMarkers[obs.id] = marker;
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Created marker: {marker.GetDebugInfo()}");
        }
    }
    
    /// <summary>
    /// Remove a marker by observation ID
    /// </summary>
    private void RemoveMarker(int observationId)
    {
        if (activeMarkers.ContainsKey(observationId))
        {
            if (activeMarkers[observationId] != null && activeMarkers[observationId].gameObject != null)
            {
                Destroy(activeMarkers[observationId].gameObject);
            }
            activeMarkers.Remove(observationId);
        }
    }
    
    /// <summary>
    /// Update positions of all active markers
    /// </summary>
    private void UpdateAllMarkerPositions()
    {
        if (minimap == null || minimap.minimapImage == null) return;
        
        var mapCenterField = typeof(StaticMapMinimap).GetField("mapCenterCoords", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var metersPerPixelField = typeof(StaticMapMinimap).GetField("metersPerPixel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (mapCenterField == null || metersPerPixelField == null) return;
        
        Vector2d mapCenter = (Vector2d)mapCenterField.GetValue(minimap);
        float metersPerPixel = (float)metersPerPixelField.GetValue(minimap);
        
        if (metersPerPixel == 0) return;
        
        RectTransform mapRect = minimap.minimapImage.GetComponent<RectTransform>();
        
        foreach (var kvp in activeMarkers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdatePosition(mapCenter, metersPerPixel, mapRect);
            }
        }
    }
    
    /// <summary>
    /// Clear all markers
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (var kvp in activeMarkers)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        activeMarkers.Clear();
    }
    
    /// <summary>
    /// Create a basic marker prefab
    /// </summary>
    private GameObject CreateMarkerPrefab()
    {
        GameObject prefab = new GameObject("MarkerPrefab");
        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(markerSize, markerSize);
        
        prefab.AddComponent<MinimapImageMarker>();
        
        return prefab;
    }
    
    /// <summary>
    /// Force update all markers immediately
    /// </summary>
    public void ForceUpdate()
    {
        UpdateMarkers();
    }
    
    /// <summary>
    /// Create 2D line connections between minimap markers
    /// </summary>
    public void CreateMinimapConnections()
    {
        if (!enableConnections)
        {
            Debug.Log("[MinimapImageMarkerManager] Connections are disabled");
            return;
        }
        
        // Clear existing connections
        ClearConnections();
        
        Debug.Log($"[MinimapImageMarkerManager] Creating connections between {activeMarkers.Count} markers");
        
        var markerList = new List<MinimapImageMarker>(activeMarkers.Values);
        int connectionsCreated = 0;
        
        // Find player position on minimap for proximity-based connections
        Vector2 playerMinimapPos = GetPlayerMinimapPosition();
        
        // Simple UI-based connections - but now player-proximity aware
        var visibleMarkers = new List<MinimapImageMarker>();
        
        // Filter to only markers that have valid UI positions
        foreach (var marker in markerList)
        {
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            if (markerRect != null && markerRect.gameObject.activeInHierarchy)
            {
                visibleMarkers.Add(marker);
            }
        }
        
        Debug.Log($"[MinimapImageMarkerManager] Found {visibleMarkers.Count} visible markers, player minimap pos: {playerMinimapPos}");
        
        // Sort markers by distance to player on minimap
        var markerDistances = new List<(MinimapImageMarker marker, float distance)>();
        
        foreach (var marker in visibleMarkers)
        {
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            Vector2 markerPos = markerRect.anchoredPosition;
            float distanceToPlayer = Vector2.Distance(markerPos, playerMinimapPos);
            markerDistances.Add((marker, distanceToPlayer));
        }
        
        // Sort by distance to player and take closest ones
        markerDistances.Sort((a, b) => a.distance.CompareTo(b.distance));
        var nearestMarkers = markerDistances.Take(maxNearestObservations).ToList();
        
        Debug.Log($"[MinimapImageMarkerManager] Selected {nearestMarkers.Count} nearest markers to player");
        
        // Connect the nearest markers to player
        for (int i = 0; i < nearestMarkers.Count; i++)
        {
            for (int j = i + 1; j < nearestMarkers.Count; j++)
            {
                var marker1 = nearestMarkers[i].marker;
                var marker2 = nearestMarkers[j].marker;
                
                // Check if markers are close enough on the minimap UI
                RectTransform rect1 = marker1.GetComponent<RectTransform>();
                RectTransform rect2 = marker2.GetComponent<RectTransform>();
                
                Vector2 pos1 = rect1.anchoredPosition;
                Vector2 pos2 = rect2.anchoredPosition;
                float uiDistance = Vector2.Distance(pos1, pos2);
                
                // Only connect if they're reasonably close on the UI (within minimap bounds)
                if (uiDistance > 5f && uiDistance < 800f) // More generous range for more connections
                {
                    CreateConnectionLine(marker1, marker2);
                    connectionsCreated++;
                    Debug.Log($"[MinimapImageMarkerManager] Connected nearest markers (dist to player: {nearestMarkers[i].distance:F1}, {nearestMarkers[j].distance:F1}) with UI distance: {uiDistance}");
                }
            }
        }
        
        Debug.Log($"[MinimapImageMarkerManager] Created {connectionsCreated} 2D connections");
        
        // Track that connections are now active
        hasConnectionsActive = connectionsCreated > 0;
        if (hasConnectionsActive)
        {
            FindPlayerTransform();
            if (playerTransform != null)
            {
                lastPlayerPosition = playerTransform.position;
                Debug.Log($"[MinimapImageMarkerManager] Tracking player position for auto-updates: {lastPlayerPosition}");
            }
        }
    }
    
    /// <summary>
    /// Create a 2D line between two minimap markers
    /// </summary>
    private void CreateConnectionLine(MinimapImageMarker marker1, MinimapImageMarker marker2)
    {
        GameObject lineObj = new GameObject("MinimapConnection");
        lineObj.transform.SetParent(markerContainer, false);
        
        // Add RectTransform for UI positioning
        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        
        // Add Image component for UI line rendering
        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = Color.yellow;
        lineImage.raycastTarget = false;
        
        // Get marker positions
        RectTransform rect1 = marker1.GetComponent<RectTransform>();
        RectTransform rect2 = marker2.GetComponent<RectTransform>();
        
        Vector2 pos1 = rect1.anchoredPosition;
        Vector2 pos2 = rect2.anchoredPosition;
        
        // Calculate line properties
        Vector2 direction = (pos2 - pos1).normalized;
        float distance = Vector2.Distance(pos1, pos2);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Position and rotate the line
        Vector2 centerPos = (pos1 + pos2) * 0.5f;
        lineRect.anchoredPosition = centerPos;
        lineRect.sizeDelta = new Vector2(distance, connectionLineWidth);
        lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        connectionLines.Add(lineObj);
        
        Debug.Log($"[MinimapImageMarkerManager] Created UI line from {pos1} to {pos2}, distance: {distance}, angle: {angle}");
    }
    
    /// <summary>
    /// Clear all existing connection lines
    /// </summary>
    private void ClearConnections()
    {
        int previousCount = connectionLines.Count;
        foreach (var line in connectionLines)
        {
            if (line != null)
            {
                DestroyImmediate(line);
            }
        }
        connectionLines.Clear();
        Debug.Log($"[MinimapImageMarkerManager] Cleared {previousCount} existing connection lines");
        hasConnectionsActive = false;
    }
    
    /// <summary>
    /// Check if player has moved far enough to warrant updating connections
    /// </summary>
    private void CheckPlayerMovement()
    {
        if (playerTransform == null)
        {
            FindPlayerTransform();
            if (playerTransform == null) return;
        }
        
        // Check cooldown to prevent excessive updates
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime < updateCooldown)
        {
            return;
        }
        
        Vector3 currentPlayerPosition = playerTransform.position;
        float distanceMoved = Vector3.Distance(lastPlayerPosition, currentPlayerPosition);
        
        if (distanceMoved >= playerMoveThreshold)
        {
            Debug.Log($"[MinimapImageMarkerManager] Player moved {distanceMoved:F1} units - updating connections (cooldown satisfied)");
            lastUpdateTime = currentTime;
            CreateMinimapConnections(); // This will also update lastPlayerPosition
        }
    }
    
    /// <summary>
    /// Find the player transform using various methods
    /// </summary>
    private void FindPlayerTransform()
    {
        if (playerTransform != null) return;
        
        // Try finding by tag first
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"[MinimapImageMarkerManager] Found player via tag: {playerTransform.name}");
            return;
        }
        
        // Try finding by KinematicCharacterController
        var characterController = FindObjectOfType<KinematicCharacterController.KinematicCharacterMotor>();
        if (characterController != null)
        {
            playerTransform = characterController.transform;
            Debug.Log($"[MinimapImageMarkerManager] Found player via KinematicCharacterMotor: {playerTransform.name}");
            return;
        }
        
        Debug.LogWarning("[MinimapImageMarkerManager] Could not find player transform for auto-updates");
    }
    
    /// <summary>
    /// Get the player's position on the minimap UI
    /// </summary>
    private Vector2 GetPlayerMinimapPosition()
    {
        if (playerTransform == null)
        {
            FindPlayerTransform();
            if (playerTransform == null) return Vector2.zero;
        }
        
        if (minimap == null || minimap.minimapImage == null) return Vector2.zero;
        
        // Get minimap center and scale info
        var mapCenterField = typeof(StaticMapMinimap).GetField("mapCenterCoords", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var metersPerPixelField = typeof(StaticMapMinimap).GetField("metersPerPixel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (mapCenterField == null || metersPerPixelField == null) return Vector2.zero;
        
        Vector2d mapCenter = (Vector2d)mapCenterField.GetValue(minimap);
        float metersPerPixel = (float)metersPerPixelField.GetValue(minimap);
        
        if (metersPerPixel == 0) return Vector2.zero;
        
        // Convert player world position to lat/lng (approximate)
        Vector3 playerWorldPos = playerTransform.position;
        
        // Simple conversion (this is approximate and might need adjustment based on your coordinate system)
        Vector2d playerLatLng = new Vector2d(
            mapCenter.x + (playerWorldPos.z / 111320.0), // z -> latitude
            mapCenter.y + (playerWorldPos.x / (111320.0 * Mathf.Cos(Mathf.Deg2Rad * (float)mapCenter.x))) // x -> longitude
        );
        
        // Convert lat/lng to minimap UI position
        RectTransform mapRect = minimap.minimapImage.GetComponent<RectTransform>();
        Vector2 mapSize = mapRect.rect.size;
        
        float deltaLat = (float)(playerLatLng.x - mapCenter.x);
        float deltaLng = (float)(playerLatLng.y - mapCenter.y);
        
        float pixelOffsetX = deltaLng * 111320.0f * Mathf.Cos(Mathf.Deg2Rad * (float)mapCenter.x) / metersPerPixel;
        float pixelOffsetY = deltaLat * 111320.0f / metersPerPixel;
        
        Vector2 playerMinimapPos = new Vector2(pixelOffsetX, pixelOffsetY);
        
        return playerMinimapPos;
    }
    
    /// <summary>
    /// Get world position of marker's associated observation
    /// </summary>
    private Vector3 GetMarkerWorldPosition(MinimapImageMarker marker)
    {
        if (marker.latLng.x != 0 || marker.latLng.y != 0)
        {
            return new Vector3(
                (float)marker.latLng.y, // longitude -> x
                0f,
                (float)marker.latLng.x  // latitude -> z
            );
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Create a default material for line connections
    /// </summary>
    private Material CreateDefaultLineMaterial()
    {
        Material mat = new Material(Shader.Find("UI/Default"));
        mat.color = Color.yellow;
        return mat;
    }
}

/// <summary>
/// Serializable class for custom icon mappings
/// </summary>
[System.Serializable]
public class TaxonIconMapping
{
    [Tooltip("Taxon name (e.g., 'Fungi', 'Insecta', 'Aves')")]
    public string taxonName;
    
    [Tooltip("Icon sprite (optional - uses colored circle if null)")]
    public Sprite icon;
    
    [Tooltip("Use custom color instead of default")]
    public bool useCustomColor = false;
    
    [Tooltip("Custom color for this taxon")]
    public Color customColor = Color.white;
}

/// <summary>
/// Internal data structure for icon information
/// </summary>
public class TaxonIconData
{
    public Sprite sprite;
    public Color color;
}
