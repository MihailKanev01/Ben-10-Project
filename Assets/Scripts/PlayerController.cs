using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 6.0f;
    public float turnSmoothTime = 0.1f;
    public float speedSmoothTime = 0.1f;

    [Header("Jump Settings")]
    public float jumpForce = 8.0f;
    public Vector3 jumpDirection = new Vector3(0.0f, 1.0f, 0.0f);
    public float gravity = -15.0f;
    public bool debugJump = true;
    public GameObject jumpEffectPrefab;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

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

    private int speedHash;
    private int jumpHash;
    private int groundedHash;

    void Start()
    {
        InitializeComponents();
        SetupInputSettings();
        SetupGroundCheck();
    }

    void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = new Vector3(0, 1f, 0);
            controller.radius = 0.5f;
        }

        animator = GetComponent<Animator>();
        mainCamera = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SetupInputSettings()
    {
        if (animator != null)
        {
            speedHash = Animator.StringToHash("Speed");
            jumpHash = Animator.StringToHash("Jump");
            groundedHash = Animator.StringToHash("Grounded");
        }

        jumpDirection = new Vector3(0.0f, 1.0f, 0.0f).normalized;
    }

    void SetupGroundCheck()
    {
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.parent = transform;
            groundCheck.localPosition = new Vector3(0, -0.9f, 0);
        }

        if (groundMask.value == 0)
        {
            groundMask = 1 << LayerMask.NameToLayer("Default");
        }
    }

    void Update()
    {
        CheckGroundState();
        HandleMovement();
        HandleJumping();
        ApplyGravity();
        UpdateCameraTarget();
    }

    void CheckGroundState()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (debugJump && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Space pressed! isGrounded: {isGrounded}, velocity.y: {velocity.y}");
        }

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
        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            float jumpVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            velocity.y = jumpVelocity;

            if (jumpEffectPrefab != null)
            {
                Instantiate(jumpEffectPrefab, groundCheck.position, Quaternion.identity);
            }

            if (debugJump)
            {
                Debug.Log($"JUMP INITIATED! Jump velocity: {jumpVelocity}");
                Debug.DrawRay(transform.position, Vector3.up * jumpVelocity * 0.1f, Color.green, 1.0f);
            }

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

        if (debugJump)
        {
            Debug.DrawRay(transform.position, Vector3.up * velocity.y * 0.05f,
                velocity.y > 0 ? Color.green : Color.red, Time.deltaTime);

            //if (Mathf.Abs(velocity.y) > 1.0f)
            //{
            //    Debug.Log($"Y velocity: {velocity.y:F2}");
            //}
        }
    }

    void UpdateCameraTarget()
    {
        if (cameraTarget != null)
        {
            cameraTarget.position = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
        }
        //else if (debugJump)
        //{
        //    Debug.LogWarning("Camera target is not assigned!");
        //}
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
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, jumpDirection * jumpForce * 0.25f);
    }
}