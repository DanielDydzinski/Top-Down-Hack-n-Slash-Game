using UnityEngine;

public class RollDodgeState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration;
    private float _timer;
    private float _dodgeForce;
    private Vector3 _leapDirection;

    private Quaternion _targetMouseRotation;
    private bool _canSteer; // The lock that persists for the whole dodge
    private bool _secondDodgeQueued;

    // True while the dodge's own horizontal push is (or should still be) feeding Mover, so it
    // keeps being applied through the no-movement tail instead of dropping to zero the instant
    // this state stops calling Move() - see UpdateState. Fed via Mover.SetHorizontalVelocity using
    // the dodge's own known speed rather than read back from CharacterController.velocity, because
    // that read used to race Mover's own separate Move() call before movement was consolidated into
    // a single per-frame Move() in Mover.Update().
    private bool _driftActive;

    public RollDodgeState(PlayerStateMachine _psm, Vector3 dodgeDirection)
    {
        psm = _psm;
        _leapDirection = dodgeDirection;
    }

    public void EnterState()
    {
        // ensuring we have the most accurate data for the dodge direction.
        psm.rotator.UpdateOrientation();

        psm.gameObject.layer = LayerMask.NameToLayer("Default");
        _timer = 0;
        _driftActive = false;
        // Defensive: clear any leftover drift feed from whatever state we came from, so it can't
        // stack with this dodge's own movement.
        psm.mover.SetHorizontalVelocity(Vector3.zero);

        // 1. DETERMINE IF WE CAN STEER (The "Lock")
        // Compare dodge input direction against the current facing direction maintained by PlayerRotate.
        // If they are within 45 degrees (dot > 0.707), we allow steering.
        Vector3 dodgeDirNorm = _leapDirection.normalized;
        Vector3 facingDirNorm = psm.rotator.facingDirVec3.normalized;

        float dot = Vector3.Dot(dodgeDirNorm, facingDirNorm);
        _canSteer = (dot > 0.707f);

        // 2. ANIMATION & SETUP
        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.CrossFade(psm.rollDodgeHash, 0.15f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isRollDodgeHash);

        float playbackSpeed = psm.dodgeAnimationSpeed;
        _duration = psm.rollDodgeAnimationClip.length / playbackSpeed;
        _dodgeForce = psm.rollDodgeForce;

        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;

        psm.ShrinkController(psm.dodgeControllerHeight);

        // Snap character to initial face direction
        if (_leapDirection != Vector3.zero)
            psm.transform.rotation = Quaternion.LookRotation(_leapDirection);
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        // Stop carrying the dash drift the instant we touch ground - otherwise Mover keeps
        // sliding us forever, since its per-frame Move() folds verticalVelocity.x/z in every frame
        // regardless of grounded state. Uses the same raycast-based ground check as
        // LocomotionState rather than CharacterController.isGrounded, which flickers false right
        // at ledge edges - a missed clear here was leaking drift into locomotion, stacking with
        // normal WASD movement until it got captured (inflated) by the next real fall.
        if (_driftActive && psm.IsGroundedWithinDistance(psm.fallHeightThreshold))
        {
            psm.mover.SetHorizontalVelocity(Vector3.zero);
            _driftActive = false;
        }

        // 1. CONDITIONAL STEERING
        // Only runs if the lock was set to true during EnterState
        if (_canSteer)
        {
            UpdateTargetRotation();
            ApplySteering();
        }

        // 2. MOVEMENT - fed through Mover (not a raw controller.Move()) using the dodge's own known
        // speed, so it combines with gravity into ONE Move() call per frame (see Mover.Update()) and
        // keeps being applied through the no-movement tail below without needing to read it back off
        // CharacterController.velocity (that read used to race Mover's own Update(), which would
        // zero the horizontal component itself once nothing was feeding it).
        // We use psm.transform.forward because it updates as we steer, allowing for curves.
        if (_timer >= _duration * psm.rollDodgeMoveStart && _timer <= _duration * psm.rollDodgeMoveEnd)
        {
            _driftActive = true;
        }

        if (_driftActive)
        {
            psm.mover.SetHorizontalVelocity(psm.transform.forward * _dodgeForce);
        }

        // 2.5 CHAIN DETECTION: once past the configured window, a fresh Shift press queues a second dodge
        if (!_secondDodgeQueued && _timer >= _duration * psm.secondDodgeWindowStart && Input.GetKeyDown(KeyCode.LeftShift))
        {
            _secondDodgeQueued = true;
        }

        // 3. EXIT
        if (_timer >= _duration * psm.rollDodgeExitAt)
        {
            if (_secondDodgeQueued)
            {
                // Continues in whatever direction we're currently facing - steering during this
                // dodge may have already turned us towards the mouse.
                psm.SwitchState(new SecondRollDodgeState(psm, psm.transform.forward));
            }
            else
            {
                psm.SwitchState(psm.locomotionState);
            }
        }
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
        // Rotate towards the mouse using the dodge-specific rotation speed
        psm.transform.rotation = Quaternion.RotateTowards(
            psm.transform.rotation,
            _targetMouseRotation,
            psm.dodgeRotationSpeed * Time.deltaTime
        );
    }

    public void ExitState()
    {
        // Only hand WASD control back immediately if we're grounded. If this dodge chained into a
        // vault that's still airborne, re-enabling here would let held input stack additively on top
        // of the carried drift in Mover.verticalVelocity.x/z (see SetHorizontalVelocity) for the few
        // frames before LocomotionState detects ungrounded and switches to FallingState - inflating
        // the velocity FallingState.EnterState() captures. LocomotionState re-enables it once actually
        // grounded (safety net for landing before fallDetectionDelay would've triggered FallingState).
        if (psm.playerMovement && psm.IsGroundedWithinDistance(psm.fallHeightThreshold)) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.rotator.UpdateOrientation(); // Recalculate facing dir after dodge is done
        psm.anim.CrossFade(psm.TransitionStateHash, 0.1f, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isRollDodgeHash);
        psm.gameObject.layer = LayerMask.NameToLayer("Player");
        psm.RestoreControllerSize();
        psm.StartDodgeHeavyWindow();

        // Defensive, same as EnterState(): the exit-timer above can fire before UpdateState's own
        // grounded check catches up (landing on a ledge, where IsGroundedWithinDistance can lag a
        // frame or two behind the timer), leaving _driftActive true and this drift never cleared -
        // it then leaks straight into LocomotionState (which doesn't clear it either) and keeps
        // sliding the player indefinitely. Gated on actually being grounded (not just "still
        // active") - if this dodge chained into a vault that's still airborne when the timer ends,
        // this drift IS the carried momentum FallingState.EnterState() expects to inherit; clearing
        // it unconditionally here would erase that momentum before Falling ever gets to see it.
        if (_driftActive && psm.IsGroundedWithinDistance(psm.fallHeightThreshold))
        {
            psm.mover.SetHorizontalVelocity(Vector3.zero);
        }
    }
}
