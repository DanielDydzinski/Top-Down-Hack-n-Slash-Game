using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttackBehavaiour : MonoBehaviour {


	public GameObject attackParticles;

	public string targetTag;
	public string friendTag;
	public float dmg;
	public float length;
	public Vector3 halfExtents;	
	public LayerMask layerMask;

	public List<Effect> effects = new List<Effect>();
	RaycastHit rayHit ;


	// Use this for initialization
	void Start () {

		CastHitBox ();
	}
	
	public void UpdateValues(string atag,string afriendTag, List<Effect> aeffect, float alength,  Vector3 ahalfExtents, GameObject aparticles, LayerMask alayer)
	{
		targetTag = atag;
		friendTag = afriendTag;
		effects = aeffect;
		length = alength;
		halfExtents	 = ahalfExtents;
		attackParticles = aparticles;
		layerMask = alayer;
	}


	private void CastHitBox()
	{
		bool hit = Physics.BoxCast (transform.position,halfExtents , transform.forward, out rayHit, transform.rotation,length,layerMask);
		if (hit) 
		{   
			bool wallHit = Physics.Raycast (transform.position, transform.forward, rayHit.distance, 1024); // 1024 hardcoded wall layer 1024
			if (!wallHit) //dont do anything if wall is in the way 
			{
				if (rayHit.collider.gameObject.tag == targetTag) //checked for layer and now wer checking again for tag? lol ... xD safety :p ..dumb
				{
					if (attackParticles != null) {
						Instantiate (attackParticles, rayHit.point, transform.rotation);
					}
					if (rayHit.collider.gameObject.GetComponent<EffectManager> ()) 
					{
						EffectManager em = rayHit.collider.gameObject.GetComponent<EffectManager> ();

						foreach (Effect e in effects) //
						{
							if(e.GetType() == typeof(TakeKnockBack)) // check if this works... !!!!!!!!!!!
							{
								TakeKnockBack knockback = (TakeKnockBack)e;
								knockback.SetExplosionPos (transform.position);
								//e = knockback;
							}
						}

						em.aEffects = effects;
						em.ApplyEffects ();
					} 
					else 
					{
						Debug.Log(rayHit.collider.gameObject.name + " don't have EffectManager. Can't apply effects.");
					}
				}
			}
		}

		Destroy (this.gameObject);
	}

}
