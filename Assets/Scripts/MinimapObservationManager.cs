using System.Collections.Generic;
using UnityEngine;
using Mapbox.Utils;
using TMPro;

/// <summary>
/// Manages emoji markers on the minimap for iNaturalist observations.
/// Links INaturalistMapController with StaticMapMinimap to display observation locations.
/// </summary>
public class MinimapObservationManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the iNaturalist observation controller")]
    public INaturalistMapController observationController;
    
    [Tooltip("Reference to the minimap")]
    public StaticMapMinimap minimap;
    
    [Tooltip("Container for all minimap markers (should be child of minimap canvas)")]
    public RectTransform markerContainer;
    
    [Header("Marker Prefab")]
    [Tooltip("Prefab for minimap markers (should have RectTransform + MinimapObservationMarker component)")]
    public GameObject markerPrefab;
    
    [Header("Visual Settings")]
    [Tooltip("Size of emoji markers")]
    [Range(12f, 48f)]
    public float markerSize = 24f;
    
    [Tooltip("TextMeshPro font that supports emoji (REQUIRED for emoji display)")]
    public TMP_FontAsset emojiFont;
    
    [Tooltip("Update marker positions every frame (disable to save performance)")]
    public bool updateEveryFrame = true;
    
    [Header("Emoji Mapping")]
    [Tooltip("Show custom emoji mapping in inspector for easy editing")]
    public List<TaxonEmojiMapping> customMappings = new List<TaxonEmojiMapping>();
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Private variables
    private Dictionary<int, MinimapObservationMarker> activeMarkers = new Dictionary<int, MinimapObservationMarker>();
    private Dictionary<string, string> emojiMap;
    
    void Start()
    {
        // Validate references
        if (observationController == null)
        {
            observationController = FindObjectOfType<INaturalistMapController>();
            if (observationController == null)
            {
                Debug.LogError("[MinimapObservationManager] INaturalistMapController not found!");
                enabled = false;
                return;
            }
        }
        
        if (minimap == null)
        {
            minimap = FindObjectOfType<StaticMapMinimap>();
            if (minimap == null)
            {
                Debug.LogError("[MinimapObservationManager] StaticMapMinimap not found!");
                enabled = false;
                return;
            }
        }
        
        // Create marker container if not assigned
        if (markerContainer == null)
        {
            GameObject containerObj = new GameObject("ObservationMarkers");
            markerContainer = containerObj.AddComponent<RectTransform>();
            
            // Parent to minimap image
            if (minimap.minimapImage != null)
            {
                markerContainer.SetParent(minimap.minimapImage.transform, false);
            }
            else
            {
                Debug.LogWarning("[MinimapObservationManager] Minimap image not found, marker container may not be positioned correctly");
                markerContainer.SetParent(minimap.transform, false);
            }
            
            // Configure RectTransform to fill parent
            markerContainer.anchorMin = Vector2.zero;
            markerContainer.anchorMax = Vector2.one;
            markerContainer.offsetMin = Vector2.zero;
            markerContainer.offsetMax = Vector2.zero;
        }
        
        // Create default marker prefab if not assigned
        if (markerPrefab == null)
        {
            markerPrefab = CreateDefaultMarkerPrefab();
        }
        
        // Initialize emoji mapping
        InitializeEmojiMap();
        
        // Start updating markers
        StartCoroutine(UpdateMarkersCoroutine());
    }
    
    void Update()
    {
        if (updateEveryFrame && minimap != null && minimap.minimapImage != null)
        {
            UpdateAllMarkerPositions();
        }
    }
    
    /// <summary>
    /// Initialize the emoji mapping dictionary
    /// </summary>
    private void InitializeEmojiMap()
    {
        emojiMap = new Dictionary<string, string>()
        {
            // Main kingdoms
            { "Fungi", "🍄" },
            { "Plantae", "🌿" },
            { "Animalia", "🐾" },
            { "Chromista", "🦠" },
            { "Protozoa", "🦠" },
            
            // More specific animal categories
            { "Insecta", "🪲" },
            { "Arachnida", "🕷️" },
            { "Aves", "🐦" },
            { "Mammalia", "🦊" },
            { "Reptilia", "🦎" },
            { "Amphibia", "🐸" },
            { "Actinopterygii", "🐟" }, // Ray-finned fish
            { "Mollusca", "🐌" },
            
            // Plant subcategories
            { "Tracheophyta", "🌳" }, // Vascular plants (trees, ferns)
            { "Bryophyta", "🌱" }, // Mosses
            { "Magnoliopsida", "🌸" }, // Flowering plants
            { "Liliopsida", "🌾" }, // Monocots (grasses, etc.)
            { "Pinopsida", "🌲" }, // Conifers
            
            // Fallback
            { "Unknown", "📍" },
            { "Default", "🔵" }
        };
        
        // Apply custom mappings (overrides defaults)
        foreach (var mapping in customMappings)
        {
            if (!string.IsNullOrEmpty(mapping.taxonName) && !string.IsNullOrEmpty(mapping.emoji))
            {
                emojiMap[mapping.taxonName] = mapping.emoji;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapObservationManager] Loaded {emojiMap.Count} emoji mappings");
        }
    }
    
    /// <summary>
    /// Get emoji for a taxon category, with fallback hierarchy
    /// </summary>
    private string GetEmojiForTaxon(string iconicTaxonName)
    {
        if (string.IsNullOrEmpty(iconicTaxonName))
            return emojiMap.GetValueOrDefault("Unknown", "📍");
        
        // Try exact match first
        if (emojiMap.ContainsKey(iconicTaxonName))
            return emojiMap[iconicTaxonName];
        
        // Try partial matches for subcategories
        foreach (var kvp in emojiMap)
        {
            if (iconicTaxonName.Contains(kvp.Key))
                return kvp.Value;
        }
        
        // Fallback to default
        return emojiMap.GetValueOrDefault("Default", "🔵");
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
    /// Sync minimap markers with current observations from INaturalistMapController
    /// </summary>
    public void UpdateMarkers()
    {
        if (observationController == null) return;
        
        // Access observations via reflection (since it's private)
        var observationsField = typeof(INaturalistMapController).GetField("observations", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (observationsField == null)
        {
            Debug.LogError("[MinimapObservationManager] Could not access observations field");
            return;
        }
        
        List<ObservationData> observations = observationsField.GetValue(observationController) as List<ObservationData>;
        
        if (observations == null || observations.Count == 0)
        {
            // Clear all markers if no observations
            ClearAllMarkers();
            return;
        }
        
        // Track which observations are current
        HashSet<int> currentObservationIds = new HashSet<int>();
        
        // Add or update markers for each observation
        foreach (var obs in observations)
        {
            currentObservationIds.Add(obs.id);
            
            if (activeMarkers.ContainsKey(obs.id))
            {
                // Marker already exists, just update position
                continue;
            }
            else
            {
                // Create new marker
                CreateMarkerForObservation(obs);
            }
        }
        
        // Remove markers for observations that no longer exist
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
        
        // Update positions for all markers
        UpdateAllMarkerPositions();
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapObservationManager] Active markers: {activeMarkers.Count}");
        }
    }
    
    /// <summary>
    /// Create a new minimap marker for an observation
    /// </summary>
    private void CreateMarkerForObservation(ObservationData obs)
    {
        if (markerPrefab == null || markerContainer == null) return;
        
        // Instantiate marker
        GameObject markerObj = Instantiate(markerPrefab, markerContainer);
        RectTransform markerRect = markerObj.GetComponent<RectTransform>();
        
        // Set size
        markerRect.sizeDelta = new Vector2(markerSize, markerSize);
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Initialize marker component
        MinimapObservationMarker marker = markerObj.GetComponent<MinimapObservationMarker>();
        if (marker == null)
            marker = markerObj.AddComponent<MinimapObservationMarker>();
        
        string emoji = GetEmojiForTaxon(obs.taxon?.iconic_taxon_name);
        marker.fontSize = markerSize;
        marker.Initialize(obs, minimap, emoji);
        
        // Apply emoji font if available
        if (emojiFont != null)
        {
            marker.SetFont(emojiFont);
        }
        
        // Store reference
        activeMarkers[obs.id] = marker;
        
        if (showDebugInfo)
        {
            Debug.Log($"[MinimapObservationManager] Created marker: {marker.GetDebugInfo()}");
        }
    }
    
    /// <summary>
    /// Remove a marker by observation ID
    /// </summary>
    private void RemoveMarker(int observationId)
    {
        if (activeMarkers.ContainsKey(observationId))
        {
            MinimapObservationMarker marker = activeMarkers[observationId];
            if (marker != null && marker.gameObject != null)
            {
                Destroy(marker.gameObject);
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
        
        // Get current map state from minimap (using reflection to access private fields)
        var mapCenterField = typeof(StaticMapMinimap).GetField("mapCenterCoords", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var metersPerPixelField = typeof(StaticMapMinimap).GetField("metersPerPixel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (mapCenterField == null || metersPerPixelField == null) return;
        
        Vector2d mapCenter = (Vector2d)mapCenterField.GetValue(minimap);
        float metersPerPixel = (float)metersPerPixelField.GetValue(minimap);
        
        if (metersPerPixel == 0) return; // Map not initialized yet
        
        RectTransform mapRect = minimap.minimapImage.GetComponent<RectTransform>();
        
        // Update each marker
        foreach (var kvp in activeMarkers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdatePosition(mapCenter, metersPerPixel, mapRect);
            }
        }
    }
    
    /// <summary>
    /// Clear all markers from the minimap
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
    /// Create a default marker prefab at runtime
    /// </summary>
    private GameObject CreateDefaultMarkerPrefab()
    {
        GameObject prefab = new GameObject("MarkerPrefab");
        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(markerSize, markerSize);
        
        MinimapObservationMarker marker = prefab.AddComponent<MinimapObservationMarker>();
        
        // Note: This prefab will be instantiated, not used directly
        return prefab;
    }
    
    /// <summary>
    /// Public method to force update all markers immediately
    /// </summary>
    public void ForceUpdate()
    {
        UpdateMarkers();
    }
}

/// <summary>
/// Serializable class for custom emoji mappings in inspector
/// </summary>
[System.Serializable]
public class TaxonEmojiMapping
{
    [Tooltip("Taxon name (e.g., 'Fungi', 'Insecta', 'Aves')")]
    public string taxonName;
    
    [Tooltip("Emoji to display (copy-paste emoji character)")]
    public string emoji;
}
