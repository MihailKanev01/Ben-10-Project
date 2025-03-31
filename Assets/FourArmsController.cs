using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FourArmsController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10.0f;
    public float jumpForce = 8.0f;
    public float gravity = -20.0f;
    public float turnSmoothTime = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

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

        // Always get the main camera (don't make this optional)
        cameraTransform = Camera.main.transform;

        // If animator reference is missing, try to get it
        if (animator == null)
            animator = GetComponent<Animator>();

        // Create ground check if not assigned
        if (groundCheck == null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.parent = transform;
            checkObj.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheck = checkObj.transform;
            Debug.Log("Created ground check object. Adjust its position if needed.");
        }
    }

    private void Update()
    {
        // Check if grounded
        CheckGrounded();

        // Handle movement
        HandleMovement();

        // Handle jumping
        HandleJumping();

        // Apply gravity
        ApplyGravity();

        // Update animations
        UpdateAnimations();
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void HandleMovement()
    {
        // Get input axes
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Create direction vector based on input
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Calculate the angle based on camera orientation
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;

            // Smoothly rotate the character to face movement direction
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Move in the direction character is facing
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }
    }

    private void HandleJumping()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            // Calculate movement speed for animation
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            float speed = direction.magnitude;

            // Set running speed parameter
            animator.SetFloat("Speed", speed);

            // Set grounded parameter
            animator.SetBool("Grounded", isGrounded);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}