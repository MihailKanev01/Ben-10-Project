using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class AlienCameraManager : MonoBehaviour
{
    // Dictionary to store cameras for each alien type
    private Dictionary<string, CinemachineCamera> alienCameras = new Dictionary<string, CinemachineCamera>();

    // Reference to currently active camera
    private CinemachineCamera currentActiveCamera;

    // Camera naming convention
    [SerializeField] private string cameraPrefix = "CM";

    // Default camera name
    [SerializeField] private string defaultCameraName = "CMBen";

    // Blend time for smooth transitions
    [SerializeField] private float blendTime = 1.0f;

    private void Awake()
    {
        // Find and register all alien cameras
        RegisterAlienCameras();

        // Set default camera as active initially
        ActivateDefaultCamera();
    }

    private void RegisterAlienCameras()
    {
        // Find all Cinemachine cameras in the scene - using the new FindObjectsByType method
        CinemachineCamera[] allCameras = GameObject.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);

        foreach (CinemachineCamera camera in allCameras)
        {
            // Get the character name from the camera object name
            string name = camera.gameObject.name;

            if (name.StartsWith(cameraPrefix))
            {
                // Remove the prefix to get the character name
                string characterName = name.Substring(cameraPrefix.Length);

                // Add to dictionary and initially disable all cameras
                alienCameras.Add(characterName, camera);
                camera.Priority = 0; // Lower priority to disable

                Debug.Log($"Found camera for: {characterName}");
            }
        }
    }

    private void ActivateDefaultCamera()
    {
        // Find the default camera (usually Ben's camera)
        CinemachineCamera defaultCamera = null;

        foreach (var pair in alienCameras)
        {
            if (pair.Value.gameObject.name == defaultCameraName)
            {
                defaultCamera = pair.Value;
                break;
            }
        }

        // If we found it, activate it
        if (defaultCamera != null)
        {
            currentActiveCamera = defaultCamera;
            defaultCamera.Priority = 10; // Higher priority to enable
            Debug.Log($"Activated default camera: {defaultCameraName}");
        }
        else
        {
            Debug.LogWarning($"Default camera {defaultCameraName} not found!");
        }
    }

    // Call this method when transforming into a new alien
    public void SwitchCamera(string alienName)
    {
        // Handle special case for human form
        if (alienName == "Ben10" || alienName == "Ben" || alienName == "Human")
        {
            // Switch to Ben's camera
            SwitchToCamera("Ben");
            return;
        }

        SwitchToCamera(alienName);
    }

    // Switch to a specific camera by character name
    public void SwitchToCamera(string characterName)
    {
        Debug.Log($"Attempting to switch to camera for: {characterName}");

        // Set blend time on the brain if needed
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            // Modern way to set blend time
            var defaultBlend = brain.DefaultBlend;
            defaultBlend.Time = blendTime;
            brain.DefaultBlend = defaultBlend;
        }

        // Disable current camera
        if (currentActiveCamera != null)
        {
            currentActiveCamera.Priority = 0;
        }

        // Check if we have a camera for this character
        CinemachineCamera newCamera = null;

        // Try exact match first
        if (alienCameras.TryGetValue(characterName, out newCamera))
        {
            newCamera.Priority = 10;
            currentActiveCamera = newCamera;
            Debug.Log($"Switched camera to {characterName}");
            return;
        }

        // If not found, try case-insensitive comparison
        foreach (var pair in alienCameras)
        {
            if (pair.Key.ToLower() == characterName.ToLower())
            {
                pair.Value.Priority = 10;
                currentActiveCamera = pair.Value;
                Debug.Log($"Switched to camera: {pair.Key} (case-insensitive match)");
                return;
            }
        }

        Debug.LogWarning($"No camera found for: {characterName}");
    }

    // For debugging - list all available cameras
    [ContextMenu("List Available Cameras")]
    public void ListAvailableCameras()
    {
        Debug.Log("=== Available Cameras ===");
        foreach (var pair in alienCameras)
        {
            Debug.Log($"Character: {pair.Key}, Camera: {pair.Value.gameObject.name}");
        }
    }
}