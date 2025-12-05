using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Optimized grass spawner using object pooling and incremental spawning to prevent frame drops.
/// Spawns grass in grid chunks around player for seamless streaming.
/// Now supports multiple grass prefab variations for natural diversity.
/// </summary>
public class OptimizedGrassPatchSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Mapbox AbstractMap component")]
    public AbstractMap map;
    
    [Tooltip("The player transform to track")]
    public Transform player;
    
    [Header("Grass Patch Settings")]
    [Tooltip("Array of grass patch prefabs to randomly choose from")]
    public GameObject[] grassPatchPrefabs;
    
    [Tooltip("Legacy single prefab (will be ignored if array is populated)")]
    public GameObject grassPatchPrefab;
    
    [Tooltip("Grass density - patches per 100 square meters")]
    [Range(0.1f, 5f)]
    public float grassDensity = 1f;
    
    [Tooltip("Radius around player to spawn grass (in meters)")]
    [Range(20f, 200f)]
    public float spawnRadius = 80f;
    
    [Tooltip("Height offset above tile surface")]
    [Range(-1f, 2f)]
    public float heightOffset = 0.05f;
    
    [Tooltip("Align patches to terrain slope")]
    public bool alignToTerrain = true;
    
    [Tooltip("Maximum raycast distance for terrain detection")]
    [Range(50f, 500f)]
    public float raycastDistance = 200f;
    
    [Tooltip("Layer mask for terrain detection (leave 0 for everything)")]
    public LayerMask terrainLayerMask = 0;
    
    [Tooltip("Random rotation for patches")]
    public bool randomRotation = true;
    
    [Tooltip("Random scale variation")]
    [Range(0f, 0.5f)]
    public float scaleVariation = 0.2f;
    
    [Tooltip("Base scale for grass patches")]
    [Range(0.5f, 3f)]
    public float baseScale = 1f;
    
    [Header("Optimization Settings")]
    [Tooltip("Size of each grid chunk (meters)")]
    [Range(10f, 50f)]
    public float chunkSize = 20f;
    
    [Tooltip("Maximum patches to spawn per frame (prevents freezing)")]
    [Range(1, 50)]
    public int patchesPerFrame = 10;
    
    [Tooltip("Initial pool size (pre-instantiated patches)")]
    [Range(50, 500)]
    public int initialPoolSize = 200;
    
    [Tooltip("Update grass position every X seconds")]
    [Range(0.1f, 2f)]
    public float updateInterval = 0.3f;
    
    [Tooltip("Use random seed based on position for consistent placement")]
    public bool deterministicPlacement = true;
    
    [Header("WebGL Optimization")]
    [Tooltip("Reduce quality for WebGL builds")]
    public bool webGLOptimizations = false;
    
    [Tooltip("WebGL-specific grass density multiplier")]
    [Range(0.1f, 1f)]
    public float webGLDensityScale = 0.7f;
    
    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool debugMode = false;
    
    [Tooltip("Show chunk grid gizmos")]
    public bool showChunkGizmos = false;
    
    // Object pools per prefab type
    private Dictionary<int, Queue<GameObject>> grassPools = new Dictionary<int, Queue<GameObject>>();
    private HashSet<GameObject> activeGrass = new HashSet<GameObject>();
    
    // Chunk management
    private Dictionary<Vector2Int, List<GameObject>> activeChunks = new Dictionary<Vector2Int, List<GameObject>>();
    private HashSet<Vector2Int> chunksToSpawn = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> chunksToRemove = new HashSet<Vector2Int>();
    
    // State tracking
    private Vector3 lastPlayerPosition;
    private float lastUpdateTime;
    private Coroutine spawnCoroutine;
    private GameObject poolContainer;
    
    void Start()
    {
        // Auto-find references
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map == null)
            {
                Debug.LogError("[OptimizedGrassSpawner] No AbstractMap found!");
                enabled = false;
                return;
            }
        }
        
        // Validate grass prefabs
        if (!ValidateGrassPrefabs())
        {
            Debug.LogError("[OptimizedGrassSpawner] No valid grass prefabs assigned!");
            enabled = false;
            return;
        }
        
        // Auto-enable WebGL optimizations
#if UNITY_WEBGL && !UNITY_EDITOR
        webGLOptimizations = true;
        if (debugMode)
        {
            Debug.Log("[OptimizedGrassSpawner] WebGL optimizations auto-enabled");
        }
#endif
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                var controller = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
                if (controller != null)
                    player = controller.transform;
            }
            
            if (player == null)
            {
                Debug.LogError("[OptimizedGrassSpawner] No player found!");
                enabled = false;
                return;
            }
        }
        
        if (grassPatchPrefab == null)
        {
            Debug.LogError("[OptimizedGrassSpawner] No grass patch prefab assigned!");
            enabled = false;
            return;
        }
        
        // Create pool container
        poolContainer = new GameObject("GrassPool");
        poolContainer.transform.SetParent(transform);
        
        // Initialize object pool
        StartCoroutine(InitializePool());
        
        lastPlayerPosition = player.position;
        
        if (debugMode)
        {
            Debug.Log($"[OptimizedGrassSpawner] Initialized. Chunk size: {chunkSize}m, Pool size: {initialPoolSize}");
        }
    }
    
    bool ValidateGrassPrefabs()
    {
        // Use array if populated, otherwise fall back to single prefab
        if (grassPatchPrefabs != null && grassPatchPrefabs.Length > 0)
        {
            // Remove null entries
            var validPrefabs = new List<GameObject>();
            foreach (var prefab in grassPatchPrefabs)
            {
                if (prefab != null)
                    validPrefabs.Add(prefab);
            }
            
            if (validPrefabs.Count > 0)
            {
                grassPatchPrefabs = validPrefabs.ToArray();
                if (debugMode)
                {
                    Debug.Log($"[OptimizedGrassSpawner] Using {grassPatchPrefabs.Length} grass prefab variants");
                }
                return true;
            }
        }
        
        // Fall back to single prefab
        if (grassPatchPrefab != null)
        {
            grassPatchPrefabs = new GameObject[] { grassPatchPrefab };
            if (debugMode)
            {
                Debug.Log("[OptimizedGrassSpawner] Using single grass prefab (legacy mode)");
            }
            return true;
        }
        
        return false;
    }

    IEnumerator InitializePool()
    {
        int patchesCreated = 0;
        int patchesPerPrefab = Mathf.Max(1, initialPoolSize / grassPatchPrefabs.Length);
        
        // Create pools for each prefab type
        for (int prefabIndex = 0; prefabIndex < grassPatchPrefabs.Length; prefabIndex++)
        {
            grassPools[prefabIndex] = new Queue<GameObject>();
            
            for (int i = 0; i < patchesPerPrefab; i++)
            {
                GameObject patch = Instantiate(grassPatchPrefabs[prefabIndex], poolContainer.transform);
                patch.SetActive(false);
                
                // Tag the patch with its prefab index for pool management
                var poolTag = patch.GetComponent<GrassPrefabTag>();
                if (poolTag == null)
                {
                    poolTag = patch.AddComponent<GrassPrefabTag>();
                }
                poolTag.prefabIndex = prefabIndex;
                
                grassPools[prefabIndex].Enqueue(patch);
                patchesCreated++;
                
                // Spread creation over multiple frames
                if (patchesCreated >= patchesPerFrame)
                {
                    patchesCreated = 0;
                    yield return null;
                }
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[OptimizedGrassSpawner] Pool initialized with {patchesCreated} patches across {grassPatchPrefabs.Length} prefab types");
        }
        
        // Spawn initial grass around player
        UpdateGrassChunks();
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval)
            return;
        
        lastUpdateTime = Time.time;
        
        // Check if player moved enough to update chunks
        float distanceMoved = Vector3.Distance(player.position, lastPlayerPosition);
        
        if (distanceMoved >= chunkSize * 0.5f) // Update when moved half a chunk
        {
            lastPlayerPosition = player.position;
            UpdateGrassChunks();
        }
    }
    
    void UpdateGrassChunks()
    {
        if (player == null || spawnCoroutine != null)
            return;
        
        // Calculate which chunks should be active
        Vector2Int playerChunk = WorldToChunk(player.position);
        int chunkRadius = Mathf.CeilToInt(spawnRadius / chunkSize);
        
        chunksToSpawn.Clear();
        chunksToRemove.Clear();
        
        // Find chunks that should be active
        HashSet<Vector2Int> desiredChunks = new HashSet<Vector2Int>();
        for (int x = -chunkRadius; x <= chunkRadius; x++)
        {
            for (int z = -chunkRadius; z <= chunkRadius; z++)
            {
                Vector2Int chunk = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                Vector3 chunkCenter = ChunkToWorld(chunk);
                
                // Only include chunks within circular radius
                if (Vector3.Distance(player.position, chunkCenter) <= spawnRadius)
                {
                    desiredChunks.Add(chunk);
                }
            }
        }
        
        // Find chunks to spawn (desired but not active)
        foreach (var chunk in desiredChunks)
        {
            if (!activeChunks.ContainsKey(chunk))
            {
                chunksToSpawn.Add(chunk);
            }
        }
        
        // Find chunks to remove (active but not desired)
        foreach (var chunk in activeChunks.Keys)
        {
            if (!desiredChunks.Contains(chunk))
            {
                chunksToRemove.Add(chunk);
            }
        }
        
        if (debugMode && (chunksToSpawn.Count > 0 || chunksToRemove.Count > 0))
        {
            Debug.Log($"[OptimizedGrassSpawner] Spawning {chunksToSpawn.Count} chunks, Removing {chunksToRemove.Count} chunks");
        }
        
        // Start incremental spawn/despawn
        spawnCoroutine = StartCoroutine(UpdateChunksGradually());
    }
    
    IEnumerator UpdateChunksGradually()
    {
        // Remove old chunks first (frees up pool objects)
        foreach (var chunk in chunksToRemove)
        {
            if (activeChunks.TryGetValue(chunk, out List<GameObject> patches))
            {
                foreach (var patch in patches)
                {
                    ReturnToPool(patch);
                }
                activeChunks.Remove(chunk);
            }
            
            yield return null; // One chunk removal per frame
        }
        
        // Spawn new chunks gradually
        foreach (var chunk in chunksToSpawn)
        {
            yield return StartCoroutine(SpawnChunk(chunk));
        }
        
        spawnCoroutine = null;
    }
    
    IEnumerator SpawnChunk(Vector2Int chunk)
    {
        Vector3 chunkCenter = ChunkToWorld(chunk);
        
        // Calculate patches for this chunk based on density
        float chunkArea = chunkSize * chunkSize;
        float effectiveDensity = grassDensity;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // Apply WebGL optimizations
        if (webGLOptimizations)
        {
            effectiveDensity *= webGLDensityScale;
        }
#endif
        
        int patchCount = Mathf.RoundToInt((chunkArea / 100f) * effectiveDensity);
        
        List<GameObject> chunkPatches = new List<GameObject>();
        
        // Use deterministic random for this chunk
        Random.State oldState = Random.state;
        if (deterministicPlacement)
        {
            int seed = chunk.x * 73856093 ^ chunk.y * 19349663;
            Random.InitState(seed);
        }
        
        int spawnedThisFrame = 0;
        
        for (int i = 0; i < patchCount; i++)
        {
            // Random position within chunk
            float offsetX = Random.Range(-chunkSize * 0.5f, chunkSize * 0.5f);
            float offsetZ = Random.Range(-chunkSize * 0.5f, chunkSize * 0.5f);
            Vector3 worldPos = chunkCenter + new Vector3(offsetX, 0, offsetZ);
            
            // Get grass patch from pool (randomly choose prefab type)
            int prefabIndex = Random.Range(0, grassPatchPrefabs.Length);
            GameObject patch = GetFromPool(prefabIndex);
            if (patch == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[OptimizedGrassSpawner] Pool {prefabIndex} exhausted, creating new patch...");
                patch = Instantiate(grassPatchPrefabs[prefabIndex], poolContainer.transform);
                
                // Tag the new patch
                var poolTag = patch.GetComponent<GrassPrefabTag>();
                if (poolTag == null)
                {
                    poolTag = patch.AddComponent<GrassPrefabTag>();
                }
                poolTag.prefabIndex = prefabIndex;
            }
            
            // Position on terrain with improved ground detection
            Vector3 finalPosition = FindGroundPosition(worldPos);
            patch.transform.position = finalPosition;
            
            // Get surface normal for rotation
            Vector3 surfaceNormal = GetSurfaceNormal(finalPosition);
            
            // Rotation
            if (alignToTerrain)
            {
                Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
                if (randomRotation)
                {
                    Quaternion yRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    patch.transform.rotation = slopeRotation * yRotation;
                }
                else
                {
                    patch.transform.rotation = slopeRotation;
                }
            }
            else if (randomRotation)
            {
                patch.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
            
            // Scale
            float scale = baseScale + Random.Range(-scaleVariation, scaleVariation);
            patch.transform.localScale = Vector3.one * scale;
            
            // Activate
            patch.SetActive(true);
            chunkPatches.Add(patch);
            activeGrass.Add(patch);
            
            spawnedThisFrame++;
            
            // Yield after spawning batch
            if (spawnedThisFrame >= patchesPerFrame)
            {
                spawnedThisFrame = 0;
                yield return null;
            }
        }
        
        if (deterministicPlacement)
        {
            Random.state = oldState;
        }
        
        activeChunks[chunk] = chunkPatches;
        
        if (debugMode)
        {
            Debug.Log($"[OptimizedGrassSpawner] Spawned chunk {chunk} with {patchCount} patches");
        }
    }
    
    GameObject GetFromPool(int prefabIndex)
    {
        if (grassPools.TryGetValue(prefabIndex, out Queue<GameObject> pool) && pool.Count > 0)
        {
            return pool.Dequeue();
        }
        return null;
    }
    
    void ReturnToPool(GameObject patch)
    {
        if (patch == null) return;
        
        // Get the prefab index from the tag
        var poolTag = patch.GetComponent<GrassPrefabTag>();
        int prefabIndex = 0;
        if (poolTag != null)
        {
            prefabIndex = poolTag.prefabIndex;
        }
        
        patch.SetActive(false);
        patch.transform.SetParent(poolContainer.transform);
        
        // Return to appropriate pool
        if (!grassPools.TryGetValue(prefabIndex, out Queue<GameObject> pool))
        {
            grassPools[prefabIndex] = new Queue<GameObject>();
            pool = grassPools[prefabIndex];
        }
        
        pool.Enqueue(patch);
        activeGrass.Remove(patch);
    }
    
    Vector2Int WorldToChunk(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
    
    Vector3 ChunkToWorld(Vector2Int chunk)
    {
        return new Vector3(
            chunk.x * chunkSize + chunkSize * 0.5f,
            player.position.y,
            chunk.y * chunkSize + chunkSize * 0.5f
        );
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;
        
        // Draw spawn radius
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(player.position, spawnRadius);
        
        // Draw chunk grid
        if (showChunkGizmos && Application.isPlaying)
        {
            foreach (var chunk in activeChunks.Keys)
            {
                Vector3 center = ChunkToWorld(chunk);
                Gizmos.color = new Color(1, 1, 0, 0.5f);
                Gizmos.DrawWireCube(center, new Vector3(chunkSize, 0.1f, chunkSize));
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
    
    /// <summary>
    /// Find the ground position using multiple detection strategies
    /// </summary>
    Vector3 FindGroundPosition(Vector3 worldPos)
    {
        // Strategy 1: Raycast from above
        RaycastHit hit;
        Vector3 rayStart = worldPos + Vector3.up * 100f;
        
        if (terrainLayerMask != 0)
        {
            // Use specific layer mask if set
            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, terrainLayerMask))
            {
                return hit.point + Vector3.up * heightOffset;
            }
        }
        else
        {
            // Raycast against everything
            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance))
            {
                return hit.point + Vector3.up * heightOffset;
            }
        }
        
        // Strategy 2: Raycast from below (in case we're inside terrain)
        rayStart = worldPos + Vector3.down * 50f;
        if (Physics.Raycast(rayStart, Vector3.up, out hit, raycastDistance))
        {
            return hit.point + Vector3.up * heightOffset;
        }
        
        // Strategy 3: Multiple raycasts around the position
        Vector3[] offsets = {
            Vector3.zero,
            Vector3.forward * 2f,
            Vector3.back * 2f,
            Vector3.left * 2f,
            Vector3.right * 2f
        };
        
        foreach (Vector3 offset in offsets)
        {
            Vector3 testPos = worldPos + offset;
            rayStart = testPos + Vector3.up * 100f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance))
            {
                return hit.point + Vector3.up * heightOffset;
            }
        }
        
        // Strategy 4: Use map tiles as fallback
        if (map != null)
        {
            // Try to get height from map tiles
            Vector3 tilePos = map.transform.InverseTransformPoint(worldPos);
            UnityTile[] tiles = map.GetComponentsInChildren<UnityTile>();
            
            foreach (UnityTile tile in tiles)
            {
                Vector3 tileSize = new Vector3((float)tile.Rect.Size.x, 10f, (float)tile.Rect.Size.y);
                Bounds tileBounds = new Bounds(tile.transform.position, tileSize);
                if (tileBounds.Contains(worldPos))
                {
                    // Use tile surface height
                    return new Vector3(worldPos.x, tile.transform.position.y + heightOffset, worldPos.z);
                }
            }
        }
        
        // Final fallback: Use original position with slight adjustment
        if (debugMode)
        {
            Debug.LogWarning($"[OptimizedGrassSpawner] Could not find ground at {worldPos}, using fallback position");
        }
        
        return new Vector3(worldPos.x, worldPos.y + heightOffset, worldPos.z);
    }
    
    /// <summary>
    /// Get surface normal for terrain alignment
    /// </summary>
    Vector3 GetSurfaceNormal(Vector3 position)
    {
        RaycastHit hit;
        Vector3 rayStart = position + Vector3.up * 5f;
        
        if (terrainLayerMask != 0)
        {
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, terrainLayerMask))
            {
                return hit.normal;
            }
        }
        else
        {
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f))
            {
                return hit.normal;
            }
        }
        
        return Vector3.up; // Default normal
    }
}

/// <summary>
/// Simple component to tag grass patches with their prefab index for pool management
/// </summary>
public class GrassPrefabTag : MonoBehaviour
{
    public int prefabIndex;
}
