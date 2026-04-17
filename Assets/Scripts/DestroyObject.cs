using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObject : MonoBehaviour {

	[SerializeField]
	private bool byTime;
	[SerializeField] private float destroyTime;
	private float timer;

	[SerializeField]
	private bool byDistance;
	[SerializeField] private float destroyDistance;
	private Vector3 startPosition;

	// Use this for initialization
	void Start () 
	{
		timer = 0f;
		startPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (byTime)
		{
			timer += Time.deltaTime;
			if (timer >= destroyTime)
			{
				Destroy (this.gameObject);
			}
		}

		if (byDistance)
		{
			float distance = Vector3.Distance (startPosition, transform.position);
			if (distance >= destroyDistance) 
			{
				Destroy (this.gameObject);
			}

		}
	}
}
