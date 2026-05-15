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

    [Header("Death Style")]
    public DeathType deathStyle = DeathType.Ragdoll;

    [Header("Ragdoll Settings")]
    [Tooltip("The separate ragdoll container prefab.")]
    public GameObject ragdollPrefab;

    [Header("Explosion Settings")]
    public float explosionForce = 600f;
    public float explosionRadius = 3f;
    public float upwardModifier = 1f;

    private bool _hasDied = false;

    public void TriggerDeath(Vector3 damageHitPoint)
    {
        // Guard against multiple death triggers on the same frame
        if (_hasDied) return;
        _hasDied = true;

        // 1. Immediately clean up combat slots and external systems
        EnemyAIController ai = GetComponent<EnemyAIController>();
        if (ai != null)
        {
            ai.CleanUpSlot();
            ai.enabled = false;
        }

        EffectManager em = GetComponent<EffectManager>();
        if (em != null) em.CleanUpAllEffects();

        // 2. Safely strip or disable physics components on the living actor
        DisableLivingComponents();

        // 3. Execute chosen death style
        switch (deathStyle)
        {
            case DeathType.AnimationOnly:
                HandleAnimationDeath();
                break;

            case DeathType.Ragdoll:
                HandleRagdollSwap(Vector3.zero, Vector3.zero);
                break;

            case DeathType.ExplodingRagdoll:
                HandleRagdollSwap(damageHitPoint, Vector3.zero, true);
                break;
        }
    }

    private void DisableLivingComponents()
    {
        // Shut down navigation completely so it stops controlling position
        NavMeshAgent nav = GetComponent<NavMeshAgent>();
        if (nav != null) nav.enabled = false;

        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null) obstacle.enabled = false;

        // Turn off main actor collision so it won't impact the player or spawned bones
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null) capsule.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private void HandleAnimationDeath()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            // Assumes your animator state machine has a trigger named "Die"
            anim.SetTrigger("Die");
        }
        // Destroy the living game object after the animation completes
        Destroy(gameObject, 4f);
    }

    private void HandleRagdollSwap(Vector3 hitPoint, Vector3 forceDirection, bool shouldExplode = false)
    {
        if (ragdollPrefab == null)
        {
            Debug.LogError($"<b>[EnemyDeathHandler]</b> Missing Ragdoll Prefab on {gameObject.name}! Defaulting to destruction.", gameObject);
            Destroy(gameObject);
            return;
        }

        // Spawn the physics ragdoll prefab container
        GameObject ragdollInstance = Instantiate(ragdollPrefab, transform.position, transform.rotation);

        // Map the exact bone pose from this animated zombie over to the ragdoll instance
        MatchTargetPose(transform, ragdollInstance.transform);

        // Enable the "Update When Offscreen" option on child meshes so they render cleanly when separated
        SkinnedMeshRenderer[] meshes = ragdollInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var mesh in meshes)
        {
            mesh.updateWhenOffscreen = true;
        }

        // Apply physical forces if it is set to explode
        if (shouldExplode)
        {
            Rigidbody[] rbs = ragdollInstance.GetComponentsInChildren<Rigidbody>();
            Vector3 explosionSource = hitPoint == Vector3.zero ? transform.position + Vector3.down : hitPoint;

            foreach (Rigidbody rb in rbs)
            {
                rb.AddExplosionForce(explosionForce, explosionSource, explosionRadius, upwardModifier, ForceMode.Impulse);
            }
        }

        // Clean up the entire ragdoll container after 10 seconds to keep the stage clean
        Destroy(ragdollInstance, 10f);

        // Instantly delete the old live actor frame so it drops out of the scene smoothly
        Destroy(gameObject);
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