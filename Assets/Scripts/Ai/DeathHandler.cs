using UnityEngine;
using UnityEngine.AI;

public class DeathHandler : MonoBehaviour
{
    public enum DeathType
    {
        AnimationOnly,
        Ragdoll,
        ExplodingRagdoll
    }

    [Header("Default & Override Settings")]
    [Tooltip("Fallback style used if the incoming hit calculation falls out of expected threshold bounds.")]
    [SerializeField] private DeathType defaultDeathStyle = DeathType.Ragdoll;

    [Header("Intensity Thresholds (Damage * Multiplier)")]
    [Tooltip("Any attack dealing total damage BELOW this value evaluates as a Low-Intensity hit.")]
    [SerializeField] private float mediumIntensityThreshold = 20f;
    [Tooltip("Any attack dealing total damage EQUAL OR ABOVE this value evaluates as a High-Intensity hit. Anything in-between is Medium-Intensity.")]
    [SerializeField] private float highIntensityThreshold = 60f;

    [Header("Probability Chances (0% to 100%)")]
    [Tooltip("Percentage chance that a Low-Intensity hit executes an animated clip. Failing this roll forces a standard Ragdoll instead.")]
    [Range(0f, 100f)]
    [SerializeField] private float animationChance = 70f;
    [Tooltip("Percentage chance that a High-Intensity hit completely vaporizes into parts. Failing this roll forces a standard Ragdoll instead.")]
    [Range(0f, 100f)]
    [SerializeField] private float explodingChance = 70f;

    [Header("Ragdoll Assets")]
    [SerializeField] private GameObject ragdollPrefab;

    [Header("parts Explo Sound")]
    [SerializeField] private AudioClip[] partsExploSound;

    [Header("Health Bar Ref")]
    public GameObject healthBar;

    [Header("Standard Physics (Punch)")]
    [Tooltip("Linear kinetic force applied directly down the incoming attack path to the ragdoll.")]
    [SerializeField] private float standardPunchForce = 15f;

    [Header("Explosion Physics")]
    [Tooltip("Radial force pushing outward away from the epicenter of an explosion.")]
    [SerializeField] private float explosionBlastForce = 500f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float upwardModifier = 0.5f;

    [Header("Explosion Settings (Exploding Style)")]
    [Tooltip("The standalone physics chunk model that breaks apart instantly on impact.")]
    [SerializeField] private GameObject explodingPartsPrefab;

    [Header("Lifespan & Delay Constraints")]
    [Tooltip("Time before the physics ragdoll or broken chunks are deleted to clear memory.")]
    [SerializeField] private float ragdollLifespan = 10f;
    [Tooltip("Time the original frame stays alive to finish playing audio/particles before total deletion.")]
    [SerializeField] private float audioCleanupDelay = 3.0f;
    [Tooltip("Lifespan duration if the actor executes an animated death sequence instead of a ragdoll.")]
    [SerializeField] private float animationDeathLifespan = 4.0f;

    private Health healthComponent;
    private EnemyAIController enemyController;
    private bool _hasDied = false;

    void Awake()
    {
        healthComponent = GetComponent<Health>();
        enemyController = GetComponent<EnemyAIController>();
        healthBar = transform.Find("HPCanvas")?.gameObject;
    }

    void OnEnable()
    {
        if (healthComponent != null) healthComponent.OnDeath += ProcessDeath;
    }

    void OnDisable()
    {
        if (healthComponent != null) healthComponent.OnDeath -= ProcessDeath;
    }

    private void ProcessDeath(HitInfo info)
    {
        if (_hasDied) return;
        _hasDied = true;

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.TriggerSlowMotion();
        }

        if (enemyController != null)
        {
            enemyController.CleanUpSlot();
        }

        EffectManager em = GetComponent<EffectManager>();
        if (em != null) em.CleanUpAllEffects();
        em.enabled = false;

        // 1. Determine death state based on damage thresholds AND random probability checks
        DeathType finalDeathStyle = CalculateHitIntensity(info);

        // 2. Cascading Asset Fallback Verification
        // If Exploding style is chosen but the prefab asset is empty, downgrade safely to standard Ragdoll
        if (finalDeathStyle == DeathType.ExplodingRagdoll && explodingPartsPrefab == null)
        {
            Debug.LogWarning($"<b>[DeathHandler]</b> explodingPartsPrefab is unassigned on {gameObject.name}! Falling back to Ragdoll style.", gameObject);
            finalDeathStyle = DeathType.Ragdoll;
        }

        // If Ragdoll style is chosen but its asset prefab is empty, downgrade safely to Animation Only
        if (finalDeathStyle == DeathType.Ragdoll && ragdollPrefab == null)
        {
            Debug.LogWarning($"<b>[DeathHandler]</b> ragdollPrefab is unassigned on {gameObject.name}! Falling back to AnimationOnly style.", gameObject);
            finalDeathStyle = DeathType.AnimationOnly;
        }

        // 3. Structural routing execution
        switch (finalDeathStyle)
        {
            case DeathType.AnimationOnly:
                DisableLivingComponents(disableAnimatorContext: false);
                HandleAnimationDeath();
                break;

            case DeathType.Ragdoll:
                DisableLivingComponents(disableAnimatorContext: true);
                ExecuteRagdollSwap(info, completelyExplode: false);
                break;

            case DeathType.ExplodingRagdoll:
                DisableLivingComponents(disableAnimatorContext: true);
                ExecuteRagdollSwap(info, completelyExplode: true);
                //play random explosion
                if (partsExploSound != null && partsExploSound.Length > 0)
                {
                    // Pick a random clip from your array
                    int randomIndex = Random.Range(0, partsExploSound.Length);
                    AudioClip randomClip = partsExploSound[randomIndex];

                    // Safely grab the AudioSource and fire the clip
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.PlayOneShot(randomClip);
                    }
                    else
                    {
                        Debug.LogWarning($"<b>[DeathHandler]</b> Missing AudioSource component on {gameObject.name} to play explosion sounds!", gameObject);
                    }
                }
                break;
        }
    }

    private DeathType CalculateHitIntensity(HitInfo info)
    {
        // If an item/ability forced an explicit override type via script, bypass randomness entirely
        if (info.overrideDeathType.HasValue)
        {
            return info.overrideDeathType.Value;
        }

        float totalImpactForce = info.damage * info.multiplier;

        // Tier 1: Low Intensity Hit
        if (totalImpactForce < mediumIntensityThreshold)
        {
            float roll = Random.Range(0f, 100f);
            return (roll <= animationChance) ? DeathType.AnimationOnly : DeathType.Ragdoll;
        }
        // Tier 3: High Intensity Hit
        else if (totalImpactForce >= highIntensityThreshold)
        {
            float roll = Random.Range(0f, 100f);
            return (roll <= explodingChance) ? DeathType.ExplodingRagdoll : DeathType.Ragdoll;
        }

        // Tier 2: Medium Intensity Hit (Always evaluates directly to Ragdoll)
        return DeathType.Ragdoll;
    }

    private void DisableLivingComponents(bool disableAnimatorContext)
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null) capsule.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (enemyController != null)
        {
            if (enemyController.nav != null) enemyController.nav.enabled = false;
            if (enemyController.obstacle != null) enemyController.obstacle.enabled = false;

            if (disableAnimatorContext)
            {
                enemyController.enabled = false;
            }
        }

        if (healthBar != null)
        {
            healthBar.SetActive(false);
        }
    }

    private void HandleAnimationDeath()
    {
        if (enemyController != null && enemyController.aiAnim != null)
        {
            enemyController.aiAnim.SetBool("isDead", true);
        }

        Destroy(gameObject, animationDeathLifespan);
    }

    private void ExecuteRagdollSwap(HitInfo info, bool completelyExplode)
    {
        Vector3 punchVector = info.forceDirection != Vector3.zero ? info.forceDirection.normalized : -transform.forward;
        float dynamicMultiplier = Mathf.Clamp(info.damage * info.multiplier, 1f, 10f);
        Vector3 blastOrigin = info.forceDirection != Vector3.zero ? transform.position - info.forceDirection : transform.position;

        if (completelyExplode)
        {
            // ─── EXPLOSION DESTRUCTION ───
            GameObject gibsInstance = Instantiate(explodingPartsPrefab, transform.position, transform.rotation);
         

            Rigidbody[] gibBodies = gibsInstance.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody gibRb in gibBodies)
            {
                gibRb.AddExplosionForce(explosionBlastForce * dynamicMultiplier, blastOrigin, explosionRadius, upwardModifier, ForceMode.Impulse);
                gibRb.AddForce((punchVector * (standardPunchForce * dynamicMultiplier)) * 0.1f, ForceMode.Impulse);
            }

            Destroy(gibsInstance, ragdollLifespan);
        }
        else
        {
            // ─── COHESIVE RAGDOLL FALL ───
            GameObject ragdollInstance = Instantiate(ragdollPrefab, transform.position, transform.rotation);
            MatchTargetPose(transform, ragdollInstance.transform);

            SkinnedMeshRenderer[] ragdollMeshes = ragdollInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var mesh in ragdollMeshes) mesh.updateWhenOffscreen = true;

            Rigidbody[] ragdollRigidbodies = ragdollInstance.GetComponentsInChildren<Rigidbody>();
            if (info.isExplosion)
            {
                // 1. Flatten the push axis on Y to prevent clipping into the floor
                Vector3 cleanPushDir = punchVector;
                cleanPushDir.y = 0f;
                cleanPushDir.Normalize();

                Vector3 finalRagdollFlyVector = cleanPushDir + (Vector3.up * 0.5f);
                finalRagdollFlyVector.Normalize();
                foreach (Rigidbody rb in ragdollRigidbodies)
                {
                    // REMOVED AddExplosionForce to eliminate conflicting vectors
                    rb.AddForce(finalRagdollFlyVector * (explosionBlastForce * dynamicMultiplier), ForceMode.Impulse);
                }

            }
            else
            {
                foreach (Rigidbody rb in ragdollRigidbodies)
                {
                    //rb.AddExplosionForce(explosionBlastForce * dynamicMultiplier, blastOrigin, explosionRadius, upwardModifier, ForceMode.Impulse);
                    rb.AddForce(punchVector * (standardPunchForce * dynamicMultiplier), ForceMode.Impulse);
                }
            }

            Destroy(ragdollInstance, ragdollLifespan);
        }

        SwitchOffFunctionality();
        Destroy(gameObject, audioCleanupDelay);
    }

    private void SwitchOffFunctionality()
    {
        SkinnedMeshRenderer[] oldMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var mesh in oldMeshes) mesh.enabled = false;

        if (enemyController != null && enemyController.aiAnim != null)
        {
            enemyController.aiAnim.enabled = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    private void MatchTargetPose(Transform sourceParent, Transform destinationParent)
    {
        for (int i = 0; i < sourceParent.childCount; i++)
        {
            var sourceChild = sourceParent.GetChild(i);
            var destinationChild = destinationParent.Find(sourceChild.name);

            if (destinationChild != null)
            {
                destinationChild.position = sourceChild.position;
                destinationChild.rotation = sourceChild.rotation;
                MatchTargetPose(sourceChild, destinationChild);
            }
        }
    }
}