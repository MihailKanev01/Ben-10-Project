// PlayerController.cs - Complete script with enhanced jump
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 6.0f;
    public float turnSmoothTime = 0.1f;
    public float speedSmoothTime = 0.1f;

    [Header("Jump Settings")]
    public float jumpForce = 8.0f; // Increased for more visible jumps
    public Vector3 jumpDirection = new Vector3(0.0f, 1.0f, 0.0f);
    public float gravity = -15.0f; // Reduced for longer jumps
    public bool debugJump = true;
    public GameObject jumpEffectPrefab; // Optional visual effect

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("References")]
    public Transform cameraTarget;

    // Private variables
    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 velocity;
    private bool isGrounded;
    private float targetSpeed;
    private Transform mainCamera;

    // Animation parameter hashes
    private int speedHash;
    private int jumpHash;
    private int groundedHash;

    void Start()
    {
        // Get components
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("No CharacterController found! Adding one automatically.");
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = new Vector3(0, 1f, 0);
            controller.radius = 0.5f;
        }

        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Cache animation hashes
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
        }
        else
        {
            Debug.LogWarning("No Animator component found on player!");
        }

        // Initialize jump direction
        jumpDirection = new Vector3(0.0f, 1.0f, 0.0f).normalized;

        // Check for missing components
        if (groundCheck == null)
        {
            Debug.LogError("GroundCheck is not assigned! Creating one at feet position.");
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.parent = transform;
            groundCheck.localPosition = new Vector3(0, -0.9f, 0); // Positioned at feet
        }

        if (groundMask.value == 0)
        {
            Debug.LogWarning("GroundMask is not set! Setting to default layer.");
            groundMask = 1 << LayerMask.NameToLayer("Default"); // Set to Default layer
        }

        Debug.Log("Player Controller initialized. Press SPACE to jump.");
    }

    void Update()
    {
        // Check if grounded - with debug logging
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (debugJump && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Space pressed! isGrounded: {isGrounded}, velocity.y: {velocity.y}");
            Debug.Log($"GroundCheck position: {groundCheck.position}, groundDistance: {groundDistance}");
            Debug.Log($"GroundMask: {groundMask.value}");
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Update animator ground state
        if (animator != null)
        {
            animator.SetBool(groundedHash, isGrounded);
        }

        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);

        // Calculate movement direction
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Apply movement
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            targetSpeed = ((running) ? runSpeed : walkSpeed) * direction.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime);
        }

        // Update animator
        if (animator != null)
        {
            animator.SetFloat(speedHash, currentSpeed);
        }

        // JUMP HANDLING - with visual feedback
        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            // Set jump velocity - MUCH HIGHER to be clearly visible
            float jumpVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            velocity.y = jumpVelocity;

            // Create visual effect at feet (optional)
            if (jumpEffectPrefab != null)
            {
                Instantiate(jumpEffectPrefab, groundCheck.position, Quaternion.identity);
            }

            // Print status to confirm jump is triggered
            if (debugJump)
            {
                Debug.Log($"JUMP INITIATED! Jump velocity: {jumpVelocity}");
                // Force draw a line in scene view to show jump trajectory
                Debug.DrawRay(transform.position, Vector3.up * jumpVelocity * 0.1f, Color.green, 1.0f);
            }

            // Trigger animation
            if (animator != null)
            {
                animator.SetTrigger(jumpHash);
                if (debugJump)
                {
                    Debug.Log("Jump animation triggered!");
                }
            }

            isGrounded = false;
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Move character with velocity
        controller.Move(velocity * Time.deltaTime);

        // Add visual debug for velocity
        if (debugJump)
        {
            // Draw persistent debug ray to show current velocity
            Debug.DrawRay(transform.position, Vector3.up * velocity.y * 0.05f,
                velocity.y > 0 ? Color.green : Color.red, Time.deltaTime);

            if (Mathf.Abs(velocity.y) > 1.0f)
            {
                Debug.Log($"Y velocity: {velocity.y:F2}");
            }
        }

        // Update camera target
        if (cameraTarget != null)
        {
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
        }
        else if (debugJump)
        {
            Debug.LogWarning("Camera target is not assigned!");
        }
    }

    // Visual debugging
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        // Draw ground check sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);

        // Draw a ray showing jump direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, jumpDirection * jumpForce * 0.25f);
    }

    // Enable/disable controller
    public void SetControllerActive(bool active)
    {
        this.enabled = active;
        if (controller != null)
        {
            controller.enabled = active;
        }
    }
}