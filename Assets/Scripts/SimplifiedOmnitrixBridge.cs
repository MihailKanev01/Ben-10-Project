using UnityEngine;
using System.Collections;

public class SimplifiedOmnitrixBridge : MonoBehaviour
{
    public OmnitrixController omnitrixController;
    public AlienWheelController alienWheelController;
    public bool debugMode = true;

    // Store currently selected alien ID
    private int lastSelectedAlienId = 0;

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
            Debug.Log("[SimplifiedBridge] Initialized");
            Debug.Log("[SimplifiedBridge] Is transformed: " + omnitrixController.IsTransformed);
        }
    }

    void Update()
    {
        // Skip updates during transformation
        if (isTransforming)
            return;

        // Get current alien selection from wheel
        int currentAlienId = AlienWheelController.alienId;

        // Skip if no selection or same as last time
        if (currentAlienId == 0 || currentAlienId == lastSelectedAlienId)
            return;

        if (debugMode)
            Debug.Log("[SimplifiedBridge] Selected alien ID: " + currentAlienId);

        // Process human to alien transformation
        if (currentAlienId > 0)
        {
            isTransforming = true;

            if (debugMode)
                Debug.Log("[SimplifiedBridge] Starting transformation to alien " + currentAlienId);

            // Don't transform if we're already transformed
            if (!omnitrixController.IsTransformed)
            {
                int alienIndex = currentAlienId - 1;
                StartCoroutine(TransformToAlien(alienIndex));
            }

            // Store this selection
            lastSelectedAlienId = currentAlienId;

            // Reset the wheel selection
            AlienWheelController.alienId = 0;

            // Close the wheel
            if (alienWheelController != null && alienWheelController.alienWheelSelected)
            {
                alienWheelController.CloseWheel();
            }
        }
    }

    private IEnumerator TransformToAlien(int alienIndex)
    {
        if (debugMode)
            Debug.Log("[SimplifiedBridge] TransformToAlien coroutine started for index " + alienIndex);

        // Safety check
        if (alienIndex < 0 || alienIndex >= omnitrixController.availableAliens.Count)
        {
            isTransforming = false;
            yield break;
        }

        // First cycle to the correct alien
        int currentIndex = omnitrixController.GetSelectedAlienIndex();
        while (currentIndex != alienIndex)
        {
            if (debugMode)
                Debug.Log("[SimplifiedBridge] Cycling aliens: current=" + currentIndex + ", target=" + alienIndex);

            omnitrixController.PublicCycleToNextAlien();
            currentIndex = omnitrixController.GetSelectedAlienIndex();
            yield return null;
        }

        // Then trigger transformation
        if (debugMode)
            Debug.Log("[SimplifiedBridge] Triggering transformation");

        omnitrixController.TransformPressed();

        // Wait to complete
        yield return new WaitForSeconds(1f);

        // Done transforming
        isTransforming = false;

        if (debugMode)
            Debug.Log("[SimplifiedBridge] Transformation complete, transformed status: " + omnitrixController.IsTransformed);
    }

    public void RevertToBen()
    {
        if (debugMode)
            Debug.Log("[SimplifiedBridge] RevertToBen called, current status: " + omnitrixController.IsTransformed);

        // Only revert if actually transformed
        if (omnitrixController.IsTransformed)
        {
            StartCoroutine(RevertToBenCoroutine());
        }
    }

    private IEnumerator RevertToBenCoroutine()
    {
        isTransforming = true;

        // Trigger reversion
        omnitrixController.RevertToBen();

        // Wait to complete
        yield return new WaitForSeconds(1f);

        // Done transforming
        isTransforming = false;
        lastSelectedAlienId = 0;

        if (debugMode)
            Debug.Log("[SimplifiedBridge] Reversion complete, transformed status: " + omnitrixController.IsTransformed);
    }
}