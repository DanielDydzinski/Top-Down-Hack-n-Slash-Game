using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FireBallBehaviour : MonoBehaviour {

	public GameObject projectile;
	public Ability explosionAbility;

	public string targetTag;
	public string friendTag;		

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

		GameObject proj = Instantiate (projectile, this.gameObject.transform);
	}
	
	// Update is called once per frame
	void Update () {

		transform.Translate (Vector3.forward * projectileSpeed * Time.deltaTime);
		DestroyByDistance ();
		
	}

	public void UpdateValues(List<Effect> aeffects ,string atag, string afriendTag, float aprojSpeed, 
		float aprojSize,float aprojRange, GameObject aproj, Ability aexplo)
	{
		effects = aeffects;
		projectileSpeed = aprojSpeed;
		projectileSize = aprojSize;
		projectileRange = aprojRange;
		projectile = aproj; 
		explosionAbility = aexplo; 
		targetTag = atag; 
		friendTag = afriendTag;
	}

	private void DestroyByDistance()
	{
		if (Vector3.Distance (spawnPos, transform.position) > projectileRange) {

			Destroy (this.gameObject);
		}
	}

	void OnTriggerEnter(Collider col)
	{
		if (howManyExplosions < 1)
		{
			if (col.gameObject.tag == targetTag) 
			{
				if (col.gameObject.GetComponent<EffectManager> ())
				{
					EffectManager em = col.gameObject.GetComponent<EffectManager> ();
					em.aEffects = effects;
					em.ApplyEffects ();
				} 
				else
				{
					Debug.Log (col.gameObject.name + " don't have EffectManager. Can't apply effects.");
				}
			}
			if (col.gameObject.tag != friendTag) // will this work?
			{
				if (explosionAbility != null)
				{
					GameObject expGO = explosionAbility.Cast (transform.position, Quaternion.identity);
				}
				howManyExplosions++;
				Destroy (this.gameObject);
			}
		}
	}
}
