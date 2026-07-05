using System;
using UnityEngine;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    // Existing event for regular hits
    public event Action<HitInfo> OnHitReceived;

    // --- NEW: Expose a dedicated event for successful blocks ---
    public event Action<HitInfo> OnBlockSuccess;

    public void TakeDamage(HitInfo info)
    {
        PlayerStateMachine psm = GetComponent<PlayerStateMachine>();

        if (psm != null)
        {
            // Check the Animator for the "Dodge" tag
            bool isDOdgeState = psm.anim.GetCurrentAnimatorStateInfo(psm.FullBodyLayer).IsTag("Dodge");

            if (isDOdgeState)
            {
                Debug.Log("Dodge! Damage Negated.");
                return;
            }

            // BLOCK CHECK — resolved ONCE here, before OnHitReceived fans out.
            // HitInfo is a struct: if this ran later (e.g. inside Health.Damage), the
            // isBlocked flag would only mutate that call's local copy and never reach
            // the sibling copy that EffectManager hands to each Effect (TakeDamage, TakeDoT...).
            // Resolving it here means every listener's copy already has isBlocked set correctly.
            if (info.attackType != AttackType.DoT && psm.GetCurrentState() is BlockState blockState)
            {
                if (blockState.TryBlock(info))
                {
                    info.isBlocked = true;
                }
            }
        }

        // Shouts the info to anyone listening (like your Health script)
        OnHitReceived?.Invoke(info);
    }
}