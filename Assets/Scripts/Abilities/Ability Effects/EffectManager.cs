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




    private DamageReceiver _receiver;

    [field:SerializeField]public List<Effect> aEffects {set; get;}

    void Awake()
    {
        _receiver = GetComponent<DamageReceiver>();
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
        // EffectManager only cares about the list of effects
        if (info.effects != null && info.effects.Count > 0)
        {
            this.aEffects = new List<Effect>(info.effects); // Copy the list
            ApplyEffects(info);
        }
    }


    public void ApplyEffects(HitInfo info)
	{
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
				StartCoroutine (e.ApplyEffect (this.gameObject, info));
			}

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

        // 4. Stop any "Generic" effects that were just started in the else block
        StopAllCoroutines();
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

