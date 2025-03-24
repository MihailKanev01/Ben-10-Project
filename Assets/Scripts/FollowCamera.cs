// FollowCamera.cs - Attach to Main Camera
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                // The target to follow (CameraTarget)
    public float followDistance = 5.0f;     // Distance from target
    public float heightOffset = 1.5f;       // Height above target
    public float followSpeed = 10.0f;       // How quickly to follow

    [Header("Rotation Settings")]
    public float rotationSpeed = 3.0f;      // How quickly to rotate around the target
    public float minVerticalAngle = -30.0f; // Min look angle (up/down)
    public float maxVerticalAngle = 60.0f;  // Max look angle (up/down)

    [Header("Collision Settings")]
    public float collisionRadius = 0.3f;
    public float minDistance = 1.0f;
    public LayerMask collisionLayers;

    // Private variables
    private float currentYaw = 0.0f;
    private float currentPitch = 0.0f;
    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        // Initialize the camera position
        if (target != null)
        {
            transform.position = CalculateIdealPosition();
            transform.LookAt(target.position + Vector3.up * heightOffset);
        }

        // Store initial rotation
        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;

        // Hide and lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Handle input for camera rotation
        HandleCameraRotation();

        // Calculate and move to new position
        Vector3 targetPosition = CalculateIdealPosition();

        // Check for collision
        targetPosition = HandleCameraCollision(targetPosition);

        // Smoothly move the camera
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            1.0f / followSpeed
        );

        // Make camera look at target
        transform.LookAt(target.position + Vector3.up * heightOffset);
    }

    private void HandleCameraRotation()
    {
        // Only get input if cursor is locked
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Update camera rotation
            currentYaw += mouseX;
            currentPitch -= mouseY; // Invert Y axis

            // Clamp the pitch angle
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    private Vector3 CalculateIdealPosition()
    {
        // Calculate rotation based on current angles
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // Calculate the camera position based on the rotation
        Vector3 targetPosition = target.position;
        Vector3 direction = rotation * -Vector3.forward;

        return targetPosition + direction * followDistance + Vector3.up * heightOffset;
    }

    private Vector3 HandleCameraCollision(Vector3 desiredPosition)
    {
        // Direction from target to desired position
        Vector3 direction = (desiredPosition - target.position).normalized;
        float distance = Vector3.Distance(target.position, desiredPosition);

        // Check for collision with a sphere cast
        RaycastHit hit;
        if (Physics.SphereCast(
            target.position,
            collisionRadius,
            direction,
            out hit,
            distance,
            collisionLayers))
        {
            // If we hit something, move the camera closer to avoid clipping
            float adjustedDistance = hit.distance - 0.1f; // Small buffer

            // Ensure we don't go below minimum distance
            adjustedDistance = Mathf.Max(adjustedDistance, minDistance);

            // Return the adjusted position
            return target.position + direction * adjustedDistance;
        }

        // No collision, return original desired position
        return desiredPosition;
    }

    // Public method to change the target (used during transformation)
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            target = newTarget;
        }
    }

    // Public methods to adjust camera settings
    public void SetFollowDistance(float distance)
    {
        followDistance = Mathf.Max(minDistance, distance);
    }

    public void SetHeightOffset(float height)
    {
        heightOffset = height;
    }

    // Toggle cursor lock when pressing Escape
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    // Helper method to toggle cursor lock
    private void ToggleCursorLock()
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