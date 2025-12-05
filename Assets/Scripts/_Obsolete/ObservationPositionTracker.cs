using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Keeps an observation GameObject positioned at its correct lat/lng coordinates
/// Updates position when the map moves or zooms
/// Ensures it stays above terrain surface (handles exaggerated heights)
/// </summary>
public class ObservationPositionTracker : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private float heightOffset = 2f; // How far above terrain to float
    [SerializeField] private bool useRaycast = false; // Enable for more accurate positioning
    [SerializeField] private float raycastDistance = 2000f;
    [SerializeField] private LayerMask terrainMask = -1;
    
    private AbstractMap map;
    private Vector2d latLng;
    private bool isInitialized = false;
    
    /// <summary>
    /// Initialize the tracker with the map, coordinates, and custom Y offset
    /// </summary>
    public void Initialize(AbstractMap mapReference, Vector2d coordinates, float customYOffset = 2f)
    {
        map = mapReference;
        latLng = coordinates;
        heightOffset = customYOffset; // Use the passed Y offset
        isInitialized = true;
    }
    
    void LateUpdate()
    {
        if (!isInitialized || map == null) return;
        
        // Get world position with terrain height from Mapbox
        // The 'true' parameter includes terrain elevation
        Vector3 worldPosition = map.GeoToWorldPosition(latLng, true);
        
        if (useRaycast)
        {
            // Optional: Use raycast for more precise positioning
            RaycastHit hit;
            Vector3 rayStart = worldPosition + Vector3.up * 100f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, terrainMask))
            {
                worldPosition = hit.point;
            }
        }
        
        // Apply height offset
        transform.position = worldPosition + Vector3.up * heightOffset;
    }
}
