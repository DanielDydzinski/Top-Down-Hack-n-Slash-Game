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
        GameObject instance = SpawnAbilityInstance(abilityPrefab, pos, rot);

        fbBehaviour = instance.GetComponent<FireBallBehaviour>();
        if (fbBehaviour != null)
        {
            fbBehaviour.Initialize(this, this.baseSettings, this.fireBallSettings, this.abilityEffects, caster);

            if (fireBallSettings.isDestructible)
            {
                // Guarded (not a blind AddComponent) because a pooled instance may already carry one
                // from an earlier cast - a bare AddComponent would stack a duplicate on every reuse.
                if (!instance.TryGetComponent<DestructibleEnvironment>(out var dest))
                {
                    dest = instance.AddComponent<DestructibleEnvironment>();
                }
                dest.enabled = true;
                dest.lethalType = this.fireBallSettings.lethalType;
                dest.destructionParticles = this.fireBallSettings.destructionParticles;
                dest.ResetState();

                // Ensure the object has an identity so it can recognize "Friends"
                var identity = instance.GetComponent<EntityIdentity>();
                if (identity == null) identity = instance.AddComponent<EntityIdentity>();
                identity.faction = this.baseSettings.myFaction;
            }
            else if (instance.TryGetComponent<DestructibleEnvironment>(out var staleDest))
            {
                // A prior pooled reuse may have left this enabled for a different, destructible cast.
                staleDest.enabled = false;
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