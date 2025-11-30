using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using MicahW.PointGrass;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Automatically updates Point Grass Renderer's scene filters when using Scene Filters distribution mode
/// </summary>
public class DynamicGrassManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The AbstractMap component")]
    public AbstractMap map;
    
    [Tooltip("The PointGrassRenderer component (should be on this GameObject)")]
    public PointGrassRenderer grassRenderer;
    
    [Tooltip("The player transform (to limit grass rendering to nearby tiles)")]
    public Transform player;

    [Header("Grass Settings")]
    [Tooltip("Number of grass points to generate")]
    [Range(1000, 50000)]
    public int pointCount = 10000;
    
    [Tooltip("Multiply point count by mesh surface area")]
    public bool multiplyByArea = true;
    
    [Tooltip("Only render grass on tiles within this distance from player (meters)")]
    [Range(50f, 500f)]
    public float grassRenderDistance = 150f;

    [Header("Update Settings")]
    [Tooltip("How often to refresh scene filters (seconds)")]
    [Range(0.5f, 5f)]
    public float updateInterval = 2f;
    
    [Tooltip("Only include meshes with this many vertices or more")]
    [Range(10, 1000)]
    public int minVertexCount = 100;

    [Header("Debug")]
    public bool debugMode = false;

    private float lastUpdateTime;
    private HashSet<MeshFilter> currentFilters = new HashSet<MeshFilter>();

    void Start()
    {
        // Auto-find references if not set
        if (map == null)
            map = FindObjectOfType<AbstractMap>();

        if (grassRenderer == null)
            grassRenderer = GetComponent<PointGrassRenderer>();
            
        if (player == null)
        {
            // Try to find player by tag or ExampleCharacterController
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                var characterController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
                if (characterController != null)
                    playerObj = characterController.gameObject;
            }
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("[DynamicGrassManager] No player found. Grass will render on all tiles (may cause performance issues)");
        }

        if (grassRenderer == null)
        {
            Debug.LogError("[DynamicGrassManager] No PointGrassRenderer component found! Please add one to this GameObject.");
            enabled = false;
            return;
        }

        if (grassRenderer.distSource != PointGrassCommon.DistributionSource.SceneFilters)
        {
            Debug.LogWarning("[DynamicGrassManager] PointGrassRenderer is not set to Scene Filters distribution mode!");
        }

        // Subscribe to tile events for immediate updates
        if (map != null)
        {
            map.OnInitialized += OnMapInitialized;
            map.OnTileFinished += OnTileFinished;
        }

        // Initial update with slight delay to allow tiles to generate
        Invoke("UpdateSceneFilters", 1f);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (map != null)
        {
            map.OnInitialized -= OnMapInitialized;
            map.OnTileFinished -= OnTileFinished;
        }
    }

    void OnMapInitialized()
    {
        if (debugMode)
            Debug.Log("[DynamicGrassManager] Map initialized - updating scene filters");
        UpdateSceneFilters();
    }

    void OnTileFinished(UnityTile tile)
    {
        if (debugMode)
            Debug.Log($"[DynamicGrassManager] Tile finished: {tile.name} - updating scene filters");
        // Small delay to ensure tile mesh is fully ready
        Invoke("UpdateSceneFilters", 0.1f);
    }

    void Update()
    {
        if (map == null || grassRenderer == null)
            return;

        // Check if enough time has passed
        if (Time.time - lastUpdateTime < updateInterval)
            return;

        UpdateSceneFilters();
        lastUpdateTime = Time.time;
    }

    private void UpdateSceneFilters()
    {
        // Get all mesh filters from tiles
        MeshFilter[] allFilters = map.GetComponentsInChildren<MeshFilter>();
        
        // Filter to only terrain meshes near player
        List<MeshFilter> terrainFilters = new List<MeshFilter>();
        Vector3 playerPos = player != null ? player.position : Vector3.zero;
        
        foreach (MeshFilter mf in allFilters)
        {
            // Skip if no mesh or too few vertices
            if (mf.sharedMesh == null || mf.sharedMesh.vertexCount < minVertexCount)
                continue;
            
            // Skip if it's a grass mesh itself or other non-terrain objects
            if (mf.gameObject == gameObject)
                continue;
            
            // Check if it's part of a tile
            UnityTile tile = mf.GetComponentInParent<UnityTile>();
            if (tile != null)
            {
                // Only add if within render distance of player
                if (player == null || Vector3.Distance(mf.transform.position, playerPos) <= grassRenderDistance)
                {
                    terrainFilters.Add(mf);
                }
            }
        }

        // Check if the list has changed
        HashSet<MeshFilter> newFilters = new HashSet<MeshFilter>(terrainFilters);
        if (newFilters.SetEquals(currentFilters))
        {
            if (debugMode)
                Debug.Log($"[DynamicGrassManager] No changes to scene filters ({currentFilters.Count} filters)");
            return;
        }

        // Update the grass renderer's scene filters
        grassRenderer.sceneFilters = terrainFilters.ToArray();
        currentFilters = newFilters;

        if (debugMode)
        {
            Debug.Log($"[DynamicGrassManager] Updated scene filters: {terrainFilters.Count} terrain meshes (within {grassRenderDistance}m of player)");
            foreach (var filter in terrainFilters)
            {
                float dist = player != null ? Vector3.Distance(filter.transform.position, playerPos) : 0f;
                Debug.Log($"  - {filter.gameObject.name} ({filter.sharedMesh.vertexCount} verts, {dist:F1}m away)");
            }
        }
        
        // Trigger rebuild via coroutine - but only if we have a reasonable number of filters
        if (terrainFilters.Count > 0 && terrainFilters.Count <= 9)
        {
            StartCoroutine(RebuildGrassWithDelay());
        }
        else if (terrainFilters.Count > 9)
        {
            Debug.LogWarning($"[DynamicGrassManager] Too many tiles ({terrainFilters.Count}) for grass rendering. Reduce grassRenderDistance or increase minVertexCount.");
        }
        
        // Debug grass renderer state
        DebugGrassRendererState();
    }
    
    private System.Collections.IEnumerator RebuildGrassWithDelay()
    {
        // Wait a frame to ensure scene filters array is properly set
        yield return null;
        
        // Toggle the component to trigger OnDisable/OnEnable
        if (grassRenderer != null && grassRenderer.enabled)
        {
            if (debugMode)
                Debug.Log("[DynamicGrassManager] Rebuilding grass renderer...");
                
            grassRenderer.enabled = false;
            yield return null;
            grassRenderer.enabled = true;
            
            if (debugMode)
                Debug.Log("[DynamicGrassManager] Grass renderer rebuilt");
        }
    }
    
    private void DebugGrassRendererState()
    {
        if (!debugMode)
            return;
            
        Debug.Log("=== GRASS RENDERER DEBUG ===");
        Debug.Log($"Distribution Source: {grassRenderer.distSource}");
        Debug.Log($"Scene Filters Count: {(grassRenderer.sceneFilters != null ? grassRenderer.sceneFilters.Length : 0)}");
        Debug.Log($"Point Count: {grassRenderer.pointCount}");
        Debug.Log($"Multiply By Area: {grassRenderer.multiplyByArea}");
        Debug.Log($"Blade Type: {grassRenderer.bladeType}");
        Debug.Log($"Material: {(grassRenderer.material != null ? grassRenderer.material.name : "NULL")}");
        Debug.Log($"Blade Mesh: {(grassRenderer.grassBladeMesh != null ? grassRenderer.grassBladeMesh.name : "NULL")}");
        
        if (grassRenderer.material != null)
        {
            Debug.Log($"Material Shader: {grassRenderer.material.shader.name}");
        }
        
        if (grassRenderer.sceneFilters != null && grassRenderer.sceneFilters.Length > 0)
        {
            Debug.Log($"First filter mesh vertices: {grassRenderer.sceneFilters[0].sharedMesh.vertexCount}");
            Debug.Log($"First filter position: {grassRenderer.sceneFilters[0].transform.position}");
            Debug.Log($"First filter active: {grassRenderer.sceneFilters[0].gameObject.activeInHierarchy}");
        }
        
        // Check if renderer is actually enabled
        var rendererBehavior = grassRenderer as MonoBehaviour;
        if (rendererBehavior != null)
        {
            Debug.Log($"PointGrassRenderer enabled: {rendererBehavior.enabled}");
            Debug.Log($"GameObject active: {rendererBehavior.gameObject.activeInHierarchy}");
        }
        
        Debug.Log("=== END DEBUG ===");
    }
}
