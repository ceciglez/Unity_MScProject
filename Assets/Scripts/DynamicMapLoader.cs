using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Dynamically updates the Mapbox map center as the player moves
/// Ensures terrain tiles load around the player's current position
/// </summary>
public class DynamicMapLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AbstractMap map;
    [SerializeField] private Transform player; // Your FPC
    
    [Header("Update Settings")]
    [SerializeField] private float updateThreshold = 100f; // Distance player must move before map updates
    [SerializeField] private bool continuousUpdate = true;
    [SerializeField] private float updateInterval = 1f; // Check every second
    
    [Header("Map Range Settings")]
    [SerializeField] private int mapRangeInTiles = 3; // How many tiles around player to load
    [SerializeField] private bool snapToTileCenter = true; // Smoother updates
    
    [Header("Player Repositioning")]
    [SerializeField] private bool repositionPlayerAfterUpdate = true;
    [SerializeField] private float repositionDelay = 0.5f; // Wait for terrain to generate
    [SerializeField] private float raycastHeight = 200f;
    [SerializeField] private LayerMask terrainMask = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showUpdateRadius = true;
    
    private Vector3 lastUpdatePosition;
    private float lastUpdateTime;
    private Vector2d currentMapCenter;
    private bool hasInitialized = false;
    private bool waitingForTerrainUpdate = false;
    private CharacterController characterController;
    
    void Awake()
    {
        // Find map if not assigned
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map == null)
            {
                Debug.LogError("DynamicMapLoader: No AbstractMap found!");
                enabled = false;
                return;
            }
        }
        
        // Find player if not assigned
        if (player == null)
        {
            player = transform; // Assume this script is on player
        }
        
        // Get CharacterController from player
        if (player != null)
        {
            characterController = player.GetComponent<CharacterController>();
        }
    }
    
    void Start()
    {
        if (map != null)
        {
            currentMapCenter = map.CenterLatitudeLongitude;
            lastUpdatePosition = player.position;
            hasInitialized = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"DynamicMapLoader initialized at {currentMapCenter.x}, {currentMapCenter.y}");
            }
        }
    }
    
    /// <summary>
    /// Get current zoom level as int
    /// </summary>
    private int GetZoomLevel()
    {
        return Mathf.RoundToInt(map.Zoom);
    }
    
    void Update()
    {
        if (!hasInitialized || !continuousUpdate || map == null || player == null)
            return;
        
        // Only check at intervals to save performance
        if (Time.time - lastUpdateTime < updateInterval)
            return;
        
        lastUpdateTime = Time.time;
        
        // Check if player has moved far enough to warrant an update
        float distanceMoved = Vector3.Distance(player.position, lastUpdatePosition);
        
        if (distanceMoved >= updateThreshold)
        {
            UpdateMapCenter();
        }
    }
    
    /// <summary>
    /// Update the map center to the player's current position
    /// </summary>
    private void UpdateMapCenter()
    {
        // Convert player's world position to lat/lng
        Vector2d playerLatLng = map.WorldToGeoPosition(player.position);
        
        int currentZoom = GetZoomLevel();
        
        if (snapToTileCenter)
        {
            // Snap to tile boundaries for smoother loading
            // This prevents constant tiny updates
            playerLatLng = SnapToTileGrid(playerLatLng, currentZoom);
        }
        
        // Only update if position actually changed (after snapping)
        if (HasPositionChanged(playerLatLng, currentMapCenter))
        {
            if (showDebugInfo)
            {
                Debug.Log($"Updating map center from {currentMapCenter.x:F6},{currentMapCenter.y:F6} " +
                         $"to {playerLatLng.x:F6},{playerLatLng.y:F6}");
            }
            
            // Update the map
            map.UpdateMap(playerLatLng, currentZoom);
            
            // Store new values
            currentMapCenter = playerLatLng;
            lastUpdatePosition = player.position;
            
            // Schedule player repositioning after terrain loads
            if (repositionPlayerAfterUpdate)
            {
                waitingForTerrainUpdate = true;
                Invoke("RepositionPlayerOnTerrain", repositionDelay);
            }
        }
    }
    
    /// <summary>
    /// Reposition player on the newly loaded terrain
    /// </summary>
    private void RepositionPlayerOnTerrain()
    {
        waitingForTerrainUpdate = false;
        
        Vector3 terrainPosition = GetTerrainPositionBelow();
        
        if (terrainPosition != Vector3.zero)
        {
            // Position player on terrain with a small offset
            Vector3 targetPosition = terrainPosition + Vector3.up * 2f;
            
            if (characterController != null)
            {
                // Disable CharacterController temporarily to move player
                characterController.enabled = false;
                player.position = targetPosition;
                characterController.enabled = true;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Player repositioned on terrain at height: {targetPosition.y}");
                }
            }
            else
            {
                player.position = targetPosition;
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("DynamicMapLoader: Could not find terrain below player after update!");
        }
    }
    
    /// <summary>
    /// Raycast down to find the terrain below the player
    /// </summary>
    private Vector3 GetTerrainPositionBelow()
    {
        Vector3 rayOrigin = player.position + Vector3.up * raycastHeight;
        RaycastHit hit;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastHeight * 2f, terrainMask))
        {
            if (showDebugInfo)
            {
                Debug.DrawRay(rayOrigin, Vector3.down * hit.distance, Color.green, 2f);
            }
            return hit.point;
        }
        
        if (showDebugInfo)
        {
            Debug.DrawRay(rayOrigin, Vector3.down * raycastHeight * 2f, Color.red, 2f);
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Snap coordinates to tile grid for smoother updates
    /// </summary>
    private Vector2d SnapToTileGrid(Vector2d latLng, int zoom)
    {
        // Calculate tile coordinates
        double n = Mathf.Pow(2, zoom);
        double latRad = latLng.x * Mathf.Deg2Rad;
        
        double tileX = ((latLng.y + 180.0) / 360.0) * n;
        double tileY = ((1.0 - Mathf.Log(Mathf.Tan((float)latRad) + 
                       (1.0f / Mathf.Cos((float)latRad))) / Mathf.PI) / 2.0) * n;
        
        // Snap to integer tile
        tileX = Mathf.Floor((float)tileX);
        tileY = Mathf.Floor((float)tileY);
        
        // Convert back to lat/lng (center of tile)
        double lng = (tileX / n) * 360.0 - 180.0;
        double latRad2 = System.Math.Atan(System.Math.Sinh(System.Math.PI * (1 - 2 * tileY / n)));
        double lat = latRad2 * Mathf.Rad2Deg;
        
        return new Vector2d(lat, lng);
    }
    
    /// <summary>
    /// Check if position has changed enough to matter
    /// </summary>
    private bool HasPositionChanged(Vector2d newPos, Vector2d oldPos)
    {
        double threshold = 0.0001; // Very small difference in lat/lng
        return Mathf.Abs((float)(newPos.x - oldPos.x)) > threshold ||
               Mathf.Abs((float)(newPos.y - oldPos.y)) > threshold;
    }
    
    /// <summary>
    /// Manually force a map update (useful for teleporting)
    /// </summary>
    public void ForceMapUpdate()
    {
        UpdateMapCenter();
    }
    
    /// <summary>
    /// Set the update threshold distance
    /// </summary>
    public void SetUpdateThreshold(float threshold)
    {
        updateThreshold = threshold;
        if (showDebugInfo)
        {
            Debug.Log($"Update threshold set to {threshold} units");
        }
    }
    
    /// <summary>
    /// Enable or disable continuous updates
    /// </summary>
    public void SetContinuousUpdate(bool enabled)
    {
        continuousUpdate = enabled;
        if (showDebugInfo)
        {
            Debug.Log($"Continuous update: {enabled}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showUpdateRadius || player == null)
            return;
        
        // Draw update threshold radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(lastUpdatePosition, updateThreshold);
        
        // Draw line from last update position to current position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(lastUpdatePosition, player.position);
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;
        
        // Draw update threshold radius (solid)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(player.position, updateThreshold);
        
        // Draw current position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(player.position, 5f);
    }
}