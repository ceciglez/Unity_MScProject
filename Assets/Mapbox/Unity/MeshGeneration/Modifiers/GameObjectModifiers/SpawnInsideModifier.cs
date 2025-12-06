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
	using Mapbox.VectorTile.Geometry;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Spawn Inside Modifier")]
	public class SpawnInsideModifier : GameObjectModifier
	{
		[SerializeField]
		int _spawnRateInSquareMeters;

		[SerializeField]
		int _maxSpawn = 1000;

		//a slider for the inspector
		[SerializeField]
		[Range(0.1f, 5.0f)]
		float _densityMultiplier = 1.0f;

		// min distance between objects
		[SerializeField]
		[Range(1f, 100.0f)]
		float _minDistanceBetweenObjects = 2.0f;

		[SerializeField]
		GameObject[] _prefabs;

		[SerializeField]
		LayerMask _layerMask;

		//avoid objects spawning on water or other layers selected
		[SerializeField]
		[Tooltip("Layers to avoid spawning on (e.g., water, buildings)")]
		LayerMask _exclusionLayerMask;

		[SerializeField]
		[Tooltip("Layer to assign to the parent mesh GameObject")]
		int _meshLayer = 0;

		[SerializeField]
		[Tooltip("Materials to exclude spawning on (drag materials from assets)")]
		Material[] _excludedMaterials;

		[SerializeField]
		[Tooltip("Feature types to avoid (water, buildings, etc.)")]
		string[] _excludedFeatureTypes = { "water", "building", "landuse" };

		[SerializeField]
		[Tooltip("Specific feature classes to exclude")]
		string[] _excludedFeatureClasses = { "lake", "river", "pond", "swimming_pool" };

		[SerializeField]
		bool _scaleDownWithWorld;

		[SerializeField]
		bool _randomizeScale;

		[SerializeField]
		bool _randomizeRotation;

		int _spawnedCount;

		private Dictionary<GameObject, List<GameObject>> _objects;
		private Queue<GameObject> _pool;
		private List<Vector3> _spawnedPositions;

		public override void Initialize()
		{
			if (_objects == null || _pool == null)
			{
				_objects = new Dictionary<GameObject, List<GameObject>>();
				_pool = new Queue<GameObject>();
			}
			if (_spawnedPositions == null)
			{
				_spawnedPositions = new List<Vector3>();
			}
		}


//generated with LLM to stick prefabs to terrain ground within a mesh area
		public override void Run(VectorEntity ve, UnityTile tile)
		{
			// Check if this vector entity should be excluded based on its properties
			if (ShouldExcludeVectorEntity(ve))
			{
				return; // Don't spawn anything on this feature
			}

			// Assign layer to the mesh GameObject if specified
			if (_meshLayer > 0)
			{
				ve.GameObject.layer = _meshLayer;
			}
			
			_spawnedCount = 0;
			_spawnedPositions.Clear();
			var bounds = ve.Mesh.bounds;
			var center = ve.Transform.position + bounds.center;
			center.y = 0;

			var area = (int)(bounds.size.x * bounds.size.z);
			int spawnCount = Mathf.Min(Mathf.RoundToInt((area / _spawnRateInSquareMeters) * _densityMultiplier), _maxSpawn);
			while (_spawnedCount < spawnCount)
			{
				var x = UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x);
				var z = UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z);
				var ray = new Ray(center + new Vector3(x, 100, z), Vector3.down * 2000);

				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, 150, _layerMask))
				{
					// Check if spawn position overlaps with excluded layers (like water)
					if (IsPositionExcluded(hit.point))
					{
						_spawnedCount++;
						continue;
					}

					// Check if this position is too close to existing spawned objects
					if (IsValidSpawnPosition(hit.point))
					{
						var index = UnityEngine.Random.Range(0, _prefabs.Length);
						var transform = GetObject(index, ve.GameObject).transform;
						transform.position = hit.point;
						
						// Add this position to our tracking list
						_spawnedPositions.Add(hit.point);
						
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
					}
				}
				_spawnedCount++;
			}
		}

	// to manage spawned objects cleanup
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
					if (Application.isEditor && !Application.isPlaying)
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
				ob.transform.SetParent(go.transform);
			}
			else
			{
				ob = ((GameObject)Instantiate(_prefabs[index], go.transform, false));
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

		private bool IsValidSpawnPosition(Vector3 newPosition)
		{
			foreach (var existingPosition in _spawnedPositions)
			{
				float distance = Vector3.Distance(newPosition, existingPosition);
				if (distance < _minDistanceBetweenObjects)
				{
					return false;
				}
			}
			return true;
		}

		private bool IsPositionExcluded(Vector3 position)
		{
			// Check for overlapping exclusion layers (water, buildings, etc.)
			var ray = new Ray(position + Vector3.up * 0.1f, Vector3.down);
			RaycastHit exclusionHit;
			
			// Check both upward and downward for water surfaces
			if (Physics.Raycast(ray, out exclusionHit, 0.2f, _exclusionLayerMask))
			{
				return true;
			}
			
			// Also check slightly above the position for water surfaces
			var upwardRay = new Ray(position + Vector3.down * 0.1f, Vector3.up);
			if (Physics.Raycast(upwardRay, out exclusionHit, 0.2f, _exclusionLayerMask))
			{
				return true;
			}
			
			// Check material references if specified
			if (_excludedMaterials != null && _excludedMaterials.Length > 0)
			{
				RaycastHit materialHit;
				var materialRay = new Ray(position + Vector3.up * 0.1f, Vector3.down);
				if (Physics.Raycast(materialRay, out materialHit, 0.5f))
				{
					var renderer = materialHit.collider.GetComponent<Renderer>();
					if (renderer != null && renderer.material != null)
					{
						foreach (Material excludedMaterial in _excludedMaterials)
						{
							if (excludedMaterial != null && renderer.material == excludedMaterial)
							{
								return true;
							}
							// Also check shared material in case of material instances
							if (excludedMaterial != null && renderer.sharedMaterial == excludedMaterial)
							{
								return true;
							}
						}
					}
				}
			}
			
			return false;
		}

		private bool ShouldExcludeVectorEntity(VectorEntity ve)
		{
			// Check if this vector entity has properties that indicate it should be excluded
			if (ve.Feature?.Properties != null)
			{
				foreach (var property in ve.Feature.Properties)
				{
					string key = property.Key.ToLower();
					string value = property.Value?.ToString()?.ToLower() ?? "";

					// Check feature types (landuse, natural, etc.)
					foreach (string excludedType in _excludedFeatureTypes)
					{
						if (key.Contains(excludedType.ToLower()) || value.Contains(excludedType.ToLower()))
						{
							return true;
						}
					}

					// Check specific feature classes
					foreach (string excludedClass in _excludedFeatureClasses)
					{
						if (value.Contains(excludedClass.ToLower()))
						{
							return true;
						}
					}

					// Common water-related properties
					if (key == "natural" && (value == "water" || value == "lake" || value == "river"))
					{
						return true;
					}
					
					if (key == "landuse" && (value == "reservoir" || value == "basin"))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
