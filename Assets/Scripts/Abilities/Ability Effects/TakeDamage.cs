using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Damage", fileName = "new Effect")]
public class TakeDamage : Effect {

	public float damageAmount;

    public override IEnumerator ApplyEffect(GameObject target)
    {
        Health hp = target.GetComponent<Health>();
        if (hp != null)
        {
            hp.Damage(damageAmount);
            // Note: You can now pass info.attacker to Health if you want to track who did the damage!
        }
        if (effectParticles != null)
        {
            var effectSpawnLocations = target.GetComponent<EffectSpawnPossitions>();
            if (effectSpawnLocations!=null)
            {
                Instantiate(effectParticles, effectSpawnLocations.center); // 4th child set up to centre. 3rd to over head. 5th to feet.
            }
        }

            yield break;
    }

    //public override IEnumerator ApplyEffect(GameObject target)
    //{
    //	if (!target.GetComponent<Health> ()) 
    //	{
    //		UnityEngine.Debug.Log (target.name + " doesn't have Health Component. Can't apply Damage Effect.");
    //		yield break;
    //	}
    //	else
    //	{
    //		Health hp = target.GetComponent<Health> ();
    //		hp.Damage (damageAmount);

    //		if (effectParticles != null) 
    //		{
    //			if(target.tag == "Enemy" || target.tag == "Player")
    //			{
    //				Instantiate (effectParticles, target.transform.GetChild(4).transform); // 4th child set up to centre. 3rd to over head. 5th to feet.
    //			}
    //		}

    //		yield break;
    //	}
    //}
}
