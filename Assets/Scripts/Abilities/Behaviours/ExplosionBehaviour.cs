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

    // Computed once in Initialize() - see PlayerStateMachine.GetFallDistanceDamageMultiplier.
    private float fallDistanceMultiplier = 1f;
    private GameObject groundImpactPrefab;

    // Immutable reference for health refunds & energy gain, matching ShockWaveBehaviour/MeleeAttackBehavaiour.
    private Ability sourceAbility;

    public void Initialize(Ability aSourceAbility, BaseAbilitySettings baseSettings, ExplosionSettings explosionSettings, List<Effect> abilityEffects, GameObject whoCasted)
    {
        sourceAbility = aSourceAbility;
        caster = whoCasted;

        effects = abilityEffects;
        explosionParticles = explosionSettings.explosionParticles;
        explosionRadius = explosionSettings.radius;
        myFaction = baseSettings.myFaction;
        damageType = baseSettings.damageType;
        damageByDistance = explosionSettings.damageByDistance;
        audioClip = explosionSettings.soundEffect;
        targetLayer = baseSettings.targetLayer;
        wallLayer = baseSettings.wallLayer;

        fallDistanceMultiplier = caster != null && caster.TryGetComponent<PlayerStateMachine>(out var psm)
            ? psm.GetFallDistanceDamageMultiplier(baseSettings)
            : 1f;
        groundImpactPrefab = baseSettings.scalesWithFallDistance ? baseSettings.groundImpactPrefab : null;

        // Was in Start() - but Start() only ever fires once per component instance, which would leave
        // every pooled reuse of this prefab past the first never actually detonating.
        audioSource = GetComponent<AudioSource>();
        // Wait one frame for values to update
        StartCoroutine(DelayedExplode());
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

        // Matched exactly to explosionRadius (already the ability's fully resolved radius) rather than
        // a separately-computed scale, same reasoning as ShockWaveBehaviour.
        if (groundImpactPrefab != null)
        {
            Vector3 impactPos = AbilityVisualEffects.ResolveGroundImpactPosition(caster, transform.position);
            GameObject impact = Instantiate(groundImpactPrefab, impactPos, Quaternion.identity);
            impact.transform.localScale = Vector3.one * explosionRadius;
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

            // 2. Wall Check - measured against the collider's actual surface, not its (possibly
            // pivot-offset) transform.position, so ground clutter doesn't wrongly occlude a target
            // whose real hitbox is unobstructed, and falloff distance matches the real nearest point.
            Vector3 impactPoint = Ability.GetTrueImpactPoint(c, transform.position, Vector3.zero);
            float dist = Vector3.Distance(transform.position, impactPoint);

            // Avoid NaN errors or division by zero if target is exactly on top of explosion origin
            Vector3 dir = dist > 0.001f ? (impactPoint - transform.position).normalized : transform.forward;

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
                    multiplier = falloff * fallDistanceMultiplier,
                    forceDirection = dir,
                    isExplosion = true,
                    sourceAbility = this.sourceAbility
                };

                if (c.TryGetComponent<PushPropagator>(out var propagator))
                {
                    float force = 5f * falloff; // Example force
                    propagator.PropagatePush(dir * force * Time.deltaTime, this.gameObject);
                }

                damageable.TakeDamage(info);
            }
        }
        Ability.RetireAbilityInstance(gameObject);
    }
}