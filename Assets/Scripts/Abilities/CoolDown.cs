using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class CoolDown  {

	public Ability ability { set; get;}
	public Stopwatch stopwatch{ set; get;}
	public float timeLeft{ set; get;}
	public bool coolDownReady{ set; get;}

	public CoolDown(Ability ab)
	{
		ability = ab;
		stopwatch = new Stopwatch();
		timeLeft = 0f;
		coolDownReady = true;
	}

	public GameObject TriggerAbility(Vector3 pos, Quaternion rot)
	{
		return ability.Cast (pos, rot);
	}

}
