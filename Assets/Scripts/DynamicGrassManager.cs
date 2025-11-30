using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using MicahW.PointGrass;
using System.Collections.Generic;
using System.Linq;

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

        // Initial update
        UpdateSceneFilters();
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
        
        // Filter to only terrain meshes (exclude water, buildings, etc.)
        List<MeshFilter> terrainFilters = new List<MeshFilter>();
        
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
                terrainFilters.Add(mf);
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
            Debug.Log($"[DynamicGrassManager] Updated scene filters: {terrainFilters.Count} terrain meshes");
            foreach (var filter in terrainFilters)
            {
                Debug.Log($"  - {filter.gameObject.name} ({filter.sharedMesh.vertexCount} verts)");
            }
        }
    }
}
