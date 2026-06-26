using UnityEngine;

public class HarvestDropReaction : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("The actual key GameObject that will fall.")]
    public GameObject keyObject;

    [Tooltip("The string visual/object connecting the key to the tree that will be destroyed.")]
    public GameObject stringObject;

    [Tooltip("How many hits from the player are required to break the key free.")]
    public int hitsRequired = 5;

    [Header("Physics Tweaks")]
    [Tooltip("Slight random push given to the key when it drops so it doesn't just drop straight down perfectly static.")]
    public float breakPushForce = 2f;

    private int _currentHits = 0;
    private bool _hasDropped = false;

    // This method will be plugged into your tree's InteractableEnvironment event
    public void OnTreeHit(HitInfo info)
    {
        // If the key already fell, ignore future hits
        if (_hasDropped) return;

        _currentHits++;

        if (_currentHits >= hitsRequired)
        {
            DropKey(info.forceDirection);
        }
    }

    private void DropKey(Vector3 hitDirection)
    {
        _hasDropped = true;

        if (keyObject == null) return;

        // 1. Cut the cord - Unparent the key so it's free in the world
        keyObject.transform.SetParent(null);

        // 2. Destroy the string child object if it exists
        if (stringObject != null)
        {
            Destroy(stringObject);
        }

        // 3. Turn on Physics for the key
        if (keyObject.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;

            // Give it a tiny, natural physics pop away from the attack direction
            Vector3 pushDir = hitDirection != Vector3.zero ? hitDirection.normalized : Vector3.forward;
            pushDir += Random.insideUnitSphere * 0.2f; // Add a tiny bit of random chaos
            pushDir.y = 0.2f; // Ensure it pops slightly upward/outward instead of straight down

            rb.AddForce(pushDir * breakPushForce, ForceMode.Impulse);
        }

        // 4. Ensure the key can actually be picked up now by enabling its collider/trigger if needed
        if (keyObject.TryGetComponent<Collider>(out var col))
        {
            col.enabled = true;
        }
    }
}