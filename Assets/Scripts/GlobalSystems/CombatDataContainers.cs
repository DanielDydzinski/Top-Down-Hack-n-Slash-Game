using System.Collections.Generic;
using UnityEngine;

// Global identifiers
public enum Faction { Player, Enemy, Neutral, Environment }
public enum DamageType { Physical, Fire, Frost, Poison, Magic }

// The "Envelope" that carries hit data
public struct HitInfo
{
    public DamageType type;
    public List<Effect> effects;
    public GameObject attacker; // Handy if you want to know who shot you
    public Faction faction;
    public float multiplier;
    public Vector3 forceDirection;
}

// The "Mail Slot" interface
public interface IDamageable
{
    void TakeDamage(HitInfo info);
}