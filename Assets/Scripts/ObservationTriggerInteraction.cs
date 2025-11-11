using UnityEngine;
using KinematicCharacterController;

/// <summary>
/// Handles collision/trigger-based interaction with observations
/// Shows UI when player walks into/near an observation object
/// Attach this to your observation prefab
/// </summary>
[RequireComponent(typeof(Collider))]
public class ObservationTriggerInteraction : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private float triggerRadius = 3f; // How close player needs to be
    [SerializeField] private bool useSphereCollider = true;
    
    [Header("UI Settings")]
    [SerializeField] private bool useScreenUI = true; // Use centered screen-space UI
    [SerializeField] private bool showCanvas = false; // Show the 3D canvas on observation (legacy)
    [SerializeField] private bool showTooltip = false; // Also show 2D tooltip UI
    [SerializeField] private bool autoFindTooltip = true;
    
    [Header("Distance Settings")]
    [SerializeField] private float hideDistance = 15f; // Hide UI when player is further than this
    
    private ObservationDisplay observationDisplay;
    private ObservationTooltip tooltip;
    private Collider triggerCollider;
    private bool playerInside = false;
    private Transform playerTransform;
    
    void Awake()
    {
        observationDisplay = GetComponent<ObservationDisplay>();
        
        if (observationDisplay == null)
        {
            Debug.LogWarning("ObservationTriggerInteraction: No ObservationDisplay found on this GameObject!");
        }
        
        // Setup trigger collider
        SetupTriggerCollider();
    }
    
    void Start()
    {
        // Find tooltip
        if (autoFindTooltip)
        {
            tooltip = FindObjectOfType<ObservationTooltip>();
            if (tooltip == null)
            {
                Debug.LogWarning("ObservationTriggerInteraction: No ObservationTooltip found in scene!");
            }
        }
    }
    
    private void SetupTriggerCollider()
    {
        // Check if already has a collider
        triggerCollider = GetComponent<Collider>();
        
        if (triggerCollider == null)
        {
            // Create a new one
            if (useSphereCollider)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = triggerRadius;
                sphere.isTrigger = true;
                triggerCollider = sphere;
            }
            else
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.size = Vector3.one * triggerRadius * 2f;
                box.isTrigger = true;
                triggerCollider = box;
            }
            
            Debug.Log($"Created trigger collider on {gameObject.name}");
        }
        else
        {
            // Make sure existing collider is set as trigger
            triggerCollider.isTrigger = true;
            
            // Adjust size if it's a sphere collider
            if (useSphereCollider && triggerCollider is SphereCollider sphere)
            {
                sphere.radius = triggerRadius;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (IsPlayer(other))
        {
            playerInside = true;
            playerTransform = other.transform;
            ShowObservationUI();
            Debug.Log($"Player entered observation trigger: {observationDisplay?.GetData().taxon?.preferred_common_name ?? "Unknown"}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if it's the player
        if (IsPlayer(other))
        {
            playerInside = false;
            playerTransform = null;
            HideObservationUI();
            Debug.Log($"Player exited observation trigger");
        }
    }
    
    private void Update()
    {
        // If player is nearby, check distance to hide UI if too far
        if (playerInside && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance > hideDistance)
            {
                // Player moved too far, hide UI
                HideObservationUI();
                Debug.Log($"Player too far ({distance:F1}m), hiding UI");
            }
        }
    }
    
    private bool IsPlayer(Collider other)
    {
        // Check for player by tag
        if (other.CompareTag("Player"))
            return true;
        
        // Check for Unity's CharacterController (old FPC)
        if (other.GetComponent<CharacterController>() != null)
            return true;
        
        // Check for FirstPersonController script (old FPC)
        if (other.GetComponent<FirstPersonController>() != null)
            return true;
        
        // Check for KinematicCharacterMotor (new KCC)
        if (other.GetComponent<KinematicCharacterMotor>() != null)
            return true;
        
        // Check for ExampleCharacterController (KCC)
        if (other.GetComponent<KinematicCharacterController.Examples.ExampleCharacterController>() != null)
            return true;
        
        return false;
    }
    
    private void ShowObservationUI()
    {
        // Show screen-space UI (recommended)
        if (useScreenUI && observationDisplay != null)
        {
            ObservationScreenUI screenUI = ObservationScreenUI.Instance;
            if (screenUI != null)
            {
                screenUI.ShowObservation(observationDisplay.GetData());
            }
            else
            {
                Debug.LogWarning("ObservationScreenUI instance not found in scene!");
            }
        }
        
        // Show the 3D canvas UI on the observation object itself (legacy)
        if (showCanvas && observationDisplay != null)
        {
            observationDisplay.ShowCanvas();
        }
        
        // Optionally also show 2D tooltip UI
        if (showTooltip && tooltip != null && observationDisplay != null)
        {
            tooltip.Show(observationDisplay.GetData());
        }
    }
    
    private void HideObservationUI()
    {
        // Hide screen-space UI
        if (useScreenUI)
        {
            ObservationScreenUI screenUI = ObservationScreenUI.Instance;
            if (screenUI != null)
            {
                screenUI.HideObservation();
            }
        }
        
        // Hide the 3D canvas UI
        if (showCanvas && observationDisplay != null)
        {
            observationDisplay.HideCanvas();
        }
        
        // Hide 2D tooltip
        if (showTooltip && tooltip != null)
        {
            tooltip.Hide();
        }
    }
    
    /// <summary>
    /// Set the tooltip reference manually
    /// </summary>
    public void SetTooltip(ObservationTooltip tooltipReference)
    {
        tooltip = tooltipReference;
    }
    
    /// <summary>
    /// Adjust trigger radius at runtime
    /// </summary>
    public void SetTriggerRadius(float radius)
    {
        triggerRadius = radius;
        
        if (triggerCollider is SphereCollider sphere)
        {
            sphere.radius = triggerRadius;
        }
        else if (triggerCollider is BoxCollider box)
        {
            box.size = Vector3.one * triggerRadius * 2f;
        }
    }
    
    public bool IsPlayerInside => playerInside;
    
    void OnDrawGizmosSelected()
    {
        // Visualize trigger radius in editor
        Gizmos.color = playerInside ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
