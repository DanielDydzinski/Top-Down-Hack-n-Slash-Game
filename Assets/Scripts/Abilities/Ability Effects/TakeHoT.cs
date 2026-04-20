using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/HealOverTime", fileName = "new Effect")]
public class TakeHoT : Effect {


	public float healAmount;
	public float healDuration;
	public float healRate;

	public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
	{

        Health hp = target.GetComponent<Health>();

        if (hp == null)
        {
            Debug.Log(target.name + " doesn't have Health Component. Can't apply HoT Effect.");
            yield break; // This works perfectly to exit a coroutine early
        }

        float elapsed = 0f;

        // while loop that respects Time.timeScale (will pause when game pauses)
        while (elapsed < healDuration)
        {
            // Apply damage
            hp.Heal(healAmount);

            // Handle Particles using your new anchor system
            if (effectParticles != null)
            {
                var anchors = target.GetComponent<EffectSpawnPossitions>();
                if (anchors != null)
                {
                    Instantiate(effectParticles, anchors.center.position, Quaternion.identity, anchors.center);
                }
            }

            // Wait for the next tick
            // Using WaitForSeconds instead of Realtime ensures it pauses with the game
            yield return new WaitForSeconds(healRate);

            // Update how long we've been burning/poisoned
            elapsed += healRate;
        }
	}
}
