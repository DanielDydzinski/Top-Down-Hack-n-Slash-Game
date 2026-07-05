using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct ShockWaveSettings
{
    public GameObject shockWaveVisualPrefab;
    public float maxRadius;
    public float expansionSpeed;
    public bool useLOS; // use line of sight?
}

[CreateAssetMenu(menuName = "Abilities/ShockWave")]
public class ShockWave : Ability
{
    [Header("--- NEW REFACTORED CONTAINER ---")]
    public ShockWaveSettings shockWaveSettings;

    [Header("--- LEGACY SUBCLASS FIELDS (TEMPORARY UNTIL MIGRATION) ---")]
    public GameObject shockWaveVisualPrefab;
    public float maxRadius = 10f;
    public float expansionSpeed = 20f;
    public bool useLOS = true; // use line of sight?

    public override GameObject Cast(Vector3 position, Quaternion rotation, GameObject caster)
    {
        if (this.abilityPrefab == null) return null;

        // Instantiates using the abilityPrefab inherited from the unified base layout
        GameObject instance = Instantiate(this.abilityPrefab, position, rotation);
        ShockWaveBehaviour behaviour = instance.GetComponent<ShockWaveBehaviour>();

        if (behaviour != null)
        {
            // FIXED: Following your exact Melee pattern cleanly passing the 4 container targets!
            behaviour.Initialize(this,this.baseSettings, this.shockWaveSettings, this.abilityEffects, caster);
        }
        else
        {
            Debug.LogWarning($"ShockWave prefab needs a ShockWaveBehaviour component attached to it!");
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

        // 2. Migrate Subclass ShockWave variables safely
        shockWaveSettings.shockWaveVisualPrefab = this.shockWaveVisualPrefab;
        shockWaveSettings.maxRadius = this.maxRadius;
        shockWaveSettings.expansionSpeed = this.expansionSpeed;
        shockWaveSettings.useLOS = this.useLOS;

        Debug.Log($"[MIGRATION SUCCESS] Unified and structured ShockWave asset: {this.name}");
    }
}

#if UNITY_EDITOR
public class GlobalShockWaveMigratorShortcut
{
    [MenuItem("Tools/Migrate All ShockWave Data")]
    public static void RunGlobalMigration()
    {
        string[] guids = AssetDatabase.FindAssets("t:ShockWave");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShockWave ability = AssetDatabase.LoadAssetAtPath<ShockWave>(path);
            
            if (ability != null)
            {
                ability.MigrateEverything();
                EditorUtility.SetDirty(ability);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GLOBAL MIGRATION] Moved records inside {count} ShockWave assets smoothly!");
    }
}
#endif