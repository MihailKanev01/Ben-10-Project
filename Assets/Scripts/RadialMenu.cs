using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadialMenu : MonoBehaviour
{
    [Header("Radial Menu Settings")]
    [SerializeField] private float radius = 250f;
    [SerializeField] private Transform centerTransform;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int slotCount = 10;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("UI Elements")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Transform dotsContainer;
    [SerializeField] private GameObject dotPrefab;

    // Stores all the slot objects
    private List<GameObject> slots = new List<GameObject>();
    private List<GameObject> dots = new List<GameObject>();
    private int currentSelection = 0;
    private float targetRotation = 0f;
    private bool isRotating = false;

    void Start()
    {
        // Create the radial menu
        CreateRadialMenu();

        // Create the pagination dots
        CreatePaginationDots();

        // Set up button listeners
        leftButton.onClick.AddListener(RotateLeft);
        rightButton.onClick.AddListener(RotateRight);

        // Initialize the selection
        UpdateSelection(0);
    }

    void Update()
    {
        // Smoothly interpolate the rotation
        if (isRotating)
        {
            float currentRot = transform.eulerAngles.z;
            float newRot = Mathf.LerpAngle(currentRot, targetRotation, Time.deltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(0, 0, newRot);

            // Check if rotation is complete
            if (Mathf.Abs(Mathf.DeltaAngle(newRot, targetRotation)) < 0.1f)
            {
                transform.rotation = Quaternion.Euler(0, 0, targetRotation);
                isRotating = false;
            }
        }
    }

    private void CreateRadialMenu()
    {
        // Clear any existing slots
        foreach (GameObject slot in slots)
        {
            Destroy(slot);
        }
        slots.Clear();

        // Create slots in a circle
        float angleStep = 360f / slotCount;

        for (int i = 0; i < slotCount; i++)
        {
            // Calculate the angle and position
            float angle = i * angleStep;
            float radian = angle * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Sin(radian) * radius, Mathf.Cos(radian) * radius, 0);

            // Create the slot
            GameObject slot = Instantiate(slotPrefab, transform);
            slot.transform.localPosition = position;

            // Look at center
            slot.transform.up = (Vector3.zero - position).normalized;

            // Store the slot
            slots.Add(slot);

            // Add click functionality
            int index = i; // Capture the index for the lambda
            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectSlot(index));
            }
        }
    }

    private void CreatePaginationDots()
    {
        // Clear any existing dots
        foreach (GameObject dot in dots)
        {
            Destroy(dot);
        }
        dots.Clear();

        // Create dots
        for (int i = 0; i < slotCount; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsContainer);
            dots.Add(dot);
        }
    }

    public void RotateLeft()
    {
        UpdateSelection((currentSelection + 1) % slotCount);
    }

    public void RotateRight()
    {
        UpdateSelection((currentSelection - 1 + slotCount) % slotCount);
    }

    private void SelectSlot(int index)
    {
        UpdateSelection(index);
    }

    private void UpdateSelection(int newSelection)
    {
        currentSelection = newSelection;

        // Calculate the target rotation to center the selected slot
        float angleStep = 360f / slotCount;
        targetRotation = -angleStep * currentSelection;
        isRotating = true;

        // Update the pagination dots
        for (int i = 0; i < dots.Count; i++)
        {
            // Get dot image component
            Image dotImage = dots[i].GetComponent<Image>();
            if (dotImage != null)
            {
                // Active dot has full alpha, others are transparent
                Color c = dotImage.color;
                c.a = (i == currentSelection) ? 1f : 0.4f;
                dotImage.color = c;
            }
        }

        // Scale up the selected slot, scale down others
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].transform.localScale = (i == currentSelection) ?
                new Vector3(1.2f, 1.2f, 1.2f) :
                new Vector3(1f, 1f, 1f);

            // You could also change the brightness/glow here
            Image slotImage = slots[i].GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.color = (i == currentSelection) ?
                    new Color(1f, 1f, 1f, 1f) :
                    new Color(0.7f, 0.7f, 0.7f, 0.7f);
            }
        }

        // Trigger any selection events
        OnSlotSelected(currentSelection);
    }

    private void OnSlotSelected(int selectedIndex)
    {
        // Implement your selection logic here
        Debug.Log($"Selected alien form {selectedIndex}");

        // You could trigger animations, update the center character model, etc.
    }
}