using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/Add Mesh Collider")]
public class AddMeshColliderModifier : GameObjectModifier
{
    [Header("Collider Settings")]
    [Tooltip("Should the collider be convex?")]
    public bool convex = false;
    
    [Tooltip("Cooking options for the mesh collider")]
    public MeshColliderCookingOptions cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;
    
    [Header("Debug")]
    public bool debugMode = false;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve.GameObject == null)
            return;

        // Check if collider already exists
        MeshCollider existingCollider = ve.GameObject.GetComponent<MeshCollider>();
        if (existingCollider != null)
        {
            if (debugMode)
                Debug.Log($"[AddMeshCollider] Collider already exists on {ve.GameObject.name}");
            return;
        }

        // Get mesh filter
        MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            if (debugMode)
                Debug.LogWarning($"[AddMeshCollider] No mesh on {ve.GameObject.name}");
            return;
        }

        // Add mesh collider
        MeshCollider collider = ve.GameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = meshFilter.sharedMesh;
        collider.convex = convex;
        collider.cookingOptions = cookingOptions;

        if (debugMode)
            Debug.Log($"[AddMeshCollider] Added collider to {ve.GameObject.name}");
    }
}
