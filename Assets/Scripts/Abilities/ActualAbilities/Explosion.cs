using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct ExplosionSettings
{
    public GameObject explosionParticles;
    public float radius;
    public bool damageByDistance;
    public AudioClip soundEffect;
}

[CreateAssetMenu(menuName = "Abilities/Explosion", fileName = "new Ability")]
public class Explosion : Ability
{

    [Header("--- NEW REFACTORED CONTAINER ---")]
    public ExplosionSettings explosionSettings;

    [Header("--- LEGACY SUBCLASS FIELDS (TEMPORARY UNTIL MIGRATION) ---")]
    public GameObject explosionParticles;
    public float radius;
    public bool damageByDistance;
    public AudioClip soundEffect;


    ExplosionBehaviour explosionBehaviour;


    public override GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster)
    {
        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        explosionBehaviour = instance.GetComponent<ExplosionBehaviour>();
        if (explosionBehaviour != null)
        {
            // UPDATED: Now passing values cleanly from the refactored baseSettings and explosionSettings containers
            explosionBehaviour.UpdateValues(this.abilityEffects, explosionSettings.explosionParticles, explosionSettings.radius, this.baseSettings.myFaction,
                this.baseSettings.damageType, explosionSettings.damageByDistance, explosionSettings.soundEffect, caster, this.baseSettings.targetLayer, this.baseSettings.wallLayer);
        }
        else
        {
            Debug.Log("Explosion needs ExplosionBehaviour");
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

        // 2. Migrate Subclass Explosion variables safely
        explosionSettings.explosionParticles = this.explosionParticles;
        explosionSettings.radius = this.radius;
        explosionSettings.damageByDistance = this.damageByDistance;
        explosionSettings.soundEffect = this.soundEffect;

        Debug.Log($"[MIGRATION SUCCESS] Unified and structured Explosion asset: {this.name}");
    }
}

#if UNITY_EDITOR
public class GlobalExplosionMigratorShortcut
{
	[MenuItem("Tools/Migrate All Explosion Data")]
	public static void RunGlobalMigration()
	{
		string[] guids = AssetDatabase.FindAssets("t:Explosion");
		int count = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			Explosion ability = AssetDatabase.LoadAssetAtPath<Explosion>(path);
			
			if (ability != null)
			{
				ability.MigrateEverything();
				EditorUtility.SetDirty(ability);
				count++;
			}
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"[GLOBAL MIGRATION] Moved records inside {count} Explosion assets smoothly!");
	}
}
#endif