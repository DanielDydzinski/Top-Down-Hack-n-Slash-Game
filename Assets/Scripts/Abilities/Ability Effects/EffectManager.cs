using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class EffectManager : MonoBehaviour
{
	private Coroutine slowCo;
	public GameObject slowParticles{ set; get;}

	private Coroutine knockBackCo;
	private float lastKnockBackDuration = 0f;
	private Coroutine kbTimerCo;
	public GameObject knockBackParticles{ set; get;}

    private Coroutine pushCo;

    private Health _health;




    private DamageReceiver _receiver;

    [field:SerializeField]public List<Effect> aEffects {set; get;}

    // This tracks every anonymous / generic coroutine running in the 'else' block
    private List<Coroutine> _activeGenericCoroutines = new List<Coroutine>();

    void Awake()
    {
        _receiver = GetComponent<DamageReceiver>();
        _health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (_receiver != null) _receiver.OnHitReceived += HandleHit;
    }

    void OnDisable()
    {
        if (_receiver != null) _receiver.OnHitReceived -= HandleHit;
    }

    private void HandleHit(HitInfo info)
    {
        // ─── THE PREDICTIVE GATEWAY GUARD ───
        // Check if the enemy is already dead, OR if this hit's damage will reduce health to/below 0
        if (_health != null)
        {
            if (_health != null && _health.GetisDead()) return;
        }

        // EffectManager only cares about the list of effects if the unit survives
        if (info.effects != null && info.effects.Count > 0)
        {
            this.aEffects = new List<Effect>(info.effects); // Copy the list
            ApplyEffects(info);
        }
    }


    public void ApplyEffects(HitInfo info)
	{

        if (_health != null && _health.GetisDead()) return;
        
        

		foreach (Effect e in aEffects) 
		{
			if (e.GetType() == typeof(TakeSlow)) 
			{
				if (slowCo != null) 
				{
					if(slowParticles != null)
					{ 
						Destroy (slowParticles);
					}
					StopCoroutine (slowCo);
				}
				slowCo = StartCoroutine (e.ApplyEffect (this.gameObject,info));
			}
            else if (e is TakeKnockBack tke)
            {
				if (tke.unconsciousDuration > lastKnockBackDuration)
				{
					if (knockBackCo != null)
					{
						if (knockBackParticles != null)
						{
							Destroy(knockBackParticles);
						}
						StopCoroutine(knockBackCo);
					}
					knockBackCo = StartCoroutine(e.ApplyEffect(this.gameObject, info));
					lastKnockBackDuration = tke.unconsciousDuration;
					if (kbTimerCo != null) StopCoroutine(kbTimerCo);
					StartCoroutine(RunLastKnockBackTimer());
				}
               
            }
            else if (e is TakePush)
            {
                if (pushCo != null) StopCoroutine(pushCo);
                pushCo = StartCoroutine(e.ApplyEffect(this.gameObject, info));
            }
            else
			{
                Coroutine genericCo = StartCoroutine (e.ApplyEffect (this.gameObject, info));
                _activeGenericCoroutines.Add(genericCo);
            }

		}

        // We check right now if the enemy died from that loop, and force a cleanup instantly!
        if (_health != null && _health.GetisDead())
        {
            CleanUpAllEffects();
        }
    }

    private IEnumerator RunLastKnockBackTimer()
    {
        while (lastKnockBackDuration > 0)
        {
            lastKnockBackDuration -= Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        lastKnockBackDuration = 0f; // Ensure it stays clean at exactly 0
    }

    public void CleanUpAllEffects()
    {
        // 1. Stop the specific tracked coroutines
        if (slowCo != null) StopCoroutine(slowCo);
        if (knockBackCo != null) StopCoroutine(knockBackCo);
        if (kbTimerCo != null) StopCoroutine(kbTimerCo);

        // 2. Destroy the particles immediately
        if (slowParticles != null) Destroy(slowParticles);
        if (knockBackParticles != null) Destroy(knockBackParticles);

        // 3. Reset the timer logic
        lastKnockBackDuration = 0f;

        // Explicitly loop and terminate all running generic/else effects (DoT, Stun, etc.)
        foreach (Coroutine co in _activeGenericCoroutines)
        {
            if (co != null) StopCoroutine(co);
        }
        _activeGenericCoroutines.Clear();

        // Fetch the component that tracks your designated effect spawn slots
        EffectSpawnPossitions anchors = GetComponent<EffectSpawnPossitions>();
        if (anchors != null)
        {
            ClearAnchorChildren(anchors.head);
            ClearAnchorChildren(anchors.center);
            ClearAnchorChildren(anchors.feet);
        }


        // 4. Stop any "Generic" effects that were just started in the else block
        StopAllCoroutines();
    }


    private void ClearAnchorChildren(Transform anchorTransform)
    {
        if (anchorTransform == null) return;

        // Loop backwards through the transform tree so index shifts don't skip objects
        for (int i = anchorTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = anchorTransform.GetChild(i);

            // Optional Safety: If your particle systems are nested inside prefabs, 
            // stopping emission first prevents visual popping on the frame of deletion.
            ParticleSystem[] psSystems = child.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in psSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            Destroy(child.gameObject);
        }
    }

    //   private void ReplaceEffect<T>(Effect eff, ref GameObject particles, ref Coroutine co) // check if this works and implement this in the code above if it works
    //{
    //	if (eff.GetType () == typeof(T))
    //	{
    //		if (co != null) 
    //		{
    //			if (particles != null)
    //			{
    //				Destroy (particles);
    //			}
    //			StopCoroutine (co);
    //		}
    //		co = StartCoroutine (eff.ApplyEffect (this.gameObject,info));
    //	}
    //}


}

