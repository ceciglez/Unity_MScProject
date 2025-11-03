using System.Collections;
using UnityEngine;
using Mapbox.Unity.Map;

/// <summary>
/// Ensures the First Person Controller works properly with Mapbox terrain
/// Handles terrain height sampling and proper positioning
/// </summary>
public class MapboxTerrainAdapter : MonoBehaviour
{
    [Header("Map Reference")]
    [SerializeField] private AbstractMap map;
    
    [Header("Terrain Settings")]
    [SerializeField] private bool stickToTerrain = true;
    [SerializeField] private float heightOffset = 2f; // Player height above terrain
    [SerializeField] private float raycastHeight = 100f; // How high to start raycasts
    [SerializeField] private LayerMask terrainMask = -1;
    
    [Header("Update Settings")]
    [SerializeField] private bool constantUpdate = true;
    [SerializeField] private float updateInterval = 0.1f; // How often to check terrain height
    
    private CharacterController characterController;
    private FirstPersonController fpsController;
    private float lastUpdateTime;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        fpsController = GetComponent<FirstPersonController>();
        
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map == null)
            {
                Debug.LogWarning("MapboxTerrainAdapter: No AbstractMap found in scene!");
            }
        }
        
        // Disable character controller initially to prevent falling
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }
    
    void Start()
    {
        // Wait for map to initialize and tiles to load before positioning
        if (map != null)
        {
            map.OnInitialized += OnMapInitialized;
            map.OnUpdated += OnMapUpdated;
        }
        else
        {
            // Fallback if no map reference
            StartCoroutine(WaitAndSnapToTerrain());
        }
    }
    
    private void OnMapInitialized()
    {
        // Wait a bit more for tiles to actually generate colliders
        StartCoroutine(WaitAndSnapToTerrain());
    }
    
    private void OnMapUpdated()
    {
        // Map has updated, reposition if needed
        if (!IsAboveTerrain())
        {
            StartCoroutine(WaitAndSnapToTerrain());
        }
    }
    
    private IEnumerator WaitAndSnapToTerrain()
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
            transform.position = new Vector3(mapCenterWorld.x, spawnHeight, mapCenterWorld.z);
            
            Debug.Log($"<color=lime>Player spawned at Y={spawnHeight:F2} (Terrain + 10m)</color>");
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
            float rayStartHeight = Mathf.Max(transform.position.y + 50f, 100f);
            Vector3 rayOrigin = new Vector3(transform.position.x, rayStartHeight, transform.position.z);
            RaycastHit hit;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayStartHeight + 100f, terrainMask))
            {
                terrainFound = true;
                float distanceBelow = transform.position.y - hit.point.y;
                Debug.Log($"<color=green>✓ Terrain collider detected at Y={hit.point.y:F2}!</color>");
                Debug.Log($"<color=green>  Player at Y={transform.position.y:F2}, Distance above terrain: {distanceBelow:F2}m</color>");
                break;
            }
            
            yield return new WaitForSeconds(0.3f);
            attempts++;
            
            if (attempts % 5 == 0)
            {
                Debug.Log($"<color=yellow>⏳ Waiting for terrain colliders... Attempt {attempts}/{maxAttempts}</color>");
            }
        }
        
        // Always enable the character controller after waiting
        if (characterController != null)
        {
            characterController.enabled = true;
            Debug.Log($"<color=lime>CharacterController ENABLED! Player can now move and will fall with gravity.</color>");
        }
        
        if (terrainFound)
        {
            Debug.Log("<color=green>SUCCESS: Terrain detected below player. Player will fall naturally to terrain.</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange>WARNING: Could not detect terrain colliders after waiting. Player may fall indefinitely or terrain may load shortly.</color>");
            
            // Last resort: Try using Mapbox's terrain height query
            if (map != null)
            {
                Vector3 terrainPos = map.GeoToWorldPosition(map.CenterLatitudeLongitude, true);
                if (terrainPos.y > 0 && terrainPos.y < 1000)
                {
                    Debug.Log($"<color=cyan>Using Mapbox terrain height: {terrainPos.y:F2}. Positioning player above it.</color>");
                    Vector3 newPos = new Vector3(terrainPos.x, terrainPos.y + 5f, terrainPos.z);
                    characterController.enabled = false;
                    transform.position = newPos;
                    characterController.enabled = true;
                }
            }
        }
    }
    
    void LateUpdate()
    {
        // Optional: Only snap to terrain if player falls through (safety net)
        if (!stickToTerrain || !constantUpdate) return;
        
        // Only update at specified interval to reduce performance impact
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            // Check if player has fallen significantly below terrain (fallen through)
            Vector3 terrainPos = GetTerrainPositionBelow();
            if (terrainPos != Vector3.zero)
            {
                float distanceBelow = terrainPos.y - transform.position.y;
                // If player is more than 5 units below terrain, they've fallen through - snap them up
                if (distanceBelow > 5f)
                {
                    Debug.LogWarning("Player fell through terrain! Repositioning...");
                    SnapToTerrain();
                }
            }
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Snap player to terrain immediately (useful for teleporting)
    /// </summary>
    public void SnapToTerrain()
    {
        Vector3 terrainPosition = GetTerrainPositionBelow();
        
        if (terrainPosition != Vector3.zero)
        {
            Vector3 targetPosition = terrainPosition + Vector3.up * heightOffset;
            
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = targetPosition;
                characterController.enabled = true;
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }
    
    /// <summary>
    /// Smoothly adjust height to match terrain (less jarring than snap)
    /// </summary>
    private void AdjustHeightToTerrain()
    {
        if (fpsController != null && fpsController.IsGrounded) return; // Let FPS controller handle it when grounded
        
        Vector3 terrainPosition = GetTerrainPositionBelow();
        
        if (terrainPosition != Vector3.zero)
        {
            float targetHeight = terrainPosition.y + heightOffset;
            float currentHeight = transform.position.y;
            
            // If we're significantly below terrain, snap up
            if (currentHeight < targetHeight - 1f)
            {
                Vector3 newPosition = transform.position;
                newPosition.y = targetHeight;
                
                if (characterController != null)
                {
                    characterController.enabled = false;
                    transform.position = newPosition;
                    characterController.enabled = true;
                }
                else
                {
                    transform.position = newPosition;
                }
            }
        }
    }
    
    /// <summary>
    /// Raycast down to find the terrain below the player
    /// Uses player's XZ position but raycasts from high above
    /// </summary>
    private Vector3 GetTerrainPositionBelow()
    {
        // Start raycast from high above the player's XZ position
        Vector3 rayOrigin = new Vector3(transform.position.x, raycastHeight, transform.position.z);
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
            Vector3 worldPos = transform.position;
            Vector3 mapboxHeight = map.GeoToWorldPosition(map.WorldToGeoPosition(worldPos), true);
            
            // Check if we got a valid height (not at 0,0,0)
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
    /// Get terrain height at current position (useful for external scripts)
    /// </summary>
    public float GetTerrainHeight()
    {
        Vector3 terrainPos = GetTerrainPositionBelow();
        return terrainPos != Vector3.zero ? terrainPos.y : transform.position.y;
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
    /// Useful when teleporting or if player gets stuck
    /// </summary>
    public void RepositionOnTerrain()
    {
        StartCoroutine(WaitAndSnapToTerrain());
    }
    
    /// <summary>
    /// Get the player's current position relative to terrain
    /// </summary>
    public float GetDistanceAboveTerrain()
    {
        Vector3 terrainPos = GetTerrainPositionBelow();
        if (terrainPos != Vector3.zero)
        {
            return transform.position.y - terrainPos.y;
        }
        return -1f; // No terrain found
    }
    
    /// <summary>
    /// Debug method to check player controller state
    /// </summary>
    [ContextMenu("Debug Player State")]
    public void DebugPlayerState()
    {
        Debug.Log("=== PLAYER STATE DEBUG ===");
        Debug.Log($"Position: {transform.position}");
        Debug.Log($"CharacterController Enabled: {(characterController != null ? characterController.enabled.ToString() : "NULL")}");
        Debug.Log($"FPSController Enabled: {(fpsController != null ? fpsController.enabled.ToString() : "NULL")}");
        
        Vector3 terrainPos = GetTerrainPositionBelow();
        if (terrainPos != Vector3.zero)
        {
            Debug.Log($"<color=green>Terrain Found at Y={terrainPos.y:F2}, Distance above: {transform.position.y - terrainPos.y:F2}m</color>");
        }
        else
        {
            Debug.Log("<color=red>NO TERRAIN DETECTED BELOW PLAYER!</color>");
        }
        
        // Try direct raycast
        Vector3 rayOrigin = new Vector3(transform.position.x, raycastHeight, transform.position.z);
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastHeight * 2f))
        {
            Debug.Log($"<color=cyan>Direct Raycast Hit: {hit.collider.name} at Y={hit.point.y:F2}</color>");
        }
        else
        {
            Debug.Log("<color=red>Direct Raycast: NO HIT</color>");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize raycast in scene view
        Gizmos.color = Color.yellow;
        Vector3 rayOrigin = new Vector3(transform.position.x, raycastHeight, transform.position.z);
        Gizmos.DrawLine(rayOrigin, new Vector3(transform.position.x, -raycastHeight, transform.position.z));
        
        // Draw current player position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // Draw height offset indicator
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * heightOffset, 0.5f);
        
        // Try to visualize terrain position below
        Vector3 terrainPos = GetTerrainPositionBelow();
        if (terrainPos != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(terrainPos, 0.8f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, terrainPos);
        }
    }
}
