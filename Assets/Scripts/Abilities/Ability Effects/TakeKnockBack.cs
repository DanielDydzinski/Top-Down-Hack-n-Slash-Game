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


    public override IEnumerator ApplyEffect(GameObject target)
    {

        Vector3 FinalExploPos = explosionPosition;
       	FinalExploPos.y = 0f;				//making all explosions from ground level for constant force in x and z direction
       	explosionPosition = FinalExploPos;

        // 1. Grab our new Controller
        EnemyAIController controller = target.GetComponent<EnemyAIController>();
        Rigidbody rb = target.GetComponent<Rigidbody>();

        if (controller != null && rb != null)
        {
            // 2. Tell the AI "Stop thinking, you're stunned"
            controller.ApplyStun(unconsciousDuration);

            // 3. Switch to Physics Mode (NavMesh OFF, Rigidbody ON)
            controller.TogglePhysicsMode(true);

            // 4. Apply the physical Oomph
            rb.AddExplosionForce(force, explosionPosition, radius);

            // 5. Handle Particles (using your existing logic)
            if (effectParticles != null)
            {
                if (target.tag == "Enemy" || target.tag == "Player")
                {
                    EffectManager em = target.GetComponent<EffectManager>();

                    GameObject particlesObj = Instantiate(effectParticles, target.transform.GetChild(3).transform); // 4th child set up to centre. 3rd to over head. 5th to feet.
                    Destroy(particlesObj, unconsciousDuration);
                    em.knockBackParticles = particlesObj;
                }
            }

            // 6. Wait for the stun to end
            yield return new WaitForSeconds(unconsciousDuration);

            // 7. Switch back to Navigation Mode (NavMesh ON, Rigidbody OFF)
            controller.TogglePhysicsMode(false);
        }
    }

    public void SetExplosionPos (Vector3 aPos)
	{
		explosionPosition = aPos;
	}

}
