using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Linq;

/// <summary>
/// Simple grass plane modifier that works with any URP material.
/// Creates textured planes on landuse features - no complex shader dependencies.
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Simple Grass Plane")]
public class SimpleGrassPlaneModifier : GameObjectModifier
{
    [Header("Landuse Filtering")]
    [Tooltip("Only spawn grass in these landuse types")]
    public string[] allowedLanduseTypes = new string[] { "park", "grass", "recreation_ground", "garden" };
    
    [Header("Grass Material")]
    [Tooltip("URP material with grass texture (use URP/Lit with alpha clipping)")]
    public Material grassMaterial;
    
    [Header("Positioning")]
    [Tooltip("Height of grass plane above terrain surface")]
    [Range(0f, 1f)]
    public float grassHeight = 0.05f;
    
    [Header("Layer Settings")]
    [Tooltip("Layer to assign to grass planes")]
    public string grassLayer = "Grass";
    [Tooltip("Check for observation overlap before spawning grass")]
    public bool avoidObservations = true;
    [Tooltip("Layer containing observations to avoid")]
    public string observationLayer = "Observations";
    [Tooltip("Layer containing grass exclusion zones")]
    public string grassExclusionLayer = "GrassExclusion";
    
    [Header("Advanced")]
    [Tooltip("Enable debug logs")]
    public bool debugMode = false;
    
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (grassMaterial == null)
        {
            if (debugMode)
                Debug.LogWarning("[SimpleGrassPlane] No grass material assigned!");
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
                    Debug.Log($"[SimpleGrassPlane] Skipping '{landuseClass}' - not in allowed list");
                return;
            }
            
            if (debugMode)
                Debug.Log($"[SimpleGrassPlane] ✓ Adding grass to '{landuseClass}' feature");
        }
        
        // Get feature bounds
        Bounds bounds = GetFeatureBounds(ve.GameObject);
        
        // Check for observation overlap if enabled
        if (avoidObservations && CheckForObservationOverlap(bounds))
        {
            if (debugMode)
                Debug.Log($"[SimpleGrassPlane] Skipping grass - overlaps with observation");
            return;
        }
        
        // Create grass plane
        CreateGrassPlane(ve.GameObject, bounds);
    }
    
    private void CreateGrassPlane(GameObject feature, Bounds bounds)
    {
        // Check for observation overlap if avoidance is enabled
        if (avoidObservations && CheckForObservationOverlap(bounds))
        {
            if (debugMode)
                Debug.Log($"[SimpleGrassPlane] Skipping grass creation due to observation overlap");
            return;
        }
        
        // Create plane matching feature bounds
        GameObject grassPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        grassPlane.name = "GrassPlane";
        
        // Position and scale
        // Unity plane is 10x10 units by default
        Vector3 position = new Vector3(
            bounds.center.x,
            bounds.min.y + grassHeight,
            bounds.center.z
        );
        
        Vector3 scale = new Vector3(
            bounds.size.x / 10f,
            1f,
            bounds.size.z / 10f
        );
        
        grassPlane.transform.position = position;
        grassPlane.transform.localScale = scale;
        
        // Apply material
        MeshRenderer renderer = grassPlane.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = grassMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        // Set grass layer
        int grassLayerIndex = LayerMask.NameToLayer(grassLayer);
        if (grassLayerIndex != -1)
        {
            grassPlane.layer = grassLayerIndex;
        }
        
        // Remove collider (grass shouldn't block player)
        MeshCollider collider = grassPlane.GetComponent<MeshCollider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        
        // Parent to feature
        grassPlane.transform.SetParent(feature.transform);
        
        if (debugMode)
            Debug.Log($"[SimpleGrassPlane] Created grass plane: {bounds.size.x:F1}x{bounds.size.z:F1}m");
    }
    
    private string GetLanduseType(VectorEntity ve)
    {
        var properties = ve.Feature.Properties;
        
        if (properties.ContainsKey("class"))
            return properties["class"].ToString();
        
        if (properties.ContainsKey("type"))
            return properties["type"].ToString();
        
        if (properties.ContainsKey("landuse"))
            return properties["landuse"].ToString();
        
        return "unknown";
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
    
    private bool CheckForObservationOverlap(Bounds grassBounds)
    {
        // Method 1: Check for observation layer colliders
        int obsLayerMask = 1 << LayerMask.NameToLayer(observationLayer);
        if (obsLayerMask > 1)
        {
            Collider[] observations = Physics.OverlapBox(
                grassBounds.center, 
                grassBounds.extents, 
                Quaternion.identity, 
                obsLayerMask
            );
            
            if (observations.Length > 0)
            {
                if (debugMode)
                    Debug.Log($"[SimpleGrassPlane] Found {observations.Length} observation collider overlaps");
                return true;
            }
        }
        
        // Method 2: Check for grass exclusion layer colliders
        int exclusionLayerMask = 1 << LayerMask.NameToLayer(grassExclusionLayer);
        if (exclusionLayerMask > 1)
        {
            Collider[] exclusionZones = Physics.OverlapBox(
                grassBounds.center, 
                grassBounds.extents, 
                Quaternion.identity, 
                exclusionLayerMask
            );
            
            if (exclusionZones.Length > 0)
            {
                if (debugMode)
                    Debug.Log($"[SimpleGrassPlane] Found {exclusionZones.Length} exclusion zone overlaps");
                return true;
            }
        }
        
        // Method 3: Check for grass exclusion markers
        GrassExclusionMarker[] exclusionMarkers = FindObjectsOfType<GrassExclusionMarker>();
        foreach (var marker in exclusionMarkers)
        {
            if (marker.DoesBoundsOverlap(grassBounds))
            {
                if (debugMode)
                    Debug.Log($"[SimpleGrassPlane] Found exclusion marker overlap at {marker.transform.position}");
                return true;
            }
        }
        
        return false;
    }
}
