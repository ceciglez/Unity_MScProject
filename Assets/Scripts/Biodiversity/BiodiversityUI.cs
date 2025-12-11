using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI controller for biodiversity visualization system
/// Displays real-time biodiversity metrics and provides user controls
///
/// FUNCTIONALITY:
/// - Displays Simpson's Diversity Index at player position with qualitative labels
/// - Shows observation count and species count in current grid cell
/// - Provides diversity intensity slider (0-2x multiplier)
/// - Manual recalculation button for biodiversity system
/// - Toggle UI visibility with 'B' key (configurable)
///
/// UI ELEMENTS:
/// - Simpson's Index text with color-coded labels:
///   * 0.0-0.2: "Very Low Diversity" (red)
///   * 0.2-0.4: "Low Diversity" (orange)
///   * 0.4-0.6: "Moderate Diversity" (yellow)
///   * 0.6-0.8: "High Diversity" (light green)
///   * 0.8-1.0: "Very High Diversity" (green)
/// - Observation count (total iNaturalist observations in cell)
/// - Species count (unique species in cell)
///
/// INTEGRATION:
/// - Queries BiodiversityScoreManager for real-time data
/// - Updates every frame based on player position
/// - Calls UpdateBiodiversityScores() when settings change
///
/// SOURCE:
/// - Unity UI system documentation
/// - Custom implementation for biodiversity data display
///
/// AI CONTRIBUTION: ~60% - UI update logic, color coding, query methods
/// HUMAN CONTRIBUTION: ~40% - UI layout, text formatting, user controls
/// </summary>
public class BiodiversityUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text simpsonsIndexText;
    public Text observationCountText;
    public Text speciesCountText;
    public Slider diversityIntensitySlider;
    public Toggle enableBiodiversityToggle;
    public Button recalculateButton;

    [Header("User Search Elements")]
    public InputField usernameSearchInput;
    public Button searchUserButton;
    public Text searchStatusText;

    [Header("Auto-Find UI (Fallback)")]
    [Tooltip("If true, will automatically find UI elements by name if not assigned")]
    public bool autoFindUIElements = true;
    
    [Header("Display Settings")]
    public bool showBiodiversityInfo = true;
    public KeyCode toggleUIKey = KeyCode.B;
    public GameObject uiPanel;

    [Header("User Search Hotkeys")]
    [Tooltip("Key to activate/focus the username search input field")]
    public KeyCode activateSearchKey = KeyCode.U;
    [Tooltip("Allow Enter key to trigger search when input field is focused")]
    public bool searchOnEnter = true;
    
    private BiodiversityScoreManager biodiversityManager;
    private Transform playerTransform;
    private bool uiVisible = true;
    private INaturalistMapController mapController;
    
    void Start()
    {
        // Find components
        biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
        mapController = FindObjectOfType<INaturalistMapController>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // Auto-find UI elements if not assigned
        if (autoFindUIElements)
        {
            AutoFindUIElements();
        }

        // Setup UI
        SetupUI();

        // Initial UI state
        if (uiPanel != null)
            uiPanel.SetActive(showBiodiversityInfo);
    }

    /// <summary>
    /// Automatically finds UI elements by name if they're not assigned in Inspector
    /// </summary>
    private void AutoFindUIElements()
    {
        Debug.Log("[BiodiversityUI] === AUTO-FIND UI ELEMENTS ===");

        // Find username search input
        if (usernameSearchInput == null)
        {
            GameObject inputObj = GameObject.Find("UsernameSearchInput");
            Debug.Log($"[BiodiversityUI] Searching for 'UsernameSearchInput': {(inputObj != null ? "FOUND" : "NOT FOUND")}");

            if (inputObj != null)
            {
                Debug.Log($"[BiodiversityUI] - GameObject found, active: {inputObj.activeInHierarchy}");

                // Check what components it has
                Component[] components = inputObj.GetComponents<Component>();
                Debug.Log($"[BiodiversityUI] - Components on GameObject: {string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name))}");

                usernameSearchInput = inputObj.GetComponent<InputField>();

                if (usernameSearchInput != null)
                {
                    Debug.Log($"[BiodiversityUI] ✓ InputField component found!");
                    Debug.Log($"[BiodiversityUI] - InputField enabled: {usernameSearchInput.enabled}");
                    Debug.Log($"[BiodiversityUI] - InputField interactable: {usernameSearchInput.interactable}");
                }
                else
                {
                    Debug.LogError("[BiodiversityUI] ✗ ✗ ✗ CRITICAL ERROR ✗ ✗ ✗");
                    Debug.LogError($"[BiodiversityUI] GameObject 'UsernameSearchInput' exists but has NO InputField component!");
                    Debug.LogError("[BiodiversityUI] ");
                    Debug.LogError("[BiodiversityUI] FIX THIS:");
                    Debug.LogError("[BiodiversityUI] 1. Select 'UsernameSearchInput' in Hierarchy");
                    Debug.LogError("[BiodiversityUI] 2. In Inspector, click 'Add Component'");
                    Debug.LogError("[BiodiversityUI] 3. Search for 'Input Field' and add it");
                    Debug.LogError("[BiodiversityUI] ");
                    Debug.LogError("[BiodiversityUI] OR delete it and create: UI → Input Field");
                }
            }
            else
            {
                Debug.LogWarning("[BiodiversityUI] ✗ UsernameSearchInput GameObject not found in scene!");
            }
        }
        else
        {
            Debug.Log($"[BiodiversityUI] UsernameSearchInput already assigned: {usernameSearchInput.name}");
        }

        // Find search button
        if (searchUserButton == null)
        {
            GameObject buttonObj = GameObject.Find("SearchUserButton");
            Debug.Log($"[BiodiversityUI] Searching for 'SearchUserButton': {(buttonObj != null ? "FOUND" : "NOT FOUND")}");

            if (buttonObj != null)
            {
                Debug.Log($"[BiodiversityUI] - GameObject found, active: {buttonObj.activeInHierarchy}");
                searchUserButton = buttonObj.GetComponent<Button>();

                if (searchUserButton != null)
                {
                    Debug.Log($"[BiodiversityUI] ✓ Button component found!");
                    Debug.Log($"[BiodiversityUI] - Button interactable: {searchUserButton.interactable}");
                }
                else
                {
                    Debug.LogError("[BiodiversityUI] ✗ GameObject found but NO Button component!");
                }
            }
            else
            {
                Debug.LogWarning("[BiodiversityUI] ✗ SearchUserButton GameObject not found in scene!");
            }
        }
        else
        {
            Debug.Log($"[BiodiversityUI] SearchUserButton already assigned: {searchUserButton.name}");
        }

        // Find search status text
        if (searchStatusText == null)
        {
            GameObject textObj = GameObject.Find("SearchStatusText");
            Debug.Log($"[BiodiversityUI] Searching for 'SearchStatusText': {(textObj != null ? "FOUND" : "NOT FOUND")}");

            if (textObj != null)
            {
                Debug.Log($"[BiodiversityUI] - GameObject found, active: {textObj.activeInHierarchy}");
                searchStatusText = textObj.GetComponent<Text>();

                if (searchStatusText != null)
                {
                    Debug.Log($"[BiodiversityUI] ✓ Text component found!");
                }
                else
                {
                    Debug.LogError("[BiodiversityUI] ✗ GameObject found but NO Text component!");
                }
            }
            else
            {
                Debug.LogWarning("[BiodiversityUI] ✗ SearchStatusText GameObject not found in scene!");
            }
        }
        else
        {
            Debug.Log($"[BiodiversityUI] SearchStatusText already assigned: {searchStatusText.name}");
        }

        Debug.Log("[BiodiversityUI] === AUTO-FIND COMPLETE ===");
    }
    
    void Update()
    {
        // Toggle UI visibility
        if (Input.GetKeyDown(toggleUIKey))
        {
            ToggleUI();
        }

        // Activate search input with U key
        if (Input.GetKeyDown(activateSearchKey))
        {
            Debug.Log($"[BiodiversityUI] U key pressed! Attempting to activate search input...");
            ActivateSearchInput();
        }

        // Trigger search on Enter key (when input field is active)
        if (searchOnEnter && Input.GetKeyDown(KeyCode.Return) && usernameSearchInput != null)
        {
            // Check if the input field is currently focused
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == usernameSearchInput.gameObject)
            {
                OnSearchUserPressed();
            }
        }

        // Deactivate input field on Escape key
        if (Input.GetKeyDown(KeyCode.Escape) && usernameSearchInput != null)
        {
            // Check if the input field is currently focused
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == usernameSearchInput.gameObject)
            {
                DeactivateSearchInput();
            }
        }

        // Debug key: Press D to check UI status
        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugUIStatus();
        }

        // Update biodiversity information
        if (uiVisible && showBiodiversityInfo && biodiversityManager != null && playerTransform != null)
        {
            UpdateBiodiversityDisplay();
        }
    }
    
    private void SetupUI()
    {
        // Setup slider
        if (diversityIntensitySlider != null)
        {
            diversityIntensitySlider.minValue = 0f;
            diversityIntensitySlider.maxValue = 2f;
            diversityIntensitySlider.value = 1f;
            diversityIntensitySlider.onValueChanged.AddListener(OnDiversityIntensityChanged);
        }
        
        // Setup toggle
        if (enableBiodiversityToggle != null)
        {
            enableBiodiversityToggle.isOn = true;
            enableBiodiversityToggle.onValueChanged.AddListener(OnBiodiversityToggled);
        }
        
        // Setup button
        if (recalculateButton != null)
        {
            recalculateButton.onClick.AddListener(OnRecalculatePressed);
        }

        // Setup user search
        if (searchUserButton != null)
        {
            searchUserButton.onClick.AddListener(OnSearchUserPressed);
        }

        // Setup default text
        if (simpsonsIndexText != null)
            simpsonsIndexText.text = "Simpson's Index: Calculating...";

        if (observationCountText != null)
            observationCountText.text = "Observations: 0";

        if (speciesCountText != null)
            speciesCountText.text = "Species: 0";

        if (searchStatusText != null)
            searchStatusText.text = "";
    }
    
    private void UpdateBiodiversityDisplay()
    {
        Vector3 playerPos = playerTransform.position;
        
        // Get Simpson's diversity data at player position
        float simpsonsIndex = biodiversityManager.GetBiodiversityAtPosition(playerPos);
        int observationCount = biodiversityManager.GetObservationCountAtPosition(playerPos);
        int speciesCount = biodiversityManager.GetSpeciesCountAtPosition(playerPos);
        
        // Update text displays
        if (simpsonsIndexText != null)
        {
            string indexText = $"Simpson's Index: {simpsonsIndex:F3}";
            if (simpsonsIndex > 0.8f)
                indexText += " (Very High Diversity)";
            else if (simpsonsIndex > 0.6f)
                indexText += " (High Diversity)";
            else if (simpsonsIndex > 0.3f)
                indexText += " (Moderate Diversity)";
            else if (simpsonsIndex > 0f)
                indexText += " (Low Diversity)";
            else
                indexText += " (No Data)";
                
            simpsonsIndexText.text = indexText;
        }
        
        if (observationCountText != null)
        {
            observationCountText.text = $"Total Observations: {observationCount}";
        }
        
        if (speciesCountText != null)
        {
            speciesCountText.text = $"Unique Species: {speciesCount}";
        }
    }
    
    private void OnDiversityIntensityChanged(float value)
    {
        // Apply global intensity to diversity visualization
        Shader.SetGlobalFloat("_DiversityIntensity", value);
        
        if (biodiversityManager != null)
        {
            // Force immediate update
            biodiversityManager.UpdateBiodiversityScores();
        }
    }
    
    private void OnBiodiversityToggled(bool enabled)
    {
        if (biodiversityManager != null)
        {
            biodiversityManager.enabled = enabled;
        }
        
        // Set global shader property to disable/enable effects
        Shader.SetGlobalFloat("_GlobalSaturation", enabled ? 1f : 1f);
    }
    
    private void OnRecalculatePressed()
    {
        if (biodiversityManager != null)
        {
            biodiversityManager.UpdateBiodiversityScores();
            Debug.Log("Biodiversity scores recalculated manually");
        }
    }
    
    public void ToggleUI()
    {
        uiVisible = !uiVisible;
        
        if (uiPanel != null)
            uiPanel.SetActive(uiVisible && showBiodiversityInfo);
    }
    
    public void SetUIVisibility(bool visible)
    {
        uiVisible = visible;

        if (uiPanel != null)
            uiPanel.SetActive(visible && showBiodiversityInfo);
    }

    /// <summary>
    /// Activates and focuses the username search input field
    /// </summary>
    public void ActivateSearchInput()
    {
        Debug.Log("[BiodiversityUI] === ACTIVATE SEARCH INPUT DEBUG ===");

        if (usernameSearchInput == null)
        {
            Debug.LogError("[BiodiversityUI] ✗ Cannot activate - usernameSearchInput is NULL!");
            Debug.LogError("[BiodiversityUI] Make sure UI elements are created with correct names:");
            Debug.LogError("[BiodiversityUI] - UsernameSearchInput (InputField)");
            Debug.LogError("[BiodiversityUI] - SearchUserButton (Button)");
            Debug.LogError("[BiodiversityUI] - SearchStatusText (Text)");
            return;
        }

        Debug.Log($"[BiodiversityUI] ✓ usernameSearchInput found: {usernameSearchInput.name}");
        Debug.Log($"[BiodiversityUI] - GameObject active: {usernameSearchInput.gameObject.activeInHierarchy}");
        Debug.Log($"[BiodiversityUI] - Component enabled: {usernameSearchInput.enabled}");
        Debug.Log($"[BiodiversityUI] - Interactable: {usernameSearchInput.interactable}");

        // Make sure the UI panel is visible
        if (uiPanel != null)
        {
            Debug.Log($"[BiodiversityUI] UI Panel: {uiPanel.name}, active: {uiPanel.activeSelf}");
            if (!uiPanel.activeSelf)
            {
                Debug.Log("[BiodiversityUI] Activating UI Panel...");
                uiPanel.SetActive(true);
                uiVisible = true;
            }
        }
        else
        {
            Debug.LogWarning("[BiodiversityUI] uiPanel is null (might be ok if not using a panel)");
        }

        // Check EventSystem
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError("[BiodiversityUI] ✗ NO EventSystem found! UI interaction requires EventSystem!");
            Debug.LogError("[BiodiversityUI] Add an EventSystem: GameObject → UI → Event System");
            return;
        }
        else
        {
            Debug.Log($"[BiodiversityUI] ✓ EventSystem found: {UnityEngine.EventSystems.EventSystem.current.name}");
        }

        // Activate and focus the input field
        Debug.Log("[BiodiversityUI] Calling ActivateInputField()...");
        usernameSearchInput.ActivateInputField();

        Debug.Log("[BiodiversityUI] Calling Select()...");
        usernameSearchInput.Select();

        // Check if it worked
        if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == usernameSearchInput.gameObject)
        {
            Debug.Log("[BiodiversityUI] ✓ SUCCESS! Input field is now selected and active!");
        }
        else
        {
            Debug.LogWarning($"[BiodiversityUI] ⚠ Input field activated but EventSystem selected: {UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.name ?? "null"}");
        }

        // Clear any existing error messages
        if (searchStatusText != null && searchStatusText.text.Contains("Error"))
        {
            searchStatusText.text = "";
        }

        Debug.Log("[BiodiversityUI] Username search input activation complete (press Enter to search, Escape to cancel)");
        Debug.Log("[BiodiversityUI] === ACTIVATION DEBUG COMPLETE ===");
    }

    /// <summary>
    /// Deactivates and unfocuses the username search input field
    /// </summary>
    public void DeactivateSearchInput()
    {
        if (usernameSearchInput == null)
            return;

        // Deactivate the input field
        usernameSearchInput.DeactivateInputField();

        // Clear the current selection
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        Debug.Log("[BiodiversityUI] Username search input deactivated");
    }

    /// <summary>
    /// Debug method to check current UI status - Press D key to run
    /// </summary>
    [ContextMenu("Debug UI Status")]
    public void DebugUIStatus()
    {
        Debug.Log("========================================");
        Debug.Log("[BiodiversityUI] === UI STATUS DEBUG ===");
        Debug.Log("========================================");

        // Check input field
        if (usernameSearchInput != null)
        {
            Debug.Log($"✓ usernameSearchInput: ASSIGNED");
            Debug.Log($"  - Name: {usernameSearchInput.name}");
            Debug.Log($"  - GameObject path: {GetGameObjectPath(usernameSearchInput.gameObject)}");
            Debug.Log($"  - Active in hierarchy: {usernameSearchInput.gameObject.activeInHierarchy}");
            Debug.Log($"  - Component enabled: {usernameSearchInput.enabled}");
            Debug.Log($"  - Interactable: {usernameSearchInput.interactable}");
            Debug.Log($"  - Current text: '{usernameSearchInput.text}'");
        }
        else
        {
            Debug.LogError("✗ usernameSearchInput: NULL");
        }

        // Check button
        if (searchUserButton != null)
        {
            Debug.Log($"✓ searchUserButton: ASSIGNED");
            Debug.Log($"  - Name: {searchUserButton.name}");
            Debug.Log($"  - Active: {searchUserButton.gameObject.activeInHierarchy}");
            Debug.Log($"  - Interactable: {searchUserButton.interactable}");
        }
        else
        {
            Debug.LogError("✗ searchUserButton: NULL");
        }

        // Check status text
        if (searchStatusText != null)
        {
            Debug.Log($"✓ searchStatusText: ASSIGNED");
            Debug.Log($"  - Name: {searchStatusText.name}");
            Debug.Log($"  - Active: {searchStatusText.gameObject.activeInHierarchy}");
            Debug.Log($"  - Current text: '{searchStatusText.text}'");
        }
        else
        {
            Debug.LogError("✗ searchStatusText: NULL");
        }

        // Check EventSystem
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            Debug.Log($"✓ EventSystem: FOUND");
            Debug.Log($"  - Name: {UnityEngine.EventSystems.EventSystem.current.name}");
            var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            Debug.Log($"  - Currently selected: {(selected != null ? selected.name : "NOTHING")}");
        }
        else
        {
            Debug.LogError("✗ EventSystem: NOT FOUND!");
            Debug.LogError("  Add one: GameObject → UI → Event System");
        }

        // Check UI Panel
        if (uiPanel != null)
        {
            Debug.Log($"✓ uiPanel: ASSIGNED");
            Debug.Log($"  - Name: {uiPanel.name}");
            Debug.Log($"  - Active: {uiPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning("⚠ uiPanel: NULL (might be intentional)");
        }

        // Check other managers
        Debug.Log($"biodiversityManager: {(biodiversityManager != null ? "✓ FOUND" : "✗ NULL")}");
        Debug.Log($"mapController: {(mapController != null ? "✓ FOUND" : "✗ NULL")}");
        Debug.Log($"playerTransform: {(playerTransform != null ? "✓ FOUND" : "✗ NULL")}");

        // Settings
        Debug.Log("--- Settings ---");
        Debug.Log($"autoFindUIElements: {autoFindUIElements}");
        Debug.Log($"activateSearchKey: {activateSearchKey}");
        Debug.Log($"searchOnEnter: {searchOnEnter}");

        Debug.Log("========================================");
        Debug.Log("[BiodiversityUI] === DEBUG COMPLETE ===");
        Debug.Log("========================================");
    }

    /// <summary>
    /// Gets the full hierarchy path of a GameObject
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void OnSearchUserPressed()
    {
        if (usernameSearchInput == null || string.IsNullOrEmpty(usernameSearchInput.text))
        {
            if (searchStatusText != null)
            {
                searchStatusText.text = "⚠️ Please enter a username";
                searchStatusText.color = new Color(1f, 0.7f, 0f); // Yellow
            }
            return;
        }

        string username = usernameSearchInput.text.Trim();
        StartCoroutine(SearchAndTeleportToUser(username));
    }

    private IEnumerator SearchAndTeleportToUser(string username)
    {
        // Show searching status with animation
        if (searchStatusText != null)
        {
            searchStatusText.text = $"🔍 Searching for user '{username}'...";
            searchStatusText.color = new Color(1f, 0.8f, 0.2f); // Orange/yellow
        }

        // Query iNaturalist API for user's last observation
        string apiUrl = $"https://api.inaturalist.org/v1/observations?user_login={username}&order=desc&order_by=observed_on&per_page=1";

        Debug.Log($"[BiodiversityUI] Fetching user observations: {apiUrl}");

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
            if (searchStatusText != null)
            {
                // Check if it's a 422 error (invalid username format)
                if (fetchError != null && fetchError.Contains("422"))
                {
                    searchStatusText.text = $"❌ Invalid username format. Try removing underscores or special characters";
                    searchStatusText.color = Color.red;
                    Debug.LogWarning($"[BiodiversityUI] Username '{username}' rejected by API (422). iNaturalist usernames cannot contain underscores.");
                }
                else
                {
                    searchStatusText.text = $"❌ Network error: {fetchError}";
                    searchStatusText.color = Color.red;
                    Debug.LogError($"[BiodiversityUI] Failed to fetch user data: {fetchError}");
                }
            }
            else
            {
                Debug.LogError($"[BiodiversityUI] Failed to fetch user data: {fetchError}");
            }
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
                if (searchStatusText != null)
                {
                    // Check if it's a 422 error (invalid username format)
                    if (request.error.Contains("422"))
                    {
                        searchStatusText.text = $"❌ Invalid username format. Try removing underscores or special characters";
                        searchStatusText.color = Color.red;
                        Debug.LogWarning($"[BiodiversityUI] Username '{username}' rejected by API (422). iNaturalist usernames cannot contain underscores.");
                    }
                    else
                    {
                        searchStatusText.text = $"❌ Network error: {request.error}";
                        searchStatusText.color = Color.red;
                        Debug.LogError($"[BiodiversityUI] Failed to fetch user data: {request.error}");
                    }
                }
                else
                {
                    Debug.LogError($"[BiodiversityUI] Failed to fetch user data: {request.error}");
                }
            }
        }
#endif
    }

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
                Debug.LogWarning($"[BiodiversityUI] No observations found for user: {username}");
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
                Debug.LogWarning($"[BiodiversityUI] User's last observation has no location");
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

            if (float.TryParse(coords[0], out float lat) && float.TryParse(coords[1], out float lng))
            {
                // Show success status
                string speciesName = lastObs.taxon?.preferred_common_name ?? lastObs.taxon?.name ?? "Unknown species";

                if (searchStatusText != null)
                {
                    searchStatusText.text = $"✅ Found '{username}'! Observed: {speciesName}";
                    searchStatusText.color = new Color(0.2f, 0.8f, 0.2f); // Green
                }

                // Reload the entire map at the new location
                StartCoroutine(TeleportAndReloadMap(username, lat, lng));

                Debug.Log($"[BiodiversityUI] Relocating map to {username}'s observation at {lat}, {lng}");
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
            Debug.LogError($"[BiodiversityUI] Error parsing user search response: {e.Message}");
        }
    }

    /// <summary>
    /// Teleports the player and reloads the entire map at the new location
    /// </summary>
    private IEnumerator TeleportAndReloadMap(string username, float lat, float lng)
    {
        // Show relocating status
        if (searchStatusText != null)
        {
            searchStatusText.text = $"🗺️ Relocating to '{username}'s location...";
            searchStatusText.color = new Color(0.5f, 0.7f, 1f); // Light blue
        }

        // Find the map
        Mapbox.Unity.Map.AbstractMap map = FindObjectOfType<Mapbox.Unity.Map.AbstractMap>();
        if (map == null)
        {
            Debug.LogError("[BiodiversityUI] Map not found!");
            if (searchStatusText != null)
            {
                searchStatusText.text = "❌ Error: Map not found";
                searchStatusText.color = Color.red;
            }
            yield break;
        }

        // Update the map center to the new location - this reloads all tiles
        Mapbox.Utils.Vector2d latLng = new Mapbox.Utils.Vector2d(lat, lng);
        map.UpdateMap(latLng);

        Debug.Log($"[BiodiversityUI] Map center updated to {lat}, {lng}");

        // Wait a frame for map to start updating
        yield return null;

        // Teleport player to the new location (with higher spawn offset)
        if (playerTransform != null)
        {
            // Convert lat/lng to world position
            Vector3 worldPosition = map.GeoToWorldPosition(latLng, true);
            playerTransform.position = worldPosition + Vector3.up * 50f; // Higher spawn to account for terrain loading

            Debug.Log($"[BiodiversityUI] Player teleported to world position: {worldPosition}");
        }
        else
        {
            Debug.LogWarning("[BiodiversityUI] Player transform not found for teleportation!");
        }

        // Wait for map tiles to start loading
        yield return new WaitForSeconds(1f);

        // Show loading observations status
        if (searchStatusText != null)
        {
            searchStatusText.text = $"📍 Loading observations for '{username}'...";
            searchStatusText.color = new Color(0.5f, 0.7f, 1f); // Light blue
        }

        // Load observations with user priority
        if (mapController != null)
        {
            yield return StartCoroutine(mapController.LoadObservationsWithUserPriority(username, lat, lng, 5f));

            if (searchStatusText != null)
            {
                searchStatusText.text = $"✅ Observations loaded! Showing '{username}' first";
                searchStatusText.color = new Color(0.2f, 0.8f, 0.2f); // Green
            }

            Debug.Log($"[BiodiversityUI] Map relocated and observations loaded with user {username} prioritized");

            // Clear status after a few seconds
            yield return new WaitForSeconds(5f);
            if (searchStatusText != null)
                searchStatusText.text = "";
        }
        else
        {
            Debug.LogError("[BiodiversityUI] Map controller not found!");
            if (searchStatusText != null)
            {
                searchStatusText.text = "❌ Error: Map controller not found";
                searchStatusText.color = Color.red;
            }
        }
    }
}