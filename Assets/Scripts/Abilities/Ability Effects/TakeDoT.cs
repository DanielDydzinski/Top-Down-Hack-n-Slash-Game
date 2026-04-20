using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Effects/DamageOverTime", fileName = "new Effect")]
public class TakeDoT : Effect
{

    public float damageAmount;
    public float damageDuration;
    public float damageRate;

    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        Health hp = target.GetComponent<Health>();

        if (hp == null)
        {
            Debug.Log(target.name + " doesn't have Health Component. Can't apply DoT Effect.");
            yield break; // This works perfectly to exit a coroutine early
        }

        float elapsed = 0f;

        // while loop that respects Time.timeScale (will pause when game pauses)
        while (elapsed < damageDuration)
        {
            // Apply damage
            hp.Damage(damageAmount);

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
            yield return new WaitForSeconds(damageRate);

            // Update how long we've been burning/poisoned
            elapsed += damageRate;
        }
    }
}

//public class TakeDoT : Effect
//{

//	public float damageAmount;
//	public float damageDuration;
//	public float damageRate;

//	public override IEnumerator ApplyEffect(GameObject target)
//	{
//		Health hp = target.GetComponent<Health>();

//		if (hp == null)
//		{
//			UnityEngine.Debug.Log(target.name + " doesn't have Health Component. Can't apply DoT Effect.");
//			yield break; // will this even work?
//		}

//		//UnityEngine.Debug.Log ("applying DoT");
//		Stopwatch timer = new Stopwatch();
//		timer.Start();

//		while (timer.Elapsed.Seconds < damageDuration)
//		{
//			yield return new WaitForSecondsRealtime(damageRate);

//			hp.Damage(damageAmount);
//			if (effectParticles != null)
//			{
//				if (target.tag == "Enemy" || target.tag == "Player")
//				{
//					Instantiate(effectParticles, target.transform.GetChild(4).transform); // 4th child set up to centre. 3rd to over head. 5th to feet.
//				}
//			}
//		}

//		timer.Stop();
//		timer.Reset();
//	}
//}
