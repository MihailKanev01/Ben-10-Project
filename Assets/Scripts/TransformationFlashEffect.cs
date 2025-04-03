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
    public float lightDuration = 2.5f;  // INCREASED from 0.5f to 2.5f
    public AnimationCurve lightIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Color Settings")]
    public Color flashColor = new Color(0.0f, 1.0f, 0.2f); // Slightly blue-green like Omnitrix

    [Header("Effect Settings")]
    public float effectDuration = 5.0f;  // INCREASED from 1.5f to 5.0f
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

        // Set colors and update duration of all particle systems
        if (mainFlash != null)
        {
            var mainModule = mainFlash.main;
            mainModule.startColor = flashColor;

            // Extend particle lifetime if needed to match the new duration
            if (mainModule.startLifetime.constant < effectDuration)
            {
                mainModule.startLifetime = effectDuration * 0.8f; // Slightly shorter than total duration
            }

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

                    // Extend particle lifetime for additional effects too
                    if (mainModule.startLifetime.constant < effectDuration)
                    {
                        mainModule.startLifetime = effectDuration * 0.7f;
                    }

                    effect.Play();
                }
            }
        }

        // Handle the light effect with extended duration
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

        // Don't turn off the light immediately - fade it out more gradually
        float extraFadeTime = 1.0f;
        timer = 0f;
        float startIntensity = flashLight.intensity;

        while (timer < extraFadeTime)
        {
            timer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(timer / extraFadeTime);

            // Fade out gradually
            flashLight.intensity = Mathf.Lerp(startIntensity, 0, normalizedTime);

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
        main.duration = 5.0f;  // Increased from 1.0f
        main.loop = false;
        main.startLifetime = 2.5f;  // Increased from 0.5f
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