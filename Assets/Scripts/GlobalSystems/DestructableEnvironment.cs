using UnityEngine;
using UnityEngine.Events;

public class DestructibleEnvironment : MonoBehaviour, IDamageable
{
    [Header("Vulnerability Settings")]
    public DamageType lethalType = DamageType.Fire;
    public bool ignoreFriendlyFire = true;

    [Header("Effects")]
    public GameObject destructionParticles;

    [Header("Events")]
    // This allows you to hook up ANY custom logic in the Unity Inspector!
    public UnityEvent<HitInfo> OnDestroyed;

    private bool _hasBeenDestroyed = false;

    // Callers that AddComponent this onto a pooled ability prefab (FireBall/AoEoT) reuse the same
    // component instance across casts - without this, _hasBeenDestroyed stays true forever after the
    // first break, so every later reuse of that pooled instance would silently never take damage again.
    public void ResetState()
    {
        _hasBeenDestroyed = false;
    }

    public void TakeDamage(HitInfo info)
    {
        // Disabling a MonoBehaviour only stops Unity from calling its own Update/OnTrigger*
        // messages - it does NOT stop other scripts from calling a public method directly, which is
        // exactly how every Behaviours/*.cs class delivers damage (GetComponent<IDamageable>() then
        // TakeDamage()). FireBall.cs/AoEoT.cs both use enabled=false as their "not destructible this
        // cast" switch for a pooled instance that had this added by a PREVIOUS, destructible cast of
        // a different ability sharing the same prefab - without this check that stale, supposedly-off
        // instance would still silently break.
        if (!enabled) return;

        // Prevent the object from taking damage multiple times in one frame
        if (_hasBeenDestroyed) return;

        // 1. Faction Check
        if (ignoreFriendlyFire)
        {
            EntityIdentity myIdentity = GetComponent<EntityIdentity>();
            if (myIdentity != null && info.faction == myIdentity.faction)
            {
                return;
            }
        }

        // 2. Type Check
        if (info.type == lethalType)
        {
            _hasBeenDestroyed = true;
            Break(info);
        }
    }

    private void Break(HitInfo info)
    {
        if (destructionParticles != null)
        {
            Instantiate(destructionParticles, transform.position, Quaternion.identity);
        }

        // Fire the event, passing the hit info to whatever is listening!
        OnDestroyed?.Invoke(info);
    }
}