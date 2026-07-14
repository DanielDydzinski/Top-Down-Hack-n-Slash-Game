using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AoEoTSettings
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
    public Vector3 spawnPositionOffset;
    public AudioClip strikeSound;

    [Header("Destruction Trait")]
    public bool isDestructible;
    public DamageType lethalType;
    public GameObject destructionParticles;
}

[CreateAssetMenu(menuName = "Abilities/AoEoT", fileName = "new Ability")]
public class AoEoT : Ability
{
    [Header("--- NEW REFACTORED CONTAINER ---")]
    public AoEoTSettings aoEotSettings;

    AoEoTBehaviour aoeotBehaviour;

    public override GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster)
    {
        // Instantiates using the abilityPrefab inherited from the unified base layout
        GameObject instance = SpawnAbilityInstance(abilityPrefab, pos, rot);

        aoeotBehaviour = instance.GetComponent<AoEoTBehaviour>();

        // 1. Pass standard behaviour values pulling from the clean structural layout containers
        aoeotBehaviour.Initialize(this, this.baseSettings, this.aoEotSettings, this.abilityEffects, caster);

        // 2. Safely configure the Destructible trait if it exists on the prefab
        if (instance.TryGetComponent<DestructibleEnvironment>(out var dest))
        {
            if (aoEotSettings.isDestructible)
            {
                dest.enabled = true;
                dest.lethalType = this.aoEotSettings.lethalType;
                dest.destructionParticles = this.aoEotSettings.destructionParticles;
                dest.ResetState();

                // Manage identity safely without hidden AddComponent if possible
                if (!instance.TryGetComponent<EntityIdentity>(out var identity))
                {
                    identity = instance.AddComponent<EntityIdentity>();
                }
                identity.faction = this.baseSettings.myFaction;
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
