using UnityEngine;
using Mapbox.Unity.Map;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controller script to manually trigger biodiversity spawning.
/// Attach this to your AbstractMap GameObject to control BIO_SpawnInsideModifier.
/// </summary>
public class BiodiversitySpawnController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to your AbstractMap (auto-found if null)")]
    public AbstractMap map;

    [Tooltip("Optional: Manually assign BIO_SpawnInsideModifier ScriptableObject asset (auto-found if null)")]
    [SerializeField] private BIO_SpawnInsideModifier spawnModifierAsset;

    [Header("Manual Controls")]
    [Tooltip("Press this key to force spawn/respawn biodiversity prefabs")]
    public KeyCode forceSpawnKey = KeyCode.B;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private BIO_SpawnInsideModifier _spawnModifier;

    void Start()
    {
        // Auto-find map if not assigned
        if (map == null)
        {
            map = GetComponent<AbstractMap>();
            if (map == null)
            {
                Debug.LogError("[BiodiversitySpawnController] No AbstractMap found! Please assign it in the Inspector.");
                enabled = false;
                return;
            }
        }

        // Use manually assigned modifier asset, or auto-find it
        if (spawnModifierAsset != null)
        {
            _spawnModifier = spawnModifierAsset;
            if (showDebugInfo)
            {
                Debug.Log($"[BiodiversitySpawnController] Using manually assigned BIO_SpawnInsideModifier: {_spawnModifier.name}");
            }
        }
        else
        {
            // Find the BIO_SpawnInsideModifier automatically
            FindSpawnModifier();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[BiodiversitySpawnController] Initialized. Press '{forceSpawnKey}' to force spawn biodiversity prefabs.");
        }
    }

    void Update()
    {
        // Manual spawn trigger
        if (Input.GetKeyDown(forceSpawnKey))
        {
            ForceSpawnBiodiversity();
        }
    }

    /// <summary>
    /// Finds the BIO_SpawnInsideModifier from loaded ScriptableObject assets
    /// </summary>
    private void FindSpawnModifier()
    {
        // Method 1: Find all instances of BIO_SpawnInsideModifier in Resources
        #if UNITY_EDITOR
        // In editor, we can use AssetDatabase
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BIO_SpawnInsideModifier");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var modifier = UnityEditor.AssetDatabase.LoadAssetAtPath<BIO_SpawnInsideModifier>(path);
            if (modifier != null)
            {
                _spawnModifier = modifier;
                if (showDebugInfo)
                {
                    Debug.Log($"[BiodiversitySpawnController] Found BIO_SpawnInsideModifier at: {path}");
                }
                return;
            }
        }
        #else
        // At runtime, try to find it via Resources
        var modifiers = Resources.FindObjectsOfTypeAll<BIO_SpawnInsideModifier>();
        if (modifiers.Length > 0)
        {
            _spawnModifier = modifiers[0];
            if (showDebugInfo)
            {
                Debug.Log($"[BiodiversitySpawnController] Found BIO_SpawnInsideModifier: {_spawnModifier.name}");
            }
            return;
        }
        #endif

        if (_spawnModifier == null)
        {
            Debug.LogWarning("[BiodiversitySpawnController] BIO_SpawnInsideModifier not found! " +
                           "Make sure it exists as an asset and is assigned to your map's GameObject modifiers.");
        }
    }

    /// <summary>
    /// Manually force spawn/respawn biodiversity prefabs
    /// </summary>
    public void ForceSpawnBiodiversity()
    {
        if (_spawnModifier == null)
        {
            Debug.LogWarning("[BiodiversitySpawnController] Cannot force spawn - BIO_SpawnInsideModifier not found!");
            FindSpawnModifier(); // Try finding it again

            if (_spawnModifier == null)
            {
                Debug.LogError("[BiodiversitySpawnController] Still cannot find BIO_SpawnInsideModifier!");
                return;
            }
        }

        Debug.Log("[BiodiversitySpawnController] Force spawning biodiversity prefabs...");
        _spawnModifier.ForceSpawn();
    }

    /// <summary>
    /// Get reference to the spawn modifier (useful for other scripts)
    /// </summary>
    public BIO_SpawnInsideModifier GetSpawnModifier()
    {
        if (_spawnModifier == null)
        {
            FindSpawnModifier();
        }
        return _spawnModifier;
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // Draw debug UI
        GUILayout.BeginArea(new Rect(10, Screen.height - 100, 400, 90));
        GUILayout.BeginVertical("box");

        GUILayout.Label("Biodiversity Spawn Controls", GUI.skin.box);
        GUILayout.Label($"Press [{forceSpawnKey}] to force spawn/respawn");

        if (_spawnModifier == null)
        {
            GUILayout.Label("⚠️ BIO_SpawnInsideModifier NOT FOUND", GUI.skin.box);
        }
        else
        {
            GUILayout.Label("✓ BIO_SpawnInsideModifier ready", GUI.skin.box);
        }

        if (GUILayout.Button("Force Spawn Now"))
        {
            ForceSpawnBiodiversity();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
