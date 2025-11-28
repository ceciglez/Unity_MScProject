using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Comprehensive landuse analyzer for more-than-human urbanism research.
/// Allows configurable rules per landuse type: materials, prefab spawning, and visual coding.
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Landuse Analyzer")]
public class LanduseAnalyzer : GameObjectModifier
{
    [Header("Landuse Rules")]
    [Tooltip("Define rules for different landuse types (park, commercial, water, etc.)")]
    public List<LanduseRule> landuseRules = new List<LanduseRule>();

    [Header("Default Settings")]
    [Tooltip("Material to use if no rule matches")]
    public Material defaultMaterial;
    
    [Tooltip("Default color if no material assigned")]
    public Color defaultColor = Color.gray;

    [Header("Advanced")]
    [Tooltip("Layer mask for terrain raycasting when spawning prefabs")]
    public LayerMask terrainMask = -1;
    
    [Tooltip("Enable debug logs for landuse detection")]
    public bool debugMode = false;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve == null || ve.GameObject == null)
            return;

        // Get the feature properties
        var properties = ve.Feature.Properties;
        
        // Try to get the 'class' property (Mapbox uses 'class' for landuse type)
        string landuseClass = "";
        if (properties.ContainsKey("class"))
        {
            landuseClass = properties["class"].ToString().ToLower();
        }
        else if (properties.ContainsKey("type"))
        {
            landuseClass = properties["type"].ToString().ToLower();
        }

        if (debugMode)
        {
            Debug.Log($"[LanduseAnalyzer] Feature detected - Class: '{landuseClass}', Properties: {string.Join(", ", properties.Keys)}");
        }

        // Find matching rule
        LanduseRule matchingRule = landuseRules.FirstOrDefault(rule => 
            rule.landuseTypes.Any(type => landuseClass.Contains(type.ToLower()))
        );

        if (matchingRule != null)
        {
            // Apply material
            ApplyMaterial(ve.GameObject, matchingRule);

            // Spawn prefabs if configured
            if (matchingRule.spawnPrefabs && matchingRule.prefabs != null && matchingRule.prefabs.Length > 0)
            {
                SpawnPrefabsInFeature(ve, tile, matchingRule);
            }

            if (debugMode)
            {
                Debug.Log($"[LanduseAnalyzer] Applied rule '{matchingRule.ruleName}' to '{landuseClass}'");
            }
        }
        else
        {
            // Apply default material
            ApplyDefaultMaterial(ve.GameObject);
            
            if (debugMode)
            {
                Debug.Log($"[LanduseAnalyzer] No rule matched for '{landuseClass}', using default");
            }
        }
    }

    private void ApplyMaterial(GameObject obj, LanduseRule rule)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        if (rule.material != null)
        {
            renderer.material = rule.material;
        }
        else
        {
            // Create material with specified color
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = rule.visualColor;
            renderer.material = mat;
        }
    }

    private void ApplyDefaultMaterial(GameObject obj)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        if (defaultMaterial != null)
        {
            renderer.material = defaultMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = defaultColor;
            renderer.material = mat;
        }
    }

    private void SpawnPrefabsInFeature(VectorEntity ve, UnityTile tile, LanduseRule rule)
    {
        if (ve.GameObject == null || rule.prefabs.Length == 0)
            return;

        // Calculate spawn count based on feature area
        Bounds bounds = GetFeatureBounds(ve.GameObject);
        float area = bounds.size.x * bounds.size.z;
        int spawnCount = Mathf.CeilToInt(area / 100f * rule.prefabDensityPer100sqm);
        spawnCount = Mathf.Clamp(spawnCount, rule.minPrefabs, rule.maxPrefabs);

        for (int i = 0; i < spawnCount; i++)
        {
            // Pick random prefab
            GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
            
            // Random position within bounds
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // Snap to terrain if enabled
            if (rule.snapToTerrain)
            {
                RaycastHit hit;
                if (Physics.Raycast(randomPos + Vector3.up * 100f, Vector3.down, out hit, 200f, terrainMask))
                {
                    randomPos = hit.point + Vector3.up * rule.heightOffset;
                }
            }

            // Instantiate prefab
            GameObject instance = Instantiate(prefab, randomPos, Quaternion.identity, ve.GameObject.transform);

            // Apply rotation
            if (rule.randomRotation)
            {
                instance.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }

            // Apply scale
            if (rule.randomScale)
            {
                float scale = Random.Range(rule.scaleRange.x, rule.scaleRange.y);
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private Bounds GetFeatureBounds(GameObject featureObj)
    {
        MeshFilter meshFilter = featureObj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            return meshFilter.mesh.bounds;
        }
        
        // Fallback to renderer bounds
        Renderer renderer = featureObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        return new Bounds(featureObj.transform.position, Vector3.one * 100f);
    }
}

/// <summary>
/// Rule configuration for a specific landuse type or category
/// </summary>
[System.Serializable]
public class LanduseRule
{
    [Header("Rule Identity")]
    [Tooltip("Name to identify this rule (e.g., 'Nature Zones', 'Commercial Areas')")]
    public string ruleName = "New Rule";

    [Header("Landuse Filtering")]
    [Tooltip("Landuse types this rule applies to (e.g., 'park', 'forest', 'commercial')")]
    public string[] landuseTypes = new string[] { "park" };

    [Header("Visual Appearance")]
    [Tooltip("Material to apply to this landuse type")]
    public Material material;
    
    [Tooltip("If no material assigned, use this color (for more-than-human friendliness coding)")]
    public Color visualColor = Color.green;

    [Header("Prefab Spawning")]
    [Tooltip("Enable prefab spawning in this landuse type")]
    public bool spawnPrefabs = false;
    
    [Tooltip("Prefabs to spawn (will pick randomly)")]
    public GameObject[] prefabs = new GameObject[0];
    
    [Tooltip("Number of prefabs per 100 square meters")]
    [Range(0.1f, 10f)]
    public float prefabDensityPer100sqm = 1f;
    
    [Tooltip("Minimum prefabs to spawn per feature")]
    [Range(0, 50)]
    public int minPrefabs = 1;
    
    [Tooltip("Maximum prefabs to spawn per feature")]
    [Range(1, 200)]
    public int maxPrefabs = 50;

    [Header("Prefab Positioning")]
    [Tooltip("Snap prefabs to terrain height")]
    public bool snapToTerrain = true;
    
    [Tooltip("Height offset above terrain")]
    public float heightOffset = 0f;
    
    [Tooltip("Random Y rotation")]
    public bool randomRotation = true;
    
    [Tooltip("Random scale variation")]
    public bool randomScale = true;
    
    [Tooltip("Min and max scale multiplier")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
}
