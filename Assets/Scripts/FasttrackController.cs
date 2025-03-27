// FasttrackController.cs - Attach to the Fasttrack model
using UnityEngine;
using System.Collections;

public class FasttrackController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 7.0f;           // Faster than normal walk speed
    public float runSpeed = 15.0f;           // Normal running is already fast
    public float superSpeedMultiplier = 3.0f; // Multiplier during super speed
    public float turnSmoothTime = 0.1f;      // Quick turning
    public float speedSmoothTime = 0.1f;     // Quick acceleration

    [Header("Jump Settings")]
    public float jumpForce = 10.0f;
    public float gravity = -25.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Speed Abilities")]
    public float speedBoostDuration = 3.0f;   // How long super speed lasts
    public float speedBoostCooldown = 5.0f;   // Cooldown between uses
    public float dashDistance = 15.0f;        // How far a dash goes
    public float dashCooldown = 2.0f;         // Cooldown between dashes
    public KeyCode speedBoostKey = KeyCode.Q; // Hold for sustained speed boost
    public KeyCode dashKey = KeyCode.E;       // Tap for instant dash

    [Header("Visual Effects")]
    public TrailRenderer speedTrail;          // Trail renderer for speed effects
    public ParticleSystem speedBoostEffect;   // Particle effect during speed boost
    public ParticleSystem dashEffect;         // Particle effect for dash

    [Header("Audio")]
    public AudioClip speedBoostSound;
    public AudioClip dashSound;

    [Header("References")]
    public Transform cameraTarget;            // Empty object for camera to follow

    // Private variables
    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCamera;
    private AudioSource audioSource;

    // Ability cooldowns and states
    private float speedBoostCooldownRemaining = 0f;
    private float dashCooldownRemaining = 0f;
    private bool isSuperSpeedActive = false;
    private bool isDashing = false;

    // Animation parameter hashes
    private int speedHash;
    private int jumpHash;
    private int groundedHash;
    private int dashHash;
    private int superSpeedHash;

    void Start()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Cache animation parameter hashes
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
            dashHash = Animator.StringToHash("Dash");
            superSpeedHash = Animator.StringToHash("SuperSpeed");
        }

        // Create ground check if missing
        if (groundCheck == null)
        {
            groundCheck = new GameObject("FasttrackGroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
        }

        // Disable trail renderer initially
        if (speedTrail != null)
        {
            speedTrail.emitting = false;
        }
    }

    void Update()
    {
        // Skip if controller is disabled
        if (controller == null || !controller.enabled)
            return;

        // Update cooldowns
        if (speedBoostCooldownRemaining > 0)
        {
            speedBoostCooldownRemaining -= Time.deltaTime;
        }

        if (dashCooldownRemaining > 0)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }

        // Skip movement during dash
        if (isDashing)
            return;

        // Check if grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Update animator
        if (animator != null)
        {
            animator.SetBool(groundedHash, isGrounded);
            animator.SetBool(superSpeedHash, isSuperSpeedActive);
        }

        // Handle speed boost activation/deactivation
        if (Input.GetKeyDown(speedBoostKey) && speedBoostCooldownRemaining <= 0 && !isSuperSpeedActive)
        {
            StartCoroutine(ActivateSuperSpeed());
        }

        // Handle dash ability
        if (Input.GetKeyDown(dashKey) && dashCooldownRemaining <= 0 && isGrounded)
        {
            StartCoroutine(PerformDash());
        }

        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);

        // Calculate movement direction relative to camera
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Apply movement
        if (direction.magnitude >= 0.1f)
        {
            // Calculate target angle for rotation (based on camera)
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;

            // Smooth rotation
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Get movement direction
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Set target speed based on input and super speed state
            float targetSpeed = walkSpeed;

            if (running) targetSpeed = runSpeed;
            if (isSuperSpeedActive) targetSpeed *= superSpeedMultiplier;

            targetSpeed *= direction.magnitude;

            // Smooth speed transitions
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

            // Move the character
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            // Slow down to zero if no input
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime);
        }

        // Update animator speed parameter
        if (animator != null)
        {
            animator.SetFloat(speedHash, currentSpeed);
        }

        // Handle jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Calculate jump velocity
            float jumpVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            velocity.y = jumpVelocity;

            // Trigger jump animation
            if (animator != null)
            {
                animator.SetTrigger(jumpHash);
            }

            isGrounded = false;
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Update camera target position
        if (cameraTarget != null)
        {
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
        }
    }

    IEnumerator ActivateSuperSpeed()
    {
        // Activate super speed
        isSuperSpeedActive = true;

        // Visual effects
        if (speedTrail != null)
        {
            speedTrail.emitting = true;
        }

        if (speedBoostEffect != null)
        {
            speedBoostEffect.Play();
        }

        // Play sound
        if (audioSource != null && speedBoostSound != null)
        {
            audioSource.PlayOneShot(speedBoostSound);
        }

        Debug.Log("Fasttrack: Super Speed activated!");

        // Stay in super speed for the duration
        yield return new WaitForSeconds(speedBoostDuration);

        // Deactivate super speed
        isSuperSpeedActive = false;

        // Turn off visual effects
        if (speedTrail != null)
        {
            speedTrail.emitting = false;
        }

        if (speedBoostEffect != null)
        {
            speedBoostEffect.Stop();
        }

        // Start cooldown
        speedBoostCooldownRemaining = speedBoostCooldown;

        Debug.Log("Fasttrack: Super Speed deactivated. Cooldown started.");
    }

    IEnumerator PerformDash()
    {
        isDashing = true;

        // Trigger dash animation
        if (animator != null)
        {
            animator.SetTrigger(dashHash);
        }

        // Play dash effect
        if (dashEffect != null)
        {
            dashEffect.Play();
        }

        // Play sound
        if (audioSource != null && dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }

        // Store current position and rotation
        Vector3 startPosition = transform.position;
        Vector3 dashDirection = transform.forward;

        // Calculate target position
        Vector3 targetPosition = startPosition + dashDirection * dashDistance;

        // Perform a raycast to check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(startPosition, dashDirection, out hit, dashDistance, groundMask))
        {
            // Adjust target position to stop before hitting obstacle
            targetPosition = hit.point - (dashDirection * controller.radius);
        }

        // Quick dash movement
        float dashDuration = 0.2f;
        float elapsedTime = 0;

        while (elapsedTime < dashDuration)
        {
            // Calculate dash progress
            float t = elapsedTime / dashDuration;
            t = t * t * (3f - 2f * t); // Smoothstep interpolation

            // Move character
            controller.enabled = false; // Temporarily disable controller to prevent collisions during dash
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            controller.enabled = true;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we reach target position exactly
        controller.enabled = false;
        transform.position = targetPosition;
        controller.enabled = true;

        // Set cooldown
        dashCooldownRemaining = dashCooldown;
        isDashing = false;

        Debug.Log("Fasttrack: Dash completed!");
    }

    // Public method to enable/disable controller (used by OmnitrixController)
    public void SetControllerActive(bool active)
    {
        this.enabled = active;
        if (controller != null)
        {
            controller.enabled = active;
        }
    }

    // Visual debugging
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            // Draw ground check sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        // Draw dash distance
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * dashDistance);
    }
}