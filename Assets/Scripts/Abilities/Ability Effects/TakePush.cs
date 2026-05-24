using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "Effects/Push", fileName = "new Push Effect")]
public class TakePush : Effect
{
    public float pushForce = 15f;
    public float pushDuration = 0.2f;

    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        // 1. Setup Direction (Ignore Y to prevent "flying" zombies)
        Vector3 dir = info.forceDirection.normalized;
        dir.y = 0;

        //SCALE PUSH FORCE WITH FALLOFF MULTIPLIER
        // This scales your base 15f force down depending on distance from shockwave center!
        float calculatedPushForce = pushForce * info.multiplier;

        // 2. Get Components
        Stats stats = target.GetComponent<Stats>();
        UnityEngine.AI.NavMeshAgent agent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        PushPropagator propagator = target.GetComponent<PushPropagator>();

        if (stats != null) stats.isPushed = true;

        // --- PRE-PUSH SETUP ---
        bool wasStoppedBeforePush = false;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            wasStoppedBeforePush = agent.isStopped;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        float elapsed = 0f;

        // --- THE DAMPING LOOP ---
        while (elapsed < pushDuration)
        {
            float percentage = elapsed / pushDuration;
            // Damping: Starts at pushForce, ends at 0
            float currentForce = Mathf.Lerp(calculatedPushForce, 0, percentage);

            // Mass resistance for THIS specific target
            float mass = stats != null ? stats.mass : 1f;
            Vector3 moveAmount = (dir * currentForce * Time.deltaTime) / mass;

            // --- THE CHAIN REACTION TRIGGER ---
            if (propagator != null)
            {
                // We tell the propagator to move us. 
                // It will automatically check for enemies behind us and push them too!
                propagator.PropagatePush(moveAmount, null);
            }
            else
            {
                // Fallback: If for some reason there is no Propagator, move manually
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    agent.Move(moveAmount);
                else
                    target.transform.position += moveAmount;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- CLEANUP ---
        if (agent != null && agent.isActiveAndEnabled)
        {
            if (!wasStoppedBeforePush) agent.isStopped = false;
            if (agent.isOnNavMesh) agent.velocity = Vector3.zero;
        }

        if (stats != null) stats.isPushed = false;
    }
}