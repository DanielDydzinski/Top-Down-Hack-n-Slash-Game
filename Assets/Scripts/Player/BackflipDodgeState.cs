using UnityEngine;

public class BackflipDodgeState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration = 0.4f; // Adjust to your animation length
    private float _timer;
    private float _dodgeForce = 2;
    private Vector3 _leapDirection;
    private bool _secondDodgeQueued;
    private bool _vaultImpulseApplied;

    // See RollDodgeState for why this exists and why it's fed via Mover.SetHorizontalVelocity
    // using the dodge's own known speed instead of a CharacterController.velocity read-back.
    private bool _driftActive;


    public BackflipDodgeState(PlayerStateMachine _psm, Vector3 dodgeDirection)
    {
        psm = _psm;
        _leapDirection = dodgeDirection;
    }

    public void EnterState()
    {

        // Revert to Player layer
        psm.gameObject.layer = LayerMask.NameToLayer("Default");

        _timer = 0;
        _driftActive = false;
        // Defensive: clear any leftover drift feed from whatever state we came from, so it can't
        // stack with this dodge's own movement.
        psm.mover.SetHorizontalVelocity(Vector3.zero);


        // This stops any pending Animation Events from firing
        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);

        // 2. PLAY THE DODGE: Since it's a full body move, play it on the Full Body layer
        psm.anim.CrossFade(psm.BackFlipDodgeHash, 0.25f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isDodgingHash);


        float playbackSpeed = psm.dodgeAnimationSpeed;

        _duration = psm.backFlipDodgeAnimationClip.length/ playbackSpeed;
       _dodgeForce=psm.backflipDodgeForce;

        //psm.mover.AddForce(leapDir, _dodgeForce);

        // 4. LOCKDOWN
        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;
        psm.rotator.StopAllRotation();

        psm.ShrinkController(psm.dodgeControllerHeight);
        _vaultImpulseApplied = false;
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;
        psm.mover.transform.rotation = Quaternion.LookRotation(_leapDirection * -1f);

        // See RollDodgeState - uses the raycast-based ground check, not CharacterController.isGrounded
        // (flickers false at ledge edges, which was leaking drift into locomotion).
        if (_driftActive && psm.IsGroundedWithinDistance(psm.fallHeightThreshold))
        {
            psm.mover.SetHorizontalVelocity(Vector3.zero);
            _driftActive = false;
        }

        if (_timer >= _duration * psm.backflipDodgeMoveStart && _timer <= _duration * psm.backflipDodgeMoveEnd)
        {
            // One-shot: fires the jump exactly as the dash starts, not the instant the state is entered.
            if (!_vaultImpulseApplied)
            {
                psm.mover.SetVerticalVelocity(psm.backflipDodgeVaultVelocity);
                _vaultImpulseApplied = true;
            }

            _driftActive = true;
        }

        // Backflip moves along the fixed leap direction (not transform.forward - the character
        // faces the opposite way).
        if (_driftActive)
        {
            psm.mover.SetHorizontalVelocity(_leapDirection.normalized * _dodgeForce);
        }

        // CHAIN DETECTION: once past the configured window, a fresh Shift press queues a second dodge
        if (!_secondDodgeQueued && _timer >= _duration * psm.secondDodgeWindowStart && Input.GetKeyDown(KeyCode.LeftShift))
        {
            _secondDodgeQueued = true;
        }

        if (_timer >= _duration * psm.backflipDodgeExitAt)
        {
            if (_secondDodgeQueued)
            {
                // Just hops backward again in the same direction as this one.
                psm.SwitchState(new SecondBackflipDodgeState(psm, _leapDirection));
            }
            else
            {
                psm.SwitchState(psm.locomotionState);
            }
        }
    }

    public void ExitState()
    {
        // See RollDodgeState.ExitState - only hand WASD control back immediately if grounded, so held
        // input can't stack additively on top of the vault/drift velocity still carried in Mover while
        // airborne. LocomotionState re-enables it once actually grounded.
        if (psm.playerMovement && psm.IsGroundedWithinDistance(psm.fallHeightThreshold)) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.rotator.UpdateOrientation();
        psm.anim.CrossFade(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isDodgingHash);
        // Revert to Player layer
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