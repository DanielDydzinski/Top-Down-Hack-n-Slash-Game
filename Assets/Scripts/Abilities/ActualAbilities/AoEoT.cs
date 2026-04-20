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

    [Header("Destruction Trait")]
    public bool isDestructible;
    public DamageType lethalType;
    public GameObject destructionParticles;



    public override GameObject Cast(Vector3 pos, Quaternion rot)
    {
        //  Instantiate the "Blank" Prefab first
        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        // Setup the Core Behaviour
        aoeotBehaviour  = instance.GetComponent<AoEoTBehaviour>();
        aoeotBehaviour.UpdateValues(this.abilityEffects, duration, rate, radius, this.myFaction, abilityParticles, this.damageType);

        // Trait Injection: Add the destructible script ONLY if this ability needs it
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

        return instance;
    }



 //   public override GameObject Cast (Vector3 pos, Quaternion rot)
	//{
	//	if (abilityPrefab.GetComponent<AoEoTBehaviour> () != null)
	//	{
	//		aoeotBehaviour = abilityPrefab.GetComponent<AoEoTBehaviour> ();
	//	} else 
	//	{
	//		Debug.Log("AoEoT needs AoEoTBehaviour");
	//		return null;
	//	}

	//	aoeotBehaviour.UpdateValues (this.abilityEffects, duration, rate, radius,this.myFaction, abilityParticles,this.damageType);

	//	return Instantiate(abilityPrefab,pos,rot);
	//}

}
