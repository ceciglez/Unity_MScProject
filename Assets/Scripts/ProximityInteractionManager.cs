using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Proximity-based interaction system for observations
/// Triggers tooltip when player/camera is close to an observation
/// </summary>
public class ProximityInteractionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AbstractMap map;
    [SerializeField] private ObservationTooltip tooltip;
    [SerializeField] private Transform playerPosition; // If null, uses map center
    
    [Header("Proximity Settings")]
    [SerializeField] private float proximityRadius = 50f; // Meters in real world
    [SerializeField] private float checkInterval = 0.1f; // How often to check (seconds)
    [SerializeField] private bool showProximityGizmo = true;
    
    [Header("Player Marker (Optional)")]
    [SerializeField] private GameObject playerMarker;
    [SerializeField] private bool createPlayerMarker = true;
    [SerializeField] private float playerMarkerHeight = 2f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool highlightNearbyObservations = true;
    [SerializeField] private float highlightScale = 1.3f;
    
    private ObservationDisplay closestObservation;
    private ObservationDisplay[] allObservations;
    private float checkTimer = 0f;
    private Vector3 lastPlayerWorldPos;
    
    void Start()
    {
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map == null)
            {
                Debug.LogError("No AbstractMap found!");
                enabled = false;
                return;
            }
        }
        
        if (tooltip == null)
        {
            tooltip = FindObjectOfType<ObservationTooltip>();
        }
        
        // Create player marker if needed
        if (createPlayerMarker && playerMarker == null)
        {
            CreatePlayerMarker();
        }
        
        // Initial check
        UpdateObservationsList();
    }
    
    void Update()
    {
        // Periodic check instead of every frame for performance
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckProximity();
        }
        
        // Update player marker position
        UpdatePlayerMarker();
    }
    
    private void CheckProximity()
    {
        // Update list of observations periodically
        UpdateObservationsList();
        
        if (allObservations == null || allObservations.Length == 0)
            return;
        
        // Get player position in world space
        Vector3 playerWorldPos = GetPlayerWorldPosition();
        
        ObservationDisplay newClosest = null;
        float closestDistance = float.MaxValue;
        
        // Find closest observation within radius
        foreach (ObservationDisplay obs in allObservations)
        {
            if (obs == null) continue;
            
            float distance = Vector3.Distance(playerWorldPos, obs.transform.position);
            
            // Check if within proximity radius
            if (distance <= proximityRadius && distance < closestDistance)
            {
                closestDistance = distance;
                newClosest = obs;
            }
            
            // Visual feedback for nearby observations
            if (highlightNearbyObservations)
            {
                if (distance <= proximityRadius)
                {
                    // Highlight - make slightly bigger
                    obs.transform.localScale = Vector3.one * highlightScale;
                }
                else
                {
                    // Normal size
                    obs.transform.localScale = Vector3.one;
                }
            }
        }
        
        // Update tooltip if closest observation changed
        if (newClosest != closestObservation)
        {
            if (closestObservation != null && highlightNearbyObservations)
            {
                // Reset previous closest
                closestObservation.transform.localScale = Vector3.one;
            }
            
            closestObservation = newClosest;
            
            if (closestObservation != null)
            {
                // Show tooltip for new closest observation
                if (tooltip != null)
                {
                    tooltip.Show(closestObservation.GetData());
                }
                
                Debug.Log($"Near observation: {closestObservation.GetData().taxon?.preferred_common_name}");
            }
            else
            {
                // Hide tooltip if no nearby observations
                if (tooltip != null)
                {
                    tooltip.Hide();
                }
            }
        }
    }
    
    private Vector3 GetPlayerWorldPosition()
    {
        if (playerPosition != null)
        {
            return playerPosition.position;
        }
        else
        {
            // Use map center position
            Vector2d mapCenter = map.CenterLatitudeLongitude;
            Vector3 worldPos = map.GeoToWorldPosition(mapCenter, true);
            worldPos.y = playerMarkerHeight; // Elevation
            return worldPos;
        }
    }
    
    private void UpdateObservationsList()
    {
        allObservations = FindObjectsOfType<ObservationDisplay>();
    }
    
    private void CreatePlayerMarker()
    {
        // Create a simple sphere as player marker
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "PlayerMarker";
        marker.transform.localScale = Vector3.one * 2f;
        
        // Make it blue
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 0.5f, 1f, 0.8f); // Blue
            renderer.material = mat;
        }
        
        playerMarker = marker;
        playerPosition = marker.transform;
    }
    
    private void UpdatePlayerMarker()
    {
        if (playerMarker != null && playerPosition == playerMarker.transform)
        {
            // Update marker to map center
            Vector2d mapCenter = map.CenterLatitudeLongitude;
            Vector3 worldPos = map.GeoToWorldPosition(mapCenter, true);
            worldPos.y = playerMarkerHeight;
            playerMarker.transform.position = worldPos;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showProximityGizmo || !Application.isPlaying) return;
        
        // Draw proximity radius
        Vector3 playerPos = GetPlayerWorldPosition();
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(playerPos, proximityRadius);
        
        // Draw line to closest observation
        if (closestObservation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerPos, closestObservation.transform.position);
        }
    }
    
    /// <summary>
    /// Manually set proximity radius
    /// </summary>
    public void SetProximityRadius(float radius)
    {
        proximityRadius = radius;
    }
    
    /// <summary>
    /// Get currently closest observation
    /// </summary>
    public ObservationDisplay GetClosestObservation()
    {
        return closestObservation;
    }
    
    /// <summary>
    /// Get all observations within proximity
    /// </summary>
    public ObservationDisplay[] GetNearbyObservations()
    {
        if (allObservations == null || allObservations.Length == 0)
            return new ObservationDisplay[0];
        
        Vector3 playerPos = GetPlayerWorldPosition();
        System.Collections.Generic.List<ObservationDisplay> nearby = new System.Collections.Generic.List<ObservationDisplay>();
        
        foreach (ObservationDisplay obs in allObservations)
        {
            if (obs == null) continue;
            
            float distance = Vector3.Distance(playerPos, obs.transform.position);
            if (distance <= proximityRadius)
            {
                nearby.Add(obs);
            }
        }
        
        return nearby.ToArray();
    }
    
    /// <summary>
    /// Force update the observations list
    /// </summary>
    public void RefreshObservations()
    {
        UpdateObservationsList();
        CheckProximity();
    }
}
