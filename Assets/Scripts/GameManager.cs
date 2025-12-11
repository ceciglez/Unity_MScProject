using UnityEngine;

/// <summary>
/// Game Manager - Persistent singleton for passing data between scenes
/// Stores the starting location selected from the main menu
///
/// FUNCTIONALITY:
/// - Persists between scene loads (DontDestroyOnLoad)
/// - Stores latitude/longitude for map initialization
/// - Stores username and species name for context
/// - Singleton pattern ensures only one instance exists
///
/// USAGE:
/// MainMenu: GameManager.Instance.SetStartLocation(lat, lng, username)
/// GameScene: double lat = GameManager.Instance.startLatitude
///
/// AI CONTRIBUTION: 95% - Complete implementation
/// HUMAN CONTRIBUTION: 5% - Requirements
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find existing instance
                _instance = FindObjectOfType<GameManager>();

                // Create new instance if none exists
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Start Location Data")]
    [Tooltip("Starting latitude (set from main menu)")]
    public double startLatitude = 37.7749; // Default: San Francisco

    [Tooltip("Starting longitude (set from main menu)")]
    public double startLongitude = -122.4194;

    [Tooltip("Username searched (if any)")]
    public string searchedUsername = "";

    [Tooltip("Species name from last observation (if any)")]
    public string observedSpecies = "";

    [Tooltip("Whether a custom start location was set")]
    public bool hasCustomStartLocation = false;

    void Awake()
    {
        // Implement singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameManager] GameManager instance created and set to persist between scenes");
        }
        else if (_instance != this)
        {
            Debug.Log("[GameManager] Duplicate GameManager detected, destroying...");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set the starting location for the game
    /// </summary>
    /// <param name="latitude">Latitude in degrees</param>
    /// <param name="longitude">Longitude in degrees</param>
    /// <param name="username">Username searched (optional)</param>
    /// <param name="species">Species observed (optional)</param>
    public void SetStartLocation(double latitude, double longitude, string username = "", string species = "")
    {
        startLatitude = latitude;
        startLongitude = longitude;
        searchedUsername = username;
        observedSpecies = species;
        hasCustomStartLocation = true;

        Debug.Log($"[GameManager] Start location set to: {latitude}, {longitude}");
        if (!string.IsNullOrEmpty(username))
        {
            Debug.Log($"[GameManager] Username: {username}");
        }
        if (!string.IsNullOrEmpty(species))
        {
            Debug.Log($"[GameManager] Species: {species}");
        }
    }

    /// <summary>
    /// Clear the custom start location (use default)
    /// </summary>
    public void ClearStartLocation()
    {
        hasCustomStartLocation = false;
        searchedUsername = "";
        observedSpecies = "";

        Debug.Log("[GameManager] Start location cleared, will use default");
    }

    /// <summary>
    /// Get the start location as a Mapbox Vector2d
    /// </summary>
    public Mapbox.Utils.Vector2d GetStartLocationAsVector2d()
    {
        return new Mapbox.Utils.Vector2d(startLatitude, startLongitude);
    }

    /// <summary>
    /// Check if a custom location was set from main menu
    /// </summary>
    public bool HasCustomLocation()
    {
        return hasCustomStartLocation;
    }

    /// <summary>
    /// Check if a user was searched
    /// </summary>
    public bool HasSearchedUser()
    {
        return !string.IsNullOrEmpty(searchedUsername);
    }

    /// <summary>
    /// Get info string about the start location
    /// </summary>
    public string GetLocationInfo()
    {
        if (!hasCustomStartLocation)
        {
            return "Default Location";
        }

        if (HasSearchedUser())
        {
            string info = $"User: {searchedUsername}";
            if (!string.IsNullOrEmpty(observedSpecies))
            {
                info += $" | Species: {observedSpecies}";
            }
            return info;
        }

        return $"Custom Location: {startLatitude:F4}, {startLongitude:F4}";
    }
}
