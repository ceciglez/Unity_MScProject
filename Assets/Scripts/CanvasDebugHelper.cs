using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to find and disable unwanted screen-space canvases
/// </summary>
public class CanvasDebugHelper : MonoBehaviour
{
    [Header("Debug Actions")]
    [Tooltip("Find all canvases in scene")]
    public bool findAllCanvases = false;
    
    [Tooltip("Disable all ScreenSpaceOverlay canvases")]
    public bool disableScreenSpaceCanvases = false;
    
    [Tooltip("Disable DebugCoordinateOverlay components")]
    public bool disableDebugCoordinateOverlay = false;
    
    [Tooltip("Find and disable PrefabUIController objects")]
    public bool disablePrefabUIController = false;

    void Start()
    {
        if (findAllCanvases)
        {
            FindAllCanvases();
        }
        
        if (disableScreenSpaceCanvases)
        {
            DisableScreenSpaceCanvases();
        }
        
        if (disableDebugCoordinateOverlay)
        {
            DisableDebugCoordinateOverlay();
        }
        
        if (disablePrefabUIController)
        {
            DisablePrefabUIController();
        }
    }
    
    void Update()
    {
        // Allow runtime control with keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.F1))
        {
            FindAllCanvases();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            DisableScreenSpaceCanvases();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            DisableDebugCoordinateOverlay();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            DisablePrefabUIController();
        }
    }
    
    public void FindAllCanvases()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true); // Include inactive
        Debug.Log($"[CanvasDebugHelper] Found {allCanvases.Length} canvases in scene:");
        
        for (int i = 0; i < allCanvases.Length; i++)
        {
            Canvas canvas = allCanvases[i];
            string status = canvas.gameObject.activeInHierarchy ? "ACTIVE" : "INACTIVE";
            string parent = canvas.transform.parent ? canvas.transform.parent.name : "ROOT";
            
            Debug.Log($"  [{i}] {canvas.name} - {canvas.renderMode} - {status} - Parent: {parent}");
            
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"    ⚠️ SCREEN SPACE OVERLAY CANVAS: {canvas.name}");
            }
        }
    }
    
    public void DisableScreenSpaceCanvases()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        int disabledCount = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Skip minimap and other essential UI
                if (canvas.name.ToLower().Contains("minimap") || 
                    canvas.name.ToLower().Contains("main") ||
                    canvas.name.ToLower().Contains("hud"))
                {
                    Debug.Log($"[CanvasDebugHelper] Keeping essential canvas: {canvas.name}");
                    continue;
                }
                
                Debug.Log($"[CanvasDebugHelper] Disabling ScreenSpaceOverlay canvas: {canvas.name}");
                canvas.gameObject.SetActive(false);
                disabledCount++;
            }
        }
        
        Debug.Log($"[CanvasDebugHelper] Disabled {disabledCount} screen space overlay canvases");
    }
    
    public void DisableDebugCoordinateOverlay()
    {
        DebugCoordinateOverlay[] debugOverlays = FindObjectsOfType<DebugCoordinateOverlay>(true);
        
        foreach (DebugCoordinateOverlay overlay in debugOverlays)
        {
            Debug.Log($"[CanvasDebugHelper] Disabling DebugCoordinateOverlay on: {overlay.gameObject.name}");
            overlay.gameObject.SetActive(false);
        }
        
        Debug.Log($"[CanvasDebugHelper] Disabled {debugOverlays.Length} DebugCoordinateOverlay components");
    }
    
    public void DisablePrefabUIController()
    {
        // Find GameObjects with PrefabUIController in name or components
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int disabledCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // Check if GameObject name contains PrefabUIController
            if (obj.name.ToLower().Contains("prefabuicontroller") || 
                obj.name.ToLower().Contains("prefab ui controller"))
            {
                Debug.Log($"[CanvasDebugHelper] Disabling PrefabUIController GameObject: {obj.name}");
                obj.SetActive(false);
                disabledCount++;
                continue;
            }
            
            // Check if it has any components with PrefabUIController in type name
            Component[] components = obj.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp != null && comp.GetType().Name.Contains("PrefabUIController"))
                {
                    Debug.Log($"[CanvasDebugHelper] Disabling GameObject with PrefabUIController component: {obj.name}");
                    obj.SetActive(false);
                    disabledCount++;
                    break;
                }
            }
        }
        
        Debug.Log($"[CanvasDebugHelper] Disabled {disabledCount} PrefabUIController objects");
    }
}