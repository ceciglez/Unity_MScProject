using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Main Menu Controller - Pre-game menu for choosing exploration mode
/// Allows users to either explore their current location or search for an iNaturalist user
///
/// FUNCTIONALITY:
/// - Exploration Mode: Start at default/current location
/// - User Search Mode: Search for iNaturalist user and start at their last observation
/// - About: Show information about the project
///
/// INTEGRATION:
/// - Uses GameManager to store selected coordinates
/// - Loads main game scene after selection
/// - Queries iNaturalist API for user search
///
/// AI CONTRIBUTION: 95% - Complete implementation
/// HUMAN CONTRIBUTION: 5% - Requirements and design direction
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("The main menu panel with mode selection buttons")]
    public GameObject mainMenuPanel;

    [Tooltip("The user search panel for entering iNaturalist username")]
    public GameObject userSearchPanel;

    [Tooltip("The about/info panel")]
    public GameObject aboutPanel;

    [Header("Main Menu Buttons")]
    public Button explorationModeButton;
    public Button searchUserModeButton;
    public Button aboutButton;
    public Button quitButton;

    [Header("User Search Elements")]
    public InputField usernameInput;
    public Button searchButton;
    public Button backButton;
    public Text searchStatusText;

    [Header("About Panel Elements")]
    public Button aboutBackButton;

    [Header("Scene Settings")]
    [Tooltip("Name of the main game scene to load")]
    public string gameSceneName = "MainScene";

    [Header("Default Location")]
    [Tooltip("Default latitude if no user search (e.g., San Francisco)")]
    public double defaultLatitude = 37.7749;

    [Tooltip("Default longitude if no user search")]
    public double defaultLongitude = -122.4194;

    private bool isSearching = false;

    void Start()
    {
        // Setup button listeners
        if (explorationModeButton != null)
            explorationModeButton.onClick.AddListener(OnExplorationModePressed);

        if (searchUserModeButton != null)
            searchUserModeButton.onClick.AddListener(OnSearchUserModePressed);

        if (aboutButton != null)
            aboutButton.onClick.AddListener(OnAboutPressed);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);

        if (searchButton != null)
            searchButton.onClick.AddListener(OnSearchButtonPressed);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackToMainMenu);

        if (aboutBackButton != null)
            aboutBackButton.onClick.AddListener(OnBackToMainMenu);

        // Show main menu panel by default
        ShowMainMenu();

        // Clear any previous location data
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearStartLocation();
        }

        Debug.Log("[MainMenu] Main menu initialized");
    }

    void Update()
    {
        // Allow Enter key to trigger search when username input is focused
        if (userSearchPanel != null && userSearchPanel.activeSelf &&
            usernameInput != null && Input.GetKeyDown(KeyCode.Return))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == usernameInput.gameObject)
            {
                OnSearchButtonPressed();
            }
        }

        // Escape key to go back
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (userSearchPanel != null && userSearchPanel.activeSelf)
                OnBackToMainMenu();
            else if (aboutPanel != null && aboutPanel.activeSelf)
                OnBackToMainMenu();
        }
    }

    /// <summary>
    /// Shows the main menu panel and hides others
    /// </summary>
    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (userSearchPanel != null) userSearchPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);

        Debug.Log("[MainMenu] Showing main menu");
    }

    /// <summary>
    /// Exploration Mode - Start at default location
    /// </summary>
    private void OnExplorationModePressed()
    {
        Debug.Log("[MainMenu] Exploration mode selected - using default location");

        // Set default location in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetStartLocation(defaultLatitude, defaultLongitude, "Default Location");
        }

        // Load the game scene
        StartCoroutine(LoadGameScene());
    }

    /// <summary>
    /// Search User Mode - Show username search panel
    /// </summary>
    private void OnSearchUserModePressed()
    {
        Debug.Log("[MainMenu] Search user mode selected");

        // Show user search panel
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (userSearchPanel != null) userSearchPanel.SetActive(true);

        // Clear previous search
        if (usernameInput != null) usernameInput.text = "";
        if (searchStatusText != null) searchStatusText.text = "";

        // Focus the input field
        if (usernameInput != null)
        {
            usernameInput.Select();
            usernameInput.ActivateInputField();
        }
    }

    /// <summary>
    /// About - Show information panel
    /// </summary>
    private void OnAboutPressed()
    {
        Debug.Log("[MainMenu] About pressed");

        // Show about panel
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(true);
    }

    /// <summary>
    /// Quit the application
    /// </summary>
    private void OnQuitPressed()
    {
        Debug.Log("[MainMenu] Quit pressed");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Back to main menu from any panel
    /// </summary>
    private void OnBackToMainMenu()
    {
        ShowMainMenu();
    }

    /// <summary>
    /// Search button pressed - query iNaturalist API
    /// </summary>
    private void OnSearchButtonPressed()
    {
        if (usernameInput == null || string.IsNullOrEmpty(usernameInput.text.Trim()))
        {
            if (searchStatusText != null)
            {
                searchStatusText.text = "⚠️ Please enter a username";
                searchStatusText.color = new Color(1f, 0.7f, 0f); // Yellow
            }
            return;
        }

        if (isSearching)
        {
            Debug.Log("[MainMenu] Search already in progress");
            return;
        }

        string username = usernameInput.text.Trim();
        StartCoroutine(SearchUserAndStartGame(username));
    }

    /// <summary>
    /// Search for iNaturalist user and start game at their last observation
    /// </summary>
    private IEnumerator SearchUserAndStartGame(string username)
    {
        isSearching = true;

        // Disable search button during search
        if (searchButton != null)
            searchButton.interactable = false;

        // Show searching status
        if (searchStatusText != null)
        {
            searchStatusText.text = $"🔍 Searching for user '{username}'...";
            searchStatusText.color = new Color(1f, 0.8f, 0.2f); // Orange/yellow
        }

        // Build API URL
        string apiUrl = $"https://api.inaturalist.org/v1/observations?user_login={username}&order=desc&order_by=observed_on&per_page=1";

        Debug.Log($"[MainMenu] Fetching user observations: {apiUrl}");

        #if UNITY_WEBGL && !UNITY_EDITOR
            // Use WebGL JavaScript bridge
            bool requestComplete = false;
            string responseData = null;
            string fetchError = null;

            WebGLNetworkBridge.Instance.FetchJSON(
                apiUrl,
                (data) => {
                    responseData = data;
                    requestComplete = true;
                },
                (error) => {
                    fetchError = error;
                    requestComplete = true;
                }
            );

            while (!requestComplete)
            {
                yield return null;
            }

            if (responseData != null)
            {
                ProcessUserSearchResponse(responseData, username);
            }
            else
            {
                HandleSearchError(fetchError, username);
            }
        #else
            // Use UnityWebRequest in Editor
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(apiUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    ProcessUserSearchResponse(request.downloadHandler.text, username);
                }
                else
                {
                    HandleSearchError(request.error, username);
                }
            }
        #endif

        // Re-enable search button
        if (searchButton != null)
            searchButton.interactable = true;

        isSearching = false;
    }

    /// <summary>
    /// Handle search errors with helpful messages
    /// </summary>
    private void HandleSearchError(string error, string username)
    {
        if (searchStatusText != null)
        {
            // Check if it's a 422 error (invalid username format)
            if (error != null && error.Contains("422"))
            {
                searchStatusText.text = $"❌ Invalid username format. Try removing underscores or special characters";
                searchStatusText.color = Color.red;
                Debug.LogWarning($"[MainMenu] Username '{username}' rejected by API (422). iNaturalist usernames cannot contain underscores.");
            }
            else
            {
                searchStatusText.text = $"❌ Network error: {error}";
                searchStatusText.color = Color.red;
                Debug.LogError($"[MainMenu] Failed to fetch user data: {error}");
            }
        }
    }

    /// <summary>
    /// Process the API response and extract coordinates
    /// </summary>
    private void ProcessUserSearchResponse(string jsonResponse, string username)
    {
        try
        {
            INaturalistResponse response = JsonUtility.FromJson<INaturalistResponse>(jsonResponse);

            if (response.results == null || response.results.Length == 0)
            {
                if (searchStatusText != null)
                {
                    searchStatusText.text = $"❌ No observations found for '{username}'";
                    searchStatusText.color = new Color(1f, 0.4f, 0f); // Orange-red
                }
                Debug.LogWarning($"[MainMenu] No observations found for user: {username}");
                return;
            }

            // Get the last observation
            ObservationData lastObs = response.results[0];

            if (string.IsNullOrEmpty(lastObs.location))
            {
                if (searchStatusText != null)
                {
                    searchStatusText.text = $"⚠️ User '{username}' found, but observation has no location";
                    searchStatusText.color = new Color(1f, 0.7f, 0f); // Yellow-orange
                }
                Debug.LogWarning($"[MainMenu] User's last observation has no location");
                return;
            }

            // Parse location (format: "lat,lng")
            string[] coords = lastObs.location.Split(',');
            if (coords.Length != 2)
            {
                if (searchStatusText != null)
                {
                    searchStatusText.text = $"❌ Invalid location format";
                    searchStatusText.color = Color.red;
                }
                return;
            }

            if (double.TryParse(coords[0], out double lat) && double.TryParse(coords[1], out double lng))
            {
                // Show success status
                string speciesName = lastObs.taxon?.preferred_common_name ?? lastObs.taxon?.name ?? "Unknown species";

                if (searchStatusText != null)
                {
                    searchStatusText.text = $"✅ Found '{username}'! Starting at their observation...";
                    searchStatusText.color = new Color(0.2f, 0.8f, 0.2f); // Green
                }

                Debug.Log($"[MainMenu] User found! Last observation: {speciesName} at {lat}, {lng}");

                // Store location in GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetStartLocation(lat, lng, username, speciesName);
                }

                // Load the game scene
                StartCoroutine(LoadGameSceneWithDelay(1f));
            }
            else
            {
                if (searchStatusText != null)
                {
                    searchStatusText.text = $"❌ Failed to parse coordinates";
                    searchStatusText.color = Color.red;
                }
            }
        }
        catch (System.Exception e)
        {
            if (searchStatusText != null)
            {
                searchStatusText.text = $"❌ Error parsing API response";
                searchStatusText.color = Color.red;
            }
            Debug.LogError($"[MainMenu] Error parsing user search response: {e.Message}");
        }
    }

    /// <summary>
    /// Load game scene with a small delay for user to see success message
    /// </summary>
    private IEnumerator LoadGameSceneWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(LoadGameScene());
    }

    /// <summary>
    /// Load the main game scene
    /// </summary>
    private IEnumerator LoadGameScene()
    {
        Debug.Log($"[MainMenu] Loading game scene: {gameSceneName}");

        // Show loading message
        if (searchStatusText != null)
        {
            searchStatusText.text = "Loading game...";
            searchStatusText.color = new Color(0.5f, 0.7f, 1f); // Light blue
        }

        // Load scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("[MainMenu] Game scene loaded");
    }
}

// These classes should already exist in INaturalistMapController.cs
// Including them here for reference if needed as separate file
#if false
[System.Serializable]
public class INaturalistResponse
{
    public int total_results;
    public int page;
    public int per_page;
    public ObservationData[] results;
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
public class TaxonData
{
    public int id;
    public string name;
    public string preferred_common_name;
    public string iconic_taxon_name;
}

[System.Serializable]
public class PhotoData
{
    public string url;
}

[System.Serializable]
public class UserData
{
    public int id;
    public string login;
    public string name;
}
#endif
