using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR.Interaction;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("--- LEGACY SUBCLASS FIELDS (TEMPORARY UNTIL MIGRATION) ---")]
    public GameObject projectilePrefab;
    public Ability explosionAbility;

    public bool isBeam;
    public float projectileSpeed;
    public float projectileSize;
    public float projectileRange;
    public AudioClip soundEffect;

    public bool isDestructible = false;
    public DamageType lethalType;
    public GameObject destructionParticles;


    FireBallBehaviour fbBehaviour;

    public override GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster)
    {
        // Still uses abilityPrefab from base class layout containers
        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        fbBehaviour = instance.GetComponent<FireBallBehaviour>();
        if (fbBehaviour != null)
        {
            // UPDATED: Pulling directly from the newly decoupled clean structural layout containers
            fbBehaviour.UpdateValues(this.abilityEffects, fireBallSettings.projectileSpeed, fireBallSettings.projectileSize, fireBallSettings.projectileRange, fireBallSettings.projectilePrefab,
                fireBallSettings.explosionAbility, this.baseSettings.myFaction, this.baseSettings.damageType, fireBallSettings.soundEffect, fireBallSettings.isBeam, caster, this.baseSettings.targetLayer, this.baseSettings.wallLayer);

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

        // 2. Migrate Subclass FireBall variables safely
        fireBallSettings.projectilePrefab = this.projectilePrefab;
        fireBallSettings.explosionAbility = this.explosionAbility;
        fireBallSettings.isBeam = this.isBeam;
        fireBallSettings.projectileSpeed = this.projectileSpeed;
        fireBallSettings.projectileSize = this.projectileSize;
        fireBallSettings.projectileRange = this.projectileRange;
        fireBallSettings.soundEffect = this.soundEffect;
        fireBallSettings.isDestructible = this.isDestructible;
        fireBallSettings.lethalType = this.lethalType;
        fireBallSettings.destructionParticles = this.destructionParticles;

        Debug.Log($"[MIGRATION SUCCESS] Unified and structured FireBall asset: {this.name}");
    }
}

#if UNITY_EDITOR
public class GlobalFireBallMigratorShortcut
{
	[MenuItem("Tools/Migrate All FireBall Data")]
	public static void RunGlobalMigration()
	{
		string[] guids = AssetDatabase.FindAssets("t:FireBall");
		int count = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			FireBall ability = AssetDatabase.LoadAssetAtPath<FireBall>(path);
			
			if (ability != null)
			{
				ability.MigrateEverything();
				EditorUtility.SetDirty(ability);
				count++;
			}
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"[GLOBAL MIGRATION] Moved records inside {count} FireBall assets smoothly!");
	}
}
#endif