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
                    multiplier = 1.0f,
                    type = damageType,
                    effects = this.effects,
                    attacker = caster != null ? caster : this.gameObject,
                    faction = this.myFaction,
                    forceDirection = pushDirection.normalized, // Dynamic radial vector out from the blast
                    isExplosion = true
                };

                // Targets will take damage freely even if separated by environment/walls
                damageable.TakeDamage(info);
            }
        }
    }

    // UPDATED: Kept signature intact so your ability manager can pass configurations identically
    public void UpdateValues(List<Effect> aeffects, float aduration, float arate, float aradius,
                             Faction faction, GameObject aparticles, DamageType dmgType,
                             AudioClip aclip, GameObject aCaster, bool aisPartial,
                             float astrikeRadius, GameObject astrikeParticles,
                             Vector3 aspawnOffset, AudioClip astrikeSound, LayerMask aTargetLayer, LayerMask aWallLayer)
    {
        effects = aeffects;
        duration = aduration;
        rate = arate;
        radius = aradius;
        myFaction = faction;
        particles = aparticles;
        damageType = dmgType;
        audioClip = aclip;
        caster = aCaster;

        isPartial = aisPartial;
        strikeRadius = astrikeRadius;
        strikeParticles = astrikeParticles;
        spawnPositionOffset = aspawnOffset;
        strikeSound = astrikeSound;

        targetLayer = aTargetLayer;
        wallLayer = aWallLayer; // Stored safely to avoid broken references elsewhere
    }
}