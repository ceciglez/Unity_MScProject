using UnityEngine;
using System.Collections;

/// <summary>
/// Dynamically adjusts NetworkConnection line colors based on biodiversity at their endpoints
/// Creates visual feedback that makes biodiverse areas visible from distance
///
/// FUNCTIONALITY:
/// - Monitors all NetworkConnection LineRenderers
/// - Queries BiodiversityScoreManager for Simpson's Index at line endpoints
/// - Interpolates line color from gray (low diversity) to vibrant cyan/green (high diversity)
/// - Updates colors in real-time as player moves through terrain
///
/// VISUAL EFFECT:
/// - Low biodiversity areas: Gray desaturated network lines
/// - High biodiversity areas: Vibrant colored network lines
/// - Creates distance-visible "heatmap" effect across terrain
/// - More performant than terrain overlays or additional volumes
///
/// INTEGRATION:
/// - Attach to same GameObject as INaturalistMapController
/// - Auto-finds BiodiversityScoreManager
/// - Updates line colors every updateInterval seconds
///
/// SOURCE:
/// - Unity LineRenderer color gradient system
/// - Color interpolation based on Simpson's Diversity Index
///
/// AI CONTRIBUTION: ~95% - System design, color interpolation, performance optimization
/// HUMAN CONTRIBUTION: ~5% - Concept, visual direction
/// </summary>
public class NetworkBiodiversityColorizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to BiodiversityScoreManager (auto-found if not set)")]
    public BiodiversityScoreManager biodiversityManager;

    [Tooltip("Player/Camera transform for position tracking (auto-found if not set)")]
    public Transform playerOrCameraTransform;

    [Header("Color Settings")]
    [Tooltip("Color for lines in low biodiversity areas")]
    public Color lowBiodiversityColor = new Color(0.3f, 0.3f, 0.3f, 0.3f); // Gray

    [Tooltip("Color for lines in high biodiversity areas")]
    public Color highBiodiversityColor = new Color(0f, 1f, 0.8f, 0.8f); // Vibrant cyan

    [Tooltip("Use gradient (interpolate between endpoints) or uniform color (average)")]
    public bool useGradient = true;

    [Header("Update Settings")]
    [Tooltip("Update interval in seconds")]
    [Range(0.1f, 5f)]
    public float updateInterval = 1f;

    [Tooltip("Minimum Simpson's Index to start showing color (below this = gray)")]
    [Range(0f, 0.3f)]
    public float minBiodiversityThreshold = 0.1f;

    [Header("Performance")]
    [Tooltip("Update only lines within this distance from camera (0 = all lines)")]
    public float updateRadius = 200f;

    [Tooltip("Update when player moves this distance (0 = update every updateInterval)")]
    [Range(0f, 100f)]
    public float playerMovementThreshold = 50f;

    [Header("Debugging")]
    public bool enableDebugLogging = true; // Enable by default to see what's happening

    private NetworkConnection[] allConnections;
    private float lastUpdateTime;
    private Camera mainCamera;
    private int totalConnectionsFound = 0;
    private Vector3 lastPlayerPosition;
    private Transform playerTransform;

    void Start()
    {
        // Find BiodiversityScoreManager
        if (biodiversityManager == null)
        {
            biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
            if (biodiversityManager == null)
            {
                Debug.LogError("[NetworkBiodiversityColorizer] No BiodiversityScoreManager found!");
                enabled = false;
                return;
            }
        }

        mainCamera = Camera.main;

        // Find player/camera transform - prioritize manually assigned reference
        if (playerOrCameraTransform != null)
        {
            playerTransform = playerOrCameraTransform;
            lastPlayerPosition = playerTransform.position;
            if (enableDebugLogging)
                Debug.Log($"[NetworkBiodiversityColorizer] ✅ Using manually assigned transform: {playerTransform.name}");
        }
        else
        {
            // Try to find Player tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                lastPlayerPosition = playerTransform.position;
                if (enableDebugLogging)
                    Debug.Log($"[NetworkBiodiversityColorizer] ✅ Player found: {player.name}");
            }
            else if (mainCamera != null)
            {
                // Fallback to main camera
                playerTransform = mainCamera.transform;
                lastPlayerPosition = playerTransform.position;
                if (enableDebugLogging)
                    Debug.LogWarning($"[NetworkBiodiversityColorizer] No 'Player' tag found! Using Main Camera '{mainCamera.name}' instead.");
            }
        }

        if (enableDebugLogging)
            Debug.Log("[NetworkBiodiversityColorizer] Initialized");

        // Initial update
        StartCoroutine(UpdateLineColorsCoroutine());
    }

    void Update()
    {
        bool shouldUpdate = false;

        // Check if player has moved significantly
        if (playerMovementThreshold > 0f && playerTransform != null)
        {
            float playerMovement = Vector3.Distance(lastPlayerPosition, playerTransform.position);

            if (playerMovement > playerMovementThreshold)
            {
                shouldUpdate = true;
                lastPlayerPosition = playerTransform.position;

                if (enableDebugLogging)
                {
                    Debug.Log($"[NetworkBiodiversityColorizer] Player moved {playerMovement:F0}m - updating line colors");
                }
            }
        }

        // Also update on interval
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            shouldUpdate = true;
        }

        if (shouldUpdate)
        {
            StartCoroutine(UpdateLineColorsCoroutine());
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Updates all network line colors based on biodiversity
    /// </summary>
    private IEnumerator UpdateLineColorsCoroutine()
    {
        // Find all NetworkConnection objects
        allConnections = FindObjectsOfType<NetworkConnection>();

        if (allConnections == null || allConnections.Length == 0)
        {
            if (enableDebugLogging)
                Debug.LogWarning("[NetworkBiodiversityColorizer] No NetworkConnection objects found!");
            yield break;
        }

        totalConnectionsFound = allConnections.Length;
        if (enableDebugLogging)
            Debug.Log($"[NetworkBiodiversityColorizer] Found {totalConnectionsFound} NetworkConnection objects");

        Vector3 cameraPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        int updatedCount = 0;
        int skippedCount = 0;

        foreach (var connection in allConnections)
        {
            if (connection == null || !connection.IsActive())
                continue;

            // Check distance from camera for performance
            if (updateRadius > 0f)
            {
                Vector3[] points = connection.GetConnectionPoints();
                if (points.Length >= 2)
                {
                    Vector3 midpoint = (points[0] + points[points.Length - 1]) * 0.5f;
                    if (Vector3.Distance(cameraPos, midpoint) > updateRadius)
                    {
                        skippedCount++;
                        continue;
                    }
                }
            }

            UpdateConnectionColor(connection);
            updatedCount++;

            // Yield every 10 connections to avoid frame hitches
            if (updatedCount % 10 == 0)
                yield return null;
        }

        if (enableDebugLogging)
            Debug.Log($"[NetworkBiodiversityColorizer] Updated {updatedCount} lines, skipped {skippedCount}");
    }

    /// <summary>
    /// Updates color for a single NetworkConnection based on biodiversity at endpoints
    /// </summary>
    private void UpdateConnectionColor(NetworkConnection connection)
    {
        Vector3[] points = connection.GetConnectionPoints();
        if (points.Length < 2)
            return;

        // Get biodiversity at start and end points
        float startBiodiversity = GetBiodiversityAtPosition(points[0]);
        float endBiodiversity = GetBiodiversityAtPosition(points[points.Length - 1]);

        // Apply threshold
        startBiodiversity = Mathf.Max(0f, startBiodiversity - minBiodiversityThreshold) / (1f - minBiodiversityThreshold);
        endBiodiversity = Mathf.Max(0f, endBiodiversity - minBiodiversityThreshold) / (1f - minBiodiversityThreshold);

        // Calculate colors
        Color startColor = Color.Lerp(lowBiodiversityColor, highBiodiversityColor, startBiodiversity);
        Color endColor = Color.Lerp(lowBiodiversityColor, highBiodiversityColor, endBiodiversity);

        if (enableDebugLogging && Random.value < 0.05f) // Log 5% of lines to avoid spam
        {
            Debug.Log($"[NetworkBiodiversityColorizer] Line biodiversity: start={startBiodiversity:F3}, end={endBiodiversity:F3}, " +
                     $"colors: {startColor} → {endColor}");
        }

        // Apply to LineRenderer
        LineRenderer lineRenderer = connection.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            if (useGradient)
            {
                // Gradient from start to end
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(startColor, 0f),
                        new GradientColorKey(endColor, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(startColor.a, 0f),
                        new GradientAlphaKey(endColor.a, 1f)
                    }
                );
                lineRenderer.colorGradient = gradient;
            }
            else
            {
                // Uniform color (average)
                Color avgColor = Color.Lerp(startColor, endColor, 0.5f);
                lineRenderer.startColor = avgColor;
                lineRenderer.endColor = avgColor;
            }
        }
    }

    /// <summary>
    /// Gets Simpson's Biodiversity Index at a world position
    /// </summary>
    private float GetBiodiversityAtPosition(Vector3 worldPos)
    {
        if (biodiversityManager == null)
            return 0f;

        // Query biodiversity manager for diversity at this position
        var hotspots = biodiversityManager.GetBiodiversityHotspots();
        if (hotspots == null || hotspots.Count == 0)
            return 0f;

        // Find closest hotspot within cell size
        float closestDistance = float.MaxValue;
        float biodiversity = 0f;

        foreach (var hotspot in hotspots)
        {
            float distance = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(hotspot.position.x, hotspot.position.z)
            );

            if (distance < closestDistance && distance < biodiversityManager.cellSize)
            {
                closestDistance = distance;
                biodiversity = hotspot.simpsonsIndex;
            }
        }

        return biodiversity;
    }

    /// <summary>
    /// Force immediate update of all line colors
    /// </summary>
    public void ForceUpdate()
    {
        StopAllCoroutines();
        StartCoroutine(UpdateLineColorsCoroutine());
    }
}
