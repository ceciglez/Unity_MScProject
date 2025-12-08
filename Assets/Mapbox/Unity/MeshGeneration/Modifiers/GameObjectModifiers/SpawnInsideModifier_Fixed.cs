// HACK:
// This will work out of the box, but it's intended to be an example of how to approach
// procedural decoration like this.
// A better approach would be to operate on the geometry itself.

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine;
	using System.Collections.Generic;
	using System;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Spawn Inside Modifier Fixed")]
	public class SpawnInsideModifier_Fixed : GameObjectModifier
	{
		[Header("Spawn Settings")]
		[SerializeField]
		int _spawnRateInSquareMeters = 100;

		[SerializeField]
		int _maxSpawn = 1000;
		
		[Header("Density Controls")]
		[SerializeField]
		[Range(0f, 1f)]
		[Tooltip("Overall density multiplier (0 = no spawning, 1 = full density)")]
		float _densityMultiplier = 0.5f;
		
		[SerializeField]
		[Range(0.1f, 100f)]
		[Tooltip("Minimum distance between spawned objects")]
		float _minDistanceBetweenObjects = 3f;

		[Header("Prefab Settings")]
		[SerializeField]
		GameObject[] _prefabs;

		[Header("Terrain Validation")]
		[SerializeField]
		[Tooltip("Primary layers for terrain detection (ground, terrain, etc.)")]
		LayerMask _layerMask = -1;
		
		[SerializeField]
		[Range(0.1f, 10f)]
		[Tooltip("Maximum height difference between spawn point and terrain for valid placement")]
		float _maxHeightDifference = 2f;
		
		[SerializeField]
		[Range(1f, 50f)]
		[Tooltip("Raycast distance from above to find terrain")]
		float _raycastDistance = 20f;
		
		[SerializeField]
		[Range(0f, 90f)]
		[Tooltip("Maximum slope angle (degrees) for valid spawning")]
		float _maxSlopeAngle = 45f;
		
		[SerializeField]
		[Tooltip("Additional layers to consider as valid ground (e.g., buildings, roads)")]
		LayerMask _additionalGroundLayers = 0;
		
		[Header("Filtering")]
		[SerializeField]
		[Tooltip("Use stricter mesh bounds checking")]
		bool _useStrictBoundsCheck = true;
		
		[SerializeField]
		[Range(0.01f, 0.5f)]
		[Tooltip("Distance tolerance for inside detection (smaller = stricter)")]
		float _insideDistanceTolerance = 0.02f;

		[Header("Debug")]
		[SerializeField]
		[Tooltip("Show debug information about spawn attempts and failures")]
		bool _showDebugInfo = false;

		[Header("Appearance")]
		[SerializeField]
		bool _scaleDownWithWorld;

		[SerializeField]
		bool _randomizeScale;

		[SerializeField]
		bool _randomizeRotation;

		int _spawnedCount;
		private List<Vector3> _spawnedPositions; // Track positions for distance checking

		private Dictionary<GameObject, List<GameObject>> _objects;
		private Queue<GameObject> _pool;

		public override void Initialize()
		{
			if (_objects == null || _pool == null || _spawnedPositions == null)
			{
				_objects = new Dictionary<GameObject, List<GameObject>>();
				_pool = new Queue<GameObject>();
				_spawnedPositions = new List<Vector3>();
			}
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			_spawnedCount = 0;
			_spawnedPositions.Clear(); // Clear previous positions for this tile
			
			var bounds = ve.Mesh.bounds;
			var center = ve.Transform.position + bounds.center;
			center.y = 0;

			var area = (int)(bounds.size.x * bounds.size.z);
			int baseSpawnCount = area / _spawnRateInSquareMeters;
			
			// Apply density multiplier
			int spawnCount = Mathf.RoundToInt(baseSpawnCount * _densityMultiplier);
			spawnCount = Mathf.Min(spawnCount, _maxSpawn);
			
			// Early exit if density is too low or no prefabs
			if (spawnCount <= 0 || _prefabs.Length == 0) return;
			
			if (_showDebugInfo)
			{
				Debug.Log($"[SpawnInsideModifier] Attempting to spawn {spawnCount} objects in area {area}m² (density: {_densityMultiplier:F2})");
			}
			
			// Get mesh collider for accurate inside testing
			var meshCollider = ve.GameObject.GetComponent<MeshCollider>();
			if (meshCollider == null)
			{
				meshCollider = ve.GameObject.AddComponent<MeshCollider>();
				meshCollider.sharedMesh = ve.Mesh;
				meshCollider.convex = false; // Non-convex for accurate polygon collision
			}
			
			int attempts = 0;
			int maxAttempts = spawnCount * 50; // More attempts for stricter filtering
			
			while (_spawnedCount < spawnCount && attempts < maxAttempts)
			{
				attempts++;
				
				var x = UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x);
				var z = UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z);
				var testPoint = center + new Vector3(x, 0, z);
				
				// Use mesh collider to test if point is inside polygon with configurable tolerance
				Vector3 closestPoint = meshCollider.ClosestPoint(testPoint);
				float distanceToMesh = Vector3.Distance(testPoint, closestPoint);
				
				// Stricter inside test
				bool isInside = _useStrictBoundsCheck ? 
					distanceToMesh < _insideDistanceTolerance : 
					distanceToMesh < 0.1f;
					
				if (!isInside) continue;
				
				// Check minimum distance to other spawned objects
				if (!IsValidSpawnPosition(testPoint)) continue;
				
				// Validate terrain contact before spawning
				if (!IsValidTerrainPosition(testPoint, center.y)) continue;
				
				var ray = new Ray(testPoint + Vector3.up * _raycastDistance, Vector3.down);

				RaycastHit hit;
				LayerMask combinedLayers = _layerMask | _additionalGroundLayers;
				
				if (Physics.Raycast(ray, out hit, _raycastDistance * 2f, combinedLayers))
				{
					// Double-check that the hit point is actually on valid ground
					if (!IsGroundPositionValid(hit.point, hit.normal))
						continue;
						
					var index = UnityEngine.Random.Range(0, _prefabs.Length);
					var transform = GetObject(index, ve.GameObject).transform;
					transform.position = hit.point;
					
					if (_randomizeRotation)
					{
						transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(-180f, 180f), 0);
					}
					
					if (!_scaleDownWithWorld)
					{
						transform.localScale = Vector3.one / tile.TileScale;
					}

					if (_randomizeScale)
					{
						var scale = transform.localScale;
						var y = UnityEngine.Random.Range(scale.y * .7f, scale.y * 1.3f);
						scale.y = y;
						transform.localScale = scale;
					}

					// Track this position for distance checking
					_spawnedPositions.Add(hit.point);
					_spawnedCount++;
				}
			}
			
			if (_showDebugInfo)
			{
				Debug.Log($"[SpawnInsideModifier] Successfully spawned {_spawnedCount}/{spawnCount} objects after {attempts} attempts");
			}
		}
		
		/// <summary>
		/// Check if the proposed spawn position maintains minimum distance from existing objects
		/// </summary>
		/// <param name="position">Position to test</param>
		/// <returns>True if position is valid (not too close to existing objects)</returns>
		private bool IsValidSpawnPosition(Vector3 position)
		{
			foreach (var existingPos in _spawnedPositions)
			{
				if (Vector3.Distance(position, existingPos) < _minDistanceBetweenObjects)
				{
					return false;
				}
			}
			return true;
		}
		
		/// <summary>
		/// Validate if a position has proper terrain contact within acceptable height range
		/// </summary>
		/// <param name="testPoint">The 2D position to test</param>
		/// <param name="referenceHeight">Reference height (usually tile center height)</param>
		/// <returns>True if terrain is within acceptable height difference</returns>
		private bool IsValidTerrainPosition(Vector3 testPoint, float referenceHeight)
		{
			// Cast ray down from above to find terrain
			var ray = new Ray(testPoint + Vector3.up * _raycastDistance, Vector3.down);
			RaycastHit hit;
			
			LayerMask combinedLayers = _layerMask | _additionalGroundLayers;
			
			if (Physics.Raycast(ray, out hit, _raycastDistance * 2f, combinedLayers))
			{
				// Check if terrain height is within acceptable range
				float heightDifference = Mathf.Abs(hit.point.y - referenceHeight);
				bool isValid = heightDifference <= _maxHeightDifference;
				
				if (_showDebugInfo && !isValid)
				{
					Debug.Log($"[SpawnInsideModifier] Height check failed: {heightDifference:F2}m > {_maxHeightDifference:F2}m max");
				}
				
				return isValid;
			}
			
			if (_showDebugInfo)
			{
				Debug.Log("[SpawnInsideModifier] No terrain found for position validation");
			}
			
			// No terrain found - invalid position
			return false;
		}
		
		/// <summary>
		/// Validate if a ground hit point is suitable for spawning (not too steep, etc.)
		/// </summary>
		/// <param name="hitPoint">The raycast hit point</param>
		/// <param name="hitNormal">The surface normal at hit point</param>
		/// <returns>True if ground is suitable for spawning</returns>
		private bool IsGroundPositionValid(Vector3 hitPoint, Vector3 hitNormal)
		{
			// Check if surface is not too steep (angle with up vector)
			float angle = Vector3.Angle(hitNormal, Vector3.up);
			bool isValid = angle <= _maxSlopeAngle;
			
			if (_showDebugInfo && !isValid)
			{
				Debug.Log($"[SpawnInsideModifier] Slope check failed: {angle:F1}° > {_maxSlopeAngle:F1}° max");
			}
			
			return isValid;
		}

		public override void OnPoolItem(VectorEntity vectorEntity)
		{
			if(_objects.ContainsKey(vectorEntity.GameObject))
			{
				foreach (var item in _objects[vectorEntity.GameObject])
				{
					item.SetActive(false);
					_pool.Enqueue(item);
				}

				_objects[vectorEntity.GameObject].Clear();
				_objects.Remove(vectorEntity.GameObject);
			}
		}

		public override void Clear()
		{
			foreach (var go in _pool)
			{
				go.Destroy();
			}
			_pool.Clear();
			foreach (var tileObject in _objects)
			{
				foreach (var go in tileObject.Value)
				{
					if (Application.isEditor)
					{
						DestroyImmediate(go);
					}
					else
					{
						Destroy(go);
					}
				}
			}
			_objects.Clear();
		}

		private GameObject GetObject(int index, GameObject go)
		{
			GameObject ob;

			if (_pool.Count > 0)
			{
				ob = _pool.Dequeue();
				ob.SetActive(true);
			}
			else
			{
				ob = Instantiate(_prefabs[index], go.transform, true);
			}

			if (_objects.ContainsKey(go))
			{
				_objects[go].Add(ob);
			}
			else
			{
				_objects.Add(go, new List<GameObject>() { ob });
			}
			return ob;
		}
	}
}