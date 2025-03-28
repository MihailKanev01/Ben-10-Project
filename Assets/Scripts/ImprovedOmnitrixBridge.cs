using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ImprovedOmnitrixBridge : MonoBehaviour
{
    [Header("References")]
    public OmnitrixController omnitrixController;
    public AlienWheelController alienWheelController;

    [Header("Settings")]
    public bool closeWheelAfterSelection = true;
    public bool enableBenButtonManagement = true;

    [Header("Debug")]
    public bool debugMode = true;

    // Store currently selected alien ID
    private int currentAlienId = 0;

    // This flag prevents processing multiple transformations at once
    private bool isTransforming = false;

    void Start()
    {
        // Find references if not assigned
        if (omnitrixController == null)
            omnitrixController = FindAnyObjectByType<OmnitrixController>();

        if (alienWheelController == null)
            alienWheelController = FindAnyObjectByType<AlienWheelController>();

        if (debugMode)
        {
            Debug.Log("[ImprovedBridge] Initialized");
            Debug.Log("[ImprovedBridge] Is transformed: " + omnitrixController.IsTransformed);
        }

        // Initial setup of Ben button
        if (enableBenButtonManagement)
        {
            UpdateBenButtonState();
        }
    }

    void Update()
    {
        // Skip updates during transformation
        if (isTransforming)
            return;

        // Update Ben button when wheel is opened
        if (enableBenButtonManagement && alienWheelController.alienWheelSelected)
        {
            UpdateBenButtonState();
        }

        // Get current alien selection from wheel
        int selectedAlienId = AlienWheelController.alienId;

        // Debug info
        if (debugMode && selectedAlienId != 0)
        {
            Debug.Log($"[ImprovedBridge] Selected ID: {selectedAlienId}, Current ID: {currentAlienId}, Transformed: {omnitrixController.IsTransformed}");
        }

        // Process alien selection
        if (selectedAlienId > 0 && selectedAlienId != currentAlienId)
        {
            if (debugMode)
                Debug.Log("[ImprovedBridge] Selected alien ID: " + selectedAlienId);

            // Begin transformation process  
            TransformToAlienWithID(selectedAlienId);
        }
        // Process Ben selection (revert to human)
        else if (selectedAlienId == 0 && currentAlienId > 0 && omnitrixController.IsTransformed)
        {
            if (debugMode)
                Debug.Log("[ImprovedBridge] Ben selected, reverting to human form");

            // Begin reversion process
            RevertToBen();
        }
    }

    private void TransformToAlienWithID(int alienId)
    {
        // Prevent multiple transformations
        isTransforming = true;

        if (debugMode)
            Debug.Log("[ImprovedBridge] Starting transformation to alien " + alienId);

        // Convert wheel ID to omnitrix index
        int alienIndex = alienId - 1;

        // Safety check
        if (alienIndex < 0 || alienIndex >= omnitrixController.availableAliens.Count)
        {
            if (debugMode)
                Debug.LogError("[ImprovedBridge] Invalid alien index: " + alienIndex);

            isTransforming = false;
            return;
        }

        // Swap the icon in the wheel UI before transformation
        if (alienWheelController != null)
        {
            alienWheelController.SwapAlienWithBen(alienId);
        }

        // Start transformation coroutine
        StartCoroutine(TransformToAlien(alienIndex));

        // Store current alien ID
        currentAlienId = alienId;

        // Reset wheel selection
        AlienWheelController.alienId = 0;

        // Close the wheel if configured
        if (closeWheelAfterSelection && alienWheelController != null)
        {
            if (debugMode)
                Debug.Log("[ImprovedBridge] Closing wheel");

            alienWheelController.alienWheelSelected = false;

            if (alienWheelController.anim != null)
                alienWheelController.anim.SetBool("openAlienWheel", false);

            alienWheelController.CloseWheel();
        }
    }

    private IEnumerator TransformToAlien(int alienIndex)
    {
        if (debugMode)
            Debug.Log("[ImprovedBridge] TransformToAlien coroutine started for index " + alienIndex);

        // First cycle to the correct alien
        int currentIndex = omnitrixController.GetSelectedAlienIndex();
        while (currentIndex != alienIndex)
        {
            if (debugMode)
                Debug.Log("[ImprovedBridge] Cycling aliens: current=" + currentIndex + ", target=" + alienIndex);

            omnitrixController.PublicCycleToNextAlien();
            currentIndex = omnitrixController.GetSelectedAlienIndex();
            yield return null;
        }

        // Then trigger transformation
        if (debugMode)
            Debug.Log("[ImprovedBridge] Triggering transformation");

        omnitrixController.TransformPressed();

        // Wait to complete
        yield return new WaitForSeconds(1f);

        // Done transforming
        isTransforming = false;

        if (debugMode)
            Debug.Log("[ImprovedBridge] Transformation complete, transformed status: " + omnitrixController.IsTransformed);
    }

    public void RevertToBen()
    {
        if (debugMode)
            Debug.Log("[ImprovedBridge] RevertToBen called, current status: " + omnitrixController.IsTransformed);

        // Only revert if actually transformed
        if (!omnitrixController.IsTransformed)
            return;

        // Prevent multiple transformations
        isTransforming = true;

        // Restore all original icons
        if (alienWheelController != null)
        {
            alienWheelController.RestoreAllIcons();
        }

        // Start reversion coroutine
        StartCoroutine(RevertToBenCoroutine());

        // Reset current alien ID
        currentAlienId = 0;

        // Reset wheel selection
        AlienWheelController.alienId = 0;

        // Close the wheel if configured
        if (closeWheelAfterSelection && alienWheelController != null && alienWheelController.alienWheelSelected)
        {
            alienWheelController.alienWheelSelected = false;

            if (alienWheelController.anim != null)
                alienWheelController.anim.SetBool("openAlienWheel", false);

            alienWheelController.CloseWheel();
        }
    }

    private IEnumerator RevertToBenCoroutine()
    {
        // Trigger reversion
        omnitrixController.RevertToBen();

        // Wait to complete
        yield return new WaitForSeconds(1f);

        // Done transforming
        isTransforming = false;

        if (debugMode)
            Debug.Log("[ImprovedBridge] Reversion complete, transformed status: " + omnitrixController.IsTransformed);
    }

    private void UpdateBenButtonState()
    {
        if (alienWheelController == null || alienWheelController.benButton == null)
            return;

        // Get the button component
        Button benButton = alienWheelController.benButton.GetComponent<Button>();
        if (benButton == null)
            return;

        // Enable Ben button only when transformed, disable when in human form
        benButton.interactable = omnitrixController.IsTransformed;

        // Update visual feedback
        if (benButton.image != null)
        {
            Color buttonColor = benButton.image.color;
            buttonColor.a = omnitrixController.IsTransformed ? 1.0f : 0.5f;
            benButton.image.color = buttonColor;
        }

        if (debugMode)
            Debug.Log("[ImprovedBridge] Ben button interactable set to: " + benButton.interactable);
    }
}