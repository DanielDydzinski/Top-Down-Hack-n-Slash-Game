using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Effects/KnockBack", fileName = "new KnockBack Effect")]
public class TakeKnockBack : Effect
{
    public float force = 20f;
    public float radius = 5f;
    public float unconsciousDuration = 1.0f;
    public float shoveDuration = 0.25f;

    [HideInInspector] public Vector3 explosionPosition;

    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        // 1. Setup References
        EnemyAIController controller = target.GetComponent<EnemyAIController>();
        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        Stats stats = target.GetComponent<Stats>();
        PlayerStateMachine psm = target.GetComponent<PlayerStateMachine>();
        PushPropagator propagator = target.GetComponent<PushPropagator>();

        Vector3 targetPos = target.transform.position;

        // Flatten explosion height for horizontal shove
        Vector3 flatExploPos = new Vector3(explosionPosition.x, targetPos.y, explosionPosition.z);
        Vector3 direction = (targetPos - flatExploPos);
        float distance = direction.magnitude;

        // If outside radius, do nothing
        if (distance > radius) yield break;

        // 2. Calculate Force with Falloff
        float falloff = 1 - (distance / radius);
        float finalForce = force * falloff;
        Vector3 shoveDir = direction.normalized;

        // 3. Handle Stun (Existing Logic)
        if (controller != null) controller.ApplyStun(unconsciousDuration);
        if (psm != null) psm.SwitchState(new PlayerStunState(psm, unconsciousDuration));

        // 4. Particles
        SpawnParticles(target);

        // 5. The Shove Loop (Now using Propagator)
        float elapsed = 0;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;

        while (elapsed < shoveDuration)
        {
            float t = elapsed / shoveDuration;
            float currentPower = Mathf.Lerp(finalForce, 0, t);
            float mass = (stats != null) ? stats.mass : 1f;

            // Calculate this frame's movement
            Vector3 moveAmount = (shoveDir * currentPower * Time.deltaTime) / mass;

            // --- THE UPGRADED MOVEMENT ---
            if (propagator != null)
            {
                // This call handles:
                // 1. Pushing enemies behind this target (Chain Reaction)
                // 2. Stopping if this target hits a wall (No sliding)
                // 3. Choosing between NavMeshAgent or CharacterController
                propagator.PropagatePush(moveAmount, null);
            }
            else
            {
                // Fallback for objects without a propagator
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    agent.Move(moveAmount);
                else
                    target.transform.position += moveAmount;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Cleanup: isStopped is handled by StunState Exit
    }

    private void SpawnParticles(GameObject target)
    {
        var anchors = target.GetComponent<EffectSpawnPossitions>();
        if (effectParticles != null && anchors != null)
        {
            GameObject particlesObj = Instantiate(effectParticles, anchors.head.position, Quaternion.identity, anchors.head);
            Destroy(particlesObj, unconsciousDuration);
        }
    }

    public void SetExplosionPos(Vector3 aPos)
    {
        explosionPosition = aPos;
    }
}