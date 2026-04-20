using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour
{
    public GameObject explosionParticles;
    public float explosionRadius;
    public bool damageByDistance;
    public Faction myFaction;
    public DamageType damageType;
    public LayerMask wallLayer; // No more hardcoded 1024

    public List<Effect> effects = new List<Effect>();

    void Start()
    {
        //waity one frame for values to update
        StartCoroutine(DelayedExplode());
        
    }

    public void UpdateValues(List<Effect> aeffects, GameObject aexploPrefab, float aRadius, Faction faction, DamageType dmgType, bool dmgByDist)
    {
        effects = aeffects;
        explosionParticles = aexploPrefab;
        explosionRadius = aRadius;
        myFaction = faction;
        damageType = dmgType;
        damageByDistance = dmgByDist;

        //Explode();
    }

    private IEnumerator DelayedExplode()
    {
        if (explosionParticles != null)
        {
            Instantiate(explosionParticles, transform.position, Quaternion.identity);
        }

        yield return null; // Wait exactly one frame
        Explode();
    }

    private void Explode()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider c in hitColliders)
        {
            // 1. Faction Check
            EntityIdentity victimIdentity = c.GetComponent<EntityIdentity>();
            if (victimIdentity != null && victimIdentity.faction == myFaction) continue;

            // 2. Wall Check
            float dist = Vector3.Distance(transform.position, c.transform.position);
            Vector3 dir = (c.transform.position - transform.position).normalized;
            if (Physics.Raycast(transform.position, dir, dist, wallLayer)) continue;

            // 3. Mailbox Check
            IDamageable damageable = c.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate falloff (Scale is 1.0 at center, 0.0 at edge)
                float falloff = 1f;
                if (damageByDistance)
                {
                    // We clamp it between 0 and 1 just to be safe
                    falloff = Mathf.Clamp01(1.0f - (dist / explosionRadius));
                }

                // Handle Knockback position before sending
                foreach (Effect e in effects)
                {
                    if (e is TakeKnockBack kb) kb.SetExplosionPos(transform.position);
                }

                // Create the package
                HitInfo info = new HitInfo
                {
                    faction = myFaction,
                    effects = new List<Effect>(effects), // Pass the list
                    type = damageType,
                    attacker = this.gameObject,
                    multiplier = falloff
                };

                // Logic for falloff: We don't modify the SO, we just tell the receiver!
                // If you want scaling, you'd handle it inside the specific Effect. 
                // For now, we deliver the package as-is.
                damageable.TakeDamage(info);
            }
        }
        Destroy(gameObject);
    }
    //	private void DamageByDistance(Vector3 targetPos, Vector3 centre,float radius)
    //	{
    //		float dmg = takeDamageEff.damageAmount;
    //		targetPos.y = 0f;
    //		float distance = Vector3.Distance (targetPos, centre);
    //		distance -= 1.0f;
    //		float scale = (distance) / radius;
    //		float dmgNew = dmg * (1.0f - scale);
    //		takeDamageEff.damageAmount = dmgNew;


    //	}

}


//public class ExplosionBehaviour : MonoBehaviour {

//	public GameObject explosionParticles;

//	public string targetTag;
//	public string friendTag;

//	public float explosionDamage;
//	public float explosionRadius;
//	public bool damageByDistance;
//    public Faction myFaction;
//    public DamageType damageType;

//    public List<Effect> effects = new List<Effect>();
//	TakeKnockBack knockBackEff = null;
//	TakeDamage takeDamageEff = null;

//	// Use this for initialization
//	void Start () {

//		bool doesDmg = false;
//		//look for knockback in effects
//		foreach (Effect e in effects) //
//		{
//			if(e.GetType() == typeof(TakeKnockBack)) // check if this works... !!!!!!!!!!!
//			{
//				knockBackEff = (TakeKnockBack)e;
//				explosionRadius = knockBackEff.radius;
//				knockBackEff.SetExplosionPos (transform.position);
//			}
//			if (e.GetType () == typeof(TakeDamage))
//			{
//				takeDamageEff = (TakeDamage)e;
//				explosionDamage = takeDamageEff.damageAmount;
//				doesDmg = true;
//			}
//		}
//		if (!doesDmg && damageByDistance)
//		{
//			Debug.Log(this.gameObject.name + "No TakeDamage effect found. Disabling DamageByDistance.");
//			damageByDistance = false;
//		}

//		if (explosionParticles != null) 
//		{
//			Instantiate (explosionParticles, transform.position, Quaternion.identity);
//		}

//		ExplosionDamage (transform.position,explosionRadius);


//	}

//	public void UpdateValues(string atag,string afriendTag, List<Effect> aeffects, GameObject aexploPrefab, bool dmgBydist, float aRadius, Faction faction, DamageType dmgType)
//	{
//		targetTag = atag;
//		friendTag = afriendTag;
//		effects = aeffects;
//		explosionParticles = aexploPrefab;
//		damageByDistance = dmgBydist;
//		explosionRadius = aRadius;
//		myFaction = faction;
//		damageType = dmgType;
//	}

//	private void ExplosionDamage(Vector3 centre, float radius)
//	{
//		Collider[] hitColliders = Physics.OverlapSphere (centre, radius);
//		foreach (Collider c in hitColliders)
//		{
//			float distance = Vector3.Distance (centre, c.transform.position);
//			Vector3 dir = c.transform.position - centre ; 
//			dir.Normalize();
//			bool wallHit = Physics.Raycast (transform.position, dir,distance , 1024); // 1024 hardcoded wall layer
//			{
//				if (!wallHit)
//				if (c.gameObject.tag == targetTag) 
//				{
//					EffectManager em = null;
//					if (c.gameObject.GetComponent<EffectManager>())
//					{
//						em = c.gameObject.GetComponent<EffectManager> ();
//					} 
//					else
//					{
//						Debug.Log(c.gameObject.name +" doesn't have Effect manager. Can't apply Effects.");
//						return;
//					}
//					if (damageByDistance)
//					{
//						DamageByDistance (c.transform.position, centre, radius);
//					}


//					em.aEffects = effects;
//					em.ApplyEffects ();

//					ResetTakeDmgdmg ();
//				}
//			}
//		}

//		Destroy (this.gameObject);
//	}

//	private void DamageByDistance(Vector3 targetPos, Vector3 centre,float radius)
//	{
//		float dmg = takeDamageEff.damageAmount;
//		targetPos.y = 0f;
//		float distance = Vector3.Distance (targetPos, centre);
//		distance -= 1.0f;
//		float scale = (distance) / radius;
//		float dmgNew = dmg * (1.0f - scale);
//		takeDamageEff.damageAmount = dmgNew;


//	}
//	private void ResetTakeDmgdmg()
//	{			
//		if (takeDamageEff != null)
//		{
//			takeDamageEff.damageAmount = explosionDamage;
//		}
//	}
//}
