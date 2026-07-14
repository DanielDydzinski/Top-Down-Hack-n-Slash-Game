using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FireBallBehaviour : MonoBehaviour
{
    [Header("Beam Functionality")]
    public bool isBeam = false;
    public float beamEnginePower = 100f;
    public float pushForceMultiplier = 1f;

    [Header("Original Settings")]
    public GameObject projectile;
    public Ability explosionAbility;
    public AudioClip fireBallAudioClip;
    public string targetTag;
    public string friendTag;
    public Faction myFaction;
    public DamageType damageType;
    public float projectileSpeed;
    public float projectileSize;
    public float projectileRange;
    public GameObject caster;

    [Header("Physics Filtering")]
    public LayerMask targetLayer; // Leverages unified targets from Ability.cs
    public LayerMask wallLayer;   // Leverages unified environment walls from Ability.cs

    public SphereCollider sc;
    public List<Effect> effects = new List<Effect>();

    private Vector3 spawnPos;
    private int howManyExplosions;
    private AudioSource audioSource;
    private float currentEffectiveSpeed;

    // The single list tracking everything via Rigidbody
    private List<Rigidbody> capturedMasses = new List<Rigidbody>();

    // Computed once in Initialize() - see PlayerStateMachine.GetFallDistanceDamageMultiplier.
    private float fallDistanceMultiplier = 1f;
    private float maxDistanceMultiplier = 1f;
    private GameObject groundImpactPrefab;

    // Immutable reference for health refunds & energy gain, matching ShockWaveBehaviour/MeleeAttackBehavaiour.
    private Ability sourceAbility;

    private GameObject trailInstance;

    // Physics.IgnoreCollision is a PERSISTENT pairing at the physics engine level - it survives
    // SetActive(false)/reactivation entirely, unlike everything else on a pooled instance. Track who
    // it was set up for so a later cast by a DIFFERENT caster (sharing this same pooled collider) can
    // undo it - otherwise a fireball first cast by the Player would permanently ignore the Player's
    // own collider forever, even after being reused for an Enemy's cast against that same Player.
    private Collider ignoredCasterCollider;

    void Update()
    {
        if (isBeam) CalculateBeamSpeed();
        else currentEffectiveSpeed = projectileSpeed;

        MoveWithTunnelingSweep(currentEffectiveSpeed * Time.deltaTime);
        DestroyByDistance();
    }

    // A plain transform.Translate can jump clean over a thin/fast target in one big-delta frame
    // (a hitch, or just a fast projectile) without ever generating an OnTriggerEnter - Unity's
    // trigger system only checks whether colliders overlap at their CURRENT position each physics
    // step, it never considers the path a non-Rigidbody transform took to get there. Sweep the
    // intended step first and, if something's in the way, stop right at contact instead of past
    // it, so the very next physics step's overlap check (and the existing OnTriggerEnter logic,
    // unchanged) actually gets a chance to see it.
    private void MoveWithTunnelingSweep(float step)
    {
        if (step <= 0f) return;

        Vector3 direction = transform.forward;
        float sweepRadius = Mathf.Max(projectileSize, 0.05f);
        int hitMask = targetLayer | wallLayer;

        if (Physics.SphereCast(transform.position, sweepRadius, direction, out RaycastHit hit, step, hitMask))
        {
            // Push slightly past the contact point (not just to it) so the collider is left
            // genuinely overlapping, not merely touching - trigger events need real penetration,
            // not tangency, to fire.
            float travelDistance = Mathf.Min(hit.distance + 0.05f, step);
            transform.position += direction * travelDistance;
        }
        else
        {
            transform.position += direction * step;
        }
    }

    private void CalculateBeamSpeed()
    {
        float totalMass = 0;
        capturedMasses.RemoveAll(rb => rb == null);
        foreach (Rigidbody rb in capturedMasses) totalMass += rb.mass;
        float weightFactor = beamEnginePower / (beamEnginePower + totalMass);
        currentEffectiveSpeed = projectileSpeed * weightFactor;
    }

    void OnTriggerEnter(Collider col)
    {
        // REFACTORED: Bitmask check replacing the hardcoded "Enviroment" string lookups
        if (((1 << col.gameObject.layer) & wallLayer) != 0)
        {
            if (explosionAbility != null && howManyExplosions < 1)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                howManyExplosions++;
            }
            ReleaseChildrenBeforeRetire();

            Ability.RetireAbilityInstance(this.gameObject);
            return;
        }

        // REFACTORED: Unified bitmask check against the flexible target layer
        if (((1 << col.gameObject.layer) & targetLayer) != 0)
        {
            EntityIdentity victimIdentity = col.GetComponent<EntityIdentity>();
            if (victimIdentity != null && victimIdentity.faction == myFaction) return;

            if (isBeam)
            {
                if (col.TryGetComponent<Rigidbody>(out var rb))
                {
                    CaptureEnemy(rb);
                }
            }
            else if (howManyExplosions < 1) // Original single fireball logic
            {
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    HitInfo info = new HitInfo
                    {
                        faction = myFaction,
                        multiplier = 1.0f * fallDistanceMultiplier,
                        type = damageType,
                        effects = this.effects,
                        attacker = caster != null ? caster : this.gameObject,
                        attackType = AttackType.Ranged,
                        isExplosion = false,
                        sourceAbility = this.sourceAbility
                    };
                    damageable.TakeDamage(info);
                }

                // No radius of its own to match (this is a point-hit projectile, not an AoE sphere) -
                // just the shared proportional fall-distance scale, same as ShockWave's own.
                if (groundImpactPrefab != null)
                {
                    Vector3 impactPos = AbilityVisualEffects.ResolveGroundImpactPosition(caster, transform.position);
                    GameObject impact = Instantiate(groundImpactPrefab, impactPos, Quaternion.identity);
                    impact.transform.localScale = Vector3.one * Ability.GetVisualFallDistanceScale(fallDistanceMultiplier, maxDistanceMultiplier);
                }

                if (explosionAbility != null)
                {
                    explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                }

                howManyExplosions++;
                ReleaseChildrenBeforeRetire();
                Ability.RetireAbilityInstance(this.gameObject);
            }
        }
    }

    private void DealDamage()
    {

    }

    void OnTriggerStay(Collider col)
    {
        if (!isBeam) return;

        CheckForEnvironmentWallAhead();

        // REFACTORED: Continuous stun evaluation matching the chosen targetLayer setup
        if (((1 << col.gameObject.layer) & targetLayer) != 0)
        {
            if (col.TryGetComponent<EnemyAIController>(out var controller))
            {
                // Only apply stun if the enemy isn't already actively stunned
                if (!controller.IsCurrentlyStunned)
                {
                    controller.ApplyStun(1.0f);
                }
            }
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (isBeam && col.TryGetComponent<Rigidbody>(out var rb))
        {
            ReleaseEnemy(rb);
        }
    }

    private void CaptureEnemy(Rigidbody rb)
    {
        if (capturedMasses.Contains(rb)) return;

        capturedMasses.Add(rb);

        if (rb.gameObject.TryGetComponent<EnemyAIController>(out var controller))
        {
            controller.ApplyStun(1.0f);
        }
        if (rb.gameObject.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.enabled = false;
        }

        // Parent directly to the fireball beam for a zero-jitter, smooth sweep
        rb.transform.SetParent(this.transform, true);
    }

    private void ReleaseEnemy(Rigidbody rb)
    {
        if (rb == null) return;

        rb.transform.SetParent(null);

        if (rb.gameObject.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            agent.enabled = true;
            if (UnityEngine.AI.NavMesh.SamplePosition(rb.transform.position, out UnityEngine.AI.NavMeshHit hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        if (capturedMasses.Contains(rb))
        {
            capturedMasses.Remove(rb);
        }
    }

    private void ReleaseAllCaptured()
    {
        for (int i = capturedMasses.Count - 1; i >= 0; i--)
        {
            ReleaseEnemy(capturedMasses[i]);
        }
        capturedMasses.Clear();
    }

    private void DestroyByDistance()
    {
        if (Vector3.Distance(spawnPos, transform.position) > projectileRange)
        {
            if (explosionAbility != null && howManyExplosions < 1)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                howManyExplosions++;
            }
            ReleaseChildrenBeforeRetire();
            Ability.RetireAbilityInstance(this.gameObject);
        }
    }

    private void CheckForEnvironmentWallAhead()
    {
        if (!isBeam) return;

        float checkDistance = projectileSize + 1f;

        // REFACTORED: Uses our accurate wallLayer configuration instead of custom bit-shifting single names
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, checkDistance, wallLayer))
        {
            // Hit a wall ahead! Detonate right now before pushing enemies through it
            if (explosionAbility != null && howManyExplosions < 1)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                howManyExplosions++;
            }

            ReleaseChildrenBeforeRetire();
            Ability.RetireAbilityInstance(this.gameObject);
        }
    }

    // Must run BEFORE this projectile itself gets deactivated/retired, never from OnDisable - Unity
    // refuses to reparent a child (the trail, or a captured beam enemy) while its parent is itself in
    // the middle of activating/deactivating, which is exactly what OnDisable would be doing here.
    // Every retire call site below calls this first, then hands the projectile itself to the pool.
    private void ReleaseChildrenBeforeRetire()
    {
        ReleaseAllCaptured();

        // Detach and hand the trail back to its own pool while this projectile is still fully active -
        // otherwise it's gone for good instead of being reused next cast.
        if (trailInstance != null && ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(trailInstance);
            trailInstance = null;
        }
    }

    public void Initialize(Ability aSourceAbility, BaseAbilitySettings baseSettings, FireBallSettings fireBallSettings, List<Effect> abilityEffects, GameObject whoCasted)
    {
        sourceAbility = aSourceAbility;
        caster = whoCasted;

        effects = abilityEffects;
        projectileSpeed = fireBallSettings.projectileSpeed;
        projectileSize = fireBallSettings.projectileSize;
        projectileRange = fireBallSettings.projectileRange;
        projectile = fireBallSettings.projectilePrefab;
        explosionAbility = fireBallSettings.explosionAbility;
        myFaction = baseSettings.myFaction;
        damageType = baseSettings.damageType;
        fireBallAudioClip = fireBallSettings.soundEffect;
        isBeam = fireBallSettings.isBeam;
        targetLayer = baseSettings.targetLayer;
        wallLayer = baseSettings.wallLayer;

        fallDistanceMultiplier = caster != null && caster.TryGetComponent<PlayerStateMachine>(out var psm)
            ? psm.GetFallDistanceDamageMultiplier(baseSettings)
            : 1f;
        maxDistanceMultiplier = baseSettings.maxDistanceMultiplier;
        groundImpactPrefab = baseSettings.scalesWithFallDistance ? baseSettings.groundImpactPrefab : null;

        // Was in Start() - but Start() only ever fires once per component instance, and this pooled
        // prefab is shared by both beam (Kamehameha) and non-beam (FireBall/IceBall) abilities, so
        // every one of these has to be redone on every cast, not just the very first spawn ever.
        howManyExplosions = 0;
        spawnPos = this.transform.position;
        currentEffectiveSpeed = projectileSpeed;

        // Idempotent both ways round - a pooled instance may currently be wearing the OTHER mode's
        // collider from whichever ability cast it last, since the same prefab is shared across
        // beam/non-beam abilities (see FireBallPrefab.prefab's shared guid across Kamehameha/FireBall).
        if (isBeam)
        {
            SphereCollider staleSphere = GetComponent<SphereCollider>();
            if (staleSphere != null) Destroy(staleSphere);
            sc = null;

            BoxCollider bc = GetComponent<BoxCollider>();
            if (bc == null) bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(projectileSize * 2f, projectileSize, projectileSize);
        }
        else
        {
            BoxCollider staleBox = GetComponent<BoxCollider>();
            if (staleBox != null) Destroy(staleBox);

            sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = projectileSize;
            sc.isTrigger = true;
        }

        {
            Collider casterCollider = caster != null ? caster.GetComponent<CharacterController>() : null;
            Collider fireballCollider = GetComponent<Collider>();

            if (ignoredCasterCollider != null && ignoredCasterCollider != casterCollider)
            {
                Physics.IgnoreCollision(ignoredCasterCollider, fireballCollider, false);
                ignoredCasterCollider = null;
            }

            if (casterCollider != null && fireballCollider != null)
            {
                Physics.IgnoreCollision(casterCollider, fireballCollider);
                ignoredCasterCollider = casterCollider;
            }
        }

        audioSource = GetComponent<AudioSource>();
        StartCoroutine(DelayedInstanciate());
    }

    private IEnumerator DelayedInstanciate()
    {
        yield return null;
        if (projectile != null)
        {
            trailInstance = ObjectPooler.Instance != null
                ? ObjectPooler.Instance.SpawnFromPool(projectile, transform.position, transform.rotation, this.transform)
                : Instantiate(projectile, this.gameObject.transform);
            if (fireBallAudioClip != null && audioSource != null) audioSource.PlayOneShot(fireBallAudioClip);
        }
    }
}