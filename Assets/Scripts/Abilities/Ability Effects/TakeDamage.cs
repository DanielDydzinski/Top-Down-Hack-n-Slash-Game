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
            // 1. Commit the structural damage to health points (Health.Damage applies any
            //    block mitigation internally, so the raw finalDamage here may not be what
            //    actually lands)
            hp.Damage(finalDamage, info);

            // 2. Read back the real outcome instead of predicting it from unmitigated damage —
            //    a blocked hit can mean this never actually kills even though finalDamage alone
            //    would have looked lethal
            bool isKillingBlow = hp.GetisDead();

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
                if (ObjectPooler.Instance != null)
                {
                    ObjectPooler.Instance.SpawnFromPool(effectParticles, effectSpawnLocations.center.position, effectSpawnLocations.center.rotation, effectSpawnLocations.center);
                }
                else
                {
                    Instantiate(effectParticles, effectSpawnLocations.center);
                }
            }
        }

        if (audioS != null && this.soundEffect != null)
        {
            audioS.PlayOneShot(soundEffect);
        }

        yield break;
    }
}