using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Applies biodiversity-based color/saturation directly to terrain chunks
/// Makes biodiversity visible from ANY distance without requiring camera to be inside volumes
///
/// APPROACH:
/// - Creates colored overlay quads at each biodiversity hotspot
/// - Quads are textured/colored based on Simpson's Index
/// - Visible from far away as colored patches on terrain
/// - Alternative to camera-based post-processing volumes
///
/// FUNCTIONALITY:
/// - Spawns flat colored quads slightly above terrain
/// - Color intensity based on biodiversity (gray → vibrant green)
/// - Uses additive or multiply blending for terrain overlay
/// - Updates dynamically with biodiversity changes
///
/// INTEGRATION:
/// - Works alongside or instead of BiodiversityVolumeSpawner
/// - Queries BiodiversityScoreManager for hotspot data
/// - Creates visual "stains" of color on terrain
///
/// CODE LOGIC SUGGESTED BY: Claude Sonnet 4.5, Dec 2025
/// PROMPT: "Create colored overlay quads on terrain that show biodiversity hotspots visible from any distance"
/// SOURCE:
/// - Unity quad mesh generation
/// - Material blending modes for terrain overlay
///
/// AI CONTRIBUTION: ~90% - Procedural quad mesh generation, color gradient mapping, hotspot positioning
/// HUMAN CONTRIBUTION: ~10% - Color palette selection, quad scale/height offset tuning
/// </summary>
public class BiodiversityTerrainColorizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to BiodiversityScoreManager")]
    public BiodiversityScoreManager biodiversityManager;

    [Header("Visual Settings")]
    [Tooltip("Enable terrain colorization")]
    public bool enableColorization = true;

    [Tooltip("Height of color overlay above terrain (prevents z-fighting)")]
    [Range(0.01f, 2f)]
    public float overlayHeight = 0.05f;

    [Tooltip("Opacity of color overlay")]
    [Range(0.1f, 1f)]
    public float overlayOpacity = 0.6f;

    [Header("Color Settings")]
    [Tooltip("Color for low biodiversity (transparent gray)")]
    public Color lowBiodiversityColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

    [Tooltip("Color for high biodiversity (vibrant green)")]
    public Color highBiodiversityColor = new Color(0f, 1f, 0.5f, 0.8f);

    [Header("Update Settings")]
    [Tooltip("Update interval (seconds)")]
    [Range(1f, 10f)]
    public float updateInterval = 3f;

    [Header("Performance")]
    [Tooltip("Maximum overlays to spawn")]
    [Range(10, 100)]
    public int maxOverlays = 50;

    [Tooltip("Minimum Simpson's Index to show color")]
    [Range(0f, 0.5f)]
    public float minBiodiversityThreshold = 0.15f;

    [Header("Debugging")]
    public bool enableDebugLogging = false;
    public KeyCode toggleKey = KeyCode.T;

    // Private fields
    private Dictionary<Vector2Int, GameObject> spawnedOverlays = new Dictionary<Vector2Int, GameObject>();
    private GameObject overlayContainer;
    private float lastUpdateTime;
    private Material overlayMaterial;

    void Start()
    {
        // Find BiodiversityScoreManager
        if (biodiversityManager == null)
        {
            biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
            if (biodiversityManager == null)
            {
                Debug.LogError("[BiodiversityTerrainColorizer] No BiodiversityScoreManager found!");
                enabled = false;
                return;
            }
        }

        // Create container
        overlayContainer = new GameObject("BiodiversityTerrainOverlays");
        overlayContainer.transform.SetParent(transform);

        // Create overlay material
        CreateOverlayMaterial();

        if (enableDebugLogging)
            Debug.Log($"[BiodiversityTerrainColorizer] Initialized");

        // Initial update
        UpdateTerrainColors();
    }

    void Update()
    {
        // Toggle
        if (Input.GetKeyDown(toggleKey))
        {
            enableColorization = !enableColorization;
            overlayContainer.SetActive(enableColorization);
            Debug.Log($"[BiodiversityTerrainColorizer] Terrain colorization: {(enableColorization ? "ON" : "OFF")}");
        }

        // Auto update
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateTerrainColors();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Creates transparent overlay material
    /// </summary>
    private void CreateOverlayMaterial()
    {
        // Use standard transparent shader
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("[BiodiversityTerrainColorizer] Sprites/Default shader not found!");
            return;
        }

        overlayMaterial = new Material(shader);

        // Set up for transparency with additive blending for glow effect
        overlayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        overlayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        overlayMaterial.SetInt("_ZWrite", 0);
        overlayMaterial.renderQueue = 3000; // Transparent queue

        if (enableDebugLogging)
            Debug.Log("[BiodiversityTerrainColorizer] Created overlay material");
    }

    /// <summary>
    /// Updates terrain colors based on biodiversity
    /// </summary>
    public void UpdateTerrainColors()
    {
        if (!enableColorization || biodiversityManager == null)
            return;

        List<BiodiversityHotspot> hotspots = biodiversityManager.GetBiodiversityHotspots();

        if (hotspots == null || hotspots.Count == 0)
            return;

        HashSet<Vector2Int> activeOverlays = new HashSet<Vector2Int>();
        int createdCount = 0;

        foreach (var hotspot in hotspots)
        {
            // Filter by threshold
            if (hotspot.simpsonsIndex < minBiodiversityThreshold)
                continue;

            // Check limit
            if (createdCount >= maxOverlays)
                break;

            Vector2Int gridPos = WorldToGridPosition(hotspot.position);
            activeOverlays.Add(gridPos);

            if (spawnedOverlays.ContainsKey(gridPos) && spawnedOverlays[gridPos] != null)
            {
                // Update existing
                UpdateOverlayColor(spawnedOverlays[gridPos], hotspot);
            }
            else
            {
                // Create new
                GameObject overlay = CreateColorOverlay(hotspot);
                if (overlay != null)
                {
                    spawnedOverlays[gridPos] = overlay;
                    createdCount++;
                }
            }
        }

        // Remove old overlays
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var kvp in spawnedOverlays)
        {
            if (!activeOverlays.Contains(kvp.Key))
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
            spawnedOverlays.Remove(key);

        if (enableDebugLogging)
            Debug.Log($"[BiodiversityTerrainColorizer] Active overlays: {spawnedOverlays.Count}");
    }

    /// <summary>
    /// Creates a colored quad overlay at hotspot position
    /// </summary>
    private GameObject CreateColorOverlay(BiodiversityHotspot hotspot)
    {
        float cellSize = biodiversityManager.cellSize;

        // Create quad
        GameObject overlay = new GameObject($"BiodiversityOverlay_{hotspot.position.x:F0}_{hotspot.position.z:F0}");
        overlay.transform.SetParent(overlayContainer.transform);
        overlay.transform.position = hotspot.position + Vector3.up * overlayHeight;
        overlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Face down

        // Add mesh
        MeshFilter meshFilter = overlay.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = overlay.AddComponent<MeshRenderer>();

        // Create quad mesh
        Mesh mesh = new Mesh();
        float halfSize = cellSize * 0.5f;

        mesh.vertices = new Vector3[]
        {
            new Vector3(-halfSize, -halfSize, 0),
            new Vector3(halfSize, -halfSize, 0),
            new Vector3(-halfSize, halfSize, 0),
            new Vector3(halfSize, halfSize, 0)
        };

        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };

        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        // Apply material
        if (overlayMaterial != null)
        {
            meshRenderer.material = new Material(overlayMaterial); // Clone
        }
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // Set sorting order higher than lines so overlays render on top
        meshRenderer.sortingOrder = 10;

        // Set initial color
        UpdateOverlayColor(overlay, hotspot);

        return overlay;
    }

    /// <summary>
    /// Updates overlay color based on biodiversity
    /// </summary>
    private void UpdateOverlayColor(GameObject overlay, BiodiversityHotspot hotspot)
    {
        MeshRenderer renderer = overlay.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.material == null)
            return;

        // Interpolate color
        Color color = Color.Lerp(lowBiodiversityColor, highBiodiversityColor, hotspot.simpsonsIndex);

        // Apply opacity
        color.a *= overlayOpacity;

        renderer.material.color = color;
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
    /// Clear all overlays
    /// </summary>
    public void ClearAllOverlays()
    {
        foreach (var kvp in spawnedOverlays)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        spawnedOverlays.Clear();
    }

    void OnDestroy()
    {
        ClearAllOverlays();
    }
}
