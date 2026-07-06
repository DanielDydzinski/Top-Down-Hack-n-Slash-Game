using System.Collections;
using System.Collections.Generic;
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

    [Header("Intensity Thresholds (% of Max Health)")]
    [Tooltip("Any attack dealing BELOW this PERCENTAGE of max health evaluates as a Low-Intensity hit (e.g. 20 means 20%).")]
    [SerializeField] private float mediumIntensityThreshold = 20f;
    [Tooltip("Any attack dealing EQUAL OR ABOVE this PERCENTAGE of max health evaluates as a High-Intensity hit (e.g. 60 means 60%).")]
    [SerializeField] private float highIntensityThreshold = 60f;

    [Header("Probability Chances (0% to 100%)")]
    [Tooltip("Percentage chance that a Low-Intensity hit executes an animated clip. Failing this roll forces a standard Ragdoll instead.")]
    [Range(0f, 100f)]
    [SerializeField] private float animationChance = 70f;
    [Tooltip("Percentage chance that a High-Intensity hit completely vaporizes into parts. Failing this roll forces a standard Ragdoll instead.")]
    [Range(0f, 100f)]
    [SerializeField] private float explodingChance = 70f;

    [Header("Ragdoll Assets")]
    [Tooltip("List of various ragdoll variations. One will be picked at random on death.")]
    [SerializeField] private List<GameObject> ragdollPrefabs = new List<GameObject>();
    [Tooltip("The ragdoll version spawned at the very end of a low-intensity death animation sequence.")]
    [SerializeField] private GameObject animationSawpRagdollPrefab;

    [Header("parts Explo Sound")]
    [SerializeField] private AudioClip[] partsExploSound;

    [Header("Health Bar Ref")]
    public GameObject healthBar;

    [Header("Standard Physics (Punch)")]
    [Tooltip("The MAXIMUM kinetic force applied if an attack takes 100% of the enemy's max health in a single hit.")]
    [SerializeField] private float maxPunchForce = 50f;

    [Header("Explosion Physics (Standard Ragdoll Fly)")]
    [Tooltip("The MAXIMUM radial force pushing a standard ragdoll outward if an explosion takes 100% of the enemy's max health.")]
    [SerializeField] private float explosionBlastForce = 500f;
    [SerializeField] private float explosionRadius = 3f;
    [Tooltip("Vertical lift applied to BOTH standard explosion ragdolls and exploding gib chunks.")]
    [SerializeField] private float upwardModifier = 0.5f;

    [Header("Explosion Settings (Complete Vaporization Style)")]
    [Tooltip("The standalone physics chunk model that breaks apart instantly on impact.")]
    [SerializeField] private GameObject explodingPartsPrefab;
    [Tooltip("A flat, unscaled physics explosion force applied exclusively to the broken body chunks.")]
    [SerializeField] private float completeExplodeBlastForce = 750f;

    [Header("Blood Bath (Ground Splash)")]
    [Tooltip("Ground blood-splash prefabs (e.g. BloodBomb variants). One is picked at random and spawned as its own independent instance alongside the ragdoll/visual, leveled so its decals project straight down regardless of the death pose's tilt.")]
    [SerializeField] private List<GameObject> bloodBathPrefabs = new List<GameObject>();
    [Tooltip("Local offset from the death position where the blood bath prefab is spawned.")]
    [SerializeField] private Vector3 bloodBathOffset = Vector3.zero;
    [Tooltip("Spawn the blood bath prefab for a low-intensity animation-only death.")]
    [SerializeField] private bool includeBloodBathAnimation = false;
    [Tooltip("Spawn the blood bath prefab for a standard ragdoll death.")]
    [SerializeField] private bool includeBloodBathRagdoll = false;
    [Tooltip("Spawn the blood bath prefab for a complete exploding ragdoll death.")]
    [SerializeField] private bool includeBloodBathExploding = false;
    [Tooltip("How long the spawned blood bath effect stays alive before being recycled back into the object pool.")]
    [SerializeField] private float bloodBathLifetime = 30f;

    [Header("Lifespan & Delay Constraints")]
    [Tooltip("Time before the physics ragdoll or broken chunks are deleted to clear memory.")]
    [SerializeField] private float ragdollLifespan = 10f;
    [Tooltip("Time the original frame stays alive to finish playing audio/particles before total deletion.")]
    [SerializeField] private float audioCleanupDelay = 3.0f;

    [Space(10)]
    // ─── NEW INTERRUPT SETTINGS ───
    [Tooltip("If true, the animation will be cut short at a random time to force the ragdoll swap mid-collapse.")]
    [SerializeField] private bool randomizeAnimationLifespan = true;
    [Tooltip("The earliest time (in seconds) the animation can be cut short into a ragdoll.")]
    [SerializeField] private float minAnimationLifespan = 0.4f;
    [Tooltip("The maximum time (in seconds) the animation can run before being cut short into a ragdoll.")]
    [SerializeField] private float maxAnimationLifespan = 0.8f;
    [Tooltip("Fallback duration if randomization is turned off (plays the full clip or custom fixed time).")]
    [SerializeField] private float animationDeathLifespan = 2.0f;

    private Health healthComponent;
    private EnemyAIController enemyController;
    private bool _hasDied = false;
    private float _maxEnemyHealth = 100f;
    private DeathType finalDeathStyle = DeathType.AnimationOnly;

    void Awake()
    {
        healthComponent = GetComponent<Health>();
        enemyController = GetComponent<EnemyAIController>();
        healthBar = transform.Find("HPCanvas")?.gameObject;
    }

    void Start()
    {
        if (healthComponent != null)
        {
            _maxEnemyHealth = healthComponent.GetMaxHealth();
        }

        if (_maxEnemyHealth <= 0f) _maxEnemyHealth = 1f;
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

        //if (TimeManager.Instance != null)
        //{
        //    TimeManager.Instance.TriggerSlowMotion();
        //}

        if (enemyController != null)
        {
            enemyController.CleanUpSlot();
        }

        EffectManager em = GetComponent<EffectManager>();
        if (em != null) em.CleanUpAllEffects();
        em.enabled = false;

        finalDeathStyle = CalculateHitIntensity(info);

        if (finalDeathStyle == DeathType.ExplodingRagdoll && explodingPartsPrefab == null)
        {
            Debug.LogWarning($"<b>[DeathHandler]</b> explodingPartsPrefab is unassigned on {gameObject.name}! Falling back to Ragdoll style.", gameObject);
            finalDeathStyle = DeathType.Ragdoll;
        }

        if (finalDeathStyle == DeathType.Ragdoll && (ragdollPrefabs == null || ragdollPrefabs.Count == 0))
        {
            Debug.LogWarning($"<b>[DeathHandler]</b> ragdollPrefabs list is empty or unassigned on {gameObject.name}! Falling back to AnimationOnly style.", gameObject);
            finalDeathStyle = DeathType.AnimationOnly;
        }

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

                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.TriggerSlowMotion();
                }
                  
                DisableLivingComponents(disableAnimatorContext: true);
                ExecuteRagdollSwap(info, completelyExplode: true);

                if (partsExploSound != null && partsExploSound.Length > 0)
                {
                    int randomIndex = Random.Range(0, partsExploSound.Length);
                    AudioClip randomClip = partsExploSound[randomIndex];

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
        if (info.overrideDeathType.HasValue)
        {
            return info.overrideDeathType.Value;
        }

        float totalImpactForce = info.damage * info.multiplier;
        float damagePercentage = (totalImpactForce / _maxEnemyHealth) * 100f;

        if (damagePercentage < mediumIntensityThreshold)
        {
            float roll = Random.Range(0f, 100f);
            return (roll <= animationChance) ? DeathType.AnimationOnly : DeathType.Ragdoll;
        }
        else if (damagePercentage >= highIntensityThreshold)
        {
            float roll = Random.Range(0f, 100f);
            return (roll <= explodingChance) ? DeathType.ExplodingRagdoll : DeathType.Ragdoll;
        }

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

    private void SpawnBloodBath()
    {
        if (bloodBathPrefabs == null || bloodBathPrefabs.Count == 0) return;

        GameObject chosenBloodBathPrefab = bloodBathPrefabs[Random.Range(0, bloodBathPrefabs.Count)];

        Vector3 spawnPosition = transform.position + bloodBathOffset;
        // Randomized yaw only - keeps the decals level so their ground raycast still hits straight down.
        Quaternion levelRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.SpawnFromPool(chosenBloodBathPrefab, spawnPosition, levelRotation, bloodBathLifetime);
        }
        else
        {
            Instantiate(chosenBloodBathPrefab, spawnPosition, levelRotation);
        }
    }

    private void HandleAnimationDeath()
    {
        if (enemyController != null && enemyController.aiAnim != null)
        {
            enemyController.aiAnim.SetBool("isDead", true);
        }

        StartCoroutine(AnimationDeathSequenceRoutine());
    }

    // ─── UPDATED ROUTINE ───
    private IEnumerator AnimationDeathSequenceRoutine()
    {
        // 1. Calculate the cutoff time point
        float activeWaitTime = randomizeAnimationLifespan
            ? Random.Range(minAnimationLifespan, maxAnimationLifespan)
            : animationDeathLifespan;

        // 2. Let the animation play normally until the timer hits the cutoff point
        yield return new WaitForSeconds(activeWaitTime);

        if (animationSawpRagdollPrefab != null)
        {
            // 3. Spawn the ragdoll surrogate container (pooled if available)
            GameObject ragdollInstanceSwap = SpawnRagdollPart(animationSawpRagdollPrefab, transform.position, transform.rotation, ragdollLifespan);
            ResetRagdollPhysics(ragdollInstanceSwap);

            // 4. Snaps the ragdoll bones directly to the intermediate animated pose frame!
            MatchTargetPose(transform, ragdollInstanceSwap.transform);
        }

        if (includeBloodBathAnimation) SpawnBloodBath();

        // Tidy up and destroy the main character shell immediately
        Destroy(gameObject);
    }

    private void ExecuteRagdollSwap(HitInfo info, bool completelyExplode)
    {
        Vector3 punchVector = info.forceDirection != Vector3.zero ? info.forceDirection.normalized : -transform.forward;
        Vector3 blastOrigin = info.forceDirection != Vector3.zero ? transform.position - info.forceDirection : transform.position;

        if (completelyExplode)
        {
            GameObject gibsInstance = SpawnRagdollPart(explodingPartsPrefab, transform.position, transform.rotation, ragdollLifespan);
            ResetRagdollPhysics(gibsInstance);

            Rigidbody[] gibBodies = gibsInstance.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody gibRb in gibBodies)
            {
                gibRb.AddExplosionForce(completeExplodeBlastForce, blastOrigin, explosionRadius, upwardModifier, ForceMode.Impulse);
                gibRb.AddForce((punchVector * maxPunchForce) * 0.1f, ForceMode.Impulse);
            }

            if (includeBloodBathExploding) SpawnBloodBath();
        }
        else
        {
            float rawDamage = info.damage * info.multiplier;
            float damageScale = Mathf.Clamp(rawDamage / _maxEnemyHealth, 0.1f, 2.0f);

            int selectedRagdollIndex = Random.Range(0, ragdollPrefabs.Count);
            GameObject chosenRagdollPrefab = ragdollPrefabs[selectedRagdollIndex];

            GameObject ragdollInstance = SpawnRagdollPart(chosenRagdollPrefab, transform.position, transform.rotation, ragdollLifespan);
            ResetRagdollPhysics(ragdollInstance);
            MatchTargetPose(transform, ragdollInstance.transform);

            SkinnedMeshRenderer[] ragdollMeshes = ragdollInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var mesh in ragdollMeshes) mesh.updateWhenOffscreen = true;

            RigidBodyAndPhysicsApplication(ragdollInstance, info, punchVector, damageScale);

            if (includeBloodBathRagdoll) SpawnBloodBath();
        }

        SwitchOffFunctionality();
        Destroy(gameObject, audioCleanupDelay);
    }

    // Spawns a ragdoll/gib/swap-ragdoll prefab through the pool (auto-recycled after lifetime),
    // falling back to the old Instantiate+Destroy(delay) pairing if no ObjectPooler exists in the scene.
    private GameObject SpawnRagdollPart(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (ObjectPooler.Instance != null)
        {
            return ObjectPooler.Instance.SpawnFromPool(prefab, position, rotation, lifetime);
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        Destroy(instance, lifetime);
        return instance;
    }

    // A reused pooled ragdoll/gib can still carry momentum from its last death - clear it before
    // MatchTargetPose or any new AddForce/AddExplosionForce is applied, so old flight doesn't
    // compound with the new hit's force.
    private void ResetRagdollPhysics(GameObject root)
    {
        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in bodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void RigidBodyAndPhysicsApplication(GameObject ragdollInstance, HitInfo info, Vector3 punchVector, float damageScale)
    {
        Rigidbody[] ragdollRigidbodies = ragdollInstance.GetComponentsInChildren<Rigidbody>();
        if (info.isExplosion)
        {
            Vector3 cleanPushDir = punchVector;
            cleanPushDir.y = 0f;
            cleanPushDir.Normalize();

            Vector3 finalRagdollFlyVector = cleanPushDir + (Vector3.up * upwardModifier);
            finalRagdollFlyVector.Normalize();

            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                rb.AddForce(finalRagdollFlyVector * (explosionBlastForce * damageScale), ForceMode.Impulse);
            }
        }
        else
        {
            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                rb.AddForce(punchVector * (maxPunchForce * damageScale), ForceMode.Impulse);
            }
        }
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

    private void MatchTargetPose(Transform livingRoot, Transform ragdollRoot)
    {
        // Gather ALL transforms inside the living character's armature
        Transform[] livingTransforms = livingRoot.GetComponentsInChildren<Transform>();

        // Create a fast-lookup dictionary of the living bones by their names
        Dictionary<string, Transform> livingBoneMap = new Dictionary<string, Transform>();
        foreach (Transform t in livingTransforms)
        {
            if (!livingBoneMap.ContainsKey(t.name))
            {
                livingBoneMap.Add(t.name, t);
            }
        }

        // Gather ALL transforms inside the newly spawned ragdoll
        Transform[] ragdollTransforms = ragdollRoot.GetComponentsInChildren<Transform>();

        // OPTIMIZATION: Temporarily disable physics on the ragdoll bones so they don't fight the snapping
        List<Rigidbody> ragdollRigidbodies = new List<Rigidbody>();
        foreach (Transform rBone in ragdollTransforms)
        {
            Rigidbody rb = rBone.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Freeze physics for a split second
                ragdollRigidbodies.Add(rb);
            }
        }

        // Match positions and rotations perfectly regardless of hierarchy depth mismatches
        foreach (Transform rBone in ragdollTransforms)
        {
            if (livingBoneMap.TryGetValue(rBone.name, out Transform matchingLivingBone))
            {
                rBone.position = matchingLivingBone.position;
                rBone.rotation = matchingLivingBone.rotation;
            }
        }

        // Wake physics back up on the ragdoll now that it's in the perfect position
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
        }
    }
}
