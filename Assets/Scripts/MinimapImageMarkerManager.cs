using System.Collections.Generic;
using UnityEngine;
using Mapbox.Utils;

/// <summary>
/// Manages image-based markers on the minimap for iNaturalist observations.
/// Uses sprites/images for proper emoji/icon display with color.
/// </summary>
public class MinimapImageMarkerManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the iNaturalist observation controller")]
    public INaturalistMapController observationController;
    
    [Tooltip("Reference to the minimap")]
    public StaticMapMinimap minimap;
    
    [Tooltip("Container for all minimap markers (should be child of minimap canvas)")]
    public RectTransform markerContainer;
    
    [Header("Marker Settings")]
    [Tooltip("Size of markers in pixels")]
    [Range(16f, 64f)]
    public float markerSize = 32f;
    
    [Tooltip("Update marker positions every frame (disable to save performance)")]
    public bool updateEveryFrame = true;
    
    [Header("Icon Mapping")]
    [Tooltip("Map taxon categories to sprites")]
    public List<TaxonIconMapping> iconMappings = new List<TaxonIconMapping>();
    
    [Tooltip("Default icon if no mapping found")]
    public Sprite defaultIcon;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Private variables
    private Dictionary<int, MinimapImageMarker> activeMarkers = new Dictionary<int, MinimapImageMarker>();
    private Dictionary<string, TaxonIconData> iconMap;
    private GameObject markerPrefab;
    
    void Start()
    {
        Debug.Log("[MinimapImageMarkerManager] Starting initialization...");
        
        // Validate references
        if (observationController == null)
        {
            observationController = FindObjectOfType<INaturalistMapController>();
            if (observationController == null)
            {
                Debug.LogError("[MinimapImageMarkerManager] INaturalistMapController not found!");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"[MinimapImageMarkerManager] Found INaturalistMapController: {observationController.gameObject.name}");
            }
        }
        
        if (minimap == null)
        {
            minimap = FindObjectOfType<StaticMapMinimap>();
            if (minimap == null)
            {
                Debug.LogError("[MinimapImageMarkerManager] StaticMapMinimap not found!");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"[MinimapImageMarkerManager] Found StaticMapMinimap: {minimap.gameObject.name}");
            }
        }
        
        // Create marker container if not assigned
        if (markerContainer == null)
        {
            GameObject containerObj = new GameObject("ObservationMarkers");
            markerContainer = containerObj.AddComponent<RectTransform>();
            
            if (minimap.minimapImage != null)
            {
                markerContainer.SetParent(minimap.minimapImage.transform, false);
            }
            else
            {
                markerContainer.SetParent(minimap.transform, false);
            }
            
            markerContainer.anchorMin = Vector2.zero;
            markerContainer.anchorMax = Vector2.one;
            markerContainer.offsetMin = Vector2.zero;
            markerContainer.offsetMax = Vector2.zero;
        }
        
        // Create marker prefab
        markerPrefab = CreateMarkerPrefab();
        
        // Initialize icon mapping
        InitializeIconMap();
        
        // Start updating markers
        StartCoroutine(UpdateMarkersCoroutine());
        
        Debug.Log("[MinimapImageMarkerManager] Initialization complete!");
    }
    
    void Update()
    {
        if (updateEveryFrame && minimap != null && minimap.minimapImage != null)
        {
            UpdateAllMarkerPositions();
        }
    }
    
    /// <summary>
    /// Initialize the icon mapping dictionary
    /// </summary>
    private void InitializeIconMap()
    {
        iconMap = new Dictionary<string, TaxonIconData>();
        
        // Default color scheme
        Dictionary<string, Color> defaultColors = new Dictionary<string, Color>()
        {
            { "Fungi", new Color(0.6f, 0.4f, 0.2f) },        // Brown
            { "Plantae", new Color(0.2f, 0.8f, 0.3f) },      // Green
            { "Animalia", new Color(0.9f, 0.6f, 0.2f) },     // Orange
            { "Insecta", new Color(1f, 0.8f, 0.2f) },        // Yellow
            { "Arachnida", new Color(0.5f, 0.3f, 0.2f) },    // Dark brown
            { "Aves", new Color(0.3f, 0.6f, 1f) },           // Blue
            { "Mammalia", new Color(0.9f, 0.5f, 0.3f) },     // Orange-red
            { "Reptilia", new Color(0.4f, 0.7f, 0.3f) },     // Light green
            { "Amphibia", new Color(0.3f, 0.8f, 0.6f) },     // Teal
            { "Actinopterygii", new Color(0.2f, 0.5f, 0.9f) }, // Blue
            { "Mollusca", new Color(0.8f, 0.6f, 0.7f) },     // Pink
            { "Unknown", Color.gray }
        };
        
        // Apply custom mappings from inspector
        int mappingsAdded = 0;
        foreach (var mapping in iconMappings)
        {
            if (!string.IsNullOrEmpty(mapping.taxonName))
            {
                Color color = mapping.useCustomColor ? mapping.customColor : 
                              (defaultColors.ContainsKey(mapping.taxonName) ? defaultColors[mapping.taxonName] : Color.white);
                
                iconMap[mapping.taxonName] = new TaxonIconData
                {
                    sprite = mapping.icon,
                    color = color
                };
                
                if (showDebugInfo)
                {
                    Debug.Log($"[MinimapImageMarkerManager] Added mapping: '{mapping.taxonName}' -> Sprite: {(mapping.icon != null ? mapping.icon.name : "NULL")}, Color: {color}");
                }
                mappingsAdded++;
            }
        }
        
        // Add defaults for any missing mappings
        foreach (var kvp in defaultColors)
        {
            if (!iconMap.ContainsKey(kvp.Key))
            {
                iconMap[kvp.Key] = new TaxonIconData
                {
                    sprite = defaultIcon,
                    color = kvp.Value
                };
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Loaded {iconMap.Count} total icon mappings ({mappingsAdded} from Inspector, {iconMap.Count - mappingsAdded} defaults)");
            Debug.Log($"[MinimapImageMarkerManager] Default icon: {(defaultIcon != null ? defaultIcon.name : "NULL")}");
        }
    }
    
    /// <summary>
    /// Get icon data for a taxon category
    /// </summary>
    private TaxonIconData GetIconDataForTaxon(string iconicTaxonName)
    {
        if (string.IsNullOrEmpty(iconicTaxonName))
        {
            if (showDebugInfo)
                Debug.LogWarning($"[MinimapImageMarkerManager] Empty taxon name, using default");
            return iconMap.GetValueOrDefault("Unknown", new TaxonIconData 
            { 
                sprite = defaultIcon, 
                color = Color.gray 
            });
        }
        
        // Try exact match
        if (iconMap.ContainsKey(iconicTaxonName))
        {
            if (showDebugInfo)
                Debug.Log($"[MinimapImageMarkerManager] Found exact match for '{iconicTaxonName}', sprite: {iconMap[iconicTaxonName].sprite?.name}");
            return iconMap[iconicTaxonName];
        }
        
        // Try partial match
        foreach (var kvp in iconMap)
        {
            if (iconicTaxonName.Contains(kvp.Key))
            {
                if (showDebugInfo)
                    Debug.Log($"[MinimapImageMarkerManager] Found partial match: '{iconicTaxonName}' contains '{kvp.Key}', sprite: {kvp.Value.sprite?.name}");
                return kvp.Value;
            }
        }
        
        // Fallback
        if (showDebugInfo)
            Debug.LogWarning($"[MinimapImageMarkerManager] No match found for '{iconicTaxonName}', using default. Available keys: {string.Join(", ", iconMap.Keys)}");
        return new TaxonIconData { sprite = defaultIcon, color = Color.white };
    }
    
    /// <summary>
    /// Coroutine that periodically checks for new/removed observations
    /// </summary>
    private System.Collections.IEnumerator UpdateMarkersCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (observationController != null)
            {
                UpdateMarkers();
            }
        }
    }
    
    /// <summary>
    /// Sync minimap markers with current observations
    /// </summary>
    public void UpdateMarkers()
    {
        if (observationController == null) return;
        
        // Access observations via reflection
        var observationsField = typeof(INaturalistMapController).GetField("observations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (observationsField == null) return;
        
        List<ObservationData> observations = observationsField.GetValue(observationController) as List<ObservationData>;
        
        if (observations == null || observations.Count == 0)
        {
            ClearAllMarkers();
            return;
        }
        
        HashSet<int> currentObservationIds = new HashSet<int>();
        
        foreach (var obs in observations)
        {
            currentObservationIds.Add(obs.id);
            
            if (!activeMarkers.ContainsKey(obs.id))
            {
                CreateMarkerForObservation(obs);
            }
        }
        
        // Remove old markers
        List<int> idsToRemove = new List<int>();
        foreach (var kvp in activeMarkers)
        {
            if (!currentObservationIds.Contains(kvp.Key))
            {
                idsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int id in idsToRemove)
        {
            RemoveMarker(id);
        }
        
        UpdateAllMarkerPositions();
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Active markers: {activeMarkers.Count}");
        }
    }
    
    /// <summary>
    /// Create a new minimap marker for an observation
    /// </summary>
    private void CreateMarkerForObservation(ObservationData obs)
    {
        if (markerPrefab == null || markerContainer == null) return;
        
        GameObject markerObj = Instantiate(markerPrefab, markerContainer);
        RectTransform markerRect = markerObj.GetComponent<RectTransform>();
        
        markerRect.sizeDelta = new Vector2(markerSize, markerSize);
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        
        MinimapImageMarker marker = markerObj.GetComponent<MinimapImageMarker>();
        
        string taxonName = obs.taxon?.iconic_taxon_name ?? "Unknown";
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Creating marker for taxon: '{taxonName}' - Common name: {obs.taxon?.preferred_common_name}");
        }
        
        TaxonIconData iconData = GetIconDataForTaxon(taxonName);
        
        // CRITICAL: If sprite is null, use default icon
        if (iconData.sprite == null && defaultIcon != null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[MinimapImageMarkerManager] Sprite is NULL for '{taxonName}', using default icon");
            }
            iconData.sprite = defaultIcon;
        }
        
        marker.Initialize(obs, minimap, iconData.sprite, iconData.color);
        
        activeMarkers[obs.id] = marker;
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapImageMarkerManager] Created marker: {marker.GetDebugInfo()}");
        }
    }
    
    /// <summary>
    /// Remove a marker by observation ID
    /// </summary>
    private void RemoveMarker(int observationId)
    {
        if (activeMarkers.ContainsKey(observationId))
        {
            if (activeMarkers[observationId] != null && activeMarkers[observationId].gameObject != null)
            {
                Destroy(activeMarkers[observationId].gameObject);
            }
            activeMarkers.Remove(observationId);
        }
    }
    
    /// <summary>
    /// Update positions of all active markers
    /// </summary>
    private void UpdateAllMarkerPositions()
    {
        if (minimap == null || minimap.minimapImage == null) return;
        
        var mapCenterField = typeof(StaticMapMinimap).GetField("mapCenterCoords", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var metersPerPixelField = typeof(StaticMapMinimap).GetField("metersPerPixel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (mapCenterField == null || metersPerPixelField == null) return;
        
        Vector2d mapCenter = (Vector2d)mapCenterField.GetValue(minimap);
        float metersPerPixel = (float)metersPerPixelField.GetValue(minimap);
        
        if (metersPerPixel == 0) return;
        
        RectTransform mapRect = minimap.minimapImage.GetComponent<RectTransform>();
        
        foreach (var kvp in activeMarkers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdatePosition(mapCenter, metersPerPixel, mapRect);
            }
        }
    }
    
    /// <summary>
    /// Clear all markers
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (var kvp in activeMarkers)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        activeMarkers.Clear();
    }
    
    /// <summary>
    /// Create a basic marker prefab
    /// </summary>
    private GameObject CreateMarkerPrefab()
    {
        GameObject prefab = new GameObject("MarkerPrefab");
        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(markerSize, markerSize);
        
        prefab.AddComponent<MinimapImageMarker>();
        
        return prefab;
    }
    
    /// <summary>
    /// Force update all markers immediately
    /// </summary>
    public void ForceUpdate()
    {
        UpdateMarkers();
    }
}

/// <summary>
/// Serializable class for custom icon mappings
/// </summary>
[System.Serializable]
public class TaxonIconMapping
{
    [Tooltip("Taxon name (e.g., 'Fungi', 'Insecta', 'Aves')")]
    public string taxonName;
    
    [Tooltip("Icon sprite (optional - uses colored circle if null)")]
    public Sprite icon;
    
    [Tooltip("Use custom color instead of default")]
    public bool useCustomColor = false;
    
    [Tooltip("Custom color for this taxon")]
    public Color customColor = Color.white;
}

/// <summary>
/// Internal data structure for icon information
/// </summary>
public class TaxonIconData
{
    public Sprite sprite;
    public Color color;
}
