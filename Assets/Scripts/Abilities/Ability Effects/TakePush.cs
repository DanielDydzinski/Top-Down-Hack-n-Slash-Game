using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Effects/Push", fileName = "new Push Effect")]
public class TakePush : Effect
{
    public float pushForce = 15f;
    public float pushDuration = 0.2f;

    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        Vector3 dir = info.forceDirection.normalized;
        dir.y = 0;

        Stats stats = target.GetComponent<Stats>();
        Mover mover = target.GetComponent<Mover>();
        UnityEngine.AI.NavMeshAgent agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (stats != null) stats.isPushed = true;

        // --- PRE-PUSH SETUP ---
        bool wasStoppedBeforePush = false;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            wasStoppedBeforePush = agent.isStopped; // Remember if they were already stunned
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        float elapsed = 0f;

        // --- THE DAMPING LOOP ---
        while (elapsed < pushDuration)
        {
            float percentage = elapsed / pushDuration;
            float currentForce = Mathf.Lerp(pushForce, 0, percentage);
            // The heavier the mass, the smaller the moveAmount
            float mass = stats != null ? stats.mass : 1f;
            Vector3 moveAmount = (dir * currentForce * Time.deltaTime) / mass; ;

            if (agent != null)
            {
                // CRITICAL FIX: Only call Move if the agent is active AND on the mesh
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.Move(moveAmount);
                }
                else
                {
                    // FALLBACK: If the agent is disabled or off-mesh, move transform directly
                    target.transform.position += moveAmount;
                }
            }
            else if (mover != null)
            {
                // For the player (CharacterController)
                var cc = mover.GetComponent<CharacterController>();
                if (cc.enabled) cc.Move(moveAmount);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- CLEANUP ---
        if (agent != null && agent.isActiveAndEnabled)
        {
            // Only resume moving if they WEREN'T stopped before the push (e.g., they aren't stunned)
            // If they are in StunState, the StunState will handle agent.isStopped = false later.
            if (!wasStoppedBeforePush)
            {
                agent.isStopped = false;
            }

            // Safety: Re-sync agent to NavMesh in case transform movement nudged them off
            if (agent.isOnNavMesh) agent.velocity = Vector3.zero;
        }

        if (stats != null) stats.isPushed = false;
    }
}