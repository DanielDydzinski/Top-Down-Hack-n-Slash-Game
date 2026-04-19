using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class Stats : MonoBehaviour
{
	[SerializeField]
	private float maxHealth;
	[SerializeField]
	private float moveSpeed;
	[SerializeField]
	private float abilityPower;
	[SerializeField]
	private float physicalPower;
	//public float damageReduction;

	private Health health;
	private Mover mover;
	private NavMeshAgent navMeshAgent;

	private bool isPlayer;

	void Start()
	{
		isPlayer = false;

        // Get components directly
        navMeshAgent = GetComponent<NavMeshAgent>();
        mover = GetComponent<Mover>();

        health = GetComponent<Health> ();

		if (mover!=null)
		{
			isPlayer = true;
		}

		UpdateMaxHealth ();
		UpdateMoveSpeed ();
		
	}


	public void UpdateMaxHealth()
	{
		health.SetMaxHealth (maxHealth);
	}

	public void UpdateMoveSpeed() // might need some comparison to workout percentage change from slow or something so we dont just over ride any slows / roots etc
	{
		if (isPlayer)
		{
			mover.SetMaxSpeed (moveSpeed);
		}
		else
		{
			navMeshAgent.speed = moveSpeed;
		}
	}

    //update ability power
    //update physical power


    public void SetMaxHealth(float health)
	{
		maxHealth = health;
	}
	public float GetMaxHealth()
	{
		return maxHealth;
	}

	public void SetMoveSpeed(float speed)
	{
		moveSpeed = speed;
	}
	public float GetMoveSpeed()
	{
		return moveSpeed;
	}

	public void SetAbilityPower(float ap)
	{
		abilityPower = ap;
	}
	public float GetAbilityPower()
	{
		return abilityPower;
	}

	public void SetPhysicalPower(float pp)
	{
		physicalPower = pp;
	}
	public float GetPhysicalPower()
	{
		return physicalPower;
	}

}
