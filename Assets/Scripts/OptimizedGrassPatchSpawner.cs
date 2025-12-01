using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Optimized grass spawner using object pooling and incremental spawning to prevent frame drops.
/// Spawns grass in grid chunks around player for seamless streaming.
/// </summary>
public class OptimizedGrassPatchSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Mapbox AbstractMap component")]
    public AbstractMap map;
    
    [Tooltip("The player transform to track")]
    public Transform player;
    
    [Header("Grass Patch Settings")]
    [Tooltip("Grass patch prefab to spawn (e.g., GrassPatch_10m from demo)")]
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
    
    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool debugMode = false;
    
    [Tooltip("Show chunk grid gizmos")]
    public bool showChunkGizmos = false;
    
    // Object pool
    private Queue<GameObject> grassPool = new Queue<GameObject>();
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
    
    IEnumerator InitializePool()
    {
        int patchesCreated = 0;
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject patch = Instantiate(grassPatchPrefab, poolContainer.transform);
            patch.SetActive(false);
            grassPool.Enqueue(patch);
            
            patchesCreated++;
            
            // Spread creation over multiple frames
            if (patchesCreated >= patchesPerFrame)
            {
                patchesCreated = 0;
                yield return null;
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[OptimizedGrassSpawner] Pool initialized with {initialPoolSize} patches");
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
        int patchCount = Mathf.RoundToInt((chunkArea / 100f) * grassDensity);
        
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
            
            // Get grass patch from pool
            GameObject patch = GetFromPool();
            if (patch == null)
            {
                if (debugMode)
                    Debug.LogWarning("[OptimizedGrassSpawner] Pool exhausted, expanding...");
                patch = Instantiate(grassPatchPrefab, poolContainer.transform);
            }
            
            // Position on terrain
            RaycastHit hit;
            Vector3 surfaceNormal = Vector3.up;
            float surfaceHeight = worldPos.y;
            
            if (Physics.Raycast(worldPos + Vector3.up * 100f, Vector3.down, out hit, 200f))
            {
                surfaceHeight = hit.point.y;
                surfaceNormal = hit.normal;
            }
            
            worldPos.y = surfaceHeight + heightOffset;
            patch.transform.position = worldPos;
            
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
    
    GameObject GetFromPool()
    {
        if (grassPool.Count > 0)
        {
            return grassPool.Dequeue();
        }
        return null;
    }
    
    void ReturnToPool(GameObject patch)
    {
        if (patch == null) return;
        
        patch.SetActive(false);
        patch.transform.SetParent(poolContainer.transform);
        grassPool.Enqueue(patch);
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
}
