// WayBigController.cs - Complete script with standing animation support
using UnityEngine;

public class WayBigController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 8.0f;
    public float runSpeed = 15.0f;
    public float jumpForce = 15.0f;
    public float turnSmoothTime = 0.2f;
    public float speedSmoothTime = 0.3f;
    public float gravity = -30.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 1.5f;
    public LayerMask groundMask;

    [Header("Special Abilities")]
    public GameObject cosmicRayPrefab;
    public Transform raySpawnPoint;
    public float rayCooldown = 5f;
    public KeyCode rayAttackKey = KeyCode.F;

    [Header("Stomp Attack")]
    public float stompRadius = 10f;
    public float stompDamage = 50f;
    public float stompForce = 20f;
    public KeyCode stompKey = KeyCode.E;
    public LayerMask enemyLayers;
    public ParticleSystem stompEffect;
    public ParticleSystem rayEffect;

    [Header("References")]
    public Transform cameraTarget; // Empty object for camera to follow

    // Private variables
    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 velocity;
    private bool isGrounded;
    private float rayCooldownRemaining = 0f;
    private float stompCooldownRemaining = 0f;
    private Transform mainCamera;
    private bool isInitialized = false;
    private bool isInStandingAnimation = false;

    // Animation parameter hashes
    private int speedHash;
    private int jumpHash;
    private int groundedHash;
    private int rayAttackHash;
    private int stompAttackHash;
    private int standingHash;

    void Awake()
    {
        // Get the CharacterController component
        controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        // Reset variables when the component is enabled
        velocity = Vector3.zero;
        currentSpeed = 0;
        isGrounded = false;

        // Make sure we initialize when enabled
        isInitialized = false;

        // Do a delayed initialization to ensure all components are active
        Invoke("Initialize", 0.1f);
    }

    void Initialize()
    {
        // Ensure controller exists and is enabled
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        if (controller != null)
        {
            controller.enabled = true;
        }

        // Get other components
        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        // Cache animation parameter hashes
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
            rayAttackHash = Animator.StringToHash("RayAttack");
            stompAttackHash = Animator.StringToHash("StompAttack");
            standingHash = Animator.StringToHash("Standing");
        }

        // Set up camera target position
        SetupCameraTarget();

        isInitialized = true;
        Debug.Log("Way Big controller initialized");
    }

    void SetupCameraTarget()
    {
        if (cameraTarget)
        {
            // Set the camera target to be at Way Big's upper body
            cameraTarget.localPosition = new Vector3(0, 8.0f, 0); // Adjust the Y value based on Way Big's height
        }
    }

    void Update()
    {
        // Skip if not initialized or controller is disabled
        if (!isInitialized || controller == null || !controller.enabled)
        {
            return;
        }

        // Check if we are in standing animation via animator
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("StandingAnimation"))
        {
            isInStandingAnimation = true;
            return; // Skip movement control during standing animation
        }
        else
        {
            isInStandingAnimation = false;
        }

        // Update cooldowns
        if (rayCooldownRemaining > 0)
        {
            rayCooldownRemaining -= Time.deltaTime;
        }

        if (stompCooldownRemaining > 0)
        {
            stompCooldownRemaining -= Time.deltaTime;
        }

        // Check if grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Update animator grounded parameter
        if (animator)
        {
            animator.SetBool(groundedHash, isGrounded);
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

            // Set target speed based on input
            float targetSpeed = ((running) ? runSpeed : walkSpeed) * direction.magnitude;

            // Smooth speed transitions
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

            // Move the character (ensuring controller is active)
            if (controller.enabled)
            {
                controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Slow down to zero if no input
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime);
        }

        // Update animator speed parameter
        if (animator)
        {
            animator.SetFloat(speedHash, currentSpeed);
        }

        // Handle jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            // Trigger jump animation
            if (animator)
            {
                animator.SetTrigger(jumpHash);
            }
        }

        // Apply gravity (ensuring controller is active)
        velocity.y += gravity * Time.deltaTime;
        if (controller.enabled)
        {
            controller.Move(velocity * Time.deltaTime);
        }

        // Cosmic ray attack
        if (Input.GetKeyDown(rayAttackKey) && rayCooldownRemaining <= 0)
        {
            FireCosmicRay();
        }

        // Stomp attack
        if (Input.GetKeyDown(stompKey) && isGrounded && stompCooldownRemaining <= 0)
        {
            PerformStomp();
        }
    }

    void FireCosmicRay()
    {
        if (cosmicRayPrefab == null || raySpawnPoint == null)
        {
            Debug.LogWarning("Cosmic Ray prefab or spawn point not set!");
            return;
        }

        // Play animation
        if (animator)
        {
            animator.SetTrigger(rayAttackHash);
        }

        // Instantiate ray projectile
        GameObject ray = Instantiate(cosmicRayPrefab, raySpawnPoint.position, raySpawnPoint.rotation);

        // Play effect
        if (rayEffect != null)
        {
            rayEffect.Play();
        }

        // Set cooldown
        rayCooldownRemaining = rayCooldown;

        Debug.Log("Way Big: Fired Cosmic Ray!");
    }

    void PerformStomp()
    {
        // Play animation
        if (animator)
        {
            animator.SetTrigger(stompAttackHash);
        }

        // Play effect
        if (stompEffect != null)
        {
            stompEffect.Play();
        }

        // Detect enemies in stomp radius
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, stompRadius, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            // Apply damage if enemy has health component
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(stompDamage);
            }

            // Apply force
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(stompForce, transform.position, stompRadius, 2.0f, ForceMode.Impulse);
            }
        }

        // Set cooldown
        stompCooldownRemaining = rayCooldown;

        Debug.Log("Way Big: Performed Stomp Attack!");
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

    // Draw gizmos for visualization in editor
    void OnDrawGizmosSelected()
    {
        // Draw ground check radius
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        // Draw stomp radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
    }
}
