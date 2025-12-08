using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Components;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/BIO Spawn Inside Modifier")]
public class BIO_SpawnInsideModifier : Mapbox.Unity.MeshGeneration.Modifiers.GameObjectModifier
{
    [SerializeField]
    int _spawnRateInSquareMeters = 50;

    [SerializeField]
    int _maxSpawn = 1000;

    [SerializeField]
    [Range(0.1f, 5.0f)]
    float _densityMultiplier = 1.0f;

    [SerializeField]
    [Range(1f, 100.0f)]
    float _minDistanceBetweenObjects = 2.0f;

    [Header("Biodiversity Prefabs")]
    [Tooltip("Prefabs for low biodiversity (e.g., dead trees)")]
    public GameObject[] lowBiodiversityPrefabs;
    [Tooltip("Prefabs for high biodiversity (e.g., happy trees)")]
    public GameObject[] highBiodiversityPrefabs;
    [Tooltip("Minimum biodiversity score (0 = all low, 1 = all high)")]
    [Range(0f, 1f)]
    public float minBiodiversityScore = 0f;
    [Tooltip("Maximum biodiversity score (0 = all low, 1 = all high)")]
    [Range(0f, 1f)]
    public float maxBiodiversityScore = 1f;

    private List<Vector3> _spawnedPositions;
    private BiodiversityScoreManager _biodiversityManager;
    private List<GameObject> _spawnedPrefabs = new List<GameObject>();
    private bool _hasSpawned = false;
    private VectorEntity _cachedVectorEntity;
    private UnityTile _cachedTile;
    private bool _observationsReady = false;
    private Coroutine _monitorCoroutine;

    public override void Initialize()
    {
        if (_spawnedPositions == null)
            _spawnedPositions = new List<Vector3>();
        if (_biodiversityManager == null)
            _biodiversityManager = GameObject.FindObjectOfType<BiodiversityScoreManager>();
    }


    // Cache the tile and entity, then wait for observations to load
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        _cachedVectorEntity = ve;
        _cachedTile = tile;

        // Start monitoring for observations if not already monitoring
        if (_monitorCoroutine == null && ve.GameObject != null)
        {
            var monoBehaviour = ve.GameObject.GetComponent<MonoBehaviour>();
            if (monoBehaviour != null)
            {
                _monitorCoroutine = monoBehaviour.StartCoroutine(MonitorObservationsAndSpawn());
            }
        }
    }

    /// <summary>
    /// Monitors for observations to be loaded and spawns prefabs when ready
    /// </summary>
    private System.Collections.IEnumerator MonitorObservationsAndSpawn()
    {
        Debug.Log("[BIO_SpawnInsideModifier] Monitoring for observations to load...");

        int checkCount = 0;
        int maxChecks = 20; // Check for up to 20 seconds
        int lastObservationCount = 0;

        while (checkCount < maxChecks)
        {
            yield return new WaitForSeconds(1f);
            checkCount++;

            // Find all observations in the scene
            ObservationDisplay[] observations = GameObject.FindObjectsOfType<ObservationDisplay>();
            int currentObservationCount = observations.Length;

            // Check if observations have been loaded and positioned
            if (currentObservationCount > 0)
            {
                // Check if observations have stabilized (count hasn't changed)
                if (currentObservationCount == lastObservationCount && lastObservationCount > 0)
                {
                    Debug.Log($"[BIO_SpawnInsideModifier] Observations loaded and stabilized! Count: {currentObservationCount}");

                    // Wait a bit more for biodiversity calculations
                    yield return new WaitForSeconds(2f);

                    _observationsReady = true;

                    // Spawn prefabs now that observations are ready
                    if (_cachedVectorEntity != null && _cachedTile != null && !_hasSpawned)
                    {
                        Debug.Log("[BIO_SpawnInsideModifier] Spawning biodiversity prefabs...");
                        RespawnPrefabs(_cachedVectorEntity, _cachedTile);
                        _hasSpawned = true;
                    }

                    _monitorCoroutine = null;
                    yield break;
                }

                lastObservationCount = currentObservationCount;
                Debug.Log($"[BIO_SpawnInsideModifier] Found {currentObservationCount} observations, waiting for stabilization...");
            }
        }

        Debug.LogWarning("[BIO_SpawnInsideModifier] Timeout waiting for observations to load. Spawning anyway...");

        // Spawn anyway even if observations didn't load
        if (_cachedVectorEntity != null && _cachedTile != null && !_hasSpawned)
        {
            RespawnPrefabs(_cachedVectorEntity, _cachedTile);
            _hasSpawned = true;
        }

        _monitorCoroutine = null;
    }

    /// <summary>
    /// Call this after observations are loaded and when the player is near the area/tile.
    /// </summary>
    public void TriggerProximitySpawn(VectorEntity ve, UnityTile tile, Transform player, float triggerDistance)
    {
        Vector3 areaCenter = ve.Transform.position + ve.Mesh.bounds.center;
        areaCenter.y = player.position.y; // ignore height difference
        float dist = Vector3.Distance(player.position, areaCenter);
        if (dist <= triggerDistance)
        {
            // Only spawn if not already spawned (optional: add a flag if you want to prevent double-spawning)
            RespawnPrefabs(ve, tile);
        }
    }

    /// <summary>
    /// Clears all spawned prefabs under the given parent GameObject.
    /// </summary>
    public void ClearSpawnedPrefabs(GameObject parent)
    {
        if (_spawnedPrefabs != null)
        {
            for (int i = _spawnedPrefabs.Count - 1; i >= 0; i--)
            {
                if (_spawnedPrefabs[i] != null && _spawnedPrefabs[i].transform.parent == parent.transform)
                {
                    GameObject.Destroy(_spawnedPrefabs[i]);
                    _spawnedPrefabs.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Manually force spawn/respawn prefabs (useful for testing or when observations update)
    /// </summary>
    public void ForceSpawn()
    {
        if (_cachedVectorEntity != null && _cachedTile != null)
        {
            Debug.Log("[BIO_SpawnInsideModifier] Force spawning biodiversity prefabs...");
            _hasSpawned = false; // Reset flag to allow respawn
            RespawnPrefabs(_cachedVectorEntity, _cachedTile);
            _hasSpawned = true;
        }
        else
        {
            Debug.LogWarning("[BIO_SpawnInsideModifier] Cannot force spawn - no cached tile/entity data!");
        }
    }

    /// <summary>
    /// Respawns prefabs for the given VectorEntity and UnityTile, using latest biodiversity data.
    /// </summary>
    public void RespawnPrefabs(VectorEntity ve, UnityTile tile)
    {
        if (_biodiversityManager == null)
            _biodiversityManager = GameObject.FindObjectOfType<BiodiversityScoreManager>();

        _spawnedPositions.Clear();
        ClearSpawnedPrefabs(ve.GameObject);

        var bounds = ve.Mesh.bounds;
        var center = ve.Transform.position + bounds.center;
        center.y = 0;
        var area = (int)(bounds.size.x * bounds.size.z);
        int spawnCount = Mathf.Min(Mathf.RoundToInt((area / _spawnRateInSquareMeters) * _densityMultiplier), _maxSpawn);

        // Get biodiversity score for this location
        float biodiversityScore = _biodiversityManager != null ? _biodiversityManager.GetSimpsonsIndexAtPosition(center) : 1f;

        Debug.Log($"[BIO_SpawnInsideModifier] Spawning {spawnCount} prefabs at {center}. Biodiversity Score: {biodiversityScore:F3}");

        // Normalize biodiversity score between min and max
        float t = Mathf.InverseLerp(minBiodiversityScore, maxBiodiversityScore, biodiversityScore);
        t = Mathf.Clamp01(t);

        int highBioCount = 0;
        int lowBioCount = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            var x = UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x);
            var z = UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z);
            var ray = new Ray(center + new Vector3(x, 100, z), Vector3.down * 2000);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 150))
            {
                if (IsValidSpawnPosition(hit.point))
                {
                    GameObject prefabToSpawn;
                    bool isHighBio = false;

                    if (UnityEngine.Random.value < t && highBiodiversityPrefabs != null && highBiodiversityPrefabs.Length > 0)
                    {
                        prefabToSpawn = highBiodiversityPrefabs[UnityEngine.Random.Range(0, highBiodiversityPrefabs.Length)];
                        isHighBio = true;
                        highBioCount++;
                    }
                    else if (lowBiodiversityPrefabs != null && lowBiodiversityPrefabs.Length > 0)
                    {
                        prefabToSpawn = lowBiodiversityPrefabs[UnityEngine.Random.Range(0, lowBiodiversityPrefabs.Length)];
                        lowBioCount++;
                    }
                    else
                    {
                        continue; // No prefab to spawn
                    }

                    var go = GameObject.Instantiate(prefabToSpawn, hit.point, Quaternion.identity, ve.GameObject.transform);
                    _spawnedPrefabs.Add(go);
                    _spawnedPositions.Add(hit.point);
                }
            }
        }

        Debug.Log($"[BIO_SpawnInsideModifier] ✓ Spawned {_spawnedPrefabs.Count} total prefabs: " +
                  $"{highBioCount} high-biodiversity ({(highBioCount / (float)_spawnedPrefabs.Count * 100):F1}%), " +
                  $"{lowBioCount} low-biodiversity ({(lowBioCount / (float)_spawnedPrefabs.Count * 100):F1}%)");
    }
    private bool IsValidSpawnPosition(Vector3 newPosition)
    {
        foreach (var existingPosition in _spawnedPositions)
        {
            float distance = Vector3.Distance(newPosition, existingPosition);
            if (distance < _minDistanceBetweenObjects)
                return false;
        }
        return true;
    }
}
