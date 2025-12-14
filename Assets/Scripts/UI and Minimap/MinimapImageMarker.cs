using UnityEngine;
using UnityEngine.UI;
using Mapbox.Utils;

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
        markerImage.color = Color.white; // Use white to preserve original sprite colors (no tint)
        
        // Preserve aspect ratio
        markerImage.preserveAspect = true;
        
        isInitialized = true;
    }
    
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
    
    public void SetSprite(Sprite sprite)
    {
        if (markerImage != null && sprite != null)
            markerImage.sprite = sprite;
    }
    
    public void SetColor(Color color)
    {
        if (markerImage != null)
            markerImage.color = color;
    }
    
    public string GetDebugInfo()
    {
        return $"{commonName} ({taxonCategory}) - Lat: {latLng.x:F6}, Lng: {latLng.y:F6}";
    }
}
