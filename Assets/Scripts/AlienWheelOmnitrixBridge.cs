using UnityEngine;

/// <summary>
/// Connects the Alien Selection Wheel UI to your OmnitrixController
/// </summary>
public class AlienWheelOmnitrixBridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to your OmnitrixController script")]
    public OmnitrixController omnitrixController;

    [Tooltip("Reference to your AlienWheelController script")]
    public AlienWheelController alienWheelController;

    [Header("Settings")]
    [Tooltip("Should the wheel close after selecting an alien?")]
    public bool closeWheelAfterSelection = true;

    [Header("Debug")]
    [Tooltip("Enable detailed debug logs")]
    public bool debugMode = true;

    // Track the last selected ID to avoid repeated processing
    private int lastProcessedId = 0;

    void Start()
    {
        // Find references if not set
        if (omnitrixController == null)
        {
            omnitrixController = FindAnyObjectByType<OmnitrixController>();
            if (omnitrixController == null)
            {
                Debug.LogError("No OmnitrixController found in scene! Please assign it in the inspector.");
            }
        }

        if (alienWheelController == null)
        {
            alienWheelController = FindAnyObjectByType<AlienWheelController>();
            if (alienWheelController == null)
            {
                Debug.LogError("No AlienWheelController found! Please assign it in the inspector.");
            }
        }

        // Initialize with no selection
        AlienWheelController.alienId = 0;
        lastProcessedId = 0;

        if (debugMode)
        {
            Debug.Log("AlienWheelOmnitrixBridge initialized.");
            Debug.Log($"Number of available aliens: {omnitrixController.availableAliens.Count}");
            for (int i = 0; i < omnitrixController.availableAliens.Count; i++)
            {
                Debug.Log($"Alien {i}: {omnitrixController.availableAliens[i].alienName}");
            }
        }
    }

    void Update()
    {
        // Check if an alien was selected from the wheel
        int selectedAlienId = AlienWheelController.alienId;

        // Only process if the ID changed and is not zero
        if (selectedAlienId > 0 && selectedAlienId != lastProcessedId)
        {
            if (debugMode)
            {
                Debug.Log($"New alien selected from wheel: ID = {selectedAlienId}");
            }

            // Convert the wheel's alien ID to your OmnitrixController's index
            int omnitrixAlienIndex = selectedAlienId - 1;

            if (debugMode)
            {
                Debug.Log($"Converting wheel ID {selectedAlienId} to Omnitrix index {omnitrixAlienIndex}");
            }

            // Make sure the index is valid
            if (omnitrixAlienIndex >= 0 && omnitrixAlienIndex < omnitrixController.availableAliens.Count)
            {
                if (debugMode)
                {
                    string alienName = omnitrixController.availableAliens[omnitrixAlienIndex].alienName;
                    Debug.Log($"Attempting to transform into: {alienName} (index {omnitrixAlienIndex})");
                }

                // Use the direct SendMessage method to cycle to and select the alien
                StartCoroutine(SelectAndTransform(omnitrixAlienIndex));

                // Close the wheel if configured to do so
                if (closeWheelAfterSelection && alienWheelController != null)
                {
                    if (debugMode)
                    {
                        Debug.Log("Closing alien wheel...");
                    }

                    alienWheelController.alienWheelSelected = false;
                    alienWheelController.anim.SetBool("openAlienWheel", false);
                    alienWheelController.CloseWheel();
                }
            }
            else
            {
                Debug.LogError($"Invalid alien index: {omnitrixAlienIndex}. Available range: 0-{omnitrixController.availableAliens.Count - 1}");
            }

            // Update the last processed ID
            lastProcessedId = selectedAlienId;

            // Reset the selection to avoid continuous transformation
            AlienWheelController.alienId = 0;
        }
    }

    /// <summary>
    /// Coroutine to select the alien and transform
    /// </summary>
    private System.Collections.IEnumerator SelectAndTransform(int targetIndex)
    {
        if (debugMode)
        {
            Debug.Log($"Starting alien selection process for index {targetIndex}");
        }

        // Get current selected index
        int currentIndex = omnitrixController.GetSelectedAlienIndex();

        if (debugMode)
        {
            Debug.Log($"Current selected index: {currentIndex}, Target index: {targetIndex}");
        }

        // First we need to cycle to the correct alien
        if (currentIndex != targetIndex)
        {
            if (debugMode)
            {
                Debug.Log("Cycling to target alien...");
            }

            // Direct method - manually cycle through aliens until we get to the right one
            while (omnitrixController.GetSelectedAlienIndex() != targetIndex)
            {
                omnitrixController.PublicCycleToNextAlien();

                if (debugMode)
                {
                    Debug.Log($"Cycled to alien: {omnitrixController.GetSelectedAlienName()}");
                }

                yield return null;
            }
        }

        if (debugMode)
        {
            Debug.Log($"Target alien selected: {omnitrixController.GetSelectedAlienName()}");
            Debug.Log("Triggering transformation...");
        }

        // Now trigger the transformation
        omnitrixController.TransformPressed();

        // Force hide cursor after transformation
        yield return new WaitForSeconds(0.5f);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (debugMode)
        {
            Debug.Log("Transformation sequence completed.");
        }
    }
}