using UnityEngine;
using UnityEngine.Events;

public class InteractableEnvironment : MonoBehaviour, IDamageable
{
    [Header("Interaction Settings")]
    [Tooltip("Minimum time required between hits to trigger another reaction.")]
    public float hitCooldown = 0.2f;

    [Tooltip("If checked, only the Player faction can trigger interactions on this object.")]
    public bool onlyInteractWithPlayer = true;

    [Header("Events")]
    public UnityEvent<HitInfo> OnInteracted;

    private float _nextHitTime;

    public void TakeDamage(HitInfo info)
    {
        // 1. Faction Check: Completely ignore hits that aren't from the Player
        if (onlyInteractWithPlayer && info.faction != Faction.Player)
        {
            return;
        }

        // 2. Cooldown check to prevent spamming
        if (Time.time < _nextHitTime) return;
        _nextHitTime = Time.time + hitCooldown;

        // 3. Broadcast the hit data safely to all listening modules
        OnInteracted?.Invoke(info);
    }
}