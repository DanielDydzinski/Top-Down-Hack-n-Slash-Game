using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class AbilityTest : MonoBehaviour {
	
	[SerializeField]
	GameObject ball;
	[SerializeField]
	GameObject ballAOE;
	PlayerToMouse ptm;

	// Use this for initialization
	void Start () {
		ptm = GetComponent<PlayerToMouse> ();
		
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetMouseButtonDown (0)) {

			Instantiate (ball, transform.position + Vector3.up , Quaternion.LookRotation(ptm.playerToMouseDir));

		}
		if (Input.GetMouseButtonDown (1)) {
			Instantiate (ballAOE, ptm.mouseInWorldPos,Quaternion.identity);
		}
		
	}
}
