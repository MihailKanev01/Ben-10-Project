// HumungousaurController.cs - Attach to the Humungousaur model
using UnityEngine;

public class HumungousaurController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float runSpeed = 10.0f;
    public float turnSmoothTime = 0.2f;
    public float speedSmoothTime = 0.3f;

    [Header("Jump Settings")]
    public float jumpForce = 7.0f;
    public Vector3 jumpDirection = new Vector3(0.0f, 1.0f, 0.0f);
    public float gravity = -20.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.6f;
    public LayerMask groundMask;

    [Header("Special Abilities")]
    public float growthDuration = 3.0f;
    public float maxSizeMultiplier = 2.0f;
    public float groundPoundRadius = 6.0f;
    public float groundPoundDamage = 30.0f;
    public float groundPoundCooldown = 5.0f;
    public KeyCode groundPoundKey = KeyCode.E;
    public KeyCode growSizeKey = KeyCode.Q;

    [Header("Effects")]
    public ParticleSystem groundPoundEffect;
    public ParticleSystem growEffect;
    public AudioClip groundPoundSound;
    public AudioClip growSound;
    public AudioClip roarSound;

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
    private AudioSource audioSource;
    private Vector3 originalScale;
    private float currentSizeMultiplier = 1.0f;
    private bool isGrowing = false;
    private float groundPoundCooldownRemaining = 0f;

    // Animation parameter hashes
    private int speedHash;
    private int jumpHash;
    private int groundedHash;
    private int poundHash;
    private int growHash;
    private int standingHash;
    private int roarHash;

    void Start()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Store original scale
        originalScale = transform.localScale;

        // Cache animation parameter hashes
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
            poundHash = Animator.StringToHash("GroundPound");
            growHash = Animator.StringToHash("Grow");
            standingHash = Animator.StringToHash("Standing");
            roarHash = Animator.StringToHash("Roar");
        }

        // Create ground check if missing
        if (groundCheck == null)
        {
            groundCheck = new GameObject("HumungousaurGroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
        }
    }

    void Update()
    {
        // Skip if controller is disabled
        if (controller == null || !controller.enabled)
            return;

        // Skip movement during growing animation
        if (isGrowing)
            return;

        // Update cooldowns
        if (groundPoundCooldownRemaining > 0)
        {
            groundPoundCooldownRemaining -= Time.deltaTime;
        }

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
            targetSpeed = ((running) ? runSpeed : walkSpeed) * direction.magnitude;

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

        // Ground Pound ability
        if (Input.GetKeyDown(groundPoundKey) && isGrounded && groundPoundCooldownRemaining <= 0)
        {
            PerformGroundPound();
        }

        // Size Change ability
        if (Input.GetKeyDown(growSizeKey) && isGrounded)
        {
            ToggleSize();
        }

        // Roar ability (just for fun)
        if (Input.GetKeyDown(KeyCode.R) && isGrounded)
        {
            if (animator != null)
            {
                animator.SetTrigger(roarHash);

                if (audioSource != null && roarSound != null)
                {
                    audioSource.PlayOneShot(roarSound);
                }
            }
        }

        // Update camera target position
        if (cameraTarget != null)
        {
            // Adjust camera height based on current size
            float heightOffset = 1.5f * currentSizeMultiplier;
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + heightOffset, transform.position.z);
        }
    }

    void PerformGroundPound()
    {
        // Play animation
        if (animator != null)
        {
            animator.SetTrigger(poundHash);
        }

        // Play effect
        if (groundPoundEffect != null)
        {
            groundPoundEffect.transform.position = groundCheck.position;
            groundPoundEffect.Play();
        }

        // Play sound
        if (audioSource != null && groundPoundSound != null)
        {
            audioSource.PlayOneShot(groundPoundSound);
        }

        // Apply damage and physics force to nearby objects
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, groundPoundRadius);
        foreach (var hitCollider in hitColliders)
        {
            // Skip self
            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
                continue;

            // Apply damage to enemies
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(groundPoundDamage);
            }

            // Apply force to rigidbodies
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(groundPoundDamage * 10f, transform.position, groundPoundRadius, 1.0f, ForceMode.Impulse);
            }
        }

        // Set cooldown
        groundPoundCooldownRemaining = groundPoundCooldown;

        Debug.Log("Humungousaur: Ground Pound!");
    }

    void ToggleSize()
    {
        if (isGrowing) return;

        StartCoroutine(ChangeSize());
    }

    System.Collections.IEnumerator ChangeSize()
    {
        isGrowing = true;

        // Target scale depends on current size
        float targetMultiplier = (currentSizeMultiplier == 1.0f) ? maxSizeMultiplier : 1.0f;

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger(growHash);
        }

        // Play effect
        if (growEffect != null)
        {
            growEffect.Play();
        }

        // Play sound
        if (audioSource != null && growSound != null)
        {
            audioSource.PlayOneShot(growSound);
        }

        // Gradually change size
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * targetMultiplier;

        // Adjust character controller during size change
        float startHeight = controller.height;
        float targetHeight = startHeight * (targetMultiplier / currentSizeMultiplier);
        float startRadius = controller.radius;
        float targetRadius = startRadius * (targetMultiplier / currentSizeMultiplier);
        Vector3 startCenter = controller.center;
        Vector3 targetCenter = startCenter * (targetMultiplier / currentSizeMultiplier);

        while (elapsedTime < growthDuration)
        {
            // Calculate progress
            float t = elapsedTime / growthDuration;

            // Apply smooth step for better visual effect
            float smoothT = t * t * (3f - 2f * t);

            // Update scale
            transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            // Update character controller
            controller.height = Mathf.Lerp(startHeight, targetHeight, smoothT);
            controller.radius = Mathf.Lerp(startRadius, targetRadius, smoothT);
            controller.center = Vector3.Lerp(startCenter, targetCenter, smoothT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we reach the target exactly
        transform.localScale = targetScale;
        controller.height = targetHeight;
        controller.radius = targetRadius;
        controller.center = targetCenter;

        // Update current size multiplier
        currentSizeMultiplier = targetMultiplier;

        // If growing larger, roar
        if (targetMultiplier > 1.0f)
        {
            if (animator != null)
            {
                animator.SetTrigger(roarHash);

                if (audioSource != null && roarSound != null)
                {
                    audioSource.PlayOneShot(roarSound);
                }
            }
        }

        isGrowing = false;
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

        // Draw ground pound radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundPoundRadius);
    }
}