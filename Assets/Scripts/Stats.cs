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

    public float mass = 3f;
	[SerializeField] private float dodgeAnimationSpeed= 1f;
	public float dodgePower;


    public float GetDodgeAnimationSpeed() => dodgeAnimationSpeed;
    public void SetDodgeAnimationSpeed(float value) => dodgeAnimationSpeed = value;
    public float GetMass() => mass;
    public void SetMass(float value) => mass = value;

    public bool isPushed = false;

    private float effectSpeedMultiplier = 1f;

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

    public void SetEffectMultiplier(float mult)
    {
        effectSpeedMultiplier = mult;
        ApplyCurrentSpeed();
    }
    public float GetModifiedSpeed() => moveSpeed * effectSpeedMultiplier;

    private void ApplyCurrentSpeed()
    {
        if (isPlayer)
        {
            // For players, the Mover calculates final speed in its Move() loop
            // so we don't need to push anything here, but we could if needed.
        }
        else if (navMeshAgent != null)
        {
            // For AI, we have to push the slowed speed directly to the agent
            navMeshAgent.speed = GetModifiedSpeed();
        }
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
