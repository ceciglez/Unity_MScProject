using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/Water Surface Conform Modifier")]
public class WaterSurfaceConformModifier : GameObjectModifier
{
    [Header("Water Surface Settings")]
    [Tooltip("Height offset above terrain elevation in meters")]
    [Range(0.01f, 1f)]
    public float surfaceOffset = 0.1f;
    
    [Tooltip("Sample resolution for terrain height (higher = more accurate but slower)")]
    [Range(4, 32)]
    public int sampleResolution = 8;
    
    [Header("Debug")]
    public bool debugMode = false;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve.GameObject == null)
            return;

        MeshFilter waterMeshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (waterMeshFilter == null || waterMeshFilter.mesh == null)
        {
            if (debugMode)
                Debug.LogWarning($"[WaterSurfaceConform] No mesh on {ve.GameObject.name}");
            return;
        }

        // Get or create a unique mesh instance for this water feature
        Mesh waterMesh = Object.Instantiate(waterMeshFilter.sharedMesh);
        waterMeshFilter.mesh = waterMesh;

        // Sample terrain heights and adjust water vertices
        AdjustWaterSurfaceToTerrain(ve.GameObject, waterMesh, tile);

        if (debugMode)
        {
            Debug.Log($"[WaterSurfaceConform] Conformed water surface '{ve.GameObject.name}' to terrain");
        }
    }

    private void AdjustWaterSurfaceToTerrain(GameObject waterObj, Mesh waterMesh, UnityTile tile)
    {
        Vector3[] vertices = waterMesh.vertices;
        Bounds meshBounds = waterMesh.bounds;
        
        // Find terrain mesh in the tile
        MeshFilter terrainMeshFilter = null;
        MeshFilter[] meshFilters = tile.GetComponentsInChildren<MeshFilter>();
        
        foreach (MeshFilter mf in meshFilters)
        {
            // Look for the main terrain mesh (usually largest or named appropriately)
            if (mf.gameObject != waterObj && mf.sharedMesh != null && mf.sharedMesh.vertexCount > 100)
            {
                terrainMeshFilter = mf;
                break;
            }
        }

        if (terrainMeshFilter == null)
        {
            if (debugMode)
                Debug.LogWarning("[WaterSurfaceConform] Could not find terrain mesh");
            return;
        }

        Mesh terrainMesh = terrainMeshFilter.sharedMesh;
        Vector3[] terrainVertices = terrainMesh.vertices;
        Transform terrainTransform = terrainMeshFilter.transform;

        // For each water vertex, find the terrain height at that position
        for (int i = 0; i < vertices.Length; i++)
        {
            // Convert water vertex to world position
            Vector3 worldPos = waterObj.transform.TransformPoint(vertices[i]);
            
            // Sample terrain height at this XZ position
            float terrainHeight = SampleTerrainHeight(worldPos, terrainVertices, terrainTransform);
            
            // Set water vertex Y to terrain height + offset
            Vector3 adjustedWorldPos = new Vector3(worldPos.x, terrainHeight + surfaceOffset, worldPos.z);
            
            // Convert back to local space
            vertices[i] = waterObj.transform.InverseTransformPoint(adjustedWorldPos);
        }

        waterMesh.vertices = vertices;
        waterMesh.RecalculateBounds();
        waterMesh.RecalculateNormals();
    }

    private float SampleTerrainHeight(Vector3 worldPos, Vector3[] terrainVertices, Transform terrainTransform)
    {
        float closestHeight = 0f;
        float closestDistanceSq = float.MaxValue;

        // Find closest terrain vertex to this position
        for (int i = 0; i < terrainVertices.Length; i += sampleResolution)
        {
            Vector3 terrainWorldPos = terrainTransform.TransformPoint(terrainVertices[i]);
            
            // Calculate XZ distance (ignore Y)
            float dx = terrainWorldPos.x - worldPos.x;
            float dz = terrainWorldPos.z - worldPos.z;
            float distanceSq = dx * dx + dz * dz;

            if (distanceSq < closestDistanceSq)
            {
                closestDistanceSq = distanceSq;
                closestHeight = terrainWorldPos.y;
            }
        }

        return closestHeight;
    }
}
