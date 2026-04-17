using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

[CreateAssetMenu(menuName = "Effects/DamageOverTime", fileName = "new Effect")]
public class TakeDoT : Effect {

	public float damageAmount;
	public float damageDuration;
	public float damageRate;

	public override IEnumerator ApplyEffect(GameObject target)
	{
		Health hp = null;
		if (!target.GetComponent<Health> ()) 
		{
			UnityEngine.Debug.Log (target.name + " doesn't have Health Component. Can't apply DoT Effect.");
			yield break; // will this even work?
		}
		else
		{
			hp = target.GetComponent<Health> ();
		}

		//UnityEngine.Debug.Log ("applying DoT");
		Stopwatch timer = new Stopwatch ();
		timer.Start ();

		while (timer.Elapsed.Seconds < damageDuration)
		{
			yield return new WaitForSecondsRealtime (damageRate);

			hp.Damage (damageAmount);
			if (effectParticles != null) 
			{
				if(target.tag == "Enemy" || target.tag == "Player")
				{
					Instantiate (effectParticles, target.transform.GetChild(4).transform); // 4th child set up to centre. 3rd to over head. 5th to feet.
				}
			}
		}

		timer.Stop ();
		timer.Reset ();
	}


}
