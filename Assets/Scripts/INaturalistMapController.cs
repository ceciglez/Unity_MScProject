using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
// using StylizedGrass; // DISABLED - For grass masking

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
    
    [Header("Taxon-Specific Prefabs")]
    [Tooltip("Enable taxon-specific prefab spawning instead of using single observationPrefab")]
    [SerializeField] private bool useTaxonSpecificPrefabs = false;
    [Tooltip("Prefabs for plant observations (Plantae)")]
    [SerializeField] private GameObject[] plantPrefabs = new GameObject[0];
    [Tooltip("Prefabs for animal observations (Animalia)")]
    [SerializeField] private GameObject[] animalPrefabs = new GameObject[0];
    [Tooltip("Prefabs for bird observations (Aves)")]
    [SerializeField] private GameObject[] birdPrefabs = new GameObject[0];
    [Tooltip("Prefabs for fungi observations (Fungi)")]
    [SerializeField] private GameObject[] fungiPrefabs = new GameObject[0];
    [Tooltip("Prefabs for insect observations (Insecta)")]
    [SerializeField] private GameObject[] insectPrefabs = new GameObject[0];
    [Tooltip("Prefabs for other/unknown taxon observations")]
    [SerializeField] private GameObject[] unknownPrefabs = new GameObject[0];
    
    [Header("Observation UI Display")]
    [Tooltip("Enable/disable worldspace UI display for observations")]
    [SerializeField] private bool enableObservationUI = true;
    [Tooltip("Distance at which observation UI becomes visible")]
    [Range(1f, 50f)]
    [SerializeField] private float uiDisplayDistance = 15f;
    [Tooltip("Always show UI (ignore proximity)")]
    [SerializeField] private bool alwaysShowUI = false;
    
    [Header("Taxon Settings")]
    [Tooltip("Scale multiplier for plant prefabs")]
    [Range(0.1f, 5f)]
    [SerializeField] private float plantScale = 1.2f;
    [Tooltip("Scale multiplier for animal prefabs")]
    [Range(0.1f, 5f)]
    [SerializeField] private float animalScale = 0.8f;
    [Tooltip("Scale multiplier for bird prefabs")]
    [Range(0.1f, 5f)]
    [SerializeField] private float birdScale = 0.6f;
    [Tooltip("Scale multiplier for fungi prefabs")]
    [Range(0.1f, 5f)]
    [SerializeField] private float fungiScale = 0.8f;
    [Tooltip("Scale multiplier for insect prefabs")]
    [Range(0.1f, 5f)]
    [SerializeField] private float insectScale = 0.4f;
    [Tooltip("Random scale variation (±percentage)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float scaleVariation = 0.2f;
    
    [Header("Y Offset Override for Taxon Prefabs")]
    [Tooltip("Override Y offset for taxon-specific prefabs (useful if custom prefabs should sit on ground)")]
    [SerializeField] private bool useCustomYOffset = true;
    [Tooltip("Y offset for plant prefabs (0 = sits on ground)")]
    [SerializeField] private float plantYOffset = 0f;
    [Tooltip("Y offset for animal prefabs (0 = sits on ground)")]
    [SerializeField] private float animalYOffset = 0f;
    [Tooltip("Y offset for bird prefabs (might want higher for flying)")]
    [SerializeField] private float birdYOffset = 2f;
    [Tooltip("Y offset for fungi prefabs (0 = sits on ground)")]
    [SerializeField] private float fungiYOffset = 0f;
    [Tooltip("Y offset for insect prefabs (0 = sits on ground)")]
    [SerializeField] private float insectYOffset = 0f;
    [Tooltip("Y offset for unknown/default observation prefab")]
    [SerializeField] private float unknownYOffset = 2f;
    
    [Header("API Settings")]
    [SerializeField] private int maxObservations = 500;
    [SerializeField] private float updateDelay = 2f;
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float reloadDistanceThreshold = 300f; // Reload when player moves 500m
    
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
    

    
    [Header("Ground Detection")]


    [Tooltip("Layer mask for ground detection raycasting")]
    [SerializeField] private LayerMask groundLayerMask = -1;
    [Tooltip("Maximum distance to raycast for ground detection")]
    [Range(10f, 1000f)]
    [SerializeField] private float groundDetectionDistance = 500f;
    [Tooltip("Multiple raycast attempts for better ground detection")]
    [Range(1, 9)]
    [SerializeField] private int raycastAttempts = 5;
    [Tooltip("Radius around position to search for ground (meters)")]
    [Range(1f, 50f)]
    [SerializeField] private float groundSearchRadius = 10f;
    [Tooltip("Enable debug visualization for ground detection")]
    [SerializeField] private bool showGroundDetectionDebug = false;
    [Tooltip("Show debug rays in scene view")]
    [SerializeField] private bool showDebugRays = false;
    [Tooltip("Force all prefabs to specific Y height (for testing)")]
    [SerializeField] private bool forceYHeight = false;
    [Tooltip("Fixed Y height to use when force enabled")]
    [SerializeField] private float forcedYHeight = 0f;
    
    [Header("Layer Settings")]
    [Tooltip("Layer for observation prefabs (should not interact with grass)")]
    [SerializeField] private string observationLayer = "Observations";
    [Tooltip("Layer for grass exclusion zones")]
    [SerializeField] private string grassExclusionLayer = "GrassExclusion";
    [SerializeField] private float grassExclusionRadius = 5f; // Clear grass around observations
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Xeno-Canto Bird Audio")]
    [Tooltip("Enable bird audio from Xeno-Canto API")]
    [SerializeField] private bool enableBirdAudio = true;
    [Tooltip("Your Xeno-Canto API key (get free at xeno-canto.org)")]
    [SerializeField] private string xenoCantoApiKey = "c414800ca86720f8a3c573c8e38c6221d36c6b2f";
    [Tooltip("Maximum audio clips to fetch per bird species")]
    [Range(1, 5)]
    [SerializeField] private int maxAudioClipsPerSpecies = 1;
    [Tooltip("Audio volume for bird sounds")]
    [Range(0f, 1f)]
    [SerializeField] private float birdAudioVolume = 0.7f;
    [Tooltip("Distance at which bird audio starts playing")]
    [Range(1f, 20f)]
    [SerializeField] private float audioTriggerDistance = 8f;
    
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
    private const string XENOCANTO_API_URL = "https://xeno-canto.org/api/2/recordings";
    
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
        

        
        // Store initial map state
        lastMapCenter = map.CenterLatitudeLongitude;
        lastMapZoom = map.Zoom;
        
        // Initial load with delay to ensure map is ready
        StartCoroutine(InitialLoad());
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
                
                // Advanced ground detection
                Vector3 groundPosition = FindGroundPositionAdvanced(worldPosition, obs);
                
                // Get appropriate prefab for this observation's taxon
                GameObject prefabToUse = GetPrefabForObservation(obs);
                if (prefabToUse == null)
                {
                    Debug.LogWarning($"[iNaturalist] No prefab available for observation {obs.id} (taxon: {GetTaxonCategory(obs)})");
                    continue;
                }
                
                // Instantiate prefab
                GameObject prefabInstance = Instantiate(prefabToUse, groundPosition, GetRandomYRotation(), map.transform);
                if (prefabInstance == null)
                {
                    Debug.LogError($"[iNaturalist] Failed to instantiate prefab for observation {obs.id}!");
                    continue;
                }
                
                // Apply taxon-specific scaling
                float finalScale = GetScaleForObservation(obs);
                prefabInstance.transform.localScale = Vector3.one * finalScale;
                
                // Set observation to proper layer (should not collide with grass)
                int obsLayer = LayerMask.NameToLayer(observationLayer);
                if (obsLayer != -1)
                {
                    SetLayerRecursively(prefabInstance, obsLayer);
                }
                
                // Create grass exclusion zone around this observation
                CreateGrassExclusionZone(groundPosition, grassExclusionRadius);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[iNaturalist] Created prefab '{prefabInstance.name}' at ground pos {groundPosition}, scale={finalScale:F2}, layer {obsLayer}, active={prefabInstance.activeSelf}");
                }
                
                // Add or update ObservationDisplay component
                ObservationDisplay display = prefabInstance.GetComponent<ObservationDisplay>();
                if (display == null)
                {
                    display = prefabInstance.AddComponent<ObservationDisplay>();
                }
                display.Initialize(obs);
                // TODO: Uncomment when compilation issue is resolved
                // display.SetUIDisplaySettings(enableObservationUI, uiDisplayDistance, alwaysShowUI);
                
                // Add trigger interaction for collision detection
                ObservationTriggerInteraction trigger = prefabInstance.GetComponent<ObservationTriggerInteraction>();
                if (trigger == null)
                {
                    trigger = prefabInstance.AddComponent<ObservationTriggerInteraction>();
                }
                
                // Add bird audio if this is a bird observation
                string taxonCategory = GetTaxonCategory(obs);
                if (showDebugInfo)
                {
                    Debug.Log($"[iNaturalist] Checking bird audio: enableBirdAudio={enableBirdAudio}, taxonCategory='{taxonCategory}', isAves={taxonCategory.ToLower() == "aves"}");
                }
                
                if (enableBirdAudio && taxonCategory.ToLower() == "aves")
                {
                    if (showDebugInfo) Debug.Log($"[XenoCanto] Starting audio load for bird: {obs.taxon?.preferred_common_name}");
                    StartCoroutine(LoadBirdAudio(obs, prefabInstance));
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
        // DISABLED: Grass shader removed
        // var grassMasking = exclusionZone.AddComponent<StylizedGrass.GrassMaskingSphere>();
        // if (grassMasking != null)
        // {
        //     grassMasking.radius = radius;
        // }
        
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
    
    /// <summary>
    /// Get the appropriate prefab for an observation based on its taxon
    /// </summary>
    private GameObject GetPrefabForObservation(ObservationData obs)
    {
        if (!useTaxonSpecificPrefabs)
        {
            return observationPrefab;
        }
        
        string taxonCategory = GetTaxonCategory(obs);
        GameObject[] prefabArray = null;
        
        switch (taxonCategory.ToLower())
        {
            case "plantae":
                prefabArray = plantPrefabs;
                break;
            case "animalia":
                prefabArray = animalPrefabs;
                break;
            case "aves":
                prefabArray = birdPrefabs;
                break;
            case "fungi":
                prefabArray = fungiPrefabs;
                break;
            case "insecta":
            case "arachnida":
                prefabArray = insectPrefabs;
                break;
            default:
                prefabArray = unknownPrefabs;
                break;
        }
        
        // Return random prefab from the appropriate array, or fallback to default
        if (prefabArray != null && prefabArray.Length > 0)
        {
            return prefabArray[UnityEngine.Random.Range(0, prefabArray.Length)];
        }
        
        // Fallback to default observation prefab
        return observationPrefab;
    }
    
    /// <summary>
    /// Get the taxon category for an observation
    /// </summary>
    private string GetTaxonCategory(ObservationData obs)
    {
        if (obs.taxon != null && !string.IsNullOrEmpty(obs.taxon.iconic_taxon_name))
        {
            return obs.taxon.iconic_taxon_name;
        }
        return "unknown";
    }
    
    /// <summary>
    /// Get the appropriate scale for an observation based on its taxon
    /// </summary>
    private float GetScaleForObservation(ObservationData obs)
    {
        if (!useTaxonSpecificPrefabs)
        {
            return 1.0f; // Default scale
        }
        
        string taxonCategory = GetTaxonCategory(obs);
        float baseScale = 1.0f; // Default scale
        
        switch (taxonCategory.ToLower())
        {
            case "plantae":
                baseScale = plantScale;
                break;
            case "animalia":
                baseScale = animalScale;
                break;
            case "aves":
                baseScale = birdScale;
                break;
            case "fungi":
                baseScale = fungiScale;
                break;
            case "insecta":
            case "arachnida":
                baseScale = insectScale;
                break;
        }
        
        // Apply random variation
        float variation = baseScale * scaleVariation * UnityEngine.Random.Range(-1f, 1f);
        return baseScale + variation;
    }
    
    /// <summary>
    /// Advanced ground detection with multiple raycast attempts and fallback strategies
    /// </summary>
    private Vector3 FindGroundPositionAdvanced(Vector3 worldPosition, ObservationData obs = null)
    {
        float yOffset = GetYOffsetForObservation(obs);
        
        if (forceYHeight)
        {
            Vector3 forcedPos = new Vector3(worldPosition.x, forcedYHeight + yOffset, worldPosition.z);
            if (showGroundDetectionDebug)
                Debug.Log($"[iNaturalist] Using forced Y height: {forcedPos}");
            return forcedPos;
        }
        
        Vector3 bestGroundPosition = worldPosition;
        bool foundGround = false;
        float bestDistance = float.MaxValue;
        RaycastHit bestHit = new RaycastHit();
        
        // Method 1: Primary raycast from above
        Vector3 rayStart = worldPosition + Vector3.up * groundDetectionDistance;
        Vector3 rayEnd = worldPosition - Vector3.up * groundDetectionDistance;
        
        if (showDebugRays)
            Debug.DrawRay(rayStart, Vector3.down * (groundDetectionDistance * 2f), Color.red, 5f);
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundDetectionDistance * 2f, groundLayerMask))
        {
            bestGroundPosition = hit.point + Vector3.up * yOffset;
            foundGround = true;
            bestDistance = Vector3.Distance(worldPosition, hit.point);
            bestHit = hit;
            
            if (showGroundDetectionDebug)
                Debug.Log($"[iNaturalist] Primary raycast hit: Y={hit.point.y:F2}, collider={hit.collider.name}, yOffset={yOffset:F2}, final Y={bestGroundPosition.y:F2}");
        }
        else if (showGroundDetectionDebug)
        {
            Debug.LogWarning($"[iNaturalist] Primary raycast MISSED from {rayStart} to {rayEnd}, layerMask={groundLayerMask.value}");
        }
        
        // Method 2: Multiple raycast attempts in a radius pattern
        if (!foundGround || raycastAttempts > 1)
        {
            for (int i = 0; i < raycastAttempts; i++)
            {
                // Create a circular pattern around the original position
                float angle = (360f / raycastAttempts) * i * Mathf.Deg2Rad;
                float searchRadius = groundSearchRadius * (i + 1) / raycastAttempts;
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * searchRadius,
                    0f,
                    Mathf.Sin(angle) * searchRadius
                );
                
                Vector3 searchPos = worldPosition + offset;
                Vector3 searchRayStart = searchPos + Vector3.up * groundDetectionDistance;
                
                if (showDebugRays)
                    Debug.DrawRay(searchRayStart, Vector3.down * (groundDetectionDistance * 2f), Color.yellow, 5f);
                
                if (Physics.Raycast(searchRayStart, Vector3.down, out RaycastHit searchHit, groundDetectionDistance * 2f, groundLayerMask))
                {
                    float distance = Vector3.Distance(worldPosition, searchHit.point);
                    
                    if (!foundGround || distance < bestDistance)
                    {
                        bestGroundPosition = searchHit.point + Vector3.up * yOffset;
                        bestDistance = distance;
                        foundGround = true;
                        bestHit = searchHit;
                        
                        if (showGroundDetectionDebug)
                            Debug.Log($"[iNaturalist] Search raycast {i} hit: Y={searchHit.point.y:F2}, collider={searchHit.collider.name}, yOffset={yOffset:F2}, final Y={bestGroundPosition.y:F2}");
                    }
                }
            }
        }
        
        // Method 3: Sphere cast fallback (wider detection)
        if (!foundGround)
        {
            Vector3 sphereStart = worldPosition + Vector3.up * groundDetectionDistance;
            if (Physics.SphereCast(sphereStart, 2f, Vector3.down, out RaycastHit sphereHit, groundDetectionDistance * 2f, groundLayerMask))
            {
                bestGroundPosition = sphereHit.point + Vector3.up * yOffset;
                foundGround = true;
                bestHit = sphereHit;
                
                if (showGroundDetectionDebug)
                    Debug.Log($"[iNaturalist] Sphere cast hit: Y={sphereHit.point.y:F2}, collider={sphereHit.collider.name}, yOffset={yOffset:F2}, final Y={bestGroundPosition.y:F2}");
            }
        }
        
        // Method 4: Check for nearby terrain using overlap sphere
        if (!foundGround)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(worldPosition, groundSearchRadius, groundLayerMask);
            if (nearbyColliders.Length > 0)
            {
                // Find the closest collider and get its top surface
                Collider closestCollider = null;
                float closestDistance = float.MaxValue;
                
                foreach (var col in nearbyColliders)
                {
                    float distance = Vector3.Distance(worldPosition, col.ClosestPoint(worldPosition));
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestCollider = col;
                    }
                }
                
                if (closestCollider != null)
                {
                    Vector3 closestPoint = closestCollider.ClosestPoint(worldPosition);
                    bestGroundPosition = new Vector3(worldPosition.x, closestPoint.y + yOffset, worldPosition.z);
                    foundGround = true;
                    
                    if (showGroundDetectionDebug)
                        Debug.Log($"[iNaturalist] Overlap sphere found: {closestCollider.name}, Y={closestPoint.y:F2}, yOffset={yOffset:F2}, final Y={bestGroundPosition.y:F2}");
                }
            }
        }
        
        // Fallback: Use original position with offset if no ground found
        if (!foundGround)
        {
            bestGroundPosition = worldPosition + Vector3.up * yOffset;
            if (showGroundDetectionDebug)
                Debug.LogError($"[iNaturalist] NO GROUND FOUND! Using fallback position Y={bestGroundPosition.y:F2} (yOffset={yOffset:F2}) for position {worldPosition}. Check layer mask and terrain setup.");
        }
        else
        {
            // Validate the result - check if it seems reasonable
            float heightDifference = Mathf.Abs(bestGroundPosition.y - worldPosition.y);
            if (heightDifference > groundDetectionDistance)
            {
                if (showGroundDetectionDebug)
                    Debug.LogWarning($"[iNaturalist] Ground position seems unreasonable: height difference {heightDifference:F2}m, using fallback");
                bestGroundPosition = worldPosition + Vector3.up * yOffset;
            }
        }
        
        // Final debug output
        if (showGroundDetectionDebug)
        {
            string taxonInfo = obs != null ? $", taxon={GetTaxonCategory(obs)}" : "";
            Debug.Log($"[iNaturalist] Final ground position: {bestGroundPosition}, foundGround={foundGround}, surface={bestHit.collider?.name}, yOffset={yOffset:F2}{taxonInfo}");
        }
        
        return bestGroundPosition;
    }
    

    
    /// <summary>
    /// Get random Y rotation for more natural placement
    /// </summary>
    private Quaternion GetRandomYRotation()
    {
        return Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
    }
    

    
    /// <summary>
    /// Get the appropriate Y offset for an observation based on its taxon
    /// </summary>
    private float GetYOffsetForObservation(ObservationData obs)
    {
        if (!useTaxonSpecificPrefabs || !useCustomYOffset || obs == null)
        {
            return 2.0f; // Default Y offset
        }
        
        string taxonCategory = GetTaxonCategory(obs);
        
        switch (taxonCategory.ToLower())
        {
            case "plantae":
                return plantYOffset;
            case "animalia":
                return animalYOffset;
            case "aves":
                return birdYOffset;
            case "fungi":
                return fungiYOffset;
            case "insecta":
            case "arachnida":
                return insectYOffset;
            default:
                return unknownYOffset;
        }
    }
    
    /// <summary>
    /// Update scaling for all currently spawned observations (useful for runtime adjustments)
    /// </summary>
    [ContextMenu("Update All Observation Scales")]
    public void UpdateAllObservationScales()
    {
        if (spawnedPrefabs == null || observations == null) return;
        
        for (int i = 0; i < spawnedPrefabs.Count && i < observations.Count; i++)
        {
            if (spawnedPrefabs[i] != null)
            {
                float newScale = GetScaleForObservation(observations[i]);
                spawnedPrefabs[i].transform.localScale = Vector3.one * newScale;
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"[iNaturalist] Updated scales for {spawnedPrefabs.Count} observation prefabs");
    }
    
    /// <summary>
    /// Reposition all observations with improved ground detection
    /// </summary>
    [ContextMenu("Reposition All Observations")]
    public void RepositionAllObservations()
    {
        if (spawnedPrefabs == null || observations == null) return;
        
        for (int i = 0; i < spawnedPrefabs.Count && i < observations.Count; i++)
        {
            if (spawnedPrefabs[i] != null)
            {
                Vector2d latLng = ParseLocation(observations[i].location);
                if (latLng != Vector2d.zero)
                {
                    Vector3 worldPosition = map.GeoToWorldPosition(latLng, true);
                    Vector3 groundPosition = FindGroundPositionAdvanced(worldPosition, observations[i]);
                    
                    spawnedPrefabs[i].transform.position = groundPosition;
                }
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"[iNaturalist] Repositioned {spawnedPrefabs.Count} observation prefabs");
    }
    
    /// <summary>
    /// Debug method to analyze ground detection issues
    /// </summary>
    [ContextMenu("Debug Ground Detection")]
    public void DebugGroundDetection()
    {
        Debug.Log("=== GROUND DETECTION DEBUG ===");
        
        // Check layer mask
        Debug.Log($"Ground Layer Mask: {groundLayerMask.value} (binary: {System.Convert.ToString(groundLayerMask.value, 2)})");
        
        // List all active layers
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                bool isInMask = (groundLayerMask.value & (1 << i)) != 0;
                Debug.Log($"Layer {i}: '{layerName}' - In mask: {isInMask}");
            }
        }
        
        // Find all colliders in scene and their layers
        Collider[] allColliders = FindObjectsOfType<Collider>();
        Debug.Log($"Found {allColliders.Length} colliders in scene:");
        
        var layerGroups = new Dictionary<int, List<string>>();
        foreach (var col in allColliders)
        {
            if (!layerGroups.ContainsKey(col.gameObject.layer))
                layerGroups[col.gameObject.layer] = new List<string>();
            layerGroups[col.gameObject.layer].Add(col.name);
        }
        
        foreach (var group in layerGroups)
        {
            string layerName = LayerMask.LayerToName(group.Key);
            bool isInMask = (groundLayerMask.value & (1 << group.Key)) != 0;
            Debug.Log($"Layer {group.Key} ('{layerName}'): {group.Value.Count} colliders, In ground mask: {isInMask}");
            
            if (group.Value.Count <= 10) // Don't spam if too many
            {
                Debug.Log($"  Colliders: {string.Join(", ", group.Value)}");
            }
        }
    }
    
    /// <summary>
    /// Remove ObservationPositionTracker from all existing observation prefabs
    /// Call this to clean up old prefabs that might have the tracker
    /// </summary>
    [ContextMenu("Remove Position Trackers")]
    public void RemoveObservationPositionTrackers()
    {
        int removedCount = 0;
        
        foreach (var prefab in spawnedPrefabs)
        {
            if (prefab != null)
            {
                ObservationPositionTracker tracker = prefab.GetComponent<ObservationPositionTracker>();
                if (tracker != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(tracker);
                    }
                    else
                    {
                        DestroyImmediate(tracker);
                    }
                    removedCount++;
                }
            }
        }
        
        Debug.Log($"[iNaturalist] Removed {removedCount} ObservationPositionTracker components");
    }
    
    /// <summary>
    /// Debug method to analyze Y offset issues in spawned prefabs
    /// </summary>
    [ContextMenu("Debug Y Offset Issues")]
    public void DebugYOffsetIssues()
    {
        Debug.Log("=== Y OFFSET DEBUG ANALYSIS ===");
        
        if (spawnedPrefabs == null || spawnedPrefabs.Count == 0)
        {
            Debug.LogWarning("No spawned prefabs to analyze!");
            return;
        }
        
        Debug.Log($"Analyzing {spawnedPrefabs.Count} spawned observation prefabs...");
        
        for (int i = 0; i < spawnedPrefabs.Count && i < observations.Count; i++)
        {
            if (spawnedPrefabs[i] != null)
            {
                GameObject prefab = spawnedPrefabs[i];
                ObservationData obs = observations[i];
                
                // Check for ObservationPositionTracker
                ObservationPositionTracker tracker = prefab.GetComponent<ObservationPositionTracker>();
                string trackerInfo = tracker != null ? $"TRACKER(offset={tracker.GetComponent<ObservationPositionTracker>()})" : "NO TRACKER";
                
                // Get expected Y offset
                float expectedYOffset = GetYOffsetForObservation(obs);
                
                // Get current position
                Vector3 currentPos = prefab.transform.position;
                
                Debug.Log($"Prefab {i}: '{prefab.name}' at Y={currentPos.y:F2}");
                Debug.Log($"  - Taxon: {GetTaxonCategory(obs)}");
                Debug.Log($"  - Expected Y offset: {expectedYOffset:F2}");
                Debug.Log($"  - Position tracker: {trackerInfo}");
                Debug.Log($"  - Components: {string.Join(", ", prefab.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }
        }
        
        // Check INaturalist controller settings
        Debug.Log($"INaturalist Controller Settings:");
        Debug.Log($"  - useTaxonSpecificPrefabs: {useTaxonSpecificPrefabs}");
        Debug.Log($"  - useCustomYOffset: {useCustomYOffset}");
        Debug.Log($"  - plantYOffset: {plantYOffset}");
        Debug.Log($"  - animalYOffset: {animalYOffset}");
        Debug.Log($"  - birdYOffset: {birdYOffset}");
        Debug.Log($"  - unknownYOffset: {unknownYOffset}");
    }
    
    /// <summary>
    /// Test ground detection at a specific position
    /// </summary>
    [ContextMenu("Test Ground Detection At Player")]
    public void TestGroundDetectionAtPlayer()
    {
        if (playerTransform == null)
        {
            Debug.LogError("No player found! Assign playerTransform or move the player in the scene.");
            return;
        }
        
        Vector3 testPos = playerTransform.position;
        Debug.Log($"=== TESTING GROUND DETECTION AT PLAYER POSITION: {testPos} ===");
        
        bool oldDebug = showGroundDetectionDebug;
        bool oldRays = showDebugRays;
        showGroundDetectionDebug = true;
        showDebugRays = true;
        
        Vector3 advancedResult = FindGroundPositionAdvanced(testPos, null);
        Debug.Log($"Ground detection result: {advancedResult}");
        
        showGroundDetectionDebug = oldDebug;
        showDebugRays = oldRays;
    }
    
    /// <summary>
    /// Load bird audio from Xeno-Canto API for bird observations
    /// </summary>
    private IEnumerator LoadBirdAudio(ObservationData obs, GameObject birdPrefab)
    {
        if (showDebugInfo) 
        {
            Debug.Log($"[XenoCanto] LoadBirdAudio called for {obs.taxon?.preferred_common_name} ({obs.taxon?.name})");
        }
        
        if (obs.taxon == null || string.IsNullOrEmpty(obs.taxon.name))
        {
            if (showDebugInfo) Debug.LogWarning($"[XenoCanto] No taxon name for bird observation {obs.id}");
            yield break;
        }
        
        // TEMPORARY: Mock audio loading for testing the UI indicator
        if (showDebugInfo)
        {
            Debug.Log($"[XenoCanto] MOCK MODE: Simulating audio found for {obs.taxon.preferred_common_name}");
        }
        
        // Add the audio indicator to test the UI
        ObservationDisplay display = birdPrefab.GetComponent<ObservationDisplay>();
        if (display != null)
        {
            display.AddAudioIndicator();
            if (showDebugInfo)
            {
                Debug.Log($"[XenoCanto] Added audio indicator to observation display (MOCK MODE)");
            }
        }
        
        // TEMPORARY: Skip actual API call for now due to 404 errors
        yield break;
    }
}

// Data structures for API responses
[System.Serializable]
public class INaturalistResponse
{
    public ObservationData[] results;
    public int total_results;
    public int page;
    public int per_page;
}

[System.Serializable]
public class ObservationData
{
    public int id;
    public string observed_on;
    public string location;
    public TaxonData taxon;
    public PhotoData[] photos;
    public UserData user;
    public bool captive;
    public string quality_grade;
}

[System.Serializable]
public class PhotoData
{
    public string url;
}

[System.Serializable]
public class TaxonData
{
    public int id;
    public string name;
    public string preferred_common_name;
    public string iconic_taxon_name;
}

[System.Serializable]
public class UserData
{
    public int id;
    public string login;
}
