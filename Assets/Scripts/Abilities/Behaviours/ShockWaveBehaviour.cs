using System.Collections.Generic;
using UnityEngine;

public class ShockWaveBehaviour : MonoBehaviour
{
    [Header("Runtime Variables")]
    public float maxRadius = 10f;
    public float expansionSpeed = 20f;
    public Faction myFaction;
    public DamageType damageType;
    public List<Effect> effects = new List<Effect>();
    public GameObject caster;
    private GameObject visualParticles;
    private GameObject activeVisual;

    // --- IMMUTABLE REFERENCE FOR HEALTH REFUNDS ---
    private Ability sourceAbility;

    [Header("Physics Filtering & Occlusion")]
    public LayerMask targetLayer;
    public LayerMask wallLayer;
    public bool useLineOfSight = true;

    private SphereCollider sc;
    private List<IDamageable> hitTargets = new List<IDamageable>();

    // FIXED: Using your unified Initialize pattern instead of the old UpdateValues
    public void Initialize(BaseAbilitySettings baseSettings, ShockWaveSettings shockSettings, List<Effect> abilityEffects, GameObject whoCasted)
    {
        caster = whoCasted;

        // Extracting data directly out of your new clean containers
        myFaction = baseSettings.myFaction;
        damageType = baseSettings.damageType;
        targetLayer = baseSettings.targetLayer;
        wallLayer = baseSettings.wallLayer;

        maxRadius = shockSettings.maxRadius;
        expansionSpeed = shockSettings.expansionSpeed;
        useLineOfSight = shockSettings.useLOS;
        visualParticles = shockSettings.shockWaveVisualPrefab;

        effects = abilityEffects;

        // CRITICAL: We find the active ability instance from the player's manager to track refunds!
        if (caster != null)
        {
            AbilityManager manager = caster.GetComponent<AbilityManager>();
            if (manager != null)
            {
                sourceAbility = manager.activeAbility;
            }
        }

        // Spawn visual effects if applicable
        if (visualParticles != null)
        {
            activeVisual = Instantiate(visualParticles, transform.position, transform.rotation, transform);
        }

        sc = GetComponent<SphereCollider>();
        if (sc != null) sc.radius = 0f;
    }

    void OnTriggerEnter(Collider col)
    {
        if (((1 << col.gameObject.layer) & targetLayer) == 0) return;

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable == null || hitTargets.Contains(damageable)) return;

        EntityIdentity victimIdentity = col.GetComponent<EntityIdentity>();
        if (victimIdentity != null && victimIdentity.faction == myFaction) return;

        Vector3 dir = col.transform.position - transform.position;
        float distance = dir.magnitude;

        if (useLineOfSight && distance > 0.01f)
        {
            if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit wallHit, distance, wallLayer))
            {
                if (wallHit.collider != col)
                {
                    return;
                }
            }
        }

        hitTargets.Add(damageable);

        float normalizedDistance = Mathf.Clamp01(distance / maxRadius);
        float falloffMultiplier = 1f - normalizedDistance;

        HitInfo info = new HitInfo
        {
            overrideDeathType = DeathHandler.DeathType.Ragdoll,
            faction = myFaction,
            multiplier = falloffMultiplier,
            type = damageType,
            effects = this.effects,
            attacker = caster,
            isExplosion = false,
            forceDirection = dir.normalized,

            // FIXED: Compiled flawlessly because sourceAbility is declared and populated above!
            sourceAbility = this.sourceAbility
        };

        damageable.TakeDamage(info);
    }

    void Update()
    {
        if (sc != null && sc.radius < maxRadius)
        {
            sc.radius += expansionSpeed * Time.deltaTime;
        }
        else
        {
            Destroy(gameObject, 0.5f); // Let visual trails finish fading
        }
    }
}