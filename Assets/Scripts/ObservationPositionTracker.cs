using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Keeps an observation GameObject positioned at its correct lat/lng coordinates
/// Updates position when the map moves or zooms
/// </summary>
public class ObservationPositionTracker : MonoBehaviour
{
    private AbstractMap map;
    private Vector2d latLng;
    private bool isInitialized = false;
    
    /// <summary>
    /// Initialize the tracker with the map and coordinates
    /// </summary>
    public void Initialize(AbstractMap mapReference, Vector2d coordinates)
    {
        map = mapReference;
        latLng = coordinates;
        isInitialized = true;
    }
    
    void LateUpdate()
    {
        if (!isInitialized || map == null) return;
        
        // Update position based on current map state
        Vector3 worldPosition = map.GeoToWorldPosition(latLng, true);
        transform.position = worldPosition;
    }
}
