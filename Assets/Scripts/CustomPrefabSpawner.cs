using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enhanced custom modifier to spawn prefabs with per-prefab configuration.
/// Each prefab can have its own density, scale, rotation, and spawn settings.
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Custom Prefab Spawner")]
public class CustomPrefabSpawner : GameObjectModifier
{
    [Header("Landuse Filtering")]
    [Tooltip("Only spawn in these landuse types (leave empty to spawn in all features)")]
    public string[] allowedLanduseTypes = new string[] { "park", "forest", "wood", "recreation_ground" };
    
    [Header("Prefab Configuration")]
    [Tooltip("Configure each prefab with individual spawn settings")]
    public List<PrefabSpawnRule> prefabRules = new List<PrefabSpawnRule>();
    
    [Header("Global Position Settings")]
    [Tooltip("Snap to terrain height")]
    public bool snapToTerrain = true;
    
    [Header("Advanced")]
    [Tooltip("Layer mask for terrain raycasting")]
    public LayerMask terrainMask = -1;
    
    [Tooltip("Enable debug logs")]
    public bool debugMode = false;
    
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (prefabRules == null || prefabRules.Count == 0)
        {
            if (debugMode)
                Debug.LogWarning("CustomPrefabSpawner: No prefab rules configured!");
            return;
        }
        
        // Check landuse type filtering
        if (allowedLanduseTypes != null && allowedLanduseTypes.Length > 0)
        {
            string landuseClass = GetLanduseType(ve);
            
            bool isAllowed = allowedLanduseTypes.Any(type => 
                landuseClass.ToLower().Contains(type.ToLower())
            );
            
            if (!isAllowed)
            {
                if (debugMode)
                {
                    Debug.Log($"[CustomPrefabSpawner] Skipping feature '{landuseClass}' - not in allowed list");
                }
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"[CustomPrefabSpawner] ✓ Feature '{landuseClass}' matches filter");
            }
        }
        
        // Get feature bounds for positioning
        Bounds bounds = GetFeatureBounds(ve.GameObject);
        float featureArea = bounds.size.x * bounds.size.z;
        
        if (debugMode)
        {
            Debug.Log($"[CustomPrefabSpawner] Processing feature with area: {featureArea:F2} sqm, Bounds: {bounds.size}");
        }
        
        // Process each prefab rule
        foreach (var rule in prefabRules)
        {
            if (rule.prefab == null || !rule.enabled)
                continue;
            
            int spawnCount = CalculateSpawnCount(rule, featureArea);
            
            if (debugMode)
            {
                Debug.Log($"[CustomPrefabSpawner] Spawning {spawnCount}x '{rule.prefab.name}' (density: {rule.density})");
            }
            
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnPrefab(ve, rule, bounds);
            }
        }
    }
    
    private string GetLanduseType(VectorEntity ve)
    {
        var properties = ve.Feature.Properties;
        
        if (debugMode)
        {
            // Log ALL properties to see what's available
            string allProps = string.Join(", ", properties.Keys.Select(k => $"{k}={properties[k]}"));
            Debug.Log($"[CustomPrefabSpawner] Feature properties: {allProps}");
        }
        
        // Try 'class' first (most common in Mapbox)
        if (properties.ContainsKey("class"))
        {
            return properties["class"].ToString();
        }
        
        // Try 'type' as fallback
        if (properties.ContainsKey("type"))
        {
            return properties["type"].ToString();
        }
        
        // Try 'landuse' as another fallback
        if (properties.ContainsKey("landuse"))
        {
            return properties["landuse"].ToString();
        }
        
        return "unknown";
    }
    
    private int CalculateSpawnCount(PrefabSpawnRule rule, float area)
    {
        int count = Mathf.RoundToInt(area / 100f * rule.density);
        return Mathf.Clamp(count, rule.minCount, rule.maxCount);
    }
    
    private void SpawnPrefab(VectorEntity ve, PrefabSpawnRule rule, Bounds bounds)
    {
        // Calculate random position within bounds
        Vector3 localPos = new Vector3(
            Random.Range(-bounds.extents.x, bounds.extents.x) + Random.Range(-rule.positionOffset.x, rule.positionOffset.x),
            0,
            Random.Range(-bounds.extents.z, bounds.extents.z) + Random.Range(-rule.positionOffset.y, rule.positionOffset.y)
        );
        
        Vector3 worldPos = ve.GameObject.transform.TransformPoint(localPos);
        
        // Snap to terrain if enabled
        if (snapToTerrain)
        {
            RaycastHit hit;
            if (Physics.Raycast(worldPos + Vector3.up * 1000f, Vector3.down, out hit, 2000f, terrainMask))
            {
                worldPos = hit.point + Vector3.up * rule.heightOffset;
            }
        }
        
        // Calculate rotation
        Quaternion rotation;
        if (rule.randomRotation)
        {
            rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }
        else
        {
            rotation = Quaternion.Euler(rule.fixedRotation);
        }
        
        // Instantiate
        GameObject instance = Instantiate(rule.prefab, worldPos, rotation);
        instance.transform.SetParent(ve.GameObject.transform);
        
        // Apply random scale
        if (rule.randomScale)
        {
            float scale = Random.Range(rule.scaleRange.x, rule.scaleRange.y);
            instance.transform.localScale = rule.prefab.transform.localScale * scale;
        }
    }
    
    private Bounds GetFeatureBounds(GameObject featureObj)
    {
        MeshFilter meshFilter = featureObj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            return meshFilter.mesh.bounds;
        }
        
        Renderer renderer = featureObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        return new Bounds(featureObj.transform.position, Vector3.one * 100f);
    }
}

/// <summary>
/// Configuration for spawning a specific prefab type
/// </summary>
[System.Serializable]
public class PrefabSpawnRule
{
    [Header("Identity")]
    [Tooltip("Name to identify this rule (e.g., 'Oak Trees', 'Pine Trees', 'Bushes')")]
    public string ruleName = "New Prefab";
    
    [Tooltip("Enable/disable this prefab spawn rule")]
    public bool enabled = true;
    
    [Header("Prefab")]
    [Tooltip("The prefab to spawn")]
    public GameObject prefab;
    
    [Header("Spawn Density")]
    [Tooltip("Number of prefabs per 100 square meters")]
    [Range(0.1f, 20f)]
    public float density = 1f;
    
    [Tooltip("Minimum prefabs to spawn per feature")]
    [Range(0, 50)]
    public int minCount = 1;
    
    [Tooltip("Maximum prefabs to spawn per feature")]
    [Range(1, 500)]
    public int maxCount = 100;
    
    [Header("Position")]
    [Tooltip("Random position offset within feature bounds")]
    public Vector2 positionOffset = new Vector2(5f, 5f);
    
    [Tooltip("Height offset above terrain")]
    public float heightOffset = 0f;
    
    [Header("Rotation")]
    [Tooltip("Random Y rotation")]
    public bool randomRotation = true;
    
    [Tooltip("If not random, use this rotation")]
    public Vector3 fixedRotation = Vector3.zero;
    
    [Header("Scale")]
    [Tooltip("Random scale variation")]
    public bool randomScale = true;
    
    [Tooltip("Min and max scale multiplier")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
}
