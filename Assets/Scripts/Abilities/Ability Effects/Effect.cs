using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.AI;

public abstract class Effect : ScriptableObject 
{
	public GameObject effectParticles;
	public abstract IEnumerator ApplyEffect (GameObject target);
}
	

