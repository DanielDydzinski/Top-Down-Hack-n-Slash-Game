using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/AoEoT", fileName = "new Ability")]
public class AoEoT : Ability {

	public GameObject abilityParticles;
	public float radius;
	public float duration;
	public float rate;

	AoEoTBehaviour aoeotBehaviour;

	public override GameObject Cast (Vector3 pos, Quaternion rot)
	{
		if (abilityPrefab.GetComponent<AoEoTBehaviour> () != null)
		{
			aoeotBehaviour = abilityPrefab.GetComponent<AoEoTBehaviour> ();
		} else 
		{
			Debug.Log("AoEoT needs AoEoTBehaviour");
			return null;
		}

		aoeotBehaviour.UpdateValues (this.abilityEffects, duration, rate, radius,this.myFaction, abilityParticles,this.damageType);

		return Instantiate(abilityPrefab,pos,rot);
	}

}
