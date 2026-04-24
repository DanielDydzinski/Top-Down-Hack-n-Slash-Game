using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Diagnostics;

[CreateAssetMenu(menuName = "Effects/KnockBack", fileName = "new Effect")]
public class TakeKnockBack : Effect
{

	public float force;
	public float radius;
	public Vector3 explosionPosition { set; get;}
	public float unconsciousDuration;


    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        Vector3 targetPos = target.transform.position;

        Vector3 FinalExploPos = explosionPosition;
       	FinalExploPos.y = targetPos.y;				//making all explosions  level for constant force in x and z direction
       	explosionPosition = FinalExploPos;

        // 1. Grab our new Controller
        EnemyAIController controller = target.GetComponent<EnemyAIController>();
        Rigidbody rb = target.GetComponent<Rigidbody>();

        // Try to find your specific State Machine
        if (target.TryGetComponent<PlayerStateMachine>(out PlayerStateMachine psm))
        {

            Vector3 direction = target.transform.position - explosionPosition;
            float distance = direction.magnitude;

            if (distance < radius)
            {
                float falloff = 1 - (distance / radius);
                float finalForce = force * falloff;

                // 1. Apply the physical shove via Mover
                psm.mover.AddForce(direction.normalized, finalForce);

                UnityEngine.Debug.Log("Take KB taking place !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                // 2. Switch to the Stun State
                psm.SwitchState(new PlayerStunState(psm, unconsciousDuration));

                // 5. Handle Particles (using your existing logic)
                var anchors = target.GetComponent<EffectSpawnPossitions>();
                if (effectParticles != null && anchors != null)
                {
                    // Knockback usually looks best spawned at the "Head" (stars/dizzy) or "Center"
                    GameObject particlesObj = Instantiate(effectParticles, anchors.head.position, Quaternion.identity, anchors.head);
                    Destroy(particlesObj, unconsciousDuration);
                }
            }

        }

            if (controller != null && rb != null)
        {
            // 2. Tell the AI "Stop thinking, you're stunned"
            controller.ApplyStun(unconsciousDuration);

            // 3. Switch to Physics Mode (NavMesh OFF, Rigidbody ON)
            //controller.TogglePhysicsMode(true); // Let stun state enter handle the physics

            // 4. Apply the physical Force
            rb.AddExplosionForce(force, explosionPosition, radius);

            // 5. Handle Particles (using your existing logic)
            var anchors = target.GetComponent<EffectSpawnPossitions>();
            if (effectParticles != null && anchors != null)
            {
                // Knockback usually looks best spawned at the "Head" (stars/dizzy) or "Center"
                GameObject particlesObj = Instantiate(effectParticles, anchors.head.position, Quaternion.identity, anchors.head);
                Destroy(particlesObj, unconsciousDuration);
            }

            // 6. Wait for the stun to end
            //yield return new WaitForSeconds(unconsciousDuration);

            // 7. Switch back to Navigation Mode (NavMesh ON, Rigidbody OFF)
            //controller.TogglePhysicsMode(false);  //move to stun state exit
        }
        yield break;
    }

    public void SetExplosionPos (Vector3 aPos)
	{
		explosionPosition = aPos;
	}

}
