using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
/// DEVELOPMENT APPROACH: Iterative human-AI collaboration
/// - HUMAN: Designed saturation-based visualization system, defined grid alignment with BiodiversityScoreManager
/// - AI: Implemented URP Volume spawning, profile cloning, spatial culling logic
/// - HUMAN: Configured saturation ranges, player radius settings, integrated with biodiversity data, performance testing
///
/// SOURCE: Unity URP Post-Processing documentation
/// ATTRIBUTION: Visual design and system integration (human), URP implementation (AI-assisted)
/// </summary>

public class BiodiversityVolumeSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the BiodiversityScoreManager (auto-found if not set)")]
    public BiodiversityScoreManager biodiversityManager;

    [Tooltip("Volume prefab with Post-Processing Volume component and Color Adjustments")]
    public GameObject volumePrefab;

    [Tooltip("Player/Camera transform for position tracking (auto-found if not set)")]
    public Transform playerOrCameraTransform;

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
    public float blendDistance = 100f;

    [Tooltip("Volume height (how tall the volume trigger is)")]
    [Range(10f, 500f)]
    public float volumeHeight = 50f;

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
    public float spawnRadius = 200f; // Default to 200m radius around player

    [Tooltip("Update volumes when player moves this distance (meters)")]
    [Range(10f, 100f)]
    public float playerMovementThreshold = 50f; // Update when player moves 50m

    [Header("Debugging")]
    public bool enableDebugLogging = true;
    public bool showVolumeGizmos = true;
    public KeyCode manualUpdateKey = KeyCode.V;

    // Private fields
    private Dictionary<Vector2Int, GameObject> spawnedVolumes = new Dictionary<Vector2Int, GameObject>();
    private GameObject volumeContainer;
    private float lastUpdateTime;
    private Transform playerTransform;
    private Vector3 lastPlayerPosition;

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

        // Find player/camera transform - prioritize manually assigned reference
        if (playerOrCameraTransform != null)
        {
            playerTransform = playerOrCameraTransform;
            lastPlayerPosition = playerTransform.position;
            Debug.Log($"[BiodiversityVolumeSpawner] ✅ Using manually assigned transform: {playerTransform.name}");
        }
        else
        {
            // Try to find Player tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                lastPlayerPosition = playerTransform.position;
                Debug.Log($"[BiodiversityVolumeSpawner] ✅ Player found: {player.name}");
            }
            else
            {
                // Fallback to main camera if no Player tag found
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerTransform = mainCam.transform;
                    lastPlayerPosition = playerTransform.position;
                    Debug.LogWarning($"[BiodiversityVolumeSpawner] No 'Player' tag found! Using Main Camera '{mainCam.name}' for movement tracking instead.");
                }
                else
                {
                    Debug.LogError("[BiodiversityVolumeSpawner] ❌ No Player OR Main Camera found! Volumes won't update with movement.");
                }
            }
        }

        // Create container for spawned volumes
        volumeContainer = new GameObject("BiodiversityVolumes");
        volumeContainer.transform.SetParent(transform);

        // Check camera setup for volume detection
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var cameraData = mainCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                Debug.Log($"[BiodiversityVolumeSpawner] 📷 Camera Volume Layer Mask (raw value): {cameraData.volumeLayerMask.value}");
                Debug.Log($"[BiodiversityVolumeSpawner] 📷 Camera Rendering Post-Processing: {cameraData.renderPostProcessing}");

                // Check if TransparentFX layer is included (volumes are now on this layer)
                int transparentFXLayerMask = 1 << LayerMask.NameToLayer("TransparentFX");
                bool hasTransparentFXLayer = (cameraData.volumeLayerMask.value & transparentFXLayerMask) != 0;

                Debug.Log($"[BiodiversityVolumeSpawner] 📷 TransparentFX layer included in volume mask: {hasTransparentFXLayer}");

                // Also check Default layer for backward compatibility
                int defaultLayerMask = 1 << LayerMask.NameToLayer("Default");
                bool hasDefaultLayer = (cameraData.volumeLayerMask.value & defaultLayerMask) != 0;
                Debug.Log($"[BiodiversityVolumeSpawner] 📷 Default layer included in volume mask: {hasDefaultLayer}");

                // Add TransparentFX layer to camera's volume mask if not present
                if (!hasTransparentFXLayer)
                {
                    Debug.LogWarning("[BiodiversityVolumeSpawner] ⚠️ Camera's Volume Layer Mask doesn't include 'TransparentFX' layer!");
                    Debug.LogWarning("[BiodiversityVolumeSpawner] 🔧 AUTO-FIX: Adding 'TransparentFX' layer to camera's volume layer mask...");

                    // Add TransparentFX layer to the volume mask
                    cameraData.volumeLayerMask |= transparentFXLayerMask;

                    Debug.Log($"[BiodiversityVolumeSpawner] ✅ Fixed! Camera Volume Layer Mask now: {cameraData.volumeLayerMask.value}");
                }
                else
                {
                    Debug.Log("[BiodiversityVolumeSpawner] ✅ Camera is correctly configured to detect volumes on TransparentFX layer");
                }

                // Also check if post-processing is enabled
                if (!cameraData.renderPostProcessing)
                {
                    Debug.LogError("[BiodiversityVolumeSpawner] ❌ CRITICAL: Camera Post-Processing is DISABLED!");
                    Debug.LogError("[BiodiversityVolumeSpawner] Volumes won't work. Enable 'Post Processing' on camera's URP settings.");
                }
            }
        }

        // Check for Global Volume and its priority
        CheckGlobalVolumePriority();

        if (enableDebugLogging)
        {
            Debug.Log($"[BiodiversityVolumeSpawner] Initialized\n" +
                     $"  Saturation range: {lowBiodiversitySaturation:F2} to {highBiodiversitySaturation:F2}\n" +
                     $"  Global baseline: {globalBaselineSaturation:F2}\n" +
                     $"  Volume priority: {volumePriority}, Blend distance: {blendDistance}m\n" +
                     $"  Max volumes: {maxVolumes}, Spawn radius: {spawnRadius}m\n" +
                     $"  Player movement threshold: {playerMovementThreshold}m");
        }

        // Initial spawn
        SpawnBiodiversityVolumes();
    }

    void Update()
    {
        // Manual update
        if (Input.GetKeyDown(manualUpdateKey))
        {
            Debug.Log("========================================");
            Debug.Log("[BiodiversityVolumeSpawner] ⚡ MANUAL UPDATE TRIGGERED (V key pressed)");
            Debug.Log($"[BiodiversityVolumeSpawner] Player position: {(playerTransform != null ? playerTransform.position.ToString() : "No player found")}");
            Debug.Log($"[BiodiversityVolumeSpawner] Current active volumes: {spawnedVolumes.Count}");
            SpawnBiodiversityVolumes();
            Debug.Log("========================================");
            return;
        }

        if (!autoUpdate) return;

        bool shouldUpdate = false;

        // Check if player has moved significantly (like INaturalistMapController and grass spawner)
        if (playerTransform != null)
        {
            float playerMovement = Vector3.Distance(lastPlayerPosition, playerTransform.position);

            if (playerMovement > playerMovementThreshold)
            {
                shouldUpdate = true;
                lastPlayerPosition = playerTransform.position;

                if (enableDebugLogging)
                {
                    Debug.Log($"[BiodiversityVolumeSpawner] Player moved {playerMovement:F0}m - updating volumes");
                }
            }
        }

        // Also update on interval (backup update method)
        if (updateInterval > 0f && Time.time - lastUpdateTime >= updateInterval)
        {
            shouldUpdate = true;
        }

        if (shouldUpdate)
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
        Debug.Log("[BiodiversityVolumeSpawner] 🔄 SpawnBiodiversityVolumes() called");

        if (biodiversityManager == null)
        {
            Debug.LogError("[BiodiversityVolumeSpawner] ❌ No BiodiversityScoreManager reference!");
            return;
        }

        Debug.Log($"[BiodiversityVolumeSpawner] ✓ BiodiversityScoreManager found: {biodiversityManager.name}");

        if (volumePrefab == null)
        {
            Debug.LogError("[BiodiversityVolumeSpawner] ❌ No volume prefab assigned! Assign a prefab in the Inspector.");
            return;
        }

        Debug.Log($"[BiodiversityVolumeSpawner] ✓ Volume prefab assigned: {volumePrefab.name}");

        // Get biodiversity hotspots from manager
        List<BiodiversityHotspot> hotspots = biodiversityManager.GetBiodiversityHotspots();

        Debug.Log($"[BiodiversityVolumeSpawner] Hotspots retrieved: {(hotspots != null ? hotspots.Count.ToString() : "NULL")}");

        if (hotspots == null || hotspots.Count == 0)
        {
            Debug.LogWarning("[BiodiversityVolumeSpawner] ⚠️ No biodiversity hotspots available yet");
            Debug.LogWarning("[BiodiversityVolumeSpawner] Tip: Wait for observations to load or move to an area with observations");
            return;
        }

        Debug.Log($"[BiodiversityVolumeSpawner] ✓ Found {hotspots.Count} biodiversity hotspots");

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

        // Check camera position relative to volumes
        Camera cam = Camera.main;
        string cameraInfo = "No camera";
        if (cam != null)
        {
            cameraInfo = $"Camera at {cam.transform.position}, looking at volumes";

            // Check if camera is inside any volume bounds
            foreach (var kvp in spawnedVolumes)
            {
                if (kvp.Value != null)
                {
                    BoxCollider col = kvp.Value.GetComponent<BoxCollider>();
                    if (col != null && col.bounds.Contains(cam.transform.position))
                    {
                        Volume vol = kvp.Value.GetComponent<Volume>();
                        UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
                        float sat = 0f;
                        if (vol != null && vol.profile != null && vol.profile.TryGet(out colorAdj))
                        {
                            sat = colorAdj.saturation.value;
                        }
                        Debug.Log($"[BiodiversityVolumeSpawner] 🎥 CAMERA IS INSIDE VOLUME: {kvp.Value.name}, Saturation={sat:F2}");
                    }
                }
            }
        }

        // ALWAYS log the summary for debugging
        Debug.Log($"[BiodiversityVolumeSpawner] ✅ Volume Update Complete\n" +
                 $"  🆕 New volumes spawned: {spawnedCount}\n" +
                 $"  🔄 Updated volumes: {updatedCount}\n" +
                 $"  🗑️  Removed volumes: {cellsToRemove.Count}\n" +
                 $"  📊 Total active volumes: {spawnedVolumes.Count}\n" +
                 $"  ⏭️  Skipped (too far from player): {skippedDistance}\n" +
                 $"  ⏭️  Skipped (max limit reached): {skippedLimit}\n" +
                 $"  🌍 Total biodiversity hotspots: {hotspots.Count}\n" +
                 $"  📷 {cameraInfo}");
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

        // Put volumes on TransparentFX layer (or create a dedicated "Volumes" layer)
        // This layer is already excluded from terrain raycasts in NetworkConnection
        // Camera can still detect volumes for post-processing on any layer
        int volumeLayer = LayerMask.NameToLayer("TransparentFX");
        if (volumeLayer == -1)
        {
            volumeLayer = LayerMask.NameToLayer("Default");
            Debug.LogWarning("[BiodiversityVolumeSpawner] TransparentFX layer not found, using Default (lines may raycast volumes)");
        }
        volumeObj.layer = volumeLayer;

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

        // Calculate saturation based on Simpson's Index
        float saturation = CalculateSaturation(hotspot.simpsonsIndex);

        // Add box collider to define volume bounds
        // CRITICAL: isTrigger must be TRUE for volume detection but collider should be on a separate layer
        // to avoid physics interactions
        BoxCollider boxCollider = volumeObj.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = volumeObj.AddComponent<BoxCollider>();
        }

        // For URP Volumes: isTrigger=true works, but we need to ensure proper bounds
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector3(biodiversityManager.cellSize, volumeHeight, biodiversityManager.cellSize);

        // CRITICAL FIX: Center the collider properly
        // Volume is at y=0.5, and should extend UP from there
        // If volumeHeight=50, collider should go from y=0.5 to y=50.5
        // So center should be at y=0 (relative to volume GameObject which is at y=0.5)
        // which puts the collider center at world y=0.5, extending from 0.5-25 to 0.5+25
        boxCollider.center = Vector3.zero; // Center relative to GameObject (which is already at y=0.5)

        // Apply color adjustment with the cloned profile
        UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
        if (volume.profile.TryGet(out colorAdj))
        {
            colorAdj.saturation.Override(saturation);
            Debug.Log($"[BiodiversityVolumeSpawner] 📦 Volume created at {worldPos}: " +
                     $"Simpson's Index={hotspot.simpsonsIndex:F3}, Saturation={saturation:F2}, " +
                     $"Priority={volume.priority}, IsGlobal={volume.isGlobal}, Weight={volume.weight}, " +
                     $"BlendDistance={volume.blendDistance}, Layer={LayerMask.LayerToName(volumeObj.layer)}");
        }
        else
        {
            // Add new ColorAdjustments if not present
            colorAdj = volume.profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
            colorAdj.saturation.Override(saturation);
            Debug.Log($"[BiodiversityVolumeSpawner] 📦 Volume created (NEW ColorAdj) at {worldPos}: " +
                     $"Simpson's Index={hotspot.simpsonsIndex:F3}, Saturation={saturation:F2}");
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

    /// <summary>
    /// Diagnostic: Check Global Volume priority to ensure local volumes will override it
    /// </summary>
    private void CheckGlobalVolumePriority()
    {
        Debug.Log("[BiodiversityVolumeSpawner] 🔍 Searching for Global Volume in scene...");

        Volume[] allVolumes = FindObjectsOfType<Volume>();
        Debug.Log($"[BiodiversityVolumeSpawner] Found {allVolumes.Length} total Volume components in scene");

        foreach (Volume vol in allVolumes)
        {
            if (vol.isGlobal)
            {
                Debug.Log($"[BiodiversityVolumeSpawner] 🌍 GLOBAL VOLUME FOUND: {vol.gameObject.name}");
                Debug.Log($"[BiodiversityVolumeSpawner]   Priority: {vol.priority}");
                Debug.Log($"[BiodiversityVolumeSpawner]   Weight: {vol.weight}");
                Debug.Log($"[BiodiversityVolumeSpawner]   Layer: {LayerMask.LayerToName(vol.gameObject.layer)}");

                // Check if profile has ColorAdjustments
                if (vol.profile != null)
                {
                    UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
                    if (vol.profile.TryGet(out colorAdj))
                    {
                        Debug.Log($"[BiodiversityVolumeSpawner]   Global Saturation: {colorAdj.saturation.value:F2}");
                    }
                    else
                    {
                        Debug.LogWarning("[BiodiversityVolumeSpawner]   ⚠️ Global Volume has no ColorAdjustments!");
                    }
                }

                // Warn if priority conflict
                if (vol.priority >= volumePriority)
                {
                    Debug.LogWarning($"[BiodiversityVolumeSpawner] ⚠️ WARNING: Global Volume priority ({vol.priority}) is >= local volume priority ({volumePriority})!");
                    Debug.LogWarning("[BiodiversityVolumeSpawner]   Local volumes won't override global! Increase 'Volume Priority' in BiodiversityVolumeSpawner.");
                }
                else
                {
                    Debug.Log($"[BiodiversityVolumeSpawner] ✅ Priority check OK: Global ({vol.priority}) < Local ({volumePriority})");
                }
            }
        }
    }

    void OnDestroy()
    {
        ClearAllVolumes();
    }
}
