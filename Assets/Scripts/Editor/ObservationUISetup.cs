using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Editor utility to quickly set up the Observation Screen UI
/// </summary>
public class ObservationUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Observation Screen UI")]
    public static void SetupUI()
    {
        // Check if already exists
        ObservationScreenUI existing = FindObjectOfType<ObservationScreenUI>();
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Screen UI Exists",
                "An ObservationScreenUI already exists in the scene. Replace it?",
                "Replace",
                "Cancel"
            );
            
            if (replace)
            {
                DestroyImmediate(existing.gameObject);
            }
            else
            {
                return;
            }
        }
        
        // Create canvas
        GameObject canvasObj = new GameObject("ObservationScreenCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create UI panel (centered)
        GameObject panelObj = new GameObject("ObservationPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500, 600);
        
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Add layout
        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        // Common name text
        GameObject commonNameObj = CreateText("CommonNameText", "Common Name", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
        commonNameObj.transform.SetParent(panelObj.transform, false);
        LayoutElement commonLayout = commonNameObj.AddComponent<LayoutElement>();
        commonLayout.preferredHeight = 40;
        
        // Scientific name text
        GameObject scientificNameObj = CreateText("ScientificNameText", "Scientific Name", 20, FontStyle.Italic, TextAnchor.MiddleCenter);
        scientificNameObj.transform.SetParent(panelObj.transform, false);
        LayoutElement sciLayout = scientificNameObj.AddComponent<LayoutElement>();
        sciLayout.preferredHeight = 30;
        
        // Photo
        GameObject photoObj = new GameObject("PhotoImage");
        photoObj.transform.SetParent(panelObj.transform, false);
        RawImage photoImage = photoObj.AddComponent<RawImage>();
        photoImage.color = Color.white;
        LayoutElement photoLayout = photoObj.AddComponent<LayoutElement>();
        photoLayout.preferredHeight = 350;
        
        // Observer text
        GameObject observerObj = CreateText("ObserverText", "Observed by: Unknown", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        observerObj.transform.SetParent(panelObj.transform, false);
        LayoutElement obsLayout = observerObj.AddComponent<LayoutElement>();
        obsLayout.preferredHeight = 25;
        
        // Date text
        GameObject dateObj = CreateText("DateText", "Date: Unknown", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        dateObj.transform.SetParent(panelObj.transform, false);
        LayoutElement dateLayout = dateObj.AddComponent<LayoutElement>();
        dateLayout.preferredHeight = 25;
        
        // Add ObservationScreenUI component
        ObservationScreenUI screenUI = canvasObj.AddComponent<ObservationScreenUI>();
        
        // Assign references using reflection since fields are private with SerializeField
        SerializedObject so = new SerializedObject(screenUI);
        so.FindProperty("screenCanvas").objectReferenceValue = canvas;
        so.FindProperty("uiPanel").objectReferenceValue = panelObj;
        so.FindProperty("commonNameText").objectReferenceValue = commonNameObj.GetComponent<Text>();
        so.FindProperty("scientificNameText").objectReferenceValue = scientificNameObj.GetComponent<Text>();
        so.FindProperty("observerText").objectReferenceValue = observerObj.GetComponent<Text>();
        so.FindProperty("dateText").objectReferenceValue = dateObj.GetComponent<Text>();
        so.FindProperty("photoImage").objectReferenceValue = photoImage;
        so.ApplyModifiedProperties();
        
        // Select the created object
        Selection.activeGameObject = canvasObj;
        
        EditorUtility.DisplayDialog(
            "Setup Complete",
            "Observation Screen UI has been created!\n\n" +
            "Make sure your observation prefabs have:\n" +
            "- ObservationDisplay component\n" +
            "- ObservationTriggerInteraction with 'Use Screen UI' enabled",
            "OK"
        );
        
        Debug.Log("<color=green>Observation Screen UI setup complete!</color>");
    }
    
    private static GameObject CreateText(string name, string content, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject textObj = new GameObject(name);
        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.resizeTextForBestFit = false;
        
        return textObj;
    }
}
