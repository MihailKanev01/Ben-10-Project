using UnityEngine;
using System.Collections;

public class FasttrackController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 7.0f;
    public float runSpeed = 15.0f;
    public float superSpeedMultiplier = 3.0f;
    public float turnSmoothTime = 0.1f;
    public float speedSmoothTime = 0.1f;

    [Header("Jump Settings")]
    public float jumpForce = 10.0f;
    public float gravity = -25.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Speed Abilities")]
    public float speedBoostDuration = 3.0f;
    public float speedBoostCooldown = 5.0f;
    public float dashDistance = 15.0f;
    public float dashCooldown = 2.0f;
    public KeyCode speedBoostKey = KeyCode.Q;
    public KeyCode dashKey = KeyCode.E;

    [Header("Visual Effects")]
    public TrailRenderer speedTrail;
    public ParticleSystem speedBoostEffect;
    public ParticleSystem dashEffect;

    [Header("Audio")]
    public AudioClip speedBoostSound;
    public AudioClip dashSound;

    [Header("Camera Settings")]
    public Transform cameraTarget;
    public float cameraTurnInfluence = 0.7f;
    public bool alignMovementWithCamera = true;

    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCamera;
    private AudioSource audioSource;

    private float speedBoostCooldownRemaining = 0f;
    private float dashCooldownRemaining = 0f;
    private bool isSuperSpeedActive = false;
    private bool isDashing = false;
    private float superSpeedTimeRemaining = 0f;
    private bool superSpeedSoundPlayed = false;

    private int speedHash;
    private int jumpHash;
    private int groundedHash;
    private int dashHash;
    private int superSpeedHash;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
            dashHash = Animator.StringToHash("Dash");
            superSpeedHash = Animator.StringToHash("SuperSpeed");
        }

        if (groundCheck == null)
        {
            groundCheck = new GameObject("FasttrackGroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
        }

        if (speedTrail != null)
        {
            speedTrail.emitting = false;
        }
    }

    void Update()
    {
        if (controller == null || !controller.enabled)
            return;

        UpdateCooldowns();

        if (isDashing)
            return;

        CheckGrounded();
        UpdateAnimator();
        HandleSuperSpeed();
        HandleDashAbility();
        ProcessMovement();
        HandleJumping();
        ApplyGravity();
        UpdateCameraTarget();
    }

    void UpdateCooldowns()
    {
        if (speedBoostCooldownRemaining > 0)
        {
            speedBoostCooldownRemaining -= Time.deltaTime;
        }

        if (dashCooldownRemaining > 0)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }
    }

    void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetBool(groundedHash, isGrounded);

            if (System.Array.Exists(animator.parameters, param => param.name == "SuperSpeed"))
            {
                animator.SetBool(superSpeedHash, isSuperSpeedActive);
            }
        }
    }

    void HandleSuperSpeed()
    {
        bool canActivate = speedBoostCooldownRemaining <= 0;

        if (Input.GetKey(speedBoostKey) && canActivate)
        {
            if (!isSuperSpeedActive)
            {
                ActivateSuperSpeed();
            }

            if (superSpeedTimeRemaining > 0)
            {
                superSpeedTimeRemaining -= Time.deltaTime;

                if (superSpeedTimeRemaining <= 0)
                {
                    DeactivateSuperSpeed();
                }
            }
        }
        else if (isSuperSpeedActive)
        {
            DeactivateSuperSpeed();
        }
    }

    void HandleDashAbility()
    {
        if (Input.GetKeyDown(dashKey) && dashCooldownRemaining <= 0 && isGrounded)
        {
            StartCoroutine(PerformDash());
        }
    }

    void ProcessMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveDir;
            float targetAngle;

            if (alignMovementWithCamera)
            {
                targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
                moveDir = Quaternion.Euler(0f, mainCamera.eulerAngles.y, 0f) * Vector3.forward * vertical +
                          Quaternion.Euler(0f, mainCamera.eulerAngles.y, 0f) * Vector3.right * horizontal;
                moveDir.Normalize();
            }
            else
            {
                targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                moveDir = direction;
            }

            if (cameraTurnInfluence > 0)
            {
                float cameraYaw = mainCamera.eulerAngles.y;
                targetAngle = Mathf.LerpAngle(targetAngle, cameraYaw, cameraTurnInfluence);
            }

            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 finalMoveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float targetSpeed = walkSpeed;

            if (running) targetSpeed = runSpeed;
            if (isSuperSpeedActive) targetSpeed *= superSpeedMultiplier;

            targetSpeed *= direction.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);
            controller.Move(finalMoveDir.normalized * currentSpeed * Time.deltaTime);
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

    void UpdateCameraTarget()
    {
        if (cameraTarget != null)
        {
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
        }
    }

    void ActivateSuperSpeed()
    {
        isSuperSpeedActive = true;
        superSpeedTimeRemaining = speedBoostDuration;

        if (speedTrail != null)
        {
            speedTrail.emitting = true;
        }

        if (speedBoostEffect != null)
        {
            speedBoostEffect.Play();
        }

        if (audioSource != null && speedBoostSound != null && !superSpeedSoundPlayed)
        {
            audioSource.PlayOneShot(speedBoostSound);
            superSpeedSoundPlayed = true;
        }

        Debug.Log("Fasttrack: Super Speed activated!");
    }

    void DeactivateSuperSpeed()
    {
        isSuperSpeedActive = false;
        superSpeedSoundPlayed = false;

        if (speedTrail != null)
        {
            speedTrail.emitting = false;
        }

        if (speedBoostEffect != null)
        {
            speedBoostEffect.Stop();
        }

        speedBoostCooldownRemaining = speedBoostCooldown;

        Debug.Log("Fasttrack: Super Speed deactivated. Cooldown started.");
    }

    IEnumerator PerformDash()
    {
        isDashing = true;

        if (animator != null && System.Array.Exists(animator.parameters, param => param.name == "Dash"))
        {
            animator.SetTrigger(dashHash);
        }

        if (dashEffect != null)
        {
            dashEffect.Play();
        }

        if (audioSource != null && dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }

        Vector3 startPosition = transform.position;
        Vector3 dashDirection = transform.forward;
        Vector3 targetPosition = startPosition + dashDirection * dashDistance;

        RaycastHit hit;
        if (Physics.Raycast(startPosition, dashDirection, out hit, dashDistance, groundMask))
        {
            targetPosition = hit.point - (dashDirection * controller.radius);
        }

        float dashDuration = 0.2f;
        float elapsedTime = 0;

        while (elapsedTime < dashDuration)
        {
            float t = elapsedTime / dashDuration;
            t = t * t * (3f - 2f * t);

            controller.enabled = false;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            controller.enabled = true;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        controller.enabled = false;
        transform.position = targetPosition;
        controller.enabled = true;

        dashCooldownRemaining = dashCooldown;
        isDashing = false;

        Debug.Log("Fasttrack: Dash completed!");
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

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * dashDistance);
    }
}