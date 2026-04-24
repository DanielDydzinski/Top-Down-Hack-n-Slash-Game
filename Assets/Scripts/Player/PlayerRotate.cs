using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRotate : MonoBehaviour {

	private Mover mover;												//holds moving direction speed etc,in this case need this to know if we are moving diagonally 
	private PlayerToMouse playerToMouse; 								//holds player to mouse direction vector

	[SerializeField]
	private bool rotateHead;											//should we rotate head to look at mouse direction?
	[SerializeField]
	private Transform playerHead;										//head to be rotated
	[SerializeField]
	private Transform playerChest;										//chest bone of the character, will be rotating it to face player to mouse dir
	[SerializeField]
	private float rotationTimeFracture;									// how fast the character will rotate (1 = 1sec, 2 = 2sec, 0.5 = 0.5sec etc...)


	private Quaternion rootRotation;									//to store the the angle to which the character will rotate to

	public enum FacingDirEnum {Forward, Backwards, Left, Right,
						TopRight, TopLeft, BottomRight, BottomLeft}		//to know which way character is currently facing and use it as a trigger to start rotation when switching to new state
	public FacingDirEnum facingDir;

	public Vector3 facingDirVec3;

    private Vector3 diagonalRight = new Vector3(0.5f, 0.0f, 0.5f);
    private Vector3 diagonalLeft = new Vector3(-0.5f, 0.0f, 0.5f);
    private Vector3 negDiagonalRight= new Vector3(0.5f, 0.0f, -0.5f);
    private Vector3 negDiagonalLeft = new Vector3(-0.5f, 0.0f, -0.5f);



    void Start()
	{	
		//get these components attached to this object
		playerToMouse = GetComponent<PlayerToMouse> ();
		mover = GetComponent<Mover> ();
	}



	// Update is called once per frame <- Using late update to override animations transform to rotate chest to mouse manually
	void LateUpdate () {

		// turns character chest to mouse direction and rotates character to conveneint facing direction
		Turning ();
	}





	void Turning ()
	{
		// get the player to mouse vector
		Vector3 playerMouseDir = playerToMouse.playerToMouseDir;

		// workout dotproduct between worlds forward direction and players to mouse direction
		float dotP = Vector3.Dot (Vector3.forward, playerMouseDir.normalized);

		//IF NOT MOVING DIAGONALY DO THIS
		//  if dot > 0.707 (45 degree angle) face character forward
		if (!mover.movingTopRight && !mover.movingTopLeft && !mover.movingBottomRight && !mover.movingBottomLeft) {
			
			if (dotP > 0.707f) {

				//trigger point to start rotating character  ( rotate him to facce forwards if he already is not)
				if (facingDir != FacingDirEnum.Forward) {

					rootRotation = Quaternion.LookRotation (Vector3.forward); // update the root values to face forward
					StopCoroutine ("Qlerp"); //stop any previous rotations that might conflict with new one 
					StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture)); //rotate the character to the correct  direction
					facingDir = FacingDirEnum.Forward; // update the facing state so this will not be triggered again 
                    facingDirVec3 = Vector3.forward;
				}
			} else if (dotP < 0.707f && dotP > -0.707f) { // if dot between angles 45 degrees and 135 degrees character needs face sideways, but which side?

				if (playerMouseDir.x > 0) { //if player to mouse direction x is possitive face right 

					if (facingDir != FacingDirEnum.Right) {

						rootRotation = Quaternion.LookRotation (Vector3.right);
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.Right;
						facingDirVec3 = Vector3.right;
                    }
				} else {    //if player to mouse direction x is negative face left 
				
					if (facingDir != FacingDirEnum.Left) {
					
						rootRotation = Quaternion.LookRotation (Vector3.left);
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.Left;
						facingDirVec3 = Vector3.left;
					}
				}
			}
		}
		if (dotP < -0.707) {	//if the angle si greater than 135 degrees make character face backwards if he already isnt
			
			if (facingDir != FacingDirEnum.Backwards) {

				rootRotation = Quaternion.LookRotation (Vector3.back);
				StopCoroutine ("Qlerp");
				StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
				facingDir = FacingDirEnum.Backwards;
				facingDirVec3 =	Vector3.back;
			}
		}

		//IF MOVING DIAGONALY DO THIS
		//we will face charatcer in diagnal diractions 
		if (mover.movingTopRight || mover.movingTopLeft) {

			if (dotP > 0f) {	// if anglee between player to mouse vector  and vector3.forward is < 90  degrees character is facing forwards
				if (playerMouseDir.x > 0) { // if mouse x asis is positive face tot he right

					if (facingDir != FacingDirEnum.TopRight) {
						
						rootRotation = Quaternion.LookRotation (new Vector3 (1.0f, 0.0f, 1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.TopRight;
						facingDirVec3 = diagonalRight;
                    }
				} else { // if player to mouse dir x is negative face to the left
					if (facingDir != FacingDirEnum.TopLeft) {

						rootRotation = Quaternion.LookRotation (new Vector3 (-1.0f, 0.0f, 1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.TopLeft;
						facingDirVec3 = diagonalLeft;
					}
				}
			} else if (dotP < 0) { // if anglee between player to mouse vector  and vector3.forward is > 90  degrees character is facing backwards
				if (playerMouseDir.x > 0) { // if mouse x asis is positive face to the right

					if (facingDir != FacingDirEnum.BottomRight) {

						rootRotation = Quaternion.LookRotation (new Vector3 (1.0f, 0.0f, -1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.BottomRight;
						facingDirVec3 = negDiagonalRight; 
					}
				} else {  // if player to mouse dir x is negative face to the left
					if (facingDir != FacingDirEnum.BottomLeft) {

						rootRotation = Quaternion.LookRotation (new Vector3 (-1.0f, 0.0f, -1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.BottomLeft;
						facingDirVec3 = negDiagonalLeft;
					}

				}
			}
		} else if (mover.movingBottomLeft || mover.movingBottomRight) {
			if (dotP < 0f) {
				if (playerMouseDir.x > 0) {
					if (facingDir != FacingDirEnum.BottomRight) {

						rootRotation = Quaternion.LookRotation (new Vector3 (1.0f, 0.0f, -1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.BottomRight;
                        facingDirVec3 = negDiagonalRight;

                    }
				} else {
					if (facingDir != FacingDirEnum.BottomLeft) {

						rootRotation = Quaternion.LookRotation (new Vector3 (-1.0f, 0.0f, -1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.BottomLeft;
                        facingDirVec3 = negDiagonalLeft;
                    }
				}
			} else if (dotP > 0) {
				if (playerMouseDir.x > 0) {
					if (facingDir != FacingDirEnum.TopRight) {

						rootRotation = Quaternion.LookRotation (new Vector3 (1.0f, 0.0f, 1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.TopRight;
						facingDirVec3 = diagonalRight;
					}
				} else {
					if (facingDir != FacingDirEnum.TopLeft) {

						rootRotation = Quaternion.LookRotation (new Vector3 (-1.0f, 0.0f, 1.0f));
						StopCoroutine ("Qlerp");
						StartCoroutine (Qlerp (transform.rotation, rootRotation, rotationTimeFracture));
						facingDir = FacingDirEnum.TopLeft;
						facingDirVec3 = diagonalLeft;
					}
				}
			}
		}
			

	
		// Create a quaternion (rotation) based on looking down the vector from the player to the mouse.
		Quaternion playerMouseRot = Quaternion.LookRotation (playerMouseDir);
		//character facing dir
		Quaternion qForward = Quaternion.LookRotation (transform.forward);
		Quaternion qRight = Quaternion.LookRotation (transform.right);

		// save players chest rotation in eular, modify the x and z axis to 0 and store it as quaternion,
		//we only want to use this to campare angle of rotation on y axis
		Vector3 chestEuler = playerChest.rotation.eulerAngles;
		chestEuler.x = 0f;
		chestEuler.z = 0f;
		Quaternion chestForward = Quaternion.Euler (chestEuler);

		//calculate the angle the animation rotates the chest against character facing rotation
		float chestAnimOffsetAngle = Quaternion.Angle (qForward, chestForward);
		float chestAnimOffsetAngleGuide = Quaternion.Angle (qRight, chestForward);

		//90degree angle check to determin polarity, as quaternions only can store values up to 180. if angle is greater than 180 reverse polarity
		if (chestAnimOffsetAngleGuide > 90.0f) {
			chestAnimOffsetAngle = -chestAnimOffsetAngle;
		}

		//store the angle as quaternion
		Quaternion chestAnimOffset = Quaternion.AngleAxis (chestAnimOffsetAngle, Vector3.up);

		// save players chest  rotation cause by animation in eular, modify the y axis to 0 and store it as quaternion,
		//so we are left with the x and z rotations to add to the final rotation at the end - we only want to override the rotation in Y axis
		//so we need to add the  x and z rotations back in
		Vector3 v = playerChest.rotation.eulerAngles;
		v.y = 0f;
		Quaternion oldXandZRotation = Quaternion.Euler (v);


		// Set the player's chest rotation to this new rotation ( add the offset to the new rotation and then add the old roation of x and z axis ; note : multiplying quaternions is adding them )
		playerChest.rotation = (playerMouseRot * chestAnimOffset) * oldXandZRotation ;

		//rotate head to always look at mouse 
		if (rotateHead) 
		{
			Vector3 vec = playerHead.rotation.eulerAngles;
			vec.y = 0f;
			Quaternion oldXandZRotationForHead = Quaternion.Euler (v);

			playerHead.rotation = playerMouseRot * oldXandZRotationForHead;

		}
	}

	//function to rotate this object between two angles over time, notice we used Ienumerator as we are using while loop so we c	an jump in and out before loop is finished
	private IEnumerator Qlerp(Quaternion from, Quaternion to, float timeFracture)
	{
		float timer = 0f;
		while (from != to) {
			
			timer += Time.deltaTime / timeFracture;
			Quaternion q = Quaternion.Lerp (from, to, timer);

			transform.rotation = q;

			yield return null;
		}
	}

    public void UpdateOrientation()
    {
        // Stop any existing lerps so they don't overwrite our dodge rotation
        StopAllCoroutines();

        // 1. Get current forward vector
        Vector3 currentForward = transform.forward;
        currentForward.y = 0;
        currentForward.Normalize();

        // 2. Snap the rootRotation to current for the next Qlerp start
        rootRotation = transform.rotation;

        // 3. Logic to map the current physical forward to your Enum
        // We use the same thresholds (0.707 for 45 deg) as your Turning() function
        float dotF = Vector3.Dot(Vector3.forward, currentForward);
        float dotR = Vector3.Dot(Vector3.right, currentForward);

        // Determine the closest Enum based on the current transform
        if (dotF > 0.8f) facingDir = FacingDirEnum.Forward;
        else if (dotF < -0.8f) facingDir = FacingDirEnum.Backwards;
        else if (dotR > 0.8f) facingDir = FacingDirEnum.Right;
        else if (dotR < -0.8f) facingDir = FacingDirEnum.Left;
        // Diagonals
        else if (dotF > 0.1f && dotR > 0.1f) facingDir = FacingDirEnum.TopRight;
        else if (dotF > 0.1f && dotR < -0.1f) facingDir = FacingDirEnum.TopLeft;
        else if (dotF < -0.1f && dotR > 0.1f) facingDir = FacingDirEnum.BottomRight;
        else if (dotF < -0.1f && dotR < -0.1f) facingDir = FacingDirEnum.BottomLeft;

        // 4. Sync the Vec3 helper
        UpdateFacingVec3();
    }

    private void UpdateFacingVec3()
    {
        facingDirVec3 = facingDir switch
        {
            FacingDirEnum.Forward => Vector3.forward,
            FacingDirEnum.Backwards => Vector3.back,
            FacingDirEnum.Left => Vector3.left,
            FacingDirEnum.Right => Vector3.right,
            FacingDirEnum.TopRight => diagonalRight,
            FacingDirEnum.TopLeft => diagonalLeft,
            FacingDirEnum.BottomRight => negDiagonalRight,
            FacingDirEnum.BottomLeft => negDiagonalLeft,
            _ => Vector3.forward
        };
    }

    public void StopAllRotation()
    {
        StopAllCoroutines();
        // If you are using a specific reference like 'rotationRoutine', 
        // use StopCoroutine(rotationRoutine) instead to be safer.
    }
}

