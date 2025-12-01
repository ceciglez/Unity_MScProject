using UnityEngine;
using UnityEngine.UI;
using Mapbox.Utils;

/// <summary>
/// Displays an image marker on the minimap for an iNaturalist observation.
/// Uses Unity UI Image instead of text for proper emoji/icon display.
/// </summary>
public class MinimapImageMarker : MonoBehaviour
{
    [Header("References")]
    public Image markerImage;
    public RectTransform markerTransform;
    
    [Header("Observation Data")]
    public Vector2d latLng;
    public string taxonCategory;
    public string commonName;
    public int observationId;
    
    // Internal state
    private StaticMapMinimap minimap;
    private bool isInitialized = false;
    
    /// <summary>
    /// Initialize the marker with observation data and icon sprite
    /// </summary>
    public void Initialize(ObservationData obs, StaticMapMinimap minimapRef, Sprite iconSprite, Color tintColor)
    {
        minimap = minimapRef;
        
        // Parse location
        latLng = ParseLocation(obs.location);
        taxonCategory = obs.taxon?.iconic_taxon_name ?? "Unknown";
        commonName = obs.taxon?.preferred_common_name ?? obs.taxon?.name ?? "Unknown";
        observationId = obs.id;
        
        // Get or create components
        if (markerTransform == null)
            markerTransform = GetComponent<RectTransform>();
        
        if (markerImage == null)
        {
            markerImage = GetComponent<Image>();
            if (markerImage == null)
                markerImage = gameObject.AddComponent<Image>();
        }
        
        // Set sprite and color
        if (iconSprite != null)
        {
            markerImage.sprite = iconSprite;
        }
        markerImage.color = tintColor;
        
        // Preserve aspect ratio
        markerImage.preserveAspect = true;
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Update the marker's position on the minimap based on current map center
    /// </summary>
    public void UpdatePosition(Vector2d mapCenter, float metersPerPixel, RectTransform mapImageRect)
    {
        if (!isInitialized || latLng == Vector2d.zero) return;
        
        // Calculate offset from map center in degrees
        double latDiff = latLng.x - mapCenter.x;
        double lonDiff = latLng.y - mapCenter.y;
        
        // Convert degrees to meters (approximate)
        double latMeters = latDiff * 111320.0;
        double lonMeters = lonDiff * 111320.0 * System.Math.Cos(mapCenter.x * Mathf.Deg2Rad);
        
        // Convert meters to pixels
        float xPixels = (float)lonMeters / metersPerPixel;
        float yPixels = (float)latMeters / metersPerPixel;
        
        // Apply to marker position
        markerTransform.anchoredPosition = new Vector2(xPixels, yPixels);
        
        // Hide marker if too far from visible area
        float maxDistance = Mathf.Max(mapImageRect.rect.width, mapImageRect.rect.height) * 0.6f;
        bool isVisible = Mathf.Abs(xPixels) < maxDistance && Mathf.Abs(yPixels) < maxDistance;
        gameObject.SetActive(isVisible);
    }
    
    /// <summary>
    /// Parse "lat,lng" string into Vector2d
    /// </summary>
    private Vector2d ParseLocation(string location)
    {
        if (string.IsNullOrEmpty(location)) return Vector2d.zero;
        
        string[] parts = location.Split(',');
        if (parts.Length != 2) return Vector2d.zero;
        
        if (double.TryParse(parts[0], System.Globalization.NumberStyles.Float, 
                           System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
            double.TryParse(parts[1], System.Globalization.NumberStyles.Float, 
                           System.Globalization.CultureInfo.InvariantCulture, out double lng))
        {
            return new Vector2d(lat, lng);
        }
        
        return Vector2d.zero;
    }
    
    /// <summary>
    /// Change the marker's sprite
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (markerImage != null && sprite != null)
            markerImage.sprite = sprite;
    }
    
    /// <summary>
    /// Change the marker's color tint
    /// </summary>
    public void SetColor(Color color)
    {
        if (markerImage != null)
            markerImage.color = color;
    }
    
    /// <summary>
    /// Get debug info about this marker
    /// </summary>
    public string GetDebugInfo()
    {
        return $"{commonName} ({taxonCategory}) - Lat: {latLng.x:F6}, Lng: {latLng.y:F6}";
    }
}
