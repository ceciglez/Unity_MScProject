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
            
            // Get terrain height using the same method as other spawners
            Vector3 terrainPos = GetTerrainPosition(interpolatedPos);
            curvePoints[i] = terrainPos + Vector3.up * terrainOffset;
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
    
    /// <summary>
    /// Get terrain position using multiple raycast strategies (same as OptimizedGrassPatchSpawner)
    /// </summary>
    private Vector3 GetTerrainPosition(Vector3 worldPos)
    {
        float raycastDistance = 200f;
        
        // Strategy 1: Raycast from above - try with specific terrain layers first
        RaycastHit hit;
        Vector3 rayStart = new Vector3(worldPos.x, worldPos.y + 100f, worldPos.z);
        
        // Try terrain-specific layers first (if any defined)
        int terrainOnlyMask = LayerMask.GetMask("Default", "Terrain"); // Common terrain layers
        if (terrainOnlyMask != 0)
        {
            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, terrainOnlyMask))
            {
                return hit.point;
            }
        }
        
        // Strategy 2: Raycast against everything EXCEPT known non-terrain layers
        int excludeMask = LayerMask.GetMask("UI", "Water", "TransparentFX", "Network");
        int allExceptExcluded = ~excludeMask;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, allExceptExcluded))
        {
            // Skip objects that are clearly not terrain (observation prefabs, etc.)
            if (hit.collider.gameObject.name.ToLower().Contains("observation") || 
                hit.collider.gameObject.name.ToLower().Contains("prefab"))
            {
                // Continue raycast from this hit point to find terrain underneath
                Vector3 continueStart = hit.point + Vector3.down * 0.1f;
                if (Physics.Raycast(continueStart, Vector3.down, out RaycastHit terrainHit, raycastDistance, allExceptExcluded))
                {
                    return terrainHit.point;
                }
            }
            else
            {
                return hit.point;
            }
        }
        
        // Strategy 3: Multiple raycasts around the position
        for (int i = 0; i < 4; i++)
        {
            float angle = (90f * i) * Mathf.Deg2Rad;
            float searchRadius = 2f;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * searchRadius,
                0f,
                Mathf.Sin(angle) * searchRadius
            );
            
            Vector3 searchStart = rayStart + offset;
            if (Physics.Raycast(searchStart, Vector3.down, out hit, raycastDistance, allExceptExcluded))
            {
                return hit.point;
            }
        }
        
        // Fallback: use ground level
        return new Vector3(worldPos.x, 0f, worldPos.z);
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
