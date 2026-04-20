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
		GameObject instance = Instantiate(abilityPrefab, pos, rot);

		explosionBehaviour = instance.GetComponent<ExplosionBehaviour>();
		if (explosionBehaviour != null)
		{
			explosionBehaviour.UpdateValues(this.abilityEffects, explosionParticles, radius, this.myFaction, this.damageType, damageByDistance);
		}
		else
		{
			Debug.Log("Explosion needs ExplosionBehaviour");
			return null;
		}

		
		return instance;
	}
}
