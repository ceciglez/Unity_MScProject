using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Manages a floating tooltip UI that displays observation details on hover
/// </summary>
public class ObservationTooltip : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas tooltipCanvas;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private RawImage photoImage;
    [SerializeField] private TMP_Text commonNameText;
    [SerializeField] private TMP_Text scientificNameText;
    [SerializeField] private TMP_Text observedDateText;
    [SerializeField] private TMP_Text observerText;
    [SerializeField] private TMP_Text locationText;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15, -10);
    [SerializeField] private float fadeSpeed = 5f;
    
    private CanvasGroup canvasGroup;
    private ObservationData currentObservation;
    private bool isVisible = false;
    
    void Awake()
    {
        if (tooltipCanvas == null)
        {
            tooltipCanvas = GetComponentInParent<Canvas>();
        }
        
        canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = tooltipPanel.gameObject.AddComponent<CanvasGroup>();
        }
        
        Hide();
    }
    
    void Update()
    {
        if (isVisible)
        {
            UpdatePosition();
            
            // Fade in
            if (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Fade out
            if (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// Show tooltip with observation data
    /// </summary>
    public void Show(ObservationData data)
    {
        if (data == null) return;
        
        currentObservation = data;
        isVisible = true;
        tooltipPanel.gameObject.SetActive(true);
        
        UpdateContent();
    }
    
    /// <summary>
    /// Hide the tooltip
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        StartCoroutine(HideAfterFade());
    }
    
    private IEnumerator HideAfterFade()
    {
        // Wait for fade out
        yield return new WaitUntil(() => canvasGroup.alpha <= 0f);
        tooltipPanel.gameObject.SetActive(false);
    }
    
    private void UpdateContent()
    {
        if (currentObservation == null) return;
        
        // Common name
        if (commonNameText != null)
        {
            string commonName = currentObservation.taxon?.preferred_common_name ?? "Unknown species";
            commonNameText.text = commonName;
        }
        
        // Scientific name
        if (scientificNameText != null)
        {
            string scientificName = currentObservation.taxon?.name ?? "";
            scientificNameText.text = scientificName;
        }
        
        // Observed date
        if (observedDateText != null)
        {
            string dateStr = "Unknown date";
            string dateSource = currentObservation.observed_on ?? currentObservation.created_at;
            
            if (!string.IsNullOrEmpty(dateSource))
            {
                try
                {
                    DateTime date = DateTime.Parse(dateSource);
                    dateStr = date.ToShortDateString();
                }
                catch { }
            }
            
            observedDateText.text = $"Observed: {dateStr}";
        }
        
        // Observer
        if (observerText != null)
        {
            string observer = currentObservation.user?.login ?? "Anonymous";
            observerText.text = $"By: {observer}";
        }
        
        // Location
        if (locationText != null && !string.IsNullOrEmpty(currentObservation.location))
        {
            string[] coords = currentObservation.location.Split(',');
            if (coords.Length == 2)
            {
                if (float.TryParse(coords[0], out float lat) && float.TryParse(coords[1], out float lng))
                {
                    locationText.text = $"Location: {lat:F4}, {lng:F4}";
                }
            }
        }
        
        // Load photo
        if (photoImage != null && currentObservation.photos != null && currentObservation.photos.Length > 0)
        {
            StartCoroutine(LoadPhoto(currentObservation.photos[0].url));
        }
    }
    
    private IEnumerator LoadPhoto(string photoUrl)
    {
        // Replace 'square' with 'medium' for better quality
        string mediumUrl = photoUrl.Replace("square", "medium");
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(mediumUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (photoImage != null)
                {
                    photoImage.texture = texture;
                }
            }
            else
            {
                Debug.LogWarning($"Failed to load tooltip photo: {request.error}");
                if (photoImage != null)
                {
                    photoImage.gameObject.SetActive(false);
                }
            }
        }
    }
    
    private void UpdatePosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        
        // Apply offset
        Vector2 targetPosition = mousePosition + offset;
        
        // Clamp to screen bounds
        float padding = 10f;
        float maxX = Screen.width - tooltipPanel.rect.width - padding;
        float maxY = Screen.height - padding;
        float minX = padding;
        float minY = tooltipPanel.rect.height + padding;
        
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        
        tooltipPanel.position = targetPosition;
    }
    
    /// <summary>
    /// Check if tooltip is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
}
