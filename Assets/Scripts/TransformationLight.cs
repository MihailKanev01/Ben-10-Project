// TransformationLight.cs - Add to a point light for enhanced transformation effects
using UnityEngine;

public class TransformationLight : MonoBehaviour
{
    public float flashDuration = 0.5f;
    public float maxIntensity = 8f;
    private Light lightComponent;
    private float timer = 0f;
    private bool isFlashing = false;

    void Awake()
    {
        lightComponent = GetComponent<Light>();
        lightComponent.intensity = 0;
        lightComponent.enabled = false;
    }

    public void StartFlash()
    {
        timer = 0f;
        isFlashing = true;
        lightComponent.enabled = true;
    }

    void Update()
    {
        if (isFlashing)
        {
            timer += Time.deltaTime;

            if (timer < flashDuration * 0.5f)
            {
                // Ramp up
                lightComponent.intensity = Mathf.Lerp(0, maxIntensity, timer / (flashDuration * 0.5f));
            }
            else if (timer < flashDuration)
            {
                // Ramp down
                lightComponent.intensity = Mathf.Lerp(maxIntensity, 0, (timer - flashDuration * 0.5f) / (flashDuration * 0.5f));
            }
            else
            {
                lightComponent.intensity = 0;
                lightComponent.enabled = false;
                isFlashing = false;
            }
        }
    }

    public void ResetLight()
    {
        isFlashing = false;
        lightComponent.intensity = 0;
        lightComponent.enabled = false;
    }
}