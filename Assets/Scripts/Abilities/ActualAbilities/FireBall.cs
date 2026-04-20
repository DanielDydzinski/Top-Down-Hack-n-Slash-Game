using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR.Interaction;

[CreateAssetMenu(menuName = "Abilities/FireBall", fileName = "new Ability")]
public class FireBall : Ability {
	
	public GameObject projectilePrefab;
	public Ability explosionAbility;

	public float projectileSpeed;
	public float projectileSize;
	public float projectileRange;

	public bool isDestructible = false;
	public DamageType lethalType;
	public GameObject destructionParticles;


    FireBallBehaviour fbBehaviour;

	public override GameObject Cast (Vector3 pos, Quaternion rot)
	{
		GameObject instance = Instantiate(abilityPrefab, pos, rot);

		fbBehaviour = instance.GetComponent<FireBallBehaviour>();
		if (fbBehaviour != null)
		{
            fbBehaviour.UpdateValues(this.abilityEffects, projectileSpeed, projectileSize, projectileRange, projectilePrefab, explosionAbility, this.myFaction, this.damageType);

            if (isDestructible)
            {
                var dest = instance.AddComponent<DestructibleEnvironment>();
                dest.lethalType = this.lethalType;
                dest.destructionParticles = this.destructionParticles;

                // Ensure the object has an identity so it can recognize "Friends"
                var identity = instance.GetComponent<EntityIdentity>();
                if (identity == null) identity = instance.AddComponent<EntityIdentity>();
                identity.faction = this.myFaction;
            }

        }
		else
		{
			Debug.Log("FireBall needs fireballBehaviour");
			return null;
		}

		



		return instance;
	}

}
