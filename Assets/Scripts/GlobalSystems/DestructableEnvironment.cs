using UnityEngine;

public class DestructibleEnvironment : MonoBehaviour, IDamageable
{

    public DamageType lethalType = DamageType.Fire;
    public GameObject destructionParticles;
    public bool ignoreFriendlyFire = true;

    public void TakeDamage(HitInfo info)
    {
        // 1. Faction Check
        if (ignoreFriendlyFire)
        {
            EntityIdentity myIdentity = GetComponent<EntityIdentity>();
            // If the person hitting me is on my team, do nothing
            if (myIdentity != null && info.faction == myIdentity.faction)
            {
                return;
            }
        }

        // 2. Type Check
        if (info.type == lethalType)
        {
            Break();
        }
    }

    private void Break()
    {
        if (destructionParticles != null)
        {
            Instantiate(destructionParticles, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}