using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public float followDistance = 5.0f;
    public float heightOffset = 1.5f;
    public float followSpeed = 10.0f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 3.0f;
    public float minVerticalAngle = -30.0f;
    public float maxVerticalAngle = 60.0f;

    [Header("Collision Settings")]
    public float collisionRadius = 0.3f;
    public float minDistance = 1.0f;
    public LayerMask collisionLayers;

    private float currentYaw = 0.0f;
    private float currentPitch = 0.0f;
    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        InitializeCamera();
        SetupCursor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        HandleCameraRotation();
        UpdateCameraPosition();
    }

    private void InitializeCamera()
    {
        if (target != null)
        {
            transform.position = CalculateIdealPosition();
            transform.LookAt(target.position + Vector3.up * heightOffset);
        }

        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;
    }

    private void SetupCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleCameraRotation()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = CalculateIdealPosition();
        targetPosition = HandleCameraCollision(targetPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            1.0f / followSpeed
        );

        transform.LookAt(target.position + Vector3.up * heightOffset);
    }

    private Vector3 CalculateIdealPosition()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 targetPosition = target.position;
        Vector3 direction = rotation * -Vector3.forward;

        return targetPosition + direction * followDistance + Vector3.up * heightOffset;
    }

    private Vector3 HandleCameraCollision(Vector3 desiredPosition)
    {
        Vector3 direction = (desiredPosition - target.position).normalized;
        float distance = Vector3.Distance(target.position, desiredPosition);

        RaycastHit hit;
        if (Physics.SphereCast(
            target.position,
            collisionRadius,
            direction,
            out hit,
            distance,
            collisionLayers))
        {
            float adjustedDistance = hit.distance - 0.1f;
            adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
            return target.position + direction * adjustedDistance;
        }

        return desiredPosition;
    }

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

    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            target = newTarget;
        }
    }

    public void SetFollowDistance(float distance)
    {
        followDistance = Mathf.Max(minDistance, distance);
    }

    public void SetHeightOffset(float height)
    {
        heightOffset = height;
    }
}