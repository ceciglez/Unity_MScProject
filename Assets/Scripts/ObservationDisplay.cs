using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Component attached to each observation prefab to handle display and interaction
/// </summary>
public class ObservationDisplay : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Renderer observationRenderer;
    [SerializeField] private Material recentMaterial;
    [SerializeField] private Material normalMaterial;
    
    [Header("Pulse Animation")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.3f;
    
    [Header("UI Display (Optional)")]
    [SerializeField] private Canvas infoCanvas;
    [SerializeField] private Text commonNameText;
    [SerializeField] private Text scientificNameText;
    [SerializeField] private RawImage photoImage;
    
    private ObservationData observationData;
    private bool isRecent = false;
    private Vector3 originalScale;
    private bool isHovered = false;
    
    void Start()
    {
        originalScale = transform.localScale;
        
        if (observationRenderer == null)
        {
            observationRenderer = GetComponent<Renderer>();
        }
        
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (enablePulse && isRecent)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
    }
    
    /// <summary>
    /// Initialize the observation display with data
    /// </summary>
    public void Initialize(ObservationData data, bool recent)
    {
        observationData = data;
        isRecent = recent;
        
        // Set material based on recency
        if (observationRenderer != null)
        {
            if (isRecent && recentMaterial != null)
            {
                observationRenderer.material = recentMaterial;
            }
            else if (normalMaterial != null)
            {
                observationRenderer.material = normalMaterial;
            }
        }
        
        // Load photo if UI components are available
        if (photoImage != null && data.photos != null && data.photos.Length > 0)
        {
            StartCoroutine(LoadPhoto(data.photos[0].url));
        }
        
        // Update text fields
        UpdateInfoDisplay();
    }
    
    private void UpdateInfoDisplay()
    {
        if (observationData == null) return;
        
        if (commonNameText != null)
        {
            string commonName = observationData.taxon?.preferred_common_name ?? "Unknown species";
            commonNameText.text = commonName;
        }
        
        if (scientificNameText != null)
        {
            string scientificName = observationData.taxon?.name ?? "";
            scientificNameText.text = scientificName;
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
                Debug.LogWarning($"Failed to load photo: {request.error}");
            }
        }
    }
    
    /// <summary>
    /// Called when mouse hovers over the observation
    /// </summary>
    void OnMouseEnter()
    {
        isHovered = true;
        
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(true);
        }
        
        // Optional: Scale up on hover
        if (!isRecent)
        {
            transform.localScale = originalScale * 1.2f;
        }
    }
    
    /// <summary>
    /// Called when mouse leaves the observation
    /// </summary>
    void OnMouseExit()
    {
        isHovered = false;
        
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }
        
        // Reset scale
        if (!isRecent)
        {
            transform.localScale = originalScale;
        }
    }
    
    /// <summary>
    /// Called when observation is clicked
    /// </summary>
    void OnMouseDown()
    {
        if (observationData != null)
        {
            string url = $"https://www.inaturalist.org/observations/{observationData.id}";
            Application.OpenURL(url);
            Debug.Log($"Opening iNaturalist observation: {url}");
        }
    }
    
    /// <summary>
    /// Get formatted observation info as string
    /// </summary>
    public string GetObservationInfo()
    {
        if (observationData == null) return "No data";
        
        string commonName = observationData.taxon?.preferred_common_name ?? "Unknown";
        string scientificName = observationData.taxon?.name ?? "";
        string observer = observationData.user?.login ?? "Anonymous";
        string date = "Unknown date";
        
        if (!string.IsNullOrEmpty(observationData.observed_on))
        {
            try
            {
                DateTime obsDate = DateTime.Parse(observationData.observed_on);
                date = obsDate.ToShortDateString();
            }
            catch { }
        }
        
        return $"{commonName}\n{scientificName}\nObserved: {date}\nBy: {observer}";
    }
    
    /// <summary>
    /// Get the observation data
    /// </summary>
    public ObservationData GetData()
    {
        return observationData;
    }
    
    /// <summary>
    /// Show the info canvas UI (called by trigger or proximity systems)
    /// </summary>
    public void ShowCanvas()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide the info canvas UI
    /// </summary>
    public void HideCanvas()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Check if canvas is currently showing
    /// </summary>
    public bool IsCanvasVisible()
    {
        return infoCanvas != null && infoCanvas.gameObject.activeSelf;
    }
}
