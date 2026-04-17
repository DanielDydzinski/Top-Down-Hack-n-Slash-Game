using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Effects/Slow", fileName = "new Effect")]
public class TakeSlow : Effect {

	public float slowAmount;
	public float slowDuration;
	public bool fadedSlow;

	public override IEnumerator ApplyEffect(GameObject target)
	{
		Stats stats = target.GetComponent<Stats> ();
		float maxSpeed = stats.GetMoveSpeed ();
		float slowedSpeed = maxSpeed;
		GameObject particlesObj;
		bool isPlayer = false;

		NavMeshAgent nva = null;
		Mover mover = null;

		if (target.tag == "Enemy")
		{
		 	nva = target.GetComponent<NavMeshAgent> ();
			slowedSpeed = maxSpeed * slowAmount;
			nva.speed = slowedSpeed;
		}
		else if(target.tag == "Player")
		{
			isPlayer = true;
			mover = target.GetComponent<Mover> ();
			slowedSpeed = maxSpeed * slowAmount;
			mover.SetSpeed (slowedSpeed);
		}

		if (effectParticles != null) 
		{
			if(target.tag == "Enemy" || target.tag == "Player")
			{
				EffectManager em = target.GetComponent<EffectManager> ();

				particlesObj = Instantiate (effectParticles, target.transform.GetChild(5).transform); // 4th child set up to centre. 3rd to over head. 5th to feet.
				Destroy(particlesObj, slowDuration);
				em.slowParticles = particlesObj;
			}
		}

		Stopwatch stopwatch = new Stopwatch ();
		stopwatch.Start ();

		while (stopwatch.Elapsed.TotalSeconds <= slowDuration)
		{
			yield return null;

			if (fadedSlow)
			{
				float newSpeed = Mathf.Lerp (slowedSpeed, maxSpeed, (float)stopwatch.Elapsed.TotalSeconds / slowDuration);

				if (isPlayer)
				{
					mover.SetSpeed (newSpeed);
				} 
				else 
				{
					nva.speed = newSpeed;
				}

			}
		}

		if (target.tag == "Enemy")
		{
			nva.speed = maxSpeed;
		}
		else if(target.tag == "Player")
		{
			mover.SetSpeed (maxSpeed);
		}

		stopwatch.Stop ();
		stopwatch.Reset ();
	}

	public void DestroyParticles()
	{
		 
	}
}
