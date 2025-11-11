using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Components;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns random nature assets (trees, rocks, vegetation) on polygon features
/// Works with parks, forests, and other natural areas from Mapbox data
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Nature Polygon Modifier")]
public class NaturePolygonModifier : GameObjectModifier
{
    [Header("Asset Settings")]
    [Tooltip("Prefabs to spawn randomly on polygons")]
    public GameObject[] naturePrefabs;
    
    [Header("Spawn Density")]
    [Tooltip("How many assets per square meter")]
    [Range(0.001f, 0.5f)]
    public float density = 0.05f;
    
    [Tooltip("Minimum distance between spawned assets")]
    public float minSpacing = 2f;
    
    [Header("Randomization")]
    [Tooltip("Random rotation on Y axis")]
    public bool randomRotation = true;
    
    [Tooltip("Random scale variation (0 = no variation, 1 = 0-200% scale)")]
    [Range(0f, 1f)]
    public float scaleVariation = 0.3f;
    
    [Header("Positioning")]
    [Tooltip("Height offset from ground")]
    public float heightOffset = 0f;
    
    [Tooltip("Use raycast to snap to terrain below")]
    public bool snapToTerrain = true;
    
    [Tooltip("Layers to raycast against for terrain")]
    public LayerMask terrainLayers = -1;
    
    [Header("Optimization")]
    [Tooltip("Maximum assets to spawn per polygon (0 = unlimited)")]
    public int maxAssetsPerPolygon = 100;
    
    [Tooltip("Parent all spawned objects under this transform")]
    public Transform parentTransform;
    
    private System.Random _random;
    
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        // Safety checks
        if (ve == null)
        {
            Debug.LogError("NaturePolygonModifier: VectorEntity is null!");
            return;
        }
        
        if (ve.GameObject == null)
        {
            Debug.LogError("NaturePolygonModifier: VectorEntity GameObject is null!");
            return;
        }
        
        if (tile == null)
        {
            Debug.LogError("NaturePolygonModifier: UnityTile is null!");
            return;
        }
        
        // Check if we have prefabs to spawn
        if (naturePrefabs == null || naturePrefabs.Length == 0)
        {
            Debug.LogWarning($"NaturePolygonModifier: No nature prefabs assigned for feature {ve.Feature?.Data?.Id}!");
            return;
        }
        
        if (ve.Feature == null)
        {
            Debug.LogError("NaturePolygonModifier: VectorEntity.Feature is null!");
            return;
        }
        
        // Initialize random with feature ID for consistency
        try
        {
            _random = new System.Random(ve.Feature.Data.Id.GetHashCode());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NaturePolygonModifier: Error creating random generator: {e.Message}");
            _random = new System.Random();
        }
        
        // Get all vertices from the polygon
        if (ve.Feature.Points == null || ve.Feature.Points.Count == 0)
        {
            Debug.LogWarning($"NaturePolygonModifier: No points in feature {ve.Feature.Data.Id}");
            return;
        }
        
        // Process each polygon ring (outer + holes)
        try
        {
            foreach (var ring in ve.Feature.Points)
            {
                if (ring == null || ring.Count < 3) continue; // Need at least 3 points for a polygon
                
                SpawnAssetsInPolygon(ring, tile, ve);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NaturePolygonModifier: Error spawning assets: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void SpawnAssetsInPolygon(List<Vector3> polygonPoints, UnityTile tile, VectorEntity ve)
    {
        // Calculate bounding box
        Vector3 min = polygonPoints[0];
        Vector3 max = polygonPoints[0];
        
        foreach (var point in polygonPoints)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        
        // Calculate area (approximate)
        float width = max.x - min.x;
        float length = max.z - min.z;
        float area = width * length;
        
        // Calculate number of assets to spawn
        int assetCount = Mathf.RoundToInt(area * density);
        
        if (maxAssetsPerPolygon > 0)
        {
            assetCount = Mathf.Min(assetCount, maxAssetsPerPolygon);
        }
        
        if (assetCount == 0) return;
        
        // Track spawned positions for spacing check
        List<Vector3> spawnedPositions = new List<Vector3>();
        
        // Attempt to spawn assets
        int attempts = 0;
        int maxAttempts = assetCount * 10; // Allow multiple attempts per asset
        
        while (spawnedPositions.Count < assetCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Generate random point in bounding box
            float randomX = (float)_random.NextDouble() * width + min.x;
            float randomZ = (float)_random.NextDouble() * length + min.z;
            Vector3 testPoint = new Vector3(randomX, 0, randomZ);
            
            // Check if point is inside polygon
            if (!IsPointInPolygon(testPoint, polygonPoints))
                continue;
            
            // Check spacing from other spawned assets
            bool tooClose = false;
            foreach (var spawnedPos in spawnedPositions)
            {
                float distance = Vector3.Distance(new Vector3(testPoint.x, 0, testPoint.z), 
                                                   new Vector3(spawnedPos.x, 0, spawnedPos.z));
                if (distance < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (tooClose) continue;
            
            // Valid position found, spawn asset
            SpawnAssetAt(testPoint, ve.GameObject);
            spawnedPositions.Add(testPoint);
        }
    }
    
    private void SpawnAssetAt(Vector3 localPosition, GameObject parentGameObject)
    {
        try
        {
            // Safety check
            if (parentGameObject == null)
            {
                Debug.LogError("NaturePolygonModifier: Parent GameObject is null!");
                return;
            }
            
            // Pick random prefab
            GameObject prefab = naturePrefabs[_random.Next(naturePrefabs.Length)];
            
            if (prefab == null)
            {
                Debug.LogWarning("NaturePolygonModifier: Selected prefab is null!");
                return;
            }
            
            // Calculate world position
            Vector3 worldPosition = parentGameObject.transform.TransformPoint(localPosition);
            worldPosition.y += heightOffset;
            
            // Snap to terrain if enabled
            if (snapToTerrain)
            {
                RaycastHit hit;
                Vector3 rayStart = worldPosition + Vector3.up * 100f;
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, terrainLayers))
                {
                    worldPosition.y = hit.point.y + heightOffset;
                }
            }
            
            // Random rotation
            Quaternion rotation = Quaternion.identity;
            if (randomRotation)
            {
                float randomAngle = (float)_random.NextDouble() * 360f;
                rotation = Quaternion.Euler(0, randomAngle, 0);
            }
            
            // Instantiate
            GameObject instance = GameObject.Instantiate(prefab, worldPosition, rotation);
            
            if (instance == null)
            {
                Debug.LogError($"NaturePolygonModifier: Failed to instantiate prefab {prefab.name}!");
                return;
            }
            
            // Random scale
            if (scaleVariation > 0)
            {
                float scaleMultiplier = 1f + ((float)_random.NextDouble() * 2f - 1f) * scaleVariation;
                instance.transform.localScale *= scaleMultiplier;
            }
            
            // Parent
            if (parentTransform != null)
            {
                instance.transform.SetParent(parentTransform);
            }
            else
            {
                instance.transform.SetParent(parentGameObject.transform);
            }
            
            // Name for debugging
            instance.name = $"{prefab.name}_Generated";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NaturePolygonModifier: Error spawning asset: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Point-in-polygon test using ray casting algorithm
    /// </summary>
    private bool IsPointInPolygon(Vector3 point, List<Vector3> polygon)
    {
        int intersections = 0;
        int vertexCount = polygon.Count;
        
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 v1 = polygon[i];
            Vector3 v2 = polygon[(i + 1) % vertexCount];
            
            // Check if ray from point to the right intersects edge
            if ((v1.z > point.z) != (v2.z > point.z))
            {
                float xIntersection = (v2.x - v1.x) * (point.z - v1.z) / (v2.z - v1.z) + v1.x;
                if (point.x < xIntersection)
                {
                    intersections++;
                }
            }
        }
        
        // Odd number of intersections = inside polygon
        return (intersections % 2) == 1;
    }
}
