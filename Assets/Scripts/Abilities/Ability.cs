using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;

public enum AnimationLayer { UpperBody, FullBody }
public enum ComboTrack { Light, Heavy, Magic, Hidden }

public enum VisualAttachPoint { Root, LeftHand, RightHand, Head, Weapon, Feet }

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

    [Header("Fall Distance Damage Scaling")]
    [FormerlySerializedAs("scalesWithFallSpeed")]
    [Tooltip("When enabled, this ability's damage/impact multiplier scales with how far (meters) the caster last fell before landing (see Mover.GetLastFallDistance). Used by mid-air Q/DodgeHeavy attacks and their grounded vault-jump (freezeInAir) counterparts. Off by default - has no effect unless set.")]
    public bool scalesWithFallDistance;
    [Tooltip("Fall distance (meters) at or below which no bonus is applied.")]
    public float minDistanceForBonus;
    [Tooltip("Fall distance (meters) at or above which the multiplier caps at maxDistanceMultiplier.")]
    public float maxDistanceForBonus;
    [FormerlySerializedAs("maxFallSpeedMultiplier")]
    [Tooltip("Damage multiplier applied at/above maxDistanceForBonus. 1 = no bonus.")]
    public float maxDistanceMultiplier;

    [Tooltip("VFX prefab spawned where this ability resolves when scalesWithFallDistance is on - different abilities can use different impact prefabs. Scaled to match this ability's own fall-distance scaling (e.g. ShockWave matches it exactly to its blast radius - see ShockWaveBehaviour). Null = no ability-specific impact; the player's default ground impact plays instead for a plain fall (see PlayerStateMachine.defaultGroundImpactPrefab).")]
    public GameObject groundImpactPrefab;
}

public abstract class Ability : ScriptableObject
{
    public enum abilitySpawnType { Onself, OnTarget, SpecifiedPoint, PlayerRoot };

    [Header("--- NEW REFACTORED CONTAINER ---")]
    public BaseAbilitySettings baseSettings;

    [Header("Main Ability Prefab")]
    public GameObject abilityPrefab;

    [Header("List of Effects")]
    public List<Effect> abilityEffects;

    [Header("Visual Cues")]
    public List<AbilityVisualCue> visualCues;

    // --- THE AUTOMATED INSPECTOR SHORTCUT ---
    protected virtual void OnEnable()
    {
        // If the layer mask is unassigned (Nothing), automatically inject your game's defaults!
        // This ensures all your existing assets get updated with zero manual work.
        if (baseSettings.targetLayer == 0)
        {
            baseSettings.targetLayer = LayerMask.GetMask("Player");
        }

        if (baseSettings.wallLayer == 0)
        {
            // Matching your exact project spelling "Enviroment" from your layer window
            baseSettings.wallLayer = LayerMask.GetMask("Enviroment", "InteractableEnvironment");
        }
    }

    // Hard cap for any visual (ground impact prefab, ShockWave's blast radius, etc.) that scales off
    // the fall-distance multiplier - kept separate from maxDistanceMultiplier (which only governs
    // damage) so a huge damage bonus can't also blow a visual out uncontrollably.
    public const float MaxFallDistanceVisualScale = 2f;

    // Remaps fallDistanceMultiplier's position within its own [1, maxDistanceMultiplier] range onto
    // the visual's separate [1, MaxFallDistanceVisualScale] range, so the visual grows at the same
    // proportional rate as the damage bonus instead of snapping straight to the visual cap the moment
    // the (much larger) damage multiplier range happens to exceed it. Same InverseLerp/Lerp idiom as
    // PlayerStateMachine.HandleLanded uses for the default (non-ability) ground impact.
    public static float GetVisualFallDistanceScale(float fallDistanceMultiplier, float maxDistanceMultiplier)
    {
        if (maxDistanceMultiplier <= 1f) return 1f;
        float t = Mathf.InverseLerp(1f, maxDistanceMultiplier, fallDistanceMultiplier);
        return Mathf.Lerp(1f, MaxFallDistanceVisualScale, t);
    }

    // Resolves the real point of contact on a target's collider rather than its (possibly
    // far-off-surface) transform.position/pivot - used for both LOS raycast aim and falloff-distance
    // measurement, so a pivot offset from its hitbox (tall enemies, off-center capsules) doesn't throw
    // off either. Ported from MeleeAttackBehavaiour.GetTrueImpactPoint - same logic, shared so
    // ShockWaveBehaviour/ExplosionBehaviour stop measuring against col.transform.position.
    // castHitPoint is an already-known cast/raycast hit point if one exists (Vector3.zero if not).
    public static Vector3 GetTrueImpactPoint(Collider col, Vector3 castOrigin, Vector3 castHitPoint)
    {
        if (castHitPoint != Vector3.zero && Vector3.Distance(castHitPoint, castOrigin) > 0.05f)
        {
            return castHitPoint;
        }

        Vector3 closest = col.ClosestPoint(castOrigin);
        if (closest != Vector3.zero && closest != castOrigin)
        {
            return closest;
        }

        Vector3 dirToCenter = (col.bounds.center - castOrigin).normalized;
        Ray skinRay = new Ray(castOrigin - (dirToCenter * 0.5f), dirToCenter);

        if (col.Raycast(skinRay, out RaycastHit skinHit, 20f))
        {
            return skinHit.point;
        }

        return col.bounds.ClosestPoint(castOrigin);
    }

    // Shared pool-first spawn/retire for the root abilityPrefab instance each Cast() creates -
    // every ActualAbilities/*.cs Cast() should route through these instead of calling
    // Instantiate/Destroy directly, so the root ability GameObject (projectile, AoE volume, etc.)
    // is reused instead of paying a fresh Instantiate cost - and a cold first-cast hitch - every time.
    public static GameObject SpawnAbilityInstance(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        return ObjectPooler.Instance != null
            ? ObjectPooler.Instance.SpawnFromPool(prefab, pos, rot)
            : Instantiate(prefab, pos, rot);
    }

    public static void RetireAbilityInstance(GameObject instance)
    {
        if (instance == null) return;

        if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(instance);
        else Destroy(instance);
    }

    public abstract GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster);
}
