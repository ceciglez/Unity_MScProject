using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages a centered screen-space UI panel that shows observation information
/// Appears when player is near an observation and hides when far away
/// </summary>
public class ObservationScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Text commonNameText;
    [SerializeField] private Text scientificNameText;
    [SerializeField] private Text observerText;
    [SerializeField] private Text dateText;
    [SerializeField] private RawImage photoImage;
    
    [Header("Display Settings")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private bool autoSetupCanvas = true;
    
    private CanvasGroup canvasGroup;
    private ObservationData currentObservation;
    private bool isVisible = false;
    private static ObservationScreenUI instance;
    
    void Awake()
    {
        // Singleton pattern - only one screen UI
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        SetupCanvas();
        
        // Start hidden
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }
    
    private void SetupCanvas()
    {
        if (autoSetupCanvas && screenCanvas == null)
        {
            // Find existing canvas or create one
            screenCanvas = GetComponentInParent<Canvas>();
            
            if (screenCanvas == null)
            {
                // Create canvas
                GameObject canvasObj = new GameObject("ObservationScreenCanvas");
                screenCanvas = canvasObj.AddComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                screenCanvas.sortingOrder = 100; // On top of other UI
                
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // Make this UI a child of canvas
                transform.SetParent(canvasObj.transform, false);
            }
        }
        
        // Get or add CanvasGroup for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Setup panel position (centered)
        if (uiPanel != null)
        {
            RectTransform rectTransform = uiPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
    }
    
    /// <summary>
    /// Show observation information
    /// </summary>
    public void ShowObservation(ObservationData data)
    {
        if (data == null) return;
        
        currentObservation = data;
        isVisible = true;
        
        // Update UI text
        UpdateUIContent();
        
        // Show panel
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }
        
        // Fade in
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(1f));
    }
    
    /// <summary>
    /// Hide the observation UI
    /// </summary>
    public void HideObservation()
    {
        isVisible = false;
        
        // Fade out
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(0f));
    }
    
    private void UpdateUIContent()
    {
        if (currentObservation == null) return;
        
        // Common name
        if (commonNameText != null)
        {
            string commonName = currentObservation.taxon?.preferred_common_name ?? "Unknown Species";
            commonNameText.text = commonName;
        }
        
        // Scientific name
        if (scientificNameText != null)
        {
            string scientificName = currentObservation.taxon?.name ?? "";
            scientificNameText.text = $"<i>{scientificName}</i>";
        }
        
        // Observer
        if (observerText != null)
        {
            string observer = currentObservation.user?.login ?? "Anonymous";
            observerText.text = $"Observed by: {observer}";
        }
        
        // Date
        if (dateText != null)
        {
            string dateStr = "Unknown date";
            if (!string.IsNullOrEmpty(currentObservation.observed_on))
            {
                try
                {
                    System.DateTime obsDate = System.DateTime.Parse(currentObservation.observed_on);
                    dateStr = obsDate.ToString("MMM dd, yyyy");
                }
                catch { }
            }
            dateText.text = $"Date: {dateStr}";
        }
        
        // Load photo
        if (photoImage != null && currentObservation.photos != null && currentObservation.photos.Length > 0)
        {
            StartCoroutine(LoadPhoto(currentObservation.photos[0].url));
        }
    }
    
    private IEnumerator LoadPhoto(string photoUrl)
    {
        if (string.IsNullOrEmpty(photoUrl) || photoImage == null) yield break;
        
        // Replace 'square' with 'medium' for better quality
        string mediumUrl = photoUrl.Replace("square", "medium");
        
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(mediumUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                if (photoImage != null)
                {
                    photoImage.texture = texture;
                }
            }
            else
            {
                Debug.LogWarning($"Failed to load photo: {request.error}");
            }
        }
    }
    
    private IEnumerator FadeCanvas(float targetAlpha)
    {
        if (canvasGroup == null) yield break;
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = 1f / fadeSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.5f;
        
        // Hide panel completely when faded out
        if (targetAlpha == 0f && uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }
    
    public bool IsVisible => isVisible;
    
    public static ObservationScreenUI Instance => instance;
}
