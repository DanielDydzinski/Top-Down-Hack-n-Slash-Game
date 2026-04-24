using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public IPlayerState currentState;

    // References for the states to use
    [HideInInspector] public Mover mover;
    [HideInInspector] public PlayerRotate rotator;
    [HideInInspector] public Animator anim;
    [HideInInspector] public PlayerToMouse playerToMouse;
    [HideInInspector] public AbilityManager abilityManager;
    [HideInInspector] public PlayerMovement playerMovement;
    [HideInInspector]public Stats stats;
    [HideInInspector] public AnimationClip backFlipDodgeAnimationClip;
    [HideInInspector] public AnimationClip rollDodgeAnimationClip;
    [HideInInspector] public CharacterController characterController;

    public  readonly int CombatStanceStateHash = Animator.StringToHash("CombatStance");
    public  readonly int TransitionStateHash = Animator.StringToHash("Transition");
    public readonly int IsMovingHash = Animator.StringToHash("isMoving");
    public readonly int BackFlipDodgeHash = Animator.StringToHash("BackFlipDodge");
    public readonly int isDodgingHash = Animator.StringToHash("isDodging");
    public readonly int rollDodgeHash = Animator.StringToHash("RollDodge");
    public readonly int isRollDodgeHash = Animator.StringToHash("isRollDodge");



    public readonly int BaseLayer = 0;
    public readonly int AttackLayer = 1;
    public readonly int FullBodyLayer = 2;
    public float dodgeAnimationSpeed = 1;

    public int attackLayerIndex = 1;

    // Cache states for easy switching
    public LocomotionState locomotionState { get; private set; }


    void Start()
    {
        mover = GetComponent<Mover>();
        rotator = GetComponent<PlayerRotate>();
        anim = GetComponent<Animator>();
        playerToMouse = GetComponent<PlayerToMouse>();
        abilityManager = GetComponent<AbilityManager>();
        playerMovement = GetComponent<PlayerMovement>();
        stats = GetComponent<Stats>();
        characterController = GetComponent<CharacterController>();

        dodgeAnimationSpeed = stats.GetDodgeAnimationSpeed();


        // 1. Initialize the dictionary
        InitializeAnimationCache();

        // 2. Grab the specific clip for your dodge if you need it as a reference
        backFlipDodgeAnimationClip = GetClipByName("BackFlipDodge");
        rollDodgeAnimationClip = GetClipByName("RollDodge");



        // Initialize states
        locomotionState = new LocomotionState(this);
        SwitchState(locomotionState);
    }

    void Update()
    {
        currentState?.UpdateState();
    }

    public void SwitchState(IPlayerState newState)
    {
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();
    }

    public Dictionary<string, float> AnimationLengths = new Dictionary<string, float>();

    private void InitializeAnimationCache()
    {
        // Fill the dictionary with EVERY clip so ActionState can find any ability length
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
            if (!AnimationLengths.ContainsKey(clip.name))
            {
                AnimationLengths.Add(clip.name, clip.length);
                Debug.Log("Initilized animationClip"+ clip.name);
            }
        }
    }

    public AnimationClip GetClipByName(string clipName)
    {
        // Search the actual clips to return the file reference
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip;
        }
        return null;
    }

    // Helper method to check state from other scripts
    public bool IsInState<T>() where T : IPlayerState
    {
        return currentState is T;
    }

    public bool IsStunned()
    {
        return currentState is PlayerStunState;
    }
    public bool IsDodging()
    {
        return currentState is BackflipDodgeState || currentState is RollDodgeState;
    }
    public IPlayerState GetCurrentState() => currentState;
}