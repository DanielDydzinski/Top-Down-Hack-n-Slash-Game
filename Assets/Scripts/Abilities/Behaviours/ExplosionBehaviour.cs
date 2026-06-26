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
    
    [Header("Layer Overrides")]
    public LayerMask targetLayer; // Determines what can actually be damaged (e.g., Enemy, Player)
    public LayerMask wallLayer;   // Blocks explosions (e.g., Environment, InteractableEnvironment)

    public List<Effect> effects = new List<Effect>();

    private AudioClip audioClip;
    private AudioSource audioSource;
    private GameObject caster;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // Wait one frame for values to update
        StartCoroutine(DelayedExplode());
    }

    // UPDATED: Added targetLayer and wallLayer setup into initialization
    public void UpdateValues(List<Effect> aeffects, GameObject aexploPrefab, float aRadius, Faction faction, DamageType dmgType,
        bool dmgByDist, AudioClip aclip, GameObject aCaster, LayerMask aTargetLayer, LayerMask aWallLayer)
    {
        effects = aeffects;
        explosionParticles = aexploPrefab;
        explosionRadius = aRadius;
        myFaction = faction;
        damageType = dmgType;
        damageByDistance = dmgByDist;
        audioClip = aclip;
        caster = aCaster;
        targetLayer = aTargetLayer;
        wallLayer = aWallLayer;
    }

    private IEnumerator DelayedExplode()
    {
        if (explosionParticles != null)
        {
            Instantiate(explosionParticles, transform.position, Quaternion.identity);
        }
        if (audioClip != null)
        {
            AudioSource.PlayClipAtPoint(audioClip, transform.position);
        }

        yield return null; // Wait exactly one frame
        Explode();
    }

    private void Explode()
    {
        // FIX 1: Pass the targetLayer mask so we don't waste performance evaluating floors/static walls
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);

        foreach (Collider c in hitColliders)
        {
            // FIX 2: Check for root components or local components to prevent damaging the attacker/caster
            if (caster != null && (c.gameObject == caster || c.transform.IsChildOf(caster.transform))) continue;

            // 1. Faction Check
            EntityIdentity victimIdentity = c.GetComponent<EntityIdentity>();
            if (victimIdentity != null && victimIdentity.faction == myFaction) continue;

            // 2. Wall Check
            float dist = Vector3.Distance(transform.position, c.transform.position);
            
            // Avoid NaN errors or division by zero if target is exactly on top of explosion origin
            Vector3 dir = dist > 0.001f ? (c.transform.position - transform.position).normalized : transform.forward;
            
            if (dist > 0.01f)
            {
                // Raycast evaluates against our wall mask (which now supports both environmental layers)
                if (Physics.Raycast(transform.position, dir, dist, wallLayer)) continue;
            }

            // 3. Process Damage/Effects Delivery
            IDamageable damageable = c.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate falloff (Scale is 1.0 at center, 0.0 at edge)
                float falloff = 1f;
                if (damageByDistance)
                {
                    // Clamp it between 0 and 1 just to be safe
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
                    effects = new List<Effect>(effects), // Pass the list copy
                    type = damageType,
                    attacker = caster != null ? caster : this.gameObject, // Set true attacker to caster if available
                    multiplier = falloff,
                    forceDirection = dir,
                    isExplosion = true
                };

                if (c.TryGetComponent<PushPropagator>(out var propagator))
                {
                    float force = 5f * falloff; // Example force
                    propagator.PropagatePush(dir * force * Time.deltaTime, this.gameObject);
                }

                damageable.TakeDamage(info);
            }
        }
        Destroy(gameObject);
    }
}