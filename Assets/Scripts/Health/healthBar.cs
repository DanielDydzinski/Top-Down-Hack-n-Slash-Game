using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class healthBar : MonoBehaviour {

	private Canvas canvas;
	private RectTransform canvasRT;

	// Use this for initialization
	void Start () {
		
		canvas = GetComponentInChildren<Canvas> ();
		if (canvas == null) {
			Debug.LogError ("No health Canvas found in " + this.gameObject.name);
		}
		canvasRT = canvas.GetComponent<RectTransform> ();
	}
	
	// Update is called once per frame // we doing this after character has moved hence late update
	void LateUpdate () {

		PointAtCamera ();
	}

	//a function to make canvas to which the health bar is attatched to always look at camera;
	private void PointAtCamera()
	{
		//Vector3 direction = Camera.main.transform.position - canvasRT.position; // get direction from camera to canvas
		//direction.x = 0f; // to ensure the health bar will not rotate from side to side
		//Quaternion newRot = Quaternion.LookRotation (direction); // store the direction as a rotation
		//canvasRT.rotation = newRot; // apply new rotation

		 canvasRT.rotation = Camera.main.transform.rotation;
	}
}
