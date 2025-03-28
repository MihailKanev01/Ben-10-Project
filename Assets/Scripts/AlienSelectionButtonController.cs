using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the behavior of individual alien selection buttons in the wheel
/// </summary>
public class AlienSelectionButtonController : MonoBehaviour
{
    [Header("Button Settings")]
    [Tooltip("Unique ID for this alien form")]
    public int id;

    [Tooltip("Display name of this alien form")]
    public string alienName;

    [Tooltip("Icon that represents this alien")]
    public Sprite icon;

    [Header("References")]
    [Tooltip("Text element to display the selected alien name")]
    public TextMeshProUGUI itemSelected;

    [Tooltip("Image element to display the selected alien icon")]
    public Image selectedAlien;

    [Header("Runtime")]
    [SerializeField, Tooltip("Is this alien currently selected?")]
    private bool selected = false;

    [Header("Omnitrix Swap Settings")]
    [Tooltip("Is this the button for Ben Tennyson?")]
    public bool isBenButton = false;

    [Tooltip("Reference to Ben's button")]
    public AlienSelectionButtonController benButton;

    // Reference to the animator component
    private Animator anim;

    // Original icon before swapping
    private Sprite originalIcon;

    void Start()
    {
        // Get reference to the Animator component
        anim = GetComponent<Animator>();

        // Store the original icon
        originalIcon = icon;

        // Check for missing references and log warnings
        if (itemSelected == null)
        {
            Debug.LogWarning("ItemSelected text is not assigned on " + gameObject.name + ". Please assign it in the inspector.", this);
        }

        if (selectedAlien == null)
        {
            Debug.LogWarning("SelectedAlien image is not assigned on " + gameObject.name + ". Please assign it in the inspector.", this);
        }

        // Find Ben's button if not assigned
        if (!isBenButton && benButton == null)
        {
            AlienSelectionButtonController[] buttons = FindObjectsOfType<AlienSelectionButtonController>();
            foreach (var button in buttons)
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
        // If this button is selected, update the selected alien display
        if (selected)
        {
            // Update the displayed icon if available
            if (selectedAlien != null && icon != null)
            {
                selectedAlien.sprite = icon;
            }

            // Update the displayed name if available
            if (itemSelected != null)
            {
                itemSelected.text = alienName;
            }
        }
    }

    /// <summary>
    /// Called when this alien button is selected
    /// </summary>
    public void Selected()
    {
        selected = true;

        // Update the global selected alien ID in the wheel controller
        AlienWheelController.alienId = id;

        // Debug message to confirm selection
        Debug.Log($"Alien {alienName} (ID: {id}) selected");
    }

    /// <summary>
    /// Called when this alien button is deselected
    /// </summary>
    public void Deselected()
    {
        selected = false;

        // Set the global selected alien ID to 0 (none selected)
        AlienWheelController.alienId = 0;
    }

    /// <summary>
    /// Called when the pointer enters this button
    /// </summary>
    public void HoverEnter()
    {
        // Activate hover animation if available
        if (anim != null)
        {
            anim.SetBool("hover", true);
        }

        // Display the alien name in the center text if available
        if (itemSelected != null)
        {
            itemSelected.text = alienName;
        }

        // Debug message to confirm hover
        Debug.Log($"Hovering over alien: {alienName}");
    }

    /// <summary>
    /// Called when the pointer exits this button
    /// </summary>
    public void HoverExit()
    {
        // Deactivate hover animation if available
        if (anim != null)
        {
            anim.SetBool("hover", false);
        }

        // Clear the center text if we're not selected and text component exists
        if (!selected && itemSelected != null)
        {
            itemSelected.text = "";
        }
    }

    /// <summary>
    /// Swaps this alien's icon with Ben's icon
    /// </summary>
    public void SwapWithBen()
    {
        if (benButton != null && !isBenButton)
        {
            // Swap icons
            Sprite tempIcon = icon;
            icon = benButton.originalIcon;
            benButton.icon = tempIcon;

            // Update display if this button is selected
            if (selected)
            {
                if (selectedAlien != null)
                {
                    selectedAlien.sprite = icon;
                }
                if (itemSelected != null)
                {
                    itemSelected.text = alienName;
                }
            }
        }
    }

    /// <summary>
    /// Restores this alien's original icon
    /// </summary>
    public void RestoreOriginalIcon()
    {
        icon = originalIcon;

        // Update display if this button is selected
        if (selected)
        {
            if (selectedAlien != null)
            {
                selectedAlien.sprite = icon;
            }
            if (itemSelected != null)
            {
                itemSelected.text = alienName;
            }
        }

        // Also restore Ben's icon
        if (benButton != null)
        {
            benButton.icon = benButton.originalIcon;
        }
    }
}