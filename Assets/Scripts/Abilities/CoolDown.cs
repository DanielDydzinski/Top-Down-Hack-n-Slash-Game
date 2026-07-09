using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolDown  {

	public Ability ability { set; get;}
	public float timeLeft{ set; get;}
	public bool coolDownReady{ set; get;}

	public CoolDown(Ability ab)
	{
		ability = ab;
		timeLeft = 0f;
		coolDownReady = true;
	}

	public GameObject TriggerAbility(Vector3 pos, Quaternion rot, GameObject aCaster)
	{
		return ability.Cast (pos, rot, aCaster);
	}

}
