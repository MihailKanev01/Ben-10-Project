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
    public Transform cameraTarget;

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

    private int speedHash;
    private int jumpHash;
    private int groundedHash;
    private int rayAttackHash;
    private int stompAttackHash;
    private int standingHash;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        velocity = Vector3.zero;
        currentSpeed = 0;
        isGrounded = false;
        isInitialized = false;
        Invoke("Initialize", 0.1f);
    }

    void Initialize()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        if (controller != null)
        {
            controller.enabled = true;
        }

        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
            rayAttackHash = Animator.StringToHash("RayAttack");
            stompAttackHash = Animator.StringToHash("StompAttack");
            standingHash = Animator.StringToHash("Standing");
        }

        SetupCameraTarget();

        isInitialized = true;
    }

    void SetupCameraTarget()
    {
        if (cameraTarget)
        {
            cameraTarget.localPosition = new Vector3(0, 8.0f, 0);
        }
    }

    void Update()
    {
        if (!isInitialized || controller == null || !controller.enabled)
            return;

        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("StandingAnimation"))
        {
            isInStandingAnimation = true;
            return;
        }
        else
        {
            isInStandingAnimation = false;
        }

        UpdateCooldowns();
        CheckGroundState();
        HandleMovement();
        HandleJumping();
        ApplyGravity();
        CheckAbilityInputs();
    }

    void UpdateCooldowns()
    {
        if (rayCooldownRemaining > 0)
        {
            rayCooldownRemaining -= Time.deltaTime;
        }

        if (stompCooldownRemaining > 0)
        {
            stompCooldownRemaining -= Time.deltaTime;
        }
    }

    void CheckGroundState()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (animator)
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
            float targetSpeed = ((running) ? runSpeed : walkSpeed) * direction.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

            if (controller.enabled)
            {
                controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
            }
        }
        else
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime);
        }

        if (animator)
        {
            animator.SetFloat(speedHash, currentSpeed);
        }
    }

    void HandleJumping()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            if (animator)
            {
                animator.SetTrigger(jumpHash);
            }
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;

        if (controller.enabled)
        {
            controller.Move(velocity * Time.deltaTime);
        }
    }

    void CheckAbilityInputs()
    {
        if (Input.GetKeyDown(rayAttackKey) && rayCooldownRemaining <= 0)
        {
            FireCosmicRay();
        }

        if (Input.GetKeyDown(stompKey) && isGrounded && stompCooldownRemaining <= 0)
        {
            PerformStomp();
        }
    }

    void FireCosmicRay()
    {
        if (cosmicRayPrefab == null || raySpawnPoint == null)
            return;

        if (animator)
        {
            animator.SetTrigger(rayAttackHash);
        }

        GameObject ray = Instantiate(cosmicRayPrefab, raySpawnPoint.position, raySpawnPoint.rotation);

        if (rayEffect != null)
        {
            rayEffect.Play();
        }

        rayCooldownRemaining = rayCooldown;
    }

    void PerformStomp()
    {
        if (animator)
        {
            animator.SetTrigger(stompAttackHash);
        }

        if (stompEffect != null)
        {
            stompEffect.Play();
        }

        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, stompRadius, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(stompDamage);
            }

            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(stompForce, transform.position, stompRadius, 2.0f, ForceMode.Impulse);
            }
        }

        stompCooldownRemaining = rayCooldown;
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
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
    }
}