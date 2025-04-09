using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Position Settings")]
    public float defaultDistance = 5.0f;
    public float minDistance = 1.5f;
    public float maxDistance = 8.0f;
    public float heightOffset = 1.6f;
    public float targetOffset = 0.75f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 3.0f;
    public float minVerticalAngle = -30.0f;
    public float maxVerticalAngle = 60.0f;

    [Header("Smoothing Settings")]
    public float positionSmoothTime = 0.1f;
    public float rotationSmoothTime = 0.1f;
    public float zoomSmoothTime = 0.2f;

    public enum CameraMode { Close, Medium, Far, Combat }

    [Header("Camera Modes")]
    public CameraMode currentMode = CameraMode.Medium;
    public float closeModeDistance = 3.0f;
    public float mediumModeDistance = 5.0f;
    public float farModeDistance = 7.0f;
    public float combatModeDistance = 4.0f;
    public KeyCode switchModeKey = KeyCode.V;

    [Header("Collision Settings")]
    public float collisionRadius = 0.3f;
    public float collisionOffset = 0.2f;
    public LayerMask collisionLayers;

    [Header("Smart Following")]
    public bool followTargetOrientation = true;
    public float orientationDelay = 0.5f;
    public float orientationSpeed = 5.0f;

    private float currentDistance;
    private float targetDistance;
    private float currentYaw = 0.0f;
    private float currentPitch = 10.0f;
    private Vector3 currentVelocity = Vector3.zero;
    private float yawSmoothVelocity = 0.0f;
    //private float pitchSmoothVelocity = 0.0f;
    private float distanceSmoothVelocity = 0.0f;
    private float lastMoveTime = 0.0f;
    private bool isPlayerMoving = false;
    private float targetYaw = 0.0f;
    private Vector3 targetPosition;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("No target assigned to ThirdPersonCamera!");
            return;
        }

        currentDistance = defaultDistance;
        targetDistance = defaultDistance;

        UpdateDistanceForMode();

        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if (Input.GetKeyDown(switchModeKey))
        {
            SwitchCameraMode();
        }

        CheckPlayerMovement();
        HandleCameraRotation();
        UpdateCameraTransform();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    void CheckPlayerMovement()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (input.magnitude > 0.1f)
        {
            isPlayerMoving = true;
            lastMoveTime = Time.time;

            if (followTargetOrientation)
            {
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
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            if (isPlayerMoving && followTargetOrientation)
            {
                currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawSmoothVelocity, rotationSmoothTime * 2);
            }
            else
            {
                currentYaw += mouseX;
            }

            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    void UpdateCameraTransform()
    {
        UpdateDistanceForMode();

        Vector3 focusPoint = target.position + Vector3.up * heightOffset;
        Vector3 directionToCamera = Quaternion.Euler(currentPitch, currentYaw, 0) * -Vector3.forward;
        Vector3 desiredPosition = focusPoint + directionToCamera * targetDistance;

        targetPosition = HandleCameraCollision(focusPoint, desiredPosition);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        Vector3 lookTarget = focusPoint + target.forward * targetOffset;
        transform.LookAt(lookTarget);
    }

    Vector3 HandleCameraCollision(Vector3 focusPoint, Vector3 desiredPosition)
    {
        Vector3 direction = (desiredPosition - focusPoint).normalized;
        float targetDistance = Vector3.Distance(focusPoint, desiredPosition);

        RaycastHit hit;
        if (Physics.SphereCast(focusPoint, collisionRadius, direction, out hit, targetDistance, collisionLayers))
        {
            float adjustedDistance = hit.distance - collisionOffset;
            adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
            currentDistance = Mathf.SmoothDamp(currentDistance, adjustedDistance, ref distanceSmoothVelocity, zoomSmoothTime);
            return focusPoint + direction * currentDistance;
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceSmoothVelocity, zoomSmoothTime);
        return focusPoint + direction * currentDistance;
    }

    void SwitchCameraMode()
    {
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

        UpdateDistanceForMode();
    }

    void UpdateDistanceForMode()
    {
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
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
            currentPitch = 10f;
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