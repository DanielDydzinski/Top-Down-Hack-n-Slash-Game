using UnityEngine;

public enum CueTrigger { Manual, OnStart, OnExecute, OnCancel }

[System.Serializable]
public class AbilityVisualCue
{
    [Tooltip("Key for this cue, used by PlayAbilityVisualCue(id) when an Animation Event needs to fire it at a frame-accurate moment that isn't Start/Execute/Cancel.")]
    public string id;

    [Tooltip("Manual = only fires via PlayAbilityVisualCue(id) from an Animation Event. OnStart/OnExecute/OnCancel fire automatically off AbilityManager's cast-lifecycle events - no Animation Event needed for those.")]
    public CueTrigger trigger = CueTrigger.Manual;

    [Header("Visual")]
    public GameObject prefab;
    public VisualAttachPoint attachPoint = VisualAttachPoint.Root;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Tooltip("If true (default), the spawned instance is parented to the resolved attachPoint, so it follows that transform (e.g. a hand) for as long as it exists. If false, it's spawned once at that world position/rotation and left unparented - it won't follow the caster afterward, useful for a ground-anchored effect or anything that should stay put even if the caster moves away.")]
    public bool parentToAttachPoint = true;

    [Header("Audio")]
    public AudioClip audio;

    [Header("Timing")]
    [Tooltip("0 (default) = spawn the instant this cue's trigger fires. >0 = wait this many seconds before actually spawning the prefab/playing the audio, so a cue can appear a beat late without needing a Start Delay baked into every particle system that wants one. Applies to any cue - Manual, OnStart, OnExecute, or OnCancel.")]
    public float delayStartTime;

    [Header("Lifecycle")]
    [Tooltip("If true, the spawned instance is tracked and force-returned to the pool the moment any other persistent cue fires for this cast, or the cast ends via Execute/Cancel - use this for a looping charge-up effect that must be cut off cleanly. If false (default), the instance is spawned and forgotten - it's expected to retire itself (a DestroyObject component on the prefab, or the particle system's own Stop Action).")]
    public bool persistUntilNextCue;

    [Tooltip("0 (default) = rely entirely on the prefab's own lifetime management (DestroyObject component or particle Stop Action), same as every other pooled VFX spawn in this codebase. >0 = force-return to the pool after this many seconds regardless, for prefabs that don't self-retire. Ignored when persistUntilNextCue is true, since that lifecycle is driven by the next cue/cast end instead.")]
    public float autoReturnDelay;

    [Tooltip("Only applies when persistUntilNextCue is true. 0 (default) = cut the effect the instant it's replaced/the cast ends (Execute/Cancel/another persistent cue). >0 = keep it alive for this many extra seconds before actually returning it to the pool, so a hard particle burst/fade can finish playing instead of vanishing on the same frame the next cue starts. Not tied to any specific phase - any persistent cue on any ability can use this to smooth its own handoff.")]
    public float delayedDestroyTime;
}
