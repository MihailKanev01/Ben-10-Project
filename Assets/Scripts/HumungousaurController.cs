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

    private int speedHash;
    private int jumpHash;
    private int groundedHash;
    private int poundHash;
    private int growHash;
    private int standingHash;
    private int roarHash;

    void Start()
    {
        InitializeComponents();
        SetupAnimation();
        SetupGroundCheck();
    }

    void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        originalScale = transform.localScale;
    }

    void SetupAnimation()
    {
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
    }

    void SetupGroundCheck()
    {
        if (groundCheck == null)
        {
            groundCheck = new GameObject("HumungousaurGroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

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

        if (animator != null)
        {
            animator.SetFloat(speedHash, currentSpeed);
        }
    }

    void HandleJumping()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            float jumpVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            velocity.y = jumpVelocity;

            if (animator != null)
            {
                animator.SetTrigger(jumpHash);
            }

            isGrounded = false;
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleAbilities()
    {
        if (Input.GetKeyDown(groundPoundKey) && isGrounded && groundPoundCooldownRemaining <= 0)
        {
            PerformGroundPound();
        }

        if (Input.GetKeyDown(growSizeKey) && isGrounded)
        {
            ToggleSize();
        }

        if (Input.GetKeyDown(KeyCode.R) && isGrounded && animator != null)
        {
            animator.SetTrigger(roarHash);

            if (audioSource != null && roarSound != null)
            {
                audioSource.PlayOneShot(roarSound);
            }
        }
    }

    void UpdateCameraTarget()
    {
        if (cameraTarget != null)
        {
            float heightOffset = 1.5f * currentSizeMultiplier;
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + heightOffset, transform.position.z);
        }
    }

    void PerformGroundPound()
    {
        if (animator != null)
        {
            animator.SetTrigger(poundHash);
        }

        if (groundPoundEffect != null)
        {
            groundPoundEffect.transform.position = groundCheck.position;
            groundPoundEffect.Play();
        }

        if (audioSource != null && groundPoundSound != null)
        {
            audioSource.PlayOneShot(groundPoundSound);
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
        if (isGrowing) return;
        StartCoroutine(ChangeSize());
    }

    System.Collections.IEnumerator ChangeSize()
    {
        isGrowing = true;
        float targetMultiplier = (currentSizeMultiplier == 1.0f) ? maxSizeMultiplier : 1.0f;

        if (animator != null)
        {
            animator.SetTrigger(growHash);
        }

        if (growEffect != null)
        {
            growEffect.Play();
        }

        if (audioSource != null && growSound != null)
        {
            audioSource.PlayOneShot(growSound);
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

        while (elapsedTime < growthDuration)
        {
            float t = elapsedTime / growthDuration;
            float smoothT = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            controller.height = Mathf.Lerp(startHeight, targetHeight, smoothT);
            controller.radius = Mathf.Lerp(startRadius, targetRadius, smoothT);
            controller.center = Vector3.Lerp(startCenter, targetCenter, smoothT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        controller.height = targetHeight;
        controller.radius = targetRadius;
        controller.center = targetCenter;

        currentSizeMultiplier = targetMultiplier;

        if (targetMultiplier > 1.0f && animator != null)
        {
            animator.SetTrigger(roarHash);

            if (audioSource != null && roarSound != null)
            {
                audioSource.PlayOneShot(roarSound);
            }
        }

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