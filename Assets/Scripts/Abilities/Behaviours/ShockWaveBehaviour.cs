using System.Collections;
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

    private SphereCollider sc;
    private float currentRadius = 0f;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    // Dynamic list to hold instances of all child materials found
    private List<Material> childMaterials = new List<Material>();
    private float startFadeValue = -0.1f; // Matches your AllIn1Vfx asset default
    private float targetFadeValue = 1.0f;  // Fully dissolved

    void Start()
    {
        sc = GetComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0f;

        if (visualParticles != null)
        {
            activeVisual = Instantiate(visualParticles, transform.position, Quaternion.identity);
            activeVisual.transform.localScale = Vector3.zero;

            // 1. Find ALL Renderers underneath the instantiated visual (Base, Shield, etc.)
            Renderer[] renderers = activeVisual.GetComponentsInChildren<Renderer>();

            foreach (Renderer rend in renderers)
            {
                // .material automatically creates a local instance copy so we don't alter the project asset permanently
                Material mat = rend.material;

                // Initialize the property safely
                mat.SetFloat("_FadeAmount", startFadeValue);
                childMaterials.Add(mat);
            }
        }
    }

    void Update()
    {
        if (currentRadius < maxRadius)
        {
            // Expand physics boundaries and game object transform bounds
            currentRadius += expansionSpeed * Time.deltaTime;
            sc.radius = currentRadius;
            activeVisual.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);

            // Linear conversion tracking how close the wave is to expiring (0.0 to 1.0)
            float progress = currentRadius / maxRadius;
            float currentFade = Mathf.Lerp(startFadeValue, targetFadeValue, progress);

            // 2. Drive the dissolve property across every single child material found
            for (int i = 0; i < childMaterials.Count; i++)
            {
                if (childMaterials[i] != null)
                {
                    childMaterials[i].SetFloat("_FadeAmount", currentFade);
                }
            }
        }
        else
        {
            // Total expiration cleanup
            Destroy(activeVisual);
            Destroy(gameObject);
        }
    }

    public void UpdateValues(List<Effect> aeffects, float aMaxRadius, float aExpansionSpeed, Faction faction, DamageType dmgType, GameObject whoCasted, GameObject visualWave)
    {
        effects = aeffects;
        maxRadius = aMaxRadius;
        expansionSpeed = aExpansionSpeed;
        myFaction = faction;
        damageType = dmgType;
        caster = whoCasted;
        visualParticles = visualWave;
    }

    void OnTriggerEnter(Collider col)
    {
        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable == null || hitTargets.Contains(damageable)) return;

        EntityIdentity victimIdentity = col.GetComponent<EntityIdentity>();
        if (victimIdentity != null && victimIdentity.faction == myFaction) return;

        Vector3 dir = col.transform.position - transform.position;
        hitTargets.Add(damageable);

        // --- FALLOFF MULTIPLIER CALCULATION ---
        // Calculate actual distance to the target
        float distance = dir.magnitude;

        // Normalize it between 0.0 (center) and 1.0 (max edge)
        // Mathf.Clamp01 ensures we don't get weird numbers if a collider bounds check behaves oddly
        float normalizedDistance = Mathf.Clamp01(distance / maxRadius);

        // Linear Falloff: 1.0 at the center, dropping down toward 0.0 at max radius
        float falloffMultiplier = 1f - normalizedDistance;

        // Optional: If you don't want it to hit for literally 0 damage at the absolute edge, 
        // you can clamp a minimum baseline (e.g., it always does at least 25% damage/force):
        // falloffMultiplier = Mathf.Clamp(falloffMultiplier, 0.25f, 1.0f);
        // --------------------------------------

        HitInfo info = new HitInfo
        {
            overrideDeathType = DeathHandler.DeathType.Ragdoll,
            faction = myFaction,
            multiplier = falloffMultiplier, // <-- Utilizing  multiplier slot here /lower dmg and force impact by distance
            type = damageType,
            effects = this.effects,
            attacker = caster,
            isExplosion = false,            
            forceDirection = dir.normalized // Normalized direction vector is cleaner for physics calculations
            
        };

        damageable.TakeDamage(info);
    }
}