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
        _dodgeForce = psm.stats.dodgePower;

        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;

        // Snap character to initial face direction
        if (_leapDirection != Vector3.zero)
            psm.transform.rotation = Quaternion.LookRotation(_leapDirection);
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        // 1. CONDITIONAL STEERING
        // Only runs if the lock was set to true during EnterState
        if (_canSteer)
        {
            UpdateTargetRotation();
            ApplySteering();
        }

        // 2. MOVEMENT 
        // We use psm.transform.forward because it updates as we steer, allowing for curves.
        if (_timer >= _duration * 0.1f && _timer <= _duration * 0.9f)
        {
            psm.mover.GetComponent<CharacterController>().Move(psm.transform.forward * _dodgeForce * Time.deltaTime);
        }

        // 3. EXIT
        if (_timer >= _duration * 0.91f)
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
        // Rotate towards the mouse using the dodge-specific rotation speed
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
        psm.rotator.UpdateOrientation(); // Recalculate facing dir after dodge is done
        psm.anim.CrossFade(psm.TransitionStateHash, 0.1f, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isRollDodgeHash);
        psm.gameObject.layer = LayerMask.NameToLayer("Player");
        psm.StartDodgeHeavyWindow();
    }
}
