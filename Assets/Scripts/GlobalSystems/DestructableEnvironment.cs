using UnityEngine;

public class DestructibleEnvironment : MonoBehaviour, IDamageable
{

    public DamageType lethalType = DamageType.Fire;
    public GameObject destructionParticles;

    public void TakeDamage(HitInfo info)
    {
        // Only react if the damage type matches (e.g., Fire vs. Cobweb)
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