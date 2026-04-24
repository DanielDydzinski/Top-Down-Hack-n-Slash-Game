using System;
using UnityEngine;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    // Other scripts (Health, EffectManager) will subscribe to this
    public event Action<HitInfo> OnHitReceived;

    public void TakeDamage(HitInfo info)
    {
        // 1. Try to get the PSM
        PlayerStateMachine psm = GetComponent<PlayerStateMachine>();

        if (psm != null)
        {
            // 2. Check the Animator for the "Dodge" tag
            // We check both layer 0 (Base) and layer 2 (FullBody) just in case
            bool isDOdgeState = psm.anim.GetCurrentAnimatorStateInfo(psm.FullBodyLayer).IsTag("Dodge");
   ;

            if (isDOdgeState)
            {
                Debug.Log("Dodge! Damage Negated.");
                // Optional: Trigger a "Dodge" UI text or sound here
                return;
            }
        }


        // Shouts the info to anyone listening
        OnHitReceived?.Invoke(info);
    }
}