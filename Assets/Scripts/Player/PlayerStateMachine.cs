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
    [HideInInspector] public Stats stats;
    [HideInInspector] public PlayerEnergy playerEnergy;
    [HideInInspector] public AnimationClip backFlipDodgeAnimationClip;
    [HideInInspector] public AnimationClip rollDodgeAnimationClip;
    [HideInInspector] public AnimationClip secondRollDodgeAnimationClip;
    [HideInInspector] public AnimationClip secondBackFlipDodgeAnimationClip;
    [HideInInspector] public AnimationClip landingAnimationClip;
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public CameraFollow cameraFollow;

    [Header("Block System Settings")]
    [Range(0f, 180f)] public float blockAngleLimit = 45f;
    public string blockLoopAnimationName = "BlockLoop";
    public List<string> blockReactionClipNames = new List<string>();
    public float blockRotationSpeed = 360f;

    [Header("Dodge Settings")]
    [SerializeField] public float dodgeRotationSpeed = 720f;
    [Tooltip("The 2nd dodge state rotation input")]
    [SerializeField] public float DodgeRotationSpeed2 = 100;
    public float dodgeAnimationSpeed = 1;
    [Tooltip("How far into the first dodge's animation (0-1) Shift must be held before a press registers as a chained second dodge.")]
    [Range(0f, 1f)] public float secondDodgeWindowStart = 0.5f;

    [Header("Roll Dodge Timing (fraction of clip duration)")]
    [Range(0f, 1f)] public float rollDodgeMoveStart = 0.1f;
    [Range(0f, 1f)] public float rollDodgeMoveEnd = 0.9f;
    [Range(0f, 1f)] public float rollDodgeExitAt = 0.91f;
    [Tooltip("Horizontal dash force for this dodge (see RollDodgeState).")]
    public float rollDodgeForce = 6f;

    [Header("Second Roll Dodge Timing (fraction of clip duration)")]
    [Range(0f, 1f)] public float secondRollDodgeMoveStart = 0.1f;
    [Range(0f, 1f)] public float secondRollDodgeMoveEnd = 0.9f;
    [Range(0f, 1f)] public float secondRollDodgeExitAt = 0.91f;
    [Tooltip("Horizontal dash force for this dodge (see SecondRollDodgeState).")]
    public float secondRollDodgeForce = 6f;
    [Tooltip("Upward velocity applied when the second roll dodge starts moving, arcing the player over obstacles.")]
    public float secondRollDodgeVaultVelocity = 6f;

    [Header("Backflip Dodge Timing (fraction of clip duration)")]
    [Range(0f, 1f)] public float backflipDodgeMoveStart = 0.2f;
    [Range(0f, 1f)] public float backflipDodgeMoveEnd = 0.7f;
    [Range(0f, 1f)] public float backflipDodgeExitAt = 0.75f;
    [Tooltip("Horizontal dash force for this dodge (see BackflipDodgeState).")]
    public float backflipDodgeForce = 6f;
    [Tooltip("Upward velocity applied when the backflip dodge starts moving, arcing the player over obstacles.")]
    public float backflipDodgeVaultVelocity = 6f;

    [Header("Second Backflip Dodge Timing (fraction of clip duration)")]
    [Range(0f, 1f)] public float secondBackflipDodgeMoveStart = 0.2f;
    [Range(0f, 1f)] public float secondBackflipDodgeMoveEnd = 0.7f;
    [Range(0f, 1f)] public float secondBackflipDodgeExitAt = 0.75f;
    [Tooltip("Horizontal dash force for this dodge (see SecondBackflipDodgeState).")]
    public float secondBackflipDodgeForce = 6f;
    [Tooltip("Upward velocity applied when the second backflip dodge starts moving, arcing the player over obstacles.")]
    public float secondBackflipDodgeVaultVelocity = 6f;

    [Header("Dodge Controller Size")]
    [Tooltip("CharacterController height while any dodge state is active, letting the player fit through low gaps.")]
    public float dodgeControllerHeight = 0.5f;

    [Header("Falling / Landing Settings")]
    [Tooltip("How far (meters) the ground must be below the player's feet before LocomotionState commits to FallingState. CharacterController.isGrounded alone flickers false on stairs/edges regardless of actual drop height, so this measures the real distance with a raycast instead.")]
    public float fallHeightThreshold = 1f;
    [Tooltip("Which layers count as ground for the fall-height raycast.")]
    public LayerMask groundLayerMask = ~0;
    [Tooltip("How long (seconds) the ground must be continuously beyond fallHeightThreshold before committing to FallingState - guards against a single-frame raycast miss (e.g. a seam between floor meshes), not the primary gate.")]
    public float fallDetectionDelay = 0.1f;
    [Tooltip("Fraction of the Landing clip's length (0-1) before LandingState returns to locomotion.")]
    [Range(0f, 1f)] public float landingExitAt = 0.9f;

    private float _defaultControllerHeight;
    private Vector3 _defaultControllerCenter;

    [Header("Dodge Heavy Attack")]
    public bool canDodgeHeavy = true;
    public float dodgeHeavyWindow = 1f;
    private float dodgeHeavyWindowStartTime = -Mathf.Infinity;

    [Header("Mid-Air Attack Drift")]
    [Tooltip("Fixed horizontal speed a Q/DodgeHeavy mid-air attack launches at (see MidFallAttackState) - always this exact value, regardless of whatever momentum (dodge, fall, etc.) the player actually had the instant it was triggered.")]
    public float midAirAttackDriftSpeed = 8f;
    [Tooltip("How long (seconds) after landing a mid-air attack holds the forced close camera zoom before releasing it back to normal - not released the instant landing is detected.")]
    public float midAirAttackZoomReleaseDelay = 1f;


    public readonly int CombatStanceStateHash = Animator.StringToHash("CombatStance");
    public readonly int TransitionStateHash = Animator.StringToHash("Transition");
    public readonly int IsMovingHash = Animator.StringToHash("isMoving");
    public readonly int BackFlipDodgeHash = Animator.StringToHash("BackFlipDodge");
    public readonly int isDodgingHash = Animator.StringToHash("isDodging");
    public readonly int rollDodgeHash = Animator.StringToHash("RollDodge");
    public readonly int isRollDodgeHash = Animator.StringToHash("isRollDodge");
    public readonly int secondRollDodgeHash = Animator.StringToHash("2ndDodge");
    public readonly int isSecondRollDodgeHash = Animator.StringToHash("isSecondRollDodge");
    public readonly int secondBackFlipDodgeHash = Animator.StringToHash("2ndBackFlipDodge");
    public readonly int isSecondBackFlipDodgeHash = Animator.StringToHash("isSecondBackFlipDodge");
    public readonly int fallingHash = Animator.StringToHash("Falling");
    public readonly int isFallingHash = Animator.StringToHash("isFalling");
    public readonly int landingHash = Animator.StringToHash("Landing");
    public readonly int isLandingHash = Animator.StringToHash("isLanding");
    [HideInInspector] public int BlockLoopHash;

    public readonly int BaseLayer = 0;
    public readonly int AttackLayer = 1;
    public readonly int FullBodyLayer = 2;
    
    public int attackLayerIndex = 1;

    // Cache states for easy switching and zero garbage allocation
    public LocomotionState locomotionState { get; private set; }
    public BlockState blockState { get; private set; } // Cached reference


    void Start()
    {
        mover = GetComponent<Mover>();
        rotator = GetComponent<PlayerRotate>();
        anim = GetComponent<Animator>();
        playerToMouse = GetComponent<PlayerToMouse>();
        abilityManager = GetComponent<AbilityManager>();
        playerMovement = GetComponent<PlayerMovement>();
        stats = GetComponent<Stats>();
        playerEnergy = GetComponent<PlayerEnergy>();
        characterController = GetComponent<CharacterController>();
        // Lives on the camera, not this GameObject - CameraFollow itself already resolves the scene
        // camera the same way (Camera.main) inside its own CamFollow(), so this stays consistent.
        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();

        BlockLoopHash = Animator.StringToHash(blockLoopAnimationName);
        dodgeAnimationSpeed = stats.GetDodgeAnimationSpeed();

        InitializeAnimationCache();

        backFlipDodgeAnimationClip = GetClipByName("BackFlipDodge");
        rollDodgeAnimationClip = GetClipByName("RollDodge");
        secondRollDodgeAnimationClip = GetClipByName("2ndDodge");
        secondBackFlipDodgeAnimationClip = GetClipByName("2ndDodge");
        landingAnimationClip = GetClipByName("Landing");

        _defaultControllerHeight = characterController.height;
        _defaultControllerCenter = characterController.center;

        // FIXED: Both states are now properly instantiated and allocated once here
        locomotionState = new LocomotionState(this);
        blockState = new BlockState(this);

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
        if (anim == null || anim.runtimeAnimatorController == null) return;

        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
            if (!AnimationLengths.ContainsKey(clip.name))
            {
                AnimationLengths.Add(clip.name, clip.length);
                Debug.Log("Initialized animationClip: " + clip.name);
            }
        }
    }

    public AnimationClip GetClipByName(string clipName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return null;

        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName) return clip;
        }
        return null;
    }

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
        return currentState is BackflipDodgeState || currentState is RollDodgeState
            || currentState is SecondRollDodgeState || currentState is SecondBackflipDodgeState;
    }

    // Ground truth for input-gating (mid-air-only ability variants, blocking a fresh dodge while
    // airborne), instead of checking "is currentState FallingState". LocomotionState only switches
    // to FallingState after fallDetectionDelay has elapsed (a debounce against a single-frame raycast
    // miss), so a dodge/attack that exits back to LocomotionState while still airborne (e.g. mid-vault)
    // would otherwise read as grounded for up to that whole delay - letting the full ground combo/dodge
    // set fire mid-air. Uses the same raycast check LocomotionState itself uses, so it agrees with the
    // moment LocomotionState would eventually commit to FallingState, just without waiting for the debounce.
    public bool IsAirborne()
    {
        return !IsGroundedWithinDistance(fallHeightThreshold);
    }

    public IPlayerState GetCurrentState() => currentState;

    // Called from RollDodgeState/BackflipDodgeState.ExitState() to open the post-dodge heavy-attack window.
    public void StartDodgeHeavyWindow()
    {
        dodgeHeavyWindowStartTime = Time.time;
    }

    // One-shot: call after the window has been used to prevent it lingering for a second press.
    public void ConsumeDodgeHeavyWindow()
    {
        dodgeHeavyWindowStartTime = -Mathf.Infinity;
    }

    public bool IsInDodgeHeavyWindow()
    {
        return canDodgeHeavy && Time.time <= dodgeHeavyWindowStartTime + dodgeHeavyWindow;
    }

    // Shrinks the CharacterController's height, keeping its bottom fixed at the same world height
    // (moving center down by half the reduction) so the player's feet stay grounded while the
    // capsule's top comes down - lets a dodge fit through low gaps instead of hovering/sinking.
    public void ShrinkController(float newHeight)
    {
        float delta = _defaultControllerHeight - newHeight;
        characterController.height = newHeight;
        characterController.center = _defaultControllerCenter - new Vector3(0f, delta * 0.5f, 0f);
    }

    public void RestoreControllerSize()
    {
        characterController.height = _defaultControllerHeight;
        characterController.center = _defaultControllerCenter;
    }

    // Measures the real distance from the player's feet to the ground below via raycast, rather
    // than trusting CharacterController.isGrounded - which flickers false on stairs and platform
    // edges regardless of how small the actual gap is. Returns true if ground is found within
    // maxDistance of the feet.
    public bool IsGroundedWithinDistance(float maxDistance)
    {
        Vector3 feetPosition = transform.position + characterController.center + Vector3.down * (characterController.height * 0.5f);
        Vector3 rayOrigin = feetPosition + Vector3.up * 0.1f;
        return Physics.Raycast(rayOrigin, Vector3.down, maxDistance + 0.1f, groundLayerMask);
    }
}
