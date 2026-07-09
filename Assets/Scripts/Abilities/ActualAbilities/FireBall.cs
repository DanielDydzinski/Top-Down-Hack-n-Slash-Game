using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR.Interaction;

[System.Serializable]
public struct FireBallSettings
{
    public GameObject projectilePrefab;
    public Ability explosionAbility;

    public bool isBeam;
    public float projectileSpeed;
    public float projectileSize;
    public float projectileRange;
    public AudioClip soundEffect;

    public bool isDestructible;
    public DamageType lethalType;
    public GameObject destructionParticles;
}

[CreateAssetMenu(menuName = "Abilities/FireBall", fileName = "new Ability")]
public class FireBall : Ability
{

    [Header("--- NEW REFACTORED CONTAINER ---")]
    public FireBallSettings fireBallSettings;

    FireBallBehaviour fbBehaviour;

    public override GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster)
    {
        // Still uses abilityPrefab from base class layout containers
        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        fbBehaviour = instance.GetComponent<FireBallBehaviour>();
        if (fbBehaviour != null)
        {
            fbBehaviour.Initialize(this, this.baseSettings, this.fireBallSettings, this.abilityEffects, caster);

            if (fireBallSettings.isDestructible)
            {
                var dest = instance.AddComponent<DestructibleEnvironment>();
                dest.lethalType = this.fireBallSettings.lethalType;
                dest.destructionParticles = this.fireBallSettings.destructionParticles;

                // Ensure the object has an identity so it can recognize "Friends"
                var identity = instance.GetComponent<EntityIdentity>();
                if (identity == null) identity = instance.AddComponent<EntityIdentity>();
                identity.faction = this.baseSettings.myFaction;
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