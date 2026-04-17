using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Explosion", fileName = "new Ability")]
public class Explosion : Ability {

	public GameObject explosionParticles;
	public float radius;
	public bool damageByDistance;

	ExplosionBehaviour explosionBehaviour;

	
	public override GameObject Cast (Vector3 pos, Quaternion rot)
	{
		if (abilityPrefab.GetComponent<ExplosionBehaviour> () != null)
		{
			explosionBehaviour = abilityPrefab.GetComponent<ExplosionBehaviour> ();
		} else 
		{
			Debug.Log("Explosion needs ExplosionBehaviour");
			return null;
		}

		explosionBehaviour.UpdateValues (this.targetTag,this.friendTag, this.abilityEffects,explosionParticles,damageByDistance,radius);
		return Instantiate(abilityPrefab,pos,rot);
	}
}
