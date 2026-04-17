using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPS : MonoBehaviour {


	Text t ;
	// Use this for initialization
	void Start () {
		t = GetComponent<Text> ();
		
	}
	
	// Update is called once per frame
	void Update () {
		float fps = 1.0f / Time.deltaTime;
		t.text = fps.ToString ();
	}
}
