using UnityEngine;
using System.Collections;

public class HumungousaurController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6.0f;      // Increased to match Ben's feel
    public float runSpeed = 10.0f;       // Increased to match Ben's feel
    public float turnSmoothTime = 0.1f;  // Matched to Ben's settings
    public float speedSmoothTime = 0.1f; // Matched to Ben's settings

    [Header("Jump Settings")]
    public float jumpForce = 8.0f;      // Matched to Ben's settings
    public float gravity = -15.0f;      // Matched to Ben's settings

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;  // Matched to Ben's settings
    public LayerMask groundMask;

    [Header("Special Abilities")]
    public float growthDuration = 1.5f;  // How long the growth transition takes
    public float maxSizeMultiplier = 2.0f;
    public float groundPoundRadius = 6.0f;
    public float groundPoundDamage = 30.0f;
    public float groundPoundCooldown = 5.0f;
    public KeyCode groundPoundKey = KeyCode.E;
    public KeyCode growSizeKey = KeyCode.F;

    [Header("Effects")]
    public ParticleSystem groundPoundEffect;
    public ParticleSystem growEffect;

    [Header("References")]
    public Transform cameraTarget;

    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCamera;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private float currentSizeMultiplier = 1.0f;
    private bool isGrowing = false;
    private float groundPoundCooldownRemaining = 0f;

    private int speedHash;
    private int jumpHash;
    private int groundedHash;

    void Start()
    {
        Debug.Log("[Humungousaur] Starting initialization");
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Important: Store the original scale
        originalScale = transform.localScale;

        // Only set up the animation parameters that actually exist
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
        }

        if (groundCheck == null)
        {
            groundCheck = new GameObject("HumungousaurGroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
        }

        // Set ground mask to everything if not specified
        if (groundMask.value == 0)
        {
            groundMask = -1; // Everything
        }

        isGrowing = false;

        // Set up camera target if missing
        if (cameraTarget == null)
        {
            cameraTarget = new GameObject("HumungosaurCameraTarget").transform;
            cameraTarget.SetParent(transform);
            cameraTarget.localPosition = new Vector3(0, 1.5f, 0);
        }
    }

    void Update()
    {
        if (controller == null || !controller.enabled)
            return;

        if (isGrowing)
            return;

        UpdateCooldowns();
        CheckGroundState();
        HandleMovement();
        HandleJumping();
        ApplyGravity();
        HandleAbilities();
        UpdateCameraTarget();
    }

    void UpdateCooldowns()
    {
        if (groundPoundCooldownRemaining > 0)
        {
            groundPoundCooldownRemaining -= Time.deltaTime;
        }
    }

    void CheckGroundState()
    {
        // Use consistent ground check method from Ben's controller
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (animator != null)
        {
            animator.SetBool(groundedHash, isGrounded);
        }
    }

    void HandleMovement()
    {
        // Use Ben's movement code for consistency
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Get movement direction from camera
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float targetSpeed = ((running) ? runSpeed : walkSpeed) * direction.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime);
        }

        if (animator != null)
        {
            animator.SetFloat(speedHash, currentSpeed);
        }
    }

    void HandleJumping()
    {
        // Use Ben's jumping code for consistency
        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            if (animator != null)
            {
                animator.SetTrigger(jumpHash);
            }

            isGrounded = false; // Immediate feedback
        }
    }

    void ApplyGravity()
    {
        // Use consistent gravity application method
        velocity.y += gravity * Time.deltaTime;

        // Limit terminal velocity
        if (velocity.y < -30f)
            velocity.y = -30f;

        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
    }

    void HandleAbilities()
    {
        // Check for F key to toggle size
        if (Input.GetKeyDown(growSizeKey))
        {
            Debug.Log("[Humungousaur] Growth key pressed");
            ToggleSize();
        }

        // Check for ground pound
        if (Input.GetKeyDown(groundPoundKey) && isGrounded && groundPoundCooldownRemaining <= 0)
        {
            PerformGroundPound();
        }
    }

    void UpdateCameraTarget()
    {
        // Update camera target position to follow the character with proper height
        if (cameraTarget != null)
        {
            // Scale the height with size for proper camera positioning during growth
            float heightOffset = 1.5f * currentSizeMultiplier;
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + heightOffset, transform.position.z);
        }
    }

    void PerformGroundPound()
    {
        Debug.Log("[Humungousaur] Performing ground pound");

        if (groundPoundEffect != null)
        {
            groundPoundEffect.transform.position = groundCheck.position;
            groundPoundEffect.Play();
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, groundPoundRadius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
                continue;

            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(groundPoundDamage);
            }

            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(groundPoundDamage * 10f, transform.position, groundPoundRadius, 1.0f, ForceMode.Impulse);
            }
        }

        groundPoundCooldownRemaining = groundPoundCooldown;
    }

    void ToggleSize()
    {
        Debug.Log("[Humungousaur] ToggleSize called. isGrowing: " + isGrowing);

        if (isGrowing)
        {
            Debug.Log("[Humungousaur] Already growing, ignoring request");
            return;
        }

        Debug.Log("[Humungousaur] Starting growth coroutine");
        StartCoroutine(ChangeSize());
    }

    IEnumerator ChangeSize()
    {
        isGrowing = true;
        float targetMultiplier = (currentSizeMultiplier == 1.0f) ? maxSizeMultiplier : 1.0f;

        Debug.Log("[Humungousaur] ChangeSize started. Growing from " + currentSizeMultiplier + "x to " + targetMultiplier + "x");

        if (growEffect != null)
        {
            growEffect.Play();
        }

        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * targetMultiplier;

        float startHeight = controller.height;
        float targetHeight = startHeight * (targetMultiplier / currentSizeMultiplier);
        float startRadius = controller.radius;
        float targetRadius = startRadius * (targetMultiplier / currentSizeMultiplier);
        Vector3 startCenter = controller.center;
        Vector3 targetCenter = startCenter * (targetMultiplier / currentSizeMultiplier);

        // Use a smooth curve for the growth effect
        while (elapsedTime < growthDuration)
        {
            float t = elapsedTime / growthDuration;

            // Smooth step formula for more natural growth
            float smoothT = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            controller.height = Mathf.Lerp(startHeight, targetHeight, smoothT);
            controller.radius = Mathf.Lerp(startRadius, targetRadius, smoothT);
            controller.center = Vector3.Lerp(startCenter, targetCenter, smoothT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we end at exactly the target values
        transform.localScale = targetScale;
        controller.height = targetHeight;
        controller.radius = targetRadius;
        controller.center = targetCenter;

        currentSizeMultiplier = targetMultiplier;

        Debug.Log("[Humungousaur] Size transition complete. Now at " + currentSizeMultiplier + "x size");
        isGrowing = false;
    }

    public void SetControllerActive(bool active)
    {
        this.enabled = active;
        if (controller != null)
        {
            controller.enabled = active;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundPoundRadius);
    }
}