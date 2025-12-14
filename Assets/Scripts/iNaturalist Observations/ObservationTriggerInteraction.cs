using UnityEngine;
using KinematicCharacterController;

[RequireComponent(typeof(Collider))]
public class ObservationTriggerInteraction : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private float triggerRadius = 3f;
    
    [Header("Distance Settings")]
    [SerializeField] private float hideDistance = 10f;
    
    private ObservationDisplay observationDisplay;
    private SphereCollider triggerCollider;
    private Transform playerTransform;
    private bool playerNearby = false;
    
    void Awake()
    {
        observationDisplay = GetComponent<ObservationDisplay>();
        if (observationDisplay == null)
        {
            Debug.LogError($"ObservationTriggerInteraction on {gameObject.name}: No ObservationDisplay component found!");
            enabled = false;
            return;
        }
        
        SetupTrigger();
        
        // Ensure canvas starts hidden
        if (observationDisplay != null)
        {
            observationDisplay.HideCanvas();
            Debug.Log($"ObservationTriggerInteraction: Ensured canvas starts hidden on {gameObject.name}");
        }
    }
    
    private void SetupTrigger()
    {
        // Find or create sphere collider
        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }
        
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ObservationTriggerInteraction: TriggerEnter from {other.gameObject.name}");
        if (IsPlayer(other))
        {
            playerNearby = true;
            playerTransform = other.transform;
            Debug.Log($"ObservationTriggerInteraction: Player detected! Showing interaction prompt on {gameObject.name}");
            ShowInteractionPrompt();
        }
        else
        {
            Debug.Log($"ObservationTriggerInteraction: Not recognized as player");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerNearby = false;
            playerTransform = null;
            Debug.Log($"ObservationTriggerInteraction: Player exited trigger. Hiding UI on {gameObject.name}");
            HideInteractionPrompt();
        }
    }
    
    private void Update()
    {
        // Check distance even while player is in trigger zone
        if (playerNearby && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance > hideDistance)
            {
                observationDisplay.OnPlayerExitRange();
            }
        }
    }
    
    private bool IsPlayer(Collider other)
    {
        // Check for Player tag
        if (other.CompareTag("Player"))
            return true;
        
        // Check for KCC components
        if (other.GetComponent<KinematicCharacterMotor>() != null)
            return true;
        
        if (other.GetComponent<KinematicCharacterController.Examples.ExampleCharacterController>() != null)
            return true;
        
        // Check for Unity CharacterController (backup)
        if (other.GetComponent<CharacterController>() != null)
            return true;
        
        return false;
    }
    
    private void ShowInteractionPrompt()
    {
        if (observationDisplay != null)
        {
            observationDisplay.OnPlayerEnterRange();
        }
    }
    
    private void HideInteractionPrompt()
    {
        if (observationDisplay != null)
        {
            observationDisplay.OnPlayerExitRange();
        }
    }
    
    // Editor visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = playerNearby ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hideDistance);
    }
}
