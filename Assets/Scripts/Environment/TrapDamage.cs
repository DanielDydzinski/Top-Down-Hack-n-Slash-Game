using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// A trigger-volume damage source (spikes, fire, poison gas, etc.) that reuses the same
// HitInfo/IDamageable pipeline as player and enemy abilities, so it automatically gets
// dodge/block resolution (DamageReceiver), Health's flinch/death handling, and EffectManager's
// coroutine-driven Effect list for free. See Behaviours/MeleeAttackBehavaiour.cs for the same
// build-HitInfo-and-call-TakeDamage pattern this mirrors.
public class TrapDamage : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Layers this trap scans for targets on (e.g. Player, Enemy).")]
    public LayerMask targetLayer;

    [Header("Targeting")]
    public bool affectsPlayer = true;
    public bool affectsEnemies = false;

    [Header("Damage")]
    public DamageType damageType = DamageType.Physical;
    public AttackType attackType = AttackType.Melee;
    public List<Effect> effects = new List<Effect>();
    public float multiplier = 1f;

    [Tooltip("If true, this hit skips DamageReceiver's dodge/block resolution - traps can't be dodged or blocked.")]
    public bool unavoidable = true;

    [Header("Retrigger")]
    [Tooltip("Minimum time before this trap can hit the same target again.")]
    public float hitCooldown = 1f;

    [Header("Events")]
    public UnityEvent<HitInfo> OnTrapTriggered;

    private readonly Dictionary<Collider, float> _nextHitTime = new Dictionary<Collider, float>();

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerExit(Collider other)
    {
        _nextHitTime.Remove(other);
    }

    private void TryHit(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) == 0) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        // Without an EntityIdentity we can't tell player from enemy, so skip rather than guess -
        // unlike attack behaviours (which default to "hit" when identity is missing), a trap gated
        // by affectsPlayer/affectsEnemies has no safe default to fall back on.
        EntityIdentity identity = other.GetComponent<EntityIdentity>();
        if (identity == null) return;

        bool allowed = (identity.faction == Faction.Player && affectsPlayer)
                    || (identity.faction == Faction.Enemy && affectsEnemies);
        if (!allowed) return;

        if (_nextHitTime.TryGetValue(other, out float nextTime) && Time.time < nextTime) return;
        _nextHitTime[other] = Time.time + hitCooldown;

        foreach (Effect e in effects)
        {
            if (e is TakeKnockBack kb) kb.SetExplosionPos(transform.position);
        }

        HitInfo info = new HitInfo
        {
            faction = Faction.Environment,
            type = damageType,
            attackType = attackType,
            effects = effects,
            attacker = this.gameObject,
            multiplier = multiplier,
            forceDirection = (other.transform.position - transform.position).normalized,
            impactPoint = other.ClosestPoint(transform.position),
            unavoidable = unavoidable
        };

        damageable.TakeDamage(info);
        OnTrapTriggered?.Invoke(info);
    }
}
