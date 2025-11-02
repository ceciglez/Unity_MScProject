using System.Collections;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

/// <summary>
/// WASD controller for navigating the map with keyboard
/// Also handles zoom with Q/E or scroll wheel
/// </summary>
public class MapUserController : MonoBehaviour
{
    [Header("Map Reference")]
    [SerializeField] private AbstractMap map;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 0.001f; // Lat/Lng units per second
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private bool useSprintKey = true;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 18f;
    [SerializeField] private bool useScrollWheel = true;
    [SerializeField] private float scrollSensitivity = 1f;
    
    [Header("Key Bindings")]
    [SerializeField] private KeyCode moveForward = KeyCode.W;
    [SerializeField] private KeyCode moveBack = KeyCode.S;
    [SerializeField] private KeyCode moveLeft = KeyCode.A;
    [SerializeField] private KeyCode moveRight = KeyCode.D;
    [SerializeField] private KeyCode zoomIn = KeyCode.E;
    [SerializeField] private KeyCode zoomOut = KeyCode.Q;
    
    [Header("Smooth Movement")]
    [SerializeField] private bool smoothMovement = true;
    [SerializeField] private float smoothSpeed = 5f;
    
    private Vector2d targetPosition;
    private float targetZoom;
    private bool isInitialized = false;
    
    void Start()
    {
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
            if (map == null)
            {
                Debug.LogError("No AbstractMap found! Please assign the map reference.");
                enabled = false;
                return;
            }
        }
        
        // Wait for map to initialize before setting targets
        StartCoroutine(InitializeAfterMap());
    }
    
    private IEnumerator InitializeAfterMap()
    {
        // Wait for map to be ready
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f);
        
        // Initialize target position and zoom to match current map
        // This preserves whatever position was set in the Inspector
        targetPosition = map.CenterLatitudeLongitude;
        targetZoom = map.Zoom;
        
        isInitialized = true;
        
        Debug.Log($"MapUserController initialized at {targetPosition.x:F4}, {targetPosition.y:F4}, zoom {targetZoom}");
    }
    
    void Update()
    {
        if (!isInitialized || map == null) return;
        
        // Store previous targets to detect if user is actually moving
        Vector2d previousTarget = targetPosition;
        float previousZoomTarget = targetZoom;
        
        HandleMovement();
        HandleZoom();
        
        // Only apply changes if user input changed the targets
        bool userInputDetected = 
            Vector2d.Distance(previousTarget, targetPosition) > 0.000001 ||
            Mathf.Abs(previousZoomTarget - targetZoom) > 0.001f;
            
        if (userInputDetected)
        {
            ApplyChanges();
        }
    }
    
    private void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;
        
        // Get WASD input
        if (Input.GetKey(moveForward)) moveInput.y += 1;
        if (Input.GetKey(moveBack)) moveInput.y -= 1;
        if (Input.GetKey(moveLeft)) moveInput.x -= 1;
        if (Input.GetKey(moveRight)) moveInput.x += 1;
        
        // Normalize diagonal movement
        if (moveInput.magnitude > 1)
        {
            moveInput.Normalize();
        }
        
        // Apply sprint
        float currentSpeed = moveSpeed;
        if (useSprintKey && Input.GetKey(sprintKey))
        {
            currentSpeed *= sprintMultiplier;
        }
        
        // Adjust speed based on zoom level (move faster when zoomed out)
        float zoomFactor = Mathf.Lerp(10f, 0.1f, (map.Zoom - minZoom) / (maxZoom - minZoom));
        currentSpeed *= zoomFactor;
        
        // Calculate new position
        if (moveInput.magnitude > 0)
        {
            // Latitude (North/South)
            targetPosition.x += moveInput.y * currentSpeed * Time.deltaTime;
            
            // Longitude (East/West)
            targetPosition.y += moveInput.x * currentSpeed * Time.deltaTime;
            
            // Clamp latitude to valid range
            targetPosition.x = Mathf.Clamp((float)targetPosition.x, -85f, 85f);
            
            // Wrap longitude
            while (targetPosition.y > 180) targetPosition.y -= 360;
            while (targetPosition.y < -180) targetPosition.y += 360;
        }
    }
    
    private void HandleZoom()
    {
        float zoomInput = 0f;
        
        // Keyboard zoom
        if (Input.GetKey(zoomIn)) zoomInput += 1;
        if (Input.GetKey(zoomOut)) zoomInput -= 1;
        
        // Scroll wheel zoom
        if (useScrollWheel)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            zoomInput += scroll * scrollSensitivity * 10f;
        }
        
        if (zoomInput != 0)
        {
            targetZoom += zoomInput * zoomSpeed * Time.deltaTime;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }
    
    private void ApplyChanges()
    {
        if (smoothMovement)
        {
            // Smooth interpolation
            Vector2d currentPos = map.CenterLatitudeLongitude;
            
            // Only update if there's meaningful difference (deadzone)
            double posDiff = Vector2d.Distance(currentPos, targetPosition);
            if (posDiff > 0.0001) // Deadzone threshold
            {
                Vector2d newPos = Vector2d.Lerp(currentPos, targetPosition, smoothSpeed * Time.deltaTime);
                map.UpdateMap(newPos, map.Zoom);
            }
            
            // Smooth zoom
            float zoomDiff = Mathf.Abs(map.Zoom - targetZoom);
            if (zoomDiff > 0.01f) // Deadzone threshold
            {
                float newZoom = Mathf.Lerp(map.Zoom, targetZoom, smoothSpeed * Time.deltaTime);
                map.UpdateMap(map.CenterLatitudeLongitude, newZoom);
            }
        }
        else
        {
            // Immediate update - only if there's actual change
            double posDiff = Vector2d.Distance(map.CenterLatitudeLongitude, targetPosition);
            float zoomDiff = Mathf.Abs(map.Zoom - targetZoom);
            
            if (posDiff > 0.00001 || zoomDiff > 0.01f)
            {
                map.UpdateMap(targetPosition, targetZoom);
            }
        }
    }
    
    /// <summary>
    /// Teleport to a specific location instantly
    /// </summary>
    public void TeleportTo(double latitude, double longitude)
    {
        targetPosition = new Vector2d(latitude, longitude);
        map.UpdateMap(targetPosition, map.Zoom);
    }
    
    /// <summary>
    /// Teleport to a specific location with zoom
    /// </summary>
    public void TeleportTo(double latitude, double longitude, float zoom)
    {
        targetPosition = new Vector2d(latitude, longitude);
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        map.UpdateMap(targetPosition, targetZoom);
    }
    
    /// <summary>
    /// Smoothly move to a location
    /// </summary>
    public void MoveTo(double latitude, double longitude)
    {
        targetPosition = new Vector2d(latitude, longitude);
    }
    
    /// <summary>
    /// Set zoom level
    /// </summary>
    public void SetZoom(float zoom)
    {
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }
    
    /// <summary>
    /// Get current map position
    /// </summary>
    public Vector2d GetPosition()
    {
        return map.CenterLatitudeLongitude;
    }
    
    /// <summary>
    /// Get current zoom level
    /// </summary>
    public float GetZoom()
    {
        return map.Zoom;
    }
}