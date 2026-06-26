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

    public void TakeDamage(HitInfo info)
    {
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