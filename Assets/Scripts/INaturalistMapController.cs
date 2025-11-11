using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;

/// <summary>
/// Controller for displaying iNaturalist observations on a Mapbox map
/// </summary>
public class INaturalistMapController : MonoBehaviour
{
    [Header("Map References")]
    [SerializeField] private AbstractMap map;
    
    [Header("Observation Prefab")]
    [SerializeField] private GameObject observationPrefab;
    [SerializeField] private Transform observationContainer;
    
    [Header("API Settings")]
    [SerializeField] private int maxObservations = 100;
    [SerializeField] private float updateDelay = 2f;
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float reloadDistanceThreshold = 500f; // Reload when player moves 500m
    
    [Header("Visual Settings")]
    [SerializeField] private float prefabScale = 1f;
    [SerializeField] private float recentObservationPulseDays = 7f;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showDebugOverlay = true;
    [SerializeField] private int debugOverlayFontSize = 11;
    
    // Private variables
    private List<ObservationData> observations = new List<ObservationData>();
    private List<GameObject> spawnedPrefabs = new List<GameObject>();
    private bool isLoading = false;
    private Vector2d lastMapCenter;
    private float lastMapZoom;
    private Vector3 lastPlayerPosition;
    private Transform playerTransform;
    private float timeSinceLastUpdate = 0f;
    private float minUpdateInterval = 1f; // Don't update more than once per second
    private const string INATURALIST_API_URL = "https://api.inaturalist.org/v1/observations";
    private GameObject debugOverlay;
    
    void Start()
    {
        if (map == null)
        {
            Debug.LogError("Map reference is not set! Please assign the AbstractMap component.");
            return;
        }
        
        if (observationPrefab == null)
        {
            Debug.LogError("Observation prefab is not set! Please assign a prefab.");
            return;
        }
        
        if (observationContainer == null)
        {
            GameObject container = new GameObject("ObservationContainer");
            container.transform.SetParent(transform);
            observationContainer = container.transform;
        }
        
        // Find player
        var kccController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
        if (kccController != null)
        {
            playerTransform = kccController.transform;
            lastPlayerPosition = playerTransform.position;
            Debug.Log($"INaturalistMapController: Found player at {playerTransform.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("INaturalistMapController: No player found - auto-reload based on movement will not work");
        }
        
        // Create debug overlay if enabled
        if (showDebugOverlay)
        {
            CreateDebugOverlay();
        }
        
        // Store initial map state
        lastMapCenter = map.CenterLatitudeLongitude;
        lastMapZoom = map.Zoom;
        
        // Initial load with delay to ensure map is ready
        StartCoroutine(InitialLoad());
    }
    
    private void CreateDebugOverlay()
    {
        debugOverlay = new GameObject("DebugCoordinateOverlay");
        DebugCoordinateOverlay overlay = debugOverlay.AddComponent<DebugCoordinateOverlay>();
        overlay.SetFontSize(debugOverlayFontSize);
        // The overlay script will auto-find the map and player
        Debug.Log("Debug coordinate overlay created");
    }
    
    void OnDestroy()
    {
        if (debugOverlay != null)
        {
            Destroy(debugOverlay);
        }
    }
    
    void Update()
    {
        if (!autoUpdate || map == null) return;
        
        timeSinceLastUpdate += Time.deltaTime;
        
        // Check if enough time has passed
        if (timeSinceLastUpdate < minUpdateInterval) return;
        
        bool shouldReload = false;
        
        // Check if map center has moved significantly
        Vector2d currentCenter = map.CenterLatitudeLongitude;
        float currentZoom = map.Zoom;
        
        float centerDiff = (float)Vector2d.Distance(lastMapCenter, currentCenter);
        float zoomDiff = Mathf.Abs(lastMapZoom - currentZoom);
        
        if (centerDiff > 0.01f || zoomDiff > 0.5f)
        {
            shouldReload = true;
            lastMapCenter = currentCenter;
            lastMapZoom = currentZoom;
        }
        
        // Check if player has moved significantly (in world space)
        if (playerTransform != null)
        {
            float playerMovement = Vector3.Distance(lastPlayerPosition, playerTransform.position);
            
            if (playerMovement > reloadDistanceThreshold)
            {
                shouldReload = true;
                lastPlayerPosition = playerTransform.position;
                
                if (showDebugInfo)
                {
                    Vector2d playerLatLng = map.WorldToGeoPosition(playerTransform.position);
                    Debug.Log($"Player moved {playerMovement:F0}m - reloading observations at {playerLatLng.x:F6}, {playerLatLng.y:F6}");
                }
            }
        }
        
        // Reload if needed
        if (shouldReload)
        {
            timeSinceLastUpdate = 0f;
            StartCoroutine(LoadiNaturalistData());
        }
    }
    
    private IEnumerator InitialLoad()
    {
        yield return new WaitForSeconds(updateDelay);
        yield return StartCoroutine(LoadiNaturalistData());
    }
    
    /// <summary>
    /// Loads iNaturalist observation data based on current map bounds
    /// </summary>
    public IEnumerator LoadiNaturalistData()
    {
        if (isLoading)
        {
            if (showDebugInfo) Debug.Log("Already loading data, skipping...");
            yield break;
        }
        
        isLoading = true;
        
        // Use player position if available, otherwise use map center
        Vector2d queryCenter;
        if (playerTransform != null)
        {
            queryCenter = map.WorldToGeoPosition(playerTransform.position);
        }
        else
        {
            queryCenter = map.CenterLatitudeLongitude;
        }
        
        float zoom = map.Zoom;
        
        // Calculate search radius based on zoom level
        // Higher zoom = closer view = smaller search radius
        float searchRadius = 2.0f / Mathf.Pow(2, zoom - 10); // In degrees
        
        float swlat = (float)(queryCenter.x - searchRadius);
        float swlng = (float)(queryCenter.y - searchRadius);
        float nelat = (float)(queryCenter.x + searchRadius);
        float nelng = (float)(queryCenter.y + searchRadius);
        
        // Build API URL
        string url = $"{INATURALIST_API_URL}?" +
                     $"swlng={swlng}&swlat={swlat}&nelng={nelng}&nelat={nelat}" +
                     $"&per_page={maxObservations}&order=desc&order_by=created_at" +
                     $"&photos=true&captive=false&quality_grade=research";
        
        if (showDebugInfo) 
        {
            Debug.Log($"Loading observations near player: Lat {queryCenter.x:F6}, Lng {queryCenter.y:F6}");
            Debug.Log($"Search bounds: [{swlat:F6}, {swlng:F6}] to [{nelat:F6}, {nelng:F6}]");
            Debug.Log($"API URL: {url}");
        }
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    INaturalistResponse response = JsonUtility.FromJson<INaturalistResponse>(request.downloadHandler.text);
                    ProcessObservations(response);
                    SpawnObservationPrefabs();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing iNaturalist data: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Error fetching iNaturalist data: {request.error}");
            }
        }
        
        isLoading = false;
    }
    
    private void ProcessObservations(INaturalistResponse response)
    {
        observations.Clear();
        
        if (response.results == null || response.results.Length == 0)
        {
            if (showDebugInfo) Debug.Log("No observations found in response");
            return;
        }
        
        foreach (var obs in response.results)
        {
            if (obs.location != null && !string.IsNullOrEmpty(obs.location) &&
                obs.photos != null && obs.photos.Length > 0 &&
                obs.taxon != null)
            {
                observations.Add(obs);
            }
        }
        
        if (showDebugInfo) Debug.Log($"Loaded {observations.Count} valid observations");
    }
    
    private void SpawnObservationPrefabs()
    {
        // Clear existing prefabs
        foreach (var prefab in spawnedPrefabs)
        {
            if (prefab != null)
                Destroy(prefab);
        }
        spawnedPrefabs.Clear();
        
        // Spawn new prefabs
        foreach (var obs in observations)
        {
            Vector2d latLng = ParseLocation(obs.location);
            
            if (latLng != Vector2d.zero)
            {
                // Convert lat/lng to Unity world position
                Vector3 worldPosition = map.GeoToWorldPosition(latLng, true);
                
                // Instantiate prefab - parent to map's root transform
                GameObject prefabInstance = Instantiate(observationPrefab, worldPosition, Quaternion.identity, map.transform);
                prefabInstance.transform.localScale = Vector3.one * prefabScale;
                
                // Add or update ObservationDisplay component
                ObservationDisplay display = prefabInstance.GetComponent<ObservationDisplay>();
                if (display == null)
                {
                    display = prefabInstance.AddComponent<ObservationDisplay>();
                }
                display.Initialize(obs);
                
                // Add a component to track and update position
                ObservationPositionTracker tracker = prefabInstance.AddComponent<ObservationPositionTracker>();
                tracker.Initialize(map, latLng);
                
                // Add trigger interaction for collision detection
                ObservationTriggerInteraction trigger = prefabInstance.GetComponent<ObservationTriggerInteraction>();
                if (trigger == null)
                {
                    trigger = prefabInstance.AddComponent<ObservationTriggerInteraction>();
                }
                
                spawnedPrefabs.Add(prefabInstance);
            }
        }
        
        if (showDebugInfo) Debug.Log($"Spawned {spawnedPrefabs.Count} observation prefabs");
    }
    
    private Vector2d ParseLocation(string location)
    {
        if (string.IsNullOrEmpty(location)) return Vector2d.zero;
        
        string[] parts = location.Split(',');
        if (parts.Length != 2) return Vector2d.zero;
        
        if (float.TryParse(parts[0], out float lat) && float.TryParse(parts[1], out float lng))
        {
            return new Vector2d(lat, lng);
        }
        
        return Vector2d.zero;
    }
    
    private bool IsRecentObservation(ObservationData obs)
    {
        if (string.IsNullOrEmpty(obs.created_at)) return false;
        
        try
        {
            DateTime observationDate = DateTime.Parse(obs.created_at);
            TimeSpan difference = DateTime.Now - observationDate;
            return difference.TotalDays <= recentObservationPulseDays;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Manually trigger a data reload
    /// </summary>
    public void ReloadData()
    {
        StartCoroutine(LoadiNaturalistData());
    }
    
    /// <summary>
    /// Clear all observation prefabs
    /// </summary>
    public void ClearObservations()
    {
        foreach (var prefab in spawnedPrefabs)
        {
            if (prefab != null)
                Destroy(prefab);
        }
        spawnedPrefabs.Clear();
        observations.Clear();
    }
}

// Data structures for JSON parsing
[Serializable]
public class INaturalistResponse
{
    public int total_results;
    public ObservationData[] results;
}

[Serializable]
public class ObservationData
{
    public int id;
    public string location;
    public string observed_on;
    public string created_at;
    public PhotoData[] photos;
    public TaxonData taxon;
    public UserData user;
}

[Serializable]
public class PhotoData
{
    public int id;
    public string url;
}

[Serializable]
public class TaxonData
{
    public int id;
    public string name;
    public string preferred_common_name;
}

[Serializable]
public class UserData
{
    public int id;
    public string login;
}