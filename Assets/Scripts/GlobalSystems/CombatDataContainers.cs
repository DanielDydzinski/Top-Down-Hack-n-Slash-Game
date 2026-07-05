using System.Collections.Generic;
using UnityEngine;

// Global identifiers
public enum Faction { Player, Enemy, Neutral, Environment }
public enum DamageType { Physical, Fire, Frost, Poison, Magic }
public enum AttackType
{
    Melee,
    Ranged,
    Magic,
    DoT
}
// The "Envelope" that carries hit data
public struct HitInfo
{
    public Ability sourceAbility;

    public float damage;
    public DamageType type;
    public AttackType attackType;
    public List<Effect> effects;
    public GameObject attacker; // Handy if you want to know who shot you
    public Faction faction;
    public float multiplier;
    public Vector3 forceDirection;
    // Allows projectiles or spells to override the target's default death type
    public DeathHandler.DeathType? overrideDeathType;
    public bool isExplosion;
    public Vector3 impactPoint;
    public bool isBlocked;
    public float energyCostPaid;            // How much energy this specific attack cost to use
    public float energyGainOnHit;           // Energy rewarded to the attacker when a hit connects
    public float energyRefundOnKillPercent; // Percentage (0-100) of cost refunded if this hit kills
}

// The "Mail Slot" interface
public interface IDamageable
{
    void TakeDamage(HitInfo info);
}