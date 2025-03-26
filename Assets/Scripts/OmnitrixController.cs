// OmnitrixController.cs - Attach to the main Player GameObject
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OmnitrixController : MonoBehaviour
{
    [System.Serializable]
    public class AlienForm
    {
        public string alienName;
        public GameObject alienModel;
        public CharacterController alienController;
        public MonoBehaviour alienScript;
        public Transform cameraTarget;
        public float transformationCooldown = 30f;
        public KeyCode transformationKey = KeyCode.Alpha1; // Customize per alien (1,2,3,etc)

        [Header("Camera Settings")]
        public float cameraDistance = 5.0f;  // Distance from camera to alien
        public float cameraHeight = 1.5f;    // Height offset for camera

        [HideInInspector] public bool isOnCooldown = false;
        [HideInInspector] public float cooldownRemaining = 0f;
    }

    [Header("Ben Tennyson")]
    public GameObject benModel;
    public CharacterController benController;
    public PlayerController benPlayerController;
    public Transform benCameraTarget;
    public float benCameraDistance = 5.0f;   // Default camera distance for Ben
    public float benCameraHeight = 1.5f;     // Default camera height for Ben

    [Header("Available Aliens")]
    public List<AlienForm> availableAliens = new List<AlienForm>();

    [Header("Transformation Settings")]
    public float transformationDuration = 15f;
    public KeyCode cycleAlienKey = KeyCode.C; // Press to cycle through aliens
    public KeyCode transformKey = KeyCode.T; // Press to transform

    [Header("Camera Settings")]
    public FollowCamera followCamera;  // Reference to your FollowCamera script

    [Header("Effects")]
    public ParticleSystem transformationEffect;
    public AudioClip transformationSound;
    public Light omnitrixFlash;

    // Private variables
    private bool isTransformed = false;
    private float transformationTimeRemaining = 0f;
    private AudioSource audioSource;
    private int currentAlienIndex = -1; // -1 represents being in Ben form
    private int selectedAlienIndex = 0; // Which alien is currently selected (but not transformed)

    void Start()
    {
        // Initialize components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Find camera if not assigned
        if (followCamera == null)
        {
            followCamera = Camera.main.GetComponent<FollowCamera>();
            if (followCamera == null)
            {
                Debug.LogError("Could not find FollowCamera on main camera! Please assign it in the inspector.");
            }
        }

        // Initialize alien models (disable all at start)
        foreach (var alien in availableAliens)
        {
            if (alien.alienModel != null)
            {
                alien.alienModel.SetActive(false);

                // Disable controllers
                if (alien.alienController != null)
                {
                    alien.alienController.enabled = false;
                }

                if (alien.alienScript != null)
                {
                    alien.alienScript.enabled = false;
                }
            }
            else
            {
                Debug.LogError($"Alien {alien.alienName} is missing its model reference!");
            }
        }

        // Enable ben by default
        benModel.SetActive(true);

        // Initialize camera target
        SetCameraTarget(benCameraTarget, benCameraDistance, benCameraHeight);

        Debug.Log("Omnitrix Controller initialized. Press T to transform, C to cycle aliens.");
    }

    void Update()
    {
        // Check for cycling through available aliens (when not transformed)
        if (Input.GetKeyDown(cycleAlienKey) && !isTransformed)
        {
            CycleToNextAlien();
        }

        // Check for transformation input
        if (Input.GetKeyDown(transformKey))
        {
            if (!isTransformed)
            {
                TransformToAlien(selectedAlienIndex);
            }
            else
            {
                RevertToBen();
            }
        }

        // Check for direct transformation keys
        for (int i = 0; i < availableAliens.Count; i++)
        {
            if (i < availableAliens.Count && Input.GetKeyDown(availableAliens[i].transformationKey))
            {
                if (!isTransformed)
                {
                    TransformToAlien(i);
                }
                else if (currentAlienIndex == i)
                {
                    RevertToBen();
                }
                else
                {
                    // Quick switch between aliens (revert first, then transform)
                    StartCoroutine(QuickSwitch(i));
                }
            }
        }

        // Update transformation timer
        if (isTransformed)
        {
            transformationTimeRemaining -= Time.deltaTime;

            if (transformationTimeRemaining <= 0)
            {
                RevertToBen();
            }
        }

        // Update cooldowns
        for (int i = 0; i < availableAliens.Count; i++)
        {
            if (availableAliens[i].isOnCooldown)
            {
                availableAliens[i].cooldownRemaining -= Time.deltaTime;

                if (availableAliens[i].cooldownRemaining <= 0)
                {
                    availableAliens[i].isOnCooldown = false;
                    availableAliens[i].cooldownRemaining = 0;
                    Debug.Log($"{availableAliens[i].alienName} is now available for transformation!");
                }
            }
        }
    }

    void CycleToNextAlien()
    {
        // Cycle to next alien
        selectedAlienIndex = (selectedAlienIndex + 1) % availableAliens.Count;
        Debug.Log($"Selected alien: {availableAliens[selectedAlienIndex].alienName}");

        // Check cooldown status
        if (availableAliens[selectedAlienIndex].isOnCooldown)
        {
            Debug.Log($"{availableAliens[selectedAlienIndex].alienName} is on cooldown: {availableAliens[selectedAlienIndex].cooldownRemaining:F1} seconds remaining");
        }
    }

    void TransformToAlien(int alienIndex)
    {
        // Validate index
        if (alienIndex < 0 || alienIndex >= availableAliens.Count)
        {
            Debug.LogError($"Invalid alien index: {alienIndex}");
            return;
        }

        AlienForm targetAlien = availableAliens[alienIndex];

        // Check cooldown
        if (targetAlien.isOnCooldown)
        {
            Debug.Log($"Cannot transform: {targetAlien.alienName} is on cooldown for {targetAlien.cooldownRemaining:F1} seconds");
            return;
        }

        if (targetAlien.alienModel == null)
        {
            Debug.LogError($"Cannot transform: {targetAlien.alienName} model is not assigned");
            return;
        }

        // Play transformation effects
        PlayTransformationEffects();

        // Start transformation sequence
        StartCoroutine(TransformationSequence(alienIndex));
    }

    void RevertToBen()
    {
        // Play transformation effects
        PlayTransformationEffects();

        // Start transformation back to Ben
        StartCoroutine(TransformationSequence(-1));
    }

    IEnumerator QuickSwitch(int alienIndex)
    {
        // Revert to Ben first (without cooldown)
        bool applyCooldown = false;
        yield return StartCoroutine(TransformationSequence(-1, applyCooldown));

        // Then transform to new alien
        yield return StartCoroutine(TransformationSequence(alienIndex));
    }

    IEnumerator TransformationSequence(int targetAlienIndex, bool applyCooldown = true)
    {
        // Freeze player during transformation
        DisableAllControllers();

        // Get current position for transformation effect
        Vector3 currentPosition = GetCurrentPosition();
        Vector3 currentRotation = GetCurrentRotation();

        // Wait for effect to start
        yield return new WaitForSeconds(0.3f);

        // Disable current model
        DisableCurrentModel();

        // Enable target model
        if (targetAlienIndex == -1)
        {
            // Transform to Ben
            benModel.SetActive(true);
            benModel.transform.position = currentPosition;
            benModel.transform.rotation = Quaternion.Euler(currentRotation);

            // Wait a frame to ensure everything is initialized
            yield return null;

            // Enable controllers
            benController.enabled = true;
            benPlayerController.enabled = true;

            // Switch camera target to Ben with default settings
            SetCameraTarget(benCameraTarget, benCameraDistance, benCameraHeight);

            // Set state
            isTransformed = false;

            // Apply cooldown to previous alien
            if (applyCooldown && currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
            {
                StartAlienCooldown(currentAlienIndex);
            }

            // Reset current alien index
            currentAlienIndex = -1;

            Debug.Log("Reverted to Ben!");
        }
        else
        {
            // Transform to specific alien
            AlienForm targetAlien = availableAliens[targetAlienIndex];

            // Activate model
            targetAlien.alienModel.SetActive(true);
            targetAlien.alienModel.transform.position = currentPosition;
            targetAlien.alienModel.transform.rotation = Quaternion.Euler(currentRotation);

            // Wait a frame to ensure everything is initialized
            yield return null;

            // Enable controllers
            if (targetAlien.alienController != null)
            {
                targetAlien.alienController.enabled = true;
            }

            if (targetAlien.alienScript != null)
            {
                targetAlien.alienScript.enabled = true;
            }

            // Switch camera target with alien-specific camera settings
            SetCameraTarget(
                targetAlien.cameraTarget,
                targetAlien.cameraDistance,
                targetAlien.cameraHeight
            );

            // Update state
            currentAlienIndex = targetAlienIndex;
            isTransformed = true;
            transformationTimeRemaining = transformationDuration;

            Debug.Log($"Transformed into {targetAlien.alienName}!");

            // Trigger standing animation if available
            Animator alienAnimator = targetAlien.alienModel.GetComponent<Animator>();
            if (alienAnimator != null)
            {
                alienAnimator.SetTrigger("Standing");

                // Wait for standing animation to complete
                yield return new WaitForSeconds(1.5f);
            }
        }

        // Wait for effects to complete
        yield return new WaitForSeconds(0.3f);
    }

    // Set camera target with specific settings
    void SetCameraTarget(Transform target, float distance, float height)
    {
        if (followCamera == null || target == null)
        {
            Debug.LogError("Cannot set camera target: Camera or target is null");
            return;
        }

        // Set the target in the camera
        followCamera.target = target;

        // Directly set the distance and height values
        followCamera.followDistance = distance;
        followCamera.heightOffset = height;

        Debug.Log($"Camera set to follow {target.name} at distance {distance} and height {height}");
    }

    void DisableAllControllers()
    {
        // Disable Ben's controllers
        if (benController != null)
            benController.enabled = false;

        if (benPlayerController != null)
            benPlayerController.enabled = false;

        // Disable all alien controllers
        foreach (var alien in availableAliens)
        {
            if (alien.alienController != null)
                alien.alienController.enabled = false;

            if (alien.alienScript != null)
                alien.alienScript.enabled = false;
        }
    }

    void DisableCurrentModel()
    {
        if (currentAlienIndex == -1)
        {
            // Currently Ben
            benModel.SetActive(false);
        }
        else if (currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
        {
            // Currently an alien
            availableAliens[currentAlienIndex].alienModel.SetActive(false);
        }
    }

    Vector3 GetCurrentPosition()
    {
        if (currentAlienIndex == -1)
        {
            // Currently Ben
            return benModel.transform.position;
        }
        else if (currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
        {
            // Currently an alien
            return availableAliens[currentAlienIndex].alienModel.transform.position;
        }

        return transform.position;
    }

    Vector3 GetCurrentRotation()
    {
        if (currentAlienIndex == -1)
        {
            // Currently Ben
            return benModel.transform.eulerAngles;
        }
        else if (currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
        {
            // Currently an alien
            return availableAliens[currentAlienIndex].alienModel.transform.eulerAngles;
        }

        return transform.eulerAngles;
    }

    void StartAlienCooldown(int alienIndex)
    {
        if (alienIndex < 0 || alienIndex >= availableAliens.Count)
            return;

        AlienForm alien = availableAliens[alienIndex];
        alien.isOnCooldown = true;
        alien.cooldownRemaining = alien.transformationCooldown;

        Debug.Log($"{alien.alienName} is now on cooldown for {alien.transformationCooldown} seconds");
    }

    void PlayTransformationEffects()
    {
        // Play particle effect at the current position
        if (transformationEffect != null)
        {
            transformationEffect.transform.position = GetCurrentPosition() + Vector3.up;
            transformationEffect.Play();
        }

        // Play sound effect
        if (audioSource != null && transformationSound != null)
        {
            audioSource.PlayOneShot(transformationSound);
        }

        // Flash omnitrix light
        if (omnitrixFlash != null)
        {
            StartCoroutine(FlashOmnitrix());
        }
    }

    IEnumerator FlashOmnitrix()
    {
        omnitrixFlash.enabled = true;

        // Flash for a short time
        yield return new WaitForSeconds(0.2f);

        omnitrixFlash.enabled = false;
    }

    // Public getter methods for UI and other scripts
    public bool IsTransformed { get { return isTransformed; } }

    public float GetTransformationTimeRemaining() { return transformationTimeRemaining; }

    public float GetTransformationTimePercentage()
    {
        if (!isTransformed) return 0f;
        return transformationTimeRemaining / transformationDuration;
    }

    public string GetCurrentAlienName()
    {
        if (!isTransformed) return "Ben Tennyson";
        if (currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
        {
            return availableAliens[currentAlienIndex].alienName;
        }
        return "Unknown Alien";
    }

    public string GetSelectedAlienName()
    {
        if (selectedAlienIndex >= 0 && selectedAlienIndex < availableAliens.Count)
        {
            return availableAliens[selectedAlienIndex].alienName;
        }
        return "Unknown Alien";
    }

    public bool IsAlienOnCooldown(int alienIndex)
    {
        if (alienIndex < 0 || alienIndex >= availableAliens.Count)
            return false;

        return availableAliens[alienIndex].isOnCooldown;
    }

    public float GetAlienCooldownPercentage(int alienIndex)
    {
        if (alienIndex < 0 || alienIndex >= availableAliens.Count)
            return 0f;

        if (!availableAliens[alienIndex].isOnCooldown)
            return 0f;

        return availableAliens[alienIndex].cooldownRemaining / availableAliens[alienIndex].transformationCooldown;
    }

    // --- ADDED HELPER METHODS FOR ALIEN WHEEL INTEGRATION ---

    /// <summary>
    /// Public method to get the current selected alien index
    /// </summary>
    public int GetSelectedAlienIndex()
    {
        return selectedAlienIndex;
    }

    /// <summary>
    /// Public method to handle transform button press
    /// </summary>
    public void TransformPressed()
    {
        if (!isTransformed)
        {
            TransformToAlien(selectedAlienIndex);
        }
        else
        {
            RevertToBen();
        }
    }

    /// <summary>
    /// Public method to cycle to the next alien
    /// Used by AlienWheelOmnitrixBridge
    /// </summary>
    public void PublicCycleToNextAlien()
    {
        // Call the private implementation
        if (!isTransformed)
        {
            CycleToNextAlien();
        }
    }
}