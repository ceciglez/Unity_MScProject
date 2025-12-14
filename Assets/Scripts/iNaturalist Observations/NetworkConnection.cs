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
    public float terrainOffset = 0.1f; // Keep lines close to ground
    
    [Tooltip("Number of points for terrain-following curves")]
    public int curveResolution = 10;
    
    [Header("Performance Settings")]
    [Tooltip("Enable terrain following (disable for better performance)")]
    public bool enableTerrainFollowing = true;
    
    [Tooltip("Use simplified straight lines for better performance")]
    public bool useSimpleLines = false;
    
    private LineRenderer lineRenderer;
    private bool isActive = false;
    
    // Performance optimization: cache terrain heights
    private static Dictionary<Vector2, float> terrainHeightCache = new Dictionary<Vector2, float>();
    private static float cacheGridSize = 5f; // Cache every 5 units
    
    // Start is called before the first frame update
    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = curveResolution;
        lineRenderer.enabled = false;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        
        // Use Unlit shader to bypass post-processing (Global Volume desaturation won't affect it)
        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Unlit/Transparent");
            Debug.LogWarning("[NetworkConnection] URP/Unlit not found, using Unlit/Transparent");
        }
        if (lineShader == null)
        {
            lineShader = Shader.Find("Sprites/Default");
            Debug.LogWarning("[NetworkConnection] Unlit/Transparent not found, falling back to Sprites/Default");
        }

        Material lineMaterial = new Material(lineShader);
        lineMaterial.color = new Color(0f, 1f, 1f, lineOpacity); // Cyan with custom opacity

        // Standard transparency setup
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_ZWrite", 0); // Don't write to depth buffer
        lineMaterial.renderQueue = 3000; // Transparent queue

        lineRenderer.material = lineMaterial;
        lineRenderer.useWorldSpace = true;
        lineRenderer.allowOcclusionWhenDynamic = false;

        // Disable shadows to prevent lines from casting/receiving shadows
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Set the GameObject layer to Network
        gameObject.layer = LayerMask.NameToLayer("Network");

        Debug.Log($"[NetworkConnection] Created LineRenderer: renderQueue 3000, occlusion disabled for visibility");
    }

    
    public void SetConnection(Vector3 start, Vector3 end, Color color)
    {
        // Update line width and opacity from inspector
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        
        // Choose between simple or terrain-following lines based on performance settings
        if (useSimpleLines || !enableTerrainFollowing)
        {
            SetSimpleConnection(start, end, color);
        }
        else
        {
            SetTerrainFollowingConnection(start, end, color);
        }
        
        lineRenderer.enabled = true;
        isActive = true;
        
        float distance = Vector3.Distance(start, end);
        Debug.Log($"[NetworkConnection] Line created: {start} to {end}, distance: {distance:F1}m, mode: {(useSimpleLines ? "Simple" : "Terrain-following")}");
    }
    
    private void SetSimpleConnection(Vector3 start, Vector3 end, Color color)
    {
        // Simple 2-point line for better performance
        lineRenderer.positionCount = 2;
        
        // Add slight height offset to keep above ground
        Vector3 startPos = start + Vector3.up * terrainOffset;
        Vector3 endPos = end + Vector3.up * terrainOffset;
        
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
        
        // Set color
        if (lineRenderer.material != null)
        {
            Color transparentColor = new Color(color.r, color.g, color.b, lineOpacity);
            lineRenderer.material.color = transparentColor;
        }
    }
    
    private void SetTerrainFollowingConnection(Vector3 start, Vector3 end, Color color)
    {
        // Create terrain-following curve with caching for performance
        Vector3[] curvePoints = new Vector3[curveResolution];
        
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 interpolatedPos = Vector3.Lerp(start, end, t);
            
            // Get terrain height using cached or optimized method
            Vector3 terrainPos = GetOptimizedTerrainPosition(interpolatedPos);
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
    }
    
    private Vector3 GetOptimizedTerrainPosition(Vector3 worldPos)
    {
        // Use caching to reduce raycast calls
        Vector2 gridPos = new Vector2(
            Mathf.Round(worldPos.x / cacheGridSize) * cacheGridSize,
            Mathf.Round(worldPos.z / cacheGridSize) * cacheGridSize
        );
        
        // Check cache first
        if (terrainHeightCache.TryGetValue(gridPos, out float cachedHeight))
        {
            return new Vector3(worldPos.x, cachedHeight, worldPos.z);
        }
        
        // If not cached, use the proven terrain detection method
        Vector3 terrainPos = GetTerrainPosition(worldPos);
        float terrainHeight = terrainPos.y;
        
        // Cache the result
        terrainHeightCache[gridPos] = terrainHeight;
        
        // Limit cache size to prevent memory issues
        if (terrainHeightCache.Count > 1000)
        {
            terrainHeightCache.Clear();
        }
        
        return terrainPos;
    }
    
    private float GetTerrainHeightFast(Vector3 worldPos)
    {
        RaycastHit hit;
        Vector3 rayStart = new Vector3(worldPos.x, worldPos.y + 50f, worldPos.z);
        
        // Single raycast with simplified layer filtering
        int excludeMask = LayerMask.GetMask("UI", "Network", "TransparentFX");
        int layerMask = ~excludeMask;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 100f, layerMask))
        {
            // Skip biodiversity volume colliders - check by name
            if (hit.collider.gameObject.name.Contains("BiodiversityVolume"))
            {
                // Cast ray from just below this hit to find actual terrain
                Vector3 continueFrom = hit.point + Vector3.down * 0.1f;
                if (Physics.Raycast(continueFrom, Vector3.down, out RaycastHit terrainHit, 100f, layerMask))
                {
                    return terrainHit.point.y;
                }
            }

            return hit.point.y;
        }
        
        // Fallback to ground level
        return 0f;
    }
    
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
            // Skip objects that are clearly not terrain (observation prefabs, volumes, etc.)
            if (hit.collider.gameObject.name.ToLower().Contains("observation") ||
                hit.collider.gameObject.name.ToLower().Contains("prefab") ||
                hit.collider.gameObject.name.Contains("BiodiversityVolume"))
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
