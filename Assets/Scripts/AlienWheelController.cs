using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Controls the overall alien selection wheel behavior
/// </summary>
public class AlienWheelController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Key to toggle the alien selection wheel")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("References")]
    [Tooltip("Animator component for the wheel animations")]
    public Animator anim;

    [Tooltip("Image to display the selected alien")]
    public Image selectedAlien;

    [Tooltip("Empty/default sprite when no alien is selected")]
    public Sprite noImage;

    [Tooltip("Audio source for wheel sound effects (optional)")]
    public AudioSource audioSource;

    [Tooltip("Sound played when opening the wheel (optional)")]
    public AudioClip openSound;

    [Header("Cursor Settings")]
    [Tooltip("Whether to show the cursor when the wheel is open")]
    public bool showCursorWhenWheelOpen = true;

    [Header("Ben Tennyson Button")]
    [Tooltip("Reference to Ben's button in the wheel")]
    public AlienSelectionButtonController benButton;

    [Tooltip("Reference to all alien buttons in the wheel")]
    public AlienSelectionButtonController[] alienButtons;

    [Header("Runtime")]
    [SerializeField, Tooltip("Is the wheel currently open?")]
    public bool alienWheelSelected = false;

    // Static ID for the currently selected alien (accessible by buttons)
    public static int alienId = 0;

    // Event that fires when an alien is selected
    public static event Action<int> OnAlienSelected;

    // Store the previous cursor state to restore it when closing the wheel
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    void Start()
    {
        // Initialize with no alien selected
        if (selectedAlien != null && noImage != null)
        {
            selectedAlien.sprite = noImage;
        }

        // Initialize cursor state
        previousCursorVisible = false;
        previousCursorLockState = CursorLockMode.Locked;

        // Find all alien buttons if not assigned
        if (alienButtons == null || alienButtons.Length == 0)
        {
            alienButtons = FindObjectsOfType<AlienSelectionButtonController>();
        }

        // Find Ben's button if not assigned
        if (benButton == null)
        {
            foreach (var button in alienButtons)
            {
                if (button.isBenButton)
                {
                    benButton = button;
                    break;
                }
            }
        }
    }

    void Update()
    {
        // Toggle the wheel when the toggle key is pressed
        if (Input.GetKeyDown(toggleKey))
        {
            // Invert the current state
            alienWheelSelected = !alienWheelSelected;

            if (alienWheelSelected)
            {
                // Open the wheel
                OpenWheel();
            }
            else
            {
                // Close the wheel
                CloseWheel();
            }
        }

        // Process the selected alien based on its ID
        ProcessSelectedAlien();
    }

    /// <summary>
    /// Opens the alien selection wheel
    /// </summary>
    private void OpenWheel()
    {
        // Store the current cursor state
        previousCursorVisible = Cursor.visible;
        previousCursorLockState = Cursor.lockState;

        Debug.Log($"Opening wheel - Storing cursor state: visible={previousCursorVisible}, lock={previousCursorLockState}");

        // Show and unlock the cursor while the wheel is open
        if (showCursorWhenWheelOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("Cursor unlocked and visible for wheel selection");
        }

        // Trigger the open animation - make sure this matches your animator parameter name
        if (anim != null)
        {
            anim.SetBool("openAlienWheel", true);
        }

        // Play open sound if available
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // Slow down time while selecting (optional)
        Time.timeScale = 0.5f;
    }

    /// <summary>
    /// Closes the alien selection wheel
    /// </summary>
    public void CloseWheel()
    {
        Debug.Log($"Closing wheel - Restoring cursor to: visible={previousCursorVisible}, lock={previousCursorLockState}");

        // Restore the previous cursor state with a slight delay
        // This helps ensure it happens after any selection processing
        Invoke("RestoreCursorState", 0.1f);

        // Trigger the close animation
        if (anim != null)
        {
            anim.SetBool("openAlienWheel", false);
        }

        // Resume normal game time
        Time.timeScale = 1.0f;

        // Update selected state
        alienWheelSelected = false;
    }

    /// <summary>
    /// Restores the cursor state with a small delay to avoid race conditions
    /// </summary>
    private void RestoreCursorState()
    {
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockState;
        Debug.Log($"Cursor state restored: visible={Cursor.visible}, lock={Cursor.lockState}");

        // Force the cursor to be locked for gameplay if we were locked before
        if (previousCursorLockState == CursorLockMode.Locked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Debug.Log("Forced cursor to locked state for gameplay");
        }
    }

    /// <summary>
    /// Process the currently selected alien based on its ID
    /// </summary>
    private void ProcessSelectedAlien()
    {
        // Use a switch statement to handle different alien selections
        switch (alienId)
        {
            case 0: // No alien selected
                if (selectedAlien != null && noImage != null)
                {
                    selectedAlien.sprite = noImage;
                }
                break;

            case 1: // First alien form
            case 2: // Second alien form
            case 3: // Third alien form
            case 4: // Fourth alien form (Humungousaur)
            case 5: // Fifth alien form
            case 6: // Sixth alien form
            case 7: // Seventh alien form
            case 8: // Eighth alien form
            case 9: // Ninth alien form
            case 10: // Tenth alien form
                // If an alien gets selected, trigger the event
                if (OnAlienSelected != null)
                {
                    Debug.Log($"Triggering OnAlienSelected event for alien ID: {alienId}");
                    OnAlienSelected.Invoke(alienId);
                }
                break;
        }
    }

    /// <summary>
    /// For debugging - force hide cursor
    /// </summary>
    public void ForceHideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Debug.Log("Cursor forcibly hidden and locked");
    }

    /// <summary>
    /// Swaps the selected alien's icon with Ben's icon
    /// </summary>
    public void SwapAlienWithBen(int alienId)
    {
        if (alienId <= 0 || benButton == null)
            return;

        // Find the selected alien button
        AlienSelectionButtonController selectedButton = null;
        foreach (var button in alienButtons)
        {
            if (button.id == alienId && !button.isBenButton)
            {
                selectedButton = button;
                break;
            }
        }

        if (selectedButton != null)
        {
            selectedButton.SwapWithBen();
        }
    }

    /// <summary>
    /// Restores all aliens' original icons
    /// </summary>
    public void RestoreAllIcons()
    {
        foreach (var button in alienButtons)
        {
            if (!button.isBenButton)
            {
                button.RestoreOriginalIcon();
            }
        }
    }
}