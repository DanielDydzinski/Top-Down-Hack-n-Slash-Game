using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("--- LEGACY SUBCLASS FIELDS (TEMPORARY UNTIL MIGRATION) ---")]
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
        // Instantiates using the abilityPrefab inherited from the unified base layout
        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        aoeotBehaviour = instance.GetComponent<AoEoTBehaviour>();

        // 1. Pass standard behaviour values pulling from the clean structural layout containers
        aoeotBehaviour.UpdateValues(
            this.abilityEffects, aoEotSettings.duration, aoEotSettings.rate, aoEotSettings.radius, this.baseSettings.myFaction,
            aoEotSettings.abilityParticles, this.baseSettings.damageType, this.baseSettings.audioClip, caster,
            aoEotSettings.isPartial, aoEotSettings.strikeRadius, aoEotSettings.strikeParticles, aoEotSettings.spawnPositionOffset, aoEotSettings.strikeSound, this.baseSettings.targetLayer, this.baseSettings.wallLayer
        );

        // 2. Safely configure the Destructible trait if it exists on the prefab
        if (instance.TryGetComponent<DestructibleEnvironment>(out var dest))
        {
            if (aoEotSettings.isDestructible)
            {
                dest.enabled = true;
                dest.lethalType = this.aoEotSettings.lethalType;
                dest.destructionParticles = this.aoEotSettings.destructionParticles;

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

    [ContextMenu("MIGRATE EVERYTHING NOW")]
    public void MigrateEverything()
    {
        // 1. Migrate Base Ability variables safely
        baseSettings.abilityName = this.abilityName;
        baseSettings.icon = this.icon;
        baseSettings.description = this.description;
        baseSettings.myFaction = this.myFaction;
        baseSettings.damageType = this.damageType;
        baseSettings.targetLayer = this.targetLayer;
        baseSettings.wallLayer = this.wallLayer;
        baseSettings.nextComboAbility = this.nextComboAbility;
        baseSettings.comboWindow = this.comboWindow;
        baseSettings.movementMultiplier = this.movementMultiplier;
        baseSettings.canRotateDuringCast = this.canRotateDuringCast;
        baseSettings.rotationOomph = this.rotationOomph;
        baseSettings.canMoveAttack = this.canMoveAttack;
        baseSettings.attackState = this.attackState;
        baseSettings.animLayer = this.animLayer;
        baseSettings.cooldown = this.cooldown;
        baseSettings.requiredRange = this.requiredRange;
        baseSettings.priority = this.priority;
        baseSettings.isRanged = this.isRanged;
        baseSettings.track = this.track;
        baseSettings.dashDirection = this.dashDirection;
        baseSettings.dashPower = this.dashPower;
        baseSettings.dashStartTime = this.dashStartTime;
        baseSettings.dashEndTime = this.dashEndTime;
        baseSettings.energyCost = this.energyCost;
        baseSettings.energyGainOnHit = this.energyGainOnHit;
        baseSettings.energyRefundOnKillPercent = this.energyRefundOnKillPercent;
        baseSettings.abilityVisualParticles = this.abilityVisualPartyicles; // Maps matching legacy layout spelling
        baseSettings.attachPoint = this.attachPoint;
        baseSettings.audioClip = this.AudioClip;
        baseSettings.visualEffectAudio = this.visualEffectAudio;
        baseSettings.spawnLocation = (Ability.abilitySpawnType)this.spawnLocation;
        baseSettings.spawnLocationOffset = this.spawnLocationOffset;
        baseSettings.spawnRotationOffset = this.spawnRotationOffset;

        // 2. Migrate Subclass AoEoT variables safely
        aoEotSettings.abilityParticles = this.abilityParticles;
        aoEotSettings.radius = this.radius;
        aoEotSettings.duration = this.duration;
        aoEotSettings.rate = this.rate;
        aoEotSettings.isPartial = this.isPartial;
        aoEotSettings.strikeRadius = this.strikeRadius;
        aoEotSettings.strikeParticles = this.strikeParticles;
        aoEotSettings.spawnPositionOffset = this.spawnPositionOffset;
        aoEotSettings.strikeSound = this.strikeSound;
        aoEotSettings.isDestructible = this.isDestructible;
        aoEotSettings.lethalType = this.lethalType;
        aoEotSettings.destructionParticles = this.destructionParticles;

        Debug.Log($"[MIGRATION SUCCESS] Unified and structured AoEoT asset: {this.name}");
    }
}

#if UNITY_EDITOR
public class GlobalAoEoTMigratorShortcut
{
    [MenuItem("Tools/Migrate All AoEoT Data")]
    public static void RunGlobalMigration()
    {
        string[] guids = AssetDatabase.FindAssets("t:AoEoT");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AoEoT ability = AssetDatabase.LoadAssetAtPath<AoEoT>(path);
            
            if (ability != null)
            {
                ability.MigrateEverything();
                EditorUtility.SetDirty(ability);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GLOBAL MIGRATION] Moved records inside {count} AoEoT assets smoothly!");
    }
}
#endif