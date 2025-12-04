using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using StylizedGrass;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Advanced grass modifier with landuse-based variations.
/// Supports different grass materials, densities, colors, and prefab spawning per landuse type.
/// Integrates with Stylized Grass Shader colormap system.
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Advanced Grass Landuse Modifier")]
public class AdvancedGrassLanduseModifier : GameObjectModifier
{
    [Header("Grass Configuration")]
    [Tooltip("Grass variation rules per landuse type")]
    public List<GrassLanduseRule> grassRules = new List<GrassLanduseRule>();

    [Header("Default Settings")]
    [Tooltip("Default grass material if no rule matches")]
    public Material defaultGrassMaterial;
    
    [Tooltip("Default grass scale")]
    [Range(0.5f, 3f)]
    public float defaultScale = 1f;

    [Header("Performance")]
    [Tooltip("Skip features smaller than this area (world units squared)")]
    [Range(1f, 200f)]
    public float minFeatureArea = 10f;
    
    [Tooltip("Maximum features to process per tile (performance limit)")]
    [Range(10, 100)]
    public int maxFeaturesPerTile = 50;

    [Header("Advanced")]
    [Tooltip("Height offset above terrain surface")]
    [Range(-0.5f, 1f)]
    public float grassHeightOffset = 0.02f;
    
    [Tooltip("Use colormap for grass tinting")]
    public bool useColorMap = true;
    
    [Tooltip("Enable debug logs")]
    public bool debugMode = false;

    private int processedFeatures = 0;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (processedFeatures >= maxFeaturesPerTile)
        {
            if (debugMode)
                Debug.Log("[AdvancedGrassLanduse] Max features per tile reached, skipping");
            return;
        }

        // Get landuse type
        string landuseType = GetLanduseType(ve);
        
        // Find matching rule
        GrassLanduseRule matchingRule = grassRules.FirstOrDefault(rule =>
            rule.landuseTypes.Any(type => landuseType.ToLower().Contains(type.ToLower()))
        );

        if (matchingRule == null)
        {
            // Use default if no rule matches
            if (defaultGrassMaterial != null)
            {
                CreateGrassForFeature(ve, defaultGrassMaterial, defaultScale, landuseType);
            }
            return;
        }

        // Skip if rule is disabled
        if (!matchingRule.enabled)
        {
            if (debugMode)
                Debug.Log($"[AdvancedGrassLanduse] Rule for '{landuseType}' is disabled");
            return;
        }

        // Check minimum area
        float area = CalculateFeatureArea(ve);
        if (area < minFeatureArea)
        {
            if (debugMode)
                Debug.Log($"[AdvancedGrassLanduse] Feature too small ({area:F1} < {minFeatureArea})");
            return;
        }

        // Apply grass based on rule
        ApplyGrassRule(ve, matchingRule, landuseType, area);
        
        // Spawn additional prefabs if configured
        if (matchingRule.spawnAdditionalPrefabs && matchingRule.additionalPrefabs.Length > 0)
        {
            SpawnAdditionalPrefabs(ve, matchingRule, area);
        }

        processedFeatures++;

        if (debugMode)
        {
            Debug.Log($"[AdvancedGrassLanduse] Applied '{matchingRule.ruleName}' to {landuseType} (area: {area:F1})");
        }
    }

    private void ApplyGrassRule(VectorEntity ve, GrassLanduseRule rule, string landuseType, float area)
    {
        // Select grass material
        Material grassMat = rule.grassMaterials.Length > 0 ? 
            rule.grassMaterials[Random.Range(0, rule.grassMaterials.Length)] : 
            defaultGrassMaterial;

        if (grassMat == null) return;

        // Calculate scale based on area and rule settings
        float scale = Random.Range(rule.scaleRange.x, rule.scaleRange.y);
        
        // Area-based scale modification
        if (rule.scaleWithArea)
        {
            float areaFactor = Mathf.Clamp01(area / 100f); // Normalize to 0-1 over 100 sq units
            scale *= (1f + areaFactor * rule.areaScaleMultiplier);
        }

        // Density-based instancing for large areas
        if (area > rule.instanceThresholdArea && rule.enableInstancing)
        {
            CreateInstancedGrass(ve, rule, grassMat, scale, landuseType);
        }
        else
        {
            CreateGrassForFeature(ve, grassMat, scale, landuseType);
        }
    }

    private void CreateGrassForFeature(VectorEntity ve, Material grassMaterial, float scale, string landuseType)
    {
        MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        // Create grass object
        GameObject grassObj = new GameObject($"Grass_{landuseType}");
        grassObj.transform.SetParent(ve.GameObject.transform, false);
        grassObj.transform.localPosition = new Vector3(0, grassHeightOffset, 0);
        grassObj.transform.localScale = Vector3.one * scale;

        // Add components
        MeshFilter grassFilter = grassObj.AddComponent<MeshFilter>();
        MeshRenderer grassRenderer = grassObj.AddComponent<MeshRenderer>();

        grassFilter.sharedMesh = meshFilter.sharedMesh;
        grassRenderer.sharedMaterial = grassMaterial;
        grassRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Apply colormap if enabled
        if (useColorMap)
        {
            StylizedGrassRenderer grassComp = grassObj.GetComponent<StylizedGrassRenderer>();
            if (grassComp == null)
            {
                grassComp = grassObj.AddComponent<StylizedGrassRenderer>();
            }
        }
    }

    private void CreateInstancedGrass(VectorEntity ve, GrassLanduseRule rule, Material grassMaterial, float scale, string landuseType)
    {
        // For large areas, create multiple smaller grass patches for better performance
        MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 size = bounds.size;
        
        // Calculate grid based on density
        int gridX = Mathf.CeilToInt(size.x / rule.instanceGridSize);
        int gridZ = Mathf.CeilToInt(size.z / rule.instanceGridSize);
        
        // Limit instances for performance
        int maxInstances = Mathf.Min(gridX * gridZ, rule.maxInstances);
        
        for (int i = 0; i < maxInstances; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-size.x * 0.4f, size.x * 0.4f),
                grassHeightOffset,
                Random.Range(-size.z * 0.4f, size.z * 0.4f)
            );

            GameObject grassInstance = new GameObject($"GrassInstance_{i}");
            grassInstance.transform.SetParent(ve.GameObject.transform, false);
            grassInstance.transform.localPosition = randomPos;
            
            float instanceScale = scale * Random.Range(0.8f, 1.2f);
            grassInstance.transform.localScale = Vector3.one * instanceScale;

            MeshFilter instanceFilter = grassInstance.AddComponent<MeshFilter>();
            MeshRenderer instanceRenderer = grassInstance.AddComponent<MeshRenderer>();

            instanceFilter.sharedMesh = meshFilter.sharedMesh;
            instanceRenderer.sharedMaterial = grassMaterial;
            instanceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        if (debugMode)
        {
            Debug.Log($"[AdvancedGrassLanduse] Created {maxInstances} grass instances for large {landuseType}");
        }
    }

    private void SpawnAdditionalPrefabs(VectorEntity ve, GrassLanduseRule rule, float area)
    {
        // Calculate spawn count based on area and density
        int spawnCount = Mathf.RoundToInt(area * rule.prefabDensity * 0.01f); // Convert percentage to actual count
        spawnCount = Mathf.Clamp(spawnCount, 0, rule.maxPrefabsPerFeature);

        Bounds bounds = GetFeatureBounds(ve.GameObject);

        for (int i = 0; i < spawnCount; i++)
        {
            // Select random prefab
            GameObject prefab = rule.additionalPrefabs[Random.Range(0, rule.additionalPrefabs.Length)];
            
            // Random position within bounds
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y + rule.prefabHeightOffset,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // Instantiate
            GameObject spawnedPrefab = Instantiate(prefab, randomPos, Quaternion.identity, ve.GameObject.transform);
            
            // Random scale
            float randomScale = Random.Range(rule.prefabScaleRange.x, rule.prefabScaleRange.y);
            spawnedPrefab.transform.localScale = Vector3.one * randomScale;
            
            // Random rotation
            if (rule.randomRotation)
            {
                spawnedPrefab.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }
    }

    private string GetLanduseType(VectorEntity ve)
    {
        if (ve.Feature?.Properties == null) return "unknown";

        var properties = ve.Feature.Properties;

        if (properties.ContainsKey("class"))
            return properties["class"].ToString();
        
        if (properties.ContainsKey("type"))
            return properties["type"].ToString();
        
        if (properties.ContainsKey("landuse"))
            return properties["landuse"].ToString();
            
        return "unknown";
    }

    private float CalculateFeatureArea(VectorEntity ve)
    {
        MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (meshFilter?.sharedMesh == null) return 0f;

        Bounds bounds = meshFilter.sharedMesh.bounds;
        return bounds.size.x * bounds.size.z;
    }

    private Bounds GetFeatureBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        // Fallback to collider bounds
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            return col.bounds;
        }

        // Default small bounds
        return new Bounds(obj.transform.position, Vector3.one);
    }

    public override void Clear()
    {
        processedFeatures = 0;
        base.Clear();
    }
}

/// <summary>
/// Configuration rule for grass variations based on landuse type
/// </summary>
[System.Serializable]
public class GrassLanduseRule
{
    [Header("Rule Identity")]
    public string ruleName = "New Grass Rule";
    public bool enabled = true;

    [Header("Landuse Matching")]
    [Tooltip("Landuse types this rule applies to")]
    public string[] landuseTypes = new string[] { "park" };

    [Header("Grass Materials")]
    [Tooltip("Grass materials to use (will pick randomly if multiple)")]
    public Material[] grassMaterials;

    [Header("Scale Settings")]
    [Tooltip("Scale range for grass")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    [Tooltip("Scale grass based on feature area")]
    public bool scaleWithArea = false;
    
    [Tooltip("Multiplier for area-based scaling")]
    [Range(0f, 2f)]
    public float areaScaleMultiplier = 0.5f;

    [Header("Instancing (Performance)")]
    [Tooltip("Enable grass instancing for large areas")]
    public bool enableInstancing = true;
    
    [Tooltip("Area threshold to start using instancing")]
    public float instanceThresholdArea = 50f;
    
    [Tooltip("Grid size for instances")]
    public float instanceGridSize = 5f;
    
    [Tooltip("Maximum instances per feature")]
    public int maxInstances = 20;

    [Header("Additional Prefab Spawning")]
    [Tooltip("Spawn additional prefabs in this landuse type")]
    public bool spawnAdditionalPrefabs = false;
    
    [Tooltip("Prefabs to spawn (trees, rocks, etc.)")]
    public GameObject[] additionalPrefabs;
    
    [Tooltip("Prefab density (percentage based on area)")]
    [Range(0f, 5f)]
    public float prefabDensity = 1f;
    
    [Tooltip("Max prefabs per feature")]
    public int maxPrefabsPerFeature = 10;
    
    [Tooltip("Height offset for spawned prefabs")]
    public float prefabHeightOffset = 0f;
    
    [Tooltip("Scale range for prefabs")]
    public Vector2 prefabScaleRange = new Vector2(0.8f, 1.2f);
    
    [Tooltip("Apply random Y rotation to prefabs")]
    public bool randomRotation = true;
}