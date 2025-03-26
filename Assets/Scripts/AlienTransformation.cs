using UnityEngine;

/// <summary>
/// Handles the player's transformation into different alien forms
/// </summary>
public class AlienTransformation : MonoBehaviour
{
    [System.Serializable]
    public class AlienForm
    {
        public string name;
        public GameObject modelPrefab;
        public RuntimeAnimatorController animatorController;
        public float moveSpeed = 5f;
        public float jumpForce = 10f;
        public bool canFly = false;
        public bool canSwim = false;
        // Add any other alien-specific abilities or properties here
    }

    [Header("Alien Forms")]
    [Tooltip("Define all available alien forms here")]
    public AlienForm[] availableAlienForms;

    [Header("Transform Settings")]
    [Tooltip("Transform effect prefab to play during transformation")]
    public GameObject transformEffectPrefab;

    [Tooltip("Duration of the transformation effect")]
    public float transformDuration = 1.0f;

    [Tooltip("Sound played during transformation")]
    public AudioClip transformSound;

    [Header("Runtime")]
    [SerializeField, Tooltip("Currently active alien form (0 = human)")]
    private int currentAlienFormId = 0;

    // References
    private Animator animator;
    private AudioSource audioSource;
    private GameObject currentAlienModel;

    void Start()
    {
        // Get component references
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Initialize as human form
        currentAlienFormId = 0;
    }

    /// <summary>
    /// Transform the player into the specified alien form
    /// </summary>
    /// <param name="alienId">ID of the alien form to transform into (0 = human)</param>
    public void TransformIntoAlien(int alienId)
    {
        // Validate alien ID
        if (alienId < 0 || alienId > availableAlienForms.Length)
        {
            Debug.LogWarning($"Invalid alien ID: {alienId}");
            return;
        }

        // Skip if already in this form
        if (alienId == currentAlienFormId)
        {
            return;
        }

        // Play transformation effect
        PlayTransformationEffect();

        // Store the new alien ID
        currentAlienFormId = alienId;

        if (alienId == 0)
        {
            // Transform back to human form
            TransformToHuman();
        }
        else
        {
            // Transform to alien form (subtract 1 because array is 0-based, but human = 0)
            TransformToAlienForm(availableAlienForms[alienId - 1]);
        }
    }

    /// <summary>
    /// Transform back to human form
    /// </summary>
    private void TransformToHuman()
    {
        // Remove current alien model if exists
        if (currentAlienModel != null)
        {
            Destroy(currentAlienModel);
            currentAlienModel = null;
        }

        // Reset to human animator controller
        if (animator != null)
        {
            // Set your human animator controller here
            // animator.runtimeAnimatorController = humanAnimatorController;
        }

        // Reset player properties (speed, abilities, etc.)
        // playerMovement.moveSpeed = humanMoveSpeed;
        // playerMovement.jumpForce = humanJumpForce;

        Debug.Log("Transformed back to human form");
    }

    /// <summary>
    /// Transform to a specific alien form
    /// </summary>
    /// <param name="alienForm">Alien form data to transform into</param>
    private void TransformToAlienForm(AlienForm alienForm)
    {
        // Remove current alien model if exists
        if (currentAlienModel != null)
        {
            Destroy(currentAlienModel);
        }

        // Instantiate new alien model
        if (alienForm.modelPrefab != null)
        {
            currentAlienModel = Instantiate(alienForm.modelPrefab, transform);
            currentAlienModel.transform.localPosition = Vector3.zero;
            currentAlienModel.transform.localRotation = Quaternion.identity;
        }

        // Set animator controller
        if (animator != null && alienForm.animatorController != null)
        {
            animator.runtimeAnimatorController = alienForm.animatorController;
        }

        // Update player properties based on alien abilities
        // Example:
        // playerMovement.moveSpeed = alienForm.moveSpeed;
        // playerMovement.jumpForce = alienForm.jumpForce;
        // playerMovement.canFly = alienForm.canFly;
        // playerMovement.canSwim = alienForm.canSwim;

        Debug.Log($"Transformed into alien: {alienForm.name}");
    }

    /// <summary>
    /// Play visual and audio effects during transformation
    /// </summary>
    private void PlayTransformationEffect()
    {
        // Play transformation sound
        if (audioSource != null && transformSound != null)
        {
            audioSource.PlayOneShot(transformSound);
        }

        // Instantiate transform effect prefab
        if (transformEffectPrefab != null)
        {
            GameObject effect = Instantiate(transformEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, transformDuration);
        }

        // You could also add screen flash, time slow, or other effects here
    }
}