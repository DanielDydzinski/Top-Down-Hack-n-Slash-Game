using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Mover))]
[RequireComponent(typeof(PlayerToMouse))]

public class CharMoveAnimControler : MonoBehaviour {

	[SerializeField]
	private Transform chest;
	Animator anim;
	PlayerToMouse playerToMouse;
	private Mover mover;

	private float attackTimer;
	private float attackComboTime;

	[SerializeField]
	private int ComboMax;
	private int attackState;
	private int attackDefaultState;

	[SerializeField]
	private float attackGlobalCD;
	private float attackGlobalCDTimer;
	// Use this for initialization
	void Start () {

		attackDefaultState = -1;
		attackState = 0;
		attackTimer = 0.0f;
		attackComboTime = 2.0f;

		playerToMouse = GetComponent<PlayerToMouse> ();
		mover = GetComponent<Mover> ();
		anim = GetComponent<Animator> ();

	}
	
	// Update is called once per frame
	void Update () {
		UpdateAnimator ();

        if (Input.GetKeyDown(KeyCode.Escape))
            {

            Application.Quit();
            }
	}

	private void UpdateAnimator()
	{
		Vector3 playerToMouseDir = playerToMouse.playerToMouseDir.normalized;
		float dotP = Vector3.Dot (mover.GetDirection (), playerToMouseDir);
		anim.SetFloat ("MoveDirMouseDirDotP", dotP);

		if (mover.GetDirection () == Vector3.zero) {
			anim.SetBool ("isMoving", false);
		}
		else 
		{
			anim.SetBool ("isMoving", true);

			if (dotP > 0.707f) {
				anim.SetFloat ("RunBlend", 1.0f);
			} 
			else if (dotP < -0.707) {
				anim.SetFloat ("RunBlend", -1.0f);
			}
		}

		attackTimer += Time.deltaTime;
		attackGlobalCDTimer += Time.deltaTime;

		if(attackGlobalCDTimer > attackGlobalCD)
		{
		if (attackTimer > attackComboTime) {
			attackTimer = 0.0f;
			attackState = attackDefaultState;
			anim.SetInteger ("AttackState", attackDefaultState);
		}
	
		if (Input.GetMouseButtonDown (0)) {
	
			attackState++;
            anim.SetInteger("AttackState", attackState);
            attackTimer = 0f;
			if (attackState >= ComboMax)
			{
				attackState = attackDefaultState;
			}
			attackGlobalCDTimer = 0 ;

		} else if (Input.GetMouseButtonUp (0)){
			
			//anim.SetInteger ("AttackState", attackDefaultState);
		}
		}
	}
		
}


