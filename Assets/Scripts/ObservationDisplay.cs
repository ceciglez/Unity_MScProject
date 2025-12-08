using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// Displays observation data 

public class ObservationDisplay : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Canvas infoCanvas;
    [SerializeField] private Text commonNameText;
    [SerializeField] private Text scientificNameText;
    [SerializeField] private RawImage photoImage;
    
    [Header("Canvas Prefab")]
    [Tooltip("Drag your custom-designed canvas prefab here. If assigned, this will be used instead of auto-generation.")]
    [SerializeField] private GameObject canvasPrefab;

    [Header("Interaction Prompt")]
    [Tooltip("Canvas prefab to show when player gets close. If not assigned, will show the auto-generated canvas.")]
    [SerializeField] private GameObject interactionPromptPrefab;
    
    [Header("Canvas Settings")]
    [SerializeField] private Vector3 canvasOffset = new Vector3(0, 2f, 0);
    [SerializeField] private float canvasSize = 0.5f;
    
    // [Header("Organism Colors")]
    // [SerializeField] private Color plantaeColor = new Color(0.2f, 0.8f, 0.2f); // Green for plants
    // [SerializeField] private Color animaliaColor = new Color(0.8f, 0.3f, 0.2f); // Red/orange for animals
    // [SerializeField] private Color fungiColor = new Color(0.6f, 0.4f, 0.8f); // Purple for fungi
    // [SerializeField] private Color defaultColor = new Color(0.5f, 0.5f, 0.5f); // Gray for unknown
    
    [Header("Camera")]
    [Tooltip("Assign the player camera here to avoid Camera.main ambiguity. If left empty, falls back to Camera.main.")]
    [SerializeField] private Camera playerCameraOverride;
    
    [Header("UI Display Settings")]
    [Tooltip("Enable/disable this observation's UI display")]
    [SerializeField] private bool showUI = true;
    
    private ObservationData observationData;
    private Camera mainCamera;
    private bool isInitialized = false;

    // Interaction System - SIMPLIFIED
    private GameObject promptInstance;
    private bool playerInRange = false;
    
    // UI Display Control
    private bool uiDisplayEnabled = true;
    private float displayDistance = 15f;
    private bool alwaysShow = false;
    private Transform playerTransform;
    private ObservationNetworkManager networkManager; // Add reference to network manager
    
    void Awake()
    {
        // Find camera
        mainCamera = playerCameraOverride != null ? playerCameraOverride : Camera.main;
        
        // Create interaction prompt if prefab is assigned
        if (interactionPromptPrefab != null)
        {
            promptInstance = Instantiate(interactionPromptPrefab, transform);
            promptInstance.transform.localPosition = canvasOffset + Vector3.up * 0.5f; // Slightly above main canvas
            promptInstance.transform.localScale = Vector3.one * 0.003f; // Small scale for world space
            promptInstance.SetActive(false); // Start hidden
            Debug.Log($"ObservationDisplay: Created interaction prompt from prefab on {gameObject.name}");
        }
        
        // DON'T auto-create canvas here - only create when Initialize() is called or canvas prefab is assigned
        Debug.Log($"ObservationDisplay.Awake: Component ready on {gameObject.name} - canvas creation deferred until needed");
    }
    
    void Start()
    {
        if (infoCanvas == null)
        {
            Debug.LogWarning($"ObservationDisplay.Start: No canvas on {gameObject.name}!");
            return;
        }
        
        // If canvas was manually assigned (not created in Awake), set it up
        if (!isInitialized && infoCanvas != null)
        {
            SetupCanvas();
            infoCanvas.gameObject.SetActive(false);
            Debug.Log($"ObservationDisplay.Start: Manually assigned canvas setup on {gameObject.name}");
        }
        
        // Check if UI components are assigned
        if (commonNameText == null) Debug.LogWarning($"ObservationDisplay: commonNameText not assigned on {gameObject.name}");
        if (scientificNameText == null) Debug.LogWarning($"ObservationDisplay: scientificNameText not assigned on {gameObject.name}");
        if (photoImage == null) Debug.LogWarning($"ObservationDisplay: photoImage not assigned on {gameObject.name}");
        
        // Find network manager
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<ObservationNetworkManager>();
        }
    }
    
    void Update()
    {
        // Handle E key when player is in range
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Create canvas if it doesn't exist yet
            if (infoCanvas == null)
            {
                if (canvasPrefab != null)
                {
                    Debug.Log($"ObservationDisplay.Update: Creating canvas from prefab for E-key interaction on {gameObject.name}");
                    CreateCanvasFromPrefab();
                }
                else
                {
                    Debug.Log($"ObservationDisplay.Update: Creating auto-generated canvas for E-key interaction on {gameObject.name}");
                    CreateCanvasAutomatically();
                }
                isInitialized = true;
            }
            
            ShowCanvas(); // Show the main observation info
            if (promptInstance != null)
            {
                promptInstance.SetActive(false); // Hide the prompt
            }
        }
        
        // Only check distance if UI is enabled and canvas exists
        if (uiDisplayEnabled && infoCanvas != null)
        {
            UpdateCanvasVisibility();
        }
        
        // Make canvas always face camera if it's visible
        if (infoCanvas != null && infoCanvas.gameObject.activeSelf && mainCamera != null)
        {
            infoCanvas.transform.LookAt(mainCamera.transform);
            infoCanvas.transform.Rotate(0, 180, 0); // Flip to face camera correctly
        }
        
        // Make interaction prompt face camera if it exists and is visible
        if (promptInstance != null && promptInstance.activeSelf && mainCamera != null)
        {
            promptInstance.transform.LookAt(mainCamera.transform);
            promptInstance.transform.Rotate(0, 180, 0);
        }
    }
    
    private void CreateCanvasAutomatically()
    {
        // Create canvas GameObject
        GameObject canvasObj = new GameObject("InfoCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = canvasOffset; // Position above observation
        
        infoCanvas = canvasObj.AddComponent<Canvas>();
        infoCanvas.renderMode = RenderMode.WorldSpace;
        
        // Set canvas size in world space - CRITICAL: This determines actual size
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300, 400); // Size in pixels before scaling
        
        // Scale down to world space - 0.005 = small billboard (1.5m wide, 2m tall)
        canvasObj.transform.localScale = Vector3.one * 0.005f;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        Debug.Log($"Canvas created - RenderMode: {infoCanvas.renderMode}, Size: {canvasRect.sizeDelta}, Scale: {canvasObj.transform.localScale}");
        
        // Create panel background
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create photo image
        GameObject photoObj = new GameObject("Photo");
        photoObj.transform.SetParent(panelObj.transform, false);
        
        photoImage = photoObj.AddComponent<RawImage>();
        
        RectTransform photoRect = photoObj.GetComponent<RectTransform>();
        photoRect.anchorMin = new Vector2(0.5f, 0.5f);
        photoRect.anchorMax = new Vector2(0.5f, 0.5f);
        photoRect.pivot = new Vector2(0.5f, 0.5f);
        photoRect.anchoredPosition = new Vector2(0, 60);
        photoRect.sizeDelta = new Vector2(250, 200);
        
        // Create common name text
        GameObject commonNameObj = new GameObject("CommonName");
        commonNameObj.transform.SetParent(panelObj.transform, false);
        
        commonNameText = commonNameObj.AddComponent<Text>();
        
        // Try to get LegacyRuntime font (Unity's new default built-in font)
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont != null)
        {
            commonNameText.font = defaultFont;
            Debug.Log("ObservationDisplay: LegacyRuntime font loaded successfully");
        }
        else
        {
            Debug.LogWarning("ObservationDisplay: LegacyRuntime font not available, using default");
        }
        
        commonNameText.fontSize = 24;
        commonNameText.fontStyle = FontStyle.Bold;
        commonNameText.color = Color.white;
        commonNameText.alignment = TextAnchor.MiddleCenter;
        commonNameText.text = "Test Text"; // Set test text immediately
        commonNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        commonNameText.verticalOverflow = VerticalWrapMode.Overflow;
        commonNameText.raycastTarget = false;
        
        RectTransform commonNameRect = commonNameObj.GetComponent<RectTransform>();
        commonNameRect.anchorMin = new Vector2(0.5f, 0.5f);
        commonNameRect.anchorMax = new Vector2(0.5f, 0.5f);
        commonNameRect.pivot = new Vector2(0.5f, 0.5f);
        commonNameRect.anchoredPosition = new Vector2(0, -70);
        commonNameRect.sizeDelta = new Vector2(280, 40);
        
        Debug.Log($"CommonName created - Text: '{commonNameText.text}', Font: {commonNameText.font?.name ?? "NULL"}, Color: {commonNameText.color}, Enabled: {commonNameText.enabled}");
        
        // Create scientific name text
        GameObject scientificNameObj = new GameObject("ScientificName");
        scientificNameObj.transform.SetParent(panelObj.transform, false);
        
        scientificNameText = scientificNameObj.AddComponent<Text>();
        
        if (defaultFont != null)
        {
            scientificNameText.font = defaultFont;
        }
        
        scientificNameText.fontSize = 18;
        scientificNameText.fontStyle = FontStyle.Italic;
        scientificNameText.color = Color.white;
        scientificNameText.alignment = TextAnchor.MiddleCenter;
        scientificNameText.text = "Scientific Name Test";
        scientificNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        scientificNameText.verticalOverflow = VerticalWrapMode.Overflow;
        scientificNameText.raycastTarget = false;
        
        RectTransform scientificNameRect = scientificNameObj.GetComponent<RectTransform>();
        scientificNameRect.anchorMin = new Vector2(0.5f, 0.5f);
        scientificNameRect.anchorMax = new Vector2(0.5f, 0.5f);
        scientificNameRect.pivot = new Vector2(0.5f, 0.5f);
        scientificNameRect.anchoredPosition = new Vector2(0, -100);
        scientificNameRect.sizeDelta = new Vector2(280, 25);
        
        isInitialized = true;
        Debug.Log($"ObservationDisplay: Canvas UI created on {gameObject.name}");
    }
    
    private void CreateCanvasFromPrefab()
    {
        // Instantiate the prefab
        GameObject canvasObj = Instantiate(canvasPrefab, transform);
        canvasObj.transform.localPosition = canvasOffset;
        
        // Get the canvas component
        infoCanvas = canvasObj.GetComponent<Canvas>();
        if (infoCanvas == null)
        {
            Debug.LogError($"ObservationDisplay: Canvas prefab doesn't have Canvas component! Falling back to auto-generation.");
            CreateCanvasAutomatically();
            return;
        }
        
        // Configure for world space
        infoCanvas.renderMode = RenderMode.WorldSpace;
        canvasObj.transform.localScale = Vector3.one * 0.005f; // Adjust scale as needed
        
        // Auto-find UI components by name (you can customize these names)
        commonNameText = canvasObj.GetComponentInChildren<Text>();
        if (commonNameText == null)
        {
            // Try finding by GameObject name
            Transform commonNameTransform = canvasObj.transform.Find("CommonName") ?? 
                                          canvasObj.transform.Find("Panel/CommonName");
            commonNameText = commonNameTransform?.GetComponent<Text>();
        }
        
        // Find scientific name text (look for italic or name containing "Scientific")
        Text[] allTexts = canvasObj.GetComponentsInChildren<Text>();
        foreach (Text text in allTexts)
        {
            if (text != commonNameText && 
                (text.fontStyle == FontStyle.Italic || 
                 text.gameObject.name.Contains("Scientific")))
            {
                scientificNameText = text;
                break;
            }
        }
        
        // Find photo image
        photoImage = canvasObj.GetComponentInChildren<RawImage>();
        
        // Log what we found
        Debug.Log($"ObservationDisplay: Canvas from prefab - CommonName: {commonNameText != null}, " +
                 $"ScientificName: {scientificNameText != null}, Photo: {photoImage != null}");
        isInitialized = true;
    }
    
    void LateUpdate()
    {
        // Make canvas face camera (billboard effect)
        if (infoCanvas != null && infoCanvas.gameObject.activeSelf && mainCamera != null)
        {
            // Simple billboard: look at camera
            Vector3 directionToCamera = mainCamera.transform.position - infoCanvas.transform.position;
            infoCanvas.transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }
    
    private void SetupCanvas()
    {
        // Set canvas to world space
        infoCanvas.renderMode = RenderMode.WorldSpace;
        
        // Position above observation
        infoCanvas.transform.position = transform.position + canvasOffset;
        
        // Small size
        RectTransform rectTransform = infoCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(300, 400) * canvasSize;
        }
        
        // Set scale
        infoCanvas.transform.localScale = Vector3.one * 0.01f;
    }
    

    /// Initialize with observation data and load photo

    public void Initialize(ObservationData data)
    {
        observationData = data;
        
        Debug.Log($"ObservationDisplay.Initialize called on {gameObject.name}");
        Debug.Log($"  Data: {(data != null ? "Valid" : "NULL")}");
        if (data != null)
        {
            Debug.Log($"  Taxon: {data.taxon?.preferred_common_name ?? data.taxon?.name ?? "No taxon"}");
        }
        
        // Create canvas now if it doesn't exist (only when actually needed)
        if (infoCanvas == null)
        {
            if (canvasPrefab != null)
            {
                Debug.Log($"ObservationDisplay.Initialize: Creating canvas from prefab on {gameObject.name}");
                CreateCanvasFromPrefab();
            }
            else
            {
                Debug.Log($"ObservationDisplay.Initialize: Creating auto-generated canvas on {gameObject.name}");
                CreateCanvasAutomatically();
            }
            
            // Ensure canvas starts hidden
            if (infoCanvas != null)
            {
                infoCanvas.gameObject.SetActive(false);
                isInitialized = true;
            }
        }
        
        // Apply default UI settings (can be overridden by INaturalistMapController)
        uiDisplayEnabled = true;
        displayDistance = 15f;
        alwaysShow = false;
        
        // Find player transform for distance calculations
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // Fallback: use main camera
                playerTransform = Camera.main?.transform;
            }
        }
        
        // Update text
        if (commonNameText != null)
        {
            string commonName = data.taxon?.preferred_common_name ?? "Unknown Species";
            commonNameText.text = commonName;
            commonNameText.enabled = true; // Force enable
            Debug.Log($"  Set common name: '{commonName}' - Text enabled: {commonNameText.enabled}, Color: {commonNameText.color}, Font: {commonNameText.font != null}");
        }
        else
        {
            Debug.LogWarning($"  commonNameText is NULL!");
        }
        
        if (scientificNameText != null)
        {
            string scientificName = data.taxon?.name ?? "";
            scientificNameText.text = $"{scientificName}"; // Removed italic tag to test
            scientificNameText.enabled = true; // Force enable
            Debug.Log($"  Set scientific name: '{scientificName}' - Text enabled: {scientificNameText.enabled}");
        }
        else
        {
            Debug.LogWarning($"  scientificNameText is NULL!");
        }
        
        // Apply color based on organism type
        //ApplyOrganismColor(data);
        
        // Load photo
        if (photoImage != null && data.photos != null && data.photos.Length > 0)
        {
            Debug.Log($"  Loading photo: {data.photos[0].url}");
            StartCoroutine(LoadPhoto(data.photos[0].url));
        }
        
        // CRITICAL: Don't auto-show canvas after initialization anymore
        // ShowCanvas();
        Debug.Log($"  Canvas ready but hidden - waiting for player interaction");
    }
    
    /// <summary>
    /// Called when player enters interaction range
    /// </summary>
    public void OnPlayerEnterRange()
    {
        playerInRange = true;
        
        // Trigger network connections when player approaches an observation
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<ObservationNetworkManager>();
        }
        
        if (networkManager != null && observationData != null)
        {
            Debug.Log($"[ObservationDisplay] Player approached {gameObject.name} - triggering network connections!");
            networkManager.TriggerConnectionsFromObservation(this);
        }
        else
        {
            Debug.LogWarning($"[ObservationDisplay] Cannot trigger connections from {gameObject.name} - NetworkManager: {networkManager != null}, Data: {observationData != null}");
        }
        
        // Show interaction prompt if prefab exists (preferred behavior)
        if (promptInstance != null)
        {
            promptInstance.SetActive(true);
            Debug.Log($"ObservationDisplay: Player entered range - showing custom prompt for {gameObject.name}");
        }
        else if (infoCanvas != null)
        {
            // Fallback: show main canvas if no prompt prefab but canvas exists
            ShowCanvas();
            Debug.Log($"ObservationDisplay: Player entered range - showing main canvas (no prompt prefab) for {gameObject.name}");
        }
        else
        {
            // No prompt prefab AND no canvas - do nothing (wait for E key to trigger canvas creation)
            Debug.Log($"ObservationDisplay: Player entered range - no prompt or canvas available for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Called when player exits interaction range
    /// </summary>
    public void OnPlayerExitRange()
    {
        playerInRange = false;
        
        // Hide prompt and main canvas
        if (promptInstance != null)
        {
            promptInstance.SetActive(false);
        }
        HideCanvas();
        
        Debug.Log($"ObservationDisplay: Player exited range - hiding UI for {gameObject.name}");
    }
    
    private IEnumerator LoadPhoto(string photoUrl)
    {
        if (string.IsNullOrEmpty(photoUrl)) yield break;
        
        // Use medium quality
        string mediumUrl = photoUrl.Replace("square", "medium");
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(mediumUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success && photoImage != null)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                photoImage.texture = texture;
            }
        }
    }
    
 
    /// Show the info canvas

    public void ShowCanvas()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(true);
            Debug.Log($"ObservationDisplay.ShowCanvas called on {gameObject.name} - Canvas now active");
        }
        else
        {
            Debug.LogWarning($"ObservationDisplay.ShowCanvas: No canvas to show on {gameObject.name}");
        }
    }
    

    /// Hide the info canvas

    public void HideCanvas()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
            Debug.Log($"ObservationDisplay.HideCanvas called on {gameObject.name} - Canvas now hidden");
        }
    }
    
    // /// <summary>
    // /// Apply color to observation prefab based on organism type
    // /// </summary>
    // private void ApplyOrganismColor(ObservationData data)
    // {
    //     if (data?.taxon == null) return;
        
    //     string iconicTaxon = data.taxon.iconic_taxon_name;
    //     Color organismColor = defaultColor;
        
    //     // Determine color based on iconic taxon
    //     if (!string.IsNullOrEmpty(iconicTaxon))
    //     {
    //         if (iconicTaxon.Equals("Plantae", System.StringComparison.OrdinalIgnoreCase))
    //         {
    //             organismColor = plantaeColor;
    //             Debug.Log($"Observation is PLANT - applying green color");
    //         }
    //         else if (iconicTaxon.Equals("Animalia", System.StringComparison.OrdinalIgnoreCase) ||
    //                  iconicTaxon.Equals("Aves", System.StringComparison.OrdinalIgnoreCase) ||
    //                  iconicTaxon.Equals("Mammalia", System.StringComparison.OrdinalIgnoreCase) ||
    //                  iconicTaxon.Equals("Reptilia", System.StringComparison.OrdinalIgnoreCase) ||
    //                  iconicTaxon.Equals("Amphibia", System.StringComparison.OrdinalIgnoreCase) ||
    //                  iconicTaxon.Equals("Actinopterygii", System.StringComparison.OrdinalIgnoreCase) ||
    //                  iconicTaxon.Equals("Insecta", System.StringComparison.OrdinalIgnoreCase) ||
    //                  iconicTaxon.Equals("Arachnida", System.StringComparison.OrdinalIgnoreCase))
    //         {
    //             organismColor = animaliaColor;
    //             Debug.Log($"Observation is ANIMAL ({iconicTaxon}) - applying red/orange color");
    //         }
    //         else if (iconicTaxon.Equals("Fungi", System.StringComparison.OrdinalIgnoreCase))
    //         {
    //             organismColor = fungiColor;
    //             Debug.Log($"Observation is FUNGI - applying purple color");
    //         }
    //         else
    //         {
    //             Debug.Log($"Observation type: {iconicTaxon} - applying default color");
    //         }
    //     }
        
    //     // Apply color to the mesh renderer (the observation prefab sphere/object)
    //     MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
    //     if (meshRenderer != null && meshRenderer.material != null)
    //     {
    //         meshRenderer.material.color = organismColor;
    //         Debug.Log($"Applied color {organismColor} to observation {gameObject.name}");
    //     }
    // }
    
    public ObservationData GetData() => observationData;
    
    public bool IsCanvasVisible() => infoCanvas != null && infoCanvas.gameObject.activeSelf;
    
    /// <summary>
    /// Add audio indicator emoji to the display text when bird audio is available
    /// </summary>
    public void AddAudioIndicator()
    {
        if (commonNameText != null && !commonNameText.text.Contains("🔊"))
        {
            commonNameText.text = "🔊 " + commonNameText.text;
            Debug.Log($"[ObservationDisplay] Added audio indicator to: {commonNameText.text}");
        }
    }
    
    /// <summary>
    /// Configure UI display settings for proximity-based visibility
    /// </summary>
    public void SetUIDisplaySettings(bool enabled, float distance, bool alwaysVisible)
    {
        uiDisplayEnabled = enabled;
        displayDistance = distance;
        alwaysShow = alwaysVisible;
        
        // Find player transform for distance calculations
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // Fallback: use main camera
                playerTransform = Camera.main?.transform;
            }
        }
        
        // Update canvas visibility immediately
        UpdateCanvasVisibility();
    }
    
    /// <summary>
    /// Update canvas visibility based on player proximity
    /// </summary>
    private void UpdateCanvasVisibility()
    {
        if (infoCanvas == null) return;
        
        bool shouldShow = false;
        
        // Check if UI is enabled both globally and locally
        if (uiDisplayEnabled && showUI)
        {
            if (alwaysShow)
            {
                shouldShow = true;
            }
            else if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                shouldShow = distance <= displayDistance;
            }
        }
        
        // Only change state if needed to avoid unnecessary SetActive calls
        bool isCurrentlyActive = infoCanvas.gameObject.activeSelf;
        if (shouldShow != isCurrentlyActive)
        {
            infoCanvas.gameObject.SetActive(shouldShow);
            
            if (shouldShow)
            {
                Debug.Log($"[ObservationDisplay] Showing UI for {observationData?.taxon?.preferred_common_name ?? "Unknown"}");
            }
        }
    }
}
