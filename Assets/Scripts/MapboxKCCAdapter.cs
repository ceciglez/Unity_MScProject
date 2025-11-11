using System.Collections;
using UnityEngine;
using Mapbox.Unity.Map;
using KinematicCharacterController;

/// <summary>
/// Ensures the Kinematic Character Controller works properly with Mapbox terrain
/// Handles terrain height sampling and proper positioning
/// Adapted for KCC instead of Unity's CharacterController
/// </summary>
public class MapboxKCCAdapter : MonoBehaviour
{
    [Header("Map Reference")]
    [SerializeField] private AbstractMap map;
    
    [Header("Character Reference")]
    [SerializeField] private KinematicCharacterMotor motor;
    
    [Header("Terrain Settings")]
    [SerializeField] private bool stickToTerrain = true;
    [SerializeField] private float heightOffset = 2f; // Player height above terrain
    [SerializeField] private float raycastHeight = 100f; // How high to start raycasts
    [SerializeField] private LayerMask terrainMask = -1;
    
    [Header("Update Settings")]
    [SerializeField] private bool constantUpdate = true;
    [SerializeField] private float updateInterval = 0.1f; // How often to check terrain height
    
    private float lastUpdateTime;
    private bool isInitialized = false;
    
    void Awake()
    {
        // Find KCC motor if not assigned
        if (motor == null)
        {
            motor = GetComponent<KinematicCharacterMotor>();
            if (motor == null)
            {
                Debug.LogError("MapboxKCCAdapter: No KinematicCharacterMotor found!");
                enabled = false;
                return;
            }
        }
        
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map == null)
            {
                Debug.LogWarning("MapboxKCCAdapter: No AbstractMap found in scene!");
            }
        }
    }
    
    void Start()
    {
        // Wait for map to initialize and tiles to load before positioning
        if (map != null)
        {
            map.OnInitialized += OnMapInitialized;
            map.OnUpdated += OnMapUpdated;
            Debug.Log("<color=cyan>MapboxKCCAdapter: Waiting for map initialization...</color>");
        }
        else
        {
            // Fallback if no map reference
            StartCoroutine(WaitAndPositionOnTerrain());
        }
    }
    
    private void OnMapInitialized()
    {
        Debug.Log("<color=green>MapboxKCCAdapter: Map initialized, positioning player...</color>");
        StartCoroutine(WaitAndPositionOnTerrain());
    }
    
    private void OnMapUpdated()
    {
        // Map has updated, reposition if needed
        if (isInitialized && !IsAboveTerrain())
        {
            Debug.Log("<color=yellow>MapboxKCCAdapter: Map updated, checking position...</color>");
            StartCoroutine(WaitAndPositionOnTerrain());
        }
    }
    
    private IEnumerator WaitAndPositionOnTerrain()
    {
        // Wait for map and terrain to initialize
        yield return new WaitForSeconds(2f);
        
        // Get terrain height at map center
        float terrainHeight = 0f;
        Vector3 mapCenterWorld = Vector3.zero;
        
        if (map != null)
        {
            // Get map center position WITHOUT height query first
            mapCenterWorld = map.GeoToWorldPosition(map.CenterLatitudeLongitude, false);
            
            // Now query the actual terrain height at map center
            Vector3 terrainPos = map.GeoToWorldPosition(map.CenterLatitudeLongitude, true);
            terrainHeight = terrainPos.y;
            
            Debug.Log($"<color=cyan>Map Center: (X:{mapCenterWorld.x:F2}, Z:{mapCenterWorld.z:F2})</color>");
            Debug.Log($"<color=yellow>Terrain Height at center: {terrainHeight:F2}</color>");
            
            // Spawn player 10 units above the terrain
            float spawnHeight = terrainHeight + 10f;
            
            // Use KCC's SetPosition method instead of direct transform manipulation
            motor.SetPosition(new Vector3(mapCenterWorld.x, spawnHeight, mapCenterWorld.z));
            
            Debug.Log($"<color=lime>Player positioned at Y={spawnHeight:F2} (Terrain + 10m)</color>");
        }
        
        // Wait a bit more for terrain colliders to be generated
        yield return new WaitForSeconds(1f);
        
        // Check if terrain exists below using Physics.Raycast directly
        bool terrainFound = false;
        int maxAttempts = 20;
        int attempts = 0;
        
        while (!terrainFound && attempts < maxAttempts)
        {
            // Try raycast from player position, but start from above
            float rayStartHeight = Mathf.Max(motor.Transform.position.y + 50f, 100f);
            Vector3 rayOrigin = new Vector3(motor.Transform.position.x, rayStartHeight, motor.Transform.position.z);
            RaycastHit hit;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayStartHeight + 100f, terrainMask))
            {
                terrainFound = true;
                float distanceBelow = motor.Transform.position.y - hit.point.y;
                Debug.Log($"<color=green>✓ Terrain collider detected at Y={hit.point.y:F2}!</color>");
                Debug.Log($"<color=green>  Player at Y={motor.Transform.position.y:F2}, Distance above terrain: {distanceBelow:F2}m</color>");
                break;
            }
            
            yield return new WaitForSeconds(0.3f);
            attempts++;
            
            if (attempts % 5 == 0)
            {
                Debug.Log($"<color=yellow>⏳ Waiting for terrain colliders... Attempt {attempts}/{maxAttempts}</color>");
            }
        }
        
        if (terrainFound)
        {
            Debug.Log("<color=green>✓✓ SUCCESS: Terrain detected. Player will fall naturally with KCC physics.</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange>WARNING: Could not detect terrain colliders. Trying Mapbox height query...</color>");
            
            // Last resort: Try using Mapbox's terrain height query
            if (map != null)
            {
                Vector3 terrainPos = map.GeoToWorldPosition(map.CenterLatitudeLongitude, true);
                if (terrainPos.y > 0 && terrainPos.y < 1000)
                {
                    Debug.Log($"<color=cyan>Using Mapbox terrain height: {terrainPos.y:F2}. Positioning player above it.</color>");
                    Vector3 newPos = new Vector3(terrainPos.x, terrainPos.y + 5f, terrainPos.z);
                    motor.SetPosition(newPos);
                }
            }
        }
        
        isInitialized = true;
        
        // Final status
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"<color=white>=== KCC POSITION INITIALIZED ===</color>");
        Debug.Log($"<color=white>Position: {motor.Transform.position}</color>");
        Debug.Log($"<color=white>Motor Enabled: {motor.enabled}</color>");
    }
    
    void LateUpdate()
    {
        if (!isInitialized || !stickToTerrain || !constantUpdate) return;
        
        // Only update at specified interval to reduce performance impact
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            // Check if player has fallen significantly below terrain (safety net)
            Vector3 terrainPos = GetTerrainPositionBelow();
            if (terrainPos != Vector3.zero)
            {
                float distanceBelow = terrainPos.y - motor.Transform.position.y;
                // If player is more than 5 units below terrain, they've fallen through
                if (distanceBelow > 5f)
                {
                    Debug.LogWarning("Player fell through terrain! Repositioning...");
                    motor.SetPosition(terrainPos + Vector3.up * heightOffset);
                }
            }
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Raycast down to find the terrain below the player
    /// Uses player's XZ position but raycasts from high above
    /// </summary>
    private Vector3 GetTerrainPositionBelow()
    {
        // Start raycast from high above the player's XZ position
        Vector3 rayOrigin = new Vector3(motor.Transform.position.x, raycastHeight, motor.Transform.position.z);
        RaycastHit hit;
        
        // Raycast down from high position
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastHeight * 2f, terrainMask))
        {
            Debug.DrawRay(rayOrigin, Vector3.down * hit.distance, Color.green, 0.1f);
            return hit.point;
        }
        
        // If raycast fails, try using Mapbox's terrain query
        if (map != null)
        {
            Vector3 worldPos = motor.Transform.position;
            Vector3 mapboxHeight = map.GeoToWorldPosition(map.WorldToGeoPosition(worldPos), true);
            
            // Check if we got a valid height
            if (mapboxHeight != Vector3.zero && !float.IsNaN(mapboxHeight.y))
            {
                Debug.DrawLine(rayOrigin, mapboxHeight, Color.cyan, 0.1f);
                return mapboxHeight;
            }
        }
        
        Debug.DrawRay(rayOrigin, Vector3.down * raycastHeight * 2f, Color.red, 0.1f);
        return Vector3.zero;
    }
    
    /// <summary>
    /// Get terrain height at current position
    /// </summary>
    public float GetTerrainHeight()
    {
        Vector3 terrainPos = GetTerrainPositionBelow();
        return terrainPos != Vector3.zero ? terrainPos.y : motor.Transform.position.y;
    }
    
    /// <summary>
    /// Check if player is above valid terrain
    /// </summary>
    public bool IsAboveTerrain()
    {
        return GetTerrainPositionBelow() != Vector3.zero;
    }
    
    /// <summary>
    /// Manually trigger player repositioning on terrain
    /// </summary>
    public void RepositionOnTerrain()
    {
        StartCoroutine(WaitAndPositionOnTerrain());
    }
    
    /// <summary>
    /// Get the player's current position relative to terrain
    /// </summary>
    public float GetDistanceAboveTerrain()
    {
        Vector3 terrainPos = GetTerrainPositionBelow();
        if (terrainPos != Vector3.zero)
        {
            return motor.Transform.position.y - terrainPos.y;
        }
        return -1f;
    }
    
    void OnDrawGizmosSelected()
    {
        if (motor == null) return;
        
        // Visualize raycast in scene view
        Gizmos.color = Color.yellow;
        Vector3 rayOrigin = new Vector3(motor.Transform.position.x, raycastHeight, motor.Transform.position.z);
        Gizmos.DrawLine(rayOrigin, new Vector3(motor.Transform.position.x, -raycastHeight, motor.Transform.position.z));
        
        // Draw current player position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(motor.Transform.position, 1f);
        
        // Draw height offset indicator
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(motor.Transform.position + Vector3.up * heightOffset, 0.5f);
        
        // Try to visualize terrain position below
        Vector3 terrainPos = GetTerrainPositionBelow();
        if (terrainPos != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(terrainPos, 0.8f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(motor.Transform.position, terrainPos);
        }
    }
}
