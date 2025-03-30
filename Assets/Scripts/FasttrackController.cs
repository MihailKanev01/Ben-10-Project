using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FasttrackController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 7.0f;
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
    public KeyCode superSpeedKey = KeyCode.LeftShift;  // Key to activate super speed (default: Shift)

    [Header("Slow Motion Settings")]
    public KeyCode slowMotionKey = KeyCode.F;   // Keybind for toggling slow motion
    public float slowMotionFactor = 0.2f;       // How slow everything else becomes (0.2 = 20% normal speed)

    [Header("Visual Effects")]
    public List<TrailRenderer> speedTrails = new List<TrailRenderer>();   // Trail renderers to use
    public float speedThreshold = 8.0f;         // Speed at which trails start to appear

    [Header("Post-Processing")]
    public MonoBehaviour postProcessVolume; // Reference to your Post Processing Volume component
    public ScriptableObject normalProfile;  // Your normal profile asset
    public ScriptableObject speedProfile;   // Your speed profile asset
    public string profileFieldName = "profile"; // The name of the field to set (usually "profile")

    [Header("Audio")]
    public AudioClip speedBoostSound;
    public AudioClip slowMotionActivateSound;   // Sound played when slow motion starts
    public AudioClip slowMotionDeactivateSound; // Sound played when slow motion ends
    public AudioClip slowMotionLoopSound;       // Ambient sound during slow motion

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
    private AudioSource slowMoAudioSource;      // Separate audio source for slow-mo effects

    private bool isSuperSpeedActive = false;
    private bool isSlowMotionActive = false;
    private bool superSpeedSoundPlayed = false;
    private float originalFixedDeltaTime;

    private int speedHash;
    private int jumpHash;
    private int groundedHash;
    private int superSpeedHash;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;
        audioSource = GetComponent<AudioSource>();
        originalFixedDeltaTime = Time.fixedDeltaTime;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Create a separate audio source for slow-mo effects
        slowMoAudioSource = gameObject.AddComponent<AudioSource>();
        slowMoAudioSource.loop = true;
        slowMoAudioSource.volume = 0.5f;
        slowMoAudioSource.spatialBlend = 0f; // Pure 2D sound

        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
            superSpeedHash = Animator.StringToHash("SuperSpeed");
        }

        if (groundCheck == null)
        {
            groundCheck = new GameObject("FasttrackGroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
        }

        // For safety, disable all trails initially
        foreach (TrailRenderer trail in speedTrails)
        {
            if (trail != null)
                trail.emitting = false;
        }
    }

    void Update()
    {
        if (controller == null || !controller.enabled)
            return;

        CheckGrounded();
        UpdateAnimator();
        HandleSlowMotion();          // Slow motion handling
        HandleSuperSpeed();          // Super speed handling
        ProcessMovement();
        HandleJumping();
        ApplyGravity();
        UpdateCameraTarget();
        UpdateTrailEffects();        // Update trail effects based on speed
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
            // Check if parameters exist before setting them
            if (HasParameter(animator, "SuperSpeed"))
            {
                animator.SetBool(superSpeedHash, isSuperSpeedActive);
            }

            if (HasParameter(animator, "Grounded"))
            {
                animator.SetBool(groundedHash, isGrounded);
            }

            // No need to set SlowMotion parameter since we don't have it
        }
    }

    void HandleSuperSpeed()
    {
        bool superSpeedKeyPressed = Input.GetKey(superSpeedKey);

        // Simply activate/deactivate super speed based on key press
        if (superSpeedKeyPressed && !isSuperSpeedActive)
        {
            ActivateSuperSpeed();
        }
        else if (!superSpeedKeyPressed && isSuperSpeedActive)
        {
            DeactivateSuperSpeed();
        }
    }

    // Method to handle slow motion as a toggle
    void HandleSlowMotion()
    {
        if (Input.GetKeyDown(slowMotionKey))
        {
            // Toggle slow motion on/off
            if (isSlowMotionActive)
            {
                DeactivateSlowMotion();
            }
            else
            {
                ActivateSlowMotion();
            }
        }
    }

    // Update your ProcessMovement method in the FasttrackController script:

    void ProcessMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
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

            // Adjust rotation smoothing based on time scale
            float actualTurnSmoothTime = turnSmoothTime;
            if (isSlowMotionActive)
            {
                // Make rotation much faster during slow motion
                actualTurnSmoothTime = turnSmoothTime * 0.1f;
            }

            // Use angle smoothing with adjusted smooth time
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                actualTurnSmoothTime,
                Mathf.Infinity,
                isSlowMotionActive ? Time.unscaledDeltaTime : Time.deltaTime  // Use unscaledDeltaTime in slow motion
            );

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 finalMoveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float targetSpeed = walkSpeed;

            // Apply super speed multiplier if active
            if (isSuperSpeedActive)
            {
                targetSpeed *= superSpeedMultiplier;
            }

            // Additional speed boost during slow motion to make Fasttrack appear faster
            if (isSlowMotionActive) targetSpeed *= (1f / slowMotionFactor);

            targetSpeed *= direction.magnitude;

            // Use different smoothing for speed based on time scale
            if (isSlowMotionActive)
            {
                // Use a much smaller smooth time for quicker response in slow motion
                currentSpeed = Mathf.SmoothDamp(
                    currentSpeed,
                    targetSpeed,
                    ref speedSmoothVelocity,
                    speedSmoothTime * 0.1f,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime
                );
            }
            else
            {
                // Normal behavior when not in slow motion
                currentSpeed = Mathf.SmoothDamp(
                    currentSpeed,
                    targetSpeed,
                    ref speedSmoothVelocity,
                    speedSmoothTime
                );
            }

            controller.Move(finalMoveDir.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            // Handle stopping in slow motion too
            if (isSlowMotionActive)
            {
                currentSpeed = Mathf.SmoothDamp(
                    currentSpeed,
                    0,
                    ref speedSmoothVelocity,
                    speedSmoothTime * 0.1f,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime
                );
            }
            else
            {
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0, ref speedSmoothVelocity, speedSmoothTime);
            }
        }

        if (animator != null)
        {
            // When in slow motion, compensate the animator speed
            if (isSlowMotionActive)
            {
                animator.SetFloat(speedHash, currentSpeed * slowMotionFactor);
                animator.speed = 1f / slowMotionFactor; // Make animations run at normal speed
            }
            else
            {
                animator.SetFloat(speedHash, currentSpeed);
                animator.speed = 1f; // Normal animation speed
            }
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

        if (audioSource != null && speedBoostSound != null && !superSpeedSoundPlayed)
        {
            audioSource.PlayOneShot(speedBoostSound);
            superSpeedSoundPlayed = true;
        }

        // REMOVED: No longer change post-processing profile for super speed

        Debug.Log("Fasttrack: Super Speed activated!");
    }

    void DeactivateSuperSpeed()
    {
        isSuperSpeedActive = false;
        superSpeedSoundPlayed = false;

        // REMOVED: No longer restore post-processing profile for super speed

        Debug.Log("Fasttrack: Super Speed deactivated.");
    }

    void ActivateSlowMotion()
    {
        isSlowMotionActive = true;

        // Slow down game time
        Time.timeScale = slowMotionFactor;
        Time.fixedDeltaTime = originalFixedDeltaTime * slowMotionFactor;

        // Play slow motion sound
        if (audioSource != null && slowMotionActivateSound != null)
        {
            audioSource.PlayOneShot(slowMotionActivateSound);
        }

        // Play slow motion loop sound
        if (slowMoAudioSource != null && slowMotionLoopSound != null)
        {
            slowMoAudioSource.clip = slowMotionLoopSound;
            slowMoAudioSource.Play();
        }

        // Apply post-processing profile for slow motion
        if (postProcessVolume != null && speedProfile != null)
        {
            SetPostProcessingProfile(speedProfile);
        }

        Debug.Log("Fasttrack: Slow Motion activated!");
    }

    void DeactivateSlowMotion()
    {
        isSlowMotionActive = false;

        // Restore normal game time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        // Play deactivation sound
        if (audioSource != null && slowMotionDeactivateSound != null)
        {
            audioSource.PlayOneShot(slowMotionDeactivateSound);
        }

        // Stop loop sound
        if (slowMoAudioSource != null)
        {
            slowMoAudioSource.Stop();
        }

        // Restore normal post-processing profile
        if (postProcessVolume != null && normalProfile != null)
        {
            SetPostProcessingProfile(normalProfile);
        }

        Debug.Log("Fasttrack: Slow Motion deactivated.");
    }

    void UpdateTrailEffects()
    {
        // Direct trail control - just turn them on/off based on current state
        foreach (TrailRenderer trail in speedTrails)
        {
            if (trail != null)
            {
                // Simple condition: super speed OR above threshold OR slow motion
                bool shouldEmit = isSuperSpeedActive || currentSpeed > speedThreshold || isSlowMotionActive;

                // Set the emission directly
                trail.emitting = shouldEmit;
            }
        }
    }

    public void SetControllerActive(bool active)
    {
        this.enabled = active;
        if (controller != null)
        {
            controller.enabled = active;
        }

        // If deactivating, make sure to reset time scale
        if (!active && isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
            isSlowMotionActive = false;
        }

        // Directly disable all trails
        foreach (TrailRenderer trail in speedTrails)
        {
            if (trail != null)
                trail.emitting = false;
        }

        if (slowMoAudioSource != null) slowMoAudioSource.Stop();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
    }

    // For safety, ensure time scale is reset if script is disabled
    void OnDisable()
    {
        if (isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }

    // Helper method to check if an animator has a parameter
    bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    // Helper method to set the post-processing profile using reflection
    // This works with any version of Post Processing Stack
    void SetPostProcessingProfile(ScriptableObject profile)
    {
        if (postProcessVolume == null || profile == null)
            return;

        System.Type volumeType = postProcessVolume.GetType();
        System.Reflection.FieldInfo fieldInfo = volumeType.GetField(profileFieldName);

        if (fieldInfo != null)
        {
            fieldInfo.SetValue(postProcessVolume, profile);
        }
        else
        {
            System.Reflection.PropertyInfo propertyInfo = volumeType.GetProperty(profileFieldName);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(postProcessVolume, profile);
            }
            else
            {
                Debug.LogWarning("Could not find a profile field or property on the post-processing volume");
            }
        }
    }
}