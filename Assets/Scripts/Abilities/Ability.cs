using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Ability : ScriptableObject{

	
	public GameObject abilityPrefab;
	public List<Effect> abilityEffects;
	public string abilityName ;
	public Sprite icon ;
	public string description ;
	public string targetTag ; // collision will be checking for this tag
	public string friendTag ;//collision will ignore this tag
	public int attackState ; // animaton state transition condition value // calls an animation with this state //animation trigger CastAbility()
	public float cooldown ;
    public float requiredRange;
	public int priority;
	public bool isRanged;
	public bool canMoveAttack;
    public enum abilitySpawnType{Onself, OnTarget, SpecifiedPoint};
	public abilitySpawnType spawnLocation;

	public abstract GameObject Cast (Vector3 pos, Quaternion rot);

}
