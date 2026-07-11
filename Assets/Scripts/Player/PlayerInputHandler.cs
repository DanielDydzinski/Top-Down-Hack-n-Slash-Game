using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    private ComboController comboController;
    private PlayerStateMachine psm;

    [Header("Potion Belt")]
    [SerializeField] private PotionBeltSlotUI beltSlot1;
    [SerializeField] private PotionBeltSlotUI beltSlot2;
    [SerializeField] private PotionBeltSlotUI beltSlot3;
    [SerializeField] private PotionBeltSlotUI beltSlot4;

    void Start()
    {
        comboController = GetComponent<ComboController>();
        psm = GetComponent<PlayerStateMachine>();
    }

    void Update()
    {
        // Centralized defensive handler handles both block and dodge on Left Shift
        HandleDefensiveInput();

        // While actively blocking, a light-attack press fires a counter instead of the normal light combo.
        if (psm.GetCurrentState() is BlockState && Input.GetMouseButtonDown(0))
        {
            comboController.OnAbilityInput(ComboController.ComboTrackId.Counter, ref comboController.counterIndex, comboController.counterAbilities);
        }
        // Prevent attacking/casting if the player is actively blocking, or landing (no attacks allowed
        // until Landing hands back to locomotion - see LandingState.DodgeReady for why dodge isn't
        // gated here too).
        else if (!psm.IsDodging() && !(psm.GetCurrentState() is BlockState) && !psm.IsLanding())
        {
            // While airborne, only Heavy (mouse 1) and Q are allowed through - everything else used to
            // fire straight into the glitched mid-air animation. Both route into MidFallAttackState
            // (via startAirborne) instead of the normal ActionState. Uses psm.IsAirborne() (a physics
            // check) rather than "currentState is FallingState" - a dodge/attack that exits back to
            // LocomotionState while still airborne (e.g. mid-vault) would otherwise read as grounded
            // until LocomotionState's fallDetectionDelay elapses, letting the full ground combo set
            // (and, via HandleDefensiveInput below, even a fresh dodge) fire mid-air.
            // Also true for the last ~1m of an actual fall: psm.IsAirborne()'s raycast threshold
            // reads "grounded" there before CharacterController.isGrounded actually fires the
            // Falling->Landing transition, so "currentState is FallingState" is checked directly too -
            // otherwise an attack could still slip through FallingState itself (skipping Landing's
            // animator cleanup entirely) in that gap. See animator_falling_state_fix memory for the
            // FullBodyLayer-stuck-on-Falling bug this caused.
            bool isFalling = psm.IsAirborne() || psm.GetCurrentState() is FallingState;

            if (!isFalling && Input.GetKeyDown(KeyCode.Space))
            {
                comboController.OnAbilityInput(ComboController.ComboTrackId.Magic, ref comboController.magicCombosIndex, comboController.magicCombos);
            }
            else if (!isFalling && Input.GetMouseButtonDown(0))
            {
                comboController.OnAbilityInput(ComboController.ComboTrackId.Light, ref comboController.lightCombosIndex, comboController.lightCombos);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (isFalling)
                {
                    // Mid-air uses its own trimmed-clip variants (see MidFallAttackState) - not the
                    // normal dodgeHeavyAbilities list, and no post-dodge window to check.
                    if (comboController.dodgeHeavyMidAirAbilities.Count > 0)
                    {
                        comboController.OnAbilityInput(ComboController.ComboTrackId.DodgeHeavyMidAir, ref comboController.dodgeHeavyMidAirIndex, comboController.dodgeHeavyMidAirAbilities, true);
                    }
                }
                else if (psm.IsInDodgeHeavyWindow() && comboController.dodgeHeavyAbilities.Count > 0)
                {
                    psm.ConsumeDodgeHeavyWindow();
                    comboController.OnAbilityInput(ComboController.ComboTrackId.DodgeHeavy, ref comboController.dodgeHeavyIndex, comboController.dodgeHeavyAbilities);
                }
                else
                {
                    comboController.OnAbilityInput(ComboController.ComboTrackId.Heavy, ref comboController.heavyCombosIndex, comboController.heavyCombos);
                }
            }
            else if (!isFalling && Input.GetKeyDown(KeyCode.F))
            {
                comboController.OnAbilityInput(ComboController.ComboTrackId.F, ref comboController.fIndex, comboController.fAbilities);
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                if (isFalling)
                {
                    // Mid-air uses its own trimmed-clip variant (see MidFallAttackState), not qAbilities.
                    if (comboController.qMidAirAbilities.Count > 0)
                    {
                        comboController.OnAbilityInput(ComboController.ComboTrackId.QMidAir, ref comboController.qMidAirIndex, comboController.qMidAirAbilities, true);
                    }
                }
                else
                {
                    comboController.OnAbilityInput(ComboController.ComboTrackId.Q, ref comboController.qIndex, comboController.qAbilities);
                }
            }
            else if (!isFalling && Input.GetKeyDown(KeyCode.E))
            {
                comboController.OnAbilityInput(ComboController.ComboTrackId.E, ref comboController.eIndex, comboController.eAbilities);
            }
            else if (!isFalling && Input.GetKeyDown(KeyCode.R))
            {
                comboController.OnAbilityInput(ComboController.ComboTrackId.R, ref comboController.rIndex, comboController.rAbilities);
            }
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            if (psm.abilityManager.IsPerformingAction())
            {
                psm.abilityManager.CancelAbilityButtonUp();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) beltSlot1?.ConsumeFromInput();
        else if (Input.GetKeyDown(KeyCode.Alpha2)) beltSlot2?.ConsumeFromInput();
        else if (Input.GetKeyDown(KeyCode.Alpha3)) beltSlot3?.ConsumeFromInput();
        else if (Input.GetKeyDown(KeyCode.Alpha4)) beltSlot4?.ConsumeFromInput();
    }

    /// <summary>
    /// Manages both Dodge and Block contextually using the Left Shift key.
    /// </summary>
    /// <summary>
    /// Manages both Dodge and Block contextually using the Left Shift key.
    /// Holding Shift blocks when still. Pressing a direction while holding Shift (or tapping Shift while running) dodges.
    /// </summary>
    /// <summary>
    /// Manages both Dodge and Block contextually using the Left Shift key.
    /// Bypasses residual movement vectors by checking hardware input states directly.
    /// </summary>
    void HandleDefensiveInput()
    {
        // Global safety lockouts. See the isFalling comment above - psm.IsAirborne() catches the gap
        // where a dodge/attack has exited back to LocomotionState but hasn't actually landed yet, so a
        // fresh Shift+direction press can't chain into another dodge (or trigger block) mid-air.
        // currentState is FallingState closes the same tail-of-fall race the ability branch guards
        // against above - psm.IsAirborne() alone can already read "grounded" a moment before
        // FallingState actually hands off to Landing.
        if (psm.IsStunned() || psm.IsDodging() || psm.IsAirborne() || psm.GetCurrentState() is FallingState) return;

        // Landing itself allows a dodge (unlike attacks, which are fully blocked - see the ability
        // branch above), but only after psm.landingDodgeDelay has elapsed since touchdown, so a
        // landing-frame dodge doesn't feel instant/glitchy.
        if (psm.GetCurrentState() is LandingState landingState && !landingState.DodgeReady) return;

        // --- NEW: Check if the player is physically pressing WASD / Directional keys right now ---
        bool isTouchingDirectionKeys = Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f;

        // --- 1. DODGE EVALUATION ---
        // Only allow a dodge if they are ACTUALLY pressing movement keys right now
        bool pressedShiftWhileMoving = Input.GetKeyDown(KeyCode.LeftShift) && isTouchingDirectionKeys;
        bool directionHitWhileBlocking = Input.GetKey(KeyCode.LeftShift) && psm.GetCurrentState() is BlockState && isTouchingDirectionKeys;

        if (pressedShiftWhileMoving || directionHitWhileBlocking)
        {
            if (psm.GetCurrentState() is ActionState) return;

            // Build the dodge direction straight from live hardware input instead of
            // psm.playerMovement.movingDirection: that field is a "last non-zero direction"
            // cache that PlayerMovement only overwrites while a WASD key is actually held, and
            // is never reset to zero when you let go. While standing still holding block it
            // stays frozen at whatever direction you last moved in *before* you started
            // blocking, and since this check fires the same frame a new direction key goes
            // down (via GetKey, no debounce), it can read that stale value ahead of
            // PlayerMovement's own Update() refreshing it this frame. isTouchingDirectionKeys
            // (above) already guarantees these raw axes are non-zero here.
            Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

            Vector3 faceDir = psm.rotator.facingDirVec3;
            float dot = Vector3.Dot(moveDir, faceDir.normalized);

            if (dot < -0.5f)
            {
                psm.SwitchState(new BackflipDodgeState(psm, moveDir));
            }
            else
            {
                psm.SwitchState(new RollDodgeState(psm, moveDir));
            }
            return; // Exit early so we don't process blocking code this frame
        }

        // --- 2. HOLD-TO-BLOCK EVALUATION ---
        // If they are holding Left Shift and NOT pressing any direction keys -> Instantly Block!
        if (Input.GetKey(KeyCode.LeftShift) && !isTouchingDirectionKeys)
        {
            if (psm.GetCurrentState() is LocomotionState)
            {
                psm.SwitchState(psm.blockState);
            }
        }
        // If they let go of Shift while in the block state -> Lower shield
        else if (!Input.GetKey(KeyCode.LeftShift) && psm.GetCurrentState() is BlockState)
        {
            psm.SwitchState(psm.locomotionState);
        }
    }
}