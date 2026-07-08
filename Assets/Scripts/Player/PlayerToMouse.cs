using UnityEngine;

public class PlayerToMouse : MonoBehaviour {

	private float camRayLength;
	// Terrain-independent (flat plane at the player's own height) - used for EVERY aiming/rotation/
	// steering consumer (PlayerRotate's facing, ability/dodge steering, LocomotionState's animation
	// blend, etc. - see every other reader of this property). None of those care about actual ground
	// height, only direction, and deriving it from the real terrain raycast below used to distort it
	// badly whenever the player stood on a platform not included in maskFloorLayer: the ray skips
	// through to real ground far below instead, and a shallow ray has to travel much farther
	// horizontally to reach lower ground, dragging the perceived aim point sideways, not just down
	// (same root cause CameraFollow's centre calculation had - see mouseInWorldPosFlat below).
	public Vector3 playerToMouseDir { private set;  get; }
	// The real terrain hit - kept separate and terrain-accurate on purpose, for things that actually
	// need to sit ON the ground surface (AOE spawn points, the mouseWorldTransform-driven dither mask).
	// Do not use this for aiming/rotation/steering - use playerToMouseDir instead.
	public Vector3 mouseInWorldPos { private set;  get; }
	// Same screen-space ray as mouseInWorldPos, but intersected against a flat plane at the player's
	// own height instead of real terrain. Backs playerToMouseDir above; CameraFollow's centre
	// calculation also reads this directly for the same terrain-independence reason.
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

		// Drives playerToMouseDir - computed unconditionally (unlike the terrain raycast below, which
		// can simply miss everything near a platform edge), since a ray toward a plane the camera is
		// generally looking down at essentially always intersects.
		Plane playerPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
		float flatDist;
		if (playerPlane.Raycast(camRay, out flatDist)) {
			mouseInWorldPosFlat = camRay.GetPoint(flatDist);
			Vector3 dir = mouseInWorldPosFlat - transform.position;

			// Already on the player's own horizontal plane, but keep this for clarity/safety.
			dir.y = 0f;
			playerToMouseDir = dir;
		}

		RaycastHit floorHit;

		if (Physics.Raycast (camRay, out floorHit, camRayLength, maskFloorLayer)) {
			mouseInWorldPos = floorHit.point;
			mouseWorldTransform.position = mouseInWorldPos;
		}
	}
}
