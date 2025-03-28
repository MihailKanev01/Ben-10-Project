using UnityEngine;
using System.Collections;
using UnityEngine.UI;

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

    // Track when transformation is in progress
    private bool transformationInProgress = false;

    // Used to detect actual Ben button click
    private int previousAlienId = -1;
    private bool wheelJustOpened = false;

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
        transformationInProgress = false;
        previousAlienId = -1;

        // Disable Ben button in human form
        UpdateBenButtonState();

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
        // Check if wheel state changed
        if (!wheelJustOpened && alienWheelController.alienWheelSelected)
        {
            wheelJustOpened = true;
            UpdateBenButtonState();
            previousAlienId = 0; // Reset when wheel opens
        }
        else if (wheelJustOpened && !alienWheelController.alienWheelSelected)
        {
            wheelJustOpened = false;
        }

        // Skip processing if a transformation is in progress
        if (transformationInProgress)
            return;

        // Get the current selection from the wheel
        int selectedAlienId = AlienWheelController.alienId;

        // Only process when alien ID changes
        if (selectedAlienId != previousAlienId)
        {
            if (debugMode)
                Debug.Log($"Alien ID changed from {previousAlienId} to {selectedAlienId}");

            // Process Ben button selection (revert to human)
            if (selectedAlienId == 0 && previousAlienId > 0 && omnitrixController.IsTransformed)
            {
                if (debugMode)
                    Debug.Log("Detected Ben button click, reverting to human form");

                // Revert to Ben
                transformationInProgress = true;
                StartCoroutine(RevertToBenCoroutine());

                // Close wheel
                if (closeWheelAfterSelection && alienWheelController != null)
                {
                    alienWheelController.alienWheelSelected = false;
                    alienWheelController.anim.SetBool("openAlienWheel", false);
                    alienWheelController.CloseWheel();
                }
            }
            // Process alien selection (transform)
            else if (selectedAlienId > 0 && selectedAlienId != lastProcessedId)
            {
                ProcessAlienSelection(selectedAlienId);
            }

            // Update previous ID
            previousAlienId = selectedAlienId;
        }
    }

    void ProcessAlienSelection(int selectedAlienId)
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

            // Set transformation in progress flag
            transformationInProgress = true;

            // Swap the icon in the wheel UI before transformation
            if (alienWheelController != null)
            {
                alienWheelController.SwapAlienWithBen(selectedAlienId);
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

        // Update Ben button state for when wheel opens next
        UpdateBenButtonState();

        // Reset transformation flag after delay
        yield return new WaitForSeconds(0.5f);
        transformationInProgress = false;

        if (debugMode)
        {
            Debug.Log("Transformation sequence completed.");
        }
    }

    /// <summary>
    /// Coroutine to revert to Ben
    /// </summary>
    private System.Collections.IEnumerator RevertToBenCoroutine()
    {
        if (debugMode)
        {
            Debug.Log("Starting reversion to Ben");
        }

        // Restore all original icons
        if (alienWheelController != null)
        {
            alienWheelController.RestoreAllIcons();
        }

        // Revert to Ben
        omnitrixController.RevertToBen();

        // Wait for reversion to complete
        yield return new WaitForSeconds(0.5f);

        // Update Ben button state for when wheel opens next
        UpdateBenButtonState();

        // Reset transformation flag
        transformationInProgress = false;

        // Reset processed ID
        lastProcessedId = 0;

        if (debugMode)
        {
            Debug.Log("Reversion to Ben completed");
        }
    }

    /// <summary>
    /// Method to revert to Ben directly
    /// </summary>
    public void RevertToBen()
    {
        if (omnitrixController.IsTransformed)
        {
            // Revert to Ben
            omnitrixController.RevertToBen();

            // Restore all original icons
            if (alienWheelController != null)
            {
                alienWheelController.RestoreAllIcons();
            }

            // Update the last processed ID
            lastProcessedId = 0;
        }
    }

    /// <summary>
    /// Updates the Ben button state (enabled/disabled)
    /// </summary>
    private void UpdateBenButtonState()
    {
        if (alienWheelController != null && alienWheelController.benButton != null)
        {
            // Get Ben's button
            Button benButton = alienWheelController.benButton.GetComponent<Button>();
            if (benButton != null)
            {
                // Only enable Ben button when transformed
                bool isTransformed = omnitrixController.IsTransformed;
                benButton.interactable = isTransformed;

                // Visual feedback
                if (benButton.image != null)
                {
                    Color color = benButton.image.color;
                    color.a = isTransformed ? 1.0f : 0.5f;
                    benButton.image.color = color;
                }

                if (debugMode)
                {
                    Debug.Log($"Ben button enabled: {isTransformed}");
                }
            }
        }
    }
}