using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using StylizedGrass; // For grass masking

/// <summary>
/// iNaturalist API quality grades
/// </summary>
public enum QualityGrade
{
    Research,   // "research" - verified by community
    NeedsId,    // "needs_id" - has photo but needs identification  
    Casual      // "casual" - doesn't meet research criteria
}

/// <summary>
/// iNaturalist API observation ordering options
/// </summary>
public enum ObservationOrder
{
    CreatedAt,      // "created_at" - when observation was created
    ObservedOn,     // "observed_on" - when organism was observed
    Species,        // "species_guess" - alphabetical by species name
    Votes          // "votes" - by community votes
}

/// <summary>
/// Sort direction for observations
/// </summary>
public enum SortDirection
{
    Desc,   // "desc" - descending (newest first)
    Asc     // "asc" - ascending (oldest first)
}

// Controller for displaying iNaturalist observations on a Mapbox map

public class INaturalistMapController : MonoBehaviour
{
    [Header("Map References")]
    [SerializeField] private AbstractMap map;
    
    [Header("Observation Prefab")]
    [SerializeField] private GameObject observationPrefab;
    [SerializeField] private Transform observationContainer;
    
    [Header("API Settings")]
    [SerializeField] private int maxObservations = 500;
    [SerializeField] private float updateDelay = 2f;
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float reloadDistanceThreshold = 500f; // Reload when player moves 500m
    
    [Header("Query Filters")]
    [Tooltip("Search radius in kilometers (overrides zoom-based calculation if > 0)")]
    [SerializeField] private float fixedSearchRadiusKm = 5f;
    [Tooltip("Require observations to have photos")]
    [SerializeField] private bool requirePhotos = false; // Changed to false
    [Tooltip("Include captive/cultivated observations (zoo, garden plants, etc.)")]
    [SerializeField] private bool includeCaptive = true; // Changed to true
    [Tooltip("Quality grades to include")]
    [SerializeField] private QualityGrade[] qualityGrades = { QualityGrade.Research, QualityGrade.NeedsId, QualityGrade.Casual }; // Added Casual
    [Tooltip("Include observations without photos")]
    [SerializeField] private bool includeObservationsWithoutPhotos = false;
    [Tooltip("Order observations by")]
    [SerializeField] private ObservationOrder orderBy = ObservationOrder.CreatedAt;
    [Tooltip("Sort direction")]
    [SerializeField] private SortDirection sortDirection = SortDirection.Desc;
    [Tooltip("Sort by distance to player after receiving API response")]
    [SerializeField] private bool sortByDistanceToPlayer = true;
    
    [Header("Visual Settings")]
    [SerializeField] private float prefabScale = 1f;
    [SerializeField] private float prefabYOffset = 2f; // Height above ground
    [SerializeField] private float recentObservationPulseDays = 7f;
    [Header("Layer Settings")]
    [Tooltip("Layer for observation prefabs (should not interact with grass)")]
    [SerializeField] private string observationLayer = "Observations";
    [Tooltip("Layer for grass exclusion zones")]
    [SerializeField] private string grassExclusionLayer = "GrassExclusion";
    [SerializeField] private float grassExclusionRadius = 5f; // Clear grass around observations
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
        
        // Calculate search radius 
        float searchRadius;
        if (fixedSearchRadiusKm > 0)
        {
            // Use fixed radius in kilometers, convert to degrees
            searchRadius = fixedSearchRadiusKm / 111f; // Approximate: 1 degree ≈ 111 km
        }
        else
        {
            // Calculate search radius based on zoom level
            // Higher zoom = closer view = smaller search radius
            searchRadius = 2.0f / Mathf.Pow(2, zoom - 10); // In degrees
        }
        
        float swlat = (float)(queryCenter.x - searchRadius);
        float swlng = (float)(queryCenter.y - searchRadius);
        float nelat = (float)(queryCenter.x + searchRadius);
        float nelng = (float)(queryCenter.y + searchRadius);
        
        // Build API URL with configurable parameters
        // Don't artificially limit - let the API return what it can (default is 30, max is 200 per page)
        // For distance sorting, we want more data to choose from
        int requestLimit = sortByDistanceToPlayer ? 200 : maxObservations; // Always use 200 when sorting by distance
        string url = BuildApiUrl(swlng, swlat, nelng, nelat, requestLimit);
        
        if (showDebugInfo) 
        {
            Debug.Log($"[iNaturalist] === API REQUEST DEBUG ===");
            Debug.Log($"[iNaturalist] Player/Query center: Lat {queryCenter.x:F6}, Lng {queryCenter.y:F6}");
            Debug.Log($"[iNaturalist] Search radius: {searchRadius:F6} degrees ({searchRadius * 111:F1} km)");
            Debug.Log($"[iNaturalist] Search bounds: [{swlat:F6}, {swlng:F6}] to [{nelat:F6}, {nelng:F6}]");
            Debug.Log($"[iNaturalist] Requesting {requestLimit} observations (sortByDistance: {sortByDistanceToPlayer})");
            Debug.Log($"[iNaturalist] === FULL API URL ===");
            Debug.Log($"[iNaturalist] {url}");
            Debug.Log($"[iNaturalist] === MAKING REQUEST ===");
        }
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[iNaturalist] === API RESPONSE DEBUG ===");
                        Debug.Log($"[iNaturalist] Response code: {request.responseCode}");
                        Debug.Log($"[iNaturalist] Response size: {request.downloadHandler.text.Length} characters");
                        
                        // Log first part of response to see structure
                        string responseStart = request.downloadHandler.text.Length > 1000 ? 
                            request.downloadHandler.text.Substring(0, 1000) + "..." : 
                            request.downloadHandler.text;
                        Debug.Log($"[iNaturalist] Response start: {responseStart}");
                        
                        // Try to extract total_results before parsing
                        if (request.downloadHandler.text.Contains("total_results"))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(
                                request.downloadHandler.text, @"""total_results"":([0-9]+)");
                            if (match.Success)
                            {
                                Debug.Log($"[iNaturalist] Raw total_results from API: {match.Groups[1].Value}");
                            }
                        }
                    }
                    
                    INaturalistResponse response = JsonUtility.FromJson<INaturalistResponse>(request.downloadHandler.text);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[iNaturalist] API returned {response.results?.Length ?? 0} total observations, total_results: {response.total_results}");
                        Debug.Log($"[iNaturalist] Current filters: requirePhotos={requirePhotos}, includeCaptive={includeCaptive}, qualityGrades=[{string.Join(",", qualityGrades)}]");
                    }
                    
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
                Debug.LogError($"[iNaturalist] === API REQUEST FAILED ===");
                Debug.LogError($"[iNaturalist] Error: {request.error}");
                Debug.LogError($"[iNaturalist] Response Code: {request.responseCode}");
                Debug.LogError($"[iNaturalist] URL: {url}");
                if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                {
                    Debug.LogError($"[iNaturalist] Response body: {request.downloadHandler.text}");
                }
            }
        }
        
        isLoading = false;
    }

    /// <summary>
    /// Debug method - Test a simple API call to see raw results
    /// Call this from Unity Inspector or another script to test
    /// </summary>
    [ContextMenu("Debug: Test Simple API Call")]
    public void DebugTestSimpleAPICall()
    {
        StartCoroutine(TestSimpleAPICall());
    }
    
    /// <summary>
    /// Test both unfiltered and filtered API calls
    /// </summary>
    [ContextMenu("Debug: Compare Filtered vs Unfiltered")]
    public void DebugCompareAPICalls()
    {
        StartCoroutine(CompareAPICalls());
    }
    
    private IEnumerator CompareAPICalls()
    {
        // Test 1: Unfiltered call
        string unfilteredUrl = "https://api.inaturalist.org/v1/observations?" +
                              "swlat=51.46&swlng=-0.11&nelat=51.48&nelng=-0.08&per_page=200";
        
        Debug.Log($"[API COMPARE] === TEST 1: UNFILTERED ===");
        Debug.Log($"[API COMPARE] URL: {unfilteredUrl}");
        
        using (UnityWebRequest request1 = UnityWebRequest.Get(unfilteredUrl))
        {
            yield return request1.SendWebRequest();
            
            if (request1.result == UnityWebRequest.Result.Success)
            {
                INaturalistResponse response1 = JsonUtility.FromJson<INaturalistResponse>(request1.downloadHandler.text);
                Debug.Log($"[API COMPARE] UNFILTERED: {response1.results?.Length ?? 0} returned, {response1.total_results} total available");
            }
        }
        
        // Test 2: Your current filtered call
        if (playerTransform != null)
        {
            Vector2d center = map.WorldToGeoPosition(playerTransform.position);
            float radius = fixedSearchRadiusKm / 111f;
            float swlat = (float)(center.x - radius);
            float swlng = (float)(center.y - radius);
            float nelat = (float)(center.x + radius);
            float nelng = (float)(center.y + radius);
            
            string filteredUrl = BuildApiUrl(swlng, swlat, nelng, nelat, 200);
            
            Debug.Log($"[API COMPARE] === TEST 2: YOUR FILTERS ===");
            Debug.Log($"[API COMPARE] URL: {filteredUrl}");
            
            using (UnityWebRequest request2 = UnityWebRequest.Get(filteredUrl))
            {
                yield return request2.SendWebRequest();
                
                if (request2.result == UnityWebRequest.Result.Success)
                {
                    INaturalistResponse response2 = JsonUtility.FromJson<INaturalistResponse>(request2.downloadHandler.text);
                    Debug.Log($"[API COMPARE] FILTERED: {response2.results?.Length ?? 0} returned, {response2.total_results} total available");
                    
                    Debug.Log($"[API COMPARE] === COMPARISON ===");
                    Debug.Log($"[API COMPARE] Filter impact: {response2.total_results} vs unfiltered results");
                    Debug.Log($"[API COMPARE] Current settings: requirePhotos={requirePhotos}, includeCaptive={includeCaptive}");
                }
            }
        }
    }
    
    private IEnumerator TestSimpleAPICall()
    {
        // Simple test call around Camberwell Green area
        // 51.4705° N, 0.0938° W (rough Camberwell coordinates)
        string testUrl = "https://api.inaturalist.org/v1/observations?" +
                        "swlat=51.46&swlng=-0.11&nelat=51.48&nelng=-0.08"; // No per_page limit to see max
        
        Debug.Log($"[API TEST] Testing simple call (NO LIMITS): {testUrl}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    INaturalistResponse response = JsonUtility.FromJson<INaturalistResponse>(request.downloadHandler.text);
                    Debug.Log($"[API TEST] SUCCESS! Got {response.results?.Length ?? 0} observations out of {response.total_results} total");
                    
                    // Show first few results
                    if (response.results != null && response.results.Length > 0)
                    {
                        for (int i = 0; i < Mathf.Min(5, response.results.Length); i++)
                        {
                            var obs = response.results[i];
                            Debug.Log($"[API TEST] #{i+1}: {obs.taxon?.preferred_common_name ?? "Unknown"} " +
                                     $"at {obs.location}, photos: {obs.photos?.Length ?? 0}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[API TEST] Parse error: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[API TEST] FAILED: {request.error}");
            }
        }
    }
    
    private void ProcessObservations(INaturalistResponse response)
    {
        observations.Clear();
        
        if (response.results == null || response.results.Length == 0)
        {
            if (showDebugInfo) Debug.Log("No observations found in response");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[iNaturalist] === DEBUGGING CAMBERWELL GREEN OBSERVATIONS ===");
            Debug.Log($"[iNaturalist] Processing {response.results.Length} observations from API (total_results: {response.total_results})");
            Debug.Log($"[iNaturalist] Current filter settings:");
            Debug.Log($"[iNaturalist] - requirePhotos = {requirePhotos}");
            Debug.Log($"[iNaturalist] - includeCaptive = {includeCaptive}"); 
            Debug.Log($"[iNaturalist] - qualityGrades = [{string.Join(",", qualityGrades)}]");
            Debug.Log($"[iNaturalist] === PROCESSING EACH OBSERVATION ===");
        }
        
        int totalReceived = response.results.Length;
        int noLocation = 0;
        int noPhotos = 0;
        int noTaxon = 0;
        
        foreach (var obs in response.results)
        {
            // Debug EVERY observation to see what we're getting
            if (showDebugInfo)
            {
                Debug.Log($"[iNaturalist] === OBSERVATION {obs.id} ===");
                Debug.Log($"[iNaturalist] - Location: '{obs.location}'");
                Debug.Log($"[iNaturalist] - Photos: {obs.photos?.Length ?? 0}");
                Debug.Log($"[iNaturalist] - Taxon: {(obs.taxon != null ? obs.taxon.preferred_common_name + " [" + obs.taxon.iconic_taxon_name + "]" : "NULL")}");
                Debug.Log($"[iNaturalist] - User: {obs.user?.login}");
                Debug.Log($"[iNaturalist] - Observed: {obs.observed_on}");
            }
            
            // Check location
            if (string.IsNullOrEmpty(obs.location))
            {
                if (showDebugInfo) Debug.Log($"[iNaturalist] ❌ FILTERED: Obs {obs.id} - NO LOCATION");
                noLocation++;
                continue;
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[iNaturalist] ✅ Location OK: '{obs.location}'");
            }
            
            // Check photos (temporarily disabled to see what we get)
            if (requirePhotos && (obs.photos == null || obs.photos.Length == 0))
            {
                if (showDebugInfo) Debug.Log($"[iNaturalist] ❌ FILTERED: Obs {obs.id} - NO PHOTOS (requirePhotos={requirePhotos})");
                noPhotos++;
                continue;
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[iNaturalist] ✅ Photos OK: {obs.photos?.Length ?? 0} photos");
            }
            
            // Check taxon
            if (obs.taxon == null)
            {
                if (showDebugInfo) Debug.Log($"[iNaturalist] ❌ FILTERED: Obs {obs.id} - NO TAXON");
                noTaxon++;
                continue;
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[iNaturalist] ✅ Taxon OK: {obs.taxon.preferred_common_name}");
            }

            if (showDebugInfo) Debug.Log($"[iNaturalist] ✅ KEEPING OBSERVATION {obs.id}: {obs.taxon.preferred_common_name}");
            observations.Add(obs);
        }
        
        int totalFiltered = noLocation + noPhotos + noTaxon;
        
        if (showDebugInfo)
        {
            Debug.Log($"[iNaturalist] === FINAL RESULTS ===");
            Debug.Log($"[iNaturalist] Results: {observations.Count} kept, {totalFiltered} filtered out of {totalReceived} total");
            Debug.Log($"[iNaturalist] Filter breakdown: {noLocation} no location, {noPhotos} no photos, {noTaxon} no taxon");
            
            if (totalFiltered > observations.Count && totalReceived > 5)
            {
                Debug.LogWarning($"[iNaturalist] ⚠️  WARNING: Filtering out {totalFiltered} of {totalReceived} observations!");
                Debug.LogWarning($"[iNaturalist] 💡 QUICK FIX: Try setting 'Require Photos' to FALSE in the Inspector");
                Debug.LogWarning($"[iNaturalist] 💡 Most observations in Camberwell may not have photos attached");
            }
            
            if (observations.Count > 0)
            {
                var firstObs = observations[0];
                Debug.Log($"[iNaturalist] ✅ Sample kept observation: {firstObs.taxon?.preferred_common_name ?? "Unknown"} [{firstObs.taxon?.iconic_taxon_name}] at {firstObs.location}");
            }
            else
            {
                Debug.LogError($"[iNaturalist] ❌ NO OBSERVATIONS KEPT! Raw API returned {totalReceived}, all filtered out.");
                Debug.LogError($"[iNaturalist] 🔧 SOLUTION: Set requirePhotos=false, or check your search area");
            }
        }
        
        // Sort by distance to player if enabled
        if (sortByDistanceToPlayer && observations.Count > 1 && playerTransform != null)
        {
            Vector2d playerLatLng = map.WorldToGeoPosition(playerTransform.position);
            
            if (showDebugInfo)
            {
                Debug.Log($"[iNaturalist] Player lat/lng: {playerLatLng} (from world pos: {playerTransform.position})");
                Debug.Log($"[iNaturalist] Sorting {observations.Count} observations by distance...");
            }
            
            // Calculate distances for all observations
            var observationsWithDistance = new List<(ObservationData obs, double distance)>();
            
            foreach (var obs in observations)
            {
                Vector2d obsLatLng = ParseLocation(obs.location);
                if (obsLatLng != Vector2d.zero)
                {
                    double distance = CalculateDistance(playerLatLng, obsLatLng);
                    observationsWithDistance.Add((obs, distance));
                }
            }
            
            // Sort by distance and take only the closest ones up to maxObservations
            observationsWithDistance.Sort((a, b) => a.distance.CompareTo(b.distance));
            observations = observationsWithDistance.Take(maxObservations).Select(x => x.obs).ToList();
            
            if (showDebugInfo && observationsWithDistance.Count > 0)
            {
                Debug.Log($"[iNaturalist] Sorted and limited to {observations.Count} closest observations");
                var closest = observationsWithDistance[0];
                Debug.Log($"[iNaturalist] Closest: {closest.obs.taxon?.preferred_common_name} at {closest.distance:F0}m");
                
                if (observationsWithDistance.Count > 5)
                {
                    var furthest = observationsWithDistance[observationsWithDistance.Count - 1];
                    Debug.Log($"[iNaturalist] Furthest (before limiting): {furthest.obs.taxon?.preferred_common_name} at {furthest.distance:F0}m");
                }
                
                // Show distance range of selected observations
                if (observations.Count > 1)
                {
                    var lastSelected = observationsWithDistance[observations.Count - 1];
                    Debug.Log($"[iNaturalist] Selected range: {closest.distance:F0}m to {lastSelected.distance:F0}m");
                }
            }
        }
    }
    
    private void SpawnObservationPrefabs()
    {
        if (showDebugInfo) Debug.Log($"[iNaturalist] SpawnObservationPrefabs called with {observations.Count} observations");
        
        // Clear existing prefabs
        foreach (var prefab in spawnedPrefabs)
        {
            if (prefab != null)
                Destroy(prefab);
        }
        spawnedPrefabs.Clear();

        if (observationPrefab == null)
        {
            Debug.LogError("[iNaturalist] observationPrefab is NULL! Cannot spawn observations.");
            return;
        }

        // Spawn new prefabs
        foreach (var obs in observations)
        {
            Vector2d latLng = ParseLocation(obs.location);
            
            if (latLng != Vector2d.zero)
            {
                // Convert lat/lng to Unity world position
                Vector3 worldPosition = map.GeoToWorldPosition(latLng, true);
                float originalY = worldPosition.y;
                
                // Try raycast to find ground (but don't fail if it doesn't work)
                RaycastHit hit;
                Vector3 rayStart = worldPosition + Vector3.up * 500f;
                bool hitGround = Physics.Raycast(rayStart, Vector3.down, out hit, 1000f);
                
                if (hitGround)
                {
                    worldPosition.y = hit.point.y + prefabYOffset;
                    if (showDebugInfo)
                    {
                        Debug.Log($"[iNaturalist] Raycast HIT: Ground Y={hit.point.y:F2}, Final Y={worldPosition.y:F2}");
                    }
                }
                else
                {
                    // Just use original Y + offset
                    worldPosition.y = originalY + prefabYOffset;
                    if (showDebugInfo && Time.frameCount % 10 == 0) // Log only occasionally to avoid spam
                    {
                        Debug.Log($"[iNaturalist] Raycast MISS: Using original Y={originalY:F2} + offset={prefabYOffset}, Final Y={worldPosition.y:F2}");
                    }
                }
                
                // Instantiate prefab
                GameObject prefabInstance = Instantiate(observationPrefab, worldPosition, Quaternion.identity, map.transform);
                if (prefabInstance == null)
                {
                    Debug.LogError($"[iNaturalist] Failed to instantiate prefab for observation {obs.id}!");
                    continue;
                }
                
                prefabInstance.transform.localScale = Vector3.one * prefabScale;
                
                // Set observation to proper layer (should not collide with grass)
                int obsLayer = LayerMask.NameToLayer(observationLayer);
                if (obsLayer != -1)
                {
                    SetLayerRecursively(prefabInstance, obsLayer);
                }
                
                // Create grass exclusion zone around this observation
                CreateGrassExclusionZone(worldPosition, grassExclusionRadius);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[iNaturalist] Created prefab '{prefabInstance.name}' at world pos {worldPosition}, layer {obsLayer}, active={prefabInstance.activeSelf}");
                }
                
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
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[iNaturalist] Failed to parse location for observation {obs.id}: '{obs.location}'");
                }
            }
        }
        
        if (showDebugInfo) 
        {
            Debug.Log($"[iNaturalist] Spawned {spawnedPrefabs.Count} observation prefabs total");
            Debug.Log($"[iNaturalist] Prefabs parent: {(observationContainer != null ? observationContainer.name : map.transform.name)}");
        }
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
    
    /// <summary>
    /// Calculate distance between two lat/lng points in meters
    /// </summary>
    private double CalculateDistance(Vector2d pos1, Vector2d pos2)
    {
        // Haversine formula for distance calculation
        const double earthRadius = 6371000; // Earth radius in meters
        
        double lat1Rad = pos1.x * Mathf.Deg2Rad;
        double lat2Rad = pos2.x * Mathf.Deg2Rad;
        double deltaLatRad = (pos2.x - pos1.x) * Mathf.Deg2Rad;
        double deltaLngRad = (pos2.y - pos1.y) * Mathf.Deg2Rad;
        
        double a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(deltaLngRad / 2) * Math.Sin(deltaLngRad / 2);
        
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadius * c;
    }
    
    /// <summary>
    /// Create an invisible sphere around observation to exclude grass rendering
    /// Uses GrassMaskingSphere from Stylized Grass Shader
    /// </summary>
    private void CreateGrassExclusionZone(Vector3 position, float radius)
    {
        GameObject exclusionZone = new GameObject($"GrassExclusion_{Time.time}");
        exclusionZone.transform.position = position;
        
        // Set to grass exclusion layer
        int exclusionLayerIndex = LayerMask.NameToLayer(grassExclusionLayer);
        if (exclusionLayerIndex != -1)
        {
            exclusionZone.layer = exclusionLayerIndex;
        }
        
        // Method 1: Use GrassMaskingSphere for Stylized Grass Shader compatibility
        var grassMasking = exclusionZone.AddComponent<StylizedGrass.GrassMaskingSphere>();
        if (grassMasking != null)
        {
            grassMasking.radius = radius;
        }
        
        // Method 2: Add sphere collider for physics-based detection
        SphereCollider exclusionCollider = exclusionZone.AddComponent<SphereCollider>();
        exclusionCollider.radius = radius;
        exclusionCollider.isTrigger = true;
        
        // Method 3: Add custom marker component for grass spawner detection
        var exclusionMarker = exclusionZone.AddComponent<GrassExclusionMarker>();
        exclusionMarker.exclusionRadius = radius;
        
        if (showDebugInfo)
        {
            Debug.Log($"[iNaturalist] Created multi-method exclusion zone at {position} with radius {radius} on layer {exclusionLayerIndex}");
        }
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
    
    /// <summary>
    /// Build iNaturalist API URL with all configured filters
    /// </summary>
    private string BuildApiUrl(float swlng, float swlat, float nelng, float nelat, int limit = -1)
    {
        int actualLimit = limit > 0 ? Mathf.Min(limit, 200) : 200; // iNaturalist max is 200 per page
        string url = $"{INATURALIST_API_URL}?" +
                     $"swlng={swlng}&swlat={swlat}&nelng={nelng}&nelat={nelat}" +
                     $"&per_page={actualLimit}";
        
        // Add quality grades
        if (qualityGrades != null && qualityGrades.Length > 0)
        {
            string qualityGradeStr = "";
            for (int i = 0; i < qualityGrades.Length; i++)
            {
                if (i > 0) qualityGradeStr += ",";
                qualityGradeStr += QualityGradeToString(qualityGrades[i]);
            }
            url += $"&quality_grade={qualityGradeStr}";
        }
        
        // Add photo requirement
        if (requirePhotos && !includeObservationsWithoutPhotos)
        {
            url += "&photos=true";
        }
        else if (includeObservationsWithoutPhotos)
        {
            url += "&photos=any";
        }
        
        // Add captive filter - FIXED: was only excluding, now includes too  
        if (includeCaptive)
        {
            url += "&captive=any"; // Include both captive and wild
        }
        else
        {
            url += "&captive=false"; // Only wild observations
        }
        
        // Add ordering
        url += $"&order={SortDirectionToString(sortDirection)}&order_by={OrderByToString(orderBy)}";
        
        return url;
    }
    
    private string QualityGradeToString(QualityGrade grade)
    {
        switch (grade)
        {
            case QualityGrade.Research: return "research";
            case QualityGrade.NeedsId: return "needs_id";
            case QualityGrade.Casual: return "casual";
            default: return "research";
        }
    }
    
    private string OrderByToString(ObservationOrder order)
    {
        switch (order)
        {
            case ObservationOrder.CreatedAt: return "created_at";
            case ObservationOrder.ObservedOn: return "observed_on";
            case ObservationOrder.Species: return "species_guess";
            case ObservationOrder.Votes: return "votes";
            default: return "created_at";
        }
    }
    
    private string SortDirectionToString(SortDirection direction)
    {
        switch (direction)
        {
            case SortDirection.Desc: return "desc";
            case SortDirection.Asc: return "asc";
            default: return "desc";
        }
    }
    
    /// <summary>
    /// Set layer recursively for GameObject and all children
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
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
    public string iconic_taxon_name; // Plantae, Animalia, Fungi, etc.
}

[Serializable]
public class UserData
{
    public int id;
    public string login;
}