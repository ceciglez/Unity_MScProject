using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Map Initializer - Initializes the Mapbox map with coordinates from GameManager
/// Should be attached to the same GameObject as AbstractMap in the game scene
///
/// FUNCTIONALITY:
/// - Reads start location from GameManager on scene load
/// - Updates the map's center coordinates before initialization
/// - Falls back to map's default coordinates if no custom location is set
///
/// INTEGRATION:
/// - Works with GameManager singleton for cross-scene data
/// - Modifies AbstractMap's location options
/// - Runs in Awake() before map initialization
///
/// SETUP:
/// 1. Attach to GameObject with AbstractMap component
/// 2. Ensure GameManager exists in scene or will be created automatically
///
/// AI CONTRIBUTION: 95% - Complete implementation
/// HUMAN CONTRIBUTION: 5% - Requirements
/// </summary>
[RequireComponent(typeof(AbstractMap))]
public class MapInitializer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the AbstractMap component (auto-assigned)")]
    public AbstractMap map;

    [Header("Debug")]
    [Tooltip("Show detailed initialization logs")]
    public bool showDebugLogs = true;

    void Awake()
    {
        // Get AbstractMap component
        if (map == null)
        {
            map = GetComponent<AbstractMap>();
        }

        if (map == null)
        {
            Debug.LogError("[MapInitializer] No AbstractMap component found! Please attach this script to the map GameObject.");
            return;
        }

        // Check if GameManager has a custom start location
        if (GameManager.Instance != null && GameManager.Instance.HasCustomLocation())
        {
            InitializeMapWithCustomLocation();
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("[MapInitializer] No custom location set, using map's default coordinates");
            }
        }
    }

    /// <summary>
    /// Initialize the map with custom coordinates from GameManager
    /// </summary>
    private void InitializeMapWithCustomLocation()
    {
        double lat = GameManager.Instance.startLatitude;
        double lng = GameManager.Instance.startLongitude;
        string locationInfo = GameManager.Instance.GetLocationInfo();

        if (showDebugLogs)
        {
            Debug.Log($"[MapInitializer] ========================================");
            Debug.Log($"[MapInitializer] INITIALIZING MAP WITH CUSTOM LOCATION");
            Debug.Log($"[MapInitializer] ========================================");
            Debug.Log($"[MapInitializer] Latitude: {lat}");
            Debug.Log($"[MapInitializer] Longitude: {lng}");
            Debug.Log($"[MapInitializer] Info: {locationInfo}");
            Debug.Log($"[MapInitializer] ========================================");
        }

        // Update the map's center latitude/longitude BEFORE initialization
        // This is the key: set it in Awake() before the map's Start() runs
        Vector2d newCenter = new Vector2d(lat, lng);

        // Method 1: Update via Options (preferred)
        if (map.Options != null && map.Options.locationOptions != null)
        {
            map.Options.locationOptions.latitudeLongitude = $"{lat},{lng}";

            if (showDebugLogs)
            {
                Debug.Log($"[MapInitializer] ✓ Map center updated via Options: {lat}, {lng}");
            }
        }

        // Method 2: Initialize with the new location (if map supports it)
        // This will be called automatically by the map's Start() method
        // but we're setting the options above so it uses our coordinates

        if (showDebugLogs)
        {
            Debug.Log($"[MapInitializer] Map will initialize at: {locationInfo}");

            if (GameManager.Instance.HasSearchedUser())
            {
                Debug.Log($"[MapInitializer] User searched: {GameManager.Instance.searchedUsername}");
                if (!string.IsNullOrEmpty(GameManager.Instance.observedSpecies))
                {
                    Debug.Log($"[MapInitializer] Species: {GameManager.Instance.observedSpecies}");
                }
            }
        }
    }

    /// <summary>
    /// Called after map is initialized - can be used to load user observations
    /// </summary>
    void Start()
    {
        // If a user was searched, we might want to auto-load their observations
        if (GameManager.Instance != null && GameManager.Instance.HasSearchedUser())
        {
            StartCoroutine(LoadUserObservationsAfterMapReady());
        }
    }

    /// <summary>
    /// Wait for map to be ready, then load user observations
    /// </summary>
    private System.Collections.IEnumerator LoadUserObservationsAfterMapReady()
    {
        // Wait a few frames for map to initialize
        yield return new WaitForSeconds(2f);

        string username = GameManager.Instance.searchedUsername;
        double lat = GameManager.Instance.startLatitude;
        double lng = GameManager.Instance.startLongitude;

        if (showDebugLogs)
        {
            Debug.Log($"[MapInitializer] Loading observations for user: {username}");
        }

        // Find the INaturalistMapController and load observations with user priority
        INaturalistMapController mapController = FindObjectOfType<INaturalistMapController>();
        if (mapController != null)
        {
            yield return StartCoroutine(mapController.LoadObservationsWithUserPriority(
                username,
                (float)lat,
                (float)lng,
                5f // 5km radius
            ));

            if (showDebugLogs)
            {
                Debug.Log($"[MapInitializer] ✓ Observations loaded for user: {username}");
            }

            // Update the BiodiversityUI to show that we started at this user's location
            BiodiversityUI biodiversityUI = FindObjectOfType<BiodiversityUI>();
            if (biodiversityUI != null && biodiversityUI.searchStatusText != null)
            {
                biodiversityUI.searchStatusText.text = $"✅ Started at '{username}'s observation";
                biodiversityUI.searchStatusText.color = new Color(0.2f, 0.8f, 0.2f); // Green

                // Clear after 5 seconds
                yield return new WaitForSeconds(5f);
                biodiversityUI.searchStatusText.text = "";
            }
        }
        else
        {
            Debug.LogWarning("[MapInitializer] INaturalistMapController not found - cannot load user observations");
        }
    }

    /// <summary>
    /// Debug method to print current map location
    /// </summary>
    [ContextMenu("Debug Current Map Location")]
    public void DebugCurrentMapLocation()
    {
        if (map != null)
        {
            Debug.Log($"[MapInitializer] Current map center: {map.CenterLatitudeLongitude}");
            Debug.Log($"[MapInitializer] Current zoom: {map.Zoom}");
        }
    }
}
