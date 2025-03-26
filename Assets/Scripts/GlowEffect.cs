using UnityEngine;
using UnityEngine.UI;

public class GlowEffect : MonoBehaviour
{
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private float glowSpeed = 1.0f;
    [SerializeField] private Color glowColor = new Color(0.5f, 1f, 0f);

    private Image image;
    private Material material;
    private float initialAlpha;

    void Start()
    {
        image = GetComponent<Image>();
        if (image != null)
        {
            // Create a material instance to avoid affecting other UI elements
            material = new Material(image.material);
            image.material = material;

            // Set the glow color
            material.SetColor("_GlowColor", glowColor);

            // Store the initial alpha
            initialAlpha = image.color.a;
        }
    }

    void Update()
    {
        if (material != null)
        {
            // Calculate pulsing glow
            float glow = Mathf.Sin(Time.time * glowSpeed) * 0.2f + 0.8f;
            material.SetFloat("_GlowIntensity", glow * glowIntensity);
        }
    }

    public void SetActive(bool active)
    {
        if (material != null)
        {
            // When active, increase the glow intensity
            material.SetFloat("_GlowIntensity", active ? glowIntensity : 0);
        }

        if (image != null)
        {
            // When active, make fully opaque, otherwise slightly transparent
            Color color = image.color;
            color.a = active ? initialAlpha : initialAlpha * 0.6f;
            image.color = color;
        }
    }
}