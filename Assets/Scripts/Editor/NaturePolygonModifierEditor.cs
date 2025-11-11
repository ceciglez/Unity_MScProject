using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to auto-populate NaturePolygonModifier with Low Poly Nature Bundle assets
/// </summary>
[CustomEditor(typeof(NaturePolygonModifier))]
public class NaturePolygonModifierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        NaturePolygonModifier modifier = (NaturePolygonModifier)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Load Trees from Low Poly Bundle"))
        {
            LoadTreePrefabs(modifier);
        }
        
        if (GUILayout.Button("Load Vegetation from Low Poly Bundle"))
        {
            LoadVegetationPrefabs(modifier);
        }
        
        if (GUILayout.Button("Load Rocks from Low Poly Bundle"))
        {
            LoadRockPrefabs(modifier);
        }
        
        if (GUILayout.Button("Load Mixed Nature Assets"))
        {
            LoadMixedAssets(modifier);
        }
        
        EditorGUILayout.Space();
        
        if (modifier.naturePrefabs != null && modifier.naturePrefabs.Length > 0)
        {
            EditorGUILayout.HelpBox($"Currently loaded: {modifier.naturePrefabs.Length} prefabs", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No prefabs loaded. Click a button above to auto-load assets.", MessageType.Warning);
        }
    }
    
    private void LoadTreePrefabs(NaturePolygonModifier modifier)
    {
        string[] searchPaths = new string[]
        {
            "Assets/LMHPOLY/Low Poly Nature Bundle/Trees/Tree Assets/Prefabs/Trees"
        };
        
        List<GameObject> prefabs = new List<GameObject>();
        
        foreach (var path in searchPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // Filter out snow, LOD, and other variants
                if (assetPath.Contains("Snow") || assetPath.Contains("LOD"))
                    continue;
                
                // Only get mesh collider versions
                if (!assetPath.Contains("Mesh_Colliders"))
                    continue;
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                
                // Limit to reasonable number
                if (prefabs.Count >= 30)
                    break;
            }
            
            if (prefabs.Count >= 30)
                break;
        }
        
        if (prefabs.Count > 0)
        {
            Undo.RecordObject(modifier, "Load Tree Prefabs");
            modifier.naturePrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(modifier);
            Debug.Log($"<color=green>Loaded {prefabs.Count} tree prefabs</color>");
        }
        else
        {
            Debug.LogWarning("No tree prefabs found. Check the path: Assets/LMHPOLY/Low Poly Nature Bundle/Trees");
        }
    }
    
    private void LoadVegetationPrefabs(NaturePolygonModifier modifier)
    {
        string[] searchPaths = new string[]
        {
            "Assets/LMHPOLY/Low Poly Nature Bundle/Vegetation/Vegetation Assets/Prefabs"
        };
        
        List<GameObject> prefabs = new List<GameObject>();
        
        foreach (var path in searchPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // Filter out snow
                if (assetPath.Contains("Snow"))
                    continue;
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                
                if (prefabs.Count >= 30)
                    break;
            }
            
            if (prefabs.Count >= 30)
                break;
        }
        
        if (prefabs.Count > 0)
        {
            Undo.RecordObject(modifier, "Load Vegetation Prefabs");
            modifier.naturePrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(modifier);
            Debug.Log($"<color=green>Loaded {prefabs.Count} vegetation prefabs</color>");
        }
        else
        {
            Debug.LogWarning("No vegetation prefabs found.");
        }
    }
    
    private void LoadRockPrefabs(NaturePolygonModifier modifier)
    {
        string[] searchPaths = new string[]
        {
            "Assets/LMHPOLY/Low Poly Nature Bundle/Rocks/Rock Assets/Prefabs"
        };
        
        List<GameObject> prefabs = new List<GameObject>();
        
        foreach (var path in searchPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (assetPath.Contains("Snow"))
                    continue;
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
                
                if (prefabs.Count >= 30)
                    break;
            }
            
            if (prefabs.Count >= 30)
                break;
        }
        
        if (prefabs.Count > 0)
        {
            Undo.RecordObject(modifier, "Load Rock Prefabs");
            modifier.naturePrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(modifier);
            Debug.Log($"<color=green>Loaded {prefabs.Count} rock prefabs</color>");
        }
        else
        {
            Debug.LogWarning("No rock prefabs found.");
        }
    }
    
    private void LoadMixedAssets(NaturePolygonModifier modifier)
    {
        string[] searchPaths = new string[]
        {
            "Assets/LMHPOLY/Low Poly Nature Bundle/Trees/Tree Assets/Prefabs/Trees",
            "Assets/LMHPOLY/Low Poly Nature Bundle/Vegetation/Vegetation Assets/Prefabs",
            "Assets/LMHPOLY/Low Poly Nature Bundle/Rocks/Rock Assets/Prefabs"
        };
        
        List<GameObject> prefabs = new List<GameObject>();
        
        foreach (var path in searchPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            
            foreach (var guid in guids.Take(10)) // Take 10 from each category
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (assetPath.Contains("Snow") || (assetPath.Contains("Trees") && assetPath.Contains("LOD")))
                    continue;
                
                // For trees, prefer mesh collider versions
                if (assetPath.Contains("Trees") && !assetPath.Contains("Mesh_Colliders"))
                    continue;
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }
        }
        
        if (prefabs.Count > 0)
        {
            Undo.RecordObject(modifier, "Load Mixed Assets");
            modifier.naturePrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(modifier);
            Debug.Log($"<color=green>Loaded {prefabs.Count} mixed nature prefabs (trees, vegetation, rocks)</color>");
        }
        else
        {
            Debug.LogWarning("No prefabs found in Low Poly Nature Bundle.");
        }
    }
}
