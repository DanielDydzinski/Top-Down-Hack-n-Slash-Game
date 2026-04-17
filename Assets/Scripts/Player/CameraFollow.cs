using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraFollow : MonoBehaviour {


	[SerializeField]
	private Transform target;						//target to follow
	[SerializeField]
	private float height;							//height of the camera
	[SerializeField]
	private float heightMin;						//minimum height camera can go
	[SerializeField]
	private float heightMax;						//maximum height the camera Camera can go
	[SerializeField]
	private float angle;							//the angle of the camera
	[SerializeField]
	private float offset;							//extra distance from player to camera in z axis
	[SerializeField]
	private float offsetMin;						//minimum distance of offset
	[SerializeField]
	private float offsetMax;						// maximum distance of offset
	[SerializeField]
	private float damping;							// how fast camera travels to its destination

	[SerializeField] float playerToscreenEdgeLimit;		//how close to the screen the player can be
	[SerializeField] LayerMask maskFloorLayer;			// mask 

	[SerializeField]private float CameraBounderXmin; ////This is the left edge of the game world.   
	[SerializeField]private float cameraBounderXMax; //// This is the right edge of the  game world. 
	[SerializeField]private float CamerabounderZMin; //// This is the bottom edge of the game world.
	[SerializeField]private float CamerabounderZMax; //// This is the top edge of the game world.

	private Vector3 centre;							//centre between the mouse and ttarget, is where the camera will travel to
	private PlayerToMouse playerToMouse;			// holds cursor info

	 enum EdgeState {Top,Bottom,Right,Left,Neither};//edges of the screen
	 EdgeState atEdgeVertical = EdgeState.Neither; // current state of vertical screen edges
	EdgeState atEdgeHorizontal = EdgeState.Neither;// current state of horizontal screen edges



	// Use this for initialization
	void Start()
	{
		Quaternion rot = Quaternion.Euler (new Vector3 (angle, 0f, 0f)); // store te angle we want the camera to be in a quaternion
		transform.rotation = rot; //apply the rotation
		playerToMouse = target.GetComponent<PlayerToMouse> (); //get componenet from the target which stores cursor info 

	}
	// Update is called once per frame
	void LateUpdate () {

		ScrollHeight (heightMin,heightMax); 
		CamFollow ();
	}

	private void CamFollow()
	{
		Vector3 cursorWorldPos = playerToMouse.mouseInWorldPos;	
		centre = new Vector3 ((target.position.x + cursorWorldPos.x) / 2.0f, 0f, (cursorWorldPos.z + target.position.z) / 2.0f); // calculate centre between player and mouse

		Vector3 currentPos = Vector3.Lerp (transform.position, centre + new Vector3 (0f, height, -offset), Time.deltaTime * damping); // lerp from current pos to centre. (doing this will give slowing down effect as it apreaces its destination)
		transform.position = currentPos; // apply the possition - neeed to do it here as we will later cast ray to get the worlds position from viewport so we want it from new possition of viewport 

		Vector3 targetCoords = Camera.main.WorldToViewportPoint (target.position); // get players possition from world space to viewport space
		bool atEdgeV = IsPlayerCloseToScreenEdgeV (targetCoords);//check if player is at vetical edge
		bool atEdgeH = IsPlayerCloseToScreenEdgeH (targetCoords);//check if player is at horizontal edge

		if (atEdgeV || atEdgeH) {

			Vector3 viewPortEdge = targetCoords;//set the viewport edge to players viewport pos for now, then adjust accordingly depending which screen edge we want
			if (atEdgeVertical == EdgeState.Top)
				viewPortEdge.y = 1.0f - playerToscreenEdgeLimit;
			if (atEdgeVertical == EdgeState.Bottom)
				viewPortEdge.y = playerToscreenEdgeLimit;
			if (atEdgeHorizontal == EdgeState.Right)
				viewPortEdge.x = 1.0f - playerToscreenEdgeLimit;
			if (atEdgeHorizontal == EdgeState.Left)
				viewPortEdge.x = playerToscreenEdgeLimit;

			Ray edgeLimitRay = Camera.main.ViewportPointToRay (viewPortEdge);  // cast a ray from the (edges)viewport possition 
			RaycastHit floorHit; // we will check when ray hit the floor
			float rayLength = 100.0f;
			if (Physics.Raycast (edgeLimitRay, out floorHit, rayLength, maskFloorLayer)) {
				
				Vector3 edgeLimitWorldPos = floorHit.point; // get the rays collision point when it colided witht he floor
				Vector3 dist = target.position - edgeLimitWorldPos; // get the distance(difference) from the screen edges world position and our players current possition
				dist.y = 0f; // make sure its all on the same plane

				if (atEdgeHorizontal == EdgeState.Right || atEdgeHorizontal == EdgeState.Left) {
					currentPos.x += dist.x; // if player is at the horizontal edge adjust cameras current pos.x to compensate for the difference /distance between player pos and screens edge world pos to make sure player never goes beyond screen edge
				}
				if (atEdgeVertical == EdgeState.Top || atEdgeVertical == EdgeState.Bottom) {
					currentPos.z += dist.z; // if player is at the horizontal edge adjust cameras current pos.z to compensate for the difference /distance between player pos and screens edge world pos to make sure player never goes beyond screen edge
				}
			}

			transform.position = currentPos; // update the cameras possition
		} //if we are not at the screen edge make sure to reset the states
		if (!atEdgeV && atEdgeVertical != EdgeState.Neither) {
			atEdgeVertical = EdgeState.Neither;
		}
		if (!atEdgeH && atEdgeHorizontal != EdgeState.Neither) {
			atEdgeHorizontal = EdgeState.Neither;
		}
		

	}
		
	//check if target is close to the screen edge in vertical axis
	private bool IsPlayerCloseToScreenEdgeV(Vector3 playerCoords) // passing viewport possition of the player
	{

		if (playerCoords.y > 1.0f - playerToscreenEdgeLimit) {
			atEdgeVertical = EdgeState.Top;
			return true;
		} else if (playerCoords.y < playerToscreenEdgeLimit) {
			atEdgeVertical = EdgeState.Bottom;
			return true;
		}

		return false;
	}
	//check if target is close to the screen edge in horizontal axis
	private bool IsPlayerCloseToScreenEdgeH(Vector3 playerCoords) // passing viewport possition of the player
	{
		if (playerCoords.x > 1.0f - playerToscreenEdgeLimit) {
			atEdgeHorizontal = EdgeState.Right;
			return true;
		} else if (playerCoords.x < playerToscreenEdgeLimit) {
			atEdgeHorizontal = EdgeState.Left;
			return true;
		}
		return false;
	}

	//might not want to have this right here
	// allaw scrolling the camera height
	private void ScrollHeight(float min, float max)
	{
		if (Input.GetAxis ("Mouse ScrollWheel") < 0) 
		{
			if(height < max)
			height++;
			CamAutoOffset (offsetMin,offsetMax);
		}
		if (Input.GetAxis ("Mouse ScrollWheel") > 0) 
		{
			if(height>min)
			height--;
			CamAutoOffset (offsetMin,offsetMax);
		}
	}

	//change the camera offset based on camera height 
	private void CamAutoOffset(float offsetMin, float offsetMax)
	{
		float diff = heightMax - heightMin;
		float factor = diff - (heightMax - height);
		factor /= diff;

		float newOffset = offsetMax * factor;
		if (newOffset < offsetMin)
			newOffset = offsetMin;	

		offset = newOffset;
	}
		
		
}
