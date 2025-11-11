using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// Debug overlay showing player and observation coordinates
/// </summary>
public class DebugCoordinateOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AbstractMap map;
    [SerializeField] private Transform playerTransform;
    
    [Header("UI Settings")]
    [SerializeField] private int fontSize = 12;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    
    private Canvas debugCanvas;
    private Text debugText;
    private GameObject[] observationObjects;
    
    public void SetFontSize(int size)
    {
        fontSize = size;
        if (debugText != null)
        {
            debugText.fontSize = size;
        }
    }
    
    void Start()
    {
        // Find map if not assigned
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map != null)
            {
                Debug.Log($"DebugOverlay: Found map: {map.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("DebugOverlay: No AbstractMap found in scene!");
            }
        }
        
        // Find player if not assigned
        if (playerTransform == null)
        {
            var kccController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
            if (kccController != null)
            {
                playerTransform = kccController.transform;
                Debug.Log($"DebugOverlay: Found player: {playerTransform.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("DebugOverlay: No KCC ExampleCharacterController found!");
            }
        }
        
        CreateDebugUI();
    }
    
    void CreateDebugUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("DebugCoordinateCanvas");
        canvasObj.transform.SetParent(transform);
        
        debugCanvas = canvasObj.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        debugCanvas.sortingOrder = 1000; // Make sure it's on top
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create background panel
        GameObject panelObj = new GameObject("DebugPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = backgroundColor;
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(10, -10);
        panelRect.sizeDelta = new Vector2(700, 400); // Made bigger
        
        // Create text
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        debugText = textObj.AddComponent<Text>();
        
        // Try to get LegacyRuntime font (Unity's new default built-in font)
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont != null)
        {
            debugText.font = defaultFont;
            Debug.Log("DebugOverlay: LegacyRuntime font loaded successfully");
        }
        else
        {
            Debug.LogWarning("DebugOverlay: LegacyRuntime font not available, text may not render");
        }
        
        debugText.fontSize = fontSize;
        debugText.color = textColor;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.supportRichText = false; // Disable rich text for now
        debugText.horizontalOverflow = HorizontalWrapMode.Overflow;
        debugText.verticalOverflow = VerticalWrapMode.Overflow;
        debugText.raycastTarget = false;
        debugText.enabled = true;
        debugText.text = "DEBUG OVERLAY LOADED\nWaiting for data..."; // Set initial text
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        Debug.Log($"DebugCoordinateOverlay: UI created - Text: '{debugText.text}', Font: {debugText.font?.name ?? "NULL"}, Material: {debugText.material?.name ?? "NULL"}, Color: {debugText.color}");
    }
    
    void Update()
    {
        if (debugText == null || map == null)
        {
            if (Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.LogWarning($"DebugCoordinateOverlay: Update check - debugText: {debugText != null}, map: {map != null}");
            }
            return;
        }
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== COORDINATE DEBUG INFO ==="); // Removed bold tag to test
        sb.AppendLine();
        
        // Player info
        if (playerTransform != null)
        {
            Vector3 playerWorldPos = playerTransform.position;
            Vector2d playerLatLng = map.WorldToGeoPosition(playerWorldPos);
            
            sb.AppendLine("PLAYER:");
            sb.AppendLine($"  World Pos: ({playerWorldPos.x:F2}, {playerWorldPos.y:F2}, {playerWorldPos.z:F2})");
            sb.AppendLine($"  Lat/Lng: ({playerLatLng.x:F6}, {playerLatLng.y:F6})");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("PLAYER: Not found");
            sb.AppendLine();
        }
        
        // Find observations by component (don't use tags)
        ObservationDisplay[] displays = FindObjectsOfType<ObservationDisplay>();
        observationObjects = new GameObject[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            observationObjects[i] = displays[i].gameObject;
        }
        
        sb.AppendLine($"OBSERVATIONS: {observationObjects.Length} found");
        
        // Show closest 3 observations
        if (playerTransform != null && observationObjects.Length > 0)
        {
            // Sort by distance
            System.Array.Sort(observationObjects, (a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;
                float distA = Vector3.Distance(playerTransform.position, a.transform.position);
                float distB = Vector3.Distance(playerTransform.position, b.transform.position);
                return distA.CompareTo(distB);
            });
            
            int count = Mathf.Min(3, observationObjects.Length);
            for (int i = 0; i < count; i++)
            {
                if (observationObjects[i] == null) continue;
                
                GameObject obs = observationObjects[i];
                Vector3 obsWorldPos = obs.transform.position;
                Vector2d obsLatLng = map.WorldToGeoPosition(obsWorldPos);
                float distance = Vector3.Distance(playerTransform.position, obsWorldPos);
                
                ObservationDisplay display = obs.GetComponent<ObservationDisplay>();
                string name = "Unknown";
                if (display != null && display.GetData() != null)
                {
                    name = display.GetData().taxon?.preferred_common_name ?? 
                           display.GetData().taxon?.name ?? "Unknown";
                    if (name.Length > 25) name = name.Substring(0, 25) + "...";
                }
                
                sb.AppendLine($"  #{i + 1}: {name}");
                sb.AppendLine($"    World: ({obsWorldPos.x:F2}, {obsWorldPos.y:F2}, {obsWorldPos.z:F2})");
                sb.AppendLine($"    LatLng: ({obsLatLng.x:F6}, {obsLatLng.y:F6})");
                sb.AppendLine($"    Distance: {distance:F2}m");
                
                // Check canvas state
                if (display != null)
                {
                    sb.AppendLine($"    Canvas Visible: {display.IsCanvasVisible()}");
                }
                sb.AppendLine();
            }
        }
        
        debugText.text = sb.ToString();
    }
    
    void OnDestroy()
    {
        if (debugCanvas != null)
        {
            Destroy(debugCanvas.gameObject);
        }
    }
}
