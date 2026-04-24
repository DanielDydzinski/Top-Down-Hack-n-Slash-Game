using UnityEngine;

public class RollDodgeState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration;
    private float _timer;
    private float _dodgeForce;
    private Vector3 _leapDirection;

    public RollDodgeState(PlayerStateMachine _psm, Vector3 dodgeDirection)
    {
        psm = _psm;
        _leapDirection = dodgeDirection;
    }

    public void EnterState()
    {
        psm.gameObject.layer = LayerMask.NameToLayer("Default");
        _timer = 0;



        // Snap character to face the dodge direction
        if (_leapDirection != Vector3.zero)
            psm.mover.transform.rotation = Quaternion.LookRotation(_leapDirection);

        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);

        psm.anim.CrossFade(psm.rollDodgeHash, 0.15f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isRollDodgeHash);

        float playbackSpeed = psm.dodgeAnimationSpeed;
        _duration = psm.rollDodgeAnimationClip.length / playbackSpeed;
        _dodgeForce = psm.stats.dodgePower;

        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;

        Debug.Log($"RollDodge | clip: {psm.rollDodgeAnimationClip?.name} | length: {psm.rollDodgeAnimationClip?.length} | speed: {playbackSpeed} | final duration: {_duration}");
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        if (_timer >= _duration * 0.1f && _timer <= _duration * 0.9f)
        {
            psm.mover.GetComponent<CharacterController>().Move(_leapDirection * _dodgeForce * Time.deltaTime);
        }

        if (_timer >= _duration * 0.91f)
        {
            psm.SwitchState(psm.locomotionState);
        }
    }

    public void ExitState()
    {
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.anim.CrossFade(psm.TransitionStateHash, 0.1f, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isRollDodgeHash);
        psm.gameObject.layer = LayerMask.NameToLayer("Player");
    }
}