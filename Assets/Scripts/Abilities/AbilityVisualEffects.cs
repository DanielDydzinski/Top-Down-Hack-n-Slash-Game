using System;
using System.Collections;
using UnityEngine;

// Sibling of AbilityManager - subscribes to its cast-lifecycle events and owns all visual/audio
// cue spawning, the same relationship EffectManager has to DamageReceiver. AbilityManager never
// calls into this class directly; it only announces OnAbilityStarted/Executed/Canceled.
public class AbilityVisualEffects : MonoBehaviour
{
    [SerializeField] private AbilityManager abilityManager;

    public Transform VisualLeftHandAttachPoint, VisualRightHandAttachPoint, VisualHeadAttachPoint, VisualWeaponAttachPoint, VisualFeetAttachPoint;

    private GameObject activePersistentEffect;
    private float activePersistentEffectDelay;

    // Coroutines are stopped automatically when this component disables - tracked separately so
    // OnDisable can force-clean a still-pending delayed removal instead of leaking the instance.
    private GameObject pendingRemoveInstance;

    void Awake()
    {
        if (abilityManager == null) abilityManager = GetComponent<AbilityManager>();
    }

    void OnEnable()
    {
        abilityManager.OnAbilityStarted += HandleStarted;
        abilityManager.OnAbilityExecuted += HandleExecuted;
        abilityManager.OnAbilityCanceled += HandleCanceled;
    }

    void OnDisable()
    {
        abilityManager.OnAbilityStarted -= HandleStarted;
        abilityManager.OnAbilityExecuted -= HandleExecuted;
        abilityManager.OnAbilityCanceled -= HandleCanceled;

        if (pendingRemoveInstance != null)
        {
            RemoveInstance(pendingRemoveInstance);
            pendingRemoveInstance = null;
        }
    }

    private void HandleStarted(Ability ability) => FireCuesForTrigger(ability, CueTrigger.OnStart);

    // Execute always ends the "in-progress cast" phase, same as Cancel - a persistent Start-phase
    // effect (e.g. Kamehameha's charge glow) must be cut off here even if the Execute cue itself
    // isn't marked persistent, otherwise it never gets cleaned up on a normal, uninterrupted cast.
    private void HandleExecuted(Ability ability)
    {
        ClearPersistentEffect();
        FireCuesForTrigger(ability, CueTrigger.OnExecute);
    }

    private void HandleCanceled(Ability ability)
    {
        ClearPersistentEffect();
        FireCuesForTrigger(ability, CueTrigger.OnCancel);
    }

    // Animation-Event entry point for Manual-trigger cues (frame-accurate mid-clip beats that
    // don't correspond to a Start/Execute/Cancel lifecycle boundary).
    public void PlayAbilityVisualCue(string id)
    {
        var cue = abilityManager.activeAbility?.visualCues?.Find(c => c.id == id);
        if (cue != null) SpawnCue(cue);
    }

    [Obsolete("Superseded by cue-based auto-firing via OnAbilityExecuted. Kept as a no-op so the handful of existing Animation Events referencing this method don't log a missing-receiver warning; safe to delete once those events are removed from their .anim clips.")]
    public void PlayVisuals() { }

    private void FireCuesForTrigger(Ability ability, CueTrigger trigger)
    {
        if (ability?.visualCues == null) return;
        foreach (var cue in ability.visualCues)
        {
            if (cue.trigger == trigger) SpawnCue(cue);
        }
    }

    private void SpawnCue(AbilityVisualCue cue)
    {
        // Clearing the outgoing persistent effect happens immediately at trigger time regardless
        // of this cue's own delayStartTime - that's the semantic cast-phase boundary, independent
        // of when this cue's own visual actually appears.
        if (cue.persistUntilNextCue) ClearPersistentEffect();

        if (cue.delayStartTime > 0f) StartCoroutine(DelayedExecuteSpawn(cue));
        else ExecuteSpawn(cue);
    }

    private IEnumerator DelayedExecuteSpawn(AbilityVisualCue cue)
    {
        yield return new WaitForSeconds(cue.delayStartTime);
        ExecuteSpawn(cue);
    }

    private void ExecuteSpawn(AbilityVisualCue cue)
    {
        // Resolved at actual spawn time, not when the delay started, so a delayed cue still lines
        // up with wherever its attach point (e.g. a swinging hand) has moved to by then.
        Transform targetParent = ResolveAttachPoint(cue.attachPoint);
        Vector3 pos = targetParent.position + cue.positionOffset;
        Quaternion rot = targetParent.rotation * Quaternion.Euler(cue.rotationOffset);

        GameObject instance = null;
        if (cue.prefab != null)
        {
            // Same pool-first, Instantiate-fallback convention as MeleeAttackBehavaiour/AoEoTBehaviour etc.
            // persistUntilNextCue cues never pass autoReturnDelay - their lifetime is driven by
            // ClearPersistentEffect() instead (the next cue, or cast end), not a fixed timer.
            float delay = cue.persistUntilNextCue ? 0f : cue.autoReturnDelay;
            if (cue.parentToAttachPoint)
            {
                instance = ObjectPooler.Instance != null
                    ? ObjectPooler.Instance.SpawnFromPool(cue.prefab, pos, rot, targetParent, delay)
                    : Instantiate(cue.prefab, pos, rot, targetParent);
            }
            else
            {
                // World-space spawn - not parented, so it stays put where it appeared instead of
                // following the attach point (e.g. a hand) for the rest of its lifetime.
                instance = ObjectPooler.Instance != null
                    ? ObjectPooler.Instance.SpawnFromPool(cue.prefab, pos, rot, delay)
                    : Instantiate(cue.prefab, pos, rot);
            }
        }
        if (cue.audio != null) AudioSource.PlayClipAtPoint(cue.audio, transform.position);
        if (cue.persistUntilNextCue)
        {
            activePersistentEffect = instance;
            activePersistentEffectDelay = cue.delayedDestroyTime;
        }
    }

    private void ClearPersistentEffect()
    {
        if (activePersistentEffect == null) return;

        GameObject toRemove = activePersistentEffect;
        float delay = activePersistentEffectDelay;
        activePersistentEffect = null;
        activePersistentEffectDelay = 0f;

        if (delay > 0f)
        {
            pendingRemoveInstance = toRemove;
            StartCoroutine(DelayedRemoveInstance(toRemove, delay));
        }
        else
        {
            RemoveInstance(toRemove);
        }
    }

    private IEnumerator DelayedRemoveInstance(GameObject instance, float delay)
    {
        // Respects Time.timeScale (pause), same as everything else in the ability/effect pipeline.
        yield return new WaitForSeconds(delay);
        RemoveInstance(instance);
        if (pendingRemoveInstance == instance) pendingRemoveInstance = null;
    }

    private void RemoveInstance(GameObject instance)
    {
        if (instance == null) return;
        if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(instance);
        else Destroy(instance);
    }

    private Transform ResolveAttachPoint(VisualAttachPoint point)
    {
        switch (point)
        {
            case VisualAttachPoint.LeftHand: return VisualLeftHandAttachPoint != null ? VisualLeftHandAttachPoint : transform;
            case VisualAttachPoint.RightHand: return VisualRightHandAttachPoint != null ? VisualRightHandAttachPoint : transform;
            case VisualAttachPoint.Head: return VisualHeadAttachPoint != null ? VisualHeadAttachPoint : transform;
            case VisualAttachPoint.Weapon: return VisualWeaponAttachPoint != null ? VisualWeaponAttachPoint : transform;
            case VisualAttachPoint.Feet: return VisualFeetAttachPoint != null ? VisualFeetAttachPoint : transform;
            default: return transform;
        }
    }

    // Ground-level spawn point for the separate fall-distance groundImpactPrefab feature
    // (BaseAbilitySettings.scalesWithFallDistance) - used by every Behaviours/*.cs class instead of
    // their own transform.position, which is wherever AbilityManager.spawnLocation points (a
    // chest/hand-height socket for most abilities, not ground level). Falls back to the caster's own
    // root transform if no AbilityVisualEffects/VisualFeetAttachPoint is set up, then to
    // fallbackPosition if there's no caster at all.
    public static Vector3 ResolveGroundImpactPosition(GameObject caster, Vector3 fallbackPosition)
    {
        if (caster == null) return fallbackPosition;

        if (caster.TryGetComponent<AbilityVisualEffects>(out var vfx) && vfx.VisualFeetAttachPoint != null)
        {
            return vfx.VisualFeetAttachPoint.position;
        }

        return caster.transform.position;
    }
}
