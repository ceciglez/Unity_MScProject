using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;

/// <summary>
/// Spawns grass patch prefabs on the tile where the player is standing.
/// Mimics the Stylized Grass Shader demo scene approach.
/// </summary>
public class GrassPatchSpawner : MonoBehaviour
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
    [Range(5f, 200f)]
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
    
    [Header("Performance")]
    [Tooltip("Update grass position every X seconds")]
    [Range(0.1f, 5f)]
    public float updateInterval = 0.5f;
    
    [Tooltip("Distance player must move before refreshing grass patches")]
    [Range(10f, 100f)]
    public float updateDistance = 40f;
    
    [Tooltip("Use random seed based on position for consistent patch placement")]
    public bool deterministicPlacement = true;
    
    [Tooltip("Remove old grass patches when player moves")]
    public bool cleanupOldPatches = true;
    
    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool debugMode = false;
    
    private UnityTile currentTile;
    private GameObject currentGrassContainer;
    private float lastUpdateTime;
    private Vector3 lastPlayerPosition;
    
    void Start()
    {
        // Auto-find references if not assigned
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map == null)
            {
                Debug.LogError("[GrassPatchSpawner] No AbstractMap found in scene!");
                enabled = false;
                return;
            }
        }
        
        if (player == null)
        {
            // Try to find player by tag
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                // Try to find ExampleCharacterController
                var controller = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
                if (controller != null)
                {
                    player = controller.transform;
                }
            }
            
            if (player == null)
            {
                Debug.LogError("[GrassPatchSpawner] No player found! Tag a GameObject as 'Player' or ensure ExampleCharacterController exists.");
                enabled = false;
                return;
            }
        }
        
        if (grassPatchPrefab == null)
        {
            Debug.LogError("[GrassPatchSpawner] No grass patch prefab assigned!");
            enabled = false;
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"[GrassPatchSpawner] Setup complete. Density: {grassDensity}, Radius: {spawnRadius}m");
        }
        
        // Initialize last player position
        lastPlayerPosition = player.position;
    }
    
    void Update()
    {
        // Throttle updates
        if (Time.time - lastUpdateTime < updateInterval)
            return;
            
        lastUpdateTime = Time.time;
        
        UpdateGrassForPlayerTile();
    }
    
    void UpdateGrassForPlayerTile()
    {
        if (player == null || map == null)
            return;
            
        // Get tile at player position
        UnityTile playerTile = GetTileAtPosition(player.position);
        
        if (playerTile == null)
        {
            if (debugMode)
                Debug.Log("[GrassPatchSpawner] Player not on a loaded tile");
            return;
        }
        
        // Check if player moved significantly or changed tiles
        float distanceMoved = Vector3.Distance(player.position, lastPlayerPosition);
        bool playerMovedEnough = distanceMoved >= updateDistance;
        bool changedTile = playerTile != currentTile;
        
        if (playerMovedEnough || changedTile)
        {
            if (debugMode)
                Debug.Log($"[GrassPatchSpawner] Updating grass - Moved: {distanceMoved:F1}m, Changed tile: {changedTile}");
            
            // Clean up old grass if enabled
            if (cleanupOldPatches && currentGrassContainer != null)
            {
                Destroy(currentGrassContainer);
            }
            
            currentTile = playerTile;
            lastPlayerPosition = player.position;
            SpawnGrassOnTile(playerTile);
        }
    }
    
    UnityTile GetTileAtPosition(Vector3 position)
    {
        // Get all tiles
        UnityTile[] tiles = map.GetComponentsInChildren<UnityTile>();
        
        foreach (UnityTile tile in tiles)
        {
            // Check if position is within tile bounds
            Bounds bounds = GetTileBounds(tile);
            if (bounds.Contains(position))
            {
                return tile;
            }
        }
        
        return null;
    }
    
    Bounds GetTileBounds(UnityTile tile)
    {
        // Get tile mesh bounds
        MeshFilter meshFilter = tile.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds bounds = meshFilter.sharedMesh.bounds;
            // Transform to world space
            bounds.center = tile.transform.TransformPoint(bounds.center);
            bounds.extents = Vector3.Scale(bounds.extents, tile.transform.lossyScale);
            return bounds;
        }
        
        // Fallback: use tile transform position with default size
        return new Bounds(tile.transform.position, Vector3.one * 100f);
    }
    
    void SpawnGrassOnTile(UnityTile tile)
    {
        if (tile == null || grassPatchPrefab == null || player == null)
            return;
        
        // Create container for this tile's grass
        currentGrassContainer = new GameObject($"GrassPatches_Around_Player");
        currentGrassContainer.transform.SetParent(tile.transform, false);
        
        MeshFilter tileMesh = tile.GetComponent<MeshFilter>();
        
        // Calculate number of patches based on density and area
        float area = Mathf.PI * spawnRadius * spawnRadius;
        int patchCount = Mathf.RoundToInt((area / 100f) * grassDensity);
        
        if (debugMode)
            Debug.Log($"[GrassPatchSpawner] Spawning {patchCount} grass patches (density: {grassDensity})");
        
        // Use deterministic random seed based on player position if enabled
        Random.State oldState = Random.state;
        if (deterministicPlacement)
        {
            int seed = GetPositionSeed(player.position);
            Random.InitState(seed);
        }
        
        // Spawn grass patches around player
        for (int i = 0; i < patchCount; i++)
        {
            // Random position within spawn radius around player
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            
            Vector3 worldPos = player.position + randomOffset;
            
            // Raycast down to get exact height and normal on tile
            RaycastHit hit;
            Vector3 surfaceNormal = Vector3.up;
            float surfaceHeight = player.position.y;
            
            if (Physics.Raycast(worldPos + Vector3.up * 100f, Vector3.down, out hit, 200f))
            {
                surfaceHeight = hit.point.y;
                surfaceNormal = hit.normal;
            }
            else
            {
                // Fallback to old method
                surfaceHeight = GetSurfaceHeight(worldPos, tile, tileMesh);
            }
            
            worldPos.y = surfaceHeight + heightOffset;
            
            // Instantiate grass patch
            GameObject patch = Instantiate(grassPatchPrefab, worldPos, Quaternion.identity, currentGrassContainer.transform);
            
            // Align to terrain if enabled
            if (alignToTerrain)
            {
                // Rotate to match terrain normal
                Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
                
                // Add random Y rotation
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
            
            // Random scale variation
            float scale = baseScale + Random.Range(-scaleVariation, scaleVariation);
            patch.transform.localScale = Vector3.one * scale;
        }
        
        // Restore random state
        if (deterministicPlacement)
        {
            Random.state = oldState;
        }
        
        if (debugMode)
            Debug.Log($"[GrassPatchSpawner] Spawned {patchCount} patches successfully");
    }
    
    int GetPositionSeed(Vector3 position)
    {
        // Create consistent seed based on grid position
        int gridX = Mathf.FloorToInt(position.x / updateDistance);
        int gridZ = Mathf.FloorToInt(position.z / updateDistance);
        return gridX * 73856093 ^ gridZ * 19349663;
    }
    
    float GetSurfaceHeight(Vector3 worldPos, UnityTile tile, MeshFilter meshFilter)
    {
        // Raycast down to get height
        RaycastHit hit;
        if (Physics.Raycast(worldPos + Vector3.up * 100f, Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }
        
        // Fallback: use tile center height
        return tile.transform.position.y;
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;
        
        // Draw spawn radius around player
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(player.position, spawnRadius);
        
        // Draw tile bounds if player is on a tile
        if (map != null)
        {
            UnityTile playerTile = GetTileAtPosition(player.position);
            if (playerTile != null)
            {
                Gizmos.color = new Color(1, 1, 0, 0.5f);
                Bounds bounds = GetTileBounds(playerTile);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
