using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;
using MicahW.PointGrass;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/Point Grass Tile Modifier")]
public class PointGrassTileModifier : GameObjectModifier
{
    [Header("Point Grass Settings")]
    [Tooltip("Grass material to use (must have PointGrass shader)")]
    public Material grassMaterial;
    
    [Tooltip("Grass blade mesh (leave null for flat blades)")]
    public Mesh grassBladeMesh;
    
    [Tooltip("Grass density (points per unit area)")]
    [Range(100f, 10000f)]
    public float pointCount = 1000f;
    
    [Tooltip("Multiply point count by tile area")]
    public bool multiplyByArea = true;
    
    [Tooltip("Density cutoff (0-1, lower = more grass)")]
    [Range(0f, 1f)]
    public float densityCutoff = 0.5f;
    
    [Tooltip("Grass height range mapping")]
    public Vector2 lengthMapping = new Vector2(0.5f, 1.5f);
    
    [Tooltip("Shadow casting mode")]
    public UnityEngine.Rendering.ShadowCastingMode shadowMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    
    [Header("Debug")]
    public bool debugMode = false;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        // Get the GameObject for this feature
        GameObject featureObject = ve.GameObject;
        
        if (featureObject == null)
        {
            if (debugMode)
                Debug.LogWarning("[PointGrassTileModifier] Feature GameObject is null");
            return;
        }

        // Check if PointGrassRenderer already exists
        PointGrassRenderer grassRenderer = featureObject.GetComponent<PointGrassRenderer>();
        if (grassRenderer != null)
        {
            if (debugMode)
                Debug.Log($"[PointGrassTileModifier] Grass renderer already exists on {featureObject.name}");
            return;
        }

        // Get the MeshFilter from the feature
        MeshFilter meshFilter = featureObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            if (debugMode)
                Debug.LogWarning($"[PointGrassTileModifier] No mesh found on {featureObject.name}");
            return;
        }

        // Add PointGrassRenderer component
        grassRenderer = featureObject.AddComponent<PointGrassRenderer>();
        
        // Configure the grass renderer
        ConfigureGrassRenderer(grassRenderer, meshFilter.sharedMesh);

        if (debugMode)
            Debug.Log($"[PointGrassTileModifier] Added grass renderer to {featureObject.name}");
    }

    private void ConfigureGrassRenderer(PointGrassRenderer grassRenderer, Mesh mesh)
    {
        // Set distribution source to mesh
        grassRenderer.distSource = PointGrassCommon.DistributionSource.Mesh;
        grassRenderer.baseMesh = mesh;
        
        // Set grass blade type
        if (grassBladeMesh != null)
        {
            grassRenderer.bladeType = PointGrassCommon.BladeType.Mesh;
            grassRenderer.grassBladeMesh = grassBladeMesh;
        }
        else
        {
            grassRenderer.bladeType = PointGrassCommon.BladeType.Flat;
        }
        
        // Set material
        if (grassMaterial != null)
        {
            grassRenderer.material = grassMaterial;
        }
        
        // Set point parameters
        grassRenderer.pointCount = pointCount;
        grassRenderer.multiplyByArea = multiplyByArea;
        grassRenderer.pointLODFactor = 1f;
        
        // Set density
        grassRenderer.useDensity = true;
        grassRenderer.densityCutoff = densityCutoff;
        
        // Set length
        grassRenderer.useLength = true;
        grassRenderer.lengthMapping = lengthMapping;
        
        // Set shadow mode
        grassRenderer.shadowMode = shadowMode;
        
        // Set randomize seed
        grassRenderer.randomiseSeed = true;
    }
}
