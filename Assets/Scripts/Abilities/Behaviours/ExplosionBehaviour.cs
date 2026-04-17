using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour {

	public GameObject explosionParticles;

	public string targetTag;
	public string friendTag;

	public float explosionDamage;
	public float explosionRadius;
	public bool damageByDistance;

	public List<Effect> effects = new List<Effect>();
	TakeKnockBack knockBackEff = null;
	TakeDamage takeDamageEff = null;

	// Use this for initialization
	void Start () {

		bool doesDmg = false;
		//look for knockback in effects
		foreach (Effect e in effects) //
		{
			if(e.GetType() == typeof(TakeKnockBack)) // check if this works... !!!!!!!!!!!
			{
				knockBackEff = (TakeKnockBack)e;
				explosionRadius = knockBackEff.radius;
				knockBackEff.SetExplosionPos (transform.position);
			}
			if (e.GetType () == typeof(TakeDamage))
			{
				takeDamageEff = (TakeDamage)e;
				explosionDamage = takeDamageEff.damageAmount;
				doesDmg = true;
			}
		}
		if (!doesDmg && damageByDistance)
		{
			Debug.Log(this.gameObject.name + "No TakeDamage effect found. Disabling DamageByDistance.");
			damageByDistance = false;
		}

		if (explosionParticles != null) 
		{
			Instantiate (explosionParticles, transform.position, Quaternion.identity);
		}

		ExplosionDamage (transform.position,explosionRadius);

		
	}

	public void UpdateValues(string atag,string afriendTag, List<Effect> aeffects, GameObject aexploPrefab, bool dmgBydist, float aRadius)
	{
		targetTag = atag;
		friendTag = afriendTag;
		effects = aeffects;
		explosionParticles = aexploPrefab;
		damageByDistance = dmgBydist;
		explosionRadius = aRadius;
	}

	private void ExplosionDamage(Vector3 centre, float radius)
	{
		Collider[] hitColliders = Physics.OverlapSphere (centre, radius);
		foreach (Collider c in hitColliders)
		{
			float distance = Vector3.Distance (centre, c.transform.position);
			Vector3 dir = c.transform.position - centre ; 
			dir.Normalize();
			bool wallHit = Physics.Raycast (transform.position, dir,distance , 1024); // 1024 hardcoded wall layer
			{
				if (!wallHit)
				if (c.gameObject.tag == targetTag) 
				{
					EffectManager em = null;
					if (c.gameObject.GetComponent<EffectManager>())
					{
						em = c.gameObject.GetComponent<EffectManager> ();
					} 
					else
					{
						Debug.Log(c.gameObject.name +" doesn't have Effect manager. Can't apply Effects.");
						return;
					}
					if (damageByDistance)
					{
						DamageByDistance (c.transform.position, centre, radius);
					}
						

					em.aEffects = effects;
					em.ApplyEffects ();

					ResetTakeDmgdmg ();
				}
			}
		}

		Destroy (this.gameObject);
	}

	private void DamageByDistance(Vector3 targetPos, Vector3 centre,float radius)
	{
		float dmg = takeDamageEff.damageAmount;
		targetPos.y = 0f;
		float distance = Vector3.Distance (targetPos, centre);
		distance -= 1.0f;
		float scale = (distance) / radius;
		float dmgNew = dmg * (1.0f - scale);
		takeDamageEff.damageAmount = dmgNew;


	}
	private void ResetTakeDmgdmg()
	{			
		if (takeDamageEff != null)
		{
			takeDamageEff.damageAmount = explosionDamage;
		}
	}
}
