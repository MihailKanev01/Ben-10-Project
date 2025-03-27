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

    void RevertToBen()
    {
        PlayTransformationEffects();
        StartCoroutine(TransformationSequence(-1));
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
        alien.cooldownRemaining = alien.transformationCooldown;
    }

    void PlayTransformationEffects()
    {
        if (transformationEffect != null)
        {
            transformationEffect.transform.position = GetCurrentPosition() + Vector3.up;
            transformationEffect.Play();
        }

        if (audioSource != null && transformationSound != null)
        {
            audioSource.PlayOneShot(transformationSound);
        }

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

    public int GetSelectedAlienIndex()
    {
        return selectedAlienIndex;
    }

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