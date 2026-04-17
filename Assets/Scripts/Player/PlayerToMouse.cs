using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerToMouse : MonoBehaviour {

	private float camRayLength;  
	public Vector3 playerToMouseDir { private set;  get; }
	public Vector3 mouseInWorldPos { private set;  get; }
	[SerializeField] LayerMask maskFloorLayer;

	// Use this for initialization
	void Start () {

		camRayLength = 100f;
	}
	
	// Update is called once per frame
	void Update () {

		CalcPlayerToMouseDir ();
	}

	private void CalcPlayerToMouseDir()
	{

		Ray camRay = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit floorHit;

		if (Physics.Raycast (camRay, out floorHit, camRayLength, maskFloorLayer)) {
			// Create a vector from the player to the point on the floor the raycast from the mouse hit.
			mouseInWorldPos = floorHit.point;
			Vector3 dir = mouseInWorldPos - transform.position;

			// Ensure the vector is entirely along the floor plane.
			dir.y = 0f;
			playerToMouseDir = dir;
		}
	}
}
