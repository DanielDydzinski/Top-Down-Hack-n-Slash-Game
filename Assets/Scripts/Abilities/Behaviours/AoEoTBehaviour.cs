using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AoEoTBehaviour : MonoBehaviour
{
    [Header("Core Settings")]
    public GameObject particles;
    public float duration;
    public float rate;
    public float radius;
    public Faction myFaction;
    public DamageType damageType;
    public List<Effect> effects = new List<Effect>();

    [Header("Physics Filtering")]
    public LayerMask targetLayer; // Leverages unified targets from Ability.cs
    public LayerMask wallLayer;   // Kept for structural signature unity

    [Header("Partial / Random Strike Settings")]
    public bool isPartial;
    public float strikeRadius;
    public GameObject strikeParticles;
    public Vector3 spawnPositionOffset; // Controls visual spawn adjustment (e.g., height)
    public AudioClip strikeSound;       // Audio clip played per localized strike

    private float lifeTimer = 0f;
    private float tickTimer = 0f;
    private SphereCollider sc;

    private AudioClip audioClip; // Core looping/ambient storm sound
    private AudioSource audioSource;
    private GameObject caster;

    // Computed once in Initialize() - see PlayerStateMachine.GetFallDistanceDamageMultiplier. Applied to
    // every tick, same as TakeDoT caching isBlocked once for its whole tick loop - the fall that triggered
    // this ability doesn't change mid-DoT.
    private float fallDistanceMultiplier = 1f;
    private GameObject groundImpactPrefab;

    // Immutable reference for health refunds & energy gain, matching ShockWaveBehaviour/MeleeAttackBehavaiour.
    private Ability sourceAbility;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        StartCoroutine(DelayedStart());
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (tickTimer >= rate)
        {
            tickTimer = 0f;
            Tick();
        }

        if (lifeTimer >= duration) Destroy(gameObject);
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        sc = GetComponent<SphereCollider>();
        if (sc != null) sc.radius = radius;

        if (particles != null) Instantiate(particles, transform);

        // Matched exactly to radius (already the ability's fully resolved radius) rather than a
        // separately-computed scale, same reasoning as ShockWaveBehaviour. Spawned once here rather
        // than per-tick since the fall that triggered this ability doesn't change mid-DoT.
        if (groundImpactPrefab != null)
        {
            GameObject impact = Instantiate(groundImpactPrefab, transform.position, Quaternion.identity);
            impact.transform.localScale = Vector3.one * radius;
        }

        if (audioClip != null && audioSource != null)
        {
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void Tick()
    {
        if (isPartial)
        {
            PerformRandomStrike();
        }
        else
        {
            PerformFullAreaTick();
        }
    }

    private void PerformRandomStrike()
    {
        Vector2 randomCirclePoint = Random.insideUnitCircle * radius;
        Vector3 groundStrikePosition = transform.position + new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y);

        // Calculate visual spawn position without snapping Y modification completely down to zero on early declarations
        Vector3 visualSpawnPosition = groundStrikePosition + spawnPositionOffset;

        if (strikeParticles != null)
        {
            Instantiate(strikeParticles, visualSpawnPosition, Quaternion.identity);
        }

        if (strikeSound != null)
        {
            AudioSource.PlayClipAtPoint(strikeSound, groundStrikePosition);
        }

        // OPTIMIZED: Uses targetLayer to ignore checking non-target environment debris
        Collider[] targets = Physics.OverlapSphere(groundStrikePosition, strikeRadius, targetLayer);
        ApplyDamageToTargets(targets, groundStrikePosition);
    }

    private void PerformFullAreaTick()
    {
        // OPTIMIZED: Uses targetLayer to skip processing non-combat layers entirely
        Collider[] targets = Physics.OverlapSphere(transform.position, radius, targetLayer);
        ApplyDamageToTargets(targets, transform.position);
    }

    private void ApplyDamageToTargets(Collider[] targets, Vector3 originCenter)
    {
        foreach (Collider c in targets)
        {
            EntityIdentity identity = c.GetComponent<EntityIdentity>();
            if (identity != null && identity.faction == myFaction) continue;

            IDamageable damageable = c.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate explosion push vector away from impact center (ignoring Y height changes)
                Vector3 pushDirection = c.transform.position - originCenter;
                pushDirection.y = 0f;

                HitInfo info = new HitInfo
                {
                    multiplier = 1.0f * fallDistanceMultiplier,
                    type = damageType,
                    effects = this.effects,
                    attacker = caster != null ? caster : this.gameObject,
                    faction = this.myFaction,
                    forceDirection = pushDirection.normalized, // Dynamic radial vector out from the blast
                    isExplosion = true,
                    sourceAbility = this.sourceAbility
                };

                // Targets will take damage freely even if separated by environment/walls
                damageable.TakeDamage(info);
            }
        }
    }

    public void Initialize(Ability aSourceAbility, BaseAbilitySettings baseSettings, AoEoTSettings aoEotSettings, List<Effect> abilityEffects, GameObject whoCasted)
    {
        sourceAbility = aSourceAbility;
        caster = whoCasted;

        effects = abilityEffects;
        duration = aoEotSettings.duration;
        rate = aoEotSettings.rate;
        radius = aoEotSettings.radius;
        myFaction = baseSettings.myFaction;
        particles = aoEotSettings.abilityParticles;
        damageType = baseSettings.damageType;
        audioClip = baseSettings.audioClip;

        isPartial = aoEotSettings.isPartial;
        strikeRadius = aoEotSettings.strikeRadius;
        strikeParticles = aoEotSettings.strikeParticles;
        spawnPositionOffset = aoEotSettings.spawnPositionOffset;
        strikeSound = aoEotSettings.strikeSound;

        targetLayer = baseSettings.targetLayer;
        wallLayer = baseSettings.wallLayer;

        fallDistanceMultiplier = caster != null && caster.TryGetComponent<PlayerStateMachine>(out var psm)
            ? psm.GetFallDistanceDamageMultiplier(baseSettings)
            : 1f;
        groundImpactPrefab = baseSettings.scalesWithFallDistance ? baseSettings.groundImpactPrefab : null;
    }
}