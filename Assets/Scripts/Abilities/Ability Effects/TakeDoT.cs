using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/DamageOverTime", fileName = "new Effect")]
public class TakeDoT : Effect
{
    public float damageAmount;
    public float damageDuration;
    public float damageRate;

    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        Health hp = target.GetComponent<Health>();
        AudioSource audioS = target.GetComponent<AudioSource>();

        float finalDamage = damageAmount * info.multiplier;

        if (hp == null)
        {
            Debug.Log(target.name + " doesn't have Health Component. Can't apply DoT Effect.");
            yield break;
        }

        // --- NEW: Cache the block status of the initial impact ---
        bool wasInitialHitBlocked =  info.isBlocked;
        float elapsed = 0f;

        while (elapsed < damageDuration)
        {
            info.damage = finalDamage;
            info.attackType = AttackType.DoT;
            info.impactPoint = Vector3.zero;

            // --- NEW: Feed the initial block state back into the tick data ---
            info.isBlocked = wasInitialHitBlocked;

            // Process the damage tick safely
            hp.Damage(finalDamage, info);

            if (effectParticles != null)
            {
                var anchors = target.GetComponent<EffectSpawnPossitions>();
                if (anchors != null)
                {
                    Instantiate(effectParticles, anchors.center.position, Quaternion.identity, anchors.center);
                }
            }

            if (audioS != null && this.soundEffect != null)
            {
                audioS.PlayOneShot(soundEffect);
            }

            yield return new WaitForSeconds(damageRate);
            elapsed += damageRate;
        }
    }
}