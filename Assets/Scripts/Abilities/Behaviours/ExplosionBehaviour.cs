using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour
{
    public GameObject explosionParticles;
    public float explosionRadius;
    public bool damageByDistance;
    public Faction myFaction;
    public DamageType damageType;
    public LayerMask wallLayer; // No more hardcoded 1024

    public List<Effect> effects = new List<Effect>();

    private AudioClip audioClip;
    private AudioSource audioSource;
    private GameObject caster;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        //waity one frame for values to update
        StartCoroutine(DelayedExplode());
        
    }

    public void UpdateValues(List<Effect> aeffects, GameObject aexploPrefab, float aRadius, Faction faction, DamageType dmgType, bool dmgByDist,AudioClip aclip, GameObject aCaster)
    {
        effects = aeffects;
        explosionParticles = aexploPrefab;
        explosionRadius = aRadius;
        myFaction = faction;
        damageType = dmgType;
        damageByDistance = dmgByDist;
        audioClip = aclip;
        caster = aCaster;
        //Explode();
    }

    private IEnumerator DelayedExplode()
    {
        if (explosionParticles != null)
        {
            Instantiate(explosionParticles, transform.position, Quaternion.identity);
        }
        if(audioClip != null)
        {
            AudioSource.PlayClipAtPoint(audioClip, transform.position);
        }

        yield return null; // Wait exactly one frame
        Explode();
    }

    private void Explode()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider c in hitColliders)
        {
            // 1. Faction Check
            EntityIdentity victimIdentity = c.GetComponent<EntityIdentity>();
            if (victimIdentity != null && victimIdentity.faction == myFaction) continue;

            // 2. Wall Check
            float dist = Vector3.Distance(transform.position, c.transform.position);
            Vector3 dir = (c.transform.position - transform.position).normalized;
            if (Physics.Raycast(transform.position, dir, dist, wallLayer)) continue;

            // 3. Mailbox Check
            IDamageable damageable = c.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate falloff (Scale is 1.0 at center, 0.0 at edge)
                float falloff = 1f;
                if (damageByDistance)
                {
                    // We clamp it between 0 and 1 just to be safe
                    falloff = Mathf.Clamp01(1.0f - (dist / explosionRadius));
                }

                // Handle Knockback position before sending
                foreach (Effect e in effects)
                {
                    if (e is TakeKnockBack kb) kb.SetExplosionPos(transform.position);
                }

                // Create the package
                HitInfo info = new HitInfo
                {
                    faction = myFaction,
                    effects = new List<Effect>(effects), // Pass the list
                    type = damageType,
                    attacker = this.gameObject,
                    multiplier = falloff,
                    forceDirection = dir,
                    isExplosion = true
                    
                };

                if (c.TryGetComponent<PushPropagator>(out var propagator))
                {
                    float force = 5f * falloff; // Example force
                    propagator.PropagatePush(dir * force * Time.deltaTime, this.gameObject);
                }

                // Logic for falloff: We don't modify the SO, we just tell the receiver!
                // If you want scaling, you'd handle it inside the specific Effect. 
                // For now, we deliver the package as-is.
                damageable.TakeDamage(info);
            }
        }
        Destroy(gameObject);
    }
    //	private void DamageByDistance(Vector3 targetPos, Vector3 centre,float radius)
    //	{
    //		float dmg = takeDamageEff.damageAmount;
    //		targetPos.y = 0f;
    //		float distance = Vector3.Distance (targetPos, centre);
    //		distance -= 1.0f;
    //		float scale = (distance) / radius;
    //		float dmgNew = dmg * (1.0f - scale);
    //		takeDamageEff.damageAmount = dmgNew;


    //	}

}

