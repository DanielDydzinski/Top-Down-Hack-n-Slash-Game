using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/ShockWave")]
public class ShockWave : Ability
{
    public GameObject shockWaveVisualPrefab;
    public float maxRadius = 10f;
    public float expansionSpeed = 20f;

    public override GameObject Cast(Vector3 position, Quaternion rotation, GameObject caster)
    {
        GameObject instance = Instantiate(this.abilityPrefab, position, rotation);
        ShockWaveBehaviour behaviour = instance.GetComponent<ShockWaveBehaviour>();

        if (behaviour != null)
        { 
            // Using the requested UpdateValues pattern
            behaviour.UpdateValues(this.abilityEffects, maxRadius, expansionSpeed, this.myFaction, this.damageType, caster, shockWaveVisualPrefab);
        }

        return instance;
    }
}