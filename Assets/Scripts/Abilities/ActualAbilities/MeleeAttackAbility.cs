using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/MeleeAttack", fileName = "new Ability")]
public class MeleeAttackAbility : Ability {

	public GameObject attackParticles;

	public float length; // length of hit box
	public Vector3 halfExtents;	
	public LayerMask TargetLayerMask;

	MeleeAttackBehavaiour meleeAttackBehavaiour;

	public override GameObject Cast (Vector3 pos, Quaternion rot)
	{
		if (abilityPrefab.GetComponent<MeleeAttackBehavaiour> () != null)
		{
			meleeAttackBehavaiour = abilityPrefab.GetComponent<MeleeAttackBehavaiour> ();
		} else 
		{
			Debug.Log("MeleeAttack needs MeleeAttackBehavaiour");
			return null;
		}

		meleeAttackBehavaiour.UpdateValues (this.myFaction,this.abilityEffects,length, halfExtents, attackParticles,this.damageType);

		return Instantiate(abilityPrefab,pos,rot);
	}
}
