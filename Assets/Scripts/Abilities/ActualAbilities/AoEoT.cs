using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/AoEoT", fileName = "new Ability")]
public class AoEoT : Ability
{
    [Header("Standard Area Settings")]
    public GameObject abilityParticles;
    public float radius;
    public float duration;
    public float rate;

    [Header("Partial Strike Configuration")]
    public bool isPartial;
    public float strikeRadius;
    public GameObject strikeParticles;
    [Tooltip("Offset applied to visual prefab spawn. Use Y value if you want objects to spawn above ground level (e.g., Meteors).")]
    public Vector3 spawnPositionOffset;
    public AudioClip strikeSound;

    [Header("Destruction Trait")]
    public bool isDestructible;
    public DamageType lethalType;
    public GameObject destructionParticles;

    AoEoTBehaviour aoeotBehaviour;

    public override GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster)
    {
        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        aoeotBehaviour = instance.GetComponent<AoEoTBehaviour>();

        // 1. Pass standard behaviour values
        aoeotBehaviour.UpdateValues(
            this.abilityEffects, duration, rate, radius, this.myFaction,
            abilityParticles, this.damageType, this.AudioClip, caster,
            isPartial, strikeRadius, strikeParticles, spawnPositionOffset, strikeSound, this.targetLayer,this.wallLayer
        );

        // 2. Safely configure the Destructible trait if it exists on the prefab
        if (instance.TryGetComponent<DestructibleEnvironment>(out var dest))
        {
            if (isDestructible)
            {
                dest.enabled = true;
                dest.lethalType = this.lethalType;
                dest.destructionParticles = this.destructionParticles;

                // Manage identity safely without hidden AddComponent if possible
                if (!instance.TryGetComponent<EntityIdentity>(out var identity))
                {
                    identity = instance.AddComponent<EntityIdentity>();
                }
                identity.faction = this.myFaction;
            }
            else
            {
                // If this specific ability shouldn't be breakable, kill the script!
                dest.enabled = false;
            }
        }

        return instance;
    }
}