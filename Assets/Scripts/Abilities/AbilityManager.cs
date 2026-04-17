using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AbilityManager : MonoBehaviour {
	
	GameObject currentAbilityObject;
	Coroutine castAbilityRoutine;
	Animator animator;
    private static readonly int AttackStateHash = Animator.StringToHash("AttackState");

    public Dictionary<string,CoolDown> cooldowns;

	[SerializeField]
	private Transform spawnLocation;

	public List<Ability> abilities;

    private List<Ability> meleeAbilities = new List<Ability>();
    private List<Ability> rangedAbilities = new List<Ability>();

    private bool IsCastingAbility = false;
    private Ability activeAbility;
    public event Action OnAbilityReady;

    // Use this for initialization
    void Start () {

		if(this.tag == "Enemy")
		{
			spawnLocation = transform.GetChild (2).transform; // in the inspector make sure the spwnloaction is a 3rd child
		}

		if (GetComponent<Animator> ()) {
			animator = GetComponent<Animator> ();
		} else {
			Debug.Log (gameObject.name + " does't have Animator");
		}

		cooldowns = new Dictionary<string, CoolDown> ();
		foreach (Ability ab in abilities)
		{
			cooldowns.Add (ab.abilityName, new CoolDown(ab));
		}
	}

	void Update()
	{
		if (Input.GetKey (KeyCode.C)) {
			CancelAbility ();
		}
	}

    public void InitializeAbilities(List<Ability> allAbilities)
    {
        // Sort them into their respective buckets once
        foreach (var a in allAbilities)
        {
            if (a.isRanged) rangedAbilities.Add(a);
            else meleeAbilities.Add(a);
        }
    }


    public void SetMovementLock(bool locked)
    {
        IsCastingAbility = locked;

        // If we are unlocking, we can also ensure the AttackState is reset
        if (!locked)
        {
            animator.SetInteger(AttackStateHash, -1);
        }
    }

	public bool GetIsCastingAbility()
	{
		return IsCastingAbility;
	}

    public void CancelAbility() // animations will have events at end of the animation to call this function - can also be used for interupts etc.
	{
		animator.SetInteger (AttackStateHash, -1); // note : animation must not have exit time or this won't work // -1 is no attack 
		Debug.Log("Canceling ability");
		CancelGetHitAnim();
        ClearActiveAbility();
    }

	public void CancelGetHitAnim()
	{
		animator.SetBool ("GetHit", false);
	}

    // This is what the Animation Event will call ()
    public void ExecuteActiveAbility()
    {
        if (activeAbility != null)
        {
            // We use the already cached activeAbility instead of looking it up by string
            currentAbilityObject = activeAbility.Cast(spawnLocation.position, spawnLocation.rotation);
            Debug.Log($"Executed: {activeAbility.abilityName}");
        }
        else
        {
            Debug.LogWarning("Animation tried to cast, but no activeAbility was set!");
        }
    }

    public void StartCastingAbility(Ability ab) // this will trigger animation to play
	{
		if (cooldowns [ab.abilityName].coolDownReady) 
		{
            //activeAbility = cooldowns[ab.name].ability;
            activeAbility = ab;
            animator.SetInteger (AttackStateHash, cooldowns [ab.abilityName].ability.attackState);
			StartCoroutine (RunCoolDown (cooldowns [ab.abilityName]));
		}
	}
    public void ClearActiveAbility()
    {
        activeAbility = null;
    }

    public void CastAbility (string name) // animation will have event set at certain frame to call this function with the ability name to triger
	{
			currentAbilityObject = cooldowns [name].TriggerAbility (spawnLocation.position, spawnLocation.rotation);
			//Debug.Log("Casting ability " + name);
		//	StartCoroutine (RunCoolDown (cooldowns [name]));

	}

	public void AddAbility(Ability ab)
	{
		cooldowns.Add(ab.abilityName, new CoolDown(ab));
	}

	private IEnumerator RunCoolDown(CoolDown cd)
	{
		cd.stopwatch.Stop ();
		cd.stopwatch.Reset ();
		cd.stopwatch.Start ();

		cd.coolDownReady = false;
		cd.timeLeft = cd.ability.cooldown;

		while (!cd.coolDownReady)
		{
			cd.timeLeft = cd.ability.cooldown - (float)cd.stopwatch.Elapsed.TotalSeconds;
			cd.coolDownReady = (cd.timeLeft <= 0f);
			yield return null;
		}
			
		cd.stopwatch.Stop ();
		cd.stopwatch.Reset ();
        // TRIGGER THE EVENT: Tell everyone listening new ability became avaliable 
        OnAbilityReady?.Invoke();

    }
    // Helper to see if the current casting ability allows movement
    public bool CurrentAbilityAllowsMovement()
    {
        // You'll need to store a reference to the 'activeAbility' when StartCasting is called
        if (activeAbility != null) return activeAbility.canMoveAttack;
        return true; // Default to true if not casting
    }

    public Ability GetHighestPriorityAbility()
    {
        Ability bestAbility = null;
        int topPriority = int.MaxValue; // Start with the lowest possible priority

        foreach (var cd in cooldowns.Values)
        {
            if (cd.coolDownReady)
            {
                // If this ability's priority is "more important" (smaller number) than our current best
                if (cd.ability.priority < topPriority)
                {
                    topPriority = cd.ability.priority;
                    bestAbility = cd.ability;
                }
            }
        }
        return bestAbility;
    }
    public Ability GetHighestPriorityReady(bool wantRanged)
    {
        List<Ability> listToSearch = wantRanged ? rangedAbilities : meleeAbilities;

        Ability best = null;
        int topPriority = int.MaxValue;

        foreach (var a in listToSearch)
        {
            if (cooldowns[a.abilityName].coolDownReady && a.priority < topPriority)
            {
                topPriority = a.priority;
                best = a;
            }
        }
        return best;
    }

    private Ability GetFirstReadyAbility()
    {
        foreach (var cd in cooldowns.Values)
        {
            if (cd.coolDownReady) return cd.ability;
        }
        return null;
    }

}
