using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for BiodiversitySpawnController with easy-to-use buttons
/// </summary>
[CustomEditor(typeof(BiodiversitySpawnController))]
public class BiodiversitySpawnControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BiodiversitySpawnController controller = (BiodiversitySpawnController)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Manual Controls", EditorStyles.boldLabel);

        // Force Spawn button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🌳 Force Spawn Biodiversity Prefabs", GUILayout.Height(40)))
        {
            if (Application.isPlaying)
            {
                controller.ForceSpawnBiodiversity();
                Debug.Log("[Editor] Force spawn triggered!");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Cannot Force Spawn",
                    "You must be in Play Mode to force spawn biodiversity prefabs.",
                    "OK"
                );
            }
        }
        GUI.backgroundColor = Color.white;

        // Status info
        EditorGUILayout.Space(5);
        if (Application.isPlaying)
        {
            var modifier = controller.GetSpawnModifier();
            if (modifier != null)
            {
                EditorGUILayout.HelpBox("✓ BIO_SpawnInsideModifier found and ready!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ BIO_SpawnInsideModifier not found! Check your map layer configuration.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use manual controls.", MessageType.Info);
        }
    }
}
