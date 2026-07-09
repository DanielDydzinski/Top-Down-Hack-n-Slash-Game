using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilitySlotUI : MonoBehaviour
{
    public enum Track { Q, E, R, F }

    [Header("Track Selection")]
    [SerializeField] protected Track track;

    [Header("References")]
    [SerializeField] protected Image icon;
    [Tooltip("Radial cooldown wipe overlay. Leave unassigned for slots with no cooldown (e.g. Light combo).")]
    [SerializeField] protected Image cooldownOverlay;
    [SerializeField] protected AbilityManager abilityManager;
    [SerializeField] protected ComboController comboController;

    protected virtual void Awake()
    {
        if (abilityManager == null || comboController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (abilityManager == null) abilityManager = player.GetComponent<AbilityManager>();
                if (comboController == null) comboController = player.GetComponent<ComboController>();
            }
        }
    }

    protected virtual void Update()
    {
        if (comboController == null || abilityManager == null) return;

        Ability current = GetCurrentAbility();
        if (current == null)
        {
            if (icon != null) icon.enabled = false;
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            return;
        }

        if (icon != null)
        {
            icon.enabled = true;
            if (current.baseSettings.icon != null) icon.sprite = current.baseSettings.icon;
        }

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = GetCooldownFraction(current);
    }

    protected virtual Ability GetCurrentAbility()
    {
        List<Ability> list;
        int index;
        ComboController.ComboTrackId trackId;
        switch (track)
        {
            case Track.Q: list = comboController.qAbilities; index = comboController.qIndex; trackId = ComboController.ComboTrackId.Q; break;
            case Track.E: list = comboController.eAbilities; index = comboController.eIndex; trackId = ComboController.ComboTrackId.E; break;
            case Track.R: list = comboController.rAbilities; index = comboController.rIndex; trackId = ComboController.ComboTrackId.R; break;
            default: list = comboController.fAbilities; index = comboController.fIndex; trackId = ComboController.ComboTrackId.F; break;
        }
        // Show the ability that will fire on the NEXT press, not the one just used.
        return comboController.PeekNextAbility(trackId, index, list);
    }

    protected float GetCooldownFraction(Ability ability)
    {
        if (abilityManager.cooldowns == null) return 0f;
        if (!abilityManager.cooldowns.TryGetValue(ability.baseSettings.abilityName, out CoolDown cd)) return 0f;
        if (cd.coolDownReady) return 0f;

        float duration = ability.baseSettings.cooldown;
        return duration <= 0f ? 0f : Mathf.Clamp01(cd.timeLeft / duration);
    }
}
