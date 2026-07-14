using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct ShockWaveSettings
{
    public GameObject shockWaveVisualPrefab;
    public float maxRadius;
    public float expansionSpeed;
    public bool useLOS; // use line of sight?

    [Header("Fall Distance Radius Scaling")]
    [FormerlySerializedAs("scaleRadiusWithFallSpeed")]
    [Tooltip("Scales this shockwave's maxRadius by the same fall-distance factor used for damage (see BaseAbilitySettings.scalesWithFallDistance) - capped at double the base maxRadius no matter how high maxDistanceMultiplier is set, so a huge damage bonus doesn't also blow the blast radius out uncontrollably.")]
    public bool scaleRadiusWithFallDistance;

    [Header("Fade")]
    [Tooltip("Maps expansion progress (0 = just spawned, 1 = reached maxRadius) to _FadeAmount on the visual's materials (0 = fully visible, 1 = fully faded). Default is a straight line (fades evenly the whole time, matching the old hardcoded behavior). Curve it toward the bottom-right (e.g. stay near 0 until ~70% progress, then ramp to 1) to keep the material more visible for most of the expansion and only fade it out hard near the end.")]
    public AnimationCurve fadeCurve;
}

[CreateAssetMenu(menuName = "Abilities/ShockWave")]
public class ShockWave : Ability
{
    [Header("--- NEW REFACTORED CONTAINER ---")]
    public ShockWaveSettings shockWaveSettings;

    public override GameObject Cast(Vector3 position, Quaternion rotation, GameObject caster)
    {
        if (this.abilityPrefab == null) return null;

        // Instantiates using the abilityPrefab inherited from the unified base layout
        GameObject instance = SpawnAbilityInstance(this.abilityPrefab, position, rotation);
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
}
