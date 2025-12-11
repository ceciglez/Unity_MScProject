using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns visual glow effects at biodiversity hotspots visible from distance
/// Creates glowing spheres/particles that indicate biodiverse areas from far away
///
/// FUNCTIONALITY:
/// - Spawns glow effects at each biodiversity hotspot
/// - Glow intensity based on Simpson's Diversity Index
/// - Color-coded: low diversity (gray) to high diversity (vibrant green/cyan)
/// - Visible from long distances for navigation
/// - Optional particle effects for extra visual impact
///
/// INTEGRATION:
/// - Works alongside BiodiversityVolumeSpawner
/// - Queries BiodiversityScoreManager for hotspot data
/// - Creates distant visual markers for exploration
///
/// USE CASE:
/// - Help players identify biodiverse areas from far away
/// - Create visual "beacons" that draw attention
/// - Provide navigation aids for exploration
/// - Enhance environmental storytelling
///
/// CODE LOGIC SUGGESTED BY: Claude Sonnet 4.5, Dec 2025
/// PROMPT: "Create glowing visual markers at biodiversity hotspots visible from distance"
/// SOURCE:
/// - Unity particle system and material emission
/// - HDR color for glow/bloom effects
///
/// AI CONTRIBUTION: ~90% - Hotspot spawning system, color interpolation, HDR intensity calculation
/// HUMAN CONTRIBUTION: ~10% - Glow color palette, intensity ranges, particle effects toggle
/// </summary>
public class BiodiversityGlowSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to BiodiversityScoreManager (auto-found if not set)")]
    public BiodiversityScoreManager biodiversityManager;

    [Header("Glow Settings")]
    [Tooltip("Enable glow effect visualization")]
    public bool enableGlowEffects = true;

    [Tooltip("Glow intensity multiplier")]
    [Range(0.5f, 5f)]
    public float glowIntensity = 2f;

    [Tooltip("Base glow size (meters)")]
    [Range(1f, 20f)]
    public float glowSize = 5f;

    [Tooltip("Height above ground for glow")]
    [Range(0f, 50f)]
    public float glowHeight = 2f;

    [Header("Color Settings")]
    [Tooltip("Color for low biodiversity areas")]
    public Color lowBiodiversityColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Gray

    [Tooltip("Color for high biodiversity areas")]
    public Color highBiodiversityColor = new Color(0f, 1f, 0.5f, 0.8f); // Cyan-green

    [Tooltip("Use HDR colors for bloom effect (requires Bloom post-processing)")]
    public bool useHDRColors = true;

    [Header("Update Settings")]
    [Tooltip("Update interval (seconds)")]
    [Range(1f, 10f)]
    public float updateInterval = 3f;

    [Header("Performance")]
    [Tooltip("Maximum number of glows to spawn")]
    [Range(10, 100)]
    public int maxGlows = 50;

    [Tooltip("Minimum Simpson's Index to spawn glow (filter low diversity)")]
    [Range(0f, 0.5f)]
    public float minBiodiversityThreshold = 0.2f;

    [Header("Debugging")]
    public bool enableDebugLogging = false;
    public KeyCode toggleKey = KeyCode.G;

    // Private fields
    private Dictionary<Vector2Int, GameObject> spawnedGlows = new Dictionary<Vector2Int, GameObject>();
    private GameObject glowContainer;
    private float lastUpdateTime;
    private Material glowMaterial;

    void Start()
    {
        // Find BiodiversityScoreManager
        if (biodiversityManager == null)
        {
            biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
            if (biodiversityManager == null)
            {
                Debug.LogError("[BiodiversityGlowSpawner] No BiodiversityScoreManager found!");
                enabled = false;
                return;
            }
        }

        // Create container
        glowContainer = new GameObject("BiodiversityGlows");
        glowContainer.transform.SetParent(transform);

        // Create glow material
        CreateGlowMaterial();

        if (enableDebugLogging)
            Debug.Log($"[BiodiversityGlowSpawner] Initialized with {maxGlows} max glows");

        // Initial spawn
        UpdateGlows();
    }

    void Update()
    {
        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
        {
            enableGlowEffects = !enableGlowEffects;
            glowContainer.SetActive(enableGlowEffects);
            Debug.Log($"[BiodiversityGlowSpawner] Glow effects: {(enableGlowEffects ? "ON" : "OFF")}");
        }

        // Auto update
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateGlows();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Creates emissive material for glow effect
    /// </summary>
    private void CreateGlowMaterial()
    {
        // Try URP/Unlit first, fallback to Sprites/Default if not found
        Shader glowShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (glowShader == null)
        {
            glowShader = Shader.Find("Sprites/Default");
            Debug.LogWarning("[BiodiversityGlowSpawner] URP/Unlit not found, using Sprites/Default");
        }

        if (glowShader == null)
        {
            Debug.LogError("[BiodiversityGlowSpawner] No suitable shader found for glow effect!");
            return;
        }

        glowMaterial = new Material(glowShader);

        // Set up transparency
        glowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive for glow
        glowMaterial.SetInt("_ZWrite", 0);
        glowMaterial.renderQueue = 3000; // Transparent queue

        if (enableDebugLogging)
            Debug.Log($"[BiodiversityGlowSpawner] Created material with shader: {glowShader.name}");
    }

    /// <summary>
    /// Updates glow effects based on biodiversity data
    /// </summary>
    public void UpdateGlows()
    {
        if (!enableGlowEffects || biodiversityManager == null)
            return;

        // Get biodiversity hotspots
        List<BiodiversityHotspot> hotspots = biodiversityManager.GetBiodiversityHotspots();

        if (hotspots == null || hotspots.Count == 0)
            return;

        // Track active hotspots
        HashSet<Vector2Int> activeGlows = new HashSet<Vector2Int>();
        int spawnedCount = 0;

        foreach (var hotspot in hotspots)
        {
            // Filter by minimum biodiversity
            if (hotspot.simpsonsIndex < minBiodiversityThreshold)
                continue;

            // Check spawn limit
            if (spawnedCount >= maxGlows)
                break;

            Vector2Int gridPos = WorldToGridPosition(hotspot.position);
            activeGlows.Add(gridPos);

            if (spawnedGlows.ContainsKey(gridPos) && spawnedGlows[gridPos] != null)
            {
                // Update existing glow
                UpdateGlowVisuals(spawnedGlows[gridPos], hotspot);
            }
            else
            {
                // Spawn new glow
                GameObject glowObj = SpawnGlow(hotspot);
                if (glowObj != null)
                {
                    spawnedGlows[gridPos] = glowObj;
                    spawnedCount++;
                }
            }
        }

        // Remove outdated glows
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var kvp in spawnedGlows)
        {
            if (!activeGlows.Contains(kvp.Key))
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
            spawnedGlows.Remove(key);

        if (enableDebugLogging)
            Debug.Log($"[BiodiversityGlowSpawner] Active glows: {spawnedGlows.Count}");
    }

    /// <summary>
    /// Spawns a glow effect at hotspot position
    /// </summary>
    private GameObject SpawnGlow(BiodiversityHotspot hotspot)
    {
        Vector3 position = hotspot.position + Vector3.up * glowHeight;

        // Create sphere for glow
        GameObject glowObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowObj.name = $"BiodiversityGlow_{hotspot.position.x:F0}_{hotspot.position.z:F0}";
        glowObj.transform.SetParent(glowContainer.transform);
        glowObj.transform.position = position;

        // Scale based on biodiversity
        float scale = glowSize * (0.5f + hotspot.simpsonsIndex * 0.5f);
        glowObj.transform.localScale = Vector3.one * scale;

        // Remove collider (visual only - no physics interactions)
        Destroy(glowObj.GetComponent<Collider>());

        // Set layer to not interfere with other systems
        glowObj.layer = LayerMask.NameToLayer("Default");

        // Apply material and color
        MeshRenderer renderer = glowObj.GetComponent<MeshRenderer>();
        if (glowMaterial != null)
        {
            renderer.material = new Material(glowMaterial); // Clone material
        }
        else
        {
            Debug.LogWarning("[BiodiversityGlowSpawner] Glow material is null, using default");
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Set sorting order higher than lines but lower than terrain overlays
        renderer.sortingOrder = 5;

        UpdateGlowVisuals(glowObj, hotspot);

        return glowObj;
    }

    /// <summary>
    /// Updates glow color and intensity based on biodiversity
    /// </summary>
    private void UpdateGlowVisuals(GameObject glowObj, BiodiversityHotspot hotspot)
    {
        MeshRenderer renderer = glowObj.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.material == null)
            return;

        // Interpolate color based on Simpson's Index
        Color baseColor = Color.Lerp(lowBiodiversityColor, highBiodiversityColor, hotspot.simpsonsIndex);

        // Apply HDR multiplier for bloom effect
        if (useHDRColors)
        {
            float hdrMultiplier = 1f + (hotspot.simpsonsIndex * glowIntensity);
            baseColor *= hdrMultiplier;
        }

        // Apply color to material
        renderer.material.color = baseColor;

        // Try to set emission color if the shader supports it
        if (renderer.material.HasProperty("_EmissionColor"))
        {
            renderer.material.SetColor("_EmissionColor", baseColor * glowIntensity);
        }
    }

    /// <summary>
    /// Converts world position to grid position
    /// </summary>
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float cellSize = biodiversityManager != null ? biodiversityManager.cellSize : 50f;
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Clear all glows
    /// </summary>
    public void ClearAllGlows()
    {
        foreach (var kvp in spawnedGlows)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        spawnedGlows.Clear();
    }

    void OnDestroy()
    {
        ClearAllGlows();
    }
}
