using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAnyAxis : MonoBehaviour {

	[SerializeField]
	private float speed;

	[SerializeField]
	private bool x;
	[SerializeField]
	private bool y;
	[SerializeField]
	private bool z;


	
	// Update is called once per frame
	void Update () 
	{
		if (x)
		{
			transform.Rotate (Vector3.right * Time.deltaTime * speed);
		}
		if (y) 
		{
			transform.Rotate (Vector3.up * Time.deltaTime * speed);
		}
		if (z)
		{
			transform.Rotate (Vector3.forward * Time.deltaTime * speed);
		}
		
	}
}
