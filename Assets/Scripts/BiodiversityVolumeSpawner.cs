using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// Spawns Unity Post-Processing Volumes in a grid aligned with BiodiversityScoreManager
/// Applies color saturation effects based on Simpson's Biodiversity Index per cell
///
/// APPROACH: Grid-Based Volumes
/// - Aligns with existing BiodiversityScoreManager grid system (cellSize)
/// - Spawns one volume per grid cell with biodiversity data
/// - Sets color saturation based on Simpson's Index (low bio = low sat, high bio = high sat)
/// - Global volume provides baseline low saturation
///
/// SOURCE: Unity Post-Processing Stack V2 documentation
/// AI CONTRIBUTION: ~75% - System design, grid alignment, volume management
/// HUMAN CONTRIBUTION: ~25% - Parameters, biodiversity integration, testing
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

        // Clear existing volumes (we'll respawn)
        ClearAllVolumes();

        int spawnedCount = 0;
        int skippedDistance = 0;
        int skippedLimit = 0;

        foreach (var hotspot in hotspots)
        {
            // Check spawn limit
            if (spawnedCount >= maxVolumes)
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

            // Convert hotspot position to grid cell
            Vector2Int gridCell = WorldToGridPosition(hotspot.position);

            // Spawn volume
            GameObject volumeObj = SpawnVolume(gridCell, hotspot);

            if (volumeObj != null)
            {
                spawnedVolumes[gridCell] = volumeObj;
                spawnedCount++;

                if (enableDebugLogging && spawnedCount <= 5) // Log first 5
                {
                    Debug.Log($"[BiodiversityVolumeSpawner] Volume #{spawnedCount}: " +
                             $"Grid {gridCell}, Simpson's Index {hotspot.simpsonsIndex:F3}, " +
                             $"Saturation {CalculateSaturation(hotspot.simpsonsIndex):F2}");
                }
            }
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[BiodiversityVolumeSpawner] ✅ Spawned {spawnedCount} volumes\n" +
                     $"  Skipped (distance): {skippedDistance}\n" +
                     $"  Skipped (limit): {skippedLimit}\n" +
                     $"  Total hotspots: {hotspots.Count}");
        }
    }

    /// <summary>
    /// Spawns a single volume at grid position with biodiversity-based saturation
    /// </summary>
    private GameObject SpawnVolume(Vector2Int gridCell, BiodiversityHotspot hotspot)
    {
        // Calculate world position (center of cell)
        Vector3 worldPos = GridToWorldPosition(gridCell);
        worldPos.y = 0f; // Ground level

        // Instantiate volume prefab
        GameObject volumeObj = Instantiate(volumePrefab, worldPos, Quaternion.identity, volumeContainer.transform);
        volumeObj.name = $"BiodiversityVolume_{gridCell.x}_{gridCell.y}";

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

        // Add box collider for local volume trigger
        BoxCollider boxCollider = volumeObj.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = volumeObj.AddComponent<BoxCollider>();
        }
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector3(biodiversityManager.cellSize, volumeHeight, biodiversityManager.cellSize);
        boxCollider.center = new Vector3(0f, volumeHeight / 2f, 0f);

        // Calculate saturation based on Simpson's Index
        float saturation = CalculateSaturation(hotspot.simpsonsIndex);

        // Apply color adjustment (you'll need to add ColorAdjustments to the volume profile)
        // NOTE: This requires a VolumeProfile with ColorAdjustments override
        if (volume.profile != null)
        {
            // Try to get existing ColorAdjustments
            UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
            if (volume.profile.TryGet(out colorAdj))
            {
                colorAdj.saturation.Override(saturation);
            }
            else
            {
                // Add new ColorAdjustments
                colorAdj = volume.profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
                colorAdj.saturation.Override(saturation);
            }
        }
        else
        {
            Debug.LogWarning($"[BiodiversityVolumeSpawner] Volume prefab has no VolumeProfile! Cannot set saturation.");
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

    void OnDestroy()
    {
        ClearAllVolumes();
    }
}
