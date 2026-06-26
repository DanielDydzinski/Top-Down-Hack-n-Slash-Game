using System.Collections;
using UnityEngine;

public class FogZoneTrigger : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("The dense fog value when the player is inside the area.")]
    [SerializeField] private float innerFogDensity = 0.165f;

    [Tooltip("The normal fog value when the player is outside.")]
    [SerializeField] private float normalFogDensity = 0.005f;

    [Tooltip("How fast the fog transitions in seconds.")]
    [SerializeField] private float fadeDuration = 2.0f;

    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private Coroutine fadeCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the zone is the player
        if (other.CompareTag(playerTag))
        {
            // Stop any active fading to avoid glitchy overlapping transitions
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            // Start fading toward the dense inner fog
            fadeCoroutine = StartCoroutine(FadeFog(RenderSettings.fogDensity, innerFogDensity));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object leaving the zone is the player
        if (other.CompareTag(playerTag))
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            // Start fading back toward the normal thin fog
            fadeCoroutine = StartCoroutine(FadeFog(RenderSettings.fogDensity, normalFogDensity));
        }
    }

    private IEnumerator FadeFog(float startValue, float targetValue)
    {
        float timePassed = 0f;

        while (timePassed < fadeDuration)
        {
            timePassed += Time.deltaTime;

            // Calculate progress fraction between 0.0 and 1.0
            float progress = timePassed / fadeDuration;

            // Lerp the global render settings fog density frame by frame
            RenderSettings.fogDensity = Mathf.Lerp(startValue, targetValue, progress);

            yield return null; // Wait until the next frame
        }

        // Hard snap to the final exact target value at the end to correct rounding math
        RenderSettings.fogDensity = targetValue;
    }
}
