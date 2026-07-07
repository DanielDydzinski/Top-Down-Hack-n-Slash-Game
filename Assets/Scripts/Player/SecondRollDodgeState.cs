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
        _dodgeForce = psm.stats.dodgePower;

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

            psm.mover.GetComponent<CharacterController>().Move(psm.transform.forward * _dodgeForce * Time.deltaTime);
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
            psm.dodgeRotationSpeed * Time.deltaTime
        );
    }

    public void ExitState()
    {
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.rotator.UpdateOrientation();
        psm.anim.CrossFade(psm.TransitionStateHash, 0.1f, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isSecondRollDodgeHash);
        psm.gameObject.layer = LayerMask.NameToLayer("Player");
        psm.RestoreControllerSize();
        psm.StartDodgeHeavyWindow();
    }
}
