using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class AOE : MonoBehaviour {

//	float dmg = 5f;
//	float duration = 6.0f;
//	float rate = 0.4f;
//
//	List<Effect> effects = new List<Effect> ();
//	Stopwatch timer;
//	Stopwatch tickTImer;
//
//	SphereCollider sc;
//
//	// Use this for initialization
//	void Start () {
//		
//		effects.Add(new TakeDamage(dmg));
//
//		sc = GetComponent<SphereCollider> ();
//		timer = new Stopwatch ();
//		tickTImer = new Stopwatch ();
//		timer.Start ();
//		tickTImer.Start ();
//	}
//
//	void Update()
//	{
//		ScanForItems (sc);
//	}
//
//
//	void ScanForItems(SphereCollider Item)
//	{
//		Vector3 center = Item.transform.position + Item.center;
//		float radius = Item.radius;
//
//		if (tickTImer.Elapsed.TotalSeconds >= rate) {
//			Collider[] allOverlappingColliders = Physics.OverlapSphere (center, radius);
//
//			foreach (Collider c in allOverlappingColliders) {
//				if (c.gameObject.tag == "Enemy") {
//					DealAOEdmg (c);
//				}
//			}
//
//			tickTImer.Stop ();
//			tickTImer.Reset ();
//			tickTImer.Start ();
//		}
//
//		if (timer.Elapsed.TotalSeconds >= duration) {
//			Destroy (this.gameObject);
//		}
//	}
//
//	private void DealAOEdmg(Collider col)
//	{
//
//		EffectManager em = col.gameObject.GetComponent<EffectManager> ();
//
//		em.aEffects = effects;
//		em.ApplyEffects ();
//
//
//	}
}
