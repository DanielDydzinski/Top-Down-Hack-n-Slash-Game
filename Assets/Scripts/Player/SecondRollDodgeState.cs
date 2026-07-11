using UnityEngine;

// A chained follow-up to RollDodgeState, entered when Shift is pressed again during the first
// roll dodge (see RollDodgeState.UpdateState). Behaves the same way but does not itself chain
// further - always returns to locomotion.
public class SecondRollDodgeState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration;
    private float _timer;
    private float _dodgeForce;
    private Vector3 _leapDirection;

    private Quaternion _targetMouseRotation;
    private bool _canSteer;
    private bool _vaultImpulseApplied;

    // See RollDodgeState for why this exists and why it's fed via Mover.SetHorizontalVelocity
    // using the dodge's own known speed instead of a CharacterController.velocity read-back.
    private bool _driftActive;

    public SecondRollDodgeState(PlayerStateMachine _psm, Vector3 dodgeDirection)
    {
        psm = _psm;
        _leapDirection = dodgeDirection;
    }

    public void EnterState()
    {
        psm.rotator.UpdateOrientation();

        psm.gameObject.layer = LayerMask.NameToLayer("Default");
        _timer = 0;
        _driftActive = false;
        // Defensive: this can be entered mid-air off the first dodge's drift feed - clear it so it
        // can't stack with this dodge's own movement.
        psm.mover.SetHorizontalVelocity(Vector3.zero);

        Vector3 dodgeDirNorm = _leapDirection.normalized;
        Vector3 facingDirNorm = psm.rotator.facingDirVec3.normalized;

        float dot = Vector3.Dot(dodgeDirNorm, facingDirNorm);
        _canSteer = (dot > 0.707f);

        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.CrossFade(psm.secondRollDodgeHash, 0.15f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isSecondRollDodgeHash);

        float playbackSpeed = psm.dodgeAnimationSpeed;
        _duration = psm.secondRollDodgeAnimationClip.length / playbackSpeed;
        _dodgeForce = psm.secondRollDodgeForce;

        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;

        psm.ShrinkController(psm.dodgeControllerHeight);
        _vaultImpulseApplied = false;

        if (_leapDirection != Vector3.zero)
            psm.transform.rotation = Quaternion.LookRotation(_leapDirection);
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        // See RollDodgeState - uses the raycast-based ground check, not CharacterController.isGrounded
        // (flickers false at ledge edges, which was leaking drift into locomotion).
        if (_driftActive && psm.IsGroundedWithinDistance(psm.fallHeightThreshold))
        {
            psm.mover.SetHorizontalVelocity(Vector3.zero);
            _driftActive = false;
        }

        if (_canSteer)
        {
            UpdateTargetRotation();
            ApplySteering();
        }

        if (_timer >= _duration * psm.secondRollDodgeMoveStart && _timer <= _duration * psm.secondRollDodgeMoveEnd)
        {
            // One-shot: fires the jump exactly as the dash starts, not the instant the state is entered.
            if (!_vaultImpulseApplied)
            {
                psm.mover.SetVerticalVelocity(psm.secondRollDodgeVaultVelocity);
                _vaultImpulseApplied = true;
            }

            _driftActive = true;
        }

        if (_driftActive)
        {
            psm.mover.SetHorizontalVelocity(psm.transform.forward * _dodgeForce);
        }

        if (_timer >= _duration * psm.secondRollDodgeExitAt)
        {
            psm.SwitchState(psm.locomotionState);
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
        psm.transform.rotation = Quaternion.RotateTowards(
            psm.transform.rotation,
            _targetMouseRotation,
            psm.DodgeRotationSpeed2 * Time.deltaTime
        );
    }

    public void ExitState()
    {
        // See RollDodgeState.ExitState - only hand WASD control back immediately if grounded, so held
        // input can't stack additively on top of the vault/drift velocity still carried in Mover while
        // airborne. LocomotionState re-enables it once actually grounded.
        if (psm.playerMovement && psm.IsGroundedWithinDistance(psm.fallHeightThreshold)) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.rotator.UpdateOrientation();
        psm.anim.CrossFade(psm.TransitionStateHash, 0.1f, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isSecondRollDodgeHash);
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
