using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Heal", fileName = "new Effect")]
public class TakeHeal : Effect {

	public float healAmount;

	public override IEnumerator ApplyEffect(GameObject target)
	{
		if (!target.GetComponent<Health> ()) 
		{
			UnityEngine.Debug.Log (target.name + " doesn't have Health Component. Can't apply Damage Effect.");
			yield break;
		}
		else
		{
			Health hp = target.GetComponent<Health> ();
			hp.Heal (healAmount);

			if (effectParticles != null) 
			{
				if(target.tag == "Enemy" || target.tag == "Player")
				{
					Instantiate (effectParticles, target.transform.GetChild(4).transform); // 4th child set up to centre. 3rd to over head. 5th to feet.
				}
			}

			yield break;
		}
	}
}
