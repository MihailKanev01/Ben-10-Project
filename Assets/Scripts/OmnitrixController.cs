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
       // public float transformationCooldown = 30f;
        public KeyCode transformationKey = KeyCode.Alpha1;

        [Header("Camera Settings")]
        public float cameraDistance = 5.0f;
        public float cameraHeight = 1.5f;

        [HideInInspector] public bool isOnCooldown = false;
        [HideInInspector] public float cooldownRemaining = 0f;
    }

    [Header("Ben Tennyson")]
    public GameObject benModel;
    public CharacterController benController;
    public PlayerController benPlayerController;
    public Transform benCameraTarget;
    public float benCameraDistance = 5.0f;
    public float benCameraHeight = 1.5f;

    [Header("Available Aliens")]
    public List<AlienForm> availableAliens = new List<AlienForm>();

    [Header("Transformation Settings")]
    public float transformationDuration = 15f;
    public KeyCode cycleAlienKey = KeyCode.C;
    public KeyCode transformKey = KeyCode.T;

    [Header("Camera Settings")]
    public FollowCamera followCamera;
    public ThirdPersonCamera thirdPersonCamera;

    [Header("Effects")]
    public ParticleSystem transformationEffect;
    public AudioClip transformationSound;
    public Light omnitrixFlash;

    [Header("Green Flash Effects")]
    public ParticleSystem greenFlashParticleSystem;
    public float flashOffset = 1.0f;
    public Light greenFlashLight;
    public float lightIntensity = 8f;
    public float lightDuration = 0.5f;
    public Camera mainCamera;
    public bool addCameraShake = true;
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.3f;

    [Header("Screen Flash")]
    public UnityEngine.UI.Image screenFlashImage;
    public float screenFlashDuration = 0.3f;
    public float maxScreenFlashAlpha = 0.5f;

    private bool isTransformed = false;
    private float transformationTimeRemaining = 0f;
    private AudioSource audioSource;
    private int currentAlienIndex = -1;
    private int selectedAlienIndex = 0;

    void Start()
    {
        InitializeComponents();
        InitializeAliens();
        SetupDefaultState();
    }

    void InitializeComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (followCamera == null && thirdPersonCamera == null)
        {
            followCamera = Camera.main.GetComponent<FollowCamera>();
            thirdPersonCamera = Camera.main.GetComponent<ThirdPersonCamera>();

            if (followCamera == null && thirdPersonCamera == null)
            {
                Debug.LogError("Could not find camera controller on main camera! Please assign it in the inspector.");
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Set up screen flash image if one was provided
        if (screenFlashImage != null)
        {
            screenFlashImage.gameObject.SetActive(false);
            screenFlashImage.color = new Color(0, 1, 0, 0); // Start transparent
        }
    }

    void InitializeAliens()
    {
        foreach (var alien in availableAliens)
        {
            if (alien.alienModel != null)
            {
                alien.alienModel.SetActive(false);

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
    }

    void SetupDefaultState()
    {
        benModel.SetActive(true);
        SetCameraTarget(benCameraTarget, benCameraDistance, benCameraHeight);
    }

    void Update()
    {
        HandleAlienSelection();
        HandleTransformationInput();
        UpdateTransformationTimer();
        UpdateAlienCooldowns();
    }

    void HandleAlienSelection()
    {
        if (Input.GetKeyDown(cycleAlienKey) && !isTransformed)
        {
            CycleToNextAlien();
        }
    }

    void HandleTransformationInput()
    {
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
                    StartCoroutine(QuickSwitch(i));
                }
            }
        }
    }

    void UpdateTransformationTimer()
    {
        if (isTransformed)
        {
            transformationTimeRemaining -= Time.deltaTime;

            if (transformationTimeRemaining <= 0)
            {
                RevertToBen();
            }
        }
    }

    void UpdateAlienCooldowns()
    {
        for (int i = 0; i < availableAliens.Count; i++)
        {
            if (availableAliens[i].isOnCooldown)
            {
                availableAliens[i].cooldownRemaining -= Time.deltaTime;

                if (availableAliens[i].cooldownRemaining <= 0)
                {
                    availableAliens[i].isOnCooldown = false;
                    availableAliens[i].cooldownRemaining = 0;
                }
            }
        }
    }

    void CycleToNextAlien()
    {
        selectedAlienIndex = (selectedAlienIndex + 1) % availableAliens.Count;
    }

    void TransformToAlien(int alienIndex)
    {
        if (alienIndex < 0 || alienIndex >= availableAliens.Count)
            return;

        AlienForm targetAlien = availableAliens[alienIndex];

        if (targetAlien.isOnCooldown)
            return;

        if (targetAlien.alienModel == null)
            return;

        PlayTransformationEffects();
        StartCoroutine(TransformationSequence(alienIndex));
    }

    public void RevertToBen()
    {
        if (isTransformed)
        {
            PlayTransformationEffects();
            StartCoroutine(TransformationSequence(-1));
        }
    }

    IEnumerator QuickSwitch(int alienIndex)
    {
        bool applyCooldown = false;
        yield return StartCoroutine(TransformationSequence(-1, applyCooldown));
        yield return StartCoroutine(TransformationSequence(alienIndex));
    }

    IEnumerator TransformationSequence(int targetAlienIndex, bool applyCooldown = true)
    {
        DisableAllControllers();

        Vector3 currentPosition = GetCurrentPosition();
        Vector3 currentRotation = GetCurrentRotation();

        yield return new WaitForSeconds(0.3f);

        DisableCurrentModel();

        if (targetAlienIndex == -1)
        {
            benModel.SetActive(true);
            benModel.transform.position = currentPosition;
            benModel.transform.rotation = Quaternion.Euler(currentRotation);

            yield return null;

            benController.enabled = true;
            benPlayerController.enabled = true;

            SetCameraTarget(benCameraTarget, benCameraDistance, benCameraHeight);

            isTransformed = false;

            if (applyCooldown && currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
            {
                StartAlienCooldown(currentAlienIndex);
            }

            currentAlienIndex = -1;
        }
        else
        {
            AlienForm targetAlien = availableAliens[targetAlienIndex];

            targetAlien.alienModel.SetActive(true);
            targetAlien.alienModel.transform.position = currentPosition;
            targetAlien.alienModel.transform.rotation = Quaternion.Euler(currentRotation);

            yield return null;

            if (targetAlien.alienController != null)
            {
                targetAlien.alienController.enabled = true;
            }

            if (targetAlien.alienScript != null)
            {
                targetAlien.alienScript.enabled = true;
            }

            SetCameraTarget(
                targetAlien.cameraTarget,
                targetAlien.cameraDistance,
                targetAlien.cameraHeight
            );

            currentAlienIndex = targetAlienIndex;
            isTransformed = true;
            transformationTimeRemaining = transformationDuration;
        }

        yield return new WaitForSeconds(0.3f);
    }

    void SetCameraTarget(Transform target, float distance, float height)
    {
        if (target == null)
            return;

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.SetTarget(target);

            if (distance <= 3.5f)
                thirdPersonCamera.SetCameraMode(ThirdPersonCamera.CameraMode.Close);
            else if (distance >= 7.0f)
                thirdPersonCamera.SetCameraMode(ThirdPersonCamera.CameraMode.Far);
            else
                thirdPersonCamera.SetCameraMode(ThirdPersonCamera.CameraMode.Medium);

            thirdPersonCamera.ResetRotation();
        }
        else if (followCamera != null)
        {
            followCamera.target = target;
            followCamera.followDistance = distance;
            followCamera.heightOffset = height;
        }
    }

    void DisableAllControllers()
    {
        if (benController != null)
            benController.enabled = false;

        if (benPlayerController != null)
            benPlayerController.enabled = false;

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
            benModel.SetActive(false);
        }
        else if (currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
        {
            availableAliens[currentAlienIndex].alienModel.SetActive(false);
        }
    }

    Vector3 GetCurrentPosition()
    {
        if (currentAlienIndex == -1)
        {
            return benModel.transform.position;
        }
        else if (currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
        {
            return availableAliens[currentAlienIndex].alienModel.transform.position;
        }

        return transform.position;
    }

    Vector3 GetCurrentRotation()
    {
        if (currentAlienIndex == -1)
        {
            return benModel.transform.eulerAngles;
        }
        else if (currentAlienIndex >= 0 && currentAlienIndex < availableAliens.Count)
        {
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
       // alien.cooldownRemaining = alien.transformationCooldown;
    }

    void PlayTransformationEffects()
    {
        // Get current position
        Vector3 currentPosition = GetCurrentPosition();

        // Add height offset
        Vector3 flashPosition = currentPosition + Vector3.up * flashOffset;

        // Play original transformation effect if available
        if (transformationEffect != null)
        {
            transformationEffect.transform.position = flashPosition;
            transformationEffect.Play();
        }

        // Play sound effect
        if (audioSource != null && transformationSound != null)
        {
            audioSource.PlayOneShot(transformationSound);
        }

        // Play green flash particle system
        if (greenFlashParticleSystem != null)
        {
            greenFlashParticleSystem.transform.position = flashPosition;
            greenFlashParticleSystem.Play();

            // Activate all child particle systems
            foreach (ParticleSystem childSystem in greenFlashParticleSystem.GetComponentsInChildren<ParticleSystem>())
            {
                if (childSystem != greenFlashParticleSystem)
                    childSystem.Play();
            }
        }

        // Camera shake effect
        if (addCameraShake && mainCamera != null)
        {
            StartCoroutine(ShakeCamera(shakeDuration, shakeIntensity));
        }

        // Screen flash effect
        if (screenFlashImage != null)
        {
            StartCoroutine(ScreenFlash(screenFlashDuration));
        }

        // Green flash light effect
        if (greenFlashLight != null)
        {
            StartCoroutine(FlashLightEffect(flashPosition));
        }

        // Original omnitrix flash
        if (omnitrixFlash != null)
        {
            StartCoroutine(FlashOmnitrix());
        }
    }

    IEnumerator FlashOmnitrix()
    {
        omnitrixFlash.enabled = true;
        yield return new WaitForSeconds(0.2f);
        omnitrixFlash.enabled = false;
    }

    IEnumerator FlashLightEffect(Vector3 position)
    {
        greenFlashLight.transform.position = position;
        greenFlashLight.color = new Color(0.0f, 1.0f, 0.0f);
        greenFlashLight.intensity = 0;
        greenFlashLight.enabled = true;

        // Ramp up
        float timer = 0;
        float rampUpDuration = lightDuration * 0.3f;
        while (timer < rampUpDuration)
        {
            timer += Time.deltaTime;
            greenFlashLight.intensity = Mathf.Lerp(0, lightIntensity, timer / rampUpDuration);
            yield return null;
        }

        // Hold at peak
        yield return new WaitForSeconds(lightDuration * 0.4f);

        // Ramp down
        timer = 0;
        float rampDownDuration = lightDuration * 0.3f;
        while (timer < rampDownDuration)
        {
            timer += Time.deltaTime;
            greenFlashLight.intensity = Mathf.Lerp(lightIntensity, 0, timer / rampDownDuration);
            yield return null;
        }

        greenFlashLight.enabled = false;
    }

    IEnumerator ShakeCamera(float duration, float magnitude)
    {
        Vector3 originalPosition = mainCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            mainCamera.transform.localPosition = new Vector3(x, y, originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = originalPosition;
    }

    IEnumerator ScreenFlash(float duration)
    {
        screenFlashImage.gameObject.SetActive(true);
        Color flashColor = new Color(0, 1, 0, 0); // Start transparent
        screenFlashImage.color = flashColor;

        // Fade in
        float elapsed = 0;
        float fadeInTime = duration * 0.3f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0, maxScreenFlashAlpha, elapsed / fadeInTime);
            screenFlashImage.color = new Color(0, 1, 0, alpha);
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(duration * 0.4f);

        // Fade out
        elapsed = 0;
        float fadeOutTime = duration * 0.3f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(maxScreenFlashAlpha, 0, elapsed / fadeOutTime);
            screenFlashImage.color = new Color(0, 1, 0, alpha);
            yield return null;
        }

        screenFlashImage.gameObject.SetActive(false);
    }

    public bool IsTransformed { get { return isTransformed; } }

    public float GetTransformationTimeRemaining() { return transformationTimeRemaining; }

    //public float GetTransformationTimePercentage()
    //{
    //    if (!isTransformed) return 0f;
    //    return transformationTimeRemaining / transformationDuration;
    //}

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

    public int GetSelectedAlienIndex()
    {
        return selectedAlienIndex;
    }

    public bool IsAlienOnCooldown(int alienIndex)
    {
        if (alienIndex < 0 || alienIndex >= availableAliens.Count)
            return false;

        return availableAliens[alienIndex].isOnCooldown;
    }

    //public float GetAlienCooldownPercentage(int alienIndex)
    //{
    //    if (alienIndex < 0 || alienIndex >= availableAliens.Count)
    //        return 0f;

    //    if (!availableAliens[alienIndex].isOnCooldown)
    //        return 0f;

    //    return availableAliens[alienIndex].cooldownRemaining / availableAliens[alienIndex].transformationCooldown;
    //}

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

    public void PublicCycleToNextAlien()
    {
        if (!isTransformed)
        {
            CycleToNextAlien();
        }
    }
}