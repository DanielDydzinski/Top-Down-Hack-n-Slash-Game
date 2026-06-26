using UnityEngine;

public class MultiHitDestructibleBridge : MonoBehaviour
{
    public int hitsRequiredToBreak = 10;
    private int _currentHits = 0;
    private bool _isBroken = false;

    private DestructibleEnvironment _destructible;

    void Start()
    {
        _destructible = GetComponent<DestructibleEnvironment>();
    }

    // This is hooked up to the InteractableEnvironment's OnInteracted event
    public void OnHitReceived(HitInfo info)
    {
        if (_isBroken) return;

        // Ensure we only count it if it's the right damage type!
        if (info.type == _destructible.lethalType)
        {
            _currentHits++;

            if (_currentHits >= hitsRequiredToBreak)
            {
                _isBroken = true;

                // BRIDGE: Pass the execution directly to our official Destruction system!
                _destructible.TakeDamage(info);
            }
        }
    }
}