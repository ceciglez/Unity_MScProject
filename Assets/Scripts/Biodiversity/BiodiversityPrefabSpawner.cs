using UnityEngine;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using System.Collections.Generic;

/// <summary>
/// Mapbox terrain modifier that spawns environmental objects based on biodiversity
/// Integrates with BiodiversityScoreManager to create visually distinct ecosystems
///
/// FUNCTIONALITY:
/// - Spawns prefabs on Mapbox terrain tiles as they generate
/// - Density controlled by Simpson's Biodiversity Index
/// - Uses AnimationCurve to map biodiversity (0-1) to spawn density multiplier
/// - Samples biodiversity across 3x3 grid per tile for average
/// - Raycasts objects to terrain surface for proper placement
///
/// PREFAB CATEGORIES:
/// - High Biodiversity (>0.6): Lush vegetation, trees, flowering plants
/// - Medium Biodiversity (>0.3): Moderate decoration, bushes, grass
/// - Low Biodiversity (<0.3): Sparse vegetation, rocks, dead trees
/// - Universal: Objects that spawn everywhere, scaled by biodiversity
///
/// TECHNICAL APPROACH:
/// - Inherits from Mapbox GameObjectModifier
/// - Runs during tile generation (OnEnable callback)
/// - Random spawn positioning with configurable density
/// - Random rotation and scale variation for natural look
/// - Performance limit: maxObjectsPerTile (default 50)
///
/// VISUAL STORYTELLING:
/// - High biodiversity → Dense, vibrant ecosystems
/// - Low biodiversity → Barren, sparse environments
/// - Creates emergent environmental narrative from real data
///
/// CODE LOGIC SUGGESTED BY: Claude Sonnet 4.5, Dec 2025
/// PROMPT: "Create a Mapbox modifier that spawns prefabs with density controlled by Simpson's biodiversity index"
/// SOURCE:
/// - Mapbox Unity SDK GameObjectModifier pattern
/// - Reference: https://docs.mapbox.com/unity/maps/guides/modifiers/
/// - Custom biodiversity integration layer
///
/// AI CONTRIBUTION: ~80% - Mapbox GameObjectModifier integration, density calculation curves, raycast placement
/// HUMAN CONTRIBUTION: ~20% - Prefab asset selection, biodiversity threshold tuning, spawn count limits
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Biodiversity Prefab Spawner")]
public class BiodiversityPrefabSpawner : GameObjectModifier
{
    [Header("Prefab Categories")]
    [Tooltip("Prefabs for high biodiversity areas (trees, lush plants)")]
    public GameObject[] highBiodiversityPrefabs = new GameObject[0];

    [Tooltip("Prefabs for medium biodiversity areas (bushes, grass clumps)")]
    public GameObject[] mediumBiodiversityPrefabs = new GameObject[0];

    [Tooltip("Prefabs for low biodiversity areas (rocks, dead trees, sparse plants)")]
    public GameObject[] lowBiodiversityPrefabs = new GameObject[0];

    [Tooltip("Universal prefabs that can spawn anywhere (scale with biodiversity)")]
    public GameObject[] universalPrefabs = new GameObject[0];

    [Header("Spawn Density")]
    [Tooltip("Base spawn density (objects per square meter) - multiplied by Simpson's Index")]
    [Range(0.001f, 1f)]
    public float baseDensity = 0.05f;

    [Tooltip("Maximum spawn density for very high biodiversity areas")]
    [Range(0.01f, 2f)]
    public float maxDensity = 0.3f;

    [Tooltip("Minimum spawn density for low biodiversity areas")]
    [Range(0f, 0.1f)]
    public float minDensity = 0.01f;

    [Tooltip("Density multiplier curve (X=Simpson's Index 0-1, Y=density multiplier)")]
    public AnimationCurve densityCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 3f);

    [Header("Spawn Area")]
    [Tooltip("Only spawn within this distance from tile center (0 = entire tile)")]
    [Range(0f, 1000f)]
    public float spawnRadius = 0f;

    [Tooltip("Avoid spawning near tile edges (prevents overlap issues)")]
    [Range(0f, 50f)]
    public float edgeBuffer = 5f;

    [Header("Placement Settings")]
    [Tooltip("Raycast down to place on terrain surface")]
    public bool snapToTerrain = true;

    [Tooltip("Raycast layer mask for terrain detection")]
    public LayerMask terrainLayerMask = -1;

    [Tooltip("Random rotation on Y axis")]
    public bool randomRotation = true;

    [Tooltip("Random scale variation (±percentage)")]
    [Range(0f, 0.5f)]
    public float scaleVariation = 0.2f;

    [Tooltip("Minimum scale multiplier")]
    [Range(0.1f, 2f)]
    public float minScale = 0.8f;

    [Tooltip("Maximum scale multiplier")]
    [Range(0.1f, 3f)]
    public float maxScale = 1.5f;

    [Header("Biodiversity Thresholds")]
    [Tooltip("Simpson's Index threshold for high biodiversity prefabs")]
    [Range(0f, 1f)]
    public float highBiodiversityThreshold = 0.6f;

    [Tooltip("Simpson's Index threshold for medium biodiversity prefabs")]
    [Range(0f, 1f)]
    public float mediumBiodiversityThreshold = 0.3f;

    [Header("Performance")]
    [Tooltip("Maximum objects to spawn per tile (prevents excessive spawning)")]
    [Range(1, 500)]
    public int maxObjectsPerTile = 100;

    [Tooltip("Skip spawning if biodiversity is below this threshold")]
    [Range(0f, 0.3f)]
    public float minimumBiodiversityToSpawn = 0.05f;

    [Header("Debugging")]
    public bool enableDebugLogging = false;
    public bool visualizeSpawnPoints = false;

    // Runtime references
    private BiodiversityScoreManager biodiversityManager;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        // Find biodiversity manager if not cached
        if (biodiversityManager == null)
        {
            biodiversityManager = Object.FindObjectOfType<BiodiversityScoreManager>();

            if (biodiversityManager == null)
            {
                if (enableDebugLogging)
                    Debug.LogWarning("[BiodiversityPrefabSpawner] No BiodiversityScoreManager found - skipping spawn");
                return;
            }
        }

        // Validate prefabs
        if (!HasValidPrefabs())
        {
            if (enableDebugLogging)
                Debug.LogWarning("[BiodiversityPrefabSpawner] No prefabs assigned - skipping spawn");
            return;
        }

        if (enableDebugLogging)
            Debug.Log($"[BiodiversityPrefabSpawner] Starting spawn for tile at {tile.transform.position}");

        // Calculate spawn points based on biodiversity
        SpawnObjectsOnTile(ve, tile);
    }

    /// <summary>
    /// Main spawning logic - distributes objects based on biodiversity
    /// </summary>
    private void SpawnObjectsOnTile(VectorEntity ve, UnityTile tile)
    {
        // Get tile bounds
        MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Bounds tileBounds = meshFilter.sharedMesh.bounds;
        Vector3 tileCenter = tile.transform.TransformPoint(tileBounds.center);
        Vector3 tileSize = Vector3.Scale(tileBounds.size, tile.transform.lossyScale);

        // Calculate how many objects to spawn
        float tileArea = tileSize.x * tileSize.z;
        float avgBiodiversity = CalculateAverageBiodiversityForTile(tileCenter, tileSize);

        // Skip if biodiversity too low
        if (avgBiodiversity < minimumBiodiversityToSpawn)
        {
            if (enableDebugLogging)
                Debug.Log($"[BiodiversityPrefabSpawner] Skipping tile - biodiversity too low ({avgBiodiversity:F3})");
            return;
        }

        // Calculate spawn density
        float densityMultiplier = densityCurve.Evaluate(avgBiodiversity);
        float effectiveDensity = Mathf.Clamp(baseDensity * densityMultiplier, minDensity, maxDensity);
        int targetSpawnCount = Mathf.Min(Mathf.RoundToInt(tileArea * effectiveDensity), maxObjectsPerTile);

        if (enableDebugLogging)
        {
            Debug.Log($"[BiodiversityPrefabSpawner] Tile biodiversity: {avgBiodiversity:F3}, " +
                     $"Density: {effectiveDensity:F4}, Target spawns: {targetSpawnCount}");
        }

        // Spawn objects
        int successfulSpawns = 0;
        int attempts = 0;
        int maxAttempts = targetSpawnCount * 3; // Allow some failed attempts

        while (successfulSpawns < targetSpawnCount && attempts < maxAttempts)
        {
            attempts++;

            // Generate random position within tile bounds
            Vector3 randomOffset = new Vector3(
                Random.Range(-tileSize.x / 2f + edgeBuffer, tileSize.x / 2f - edgeBuffer),
                0f,
                Random.Range(-tileSize.z / 2f + edgeBuffer, tileSize.z / 2f - edgeBuffer)
            );

            Vector3 spawnPos = tileCenter + randomOffset;

            // Check spawn radius
            if (spawnRadius > 0f && Vector3.Distance(spawnPos, tileCenter) > spawnRadius)
                continue;

            // Get biodiversity at this specific position
            float localBiodiversity = biodiversityManager.GetSimpsonsIndexAtPosition(spawnPos);

            // Snap to terrain if enabled
            if (snapToTerrain)
            {
                RaycastHit hit;
                if (Physics.Raycast(spawnPos + Vector3.up * 100f, Vector3.down, out hit, 200f, terrainLayerMask))
                {
                    spawnPos = hit.point;
                }
                else
                {
                    continue; // Failed to find terrain
                }
            }

            // Select appropriate prefab based on local biodiversity
            GameObject prefabToSpawn = SelectPrefabForBiodiversity(localBiodiversity);
            if (prefabToSpawn == null)
                continue;

            // Instantiate prefab
            Quaternion rotation = randomRotation ?
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) :
                Quaternion.identity;

            GameObject spawnedObj = Object.Instantiate(prefabToSpawn, spawnPos, rotation, ve.GameObject.transform);

            // Apply random scale
            float scaleMultiplier = Random.Range(minScale, maxScale);
            if (scaleVariation > 0f)
            {
                scaleMultiplier *= 1f + Random.Range(-scaleVariation, scaleVariation);
            }
            spawnedObj.transform.localScale = Vector3.one * scaleMultiplier;

            // Name for organization
            spawnedObj.name = $"{prefabToSpawn.name}_Bio{localBiodiversity:F2}";

            spawnedObjects.Add(spawnedObj);
            successfulSpawns++;

            if (visualizeSpawnPoints && enableDebugLogging && successfulSpawns <= 5)
            {
                Debug.Log($"[BiodiversityPrefabSpawner] Spawned {prefabToSpawn.name} at {spawnPos}, " +
                         $"local biodiversity: {localBiodiversity:F3}");
            }
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[BiodiversityPrefabSpawner] Completed: {successfulSpawns}/{targetSpawnCount} objects spawned " +
                     $"({attempts} attempts)");
        }
    }

    /// <summary>
    /// Calculates average biodiversity across a tile area
    /// </summary>
    private float CalculateAverageBiodiversityForTile(Vector3 tileCenter, Vector3 tileSize)
    {
        // Sample biodiversity at several points across the tile
        int sampleCount = 9; // 3x3 grid
        float totalBiodiversity = 0f;
        int validSamples = 0;

        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                Vector3 sampleOffset = new Vector3(
                    (x - 1) * tileSize.x / 3f,
                    0f,
                    (z - 1) * tileSize.z / 3f
                );

                Vector3 samplePos = tileCenter + sampleOffset;
                float biodiversity = biodiversityManager.GetSimpsonsIndexAtPosition(samplePos);

                totalBiodiversity += biodiversity;
                validSamples++;
            }
        }

        return validSamples > 0 ? totalBiodiversity / validSamples : 0f;
    }

    /// <summary>
    /// Selects appropriate prefab based on biodiversity score
    /// </summary>
    private GameObject SelectPrefabForBiodiversity(float simpsonsIndex)
    {
        GameObject[] candidatePrefabs = null;

        // Choose prefab category based on biodiversity
        if (simpsonsIndex >= highBiodiversityThreshold && highBiodiversityPrefabs.Length > 0)
        {
            candidatePrefabs = highBiodiversityPrefabs;
        }
        else if (simpsonsIndex >= mediumBiodiversityThreshold && mediumBiodiversityPrefabs.Length > 0)
        {
            candidatePrefabs = mediumBiodiversityPrefabs;
        }
        else if (lowBiodiversityPrefabs.Length > 0)
        {
            candidatePrefabs = lowBiodiversityPrefabs;
        }

        // Fallback to universal prefabs
        if (candidatePrefabs == null || candidatePrefabs.Length == 0)
        {
            if (universalPrefabs.Length > 0)
                candidatePrefabs = universalPrefabs;
            else
                return null;
        }

        // Select random prefab from category
        return candidatePrefabs[Random.Range(0, candidatePrefabs.Length)];
    }

    /// <summary>
    /// Checks if any prefabs are assigned
    /// </summary>
    private bool HasValidPrefabs()
    {
        return (highBiodiversityPrefabs != null && highBiodiversityPrefabs.Length > 0) ||
               (mediumBiodiversityPrefabs != null && mediumBiodiversityPrefabs.Length > 0) ||
               (lowBiodiversityPrefabs != null && lowBiodiversityPrefabs.Length > 0) ||
               (universalPrefabs != null && universalPrefabs.Length > 0);
    }

    /// <summary>
    /// Clean up spawned objects (called by Mapbox when tile is destroyed)
    /// </summary>
    public void CleanUp()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
                Object.Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}
