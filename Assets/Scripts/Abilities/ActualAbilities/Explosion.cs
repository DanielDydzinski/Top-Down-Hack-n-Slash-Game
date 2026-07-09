using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    ExplosionBehaviour explosionBehaviour;


    public override GameObject Cast(Vector3 pos, Quaternion rot, GameObject caster)
    {
        GameObject instance = Instantiate(abilityPrefab, pos, rot);

        explosionBehaviour = instance.GetComponent<ExplosionBehaviour>();
        if (explosionBehaviour != null)
        {
            explosionBehaviour.Initialize(this, this.baseSettings, this.explosionSettings, this.abilityEffects, caster);
        }
        else
        {
            Debug.Log("Explosion needs ExplosionBehaviour");
            return null;
        }

        return instance;
    }
}
