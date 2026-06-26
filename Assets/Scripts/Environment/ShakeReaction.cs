using UnityEngine;
using System.Collections;

public class ShakeReaction : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeDuration = 0.3f;
    public float shakeIntensity = 0.15f;

    [Header("Optional Audio")]
    [Tooltip("Leave this empty if you don't want this specific object to make sound when shaken.")]
    public AudioClip shakeSound;

    private Vector3 _originalPosition;
    private bool _isShaking = false;
    private AudioSource _audioSource;

    void Start()
    {
        _originalPosition = transform.position;

        // Cache the AudioSource if one exists on this object
        TryGetComponent(out _audioSource);
    }

    public void TriggerShake(HitInfo info)
    {
        // 1. Play the optional sound if assigned and an AudioSource is available
        if (shakeSound != null && _audioSource != null)
        {
            // PlayOneShot is perfect here because it allows sounds to overlap 
            // if the object is hit again before the first sound finishes
            _audioSource.PlayOneShot(shakeSound);
        }

        // 2. Trigger the physical shake visual
        if (!_isShaking)
        {
            StartCoroutine(ShakeRoutine(info.forceDirection));
        }
    }

    private IEnumerator ShakeRoutine(Vector3 hitDirection)
    {
        _isShaking = true;
        float elapsed = 0f;
        Vector3 biasDir = hitDirection != Vector3.zero ? hitDirection.normalized : -transform.forward;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / shakeDuration;
            float currentStrength = Mathf.Lerp(shakeIntensity, 0f, percentComplete);

            Vector3 randomOffset = Random.insideUnitSphere * currentStrength;
            transform.position = _originalPosition + (biasDir * currentStrength * 0.5f) + randomOffset;

            yield return null;
        }

        transform.position = _originalPosition;
        _isShaking = false;
    }
}