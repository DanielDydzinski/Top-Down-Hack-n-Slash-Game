using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimationLayer { UpperBody, FullBody }
public enum ComboTrack { Light, Heavy, Magic }

public abstract class Ability : ScriptableObject{

    [Header("Main Ability Prefab")]
    public GameObject abilityPrefab;
    [Header("List of Effects")]
    public List<Effect> abilityEffects;
    [Header("Ability Info")]
    public string abilityName ;
	public Sprite icon ;
	public string description ;
    [Header("Identity")]
    public Faction myFaction; // for Identity Checks
	public DamageType damageType;
    [Header("Combo Settings")]
    public Ability nextComboAbility; // If null, combo ends
    public float comboWindow = 1.0f; // Time to press button again
    [Header("State Control")]
    public float movementMultiplier = 1.0f; // Slow down during cast? (e.g. 0.5f)
    public bool canRotateDuringCast;
    public bool canMoveAttack;
    public int attackState ; // animaton state transition condition value // calls an animation with this state //animation trigger CastAbility()
    public AnimationLayer animLayer;
	public float cooldown ;
    public float requiredRange;
	public int priority;
	public bool isRanged;
    public ComboTrack track; // which combo sequence/track does this belong to - player only
    [Header("Dash Settings")]
    public Vector3 dashDirection;
    public float dashPower; //dash power during ability cast?
    [Range(0f, 1f)]
    public float dashStartTime;  // 0 to 1 - how far into animation to start dashing ( 0.1 = 10% into animation)
    [Range(0f,1f)]
    public float dashEndTime;   // 0- 1 how far into animationtime to stop 0.9 = 90% of animation

    public GameObject abilityVisualPartyicles; // visuals


    public enum abilitySpawnType{Onself, OnTarget, SpecifiedPoint, PlayerRoot};
    [Header("Ability Positioning")]
    public abilitySpawnType spawnLocation;
	public Vector3 spawnLocationOffset;
    public Vector3 spawnRotationOffset;

	public abstract GameObject Cast (Vector3 pos, Quaternion rot);

}
