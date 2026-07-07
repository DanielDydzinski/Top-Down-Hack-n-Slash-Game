using UnityEngine;
using UnityEngine.AI;

public class PushPropagator : MonoBehaviour
{
    private NavMeshAgent agent;
    private CharacterController playerCC;
    private Stats stats;

    [Header("Detection Settings")]
    public LayerMask pushableLayers; // Enemy and Player
    public LayerMask wallLayer;      // Environment/Obstacles

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCC = GetComponent<CharacterController>();
        stats = GetComponent<Stats>();

        // Default layers if not set in Inspector
        if (pushableLayers == 0) pushableLayers = LayerMask.GetMask("Enemy", "Player");
        if (wallLayer == 0) wallLayer = LayerMask.GetMask("Environment");
    }

    public void PropagatePush(Vector3 velocity, GameObject source)
    {
        if (velocity.magnitude < 0.001f) return;

        float myRadius = agent != null ? agent.radius : (playerCC != null ? playerCC.radius : 0.5f);
        Vector3 direction = velocity.normalized;
        float distance = velocity.magnitude;

        // --- 1. WALL CHECK (The "Thud" Logic) ---
        // We check if a wall is in the way. If so, we stop immediately.
        if (Physics.SphereCast(transform.position + Vector3.up, myRadius * 0.9f, direction, out RaycastHit wallHit, distance, wallLayer))
        {
            // Optional: You could play a "Wall Thump" sound here or spawn dust particles
            return;
        }

        // --- 2. CHAIN REACTION CHECK ---
        if (Physics.SphereCast(transform.position + Vector3.up, myRadius * 0.9f, direction, out RaycastHit hit, distance + 0.1f, pushableLayers))
        {
            if (hit.collider.gameObject != gameObject && hit.collider.gameObject != source)
            {
                if (hit.collider.TryGetComponent<PushPropagator>(out var nextPropagator))
                {
                    float myMass = stats != null ? stats.mass : 1f;
                    float theirMass = hit.collider.GetComponent<Stats>()?.mass ?? 1f;

                    float transferRatio = myMass / (myMass + theirMass);
                    nextPropagator.PropagatePush(velocity * transferRatio, gameObject);
                    velocity *= (1f - transferRatio);
                }
            }
        }

        // --- 3. APPLY MOVEMENT ---
        if (agent != null && agent.enabled)
        {
            agent.Move(velocity);
        }
        else if (playerCC != null && playerCC.enabled)
        {
            playerCC.Move(velocity);
        }
    }
}