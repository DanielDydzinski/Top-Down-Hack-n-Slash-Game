using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboSlotUI : AbilitySlotUI
{
    public enum ComboKind { Light, Heavy }

    [Header("Combo Track")]
    [SerializeField] private ComboKind comboKind;

    [Header("Combo Window Bar")]
    [Tooltip("Top-down fill that drains as the window to chain the next combo hit closes.")]
    [SerializeField] private Image windowBar;

    private ComboController.ComboTrackId TrackId =>
        comboKind == ComboKind.Light ? ComboController.ComboTrackId.Light : ComboController.ComboTrackId.Heavy;

    private List<Ability> ComboList =>
        comboKind == ComboKind.Light ? comboController.lightCombos : comboController.heavyCombos;

    private int ComboIndex =>
        comboKind == ComboKind.Light ? comboController.lightCombosIndex : comboController.heavyCombosIndex;

    protected override void Update()
    {
        base.Update(); // icon + (Heavy-only) cooldownOverlay via the overridden GetCurrentAbility() below

        if (windowBar == null || comboController == null) return;

        // The window bar tracks THIS track's own last-fired ability/time, not the preview
        // ability shown by GetCurrentAbility() - each track now keeps its own state.
        Ability lastFired = comboController.GetLastFiredAbility(TrackId);
        if (lastFired == null || lastFired.comboWindow <= 0f)
        {
            windowBar.fillAmount = 0f;
            return;
        }

        float elapsed = Time.time - comboController.GetLastInputTime(TrackId);
        if (elapsed >= lastFired.comboWindow)
        {
            windowBar.fillAmount = 0f;
            return;
        }

        windowBar.fillAmount = 1f - Mathf.Clamp01(elapsed / lastFired.comboWindow);
    }

    protected override Ability GetCurrentAbility()
    {
        // Show the ability that will fire on the NEXT press, not the one just used.
        return comboController.PeekNextAbility(TrackId, ComboIndex, ComboList);
    }
}
