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
    private GameObject groundImpactPrefab;

    // Immutable reference for health refunds & energy gain, matching ShockWaveBehaviour/MeleeAttackBehavaiour.
    private Ability sourceAbility;

    void Start()
    {
        howManyExplosions = 0;
        spawnPos = this.transform.position;
        currentEffectiveSpeed = projectileSpeed;

        if (isBeam)
        {
            SphereCollider oldSc = GetComponent<SphereCollider>();
            if (oldSc != null) Destroy(oldSc);

            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(projectileSize * 2f, projectileSize, projectileSize);
        }
        else
        {
            sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = projectileSize;
            sc.isTrigger = true;
        }

        if (caster != null)
        {
            CharacterController playerCC = caster.GetComponent<CharacterController>();
            Collider fireballCollider = GetComponent<Collider>();

            if (playerCC != null && fireballCollider != null)
            {
                Physics.IgnoreCollision(playerCC, fireballCollider);
            }
        }

        audioSource = GetComponent<AudioSource>();
        StartCoroutine(DelayedInstanciate());
    }

    void Update()
    {
        if (isBeam) CalculateBeamSpeed();
        else currentEffectiveSpeed = projectileSpeed;

        transform.Translate(Vector3.forward * currentEffectiveSpeed * Time.deltaTime);
        DestroyByDistance();
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
            ReleaseAllCaptured();

            Destroy(this.gameObject);
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
                // just the shared capped fall-distance factor, same as ShockWave's own cap.
                if (groundImpactPrefab != null)
                {
                    GameObject impact = Instantiate(groundImpactPrefab, transform.position, Quaternion.identity);
                    impact.transform.localScale = Vector3.one * Mathf.Min(fallDistanceMultiplier, Ability.MaxFallDistanceVisualScale);
                }

                if (explosionAbility != null)
                {
                    explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                }

                howManyExplosions++;
                Destroy(this.gameObject);
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
            ReleaseAllCaptured();
            Destroy(this.gameObject);
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

            ReleaseAllCaptured();
            Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        ReleaseAllCaptured();
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
        groundImpactPrefab = baseSettings.scalesWithFallDistance ? baseSettings.groundImpactPrefab : null;
    }

    private IEnumerator DelayedInstanciate()
    {
        yield return null;
        if (projectile != null)
        {
            Instantiate(projectile, this.gameObject.transform);
            if (fireBallAudioClip != null && audioSource != null) audioSource.PlayOneShot(fireBallAudioClip);
        }
    }
}