using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mapbox.Utils;

/// <summary>
/// Displays an emoji marker on the minimap for an iNaturalist observation.
/// Automatically updates position relative to the map center.
/// </summary>
public class MinimapObservationMarker : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI emojiText;
    public RectTransform markerTransform;
    
    [Header("Observation Data")]
    public Vector2d latLng; // Geographic coordinates of the observation
    public string taxonCategory; // Plantae, Animalia, Fungi, etc.
    public string commonName;
    public int observationId;
    
    [Header("Visual Settings")]
    public float fontSize = 24f;
    public bool showShadow = true;
    
    // Internal state
    private StaticMapMinimap minimap;
    private bool isInitialized = false;
    
    /// <summary>
    /// Initialize the marker with observation data and emoji
    /// </summary>
    public void Initialize(ObservationData obs, StaticMapMinimap minimapRef, string emoji)
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
        
        if (emojiText == null)
        {
            // Create TextMeshPro component
            GameObject textObj = new GameObject("EmojiText");
            textObj.transform.SetParent(transform, false);
            
            emojiText = textObj.AddComponent<TextMeshProUGUI>();
            
            // Configure RectTransform to fill parent
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        // Set emoji text
        emojiText.text = emoji;
        emojiText.fontSize = fontSize;
        emojiText.alignment = TextAlignmentOptions.Center;
        emojiText.enableAutoSizing = false;
        
        // Set color to black for visibility
        emojiText.color = Color.black;
        
        // Enable outline for better contrast
        emojiText.outlineWidth = 0.2f;
        emojiText.outlineColor = Color.white;
        
        // CRITICAL: Load emoji-compatible font
        // Try to load system font that supports emoji (fallback chain)
        TMP_FontAsset loadedFont = LoadEmojiFontAsset();
        if (loadedFont != null)
        {
            emojiText.font = loadedFont;
        }
        else
        {
            Debug.LogWarning("[MinimapObservationMarker] No emoji font found - emojis may not display correctly. " +
                           "Assign an emoji font in MinimapObservationManager.");
        }
        
        // Optional shadow for better visibility
        if (showShadow)
        {
            // Create shadow using duplicate TextMeshPro slightly offset
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(emojiText.transform, false);
            shadowObj.transform.localPosition = new Vector3(2, -2, 0);
            
            TextMeshProUGUI shadowText = shadowObj.AddComponent<TextMeshProUGUI>();
            shadowText.text = emoji;
            shadowText.fontSize = fontSize;
            shadowText.alignment = TextAlignmentOptions.Center;
            shadowText.color = new Color(0, 0, 0, 0.8f);
            shadowText.font = emojiText.font; // Use same font
            
            RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.offsetMin = Vector2.zero;
            shadowRect.offsetMax = Vector2.zero;
            
            // Render shadow behind emoji
            shadowObj.transform.SetAsFirstSibling();
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Update the marker's position on the minimap based on current map center.
    /// Call this whenever the minimap pans or updates.
    /// </summary>
    public void UpdatePosition(Vector2d mapCenter, float metersPerPixel, RectTransform mapImageRect)
    {
        if (!isInitialized || latLng == Vector2d.zero) return;
        
        // Calculate offset from map center in degrees
        double latDiff = latLng.x - mapCenter.x;
        double lonDiff = latLng.y - mapCenter.y;
        
        // Convert degrees to meters (approximate)
        double latMeters = latDiff * 111320.0; // 1 degree latitude ≈ 111.32km
        double lonMeters = lonDiff * 111320.0 * System.Math.Cos(mapCenter.x * Mathf.Deg2Rad);
        
        // Convert meters to pixels
        float xPixels = (float)lonMeters / metersPerPixel;
        float yPixels = (float)latMeters / metersPerPixel;
        
        // Apply to marker position (relative to map image center)
        markerTransform.anchoredPosition = new Vector2(xPixels, yPixels);
        
        // Optional: Hide marker if too far from visible area
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
    /// Change the emoji displayed
    /// </summary>
    public void SetEmoji(string emoji)
    {
        if (emojiText != null)
            emojiText.text = emoji;
    }
    
    /// <summary>
    /// Get debug info about this marker
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[{emoji}] {commonName} ({taxonCategory}) - Lat: {latLng.x:F6}, Lng: {latLng.y:F6}";
    }
    
    /// <summary>
    /// Load an emoji-compatible TMP font asset
    /// </summary>
    private TMP_FontAsset LoadEmojiFontAsset()
    {
        // Try to find any existing TMP font that might support emoji
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        
        // Prefer fonts with "Emoji" in name
        foreach (var font in allFonts)
        {
            if (font.name.ToLower().Contains("emoji") || 
                font.name.ToLower().Contains("noto") ||
                font.name.ToLower().Contains("color"))
            {
                return font;
            }
        }
        
        // Try loading from Resources
        TMP_FontAsset resourceFont = Resources.Load<TMP_FontAsset>("Fonts/EmojiOne");
        if (resourceFont != null) return resourceFont;
        
        // Return any available font as last resort (will show boxes but better than crash)
        return allFonts.Length > 0 ? allFonts[0] : null;
    }
    
    /// <summary>
    /// Set a custom font for emoji rendering
    /// </summary>
    public void SetFont(TMP_FontAsset font)
    {
        if (emojiText != null && font != null)
        {
            emojiText.font = font;
        }
    }
    
    private string emoji => emojiText?.text ?? "?";
}
