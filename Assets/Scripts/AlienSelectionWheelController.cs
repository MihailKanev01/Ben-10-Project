using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the overall alien selection wheel behavior
/// </summary>
public class AlienSelectionWheelController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Key to toggle the alien selection wheel")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("References")]
    [Tooltip("Animator component for the wheel animations")]
    public Animator anim;

    [Tooltip("Image to display the selected alien")]
    public Image selectedAlienDisplay;

    [Tooltip("Empty/default sprite when no alien is selected")]
    public Sprite noAlienSprite;

    [Tooltip("Audio source for wheel sound effects (optional)")]
    public AudioSource audioSource;

    [Tooltip("Sound played when opening the wheel (optional)")]
    public AudioClip openSound;

    [Tooltip("Sound played when selecting an alien (optional)")]
    public AudioClip selectSound;

    [Header("Runtime")]
    [SerializeField, Tooltip("Is the wheel currently open?")]
    private bool wheelOpen = false;

    // Static ID for the currently selected alien (accessible by buttons)
    public static int selectedAlienId = 0;

    // Reference to the game object that should receive the alien selection
    private GameObject player;

    void Start()
    {
        // Initialize the wheel as closed
        wheelOpen = false;

        // Find the player object (adjust this to your game's structure)
        player = GameObject.FindGameObjectWithTag("Player");

        // Initialize with no alien selected
        selectedAlienDisplay.sprite = noAlienSprite;
    }

    void Update()
    {
        // Toggle the wheel when the toggle key is pressed
        if (Input.GetKeyDown(toggleKey))
        {
            // Invert the current state
            wheelOpen = !wheelOpen;

            if (wheelOpen)
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
        // Trigger the open animation
        anim.SetBool("openAlienWheel", true);

        // Play open sound if available
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // Optional: Pause or slow down game time
        Time.timeScale = 0.5f;
    }

    /// <summary>
    /// Closes the alien selection wheel
    /// </summary>
    private void CloseWheel()
    {
        // Trigger the close animation
        anim.SetBool("openAlienWheel", false);

        // Resume normal game time
        Time.timeScale = 1.0f;
    }

    /// <summary>
    /// Process the currently selected alien based on its ID
    /// </summary>
    private void ProcessSelectedAlien()
    {
        // Use a switch statement to handle different alien selections
        switch (selectedAlienId)
        {
            case 0: // No alien selected
                selectedAlienDisplay.sprite = noAlienSprite;
                break;

            case 1: // First alien form
                ApplyAlienSelection(1);
                break;

            case 2: // Second alien form
                ApplyAlienSelection(2);
                break;

            case 3: // Third alien form
                ApplyAlienSelection(3);
                break;

            case 4: // Fourth alien form
                ApplyAlienSelection(4);
                break;

            case 5: // Fifth alien form
                ApplyAlienSelection(5);
                break;

            case 6: // Sixth alien form
                ApplyAlienSelection(6);
                break;

            case 7: // Seventh alien form
                ApplyAlienSelection(7);
                break;

            case 8: // Eighth alien form
                ApplyAlienSelection(8);
                break;

            case 9: // Ninth alien form
                ApplyAlienSelection(9);
                break;

            case 10: // Tenth alien form
                ApplyAlienSelection(10);
                break;
        }
    }

    /// <summary>
    /// Apply the selected alien form to the player
    /// </summary>
    /// <param name="alienId">ID of the selected alien</param>
    private void ApplyAlienSelection(int alienId)
    {
        // Play selection sound if available
        if (audioSource != null && selectSound != null && selectedAlienId != 0)
        {
            audioSource.PlayOneShot(selectSound);
        }

        // If you have a player character that should transform
        if (player != null)
        {
            // Get the player's alien transformation component (you'll need to create this)
            AlienTransformation alienTransformation = player.GetComponent<AlienTransformation>();

            if (alienTransformation != null)
            {
                // Transform the player into the selected alien
                alienTransformation.TransformIntoAlien(alienId);
            }
        }

        Debug.Log($"Transformed into Alien Form #{alienId}");
    }
}