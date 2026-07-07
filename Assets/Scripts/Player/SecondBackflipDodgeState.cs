using UnityEngine;

// A chained follow-up to BackflipDodgeState, entered when Shift is pressed again during the
// first backflip dodge (see BackflipDodgeState.UpdateState). Behaves the same way but does not
// itself chain further - always returns to locomotion.
public class SecondBackflipDodgeState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration = 0.4f;
    private float _timer;
    private float _dodgeForce = 2;
    private Vector3 _leapDirection;
    private bool _vaultImpulseApplied;

    public SecondBackflipDodgeState(PlayerStateMachine _psm, Vector3 dodgeDirection)
    {
        psm = _psm;
        _leapDirection = dodgeDirection;
    }

    public void EnterState()
    {
        psm.gameObject.layer = LayerMask.NameToLayer("Default");

        _timer = 0;

        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);

        psm.anim.CrossFade(psm.secondBackFlipDodgeHash, 0.25f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isSecondBackFlipDodgeHash);

        float playbackSpeed = psm.dodgeAnimationSpeed;

        _duration = psm.secondBackFlipDodgeAnimationClip.length / playbackSpeed;
        _dodgeForce = psm.stats.dodgePower;

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

        if (_timer >= _duration * psm.secondBackflipDodgeMoveStart && _timer <= _duration * psm.secondBackflipDodgeMoveEnd)
        {
            // One-shot: fires the jump exactly as the dash starts, not the instant the state is entered.
            if (!_vaultImpulseApplied)
            {
                psm.mover.SetVerticalVelocity(psm.secondBackflipDodgeVaultVelocity);
                _vaultImpulseApplied = true;
            }

            psm.mover.GetComponent<CharacterController>().Move(_leapDirection * _dodgeForce * Time.deltaTime);
        }
        if (_timer >= _duration * psm.secondBackflipDodgeExitAt)
        {
            psm.SwitchState(psm.locomotionState);
        }
    }

    public void ExitState()
    {
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.rotator.UpdateOrientation();
        psm.anim.CrossFade(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isSecondBackFlipDodgeHash);
        psm.gameObject.layer = LayerMask.NameToLayer("Player");
        psm.RestoreControllerSize();
        psm.StartDodgeHeavyWindow();
    }
}
