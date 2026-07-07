using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public enum AnimationLayer { UpperBody, FullBody }
public enum ComboTrack { Light, Heavy, Magic, Hidden }

public enum VisualAttachPoint { Root, LeftHand, RightHand, Head, Weapon }

[System.Serializable]
public struct BaseAbilitySettings
{
    [Header("Ability Info")]
    public string abilityName;
    public Sprite icon;
    public string description;

    [Header("Identity")]
    public Faction myFaction; // for Identity Checks
    public DamageType damageType;

    [Header("Physics Filtering")]
    public LayerMask targetLayer; // What this ability can target/damage
    public LayerMask wallLayer;   // What blocks this ability's line-of-sight

    [Header("Combo Settings")]
    public Ability nextComboAbility; // If null, combo ends
    public float comboWindow; // Time to press button again

    [Header("State Control")]
    public float movementMultiplier; // Slow down during cast? (e.g. 0.5f)
    public bool canRotateDuringCast;

    [Description("Lower the heavier turning during cast")]
    public float rotationOomph;
    public bool canMoveAttack;
    public int attackState; // animaton state transition condition value // calls an animation with this state //animation trigger CastAbility()
    public AnimationLayer animLayer;
    public float cooldown;
    public float requiredRange;
    public int priority;
    public bool isRanged;
    public ComboTrack track; // which combo sequence/track does this belong to - player only

    [Header("Dash Settings")]
    public Vector3 dashDirection;
    public float dashPower; //dash power during ability cast?
    [Range(0f, 1f)]
    public float dashStartTime;  // 0 to 1 - how far into animation to start dashing ( 0.1 = 10% into animation)
    [Range(0f, 1f)]
    public float dashEndTime;   // 0- 1 how far into animationtime to stop 0.9 = 90% of animation

    [Header("Vault Settings")]
    [Tooltip("One-shot upward velocity applied exactly when the dash window opens (dashStartTime). 0 or less disables the jump.")]
    public float verticalJumpForce;
    [Tooltip("CharacterController height while this ability's dash window is active. 0 or less leaves the controller size untouched.")]
    public float abilityControllerHeight;
    [Tooltip("Enables the mid-air freeze below. Only meaningful when verticalJumpForce > 0.")]
    public bool freezeInAir;
    [Range(0f, 1f)]
    [Tooltip("How far into the clip (0-1) to check whether we're still airborne - if so, the animation freezes there until landing.")]
    public float airFreezeCheckPoint;
    [Tooltip("Ground-distance threshold (meters) used at the check point above - closer to the ground than this counts as landed, not airborne, so the animation is not frozen.")]
    public float airbornHeightThreshold;
    [Tooltip("Zeros the carried-over horizontal drift the instant the player touches ground again, instead of letting it continue until the ability ends.")]
    public bool stopDashOnGrounded;

    [Header("Energy Settings")]
    [Tooltip("How much energy it costs to use this ability.")]
    public float energyCost;

    [Tooltip("How much energy the player gets back per enemy hit (e.g. for Light Attacks).")]
    public float energyGainOnHit;

    [Tooltip("Percentage (0 to 100) of the energyCost refunded to the player if this attack lands a killing blow.")]
    [Range(0f, 100f)]
    public float energyRefundOnKillPercent;

    public GameObject abilityVisualParticles; // visuals
    public VisualAttachPoint attachPoint; // Instead of public Transform
    public AudioClip audioClip;
    public AudioClip visualEffectAudio;

    [Header("Ability Positioning")]
    public Ability.abilitySpawnType spawnLocation;
    public Vector3 spawnLocationOffset;
    public Vector3 spawnRotationOffset;
}

public abstract class Ability : ScriptableObject
{

    [Header("--- NEW REFACTORED CONTAINER ---")]
    public BaseAbilitySettings baseSettings;

    [Header("Main Ability Prefab")]
    public GameObject abilityPrefab;

    [Header("List of Effects")]
    public List<Effect> abilityEffects;

    [Header("--- LEGACY BASE FIELDS (TEMPORARY UNTIL MIGRATION) ---")]
    [Header("Ability Info")]
    public string abilityName;
    public Sprite icon;
    public string description;

    [Header("Identity")]
    public Faction myFaction; // for Identity Checks
    public DamageType damageType;

    [Header("Physics Filtering")]
    public LayerMask targetLayer; // What this ability can target/damage
    public LayerMask wallLayer;   // What blocks this ability's line-of-sight

    [Header("Combo Settings")]
    public Ability nextComboAbility; // If null, combo ends
    public float comboWindow = 1.0f; // Time to press button again

    [Header("State Control")]
    public float movementMultiplier = 1.0f; // Slow down during cast? (e.g. 0.5f)
    public bool canRotateDuringCast;

    [Description("Lower the heavier turning during cast")]
    public float rotationOomph = 100f;
    public bool canMoveAttack;
    public int attackState; // animaton state transition condition value // calls an animation with this state //animation trigger CastAbility()
    public AnimationLayer animLayer;
    public float cooldown;
    public float requiredRange;
    public int priority;
    public bool isRanged;
    public ComboTrack track; // which combo sequence/track does this belong to - player only

    [Header("Dash Settings")]
    public Vector3 dashDirection;
    public float dashPower; //dash power during ability cast?
    [Range(0f, 1f)]
    public float dashStartTime;  // 0 to 1 - how far into animation to start dashing ( 0.1 = 10% into animation)
    [Range(0f, 1f)]
    public float dashEndTime;   // 0- 1 how far into animationtime to stop 0.9 = 90% of animation

    [Header("Energy Settings")]
    [Tooltip("How much energy it costs to use this ability.")]
    public float energyCost;

    [Tooltip("How much energy the player gets back per enemy hit (e.g. for Light Attacks).")]
    public float energyGainOnHit;

    [Tooltip("Percentage (0 to 100) of the energyCost refunded to the player if this attack lands a killing blow.")]
    [Range(0f, 100f)]
    public float energyRefundOnKillPercent;

    public GameObject abilityVisualPartyicles; // visuals
    public VisualAttachPoint attachPoint; // Instead of public Transform
    public AudioClip AudioClip;
    public AudioClip visualEffectAudio;

    public enum abilitySpawnType { Onself, OnTarget, SpecifiedPoint, PlayerRoot };
    [Header("Ability Positioning")]
    public abilitySpawnType spawnLocation;
    public Vector3 spawnLocationOffset;
    public Vector3 spawnRotationOffset;

    // --- THE AUTOMATED INSPECTOR SHORTCUT ---
    protected virtual void OnEnable()
    {
        // If the layer mask is unassigned (Nothing), automatically inject your game's defaults!
        // This ensures all your existing assets get updated with zero manual work.
        if (targetLayer == 0)
        {
            targetLayer = LayerMask.GetMask("Player");
        }

        if (wallLayer == 0)
        {
            // Matching your exact project spelling "Enviroment" from your layer window
            wallLayer = LayerMask.GetMask("Enviroment", "InteractableEnvironment");
        }
    }

    public abstract GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster);
}