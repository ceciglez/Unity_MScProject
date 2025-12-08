using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkConnection : MonoBehaviour
{
    [Header("Line Styling")]
    [Tooltip("Width of the connection lines")]
    public float lineWidth = 0.5f;
    
    [Tooltip("Opacity of the connection lines (0-1)")]
    [Range(0f, 1f)]
    public float lineOpacity = 0.3f;
    
    [Tooltip("Height above terrain for lines")]
    public float terrainOffset = 0.1f;
    
    [Tooltip("Number of points for terrain-following curves")]
    public int curveResolution = 10;
    
    private LineRenderer lineRenderer;
    private bool isActive = false;
    
    // Start is called before the first frame update
    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = curveResolution;
        lineRenderer.enabled = false;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        
        // Create transparent material using reliable Sprites/Default shader
        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = new Color(0f, 1f, 1f, lineOpacity); // Cyan with custom opacity
        
        // Simple transparency setup
        lineMaterial.SetFloat("_Mode", 2); // Fade mode
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.DisableKeyword("_ALPHATEST_ON");
        lineMaterial.EnableKeyword("_ALPHABLEND_ON");
        lineMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        lineMaterial.renderQueue = 3000; // Standard transparent queue
        
        lineRenderer.material = lineMaterial;
        lineRenderer.useWorldSpace = true;
        lineRenderer.allowOcclusionWhenDynamic = false;
        
        // Set rendering order to be behind other objects but still visible
        lineRenderer.sortingOrder = -10; // Less negative to ensure visibility
        lineRenderer.sortingLayerName = "Default"; // Use default layer for now
        
        // Disable shadows to prevent lines from casting/receiving shadows
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        
        // Set the GameObject layer to Network
        gameObject.layer = LayerMask.NameToLayer("Network");
        
        Debug.Log($"[NetworkConnection] Created terrain-following LineRenderer with width: {lineWidth}, opacity: {lineOpacity}, layer: Network, sortingOrder: -100");
    }

    
    public void SetConnection(Vector3 start, Vector3 end, Color color)
    {
        // Update line width and opacity from inspector
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        
        // Create terrain-following curve
        Vector3[] curvePoints = new Vector3[curveResolution];
        
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 interpolatedPos = Vector3.Lerp(start, end, t);
            
            // Raycast down to find terrain height, ignoring observation prefabs
            int terrainLayerMask = ~(1 << LayerMask.NameToLayer("Network")); // Ignore Network layer
            if (Physics.Raycast(interpolatedPos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, terrainLayerMask))
            {
                // Use the terrain hit point, not the observation prefab position
                curvePoints[i] = hit.point + Vector3.up * terrainOffset;
            }
            else
            {
                // Fallback: use interpolated position at terrain offset height
                curvePoints[i] = new Vector3(interpolatedPos.x, 0f, interpolatedPos.z) + Vector3.up * terrainOffset;
            }
        }
        
        lineRenderer.positionCount = curveResolution;
        lineRenderer.SetPositions(curvePoints);
        
        // Set color using material with inspector opacity
        if (lineRenderer.material != null)
        {
            Color transparentColor = new Color(color.r, color.g, color.b, lineOpacity);
            lineRenderer.material.color = transparentColor;
        }
        
        lineRenderer.enabled = true;
        isActive = true;
        
        float distance = Vector3.Distance(start, end);
        Debug.Log($"[NetworkConnection] Terrain-following line created: {start} to {end}, distance: {distance:F1}m, points: {curveResolution}, offset: {terrainOffset}m");
    }

    public void SetActive(bool active)
    {
        lineRenderer.enabled = active;
        gameObject.SetActive(active); // Make sure GameObject is also active
        isActive = active;
        
        Debug.Log($"[NetworkConnection] SetActive({active}): GameObject active: {gameObject.activeInHierarchy}, LineRenderer enabled: {lineRenderer.enabled}");
    }

    public bool IsActive()
    {
        return isActive;
    }

    public Vector3[] GetConnectionPoints()
    {
        Vector3[] points = new Vector3[2];
        lineRenderer.GetPositions(points);
        return points; // Returns the elevated positions
    }
}
