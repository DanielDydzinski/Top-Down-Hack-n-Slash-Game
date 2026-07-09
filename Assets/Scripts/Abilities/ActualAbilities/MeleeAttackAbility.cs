using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
