using UnityEngine;
using System.Collections;

public class ShrinkReaction : MonoBehaviour
{
    public float shrinkDuration = 0.5f;
    public AudioClip soundClip;

    public void TriggerShrink(HitInfo info)
    {
        // Turn off any hazardous tickers or colliders immediately so it stops hurting things while shrinking
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<AoEoTBehaviour>(out var behaviour)) behaviour.enabled = false;

        StartCoroutine(ShrinkRoutine());
    }

    private IEnumerator ShrinkRoutine()
    {
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        if (soundClip != null)
        {
            AudioSource.PlayClipAtPoint(soundClip, transform.position);
        }

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / shrinkDuration);
            yield return null;
        }

        // Clean up the object from memory cleanly!
        Destroy(gameObject);
    }
}