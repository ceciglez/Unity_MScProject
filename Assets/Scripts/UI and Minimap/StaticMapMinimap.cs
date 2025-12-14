using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using Mapbox.Utils;

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
    
    [Tooltip("Map width in pixels (larger = more buffer before reload, higher quality)")]
    [Range(512, 1280)]
    public int mapWidth = 1024;
    
    [Tooltip("Map height in pixels")]
    [Range(512, 1280)]
    public int mapHeight = 1024;
    
    [Tooltip("Map zoom level (higher = more zoomed in)")]
    [Range(10, 30)]
    public int zoomLevel = 16;
    
    [Tooltip("Radius around player to show in meters (for calculating scale)")]
    [Range(100f, 1000f)]
    public float mapRadiusMeters = 300f;
    
    [Header("Scale Calibration")]
    [Tooltip("Manual scale adjustment multiplier (1.0 = auto-calculated, adjust if offset persists)")]
    [Range(0.5f, 2.0f)]
    public float scaleMultiplier = 1.0f;
    
    [Header("Update Settings")]
    [Tooltip("Regenerate map when player is this far from the map center (meters) - higher = fewer reloads")]
    [Range(50f, 500f)]
    public float updateDistanceThreshold = 200f;
    
    [Tooltip("Minimum time between map updates (seconds)")]
    [Range(0.5f, 10f)]
    public float updateCooldown = 2f;
    
    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool debugMode = false;
    
    [Tooltip("Force reload the minimap (useful when adjusting scale multiplier)")]
    public bool forceReload = false;
    
    private Texture2D currentMapTexture;
    private Vector2d lastMapCenter;
    private Vector2d mapCenterCoords; // Center of the current loaded map
    private float metersPerPixel; // Scale factor for panning
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
        
        // Check if force reload button was pressed
        if (forceReload)
        {
            forceReload = false; // Reset the button
            if (debugMode)
            {
                Debug.Log("[StaticMapMinimap] Force reload triggered");
            }
            LoadMapForCurrentPosition();
            return; // Skip normal update this frame
        }
        
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
        if (playerMarker == null || minimapImage == null || playerTransform == null)
            return;
        
        // Don't update until first map is loaded (metersPerPixel will be 0)
        if (metersPerPixel == 0 || mapCenterCoords.x == 0 && mapCenterCoords.y == 0)
            return;
        
        // Get current player position in lat/lon
        Vector2d playerCoords = mainMap.WorldToGeoPosition(playerTransform.position);
        
        // Calculate offset from map center in degrees
        double latDiff = playerCoords.x - mapCenterCoords.x;
        double lonDiff = playerCoords.y - mapCenterCoords.y;
        
        // Convert to meters using more accurate formulas
        // Latitude: 1 degree = 111,320 meters (constant)
        double latMeters = latDiff * 111320.0;
        
        // Longitude: varies by latitude - 1 degree = 111,320 * cos(latitude) meters
        // Use the map center latitude for consistent calculation
        double lonMeters = lonDiff * 111320.0 * Mathf.Cos((float)mapCenterCoords.x * Mathf.Deg2Rad);
        
        // Convert meters to pixels using calculated scale
        float xPixels = -(float)(lonMeters / metersPerPixel); // Negative: moving east should shift map west
        float yPixels = -(float)(latMeters / metersPerPixel); // Negative: Unity Y is up, map Y is down
        
        if (debugMode && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            Debug.Log($"[Minimap] Player: {playerCoords.x:F6}, {playerCoords.y:F6} | Center: {mapCenterCoords.x:F6}, {mapCenterCoords.y:F6}");
            Debug.Log($"[Minimap] Diff: lat={latDiff:F7}° lon={lonDiff:F7}° | Meters: {latMeters:F2}m lat, {lonMeters:F2}m lon");
            Debug.Log($"[Minimap] MetersPerPixel: {metersPerPixel:F3} | Pixels: X={xPixels:F1}, Y={yPixels:F1}");
        }
        
        // Pan the minimap image (move the RawImage's RectTransform)
        RectTransform mapRect = minimapImage.GetComponent<RectTransform>();
        if (mapRect != null)
        {
            mapRect.anchoredPosition = new Vector2(xPixels, yPixels);
        }
        
        // Keep player marker centered in the viewport
        playerMarker.anchoredPosition = Vector2.zero;
        
        // Rotate marker to show player's facing direction
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
        
        // Check distance from MAP CENTER (not last update position)
        // This allows the map to pan smoothly until player gets too far from center
        Vector2d currentLatLon = mainMap.WorldToGeoPosition(playerTransform.position);
        double distance = CalculateDistance(currentLatLon, mapCenterCoords);
        
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
        string url = $"https://api.mapbox.com/styles/v1/mapbox/{mapStyle}/static/" +
                     $"{center.y:F6},{center.x:F6},{validZoom}/{validWidth}x{validHeight}" +
                     $"?access_token={mapboxAccessToken}";

        if (debugMode)
        {
            Debug.Log($"[StaticMapMinimap] Fetching map: Lat={center.x:F6}, Lon={center.y:F6}, Zoom={validZoom}");
            Debug.Log($"[StaticMapMinimap] URL: {url}");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[StaticMapMinimap] Using WebGL JavaScript bridge for texture fetch");

        // Use JavaScript bridge in WebGL builds
        bool requestComplete = false;
        Texture2D fetchedTexture = null;

        WebGLNetworkBridge.Instance.FetchTexture(
            url,
            (base64Data) => {
                // Success callback
                Debug.Log($"[StaticMapMinimap] Texture fetch successful, converting from base64");
                fetchedTexture = WebGLNetworkBridge.Base64ToTexture(base64Data);
                requestComplete = true;
            },
            (error) => {
                // Error callback
                Debug.LogError($"[StaticMapMinimap] Texture fetch failed: {error}");
                Debug.LogError("[StaticMapMinimap] WEBGL TROUBLESHOOTING:");
                Debug.LogError("  1. Check browser console (F12) for CORS or network errors");
                Debug.LogError("  2. Ensure you're using a local server (not file://)");
                Debug.LogError("  3. Verify Mapbox access token is valid");
                Debug.LogError("  4. Check if ad blocker is blocking Mapbox");
                Debug.LogError("  5. Try in incognito/private browsing mode");
                requestComplete = true;
            }
        );

        // Wait for JavaScript callback
        while (!requestComplete)
        {
            yield return null;
        }

        if (fetchedTexture != null)
        {
            currentMapTexture = fetchedTexture;
            minimapImage.texture = currentMapTexture;

            // Store the center coordinates for this map
            mapCenterCoords = center;

            // Reset the map image position to center
            RectTransform mapRect = minimapImage.GetComponent<RectTransform>();
            if (mapRect != null)
            {
                mapRect.anchoredPosition = Vector2.zero;
            }

            // Calculate meters per pixel
            double earthCircumference = 40075017.0;
            double latitudeRadians = center.x * Mathf.Deg2Rad;
            float baseMetersPerPixel = (float)(earthCircumference * Mathf.Cos((float)latitudeRadians) / (256.0 * Mathf.Pow(2, validZoom)));
            metersPerPixel = baseMetersPerPixel * scaleMultiplier;

            if (debugMode)
            {
                Debug.Log($"[StaticMapMinimap] Map loaded successfully via WebGL bridge!");
                Debug.Log($"[StaticMapMinimap] Center: {center.x:F6}, {center.y:F6}");
                Debug.Log($"[StaticMapMinimap] Meters/pixel: {metersPerPixel:F3}");
            }
        }
#else
        Debug.Log("[StaticMapMinimap] Using UnityWebRequest (Editor mode)");

        // Use UnityWebRequest in Editor
        using (UnityWebRequest request = WebGLCorsHelper.CreateCorsTextureRequest(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                currentMapTexture = DownloadHandlerTexture.GetContent(request);
                minimapImage.texture = currentMapTexture;

                mapCenterCoords = center;

                RectTransform mapRect = minimapImage.GetComponent<RectTransform>();
                if (mapRect != null)
                {
                    mapRect.anchoredPosition = Vector2.zero;
                }

                double earthCircumference = 40075017.0;
                double latitudeRadians = center.x * Mathf.Deg2Rad;
                float baseMetersPerPixel = (float)(earthCircumference * Mathf.Cos((float)latitudeRadians) / (256.0 * Mathf.Pow(2, validZoom)));
                metersPerPixel = baseMetersPerPixel * scaleMultiplier;

                if (debugMode)
                {
                    Debug.Log($"[StaticMapMinimap] Map loaded successfully");
                }
            }
            else
            {
                Debug.LogError($"[StaticMapMinimap] Failed to load map: {request.error}");
                WebGLCorsHelper.LogRequestError(request, "Minimap Load");
            }
        }
#endif

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
