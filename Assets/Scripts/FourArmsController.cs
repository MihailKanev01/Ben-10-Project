using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FourArmsController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10.0f;
    public float jumpForce = 15.0f;
    public float gravity = -20.0f;
    public float turnSmoothTime = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 1.0f;  // INCREASED to 1.0
    public LayerMask groundMask = -1;    // Set to Everything by default

    [Header("References")]
    public Animator animator;

    // Private variables
    private CharacterController controller;
    private float turnSmoothVelocity;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform cameraTransform;

    private void Start()
    {
        // Get component references
        controller = GetComponent<CharacterController>();

        // Always get the main camera
        cameraTransform = Camera.main.transform;

        // If animator reference is missing, try to get it
        if (animator == null)
            animator = GetComponent<Animator>();

        // Create ground check if not assigned
        if (groundCheck == null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.parent = transform;

            // Position the ground check much lower for FourArms
            checkObj.transform.localPosition = new Vector3(0, -2.0f, 0);

            groundCheck = checkObj.transform;
            Debug.Log("Created ground check at -2.0 height");
        }

        // If no ground mask is assigned, default to everything
        if (groundMask.value == 0)
        {
            groundMask = ~0; // Everything
        }

        // Extra diagnostic at startup
        Debug.Log($"Controller center: {controller.center}, height: {controller.height}, radius: {controller.radius}");
        Debug.Log($"Ground check at world pos: {groundCheck.position}, using mask: {groundMask.value}");
    }

    private void Update()
    {
        // Use multiple ground detection methods for reliability
        CheckGroundedMultiMethod();

        // Debug output - always show ground state
        Debug.DrawRay(groundCheck.position, Vector3.down * groundDistance, isGrounded ? Color.green : Color.red);

        HandleMovement();
        HandleJumping();
        ApplyGravity();
        UpdateAnimations();
    }

    private void CheckGroundedMultiMethod()
    {
        // Method 1: Sphere cast
        bool sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Method 2: Ray cast
        bool rayGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance * 1.5f, groundMask);

        // Method 3: Character controller's isGrounded
        bool controllerGrounded = controller.isGrounded;

        // Debug output all methods
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Ground detection: Sphere={sphereGrounded}, Ray={rayGrounded}, Controller={controllerGrounded}");
        }

        // Use the most reliable method or combination
        isGrounded = sphereGrounded || rayGrounded || controllerGrounded;

        // Reset Y velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void HandleMovement()
    {
        // Movement code remains the same
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }
    }

    private void HandleJumping()
    {
        // Enhanced jump detection
        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            // Use a stronger jump for a character like FourArms
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            Debug.Log($"JUMP TRIGGERED! Velocity={velocity.y}");

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
    }

    private void ApplyGravity()
    {
        // Apply gravity with limiting
        velocity.y += gravity * Time.deltaTime;

        // Limit terminal velocity
        if (velocity.y < -30f)
            velocity.y = -30f;

        // Apply vertical movement
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            float speed = direction.magnitude;

            animator.SetFloat("Speed", speed);
            animator.SetBool("Grounded", isGrounded);
        }
    }

    // Visual debugging
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            // Always show ground check in Scene view
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * (groundDistance * 1.5f));
        }
    }
}