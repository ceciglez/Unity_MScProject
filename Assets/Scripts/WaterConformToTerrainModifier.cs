using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/Water Conform To Terrain Modifier")]
public class WaterConformToTerrainModifier : GameObjectModifier
{
    [Header("Terrain Settings")]
    [Tooltip("Height offset above terrain in meters")]
    [Range(0.01f, 2f)]
    public float heightAboveTerrain = 0.15f;
    
    [Tooltip("Should water mesh vertices conform to terrain elevation?")]
    public bool conformVerticesToTerrain = false;
    
    [Header("Debug")]
    public bool debugMode = false;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve.GameObject == null)
            return;

        if (conformVerticesToTerrain)
        {
            // Modify water mesh vertices to follow terrain
            ConformMeshToTerrain(ve.GameObject, tile);
        }
        else
        {
            // Simple height offset
            Vector3 currentPosition = ve.GameObject.transform.localPosition;
            ve.GameObject.transform.localPosition = new Vector3(
                currentPosition.x, 
                currentPosition.y + heightAboveTerrain, 
                currentPosition.z
            );
        }

        if (debugMode)
        {
            Debug.Log($"[WaterConformToTerrain] Processed water '{ve.GameObject.name}'");
        }
    }

    private void ConformMeshToTerrain(GameObject waterObj, UnityTile tile)
    {
        MeshFilter meshFilter = waterObj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
            return;

        Mesh waterMesh = meshFilter.mesh;
        Vector3[] vertices = waterMesh.vertices;
        
        // Get terrain mesh for height sampling
        GameObject terrainObj = tile.GetComponentInChildren<MeshRenderer>()?.gameObject;
        if (terrainObj == null)
        {
            if (debugMode)
                Debug.LogWarning("[WaterConformToTerrain] Could not find terrain mesh");
            return;
        }

        MeshCollider terrainCollider = terrainObj.GetComponent<MeshCollider>();
        if (terrainCollider == null)
        {
            // Add temporary collider for raycasting
            terrainCollider = terrainObj.AddComponent<MeshCollider>();
        }

        // Adjust each vertex to terrain height
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = waterObj.transform.TransformPoint(vertices[i]);
            
            // Raycast down to find terrain height
            RaycastHit hit;
            if (Physics.Raycast(worldPos + Vector3.up * 100f, Vector3.down, out hit, 200f))
            {
                if (hit.collider == terrainCollider)
                {
                    // Set water vertex to terrain height + offset
                    Vector3 terrainPoint = hit.point;
                    terrainPoint.y += heightAboveTerrain;
                    vertices[i] = waterObj.transform.InverseTransformPoint(terrainPoint);
                }
            }
        }

        waterMesh.vertices = vertices;
        waterMesh.RecalculateBounds();
        waterMesh.RecalculateNormals();
    }
}
