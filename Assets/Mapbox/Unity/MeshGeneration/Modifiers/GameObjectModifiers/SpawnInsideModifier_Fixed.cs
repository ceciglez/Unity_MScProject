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
		[Range(0.1f, 10f)]
		[Tooltip("Minimum distance between spawned objects")]
		float _minDistanceBetweenObjects = 3f;

		[Header("Prefab Settings")]
		[SerializeField]
		GameObject[] _prefabs;

		[Header("Physics")]
		[SerializeField]
		LayerMask _layerMask;
		
		[Header("Filtering")]
		[SerializeField]
		[Tooltip("Use stricter mesh bounds checking")]
		bool _useStrictBoundsCheck = true;
		
		[SerializeField]
		[Range(0.01f, 0.5f)]
		[Tooltip("Distance tolerance for inside detection (smaller = stricter)")]
		float _insideDistanceTolerance = 0.02f;

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
				
				var ray = new Ray(testPoint + Vector3.up * 100, Vector3.down * 2000);

				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, 150, _layerMask))
				{
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