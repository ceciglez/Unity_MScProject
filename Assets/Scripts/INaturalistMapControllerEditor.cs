using UnityEngine;
using UnityEditor;
using TMPro;

#if UNITY_EDITOR
/// <summary>
/// Custom Editor for INaturalistMapController to add helpful buttons and info
/// </summary>
[CustomEditor(typeof(INaturalistMapController))]
public class INaturalistMapControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        INaturalistMapController controller = (INaturalistMapController)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        
        // Reload button
        if (GUILayout.Button("Reload Observations"))
        {
            if (Application.isPlaying)
            {
                controller.ReloadData();
            }
            else
            {
                EditorUtility.DisplayDialog("Not Playing", 
                    "Please enter Play mode to reload observations.", "OK");
            }
        }
        
        // Clear button
        if (GUILayout.Button("Clear All Observations"))
        {
            if (Application.isPlaying)
            {
                controller.ClearObservations();
            }
            else
            {
                EditorUtility.DisplayDialog("Not Playing", 
                    "Please enter Play mode to clear observations.", "OK");
            }
        }
        
        EditorGUILayout.Space();
        
        // Quick setup buttons
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Observation Prefab"))
        {
            CreateObservationPrefab();
        }
        
        if (GUILayout.Button("Setup Tooltip UI"))
        {
            CreateTooltipUI();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Make sure to:\n" +
            "1. Install Mapbox SDK\n" +
            "2. Assign AbstractMap reference\n" +
            "3. Create and assign Observation Prefab\n" +
            "4. Setup Tooltip UI (optional)", 
            MessageType.Info);
    }
    
    private void CreateObservationPrefab()
    {
        // Create a simple sphere prefab for observations
        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obs.name = "ObservationPrefab";
        obs.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Create material
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(76f/255f, 175f/255f, 80f/255f, 0.8f);
        obs.GetComponent<Renderer>().material = mat;
        
        // Add ObservationDisplay component
        obs.AddComponent<ObservationDisplay>();
        
        // Save as prefab
        string path = "Assets/ObservationPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(obs, path);
        
        // Cleanup
        DestroyImmediate(obs);
        
        EditorUtility.DisplayDialog("Prefab Created", 
            $"Observation prefab created at {path}\n\nDon't forget to assign it in the inspector!", "OK");
        
        // Select the prefab
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
    
    private void CreateTooltipUI()
    {
        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create tooltip panel
        GameObject panel = new GameObject("TooltipPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 400);
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f);
        
        panel.AddComponent<CanvasGroup>();
        
        // Add UI elements
        CreateUIElement(panel, "Photo", 280, 200, 0, 200, typeof(TextMeshProUGUI));
        CreateUIElement(panel, "CommonName", 280, 30, 0, 160, typeof(TextMeshProUGUI));
        CreateUIElement(panel, "ScientificName", 280, 25, 0, 130, typeof(TextMeshProUGUI));
        CreateUIElement(panel, "ObservedDate", 280, 20, 0, 105, typeof(TextMeshProUGUI));
        CreateUIElement(panel, "Observer", 280, 20, 0, 80, typeof(TextMeshProUGUI));
        CreateUIElement(panel, "Location", 280, 20, 0, 55, typeof(TextMeshProUGUI));
        
        // Add tooltip component
        panel.AddComponent<ObservationTooltip>();
        
        EditorUtility.DisplayDialog("Tooltip Created", 
            "Tooltip UI created!\n\nPlease assign all UI references in the ObservationTooltip component.", "OK");
        
        Selection.activeGameObject = panel;
    }
    
    private GameObject CreateUIElement(GameObject parent, string name, float width, float height, float x, float y, System.Type componentType)
    {
        GameObject element = new GameObject(name);
        element.transform.SetParent(parent.transform, false);
        
        RectTransform rt = element.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
        
        element.AddComponent(componentType);
        
        return element;
    }
}
#endif
