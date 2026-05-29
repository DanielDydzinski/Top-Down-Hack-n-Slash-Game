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
            // Fallback to guarantee audio won't crash if an AudioSource isn't on the prefab base
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

        // Play core ambient sound if provided
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
        // 1. Determine ground target point within the circle area
        Vector2 randomCirclePoint = Random.insideUnitCircle * radius;
        Vector3 groundStrikePosition = transform.position + new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y);

        // 2. Compute visual creation point by applying your custom offset vector
        Vector3 visualSpawnPosition = groundStrikePosition + spawnPositionOffset;
        visualSpawnPosition.y = 0f;

        // 3. Spawn the localized projectile/impact effect
        if (strikeParticles != null)
        {
            Instantiate(strikeParticles, visualSpawnPosition, Quaternion.identity);
        }

        // 4. Play the localized sound element directly at the target location
        if (strikeSound != null)
        {
            AudioSource.PlayClipAtPoint(strikeSound, groundStrikePosition);
        }

        // 5. Query physics collision overlap from the flat impact ground zero
        Collider[] targets = Physics.OverlapSphere(groundStrikePosition, strikeRadius);
        ApplyDamageToTargets(targets, groundStrikePosition);
    }

    private void PerformFullAreaTick()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, radius);
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

                damageable.TakeDamage(info);
            }
        }
    }

    public void UpdateValues(List<Effect> aeffects, float aduration, float arate, float aradius,
                             Faction faction, GameObject aparticles, DamageType dmgType,
                             AudioClip aclip, GameObject aCaster, bool aisPartial,
                             float astrikeRadius, GameObject astrikeParticles,
                             Vector3 aspawnOffset, AudioClip astrikeSound)
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
    }
}