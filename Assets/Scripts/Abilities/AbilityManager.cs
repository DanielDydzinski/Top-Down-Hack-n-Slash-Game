using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static Ability;

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
    public Ability activeAbility;
    private Transform activeTarget; // The target for the current ability execution
    public event Action OnAbilityReady;


    private EffectSpawnPossitions effectSpawnPossitions;

    public Transform VisualLeftHandAttachPoint, VisualRightHandAttachPoint;
    private GameObject activeVisualEffect;

    // Use this for initialization
    void Start () {

        effectSpawnPossitions = GetComponent<EffectSpawnPossitions>();

		//if(this.tag == "Enemy")
		//{
		//	spawnLocation = transform.GetChild (2).transform; // in the inspector make sure the spwnloaction is a 3rd child
		//}

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
    public bool IsPerformingAction()
    {
        // If the animator is anything other than -1, an ability is technically "active"
        return animator.GetInteger(AttackStateHash) != -1;
    }
    public void CancelAbility() // animations will have events at end of the animation to call this function - can also be used for interupts etc.
    {
        animator.SetInteger(AttackStateHash, -1); // note : animation must not have exit time or this won't work // -1 is no attack 
        Debug.Log("Canceling ability");
        if (activeVisualEffect != null)
        {
            Destroy(activeVisualEffect);
        }
        CancelGetHitAnim();
        ClearActiveAbility();
    }

    public void CancelAbilityButtonUp()
    {
        // 1. If we aren't doing anything, don't bother
        if (activeAbility == null || !IsPerformingAction()) return;

        PlayerStateMachine psm = GetComponent<PlayerStateMachine>();

        // 2. Determine which animator layer we need to clear
        int targetLayer = psm.BaseLayer; // Default

        if (activeAbility.animLayer == AnimationLayer.UpperBody)
        {
            targetLayer = psm.AttackLayer; // Index 1
        }
        else if (activeAbility.animLayer == AnimationLayer.FullBody)
        {
            targetLayer = psm.FullBodyLayer; // Index 2
        }

        // 3. Force the crossfade to the empty "Transition" state on that specific layer
        // 0.1f is the transition duration—it makes the stop feel smooth rather than a "pop"
        animator.CrossFade(psm.TransitionStateHash, 0.1f, targetLayer);

        // 4. Run your standard cleanup (resets AttackState to -1, clears activeAbility)
        CancelAbility();
    }

    public void CancelGetHitAnim()
	{
		animator.SetBool ("GetHit", false);
	}

    // This is what the Animation Event will call ()
    public void ExecuteActiveAbility()
    {
        Vector3 spawnPos = spawnLocation.position;
        Quaternion spawnRot = spawnLocation.rotation;

        if (activeAbility != null)
        {

            switch (activeAbility.spawnLocation)
            {
                case abilitySpawnType.Onself:
                    // Stays as spawnLocation defaults
                    spawnPos += activeAbility.spawnLocationOffset;

                    break;

                case abilitySpawnType.OnTarget:
                    if (activeTarget != null)
                    {
                        spawnPos = activeTarget.position;
                        spawnPos += activeAbility.spawnLocationOffset;
                        spawnRot = Quaternion.identity;
                    }
                    break;
                case abilitySpawnType.PlayerRoot:

                    spawnPos = effectSpawnPossitions.abilitySpawnRootPos.position;
                        spawnRot = this.transform.rotation;

                    break;

                case abilitySpawnType.SpecifiedPoint:

                    spawnRot *= Quaternion.Euler(activeAbility.spawnRotationOffset);
                    // Handle third case here
                    break;
            }

            // We use the already cached activeAbility instead of looking it up by string
            currentAbilityObject = activeAbility.Cast(spawnPos, spawnRot,this.gameObject);
           // Debug.Log($"Executed: {activeAbility.abilityName}");
        }
        else
        {
            Debug.LogWarning("Animation tried to cast, but no activeAbility was set! from " +this.gameObject.name);
        }
    }

    public void StartCastingAbility(Ability ab, Transform target) // this will trigger animation to play
	{
		if (cooldowns [ab.abilityName].coolDownReady) 
		{

            activeTarget = target;
            //activeAbility = cooldowns[ab.name].ability;
            activeAbility = ab;
            animator.SetInteger (AttackStateHash, cooldowns [ab.abilityName].ability.attackState);
            if(ab.AudioClip!=null) GetComponent<AudioSource>().PlayOneShot(ab.AudioClip);//play ability audio when casting
			StartCoroutine (RunCoolDown (cooldowns [ab.abilityName]));
		}
	}
    public void ClearActiveAbility()
    {
        activeAbility = null;
    }

    public void CastAbility (string name) // animation will have event set at certain frame to call this function with the ability name to triger
	{
			currentAbilityObject = cooldowns [name].TriggerAbility (spawnLocation.position, spawnLocation.rotation,this.gameObject);
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

    public void PlayVisuals()
    {
        // 1. Safety Check
        if (activeAbility == null || activeAbility.abilityVisualPartyicles == null) return;

        // 2. Identify the target parent based on your Enum
        Transform targetParent = transform; // Default fallback to character root

        if (activeAbility.attachPoint == VisualAttachPoint.RightHand)
        {
            if (VisualRightHandAttachPoint != null) targetParent = VisualRightHandAttachPoint;
        }
        else if (activeAbility.attachPoint == VisualAttachPoint.LeftHand)
        {
            if (VisualLeftHandAttachPoint != null) targetParent = VisualLeftHandAttachPoint;
        }
        // Root/Default case uses the 'transform' initialized above

        // 3. Calculate Final Position/Rotation using the SO Offsets
        Vector3 finalPos = targetParent.position + activeAbility.spawnLocationOffset;

        // Combining the parent rotation with the SO's rotation offset
        Quaternion finalRot = targetParent.rotation * Quaternion.Euler(activeAbility.spawnRotationOffset);

        // 4. Instantiate and Parent
        // This ensures the particle stays glued to the hand/head during animations
            activeVisualEffect = Instantiate(
            activeAbility.abilityVisualPartyicles,
            finalPos,
            finalRot,
            targetParent
        );

        // Optional: If you want to auto-destroy visuals after a set time
        // Destroy(vfx, 3.0f); 
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
