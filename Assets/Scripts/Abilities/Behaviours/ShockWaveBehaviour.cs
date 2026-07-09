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

    // --- IMMUTABLE REFERENCE FOR HEALTH REFUNDS & ENERGY GAIN ---
    private Ability sourceAbility;

    // Computed once in Initialize() - see PlayerStateMachine.GetFallDistanceDamageMultiplier.
    private float fallDistanceMultiplier = 1f;

    [Header("Physics Filtering & Occlusion")]
    public LayerMask targetLayer;
    public LayerMask wallLayer;
    public bool useLineOfSight = true;

    private SphereCollider sc;
    private List<IDamageable> hitTargets = new List<IDamageable>();

    // Shader/Material property cache for clean fading
    private List<Material> childMaterials = new List<Material>();
    private float currentFade = 0f;

    // FIXED: Keeps the direct master reference for gain-on-hit tracking
    public void Initialize(Ability aSourceAbility, BaseAbilitySettings baseSettings, ShockWaveSettings shockSettings, List<Effect> abilityEffects, GameObject whoCasted)
    {
        caster = whoCasted;
        sourceAbility = aSourceAbility;

        fallDistanceMultiplier = caster != null && caster.TryGetComponent<PlayerStateMachine>(out var psm)
            ? psm.GetFallDistanceDamageMultiplier(baseSettings)
            : 1f;

        myFaction = baseSettings.myFaction;
        damageType = baseSettings.damageType;
        targetLayer = baseSettings.targetLayer;
        wallLayer = baseSettings.wallLayer;

        maxRadius = shockSettings.maxRadius;
        if (shockSettings.scaleRadiusWithFallDistance)
        {
            // Independent hard cap from maxDistanceMultiplier (which only governs damage) - radius
            // never scales past double the base maxRadius regardless of how the damage bonus is tuned.
            maxRadius *= Mathf.Min(fallDistanceMultiplier, Ability.MaxFallDistanceVisualScale);
        }

        Debug.Log($"[GroundImpact] {aSourceAbility?.name}: baseRadius={shockSettings.maxRadius:F2} scaleRadiusWithFallDistance={shockSettings.scaleRadiusWithFallDistance} fallDistanceMultiplier={fallDistanceMultiplier:F2} finalMaxRadius={maxRadius:F2} scalesWithFallDistance={baseSettings.scalesWithFallDistance} groundImpactPrefabAssigned={baseSettings.groundImpactPrefab != null}");

        expansionSpeed = shockSettings.expansionSpeed;
        useLineOfSight = shockSettings.useLOS;
        visualParticles = shockSettings.shockWaveVisualPrefab;

        effects = abilityEffects;

        // Ground impact matched exactly to the (already fall-scaled) blast radius above, rather than
        // a separately-computed scale - keeps the visual honest about how big the actual damage sphere is.
        if (baseSettings.scalesWithFallDistance && baseSettings.groundImpactPrefab != null)
        {
            GameObject impact = Instantiate(baseSettings.groundImpactPrefab, transform.position, Quaternion.identity);
            impact.transform.localScale = Vector3.one * maxRadius;
        }

        // Spawn visual effects if applicable
        if (visualParticles != null)
        {
            activeVisual = Instantiate(visualParticles, transform.position,Quaternion.identity);

            // Cache all renderers on the visual instance to alter the material properties safely
            Renderer[] renderers = activeVisual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                childMaterials.AddRange(r.materials);
            }
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
            multiplier = falloffMultiplier * fallDistanceMultiplier,
            type = damageType,
            effects = this.effects,
            attacker = caster,
            isExplosion = false,
            forceDirection = dir.normalized,
            sourceAbility = this.sourceAbility
        };

        damageable.TakeDamage(info);
    }

    void Update()
    {
        if (sc != null && sc.radius < maxRadius)
        {
            // 1. Expand the physical collider
            sc.radius += expansionSpeed * Time.deltaTime;

            // 2. Sync the active mesh visual scale with the expanding radius
            if (activeVisual != null)
            {
                float currentDiameter = sc.radius;
                activeVisual.transform.localScale = new Vector3(currentDiameter, currentDiameter, currentDiameter);
            }

            // 3. DYNAMIC FADE: Calculate exact percentage from 0 to 1 based on expansion progress
            if (maxRadius > 0.01f && childMaterials.Count > 0)
            {
                currentFade = sc.radius / maxRadius; // Dynamic ratio!
                Debug.Log( "Current fade == "  +currentFade);

                if (currentFade > 0)
                {
                    for (int i = 0; i < childMaterials.Count; i++)
                    {
                        if (childMaterials[i] != null)
                        {
                            childMaterials[i].SetFloat("_FadeAmount", currentFade);
                        }
                    }
                }
            }
        }
        else
        {
            // Clean up objects instantly since it reached max radius and is fully transparent
            if (activeVisual != null) Destroy(activeVisual);
            Destroy(gameObject);
        }
    }
}