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
        
        // Create transparent material using Unlit shader (better for transparency)
        Material lineMaterial = new Material(Shader.Find("Unlit/Transparent"));
        lineMaterial.color = new Color(0f, 1f, 1f, lineOpacity); // Cyan with custom opacity
        
        lineRenderer.material = lineMaterial;
        lineRenderer.useWorldSpace = true;
        lineRenderer.allowOcclusionWhenDynamic = false;
        lineRenderer.sortingOrder = 1000;
        
        Debug.Log($"[NetworkConnection] Created terrain-following LineRenderer with width: {lineWidth}, opacity: {lineOpacity}");
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
            
            // Raycast down to find terrain height
            if (Physics.Raycast(interpolatedPos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
            {
                curvePoints[i] = hit.point + Vector3.up * terrainOffset;
            }
            else
            {
                // Fallback if no terrain hit
                curvePoints[i] = interpolatedPos + Vector3.up * terrainOffset;
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
