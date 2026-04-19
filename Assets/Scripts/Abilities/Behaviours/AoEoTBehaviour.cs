using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class AoEoTBehaviour : MonoBehaviour
{
    public GameObject particles;
    public float duration;
    public float rate;
    public float radius;
    public Faction myFaction;
    public DamageType damageType;
    public List<Effect> effects = new List<Effect>();

    private float lifeTimer = 0f;
    private float tickTimer = 0f;

    void Start()
    {
        
        if (particles != null) Instantiate(particles, transform);
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (tickTimer >= rate)
        {
            tickTimer = 0f;
            Tick();
        }

        if (lifeTimer >= duration) Destroy(gameObject);
    }

    private void Tick()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider c in targets)
        {
            EntityIdentity identity = c.GetComponent<EntityIdentity>();
            if (identity != null && identity.faction == myFaction) continue;

            IDamageable damageable = c.GetComponent<IDamageable>();
            if (damageable != null)
            {
                HitInfo info = new HitInfo
                {
                    type = damageType,
                    effects = this.effects,
                    attacker = this.gameObject
                };
                damageable.TakeDamage(info);
            }
        }
    }

    public void UpdateValues(List<Effect> aeffects, float aduration, float arate, float aradius, Faction faction, GameObject aparticles, DamageType dmgType)
    {
        effects = aeffects;
        duration = aduration;
        rate = arate;
        radius = aradius;
        myFaction = faction;
        particles = aparticles;
        damageType = dmgType;
    }
}

//public class AoEoTBehaviour : MonoBehaviour {

//	public GameObject particles;

//	public float duration;
//	public float rate;
//	public float radius;
//	public string targetTag;

//	public List<Effect> effects = new List<Effect> ();

//	Stopwatch timer;
//	Stopwatch tickTImer;
//	SphereCollider sc;

//	// Use this for initialization
//	void Start () 
//	{

//		sc = GetComponent<SphereCollider> ();
//		sc.radius = radius;
//		timer = new Stopwatch ();
//		tickTImer = new Stopwatch ();
//		timer.Start ();
//		tickTImer.Start ();

//		if (particles != null) {
//			Instantiate (particles, this.gameObject.transform);
//		}
//	}

//	// Update is called once per frame
//	void Update ()
//	{
//		ScanForTargets (sc);
//	}

//	public void UpdateValues(List<Effect> aeffects, float aduration, float arate,float aradius,string atargetTag, GameObject aparticles)
//	{
//		effects = aeffects;
//		duration = aduration;
//		rate = arate;
//		radius = aradius;
//		targetTag = atargetTag;
//		particles = aparticles;
//	}

//	void ScanForTargets(SphereCollider sphereCol)
//	{
//		Vector3 center = sphereCol.transform.position + sphereCol.center;
//		float radius = sphereCol.radius;

//		if (tickTImer.Elapsed.TotalSeconds >= rate)
//		{
//			Collider[] allOverlappingColliders = Physics.OverlapSphere (center, radius);

//			foreach (Collider c in allOverlappingColliders) 
//			{
//				if (c.gameObject.tag == targetTag) 
//				{
//					ApplyAllEffects (c);
//				}
//			}

//			tickTImer.Stop ();
//			tickTImer.Reset ();
//			tickTImer.Start ();
//		}

//		if (timer.Elapsed.TotalSeconds >= duration) 
//		{
//			Destroy (this.gameObject);
//		}
//	}

//	private void ApplyAllEffects(Collider col)
//	{

//		EffectManager em = col.gameObject.GetComponent<EffectManager> ();

//		em.aEffects = effects;
//		em.ApplyEffects ();


//	}
//}
