using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Linq;

/// <summary>
/// Spawns grass meshes on landuse features (parks, gardens, etc.) using Stylized Grass Shader.
/// Creates separate mesh objects with grass material only where needed.
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Stylized Grass Landuse")]
public class StylizedGrassLanduseModifier : GameObjectModifier
{
    [Header("Landuse Filtering")]
    [Tooltip("Only spawn grass in these landuse types")]
    public string[] allowedLanduseTypes = new string[] 
    { 
        "park", 
        "grass", 
        "recreation_ground", 
        "garden",
        "meadow",
        "village_green",
        "forest",
        "nature_reserve"
    };
    
    [Header("Grass Material")]
    [Tooltip("Stylized Grass Shader material (will render grass on this mesh)")]
    public Material grassMaterial;
    
    [Header("Grass Settings")]
    [Tooltip("Height offset above terrain surface")]
    [Range(-0.5f, 1f)]
    public float grassHeightOffset = 0.05f;
    
    [Tooltip("Scale factor for grass mesh")]
    [Range(0.5f, 2f)]
    public float meshScale = 1f;
    
    [Header("Performance")]
    [Tooltip("Skip features smaller than this area (world units squared)")]
    [Range(1f, 100f)]
    public float minFeatureArea = 10f;
    
    [Header("Debug")]
    [Tooltip("Enable debug logs")]
    public bool debugMode = false;
    
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (grassMaterial == null)
        {
            if (debugMode)
                Debug.LogWarning("[StylizedGrassLanduse] No grass material assigned!");
            return;
        }
        
        // Check landuse type filtering
        string landuseClass = GetLanduseType(ve);
        bool isAllowed = allowedLanduseTypes.Any(type => 
            landuseClass.ToLower().Contains(type.ToLower())
        );
        
        if (!isAllowed)
        {
            if (debugMode)
                Debug.Log($"[StylizedGrassLanduse] Skipping landuse type: {landuseClass}");
            return;
        }
        
        // Get the mesh
        MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            if (debugMode)
                Debug.LogWarning($"[StylizedGrassLanduse] No mesh on {ve.GameObject.name}");
            return;
        }
        
        Mesh originalMesh = meshFilter.sharedMesh;
        
        // Skip if too small
        float area = CalculateApproximateArea(originalMesh);
        if (area < minFeatureArea)
        {
            if (debugMode)
                Debug.Log($"[StylizedGrassLanduse] Feature too small ({area:F1} < {minFeatureArea}), skipping");
            return;
        }
        
        // Create grass mesh object
        CreateGrassMesh(ve, originalMesh, landuseClass);
    }
    
    private void CreateGrassMesh(VectorEntity ve, Mesh originalMesh, string landuseType)
    {
        // Create new GameObject for grass
        GameObject grassObj = new GameObject($"Grass_{landuseType}");
        grassObj.transform.SetParent(ve.GameObject.transform, false);
        
        // Position slightly above terrain
        grassObj.transform.localPosition = new Vector3(0, grassHeightOffset, 0);
        grassObj.transform.localScale = Vector3.one * meshScale;
        
        // Add mesh components
        MeshFilter grassFilter = grassObj.AddComponent<MeshFilter>();
        MeshRenderer grassRenderer = grassObj.AddComponent<MeshRenderer>();
        
        // Copy mesh
        grassFilter.sharedMesh = originalMesh;
        
        // Apply grass material
        grassRenderer.sharedMaterial = grassMaterial;
        grassRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        
        if (debugMode)
        {
            Debug.Log($"[StylizedGrassLanduse] Created grass on {landuseType} feature (area: {CalculateApproximateArea(originalMesh):F1})");
        }
    }
    
    private string GetLanduseType(VectorEntity ve)
    {
        if (ve.Feature == null || ve.Feature.Properties == null)
            return "unknown";
            
        // Check various property keys for landuse type
        if (ve.Feature.Properties.ContainsKey("class"))
            return ve.Feature.Properties["class"].ToString();
        
        if (ve.Feature.Properties.ContainsKey("type"))
            return ve.Feature.Properties["type"].ToString();
        
        if (ve.Feature.Properties.ContainsKey("landuse"))
            return ve.Feature.Properties["landuse"].ToString();
            
        return "unknown";
    }
    
    private float CalculateApproximateArea(Mesh mesh)
    {
        if (mesh == null || mesh.vertexCount == 0)
            return 0f;
        
        // Use bounds as rough area estimate
        Bounds bounds = mesh.bounds;
        return bounds.size.x * bounds.size.z;
    }
}
