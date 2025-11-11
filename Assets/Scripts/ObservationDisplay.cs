using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Displays observation data with a small centered world-space canvas
/// </summary>
public class ObservationDisplay : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Canvas infoCanvas;
    [SerializeField] private Text commonNameText;
    [SerializeField] private Text scientificNameText;
    [SerializeField] private RawImage photoImage;
    
    [Header("Canvas Settings")]
    [SerializeField] private Vector3 canvasOffset = new Vector3(0, 2f, 0);
    [SerializeField] private float canvasSize = 0.5f;
    
    private ObservationData observationData;
    private Camera mainCamera;
    private bool isInitialized = false;
    
    void Awake()
    {
        // Create canvas in Awake so it exists before Start
        mainCamera = Camera.main;
        
        // If no canvas assigned, create one automatically
        if (infoCanvas == null)
        {
            Debug.Log($"ObservationDisplay.Awake: Creating canvas on {gameObject.name}");
            CreateCanvasAutomatically();
        }
        
        // Ensure canvas starts hidden immediately
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
            Debug.Log($"ObservationDisplay.Awake: Canvas created and hidden on {gameObject.name}");
        }
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
    
    /// <summary>
    /// Initialize with observation data and load photo
    /// </summary>
    public void Initialize(ObservationData data)
    {
        observationData = data;
        
        Debug.Log($"ObservationDisplay.Initialize called on {gameObject.name}");
        Debug.Log($"  Data: {(data != null ? "Valid" : "NULL")}");
        if (data != null)
        {
            Debug.Log($"  Taxon: {data.taxon?.preferred_common_name ?? data.taxon?.name ?? "No taxon"}");
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
        
        // Load photo
        if (photoImage != null && data.photos != null && data.photos.Length > 0)
        {
            Debug.Log($"  Loading photo: {data.photos[0].url}");
            StartCoroutine(LoadPhoto(data.photos[0].url));
        }
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
    
    /// <summary>
    /// Show the info canvas
    /// </summary>
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
    
    /// <summary>
    /// Hide the info canvas
    /// </summary>
    public void HideCanvas()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
            Debug.Log($"ObservationDisplay.HideCanvas called on {gameObject.name} - Canvas now hidden");
        }
    }
    
    public ObservationData GetData() => observationData;
    
    public bool IsCanvasVisible() => infoCanvas != null && infoCanvas.gameObject.activeSelf;
}
