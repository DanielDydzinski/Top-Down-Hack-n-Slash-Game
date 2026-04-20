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
        GameObject instance = Instantiate(abilityPrefab, pos, rot);


     
		meleeAttackBehavaiour = instance.GetComponent<MeleeAttackBehavaiour> ();
		if(meleeAttackBehavaiour!=null)
		{
            meleeAttackBehavaiour.UpdateValues(this.myFaction, this.abilityEffects, length, halfExtents, attackParticles, this.damageType);
        }
		 else 
		{
			Debug.Log("MeleeAttack needs MeleeAttackBehavaiour");
			return null;
		}

		return instance;
	}
}
