using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/FireBall", fileName = "new Ability")]
public class FireBall : Ability {
	
	public GameObject projectilePrefab;
	public Ability explosionAbility;

	public float projectileSpeed;
	public float projectileSize;
	public float projectileRange;

	FireBallBehaviour fbBehaviour;

	public override GameObject Cast (Vector3 pos, Quaternion rot)
	{
		if (abilityPrefab.GetComponent<FireBallBehaviour> () != null)
		{
			fbBehaviour = abilityPrefab.GetComponent<FireBallBehaviour> ();
		} else 
		{
			Debug.Log("FireBall needs fireballBehaviour");
			return null;
		}

		fbBehaviour.UpdateValues (this.abilityEffects,this.targetTag,this.friendTag,projectileSpeed, projectileSize,projectileRange ,projectilePrefab,explosionAbility);

		return Instantiate(abilityPrefab,pos,rot);
	}

}
