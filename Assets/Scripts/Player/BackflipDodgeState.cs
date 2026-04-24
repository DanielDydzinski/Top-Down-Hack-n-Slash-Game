using UnityEngine;

public class BackflipDodgeState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration = 0.4f; // Adjust to your animation length
    private float _timer;
    private float _dodgeForce = 2;
    private Vector3 _leapDirection;

 
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

       
        // This stops any pending Animation Events from firing
        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);

        // 2. PLAY THE DODGE: Since it's a full body move, play it on the Full Body layer
        psm.anim.CrossFade(psm.BackFlipDodgeHash, 0.25f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isDodgingHash);


        float playbackSpeed = psm.dodgeAnimationSpeed;

        _duration = psm.backFlipDodgeAnimationClip.length/ playbackSpeed;
       _dodgeForce=psm.stats.dodgePower;

        //psm.mover.AddForce(leapDir, _dodgeForce);

        // 4. LOCKDOWN
        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        

        if (_timer >= _duration * 0.2f && _timer <= _duration * 0.7f)
        {
            psm.mover.GetComponent<CharacterController>().Move(_leapDirection * _dodgeForce * Time.deltaTime);
        }
        if (_timer >= _duration * 0.75f)
        {
            psm.SwitchState(psm.locomotionState);
        }
    }

    public void ExitState()
    {
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.anim.CrossFade(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isDodgingHash);
        // Revert to Player layer
        psm.gameObject.layer = LayerMask.NameToLayer("Player");
    }
}