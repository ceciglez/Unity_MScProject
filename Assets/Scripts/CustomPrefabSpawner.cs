using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Collections.Generic;

/// <summary>
/// Custom modifier to spawn multiple prefabs in vector layer features (parks, forests, etc.)
/// Gives more control than Mapbox's built-in PrefabModifier
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Custom Prefab Spawner")]
public class CustomPrefabSpawner : GameObjectModifier
{
    [Header("Prefab Settings")]
    [Tooltip("List of prefabs to spawn (will pick randomly from this list)")]
    public GameObject[] prefabs;
    
    [Header("Spawn Density")]
    [Tooltip("Spawn mode: Fixed quantity or density-based")]
    public SpawnMode spawnMode = SpawnMode.Fixed;
    
    [Tooltip("Fixed: Exact number to spawn per feature")]
    [Range(1, 100)]
    public int fixedQuantity = 10;
    
    [Tooltip("Density: Number of prefabs per 100 square meters")]
    [Range(0.1f, 10f)]
    public float density = 1f;
    
    [Header("Position Settings")]
    [Tooltip("Random position offset within feature bounds")]
    public Vector2 positionOffset = new Vector2(5f, 5f);
    
    [Tooltip("Snap to terrain height")]
    public bool snapToTerrain = true;
    
    [Tooltip("Height offset above terrain")]
    public float heightOffset = 0f;
    
    [Header("Rotation Settings")]
    [Tooltip("Random Y rotation")]
    public bool randomRotation = true;
    
    [Tooltip("If not random, use this rotation")]
    public Vector3 fixedRotation = Vector3.zero;
    
    [Header("Scale Settings")]
    [Tooltip("Random scale variation")]
    public bool randomScale = true;
    
    [Tooltip("Min and max scale multiplier")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    [Header("Advanced")]
    [Tooltip("Layer mask for terrain raycasting")]
    public LayerMask terrainMask = -1;
    
    public enum SpawnMode
    {
        Fixed,      // Spawn exact number per feature
        Density     // Spawn based on feature area
    }
    
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("CustomPrefabSpawner: No prefabs assigned!");
            return;
        }
        
        // Calculate how many to spawn
        int spawnCount = CalculateSpawnCount(ve);
        
        // Get feature bounds for random positioning
        Bounds bounds = ve.GameObject.GetComponent<MeshFilter>()?.mesh?.bounds ?? new Bounds(Vector3.zero, Vector3.one * 10);
        
        // Spawn prefabs
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnPrefab(ve, tile, bounds);
        }
    }
    
    private int CalculateSpawnCount(VectorEntity ve)
    {
        if (spawnMode == SpawnMode.Fixed)
        {
            return fixedQuantity;
        }
        else
        {
            // Calculate based on area (approximate)
            MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                Bounds bounds = meshFilter.mesh.bounds;
                float area = bounds.size.x * bounds.size.z; // Approximate area in world units
                int count = Mathf.RoundToInt(area / 100f * density);
                return Mathf.Max(1, count); // At least 1
            }
            return fixedQuantity; // Fallback
        }
    }
    
    private void SpawnPrefab(VectorEntity ve, UnityTile tile, Bounds bounds)
    {
        // Pick random prefab
        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        
        // Calculate random position within bounds
        Vector3 localPos = new Vector3(
            Random.Range(-bounds.extents.x, bounds.extents.x) + Random.Range(-positionOffset.x, positionOffset.x),
            0,
            Random.Range(-bounds.extents.z, bounds.extents.z) + Random.Range(-positionOffset.y, positionOffset.y)
        );
        
        Vector3 worldPos = ve.GameObject.transform.TransformPoint(localPos);
        
        // Snap to terrain if enabled
        if (snapToTerrain)
        {
            RaycastHit hit;
            if (Physics.Raycast(worldPos + Vector3.up * 1000f, Vector3.down, out hit, 2000f, terrainMask))
            {
                worldPos = hit.point + Vector3.up * heightOffset;
            }
        }
        
        // Calculate rotation
        Quaternion rotation;
        if (randomRotation)
        {
            rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }
        else
        {
            rotation = Quaternion.Euler(fixedRotation);
        }
        
        // Instantiate
        GameObject instance = Instantiate(prefab, worldPos, rotation);
        instance.transform.SetParent(ve.GameObject.transform);
        
        // Apply random scale
        if (randomScale)
        {
            float scale = Random.Range(scaleRange.x, scaleRange.y);
            instance.transform.localScale = prefab.transform.localScale * scale;
        }
    }
}
