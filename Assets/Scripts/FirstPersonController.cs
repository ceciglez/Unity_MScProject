using UnityEngine;

/// <summary>
/// First Person Controller with terrain collision support for Mapbox scenes
/// Handles movement, jumping, and stays grounded on terrain
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private bool canJump = true;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask = -1; // Everything by default
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float slopeLimit = 45f;
    
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float lookSmoothness = 10f;
    [SerializeField] private float maxLookAngle = 90f;
    
    [Header("Crouch Settings")]
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    
    [Header("Key Bindings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    
    // Components
    private CharacterController characterController;
    
    // Movement state
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;
    
    // Look state
    private float currentCameraRotationX = 0f;
    private Vector2 currentLookInput;
    private Vector2 smoothLookVelocity;
    
    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        // Setup camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("FirstPersonController: No camera found! Please assign a camera.");
            }
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Set initial height
        characterController.height = standingHeight;
    }
    
    void Update()
    {
        HandleInput();
        HandleLook();
        HandleMovement();
        HandleCrouch();
    }
    
    private void HandleInput()
    {
        // Movement input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveZ).normalized;
        
        // Look input
        lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        
        // Sprint
        isSprinting = Input.GetKey(sprintKey) && !isCrouching && isGrounded;
        
        // Crouch toggle
        if (canCrouch && Input.GetKeyDown(crouchKey))
        {
            if (!isCrouching)
            {
                StartCrouch();
            }
            else if (CanStandUp())
            {
                StopCrouch();
            }
        }
        
        // Jump
        if (canJump && Input.GetKeyDown(jumpKey) && isGrounded && !isCrouching)
        {
            Jump();
        }
        
        // Cursor lock/unlock (ESC to unlock)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // Smooth look input
        currentLookInput = Vector2.SmoothDamp(
            currentLookInput,
            lookInput,
            ref smoothLookVelocity,
            1f / lookSmoothness
        );
        
        // Rotate player body (Y axis)
        transform.Rotate(Vector3.up * currentLookInput.x * lookSensitivity);
        
        // Rotate camera (X axis)
        currentCameraRotationX -= currentLookInput.y * lookSensitivity;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -maxLookAngle, maxLookAngle);
        
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(currentCameraRotationX, 0f, 0f);
        }
    }
    
    private void HandleMovement()
    {
        // Check if grounded
        isGrounded = CheckGrounded();
        
        // Reset velocity if grounded and falling
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        
        // Calculate target speed
        float targetSpeed = walkSpeed;
        if (isSprinting) targetSpeed = sprintSpeed;
        else if (isCrouching) targetSpeed = crouchSpeed;
        
        // Calculate move direction relative to player
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = moveDirection * targetSpeed;
        
        // Smoothly interpolate to target velocity
        float interpolation = (moveInput.magnitude > 0) ? acceleration : deceleration;
        currentMoveVelocity = Vector3.Lerp(
            currentMoveVelocity,
            targetVelocity,
            interpolation * Time.deltaTime
        );
        
        // Apply gravity
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        
        // Combine movement and gravity
        Vector3 finalMovement = currentMoveVelocity + velocity;
        
        // Move the character
        characterController.Move(finalMovement * Time.deltaTime);
    }
    
    private bool CheckGrounded()
    {
        // Raycast downward from center of character
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        float rayDistance = groundCheckDistance + 0.1f;
        
        bool hit = Physics.Raycast(rayOrigin, Vector3.down, rayDistance, groundMask);
        
        // Debug visualization
        Debug.DrawRay(rayOrigin, Vector3.down * rayDistance, hit ? Color.green : Color.red);
        
        return hit;
    }
    
    private void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
    
    private void StartCrouch()
    {
        isCrouching = true;
        isSprinting = false;
    }
    
    private void StopCrouch()
    {
        isCrouching = false;
    }
    
    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        characterController.height = Mathf.Lerp(
            characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );
        
        // Adjust camera position based on height
        if (playerCamera != null)
        {
            Vector3 cameraLocalPos = playerCamera.transform.localPosition;
            cameraLocalPos.y = characterController.height * 0.9f; // Camera near top of controller
            playerCamera.transform.localPosition = cameraLocalPos;
        }
    }
    
    private bool CanStandUp()
    {
        // Check if there's room to stand up
        Vector3 rayOrigin = transform.position + Vector3.up * crouchHeight;
        float checkHeight = standingHeight - crouchHeight + 0.1f;
        
        return !Physics.Raycast(rayOrigin, Vector3.up, checkHeight, groundMask);
    }
    
    // Public methods for external control
    public void SetPosition(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
    }
    
    public void SetLookSensitivity(float sensitivity)
    {
        lookSensitivity = sensitivity;
    }
    
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public Vector3 Velocity => currentMoveVelocity;
}