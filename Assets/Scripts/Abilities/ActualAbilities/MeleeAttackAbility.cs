using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct MeleeAttackSettings
{
    public GameObject attackParticles;
    public float length; // length of hit box
    public Vector3 halfExtents;
    public int howManyEnemiesToHit;
    public AudioClip missHitAudio;
}

[CreateAssetMenu(menuName = "Abilities/MeleeAttack", fileName = "new Ability")]
public class MeleeAttackAbility : Ability
{

    [Header("--- NEW REFACTORED CONTAINER ---")]
    public MeleeAttackSettings meleeSettings;

    [Header("--- LEGACY SUBCLASS FIELDS (TEMPORARY UNTIL MIGRATION) ---")]
    public GameObject attackParticles;
    public float length; // length of hit box
    public Vector3 halfExtents;
    public int howManyEnemiesToHit = 1;
    public AudioClip missHitAudio;

    MeleeAttackBehavaiour meleeAttackBehavaiour;

    public override GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster)
    {
        if (abilityPrefab == null) return null;

        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        meleeAttackBehavaiour = instance.GetComponent<MeleeAttackBehavaiour>();
        if (meleeAttackBehavaiour != null)
        {
            // Passing exactly 4 arguments: base settings, melee settings, effects list, and caster
            meleeAttackBehavaiour.Initialize(this,this.baseSettings, this.meleeSettings, this.abilityEffects, caster);
        }
        else
        {
            Debug.Log("MeleeAttack needs MeleeAttackBehavaiour");
            return null;
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

        // 2. Migrate Subclass Melee variables safely
        meleeSettings.attackParticles = this.attackParticles;
        meleeSettings.length = this.length;
        meleeSettings.halfExtents = this.halfExtents;
        meleeSettings.howManyEnemiesToHit = this.howManyEnemiesToHit;
        meleeSettings.missHitAudio = this.missHitAudio;

        Debug.Log($"[MIGRATION SUCCESS] Unified and structured: {this.name}");
    }
}

#if UNITY_EDITOR
public class GlobalAbilityMigratorShortcut
{
    [MenuItem("Tools/Migrate All Ability Data")]
    public static void RunGlobalMigration() 
    {
        string[] guids = AssetDatabase.FindAssets("t:MeleeAttackAbility");
        int count = 0;

        foreach (string guid in guids) 
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MeleeAttackAbility ability = AssetDatabase.LoadAssetAtPath<MeleeAttackAbility>(path);
            
            if (ability != null) 
            {
                ability.MigrateEverything();
                EditorUtility.SetDirty(ability);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GLOBAL MIGRATION] Moved records inside {count} assets smoothly!");
    }
}
#endif