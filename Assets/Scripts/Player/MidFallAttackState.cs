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

    private float _driftSpeed;
    private bool _driftStopped;
    private Quaternion _targetMouseRotation;

    public MidFallAttackState(PlayerStateMachine stateMachine, Ability abilityToRun)
    {
        psm = stateMachine;
        ability = abilityToRun;
        layerIndex = (ability.baseSettings.animLayer == AnimationLayer.FullBody) ? 2 : 1;
    }

    public void EnterState()
    {
        psm.rotator.StopAllRotation();
        psm.rotator.enabled = false;

        // Own movement entirely while airborne - PlayerMovement.Update() calls its own Mover.Move()
        // whenever a direction key is held, a second CharacterController.Move() in the same frame as
        // Mover's gravity Move(), which makes isGrounded unreliable (see Mover.SetHorizontalVelocity).
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

        // Carry over whatever horizontal momentum the fall had. characterController.velocity reflects
        // the last actual Move() delta, so this is still accurate even though FallingState.ExitState()
        // already zeroed Mover's own internal horizontal-velocity bookkeeping this same frame.
        Vector3 horizontalVelocity = psm.characterController.velocity;
        horizontalVelocity.y = 0f;
        _driftSpeed = horizontalVelocity.magnitude;

        if (ability.baseSettings.abilityControllerHeight > 0f)
            psm.ShrinkController(ability.baseSettings.abilityControllerHeight);

        _firstUpdateSkipped = false;
        _routed = false;
        _isFrozen = false;
        _timeSinceLanded = 0f;
        _driftStopped = false;
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
            if (!psm.characterController.isGrounded) return;

            _isFrozen = false;
            psm.anim.speed = 1f;

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
            // waiting for any particular normalized time.
            _routed = true;
            if (!psm.characterController.isGrounded)
            {
                _isFrozen = true;
                psm.anim.speed = 0f;
                return;
            }
            // Already grounded by the time we routed - fall through to the exit check.
        }

        // Landed (or grounded before ever needing to freeze): let the rest of the clip play out, then
        // exit exactly like ActionState does.
        _timeSinceLanded += Time.deltaTime;
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

        psm.abilityManager.SetMovementLock(false);
        psm.abilityManager.CancelAbility();
        psm.anim.SetBool(psm.IsMovingHash, true);
        psm.anim.SetInteger(ActionState.attackStateHash, -1);
        psm.RestoreControllerSize();

        psm.mover.SetHorizontalVelocity(Vector3.zero);

        // Safety net: if interrupted (e.g. hit/stagger) before landing, don't leave the Animator
        // permanently paused for whatever state comes next.
        if (_isFrozen) psm.anim.speed = 1f;
    }
}
