using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FireBallBehaviour : MonoBehaviour {

	public GameObject projectile;
	public Ability explosionAbility;

	public string targetTag;
	public string friendTag;
    public Faction myFaction;
    public DamageType damageType;

    public float projectileSpeed; 	
	public float projectileSize;	
	public float projectileRange;


	public SphereCollider sc;

	public List<Effect> effects = new List<Effect>();
	Vector3 spawnPos;
	private int howManyExplosions; //prevent multiple instances of explosion

	// Use this for initialization
	void Start () {

		howManyExplosions = 0;
		spawnPos = this.transform.position;

		sc = GetComponent<SphereCollider> ();
		sc.radius = projectileSize;
		sc.isTrigger = true;

        //GameObject proj = Instantiate (projectile, this.gameObject.transform);
        StartCoroutine(DelayedInstanciate());
    }
	
	// Update is called once per frame
	void Update () {

		transform.Translate (Vector3.forward * projectileSpeed * Time.deltaTime);
		DestroyByDistance ();
		
	}

	public void UpdateValues(List<Effect> aeffects , float aprojSpeed, 
		float aprojSize,float aprojRange, GameObject aproj, Ability aexplo, Faction faction,DamageType dmgType)
	{
		effects = aeffects;
		projectileSpeed = aprojSpeed;
		projectileSize = aprojSize;
		projectileRange = aprojRange;
		projectile = aproj; 
		explosionAbility = aexplo; 
		myFaction = faction;
        damageType = dmgType;

    }

    private IEnumerator DelayedInstanciate()
    {
        yield return null; // Wait exactly one frame
        if (projectile != null)
        {
            Instantiate(projectile, this.gameObject.transform);
        }
    }

    private void DestroyByDistance()
	{
		if (Vector3.Distance (spawnPos, transform.position) > projectileRange) {

			Destroy (this.gameObject);
		}
	}

	void OnTriggerEnter(Collider col)
	{   //make sure we only do one 
        if (howManyExplosions < 1)
        {

            // 1. Check for Identity (The "Who are you?" check)
            EntityIdentity victimIdentity = col.GetComponent<EntityIdentity>();

            // If they have an identity and it's the same as ours, ignore (Friend)
            if (victimIdentity != null && victimIdentity.faction == myFaction)
            {
                return;
            }

            // 2. Try to hit the Mail Slot (The "Can I hurt you?" check)
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Create the HitInfo envelope
                HitInfo info = new HitInfo
                {
                    faction = myFaction,
                    multiplier = 1.0f,
                    type = damageType,
                    effects = this.effects,
                    attacker = this.gameObject
                    
                };

                // Deliver the envelope!
                damageable.TakeDamage(info);
            }

            if (explosionAbility != null)
            {
                GameObject expGO = explosionAbility.Cast(transform.position, Quaternion.identity);
            }
            howManyExplosions++;
            Destroy(this.gameObject);
        }
        
        //if (howManyExplosions < 1)
        //{
        //	if (col.gameObject.tag == targetTag) 
        //	{
        //		if (col.gameObject.GetComponent<EffectManager> ())
        //		{
        //			EffectManager em = col.gameObject.GetComponent<EffectManager> ();
        //			em.aEffects = effects;
        //			em.ApplyEffects ();
        //		} 
        //		else
        //		{
        //			Debug.Log (col.gameObject.name + " don't have EffectManager. Can't apply effects.");
        //		}
        //	}
        //	if (col.gameObject.tag != friendTag) // will this work?
        //	{
        //		if (explosionAbility != null)
        //		{
        //			GameObject expGO = explosionAbility.Cast (transform.position, Quaternion.identity);
        //		}
        //		howManyExplosions++;
        //		Destroy (this.gameObject);
        //	}
        //}
    }
}
