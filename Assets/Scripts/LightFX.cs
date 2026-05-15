using UnityEngine;
using System.Collections;

public class LightFX : MonoBehaviour
{
    private Light targetLight;

    [Header("Mode Selection")]
    public bool fadeFlash = false;
    public bool flicker = false;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 2.0f;
    [SerializeField] private float targetIntensity = 0f;
    [SerializeField] private Color targetColor = Color.black;

    [Header("Flicker Settings")]
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 1.5f;
    [SerializeField] private Color flickerColorA = Color.white;
    [SerializeField] private Color flickerColorB = Color.yellow;
    [SerializeField] private float flickerSpeed = 0.05f;

    private float startIntensity;
    private Color startColor;

    void Start()
    {
        targetLight = GetComponent<Light>();
        startIntensity = targetLight.intensity;
        startColor = targetLight.color;

        if (fadeFlash) StartCoroutine(FadeRoutine());
        if (flicker) StartCoroutine(FlickerRoutine());
    }

    IEnumerator FadeRoutine()
    {
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            targetLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            targetLight.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Randomly pick a value between your min/max settings
            targetLight.intensity = Random.Range(minIntensity, maxIntensity);

            // Randomly blend between your two chosen colors
            targetLight.color = Color.Lerp(flickerColorA, flickerColorB, Random.value);

            yield return new WaitForSeconds(flickerSpeed);
        }
    }
}
