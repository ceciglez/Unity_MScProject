using UnityEngine;

/// <summary>
/// Manages interactions between observations and UI tooltip
/// Handles mouse hover detection and tooltip display
/// </summary>
public class ObservationInteractionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObservationTooltip tooltip;
    [SerializeField] private Camera mainCamera;
    
    [Header("Interaction Settings")]
    [SerializeField] private float raycastDistance = 1000f;
    [SerializeField] private LayerMask observationLayer;
    [SerializeField] private bool useMousePosition = true;
    
    private ObservationDisplay currentHoveredObservation;
    private bool isOverUI = false;
    
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera found! Interaction may not work.");
            }
        }
        
        if (tooltip == null)
        {
            tooltip = FindObjectOfType<ObservationTooltip>();
            if (tooltip == null)
            {
                Debug.LogWarning("No ObservationTooltip found in scene!");
            }
        }
    }
    
    void Update()
    {
        if (mainCamera == null || !useMousePosition) return;
        
        // Check if mouse is over UI
        isOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        
        if (!isOverUI)
        {
            CheckForObservationHover();
        }
        else
        {
            // Hide tooltip if over UI
            if (currentHoveredObservation != null)
            {
                OnObservationExit(currentHoveredObservation);
            }
        }
    }
    
    private void CheckForObservationHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        bool hitObservation = false;
        
        if (observationLayer != 0)
        {
            // Use layer mask if specified
            hitObservation = Physics.Raycast(ray, out hit, raycastDistance, observationLayer);
        }
        else
        {
            // Raycast all layers
            hitObservation = Physics.Raycast(ray, out hit, raycastDistance);
        }
        
        if (hitObservation)
        {
            ObservationDisplay obs = hit.collider.GetComponent<ObservationDisplay>();
            
            if (obs != null)
            {
                // New observation hovered
                if (obs != currentHoveredObservation)
                {
                    // Exit previous
                    if (currentHoveredObservation != null)
                    {
                        OnObservationExit(currentHoveredObservation);
                    }
                    
                    // Enter new
                    OnObservationEnter(obs);
                }
            }
            else
            {
                // Hit something but not an observation
                if (currentHoveredObservation != null)
                {
                    OnObservationExit(currentHoveredObservation);
                }
            }
        }
        else
        {
            // No hit
            if (currentHoveredObservation != null)
            {
                OnObservationExit(currentHoveredObservation);
            }
        }
    }
    
    private void OnObservationEnter(ObservationDisplay obs)
    {
        currentHoveredObservation = obs;
        
        if (tooltip != null)
        {
            tooltip.Show(obs.GetData());
        }
        
        // Change cursor
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    
    private void OnObservationExit(ObservationDisplay obs)
    {
        if (currentHoveredObservation == obs)
        {
            currentHoveredObservation = null;
            
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }
    }
    
    /// <summary>
    /// Manually set which observation is hovered (for non-mouse input)
    /// </summary>
    public void SetHoveredObservation(ObservationDisplay obs)
    {
        if (obs != currentHoveredObservation)
        {
            if (currentHoveredObservation != null)
            {
                OnObservationExit(currentHoveredObservation);
            }
            
            if (obs != null)
            {
                OnObservationEnter(obs);
            }
        }
    }
    
    /// <summary>
    /// Clear current hover state
    /// </summary>
    public void ClearHover()
    {
        if (currentHoveredObservation != null)
        {
            OnObservationExit(currentHoveredObservation);
        }
    }
    
    /// <summary>
    /// Get currently hovered observation
    /// </summary>
    public ObservationDisplay GetHoveredObservation()
    {
        return currentHoveredObservation;
    }
}
