// ThirdPersonCamera.cs - Attach to Main Camera
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                      // The target to follow (player character)

    [Header("Position Settings")]
    public float defaultDistance = 5.0f;          // Default distance from target
    public float minDistance = 1.5f;              // Minimum distance when obstacles are hit
    public float maxDistance = 8.0f;              // Maximum distance (for zoom out)
    public float heightOffset = 1.6f;             // Height above target
    public float targetOffset = 0.75f;            // Forward offset for the camera's focus point (over-the-shoulder effect)

    [Header("Rotation Settings")]
    public float rotationSpeed = 3.0f;            // Mouse rotation speed
    public float minVerticalAngle = -30.0f;       // Min look angle (up/down)
    public float maxVerticalAngle = 60.0f;        // Max look angle (up/down)

    [Header("Smoothing Settings")]
    public float positionSmoothTime = 0.1f;       // Position smoothing
    public float rotationSmoothTime = 0.1f;       // Rotation smoothing
    public float zoomSmoothTime = 0.2f;           // Zoom smoothing

    // Define the camera mode enum (no Header attribute on the enum itself)
    public enum CameraMode { Close, Medium, Far, Combat }

    [Header("Camera Modes")]
    public CameraMode currentMode = CameraMode.Medium;
    public float closeModeDistance = 3.0f;
    public float mediumModeDistance = 5.0f;
    public float farModeDistance = 7.0f;
    public float combatModeDistance = 4.0f;
    public KeyCode switchModeKey = KeyCode.V;     // Key to cycle camera modes

    [Header("Collision Settings")]
    public float collisionRadius = 0.3f;          // Size of collision detection sphere
    public float collisionOffset = 0.2f;          // How much to pull back from collision
    public LayerMask collisionLayers;             // Layers to check for collisions

    [Header("Smart Following")]
    public bool followTargetOrientation = true;   // Camera follows player's direction when moving
    public float orientationDelay = 0.5f;         // Delay before camera reorients behind player (seconds)
    public float orientationSpeed = 5.0f;         // How fast camera moves behind player

    // Private variables
    private float currentDistance;                // Current actual distance
    private float targetDistance;                 // Target distance based on mode and collision
    private float currentYaw = 0.0f;              // Current horizontal rotation
    private float currentPitch = 10.0f;           // Current vertical rotation
    private Vector3 currentVelocity = Vector3.zero;
    private float yawSmoothVelocity = 0.0f;
    private float pitchSmoothVelocity = 0.0f;
    private float distanceSmoothVelocity = 0.0f;
    private float lastMoveTime = 0.0f;            // Time when player last moved
    private bool isPlayerMoving = false;
    private float targetYaw = 0.0f;
    private Vector3 targetPosition;

    void Start()
    {
        // Initialize camera
        if (target == null)
        {
            Debug.LogError("No target assigned to ThirdPersonCamera!");
            return;
        }

        // Initialize distances
        currentDistance = defaultDistance;
        targetDistance = defaultDistance;

        // Set initial distance based on camera mode
        UpdateDistanceForMode();

        // Store initial rotation
        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;

        // Lock cursor for camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Switch camera mode with key press
        if (Input.GetKeyDown(switchModeKey))
        {
            SwitchCameraMode();
        }

        // Check if player is moving
        CheckPlayerMovement();

        // Handle mouse input for rotation
        HandleCameraRotation();

        // Update camera position and orientation
        UpdateCameraTransform();
    }

    void CheckPlayerMovement()
    {
        // Detect player movement (could be enhanced to check actual character movement)
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (input.magnitude > 0.1f)
        {
            isPlayerMoving = true;
            lastMoveTime = Time.time;

            if (followTargetOrientation)
            {
                // Store target's forward direction as a rotation
                targetYaw = target.eulerAngles.y;
            }
        }
        else if (Time.time - lastMoveTime > orientationDelay)
        {
            isPlayerMoving = false;
        }
    }

    void HandleCameraRotation()
    {
        // Only process mouse input if cursor is locked
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Update camera rotation
            if (isPlayerMoving && followTargetOrientation)
            {
                // Smoothly move toward the target's orientation
                currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawSmoothVelocity, rotationSmoothTime * 2);
            }
            else
            {
                // Standard manual rotation
                currentYaw += mouseX;
            }

            // Update pitch with clamping
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    void UpdateCameraTransform()
    {
        // First, update distance based on mode
        UpdateDistanceForMode();

        // Calculate focus point (slightly ahead of target for better framing)
        Vector3 focusPoint = target.position + Vector3.up * heightOffset;

        // Calculate the camera's desired position based on target and rotation
        Vector3 directionToCamera = Quaternion.Euler(currentPitch, currentYaw, 0) * -Vector3.forward;
        Vector3 desiredPosition = focusPoint + directionToCamera * targetDistance;

        // Handle collision detection
        targetPosition = HandleCameraCollision(focusPoint, desiredPosition);

        // Smoothly move the camera
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        // Calculate look target (slightly offset for over-shoulder effect)
        Vector3 lookTarget = focusPoint + target.forward * targetOffset;

        // Keep camera looking at focus point
        transform.LookAt(lookTarget);
    }

    Vector3 HandleCameraCollision(Vector3 focusPoint, Vector3 desiredPosition)
    {
        // Direction and distance from focus point to desired position
        Vector3 direction = (desiredPosition - focusPoint).normalized;
        float targetDistance = Vector3.Distance(focusPoint, desiredPosition);

        // Check for collision with a sphere cast
        RaycastHit hit;
        if (Physics.SphereCast(focusPoint, collisionRadius, direction, out hit, targetDistance, collisionLayers))
        {
            // Calculate how far to position the camera from the collision point
            float adjustedDistance = hit.distance - collisionOffset;

            // Ensure we don't go below minimum distance
            adjustedDistance = Mathf.Max(adjustedDistance, minDistance);

            // Update the current camera distance
            currentDistance = Mathf.SmoothDamp(currentDistance, adjustedDistance, ref distanceSmoothVelocity, zoomSmoothTime);

            // Return the adjusted position
            return focusPoint + direction * currentDistance;
        }

        // No collision - interpolate toward target distance
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceSmoothVelocity, zoomSmoothTime);
        return focusPoint + direction * currentDistance;
    }

    void SwitchCameraMode()
    {
        // Cycle through camera modes
        switch (currentMode)
        {
            case CameraMode.Close:
                currentMode = CameraMode.Medium;
                break;
            case CameraMode.Medium:
                currentMode = CameraMode.Far;
                break;
            case CameraMode.Far:
                currentMode = CameraMode.Combat;
                break;
            case CameraMode.Combat:
                currentMode = CameraMode.Close;
                break;
        }

        Debug.Log($"Camera mode switched to: {currentMode}");
        UpdateDistanceForMode();
    }

    void UpdateDistanceForMode()
    {
        // Set target distance based on current mode
        switch (currentMode)
        {
            case CameraMode.Close:
                targetDistance = closeModeDistance;
                break;
            case CameraMode.Medium:
                targetDistance = mediumModeDistance;
                break;
            case CameraMode.Far:
                targetDistance = farModeDistance;
                break;
            case CameraMode.Combat:
                targetDistance = combatModeDistance;
                break;
        }
    }

    // Public methods for external control

    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        UpdateDistanceForMode();
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            target = newTarget;
        }
    }

    public void ResetRotation()
    {
        // Reset camera to behind the player
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
            currentPitch = 10f; // Slight downward angle
        }
    }

    // Toggle cursor lock when pressing Escape
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    void ToggleCursorLock()
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