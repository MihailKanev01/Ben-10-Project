using UnityEngine;
using System.Collections;

public class JetrayController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float flySpeed = 14.0f;
    public float turnSmoothTime = 0.1f;
    public float speedSmoothTime = 0.1f;

    [Header("Flight Settings")]
    public float verticalSpeed = 8.0f;
    public KeyCode flyToggleKey = KeyCode.Space;
    public float bankingFactor = 30.0f;     // How much Jetray banks into turns
    public float flyingPitchFactor = 30.0f; // How much Jetray pitches forward when flying

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Visual Effects")]
    public TrailRenderer[] wingTrails;
    public ParticleSystem flyEffect;
    public Light jetrayGlow;

    [Header("Audio")]
    public AudioClip flySound;

    [Header("Camera Settings")]
    public Transform cameraTarget;
    public float cameraFlyHeight = 2.0f;
    public float cameraGroundHeight = 1.5f;

    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 velocity;
    private Transform mainCamera;
    private AudioSource audioSource;

    private bool isFlying = false;
    private bool isGrounded = true;
    private float verticalVelocity = 0f;
    private float gravity = -15.0f;

    // Animation parameter hashes
    private int speedHash;
    private int flyingHash;
    private int groundedHash;

    void Start()
    {
        InitializeComponents();
        SetupAnimation();
        SetupEffects(false);
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
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 20.0f;
        }

        if (groundCheck == null)
        {
            groundCheck = new GameObject("JetrayGroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
        }

        if (jetrayGlow != null)
        {
            jetrayGlow.intensity = 0.5f;
        }
    }

    void SetupAnimation()
    {
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            flyingHash = Animator.StringToHash("Flying");
            groundedHash = Animator.StringToHash("Grounded");
        }
    }

    void SetupEffects(bool flying)
    {
        // Wing trails
        if (wingTrails != null)
        {
            foreach (var trail in wingTrails)
            {
                if (trail != null)
                {
                    trail.emitting = flying;
                }
            }
        }

        // Particle effects
        if (flyEffect != null)
        {
            if (flying) flyEffect.Play();
            else flyEffect.Stop();
        }

        // Glow effect
        if (jetrayGlow != null)
        {
            jetrayGlow.intensity = flying ? 1.5f : 0.5f;
        }
    }

    void Update()
    {
        if (controller == null || !controller.enabled)
            return;

        CheckGroundState();
        HandleFlightToggle();

        if (isFlying)
        {
            HandleFlightMovement();
        }
        else
        {
            HandleGroundMovement();
        }

        UpdateAnimator();
        UpdateCameraTarget();
    }

    void CheckGroundState()
    {
        if (!isFlying)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
    }

    void HandleFlightToggle()
    {
        if (Input.GetKeyDown(flyToggleKey))
        {
            ToggleFlight();
        }
    }

    void ToggleFlight()
    {
        isFlying = !isFlying;

        // Play sound effect
        if (audioSource != null && flySound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(flySound);
        }

        // Adjust controller properties for flight
        if (isFlying)
        {
            // Initial upward and forward impulse for a dynamic takeoff
            velocity.y = 4f;

            // If we're moving, take off in that direction
            if (currentSpeed > 0.5f)
            {
                // Create a forward impulse based on current speed
                StartCoroutine(SmoothTakeoff());
            }

            controller.slopeLimit = 90f; // Allow moving up steep surfaces when flying
            controller.stepOffset = 0f; // No step offset in flight mode
        }
        else
        {
            controller.slopeLimit = 45f; // Normal ground slope limit
            controller.stepOffset = 0.3f; // Normal step offset for ground movement
        }

        // Set up visual effects
        SetupEffects(isFlying);

        // Update animator
        if (animator != null)
        {
            animator.SetBool(flyingHash, isFlying);
        }
    }

    IEnumerator SmoothTakeoff()
    {
        float takeoffTime = 0.5f;
        float elapsedTime = 0;
        Vector3 initialForward = transform.forward;
        float takeoffSpeed = currentSpeed * 1.5f; // Boost initial takeoff speed

        while (elapsedTime < takeoffTime)
        {
            controller.Move(initialForward * takeoffSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    void HandleGroundMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float targetSpeed = walkSpeed * direction.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleFlightMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float upDown = 0f;

        // Calculate roll angle based on turning (banking into turns)
        float rollAngle = 0;
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            rollAngle = -horizontal * bankingFactor;
        }

        // Vertical movement for flight
        if (Input.GetKey(KeyCode.E))
        {
            upDown = 1f;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            upDown = -1f;
        }

        // Get movement direction from input
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Calculate base pitch from vertical input
        float basePitch = -upDown * 30f; // Max pitch angle from up/down controls

        // Get forward movement intensity (0 to 1)
        float forwardIntensity = Mathf.Clamp01(vertical);

        // Calculate dynamic forward lean based on forward movement
        float forwardLeanAngle = forwardIntensity * 30f; // Lean forward up to 30 degrees when moving forward

        // Combined pitch: vertical controls + forward lean
        float targetPitch = basePitch;
        if (vertical > 0)
        {
            targetPitch -= forwardLeanAngle; // Add forward tilt when moving forward
        }

        // Apply smooth rotation
        Vector3 currentEuler = transform.eulerAngles;
        float smoothPitch = Mathf.LerpAngle(currentEuler.x, targetPitch, Time.deltaTime * 3f);

        if (direction.magnitude >= 0.1f)
        {
            // Rotate in flight direction
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            // Apply rotation with pitch and roll (banking into turns)
            transform.rotation = Quaternion.Euler(smoothPitch, angle, rollAngle);

            // Move in the direction you're facing
            Vector3 moveDir = transform.forward;
            float targetSpeed = flySpeed * direction.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

            // Apply movement - add a slight downward movement for realism when pitched forward
            Vector3 moveVector = moveDir * currentSpeed;
            if (smoothPitch < -5f)
            {
                // Add subtle downward drift when pitched forward to make flight feel more realistic
                moveVector += Vector3.down * Mathf.Abs(smoothPitch) * 0.01f;
            }

            controller.Move(moveVector * Time.deltaTime);
        }
        else
        {
            // Just adjust pitch and slowly reduce speed if no horizontal input
            // Gradually return to level flight (no roll) when not turning
            float currentRoll = transform.eulerAngles.z;
            if (currentRoll > 180) currentRoll -= 360; // Normalize to -180 to 180
            float targetRoll = 0;
            float smoothRoll = Mathf.LerpAngle(currentRoll, targetRoll, Time.deltaTime * 3f);

            transform.rotation = Quaternion.Euler(smoothPitch, transform.eulerAngles.y, smoothRoll);
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime * 2);
        }

        // Vertical movement
        verticalVelocity = upDown * verticalSpeed;
        controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetBool(groundedHash, isGrounded);
            animator.SetFloat(speedHash, currentSpeed);
            animator.SetBool(flyingHash, isFlying);
        }
    }

    void UpdateCameraTarget()
    {
        if (cameraTarget != null)
        {
            float targetHeight = isFlying ? cameraFlyHeight : cameraGroundHeight;
            Vector3 targetPosition = new Vector3(
                transform.position.x,
                transform.position.y + targetHeight,
                transform.position.z
            );

            // Smoothly move the camera target
            cameraTarget.position = Vector3.Lerp(
                cameraTarget.position,
                targetPosition,
                Time.deltaTime * 5f
            );
        }
    }

    public void SetControllerActive(bool active)
    {
        this.enabled = active;
        if (controller != null)
        {
            controller.enabled = active;
        }

        // Disable flight and effects when deactivated
        if (!active && isFlying)
        {
            isFlying = false;
            SetupEffects(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}