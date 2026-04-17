using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Mover : MonoBehaviour {
	
	[SerializeField]
	private float maxSpeed;
	[SerializeField]
	private float minSpeed;
	[SerializeField]
	private float speed;

	public bool movingBottomLeft{ get; set;}
	public bool movingBottomRight{ get; set;}
	public bool movingTopRight{ get; set;}
	public bool movingTopLeft{ get; set;}

	private Vector3 direction;

	// Use this for initialization
	void Start () {

		movingBottomLeft = false;
		movingBottomRight = false;
		movingTopLeft = false;
		movingTopRight = false;
		direction = Vector3.zero;
		
	}

	//make sure to update direction first
	public void Move()
	{
		transform.Translate (direction * speed * Time.deltaTime,Space.World);
	}
	public void SetDirection(Vector3 dir)
	{
		dir = dir.normalized;
		direction = dir;
	}

	public void SetMaxSpeed(float mspeed)
	{
		maxSpeed = mspeed;
	}
	public float GetMaxSpeed()
	{
		return maxSpeed;
	}


	public void SetSpeed(float value)
	{
		speed = Mathf.Clamp (value, minSpeed, maxSpeed);
	}
	public float GetSpeed()
	{
		return speed;
	}

	public Vector3 GetDirection()
	{
		return direction;
	}
		


}
