using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mover))]
public class PlayerMovement : MonoBehaviour {

	private Mover _Mover;
	private Vector3 movingDirection;
	private Vector3 diagonalRight;
	private Vector3 diagonalLeft;
	private Vector3 negDiagonalRight;
	private Vector3 negDiagonalLeft;
	private bool move;


	// Use this for initialization
	void Start () {
		
		InitializeVariables ();
	}
	
	// Update is called once per frame
	void Update () {

		WSADmovement ();
		
	}

	private void InitializeVariables()
	{
		_Mover = GetComponent<Mover> ();
		movingDirection = Vector3.zero;
		move = false;


		diagonalRight = new Vector3 (0.5f, 0.0f, 0.5f);
		diagonalLeft = new Vector3 (-0.5f, 0.0f, 0.5f);
		negDiagonalRight = new Vector3 (0.5f, 0.0f, -0.5f);
		negDiagonalLeft = new Vector3 (-0.5f, 0.0f, -0.5f);
			
	}

	private void WSADmovement()
	{
		if (Input.GetKey (KeyCode.W) || Input.GetKey (KeyCode.S) || Input.GetKey (KeyCode.D) || Input.GetKey (KeyCode.A)) {
			if (Input.GetKey (KeyCode.W)) {
				movingDirection = Vector3.forward;
				move = true;
			}
			if (Input.GetKey (KeyCode.S)) {
				movingDirection = Vector3.back;
				move = true;
			}
			if (Input.GetKey (KeyCode.D)) {
				movingDirection = Vector3.right;
				move = true;
			}
			if (Input.GetKey (KeyCode.A)) {
				movingDirection = Vector3.left;
				move = true;
			}
				


			if (Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.D)) {
				movingDirection = diagonalRight;
				_Mover.movingTopRight = true;
				move = true;
			} else {
				_Mover.movingTopRight = false;
			}
			if (Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.A)) {
				movingDirection = diagonalLeft;
				_Mover.movingTopLeft = true;
				move = true;
			} else {
				_Mover.movingTopLeft = false;
			}
			if (Input.GetKey (KeyCode.S) && Input.GetKey (KeyCode.D)) {
				movingDirection = negDiagonalRight;
				_Mover.movingBottomRight = true;
				move = true;
			}else {
				_Mover.movingBottomRight = false;
			}
			if (Input.GetKey (KeyCode.S) && Input.GetKey (KeyCode.A)) {
				movingDirection = negDiagonalLeft;
				_Mover.movingBottomLeft = true;
				move = true;
			}else {
				_Mover.movingBottomLeft = false;
			}

		} else {
			move = false;
			_Mover.SetDirection (Vector3.zero);
		}
			
		if (move == true) {
		
			_Mover.SetDirection (movingDirection);
			_Mover.Move ();
		} 



	}



		
}
