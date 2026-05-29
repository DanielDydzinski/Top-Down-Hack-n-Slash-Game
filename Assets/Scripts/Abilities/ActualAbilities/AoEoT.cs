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

        // Pass everything smoothly to the runtime behaviour engine
        aoeotBehaviour.UpdateValues(
            this.abilityEffects, duration, rate, radius, this.myFaction,
            abilityParticles, this.damageType, this.AudioClip, caster,
            isPartial, strikeRadius, strikeParticles, spawnPositionOffset, strikeSound
        );

        if (isDestructible)
        {
            var dest = instance.AddComponent<DestructibleEnvironment>();
            dest.lethalType = this.lethalType;
            dest.destructionParticles = this.destructionParticles;

            var identity = instance.GetComponent<EntityIdentity>();
            if (identity == null) identity = instance.AddComponent<EntityIdentity>();
            identity.faction = this.myFaction;
        }

        return instance;
    }
}