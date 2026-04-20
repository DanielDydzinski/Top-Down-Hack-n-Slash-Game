using UnityEngine;
using System.Collections;
public class DestructibleEnvironment : MonoBehaviour, IDamageable
{

    public DamageType lethalType = DamageType.Fire;
    public GameObject destructionParticles;
    public bool ignoreFriendlyFire = true;
    private float deathDuration = 0.5f;

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
            StartCoroutine(Shrink());
            Break();
        }
    }

    private void Break()
    {
        if (destructionParticles != null)
        {
            Instantiate(destructionParticles, transform.position, Quaternion.identity);
        }
        Destroy(gameObject,deathDuration + 0.01f);
    }

    IEnumerator Shrink()
    {
        Vector3 startScale = transform.localScale;
        deathDuration = 0.5f;
        float time = 0;
        while (time < deathDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, time / deathDuration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;
    }
}