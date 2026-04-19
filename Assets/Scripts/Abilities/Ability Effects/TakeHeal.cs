using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Heal", fileName = "new Effect")]
public class TakeHeal : Effect {

	public float healAmount;

    public override IEnumerator ApplyEffect(GameObject target)
    {
        Health hp = target.GetComponent<Health>();
        if (hp != null)
        {
            hp.Heal(healAmount);
          
        }
        else
        {
            Debug.Log(target.name + " doesn't have Health Component. Can't apply Heal Effect.");
        }

        if (effectParticles != null)
        {
            var effectSpawnLocations = target.GetComponent<EffectSpawnPossitions>();
            if (effectSpawnLocations != null)
            {
                Instantiate(effectParticles, effectSpawnLocations.center); // 4th child set up to centre. 3rd to over head. 5th to feet.
            }
        }

        yield break;
    }
	
}
