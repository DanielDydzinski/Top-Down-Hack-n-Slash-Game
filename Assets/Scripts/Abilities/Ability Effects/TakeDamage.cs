using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Damage", fileName = "new Effect")]
public class TakeDamage : Effect
{

    public float damageAmount;

    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        Health hp = target.GetComponent<Health>();
        AudioSource audioS = target.GetComponent<AudioSource>();

        // Account for damage multiplier tracking
        float finalDamage = damageAmount * info.multiplier;
        info.damage = finalDamage;

        if (hp != null && !hp.GetisDead())
        {
            // 1. Determine if this specific damage slice is fatal BEFORE applying it
            bool isKillingBlow = (hp.Gethealth() - finalDamage <= 0f);

            // 2. Commit the structural damage to health points
            hp.Damage(finalDamage, info);

            // 3. Process energy tracking models
            if (info.attacker != null && info.sourceAbility != null)
            {
                PlayerEnergy attackerEnergy = info.attacker.GetComponent<PlayerEnergy>();

                if (attackerEnergy != null)
                {
                    // CASE A: Standard Gain Energy On Hit (Always occurs if configured)
                    float gainAmount = info.sourceAbility.baseSettings.energyGainOnHit;
                    if (gainAmount > 0)
                    {
                        attackerEnergy.GainEnergy(gainAmount);
                    }

                    // CASE B: Delayed/Instant Percent Refund On Kill 
                    if (isKillingBlow)
                    {
                        float cost = info.sourceAbility.baseSettings.energyCost;
                        float refundPercent = info.sourceAbility.baseSettings.energyRefundOnKillPercent;

                        if (refundPercent > 0 && cost > 0)
                        {
                            float energyToRefund = cost * (refundPercent / 100f);
                            attackerEnergy.GainEnergy(energyToRefund);

                           // Debug.Log($"[DEATH REFUND] Target killed via {info.sourceAbility.baseSettings.abilityName}. Refunded: {energyToRefund} EP.");
                        }
                    }
                }
            }
        }

        if (effectParticles != null)
        {
            var effectSpawnLocations = target.GetComponent<EffectSpawnPossitions>();
            if (effectSpawnLocations != null)
            {
                Instantiate(effectParticles, effectSpawnLocations.center);
            }
        }

        if (audioS != null && this.soundEffect != null)
        {
            audioS.PlayOneShot(soundEffect);
        }

        yield break;
    }
}