using System;
using UnityEngine;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    // Other scripts (Health, EffectManager) will subscribe to this
    public event Action<HitInfo> OnHitReceived;

    public void TakeDamage(HitInfo info)
    {
        // Shouts the info to anyone listening
        OnHitReceived?.Invoke(info);
    }
}