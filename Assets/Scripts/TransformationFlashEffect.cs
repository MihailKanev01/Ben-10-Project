using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a Ben 10 style transformation flash effect using particle systems.
/// Attach this to a parent GameObject containing particle systems for the flash effect.
/// </summary>
public class TransformationFlashEffect : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem mainFlash;
    public ParticleSystem[] additionalEffects;

    [Header("Light Settings")]
    public Light flashLight;
    public float maxLightIntensity = 8f;
    public float lightDuration = 0.5f;
    public AnimationCurve lightIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Color Settings")]
    public Color flashColor = new Color(0.0f, 1.0f, 0.2f); // Slightly blue-green like Omnitrix

    [Header("Effect Settings")]
    public float effectDuration = 1.5f;
    public float effectScale = 1.0f;

    private bool isPlaying = false;

    /// <summary>
    /// Positions the effect at the specified location and plays all particle systems
    /// </summary>
    public void PlayEffect(Vector3 position)
    {
        if (isPlaying)
            return;

        transform.position = position;
        StartCoroutine(PlayEffectSequence());
    }

    /// <summary>
    /// Plays the effect at the current transform position
    /// </summary>
    public void PlayEffect()
    {
        if (isPlaying)
            return;

        StartCoroutine(PlayEffectSequence());
    }

    private IEnumerator PlayEffectSequence()
    {
        isPlaying = true;

        // Set the scale of the effect
        transform.localScale = Vector3.one * effectScale;

        // Set colors of all particle systems
        if (mainFlash != null)
        {
            var mainModule = mainFlash.main;
            mainModule.startColor = flashColor;
            mainFlash.Play();
        }

        // Play all additional particle systems
        if (additionalEffects != null)
        {
            foreach (var effect in additionalEffects)
            {
                if (effect != null)
                {
                    var mainModule = effect.main;
                    mainModule.startColor = flashColor;
                    effect.Play();
                }
            }
        }

        // Handle the light effect
        if (flashLight != null)
        {
            StartCoroutine(LightFlashSequence());
        }

        // Wait for the effect to complete
        yield return new WaitForSeconds(effectDuration);

        isPlaying = false;
    }

    private IEnumerator LightFlashSequence()
    {
        flashLight.color = flashColor;
        flashLight.enabled = true;

        float timer = 0f;

        while (timer < lightDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(timer / lightDuration);

            // Use animation curve to determine intensity
            float intensityFactor = lightIntensityCurve.Evaluate(normalizedTime);
            flashLight.intensity = maxLightIntensity * intensityFactor;

            yield return null;
        }

        flashLight.enabled = false;
    }

    // Utility method to set up a basic flash effect
    public static TransformationFlashEffect CreateBasicFlashEffect(Transform parent = null)
    {
        GameObject flashObj = new GameObject("Ben10_TransformationFlash");
        if (parent != null)
            flashObj.transform.SetParent(parent, false);

        TransformationFlashEffect effect = flashObj.AddComponent<TransformationFlashEffect>();

        // Create main flash particle system
        GameObject mainFlashObj = new GameObject("MainFlash");
        mainFlashObj.transform.SetParent(flashObj.transform, false);
        ParticleSystem mainPS = mainFlashObj.AddComponent<ParticleSystem>();
        effect.mainFlash = mainPS;

        // Set up main flash
        var main = mainPS.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 0f;
        main.startSize = 10f;
        main.startColor = Color.green;
        main.gravityModifier = 0f;

        // Add light
        GameObject lightObj = new GameObject("FlashLight");
        lightObj.transform.SetParent(flashObj.transform, false);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.green;
        light.range = 20f;
        light.intensity = 0f;
        light.enabled = false;
        effect.flashLight = light;

        return effect;
    }
}