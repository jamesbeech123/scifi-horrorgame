using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingScript : MonoBehaviour
{
    private Light lightSource;

    public enum LightMode { Flicker, SineWave }
    [Header("Light Mode")]
    public LightMode lightMode;

    [Header("Flicker Settings")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 0.1f;

    [Header("Sine Wave Settings")]
    public float sineMinIntensity = 0.5f;
    public float sineMaxIntensity = 1.5f;
    public float sineSpeed = 1.0f;

    void Start()
    {
        lightSource = GetComponent<Light>();
        if (lightMode == LightMode.Flicker)
        {
            StartCoroutine(FlickerLight());
        }
        else if (lightMode == LightMode.SineWave)
        {
            StartCoroutine(SineWaveLight());
        }
    }

    // Coroutine to flicker the light
    IEnumerator FlickerLight()
    {
        while (true)
        {
            lightSource.intensity = Random.Range(minIntensity, maxIntensity);
            yield return new WaitForSeconds(flickerSpeed);
        }
    }

    // Coroutine to change light intensity in a sine wave pattern
    IEnumerator SineWaveLight()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * sineSpeed) + 1.0f) / 2.0f;
            lightSource.intensity = Mathf.Lerp(sineMinIntensity, sineMaxIntensity, t);
            yield return null;
        }
    }
}