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
    public float jumpForce = 15.0f;
    public float gravity = -25.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 1.0f;
    public LayerMask groundMask = -1;

    [Header("Speed Abilities")]
    public KeyCode superSpeedKey = KeyCode.LeftShift;
    public KeyCode slowMotionKey = KeyCode.F;
    public float slowMotionFactor = 0.2f;

    [Header("Visual Effects")]
    // Mesh trail system
    public bool useMeshTrails = true;
    public float meshTrailActiveTime = 2f;
    public float meshRefreshRate = 0.03f;
    public float meshDestroyDelay = 3f;
    public Transform meshTrailSpawnPosition;
    public string meshTrailShaderVarRef = "_Alpha"; // Common dissolve parameter name
    public float meshTrailShaderVarRate = 0.1f;
    public float meshTrailShaderVarRefreshRate = 0.05f;
    public Material meshTrailMaterial;

    public float speedThreshold = 8.0f;

    [Header("Post-Processing")]
    public MonoBehaviour postProcessVolume;
    public ScriptableObject normalProfile;
    public ScriptableObject speedProfile;
    public string profileFieldName = "profile";

    [Header("Audio")]
    public AudioClip speedBoostSound;
    public AudioClip slowMotionActivateSound;
    public AudioClip slowMotionDeactivateSound;
    public AudioClip slowMotionLoopSound;

    [Header("Camera Settings")]
    public Transform cameraTarget;
    public float cameraTurnInfluence = 0.7f;
    public bool alignMovementWithCamera = true;

    [Header("Debug")]
    public bool showDebugVisuals = true;

    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform mainCamera;
    private AudioSource audioSource;
    private AudioSource slowMoAudioSource;

    private bool isSuperSpeedActive = false;
    private bool isSlowMotionActive = false;
    private bool superSpeedSoundPlayed = false;
    private float originalFixedDeltaTime;

    // Mesh Trail variables
    private bool isMeshTrailActive = false;
    private Coroutine activeMeshTrailCoroutine;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

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
            groundCheck.localPosition = new Vector3(0, -1.5f, 0);
            Debug.Log("Created ground check at -1.5 height");
        }

        // Set ground mask to Everything if not specified
        if (groundMask.value == 0)
        {
            groundMask = ~0; // Everything
            Debug.Log("Ground mask was 0, set to Everything layer");
        }

        // Cache skinned mesh renderers for mesh trails
        if (useMeshTrails)
        {
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderers.Length == 0)
            {
                Debug.LogWarning("No SkinnedMeshRenderers found for mesh trails. Check character hierarchy.");
            }

            // Set default spawn position if not specified
            if (meshTrailSpawnPosition == null)
            {
                meshTrailSpawnPosition = transform;
            }
        }

        // Debug info at startup
        Debug.Log($"Character controller: height={controller.height}, radius={controller.radius}, center={controller.center}");
        Debug.Log($"Ground check at: {groundCheck.position}, using mask: {groundMask.value}");
    }

    void Update()
    {
        if (controller == null || !controller.enabled)
            return;

        // Check ground status using multiple methods
        CheckGroundedMultiMethod();

        // Show debug visuals
        if (showDebugVisuals)
        {
            Debug.DrawRay(groundCheck.position, Vector3.down * groundDistance, isGrounded ? Color.green : Color.red);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"SPACE pressed! isGrounded={isGrounded}, velocity.y={velocity.y}");
            }
        }

        UpdateAnimator();
        HandleSlowMotion();
        HandleSuperSpeed();
        ProcessMovement();
        HandleJumping();
        ApplyGravity();
        UpdateCameraTarget();
        UpdateVisualEffects();
    }

    void CheckGroundedMultiMethod()
    {
        // Method 1: Original sphere cast
        bool sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Method 2: Raycast down
        bool rayGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance * 1.5f, groundMask);

        // Method 3: Character controller's built-in check
        bool controllerGrounded = controller.isGrounded;

        // Combine methods for maximum reliability
        isGrounded = sphereGrounded || rayGrounded || controllerGrounded;

        // Reset velocity when grounded
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

    // Modify the ProcessMovement method by removing the speed boost in slow motion:

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
                isSlowMotionActive ? Time.unscaledDeltaTime : Time.deltaTime
            );

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 finalMoveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float targetSpeed = walkSpeed;

            // Apply super speed multiplier if active
            if (isSuperSpeedActive)
            {
                targetSpeed *= superSpeedMultiplier;
            }

            // REMOVE THIS SPEED BOOST TO ALLOW TRUE SLOW MOTION:
            // if (isSlowMotionActive) targetSpeed *= (1f / slowMotionFactor);

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
            // Just set the speed parameter directly without compensation for slow motion
            animator.SetFloat(speedHash, currentSpeed);

            // Don't adjust animator speed in slow motion - let it actually run slow
            animator.speed = 1.0f; // Same speed in both normal and slow motion
        }
    }

    void HandleJumping()
    {
        // Use both input methods for better detection
        bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);

        if (jumpPressed && isGrounded)
        {
            // Calculate jump velocity with a slight boost
            float jumpVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            velocity.y = jumpVelocity;

            Debug.Log($"FASTTRACK JUMP! Velocity={jumpVelocity}");

            if (animator != null)
            {
                animator.SetTrigger(jumpHash);
            }

            // Force grounded state update
            isGrounded = false;
        }
    }

    void ApplyGravity()
    {
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Limit terminal velocity
        if (velocity.y < -30f)
            velocity.y = -30f;

        // Apply vertical movement
        Vector3 verticalMovement = new Vector3(0, velocity.y, 0) * Time.deltaTime;
        controller.Move(verticalMovement);

        // Debug visualize
        if (showDebugVisuals && Mathf.Abs(velocity.y) > 2)
        {
            Debug.DrawRay(transform.position, Vector3.up * velocity.y * 0.1f,
                velocity.y > 0 ? Color.green : Color.red);
        }
    }

    void UpdateCameraTarget()
    {
        if (cameraTarget != null)
        {
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
        }
    }

    void UpdateVisualEffects()
    {
        // Check if we should show trail effects based on character state
        bool shouldShowTrails = isSuperSpeedActive || currentSpeed > speedThreshold || isSlowMotionActive;

        // Handle mesh trails if enabled
        if (useMeshTrails && shouldShowTrails && !isMeshTrailActive)
        {
            if (activeMeshTrailCoroutine != null)
            {
                StopCoroutine(activeMeshTrailCoroutine);
            }
            activeMeshTrailCoroutine = StartCoroutine(ActivateMeshTrail(meshTrailActiveTime));
        }
        else if (useMeshTrails && !shouldShowTrails && isMeshTrailActive)
        {
            if (activeMeshTrailCoroutine != null)
            {
                StopCoroutine(activeMeshTrailCoroutine);
                isMeshTrailActive = false;
            }
        }
    }

    IEnumerator ActivateMeshTrail(float timeActive)
    {
        isMeshTrailActive = true;

        // Cache skinnedMeshRenderers if not already cached
        if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0)
        {
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderers.Length == 0)
            {
                Debug.LogWarning("No SkinnedMeshRenderers found for mesh trails.");
                isMeshTrailActive = false;
                yield break;
            }
        }

        // Check if we have the required material
        if (meshTrailMaterial == null)
        {
            Debug.LogWarning("Mesh trail material is not assigned.");
            isMeshTrailActive = false;
            yield break;
        }

        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            // Create mesh copies for each skinned mesh renderer
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                if (skinnedMeshRenderers[i] == null) continue;

                // Create game object for the trail mesh
                GameObject trailMeshObject = new GameObject($"TrailMesh_{skinnedMeshRenderers[i].name}");

                // Position and rotation setup
                trailMeshObject.transform.SetPositionAndRotation(
                    meshTrailSpawnPosition != null ? meshTrailSpawnPosition.position : transform.position,
                    meshTrailSpawnPosition != null ? meshTrailSpawnPosition.rotation : transform.rotation
                );

                // Apply rotation correction to make trails vertical behind the character
                trailMeshObject.transform.Rotate(-90, 0, 180);

                // Add mesh components
                MeshRenderer meshRenderer = trailMeshObject.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = trailMeshObject.AddComponent<MeshFilter>();

                // Disable shadows for better performance
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;

                // Create a new mesh and bake the skinned mesh to it
                Mesh bakeMesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(bakeMesh);
                meshFilter.mesh = bakeMesh;

                // Set material
                meshRenderer.material = new Material(meshTrailMaterial);

                // Start dissolve animation coroutine
                StartCoroutine(AnimateMaterialFloat(
                    meshRenderer.material,
                    0,
                    meshTrailShaderVarRate,
                    meshTrailShaderVarRefreshRate
                ));

                // Schedule destruction
                Destroy(trailMeshObject, meshDestroyDelay);
            }

            // Wait for next refresh cycle
            yield return new WaitForSeconds(
                isSlowMotionActive ? meshRefreshRate * slowMotionFactor : meshRefreshRate
            );
        }

        isMeshTrailActive = false;
    }

    IEnumerator AnimateMaterialFloat(Material material, float goal, float rate, float refreshRate)
    {
        if (string.IsNullOrEmpty(meshTrailShaderVarRef))
        {
            Debug.LogWarning("Shader variable reference name is empty.");
            yield break;
        }

        if (!material.HasProperty(meshTrailShaderVarRef))
        {
            Debug.LogWarning($"Shader variable '{meshTrailShaderVarRef}' not found in trail material.");
            yield break;
        }

        float valueToAnimate = material.GetFloat(meshTrailShaderVarRef);

        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            material.SetFloat(meshTrailShaderVarRef, valueToAnimate);

            // Use scaled or unscaled time based on slow motion state
            yield return new WaitForSeconds(
                isSlowMotionActive ? refreshRate * slowMotionFactor : refreshRate
            );
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

        Debug.Log("Fasttrack: Super Speed activated!");
    }

    void DeactivateSuperSpeed()
    {
        isSuperSpeedActive = false;
        superSpeedSoundPlayed = false;

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

        // Clean up mesh trails when deactivating
        if (!active && useMeshTrails && isMeshTrailActive)
        {
            if (activeMeshTrailCoroutine != null)
            {
                StopCoroutine(activeMeshTrailCoroutine);
                isMeshTrailActive = false;
            }
        }

        if (slowMoAudioSource != null) slowMoAudioSource.Stop();
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            // Always show ground check
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);

            // Show raycast
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundDistance * 1.5f);
        }

        // Show mesh trail spawn position if assigned
        if (meshTrailSpawnPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(meshTrailSpawnPosition.position, 0.2f);
            Gizmos.DrawRay(meshTrailSpawnPosition.position, meshTrailSpawnPosition.forward * 0.5f);
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