using UnityEngine;

/// <summary>
/// Drop anywhere in the scene (or on the Camera / Player).
/// Exposes all dither-fade feature toggles in the Inspector AND as a static API
/// so you can flip them from any script at runtime.
///
/// Modes summary:
///   Player circle ON  + Mouse circle ON  → fade only inside both circles
///   Player circle ON  + Mouse circle OFF → fade only around player
///   Player circle OFF + Mouse circle ON  → fade only around mouse
///   Player circle OFF + Mouse circle OFF → whole obstructing object fades
///                                          (original asset behaviour)
/// </summary>
public class DitherFadeSettings : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static DitherFadeSettings Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Apply();
    }

    // ── Inspector fields ──────────────────────────────────────────────────

    [Header("Circle masks")]
    [Tooltip("Fade only inside a circle centred on the Player.\n" +
             "Requires DitherFadeCircleMask (Slot=Player) on your Player.")]
    public bool usePlayerCircle = true;

    [Tooltip("Fade only inside a circle centred on the Mouse world position.\n" +
             "Requires DitherFadeCircleMask (Slot=Mouse) on mouseWorldTransform.")]
    public bool useMouseCircle = true;

    // ── Shader global IDs ─────────────────────────────────────────────────
    private static readonly int IDUsePlayerCircle = Shader.PropertyToID("_DitherUsePlayerCircle");
    private static readonly int IDUseMouseCircle  = Shader.PropertyToID("_DitherUseMouseCircle");

    // ── Apply current settings to the shader ──────────────────────────────
    private void Apply()
    {
        Shader.SetGlobalFloat(IDUsePlayerCircle, usePlayerCircle ? 1f : 0f);
        Shader.SetGlobalFloat(IDUseMouseCircle,  useMouseCircle  ? 1f : 0f);
    }

#if UNITY_EDITOR
    // Re-apply whenever you toggle a checkbox in the Inspector during Play Mode
    private void OnValidate() => Apply();
#endif

    // ── Static API — call from any script ─────────────────────────────────

    /// <summary>Enable or disable the player-circle fade mask.</summary>
    public static void SetPlayerCircle(bool enabled)
    {
        if (Instance != null) Instance.usePlayerCircle = enabled;
        Shader.SetGlobalFloat(IDUsePlayerCircle, enabled ? 1f : 0f);
    }

    /// <summary>Enable or disable the mouse-circle fade mask.</summary>
    public static void SetMouseCircle(bool enabled)
    {
        if (Instance != null) Instance.useMouseCircle = enabled;
        Shader.SetGlobalFloat(IDUseMouseCircle, enabled ? 1f : 0f);
    }

    /// <summary>
    /// Convenience: disable both circles so the whole obstructing object fades
    /// (original Camera Object Fader behaviour, no reveal circles at all).
    /// </summary>
    public static void SetWholeObjectMode()
    {
        SetPlayerCircle(false);
        SetMouseCircle(false);
    }

    /// <summary>Restore both circles at once.</summary>
    public static void SetCircleMode(bool playerCircle = true, bool mouseCircle = true)
    {
        SetPlayerCircle(playerCircle);
        SetMouseCircle(mouseCircle);
    }
}
