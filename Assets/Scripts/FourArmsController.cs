using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class FourArmsController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10.0f;
    public float jumpForce = 15.0f;
    public float gravity = -20.0f;
    public float turnSmoothTime = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask = -1;

    [Header("Jump Settings")]
    public float jumpBufferTime = 0.5f;

    [Header("Combat Settings")]
    public float lightAttackCooldown = 0.3f;
    public float heavyAttackCooldown = 0.8f;
    public float specialAttackCooldown = 1.2f;
    public float comboAttackCooldown = 1.5f;
    public LayerMask enemyLayers;
    public float attackRadius = 2.0f;
    public float attackDamage = 10f;
    public float heavyDamageMultiplier = 2.0f;
    public float specialDamageMultiplier = 3.0f;

    [Header("Combat Keys")]
    public KeyCode lightAttackKey = KeyCode.Z;
    public KeyCode heavyAttackKey = KeyCode.X;
    public KeyCode specialAttackKey = KeyCode.Q;
    public KeyCode comboAttackKey = KeyCode.E;

    [Header("Debug Controls")]
    public KeyCode directTriggerCombo1 = KeyCode.Alpha1;  // Press 1 to directly trigger ComboAttack1
    public KeyCode directTriggerCombo2 = KeyCode.Alpha2;  // Press 2 to directly trigger ComboAttack2
    public bool verboseDebug = true;                      // Show detailed debug info

    // Private variables
    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform cameraTransform;

    // Jump state tracking
    private bool isJumping = false;
    private float jumpStateTimer = 0f;

    // Combat state tracking
    private bool isAttacking = false;
    private float lightAttackTimer = 0f;
    private float heavyAttackTimer = 0f;
    private float specialAttackTimer = 0f;
    private float comboAttackTimer = 0f;
    private int comboCounter = 0;
    private float comboResetTimer = 0f;
    private int lastComboUsed = 0;  // 0=none, 1=Combo1, 2=Combo2

    // Animation state monitoring
    private bool wasInAttackAnimation = false;
    private float attackTimeRemaining = 0f;

    private void Start()
    {
        // Get component references
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cameraTransform = Camera.main.transform;

        if (animator == null)
        {
            Debug.LogError("No Animator component found on FourArms!");
        }
        else if (verboseDebug)
        {
            // Log all animator parameters
            Debug.Log("===== FourArms Animator Parameters =====");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"- {param.name} ({param.type})");
            }
        }

        // Create ground check if not assigned
        if (groundCheck == null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.parent = transform;
            checkObj.transform.localPosition = new Vector3(0, -2.0f, 0);
            groundCheck = checkObj.transform;
        }

        // If no ground mask is assigned, default to everything
        if (groundMask.value == 0)
        {
            groundMask = ~0; // Everything
        }

        // Ensure controller is enabled
        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    private void Update()
    {
        // Ensure controller is enabled
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        UpdateTimers();
        CheckGroundedMultiMethod();
        UpdateAnimationStates();

        // Process inputs only if not in attack animation
        if (!isAttacking)
        {
            HandleMovement();
            HandleJumping();
            HandleCombat();
            HandleDebugControls();
        }

        // These always run
        ApplyGravity();
        UpdateLocomotionAnimation();
    }

    private void HandleDebugControls()
    {
        // Direct combo attack triggers for testing
        if (Input.GetKeyDown(directTriggerCombo1) && !isAttacking)
        {
            if (verboseDebug) Debug.Log("Directly triggering ComboAttack1");
            TriggerSpecificAnimation("ComboAttack1", comboAttackCooldown);
        }
        else if (Input.GetKeyDown(directTriggerCombo2) && !isAttacking)
        {
            if (verboseDebug) Debug.Log("Directly triggering ComboAttack2");
            TriggerSpecificAnimation("ComboAttack2", comboAttackCooldown);
        }
    }

    private void UpdateTimers()
    {
        // Update attack cooldowns
        if (lightAttackTimer > 0) lightAttackTimer -= Time.deltaTime;
        if (heavyAttackTimer > 0) heavyAttackTimer -= Time.deltaTime;
        if (specialAttackTimer > 0) specialAttackTimer -= Time.deltaTime;
        if (comboAttackTimer > 0) comboAttackTimer -= Time.deltaTime;

        // Update jump state
        if (isJumping)
        {
            jumpStateTimer -= Time.deltaTime;
            if (jumpStateTimer <= 0)
            {
                isJumping = false;
            }
        }

        // Update combo counter
        if (comboCounter > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0)
            {
                comboCounter = 0;
                if (verboseDebug) Debug.Log("Combo counter reset");
            }
        }

        // Update attack state timer
        if (attackTimeRemaining > 0)
        {
            attackTimeRemaining -= Time.deltaTime;
            if (attackTimeRemaining <= 0)
            {
                isAttacking = false;
            }
        }
    }

    private void CheckGroundedMultiMethod()
    {
        // If we're in an enforced jump state, don't check for ground yet
        if (isJumping)
        {
            isGrounded = false;
            return;
        }

        // Multiple ground detection methods for reliability
        bool sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        bool rayGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance * 1.2f, groundMask);
        bool controllerGrounded = controller.isGrounded;

        // Consider grounded if at least 2 methods agree
        int groundedCount = (sphereGrounded ? 1 : 0) + (rayGrounded ? 1 : 0) + (controllerGrounded ? 1 : 0);
        isGrounded = groundedCount >= 2;

        // Reset velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void UpdateAnimationStates()
    {
        if (animator != null)
        {
            // Update grounded state in animator
            animator.SetBool("Grounded", isGrounded);

            // Monitor if we're in an attack animation state
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Check if we're in any attack animation by name
            bool inAttackAnim = stateInfo.IsName("LightAttack1") || stateInfo.IsName("LightAttack2") ||
                               stateInfo.IsName("HeavyAttack1") || stateInfo.IsName("HeavyAttack2") ||
                               stateInfo.IsName("SpecialAttack1") || stateInfo.IsName("SpecialAttack2") ||
                               stateInfo.IsName("ComboAttack1") || stateInfo.IsName("ComboAttack2");

            if (inAttackAnim)
            {
                // Entering attack animation
                if (!wasInAttackAnimation)
                {
                    isAttacking = true;

                    // Calculate remaining time based on animation length and normalized time
                    float animLength = stateInfo.length;
                    float normalizedTime = stateInfo.normalizedTime;
                    attackTimeRemaining = animLength * (1f - normalizedTime);

                    if (verboseDebug)
                    {
                        Debug.Log($"Attack animation started: {stateInfo.fullPathHash}, duration: {animLength}s, remaining: {attackTimeRemaining}s");
                    }
                }
            }
            else if (wasInAttackAnimation)
            {
                // Exiting attack animation
                isAttacking = false;
                if (verboseDebug)
                {
                    Debug.Log("Attack animation ended");
                }
            }

            // Update tracking flag
            wasInAttackAnimation = inAttackAnim;
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Apply movement with a very small downward component to help with slopes
            Vector3 moveVector = moveDir.normalized * moveSpeed;
            moveVector.y = -0.5f; // Small downward force to keep grounded

            // Execute the move command
            if (controller.enabled)
            {
                controller.Move(moveVector * Time.deltaTime);
            }
        }
    }

    private void HandleJumping()
    {
        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) && isGrounded && !isJumping)
        {
            // Calculate jump velocity based on physics formula
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            // Set jumping state for minimum duration
            isJumping = true;
            jumpStateTimer = jumpBufferTime;
            isGrounded = false; // Immediately set to not grounded

            // Trigger jump animation
            if (animator != null)
            {
                animator.SetTrigger("Jump");
                animator.SetBool("Grounded", false); // Force to false
            }
        }
    }

    private void HandleCombat()
    {
        // Only allow attacks when grounded
        if (!isGrounded) return;

        // Handle light attacks
        if (Input.GetKeyDown(lightAttackKey) && lightAttackTimer <= 0)
        {
            // Alternate between light attack 1 and 2
            string attackName = (comboCounter % 2 == 0) ? "LightAttack1" : "LightAttack2";
            TriggerAttack(attackName, lightAttackCooldown, attackDamage);
        }

        // Handle heavy attacks
        else if (Input.GetKeyDown(heavyAttackKey) && heavyAttackTimer <= 0)
        {
            // Alternate between heavy attack 1 and 2
            string attackName = (comboCounter % 2 == 0) ? "HeavyAttack1" : "HeavyAttack2";
            TriggerAttack(attackName, heavyAttackCooldown, attackDamage * heavyDamageMultiplier);
        }

        // Handle special attacks
        else if (Input.GetKeyDown(specialAttackKey) && specialAttackTimer <= 0)
        {
            // Alternate between special attack 1 and 2
            string attackName = (comboCounter % 2 == 0) ? "SpecialAttack1" : "SpecialAttack2";
            TriggerAttack(attackName, specialAttackCooldown, attackDamage * specialDamageMultiplier);
        }

        // Handle combo attacks (requires combo counter >= 2)
        else if (Input.GetKeyDown(comboAttackKey) && comboAttackTimer <= 0 && comboCounter >= 2)
        {
            // Always alternate combo attacks
            string attackName = (lastComboUsed != 1) ? "ComboAttack1" : "ComboAttack2";
            lastComboUsed = (lastComboUsed != 1) ? 1 : 2;

            TriggerAttack(attackName, comboAttackCooldown, attackDamage * 4f);

            // Reset combo counter after using combo attack
            comboCounter = 0;
        }
    }

    private void TriggerAttack(string attackName, float cooldown, float damage)
    {
        if (animator == null) return;

        // Check if this parameter exists
        bool parameterExists = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == attackName && param.type == AnimatorControllerParameterType.Trigger)
            {
                parameterExists = true;
                break;
            }
        }

        if (!parameterExists)
        {
            Debug.LogError($"Attack parameter '{attackName}' not found in animator!");
            return;
        }

        // Trigger the animation
        animator.SetTrigger(attackName);
        Debug.Log($"Triggered {attackName} animation");

        // Set attack state and cooldown
        isAttacking = true;

        // Set specific cooldown based on attack type
        if (attackName.Contains("Light"))
        {
            lightAttackTimer = cooldown;
        }
        else if (attackName.Contains("Heavy"))
        {
            heavyAttackTimer = cooldown;
        }
        else if (attackName.Contains("Special"))
        {
            specialAttackTimer = cooldown;
        }
        else if (attackName.Contains("Combo"))
        {
            comboAttackTimer = cooldown;
        }

        // Estimate animation length based on type
        float estimatedLength = 0.7f;
        if (attackName.Contains("Heavy") || attackName.Contains("Special"))
        {
            estimatedLength = 1.0f;
        }
        else if (attackName.Contains("Combo"))
        {
            estimatedLength = 1.5f;
        }

        // Set attack time remaining and apply damage
        attackTimeRemaining = estimatedLength;
        StartCoroutine(ApplyDamageAfterDelay(damage, estimatedLength * 0.5f));

        // Update combo counter for regular attacks (not combo attacks)
        if (!attackName.Contains("Combo"))
        {
            comboCounter++;
            comboResetTimer = 2.0f;
            if (verboseDebug) Debug.Log($"Combo counter: {comboCounter}");
        }
    }

    // Direct trigger for specific animations (used by debug controls)
    private void TriggerSpecificAnimation(string animationName, float cooldown)
    {
        if (animator == null) return;

        // Directly trigger the animation 
        animator.SetTrigger(animationName);
        Debug.Log($"DIRECT TRIGGER: {animationName}");

        // Set attack state and cooldown
        isAttacking = true;
        attackTimeRemaining = 1.5f; // Longer time for combo attacks

        if (animationName.Contains("Combo"))
        {
            comboAttackTimer = cooldown;
            lastComboUsed = animationName.Contains("1") ? 1 : 2;
        }

        // Apply damage
        StartCoroutine(ApplyDamageAfterDelay(attackDamage * 4f, 0.7f));
    }

    private IEnumerator ApplyDamageAfterDelay(float damage, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Find enemies in attack radius
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, attackRadius, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            if (verboseDebug) Debug.Log($"Hit enemy: {enemy.name} with {damage} damage");

            // Apply damage if enemy has health component
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            // Apply force to physics objects
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(transform.forward * damage * 10f, ForceMode.Impulse);
            }
        }
    }

    private void ApplyGravity()
    {
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Limit terminal velocity
        if (velocity.y < -30f)
            velocity.y = -30f;

        // Apply vertical movement
        if (controller.enabled)
        {
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
        }
    }

    private void UpdateLocomotionAnimation()
    {
        if (animator != null)
        {
            // Get movement input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            float speed = direction.magnitude;

            // Update speed parameter
            animator.SetFloat("Speed", speed);
        }
    }

    // Called when character takes damage
    public void TakeDamage(float amount)
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }

        Debug.Log($"FourArms took {amount} damage!");
    }

    // Visual debugging
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            // Ground check visualization
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        // Attack range visualization
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1.5f, attackRadius);
    }
}