using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Main Menu Overlay - Pre-game menu as an overlay in the same scene
/// Shows before map initialization, hides when user selects a mode
///
/// FUNCTIONALITY:
/// - Overlays the game scene on startup
/// - Pauses/blocks interaction until mode selected
/// - Exploration Mode: Use current/default location
/// - User Search Mode: Search for iNaturalist user
/// - Initializes map after selection
///
/// ADVANTAGES OVER SEPARATE SCENE:
/// - Simpler setup (no scene transitions)
/// - Faster (no scene loading)
/// - Can show map preview in background
/// - Single scene to manage
///
/// INTEGRATION:
/// - Attach to Canvas in game scene
/// - Blocks map initialization until user chooses
/// - Works with existing map setup
///
/// AI CONTRIBUTION: 95% - Complete implementation
/// HUMAN CONTRIBUTION: 5% - Requirements
/// </summary>
public class MainMenuOverlay : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("The main menu panel - shown on startup")]
    public GameObject mainMenuPanel;

    [Tooltip("The user search panel")]
    public GameObject userSearchPanel;

    [Tooltip("The about/info panel")]
    public GameObject aboutPanel;

    [Header("Main Menu Buttons")]
    public Button explorationModeButton;
    public Button searchUserModeButton;
    public Button aboutButton;

    [Header("User Search Elements")]
    public InputField usernameInput;
    public Button searchButton;
    public Button backButton;
    public Text searchStatusText;

    [Header("About Panel Elements")]
    public Button aboutBackButton;

    [Header("Map References")]
    [Tooltip("Reference to the Mapbox AbstractMap to initialize")]
    public Mapbox.Unity.Map.AbstractMap map;

    [Tooltip("Reference to INaturalistMapController")]
    public INaturalistMapController mapController;

    [Header("Biodiversity Effect References")]
    [Tooltip("Reference to BiodiversityVolumeSpawner - will be disabled during menu")]
    public BiodiversityVolumeSpawner biodiversityVolumeSpawner;

    [Tooltip("Global Volume GameObject - will be disabled during menu")]
    public GameObject globalVolumeObject;

    [Header("Default Location")]
    [Tooltip("Default latitude if no user search")]
    public double defaultLatitude = 37.7749;

    [Tooltip("Default longitude if no user search")]
    public double defaultLongitude = -122.4194;

    [Header("Settings")]
    [Tooltip("Hide overlay after selection")]
    public bool hideAfterSelection = true;

    [Tooltip("Block player movement until selection")]
    public bool blockPlayerMovement = true;

    [Tooltip("Lock cursor after game starts (first-person mode)")]
    public bool lockCursorAfterStart = true;

    private bool isSearching = false;
    private bool hasSelectedMode = false;
    private GameObject player;
    private MonoBehaviour[] playerControllers;
    private MonoBehaviour[] cameraControllers;
    private System.Collections.Generic.List<MonoBehaviour> allDisabledScripts = new System.Collections.Generic.List<MonoBehaviour>();

    void Start()
    {
        // Setup button listeners
        if (explorationModeButton != null)
            explorationModeButton.onClick.AddListener(OnExplorationModePressed);

        if (searchUserModeButton != null)
            searchUserModeButton.onClick.AddListener(OnSearchUserModePressed);

        if (aboutButton != null)
            aboutButton.onClick.AddListener(OnAboutPressed);

        if (searchButton != null)
            searchButton.onClick.AddListener(OnSearchButtonPressed);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackToMainMenu);

        if (aboutBackButton != null)
            aboutBackButton.onClick.AddListener(OnBackToMainMenu);

        // Find map if not assigned
        if (map == null)
        {
            map = FindObjectOfType<Mapbox.Unity.Map.AbstractMap>();
        }

        // Find map controller if not assigned
        if (mapController == null)
        {
            mapController = FindObjectOfType<INaturalistMapController>();
        }

        // Find biodiversity volume spawner if not assigned
        if (biodiversityVolumeSpawner == null)
        {
            biodiversityVolumeSpawner = FindObjectOfType<BiodiversityVolumeSpawner>();
        }

        // Find global volume if not assigned
        if (globalVolumeObject == null)
        {
            globalVolumeObject = GameObject.Find("Global Volume");
        }

        // Disable biodiversity effects during menu
        DisableBiodiversityEffects();

        // Find player
        player = GameObject.FindGameObjectWithTag("Player");

        // Block player movement
        if (blockPlayerMovement && player != null)
        {
            DisablePlayerMovement();
        }

        // Show main menu overlay
        ShowMainMenu();

        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[MainMenuOverlay] Overlay initialized - waiting for user selection");
    }

    void Update()
    {
        // Keep cursor visible and unlocked during menu
        if (!hasSelectedMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

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
        if (Input.GetKeyDown(KeyCode.Escape) && !hasSelectedMode)
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

        Debug.Log("[MainMenuOverlay] Showing main menu");
    }

    /// <summary>
    /// Exploration Mode - Use default location
    /// </summary>
    private void OnExplorationModePressed()
    {
        Debug.Log("[MainMenuOverlay] Exploration mode selected - using default location");

        hasSelectedMode = true;

        // Initialize map at default location
        StartCoroutine(InitializeMapAndStart(defaultLatitude, defaultLongitude, "", ""));
    }

    /// <summary>
    /// Search User Mode - Show username search panel
    /// </summary>
    private void OnSearchUserModePressed()
    {
        Debug.Log("[MainMenuOverlay] Search user mode selected");

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
        Debug.Log("[MainMenuOverlay] About pressed");

        // Show about panel
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(true);
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
            Debug.Log("[MainMenuOverlay] Search already in progress");
            return;
        }

        string username = usernameInput.text.Trim();
        StartCoroutine(SearchUserAndInitializeMap(username));
    }

    /// <summary>
    /// Search for iNaturalist user and initialize map at their last observation
    /// </summary>
    private IEnumerator SearchUserAndInitializeMap(string username)
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

        Debug.Log($"[MainMenuOverlay] Fetching user observations: {apiUrl}");

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
                Debug.LogWarning($"[MainMenuOverlay] Username '{username}' rejected by API (422). iNaturalist usernames cannot contain underscores.");
            }
            else
            {
                searchStatusText.text = $"❌ Network error: {error}";
                searchStatusText.color = Color.red;
                Debug.LogError($"[MainMenuOverlay] Failed to fetch user data: {error}");
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
                Debug.LogWarning($"[MainMenuOverlay] No observations found for user: {username}");
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
                Debug.LogWarning($"[MainMenuOverlay] User's last observation has no location");
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

                Debug.Log($"[MainMenuOverlay] User found! Last observation: {speciesName} at {lat}, {lng}");

                hasSelectedMode = true;

                // Initialize map at user's location
                StartCoroutine(InitializeMapAndStart(lat, lng, username, speciesName));
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
            Debug.LogError($"[MainMenuOverlay] Error parsing user search response: {e.Message}");
        }
    }

    /// <summary>
    /// Initialize the map at the selected location and start the game
    /// </summary>
    private IEnumerator InitializeMapAndStart(double lat, double lng, string username = "", string species = "")
    {
        Debug.Log($"[MainMenuOverlay] ========================================");
        Debug.Log($"[MainMenuOverlay] INITIALIZING MAP");
        Debug.Log($"[MainMenuOverlay] Location: {lat}, {lng}");
        if (!string.IsNullOrEmpty(username))
        {
            Debug.Log($"[MainMenuOverlay] User: {username}");
            Debug.Log($"[MainMenuOverlay] Species: {species}");
        }
        Debug.Log($"[MainMenuOverlay] ========================================");

        // Update map center coordinates
        if (map != null)
        {
            Mapbox.Utils.Vector2d newCenter = new Mapbox.Utils.Vector2d(lat, lng);

            // Update via Options
            if (map.Options != null && map.Options.locationOptions != null)
            {
                map.Options.locationOptions.latitudeLongitude = $"{lat},{lng}";
                Debug.Log($"[MainMenuOverlay] ✓ Map center set to: {lat}, {lng}");
            }

            // Initialize/Update the map
            map.Initialize(newCenter, map.AbsoluteZoom);

            Debug.Log("[MainMenuOverlay] Map initialization started");
        }
        else
        {
            Debug.LogError("[MainMenuOverlay] Map reference is null!");
        }

        // Wait for map to initialize
        yield return new WaitForSeconds(2f);

        // If user was searched, load their observations
        if (!string.IsNullOrEmpty(username) && mapController != null)
        {
            Debug.Log($"[MainMenuOverlay] Loading observations for user: {username}");

            yield return StartCoroutine(mapController.LoadObservationsWithUserPriority(
                username,
                (float)lat,
                (float)lng,
                5f // 5km radius
            ));

            Debug.Log($"[MainMenuOverlay] ✓ Observations loaded for user: {username}");
        }

        // Hide the overlay
        if (hideAfterSelection)
        {
            HideOverlay();
        }

        // Enable player movement
        if (blockPlayerMovement && player != null)
        {
            EnablePlayerMovement();
        }

        // Enable biodiversity effects now that game has started
        EnableBiodiversityEffects();

        // Restore cursor state (lock if needed for first-person mode)
        if (lockCursorAfterStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[MainMenuOverlay] Cursor locked for gameplay");
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[MainMenuOverlay] Cursor remains unlocked");
        }

        Debug.Log("[MainMenuOverlay] Game started!");
    }

    /// <summary>
    /// Hide the menu overlay
    /// </summary>
    private void HideOverlay()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (userSearchPanel != null) userSearchPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);

        Debug.Log("[MainMenuOverlay] Overlay hidden");
    }

    /// <summary>
    /// Disable player movement scripts and camera controls
    /// </summary>
    private void DisablePlayerMovement()
    {
        if (player == null) return;

        allDisabledScripts.Clear();

        // Disable ALL scripts on player GameObject
        playerControllers = player.GetComponents<MonoBehaviour>();
        foreach (var controller in playerControllers)
        {
            if (controller is MainMenuOverlay) continue;
            if (controller == null || !controller.enabled) continue;

            controller.enabled = false;
            allDisabledScripts.Add(controller);
            Debug.Log($"[MainMenuOverlay] Disabled on Player: {controller.GetType().Name}");
        }

        // Disable camera scripts if camera is separate
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.gameObject != player)
        {
            cameraControllers = mainCamera.GetComponents<MonoBehaviour>();
            foreach (var script in cameraControllers)
            {
                if (script is MainMenuOverlay) continue;
                if (script == null || !script.enabled) continue;

                script.enabled = false;
                allDisabledScripts.Add(script);
                Debug.Log($"[MainMenuOverlay] Disabled on Camera: {script.GetType().Name}");
            }
        }

        // ALSO disable InGameUIController and any other interfering scripts
        InGameUIController uiController = FindObjectOfType<InGameUIController>();
        if (uiController != null && uiController.enabled)
        {
            uiController.enabled = false;
            allDisabledScripts.Add(uiController);
            Debug.Log("[MainMenuOverlay] Disabled InGameUIController");
        }

        Debug.Log($"[MainMenuOverlay] Disabled {allDisabledScripts.Count} scripts total");
    }

    /// <summary>
    /// Enable player movement scripts and camera controls
    /// </summary>
    private void EnablePlayerMovement()
    {
        // Re-enable all scripts that were disabled
        foreach (var script in allDisabledScripts)
        {
            if (script != null && script is MainMenuOverlay) continue;
            if (script != null)
            {
                script.enabled = true;
                Debug.Log($"[MainMenuOverlay] Re-enabled: {script.GetType().Name}");
            }
        }

        Debug.Log($"[MainMenuOverlay] Re-enabled {allDisabledScripts.Count} scripts");
        allDisabledScripts.Clear();
    }

    /// <summary>
    /// Disable biodiversity post-processing effects during menu
    /// </summary>
    private void DisableBiodiversityEffects()
    {
        // Disable BiodiversityVolumeSpawner script
        if (biodiversityVolumeSpawner != null)
        {
            biodiversityVolumeSpawner.enabled = false;
            Debug.Log("[MainMenuOverlay] BiodiversityVolumeSpawner disabled");
        }

        // Disable Global Volume GameObject (contains post-processing)
        if (globalVolumeObject != null)
        {
            globalVolumeObject.SetActive(false);
            Debug.Log("[MainMenuOverlay] Global Volume disabled");
        }

        Debug.Log("[MainMenuOverlay] ✓ Biodiversity effects disabled - menu will show normal colors");
    }

    /// <summary>
    /// Enable biodiversity post-processing effects after game starts
    /// </summary>
    private void EnableBiodiversityEffects()
    {
        // Enable BiodiversityVolumeSpawner script
        if (biodiversityVolumeSpawner != null)
        {
            biodiversityVolumeSpawner.enabled = true;
            Debug.Log("[MainMenuOverlay] BiodiversityVolumeSpawner enabled");
        }

        // Enable Global Volume GameObject
        if (globalVolumeObject != null)
        {
            globalVolumeObject.SetActive(true);
            Debug.Log("[MainMenuOverlay] Global Volume enabled");
        }

        Debug.Log("[MainMenuOverlay] ✓ Biodiversity effects enabled - colors will now show biodiversity");
    }

    /// <summary>
    /// Public method to show overlay again (if needed)
    /// </summary>
    public void ShowOverlay()
    {
        ShowMainMenu();
        hasSelectedMode = false;

        if (blockPlayerMovement && player != null)
        {
            DisablePlayerMovement();
        }

        // Disable biodiversity effects when showing menu again
        DisableBiodiversityEffects();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
