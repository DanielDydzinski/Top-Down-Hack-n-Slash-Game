using UnityEngine;

// Entered instead of ActionState when Q or DodgeHeavy fires while the player is already falling
// (see PlayerInputHandler - only those two tracks are allowed mid-air, using the dedicated
// qMidAirAbilities/dodgeHeavyMidAirAbilities lists on ComboController). Those abilities' clips are
// already trimmed to start at the desired freeze pose, so this state doesn't need to seek or time
// anything within the clip - it just escapes Falling exactly the way RollDodgeState/BackflipDodgeState
// already do it (Play(Transition) on BOTH layers, unconditionally), lets the ability's own
// AttackState-int-driven Animator wiring route to the real state completely on its own (the same
// mechanism that already correctly plays every grounded cast), and freezes (animSpeed = 0) the moment
// it detects that routing has happened - frame 0 of the trimmed clip IS the freeze pose. Holds frozen
// until the ground is detected, then finishes playing the rest of the clip and exits like a normal cast.
public class MidFallAttackState : IPlayerState
{
    private PlayerStateMachine psm;
    private Ability ability;
    private int layerIndex;

    private const float ExitGracePeriod = 0.05f;

    private bool _firstUpdateSkipped;
    private bool _routed;
    private bool _isFrozen;
    private float _timeSinceLanded;
    private bool _zoomOverrideReleased;

    private float _driftSpeed;
    private bool _driftStopped;
    private Quaternion _targetMouseRotation;

    // Captured in EnterState() before anything freezes playback, so unfreezing restores whatever this
    // clip's actual speed was (some animations run at other-than-1x by default) instead of assuming 1.
    private float _originalAnimSpeed;

    public MidFallAttackState(PlayerStateMachine stateMachine, Ability abilityToRun)
    {
        psm = stateMachine;
        ability = abilityToRun;
        layerIndex = (ability.baseSettings.animLayer == AnimationLayer.FullBody) ? 2 : 1;
    }

    public void EnterState()
    {
        // Capture before anything (including the freeze below) can touch it.
        _originalAnimSpeed = psm.anim.speed;

        psm.rotator.StopAllRotation();
        psm.rotator.enabled = false;

        // Own movement entirely while airborne - PlayerMovement's WASD direction feeds into the same
        // single per-frame Mover.Move() as this state's own drift (see Mover.Update()), so leaving it
        // enabled would let player input steer on top of the controlled mid-air trajectory.
        psm.playerMovement.enabled = false;

        psm.abilityManager.SetMovementLock(true);

        // Exactly what RollDodgeState/BackflipDodgeState already do to force a clean reset before the
        // real animation takes over - Falling only ever plays on FullBodyLayer and has zero outgoing
        // transitions of its own, so this is the only way out of it. Both layers, unconditionally,
        // regardless of which one this specific ability's own animation ends up using.
        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);

        // Now let the ability's own AttackState-int-driven wiring take over completely on its own -
        // the exact same mechanism that already correctly plays every grounded cast. UpdateState below
        // just watches for it to finish routing, then freezes immediately (the clip already starts at
        // the freeze pose, no seeking or timing needed).
        psm.abilityManager.StartCastingAbility(ability, null);
        psm.mover.SetSpeedMultiplier(0f);

        // Force a close zoom for the whole mid-air attack (see CameraFollow.SetZoomOverride) - held
        // through landing and released psm.midAirAttackZoomReleaseDelay seconds later (see
        // UpdateState), not the instant landing is detected. ExitState() below still clears it
        // unconditionally as a safety net if this state is interrupted first.
        if (psm.cameraFollow != null) psm.cameraFollow.SetZoomOverride();

        // Fixed launch speed, not a carry-over of whatever momentum the player actually had (dodge push,
        // vault, normal fall, etc. all varied - see psm.midAirAttackDriftSpeed). Direction still comes
        // from transform.forward every frame in UpdateState, so steering still works; only the magnitude
        // is now constant so the attack feels the same regardless of what preceded it.
        _driftSpeed = psm.midAirAttackDriftSpeed;

        if (ability.baseSettings.abilityControllerHeight > 0f)
            psm.ShrinkController(ability.baseSettings.abilityControllerHeight);

        _firstUpdateSkipped = false;
        _routed = false;
        _isFrozen = false;
        _timeSinceLanded = 0f;
        _driftStopped = false;
        _zoomOverrideReleased = false;
    }

    public void UpdateState()
    {
        // Steering: bends the carried-over drift toward the mouse the whole time we're airborne
        // (including while frozen - that's exactly when you have time to aim), the same rotationOomph
        // "drag" feel HandleDashMovement already gives the dash itself on the ground.
        if (ability.baseSettings.canRotateDuringCast)
        {
            UpdateTargetRotation();
            ApplySteering();
        }

        if (!_driftStopped)
        {
            psm.mover.SetHorizontalVelocity(psm.transform.forward * _driftSpeed);
        }

        if (_isFrozen)
        {
            // Raycast-based check, not characterController.isGrounded - the latter can misreport
            // grounded=true for a frame (e.g. right after EnterState()'s ShrinkController resize),
            // which was clearing the zoom override almost immediately instead of holding it through
            // the actual fall.
            if (!psm.IsGroundedWithinDistance(psm.fallHeightThreshold)) return;

            _isFrozen = false;
            psm.anim.speed = _originalAnimSpeed;

            if (ability.baseSettings.stopDashOnGrounded)
            {
                psm.mover.SetHorizontalVelocity(Vector3.zero);
                _driftStopped = true;
            }
        }

        if (!_routed)
        {
            // Skip exactly one frame first: EnterState()'s Play(TransitionStateHash, ...) calls are
            // only actually applied by Unity during its Animator evaluation step, which runs after all
            // scripts' Update() for that frame - not synchronously when Play() is called. Reading
            // GetCurrentAnimatorStateInfo immediately would still see last frame's state (e.g. Falling
            // on FullBodyLayer) and wrongly conclude we'd already routed off Transition.
            if (!_firstUpdateSkipped)
            {
                _firstUpdateSkipped = true;
                return;
            }

            AnimatorStateInfo info = psm.anim.GetCurrentAnimatorStateInfo(layerIndex);

            // AttackState's own wiring hasn't routed us off "Transition" into the real ability state
            // yet - nothing to do until it has.
            if (info.shortNameHash == psm.TransitionStateHash) return;

            // Routed - the clip already starts at the freeze pose, so freeze right here, no seeking or
            // waiting for any particular normalized time. Same raycast-based check as above - this is
            // exactly the frame right after ShrinkController() ran in EnterState(), where
            // characterController.isGrounded is most likely to falsely read true.
            _routed = true;
            if (!psm.IsGroundedWithinDistance(psm.fallHeightThreshold))
            {
                _isFrozen = true;
                psm.anim.speed = 0f;
                return;
            }
            // Already grounded by the time we routed - never freezes, so the unfreeze branch above
            // never runs either. Fall through to the exit check.
        }

        // Landed (or grounded before ever needing to freeze): let the rest of the clip play out, then
        // exit exactly like ActionState does. _timeSinceLanded only starts advancing once we reach
        // here, so it doubles as "time since landing" for the zoom-release delay below.
        _timeSinceLanded += Time.deltaTime;

        // Held for a beat after landing rather than released the instant landing is detected - see
        // psm.midAirAttackZoomReleaseDelay. Guarded so it only fires once; ExitState() still clears it
        // unconditionally as a safety net if this state is interrupted before the delay elapses.
        if (!_zoomOverrideReleased && _timeSinceLanded >= psm.midAirAttackZoomReleaseDelay)
        {
            if (psm.cameraFollow != null) psm.cameraFollow.ClearZoomOverride();
            _zoomOverrideReleased = true;
        }

        if (_timeSinceLanded < ExitGracePeriod) return;
        if (psm.anim.GetInteger(ActionState.attackStateHash) == -1) psm.SwitchState(new LocomotionState(psm));
    }

    private void UpdateTargetRotation()
    {
        Vector3 currentMouseDir = psm.playerToMouse.playerToMouseDir;
        if (currentMouseDir != Vector3.zero)
        {
            currentMouseDir.y = 0;
            _targetMouseRotation = Quaternion.LookRotation(currentMouseDir.normalized);
        }
    }

    private void ApplySteering()
    {
        psm.transform.rotation = Quaternion.RotateTowards(
            psm.transform.rotation,
            _targetMouseRotation,
            ability.baseSettings.rotationOomph * Time.deltaTime
        );
    }

    public void ExitState()
    {
        psm.mover.enabled = true;
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.rotator.UpdateOrientation();

        // Runs whether this state ends normally (landed) or is interrupted (e.g. hit/stagger) -
        // either way the forced close zoom shouldn't outlive the mid-air attack.
        if (psm.cameraFollow != null) psm.cameraFollow.ClearZoomOverride();

        psm.abilityManager.SetMovementLock(false);
        psm.abilityManager.CancelAbility();
        psm.anim.SetBool(psm.IsMovingHash, true);
        psm.anim.SetInteger(ActionState.attackStateHash, -1);
        psm.RestoreControllerSize();

        psm.mover.SetHorizontalVelocity(Vector3.zero);

        // Safety net: if interrupted (e.g. hit/stagger) before landing, don't leave the Animator
        // permanently paused for whatever state comes next.
        if (_isFrozen) psm.anim.speed = _originalAnimSpeed;
    }
}
