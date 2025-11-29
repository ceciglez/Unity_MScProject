using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using Mapbox.Utils;

/// <summary>
/// Displays a static Mapbox map image as a minimap with player position marker.
/// Uses Mapbox Static Images API for 2D overhead view.
/// Much more performant than rendering a second 3D map.
/// </summary>
public class StaticMapMinimap : MonoBehaviour
{
    [Header("Map References")]
    [Tooltip("Reference to the main AbstractMap to get player location")]
    public Mapbox.Unity.Map.AbstractMap mainMap;
    
    [Tooltip("The player transform to track")]
    public Transform playerTransform;
    
    [Header("UI Elements")]
    [Tooltip("UI Image component to display the map")]
    public RawImage minimapImage;
    
    [Tooltip("UI Image for the player position marker (arrow/dot)")]
    public RectTransform playerMarker;
    
    [Header("Map Configuration")]
    [Tooltip("Mapbox access token (will use mainMap's token if not provided)")]
    public string mapboxAccessToken = "";
    
    [Tooltip("Map style - streets-v11, light-v10, dark-v10, satellite-streets-v11, outdoors-v11")]
    public string mapStyle = "streets-v11";
    
    [Tooltip("Map width in pixels (higher = better quality but slower to load)")]
    [Range(256, 1280)]
    public int mapWidth = 512;
    
    [Tooltip("Map height in pixels")]
    [Range(256, 1280)]
    public int mapHeight = 512;
    
    [Tooltip("Map zoom level (higher = more zoomed in)")]
    [Range(10, 18)]
    public int zoomLevel = 15;
    
    [Tooltip("Radius around player to show in meters (approximate)")]
    [Range(100f, 2000f)]
    public float mapRadiusMeters = 500f;
    
    [Header("Update Settings")]
    [Tooltip("Regenerate map image when player moves this distance (meters) - lower = more updates")]
    [Range(10f, 200f)]
    public float updateDistanceThreshold = 50f;
    
    [Tooltip("Minimum time between map updates (seconds)")]
    [Range(0.5f, 10f)]
    public float updateCooldown = 2f;
    
    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool debugMode = false;
    
    private Texture2D currentMapTexture;
    private Vector2d lastMapCenter;
    private float lastUpdateTime;
    private bool isLoadingMap = false;
    
    void Start()
    {
        if (mainMap == null)
        {
            mainMap = FindObjectOfType<Mapbox.Unity.Map.AbstractMap>();
        }
        
        if (string.IsNullOrEmpty(mapboxAccessToken) && mainMap != null)
        {
            // Try to get token from main map (may not be accessible depending on Mapbox SDK version)
            // You might need to set this manually in Inspector
            mapboxAccessToken = "pk.eyJ1IjoicGltaWthIiwiYSI6ImNtaGk2dnJpNzB2enUyanFyanIxZGpyMDMifQ.DwWpKIUfpc0X-laRco2jmA";
        }
        
        if (playerTransform == null)
        {
            Debug.LogError("[StaticMapMinimap] Player transform not assigned!");
            return;
        }
        
        // Wait for map to initialize before loading minimap
        StartCoroutine(WaitForMapAndLoad());
    }
    
    private IEnumerator WaitForMapAndLoad()
    {
        // Wait until map is initialized (or timeout after 10 seconds)
        float timeout = 10f;
        float elapsed = 0f;
        
        while (mainMap != null && !mainMap.IsAccessTokenValid && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogError("[StaticMapMinimap] Map failed to initialize within timeout");
            yield break;
        }
        
        // Additional wait to ensure map has processed initial position
        yield return new WaitForSeconds(1f);
        
        if (debugMode)
        {
            Debug.Log("[StaticMapMinimap] Map initialized, loading minimap");
        }
        
        // Initial map load
        LoadMapForCurrentPosition();
    }
    
    void Update()
    {
        if (mainMap == null || playerTransform == null || minimapImage == null)
            return;
        
        // Update player marker position
        UpdatePlayerMarker();
        
        // Check if we need to regenerate the map
        if (ShouldUpdateMap())
        {
            LoadMapForCurrentPosition();
        }
    }
    
    private void UpdatePlayerMarker()
    {
        if (playerMarker == null || minimapImage == null)
            return;
        
        // Player marker is ALWAYS centered on the minimap
        // The map image itself moves/updates to keep player at center
        playerMarker.anchoredPosition = Vector2.zero;
        
        // Rotate marker to face player direction
        float playerRotation = playerTransform.eulerAngles.y;
        playerMarker.rotation = Quaternion.Euler(0, 0, -playerRotation);
    }
    
    private bool ShouldUpdateMap()
    {
        if (isLoadingMap)
            return false;
        
        // Check cooldown
        if (Time.time - lastUpdateTime < updateCooldown)
            return false;
        
        // Check distance moved
        Vector2d currentLatLon = mainMap.WorldToGeoPosition(playerTransform.position);
        double distance = CalculateDistance(currentLatLon, lastMapCenter);
        
        return distance > updateDistanceThreshold;
    }
    
    private void LoadMapForCurrentPosition()
    {
        if (isLoadingMap)
            return;
        
        Vector2d centerLatLon = mainMap.WorldToGeoPosition(playerTransform.position);
        lastMapCenter = centerLatLon;
        lastUpdateTime = Time.time;
        
        StartCoroutine(FetchStaticMap(centerLatLon));
    }
    
    private IEnumerator FetchStaticMap(Vector2d center)
    {
        isLoadingMap = true;
        
        // Validate coordinates
        if (double.IsNaN(center.x) || double.IsNaN(center.y) || 
            center.x < -90 || center.x > 90 || center.y < -180 || center.y > 180)
        {
            Debug.LogError($"[StaticMapMinimap] Invalid coordinates: {center.x}, {center.y}");
            isLoadingMap = false;
            yield break;
        }
        
        // Ensure dimensions are valid (must be between 1-1280)
        int validWidth = Mathf.Clamp(mapWidth, 1, 1280);
        int validHeight = Mathf.Clamp(mapHeight, 1, 1280);
        int validZoom = Mathf.Clamp(zoomLevel, 0, 22);
        
        // Build Mapbox Static Images API URL
        // Format: https://api.mapbox.com/styles/v1/{username}/{style_id}/static/{lon},{lat},{zoom}/{width}x{height}?access_token={token}
        string url = $"https://api.mapbox.com/styles/v1/mapbox/{mapStyle}/static/" +
                     $"{center.y:F6},{center.x:F6},{validZoom}/{validWidth}x{validHeight}" +
                     $"?access_token={mapboxAccessToken}";
        
        if (debugMode)
        {
            Debug.Log($"[StaticMapMinimap] Fetching map: Lat={center.x:F6}, Lon={center.y:F6}, Zoom={validZoom}");
            Debug.Log($"[StaticMapMinimap] URL: {url}");
        }
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                currentMapTexture = DownloadHandlerTexture.GetContent(request);
                minimapImage.texture = currentMapTexture;
                
                if (debugMode)
                {
                    Debug.Log("[StaticMapMinimap] Map loaded successfully");
                }
            }
            else
            {
                Debug.LogError($"[StaticMapMinimap] Failed to load map: {request.error}");
            }
        }
        
        isLoadingMap = false;
    }
    
    private double CalculateDistance(Vector2d point1, Vector2d point2)
    {
        // Haversine formula for distance between two lat/lon points
        double lat1 = point1.x * Mathf.Deg2Rad;
        double lat2 = point2.x * Mathf.Deg2Rad;
        double dLat = (point2.x - point1.x) * Mathf.Deg2Rad;
        double dLon = (point2.y - point1.y) * Mathf.Deg2Rad;
        
        double a = Mathf.Sin((float)dLat / 2) * Mathf.Sin((float)dLat / 2) +
                   Mathf.Cos((float)lat1) * Mathf.Cos((float)lat2) *
                   Mathf.Sin((float)dLon / 2) * Mathf.Sin((float)dLon / 2);
        
        double c = 2 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1 - a)));
        double distance = 6371000 * c; // Earth radius in meters
        
        return distance;
    }
}
