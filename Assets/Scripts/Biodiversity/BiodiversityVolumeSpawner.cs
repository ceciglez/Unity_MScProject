using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// Spawns Unity Post-Processing Volumes in a grid aligned with BiodiversityScoreManager
/// Applies color saturation effects based on Simpson's Biodiversity Index per cell
///
/// APPROACH: Grid-Based Volumes with Dynamic Updates
/// - Aligns with existing BiodiversityScoreManager grid system (cellSize)
/// - Spawns one volume per grid cell with biodiversity data
/// - Sets color saturation based on Simpson's Index (low bio = low sat, high bio = high sat)
/// - Global volume provides baseline low saturation
///
/// OPTIMIZATIONS (v2):
/// - Dynamic volume updates: Updates existing volumes instead of destroying/recreating
/// - Profile cloning: Each volume gets unique profile instance to prevent conflicts
/// - Smart culling: Only spawns volumes near player and removes distant ones
/// - Public API: Query methods for saturation values and volume data
///
/// INTEGRATION WITH GLOBAL VOLUME:
/// - Global Volume should have Priority = 0, Is Global = true, low saturation
/// - Local volumes have Priority = volumePriority (default 5), override in biodiverse areas
/// - Smooth blending controlled by blendDistance parameter
///
/// SOURCE: Unity URP Post-Processing documentation
/// AI CONTRIBUTION: ~85% - System design, optimization, profile management
/// HUMAN CONTRIBUTION: ~15% - Parameters, biodiversity integration, testing
/// </summary>

public class BiodiversityVolumeSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the BiodiversityScoreManager (auto-found if not set)")]
    public BiodiversityScoreManager biodiversityManager;

    [Tooltip("Volume prefab with Post-Processing Volume component and Color Adjustments")]
    public GameObject volumePrefab;

    [Header("Saturation Settings")]
    [Tooltip("Saturation for low biodiversity areas (Simpson's Index = 0)")]
    [Range(-1f, 0f)]
    public float lowBiodiversitySaturation = -0.8f;

    [Tooltip("Saturation for high biodiversity areas (Simpson's Index = 1)")]
    [Range(0f, 1f)]
    public float highBiodiversitySaturation = 0.5f;

    [Tooltip("Global baseline saturation (applied everywhere, overridden by local volumes)")]
    [Range(-1f, 0f)]
    public float globalBaselineSaturation = -0.5f;

    [Header("Volume Settings")]
    [Tooltip("Priority for spawned volumes (higher = overrides lower priority)")]
    [Range(0, 10)]
    public int volumePriority = 5;

    [Tooltip("Blend distance for volume effects (meters)")]
    [Range(5f, 100f)]
    public float blendDistance = 25f;

    [Tooltip("Volume height (how tall the volume trigger is)")]
    [Range(10f, 500f)]
    public float volumeHeight = 100f;

    [Header("Update Settings")]
    [Tooltip("Automatically update volumes when biodiversity recalculates")]
    public bool autoUpdate = true;

    [Tooltip("Update interval (seconds) - 0 means only update on biodiversity changes")]
    [Range(0f, 10f)]
    public float updateInterval = 5f;

    [Header("Performance")]
    [Tooltip("Maximum number of volumes to spawn (limits for performance)")]
    [Range(10, 200)]
    public int maxVolumes = 100;

    [Tooltip("Only spawn volumes near player (meters, 0 = unlimited)")]
    [Range(0f, 500f)]
    public float spawnRadius = 0f;

    [Header("Debugging")]
    public bool enableDebugLogging = true;
    public bool showVolumeGizmos = true;
    public KeyCode manualUpdateKey = KeyCode.V;

    // Private fields
    private Dictionary<Vector2Int, GameObject> spawnedVolumes = new Dictionary<Vector2Int, GameObject>();
    private GameObject volumeContainer;
    private float lastUpdateTime;
    private Transform playerTransform;

    void Start()
    {
        // Find BiodiversityScoreManager if not assigned
        if (biodiversityManager == null)
        {
            biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
            if (biodiversityManager == null)
            {
                Debug.LogError("[BiodiversityVolumeSpawner] No BiodiversityScoreManager found in scene! Cannot spawn volumes.");
                enabled = false;
                return;
            }
        }

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Create container for spawned volumes
        volumeContainer = new GameObject("BiodiversityVolumes");
        volumeContainer.transform.SetParent(transform);

        if (enableDebugLogging)
        {
            Debug.Log($"[BiodiversityVolumeSpawner] Initialized\n" +
                     $"  Saturation range: {lowBiodiversitySaturation:F2} to {highBiodiversitySaturation:F2}\n" +
                     $"  Global baseline: {globalBaselineSaturation:F2}\n" +
                     $"  Volume priority: {volumePriority}, Blend distance: {blendDistance}m\n" +
                     $"  Max volumes: {maxVolumes}, Spawn radius: {spawnRadius}m");
        }

        // Initial spawn
        SpawnBiodiversityVolumes();
    }

    void Update()
    {
        // Manual update
        if (Input.GetKeyDown(manualUpdateKey))
        {
            Debug.Log("[BiodiversityVolumeSpawner] Manual update triggered!");
            SpawnBiodiversityVolumes();
        }

        // Auto update
        if (autoUpdate && updateInterval > 0f && Time.time - lastUpdateTime >= updateInterval)
        {
            SpawnBiodiversityVolumes();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Main method: Spawns/updates volumes based on current biodiversity data
    /// NOW OPTIMIZED: Updates existing volumes instead of destroying and recreating
    /// </summary>
    public void SpawnBiodiversityVolumes()
    {
        if (biodiversityManager == null)
        {
            Debug.LogWarning("[BiodiversityVolumeSpawner] No BiodiversityScoreManager reference!");
            return;
        }

        if (volumePrefab == null)
        {
            Debug.LogError("[BiodiversityVolumeSpawner] No volume prefab assigned!");
            return;
        }

        // Get biodiversity hotspots from manager
        List<BiodiversityHotspot> hotspots = biodiversityManager.GetBiodiversityHotspots();

        if (hotspots == null || hotspots.Count == 0)
        {
            if (enableDebugLogging)
                Debug.Log("[BiodiversityVolumeSpawner] No biodiversity hotspots available yet");
            return;
        }

        // Get player position for radius filtering
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        // Track which grid cells should have volumes
        HashSet<Vector2Int> activeHotspotCells = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, BiodiversityHotspot> hotspotByCell = new Dictionary<Vector2Int, BiodiversityHotspot>();

        int spawnedCount = 0;
        int updatedCount = 0;
        int skippedDistance = 0;
        int skippedLimit = 0;

        // First pass: identify which cells need volumes
        foreach (var hotspot in hotspots)
        {
            // Check spawn limit
            if (activeHotspotCells.Count >= maxVolumes)
            {
                skippedLimit++;
                continue;
            }

            // Check spawn radius
            if (spawnRadius > 0f && Vector3.Distance(hotspot.position, playerPos) > spawnRadius)
            {
                skippedDistance++;
                continue;
            }

            Vector2Int gridCell = WorldToGridPosition(hotspot.position);
            activeHotspotCells.Add(gridCell);
            hotspotByCell[gridCell] = hotspot;
        }

        // Second pass: Remove volumes that are no longer needed
        List<Vector2Int> cellsToRemove = new List<Vector2Int>();
        foreach (var kvp in spawnedVolumes)
        {
            if (!activeHotspotCells.Contains(kvp.Key))
            {
                cellsToRemove.Add(kvp.Key);
            }
        }

        foreach (var cell in cellsToRemove)
        {
            if (spawnedVolumes[cell] != null)
            {
                Destroy(spawnedVolumes[cell]);
            }
            spawnedVolumes.Remove(cell);
        }

        // Third pass: Spawn new volumes or update existing ones
        foreach (var gridCell in activeHotspotCells)
        {
            BiodiversityHotspot hotspot = hotspotByCell[gridCell];

            if (spawnedVolumes.ContainsKey(gridCell) && spawnedVolumes[gridCell] != null)
            {
                // Volume already exists - just update its saturation
                UpdateVolumeData(spawnedVolumes[gridCell], hotspot);
                updatedCount++;
            }
            else
            {
                // Spawn new volume
                GameObject volumeObj = SpawnVolume(gridCell, hotspot);

                if (volumeObj != null)
                {
                    spawnedVolumes[gridCell] = volumeObj;
                    spawnedCount++;

                    if (enableDebugLogging && spawnedCount <= 5) // Log first 5
                    {
                        Debug.Log($"[BiodiversityVolumeSpawner] New Volume #{spawnedCount}: " +
                                 $"Grid {gridCell}, Simpson's Index {hotspot.simpsonsIndex:F3}, " +
                                 $"Saturation {CalculateSaturation(hotspot.simpsonsIndex):F2}");
                    }
                }
            }
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[BiodiversityVolumeSpawner] ✅ Volume Update Complete\n" +
                     $"  New volumes: {spawnedCount}\n" +
                     $"  Updated volumes: {updatedCount}\n" +
                     $"  Removed volumes: {cellsToRemove.Count}\n" +
                     $"  Total active: {spawnedVolumes.Count}\n" +
                     $"  Skipped (distance): {skippedDistance}\n" +
                     $"  Skipped (limit): {skippedLimit}\n" +
                     $"  Total hotspots: {hotspots.Count}");
        }
    }

    /// <summary>
    /// Updates an existing volume's saturation value without recreating it
    /// </summary>
    private void UpdateVolumeData(GameObject volumeObj, BiodiversityHotspot hotspot)
    {
        Volume volume = volumeObj.GetComponent<Volume>();
        if (volume == null || volume.profile == null)
            return;

        float saturation = CalculateSaturation(hotspot.simpsonsIndex);

        UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
        if (volume.profile.TryGet(out colorAdj))
        {
            colorAdj.saturation.Override(saturation);
        }
    }

    /// <summary>
    /// Spawns a single volume at grid position with biodiversity-based saturation
    /// IMPROVED: Creates unique profile instance to avoid shared profile conflicts
    /// </summary>
    private GameObject SpawnVolume(Vector2Int gridCell, BiodiversityHotspot hotspot)
    {
        // Calculate world position (center of cell)
        Vector3 worldPos = GridToWorldPosition(gridCell);
        worldPos.y = 0.5f; // Slight offset above ground to avoid rendering conflicts with LineRenderers

        // Instantiate volume prefab
        GameObject volumeObj = Instantiate(volumePrefab, worldPos, Quaternion.identity, volumeContainer.transform);
        volumeObj.name = $"BiodiversityVolume_{gridCell.x}_{gridCell.y}";

        // Keep on Default layer - volumes are filtered out of raycasts by name in NetworkConnection
        // This ensures camera can still detect triggers for post-processing activation
        volumeObj.layer = LayerMask.NameToLayer("Default");

        // Get or add Volume component
        Volume volume = volumeObj.GetComponent<Volume>();
        if (volume == null)
        {
            volume = volumeObj.AddComponent<Volume>();
        }

        // Configure volume
        volume.isGlobal = false; // Local volume
        volume.priority = volumePriority;
        volume.weight = 1f;
        volume.blendDistance = blendDistance;

        // IMPORTANT: Clone the profile to create a unique instance
        // This prevents all volumes from sharing the same profile and settings
        if (volume.profile != null)
        {
            volume.profile = Instantiate(volume.profile);
        }
        else
        {
            Debug.LogWarning($"[BiodiversityVolumeSpawner] Volume prefab has no VolumeProfile! Cannot set saturation.");
            return volumeObj;
        }

        // Add box collider for local volume trigger
        BoxCollider boxCollider = volumeObj.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = volumeObj.AddComponent<BoxCollider>();
        }
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector3(biodiversityManager.cellSize, volumeHeight, biodiversityManager.cellSize);
        // Center adjusted: volume is at y=0.5f, so collider extends from 0.5f to (0.5f + volumeHeight)
        boxCollider.center = new Vector3(0f, volumeHeight / 2f, 0f);

        // Calculate saturation based on Simpson's Index
        float saturation = CalculateSaturation(hotspot.simpsonsIndex);

        // Apply color adjustment with the cloned profile
        UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
        if (volume.profile.TryGet(out colorAdj))
        {
            colorAdj.saturation.Override(saturation);
        }
        else
        {
            // Add new ColorAdjustments if not present
            colorAdj = volume.profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
            colorAdj.saturation.Override(saturation);
        }

        return volumeObj;
    }

    /// <summary>
    /// Calculates saturation value from Simpson's Biodiversity Index
    /// Maps Simpson's Index (0-1) to saturation range (low to high)
    /// </summary>
    private float CalculateSaturation(float simpsonsIndex)
    {
        // Linear interpolation from low to high biodiversity saturation
        float saturation = Mathf.Lerp(lowBiodiversitySaturation, highBiodiversitySaturation, simpsonsIndex);
        return saturation;
    }

    /// <summary>
    /// Clears all spawned volumes
    /// </summary>
    public void ClearAllVolumes()
    {
        foreach (var kvp in spawnedVolumes)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        spawnedVolumes.Clear();

        if (enableDebugLogging)
            Debug.Log("[BiodiversityVolumeSpawner] Cleared all volumes");
    }

    /// <summary>
    /// Converts world position to grid cell (matches BiodiversityScoreManager)
    /// </summary>
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float cellSize = biodiversityManager.cellSize;
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Converts grid cell to world position (center of cell)
    /// </summary>
    private Vector3 GridToWorldPosition(Vector2Int gridCell)
    {
        float cellSize = biodiversityManager.cellSize;
        float x = (gridCell.x + 0.5f) * cellSize;
        float z = (gridCell.y + 0.5f) * cellSize;
        return new Vector3(x, 0f, z);
    }

    void OnDrawGizmos()
    {
        if (!showVolumeGizmos || spawnedVolumes == null || spawnedVolumes.Count == 0)
            return;

        foreach (var kvp in spawnedVolumes)
        {
            if (kvp.Value == null)
                continue;

            Vector3 position = kvp.Value.transform.position;
            float cellSize = biodiversityManager != null ? biodiversityManager.cellSize : 50f;

            // Get saturation from volume
            Volume volume = kvp.Value.GetComponent<Volume>();
            float saturation = 0f;
            if (volume != null && volume.profile != null)
            {
                UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
                if (volume.profile.TryGet(out colorAdj))
                {
                    saturation = colorAdj.saturation.value;
                }
            }

            // Color gizmo based on saturation (green = high, red = low)
            Color gizmoColor = Color.Lerp(Color.red, Color.green, Mathf.InverseLerp(lowBiodiversitySaturation, highBiodiversitySaturation, saturation));
            gizmoColor.a = 0.3f;

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(position + Vector3.up * volumeHeight / 2f, new Vector3(cellSize, volumeHeight, cellSize));

            // Wireframe
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(position + Vector3.up * volumeHeight / 2f, new Vector3(cellSize, volumeHeight, cellSize));
        }
    }

    // ==================== PUBLIC API METHODS ====================

    /// <summary>
    /// Manually refresh volumes without full respawn (calls the optimized update)
    /// </summary>
    public void RefreshVolumes()
    {
        SpawnBiodiversityVolumes();
    }

    /// <summary>
    /// Runtime adjustment of saturation range
    /// </summary>
    public void SetSaturationRange(float low, float high)
    {
        lowBiodiversitySaturation = Mathf.Clamp(low, -1f, 0f);
        highBiodiversitySaturation = Mathf.Clamp(high, 0f, 1f);

        if (enableDebugLogging)
            Debug.Log($"[BiodiversityVolumeSpawner] Saturation range updated: {lowBiodiversitySaturation:F2} to {highBiodiversitySaturation:F2}");

        // Refresh all volumes with new saturation values
        RefreshVolumes();
    }

    /// <summary>
    /// Query the volume at a specific world position
    /// </summary>
    public GameObject GetVolumeAtPosition(Vector3 worldPos)
    {
        Vector2Int gridCell = WorldToGridPosition(worldPos);

        if (spawnedVolumes.TryGetValue(gridCell, out GameObject volumeObj))
        {
            return volumeObj;
        }

        return null;
    }

    /// <summary>
    /// Get the saturation value at a specific world position
    /// </summary>
    public float GetSaturationAtPosition(Vector3 worldPos)
    {
        GameObject volumeObj = GetVolumeAtPosition(worldPos);

        if (volumeObj != null)
        {
            Volume volume = volumeObj.GetComponent<Volume>();
            if (volume != null && volume.profile != null)
            {
                UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
                if (volume.profile.TryGet(out colorAdj))
                {
                    return colorAdj.saturation.value;
                }
            }
        }

        return globalBaselineSaturation; // Return baseline if no local volume found
    }

    /// <summary>
    /// Get total number of active volumes
    /// </summary>
    public int GetActiveVolumeCount()
    {
        return spawnedVolumes.Count;
    }

    /// <summary>
    /// Check if a volume exists at a specific grid position
    /// </summary>
    public bool HasVolumeAtGridPosition(Vector2Int gridCell)
    {
        return spawnedVolumes.ContainsKey(gridCell) && spawnedVolumes[gridCell] != null;
    }

    void OnDestroy()
    {
        ClearAllVolumes();
    }
}
