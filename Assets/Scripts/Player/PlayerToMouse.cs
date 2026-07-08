using UnityEngine;

public class PlayerToMouse : MonoBehaviour {

	private float camRayLength;
	public Vector3 playerToMouseDir { private set;  get; }
	public Vector3 mouseInWorldPos { private set;  get; }
	// Same screen-space ray as mouseInWorldPos, but intersected against a flat plane at the player's
	// own height instead of real terrain - CameraFollow's centre calculation uses this instead of
	// mouseInWorldPos, so panning doesn't speed up/slow down purely because of terrain height under the
	// cursor (a shallow ray has to travel much farther horizontally to reach lower ground, e.g. near a
	// cliff/canyon, than to reach ground at the player's own height). mouseInWorldPos itself stays
	// terrain-accurate for everything else (AOE spawn points, the mouseWorldTransform-driven dither
	// mask, etc.) - don't repoint those at this instead.
	public Vector3 mouseInWorldPosFlat { private set; get; }
	[SerializeField] LayerMask maskFloorLayer;
	[SerializeField] private Transform mouseWorldTransform;

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

		Plane playerPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
		float flatDist;
		if (playerPlane.Raycast(camRay, out flatDist)) {
			mouseInWorldPosFlat = camRay.GetPoint(flatDist);
		}

		RaycastHit floorHit;

		if (Physics.Raycast (camRay, out floorHit, camRayLength, maskFloorLayer)) {
			// Create a vector from the player to the point on the floor the raycast from the mouse hit.
			mouseInWorldPos = floorHit.point;
			Vector3 dir = mouseInWorldPos - transform.position;

			// Ensure the vector is entirely along the floor plane.
			dir.y = 0f;
			playerToMouseDir = dir;

			mouseWorldTransform.position = mouseInWorldPos;
		}
	}
}
