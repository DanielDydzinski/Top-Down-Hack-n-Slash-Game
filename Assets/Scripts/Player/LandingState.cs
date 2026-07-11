using UnityEngine;

// Entered from FallingState once the ground is detected. Plays the landing animation for a
// fixed fraction of its clip length (psm.landingExitAt), matching the same timer-based exit
// pattern used by the dodge states, then returns to locomotion.
public class LandingState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration;
    private float _timer;

    // Attacks are blocked outright for the whole state (see PlayerInputHandler's IsLanding() check) -
    // dodging just gets a short delay instead, so a landing-frame dodge doesn't feel instant/glitchy.
    public bool DodgeReady => _timer >= psm.landingDodgeDelay;

    public LandingState(PlayerStateMachine _psm)
    {
        psm = _psm;
    }

    public void EnterState()
    {
        _timer = 0f;

        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;

        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.CrossFade(psm.landingHash, 0.1f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isLandingHash);

        _duration = psm.landingAnimationClip != null ? psm.landingAnimationClip.length : 0.3f;
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        if (_timer >= _duration * psm.landingExitAt)
        {
            psm.SwitchState(psm.locomotionState);
        }
    }

    public void ExitState()
    {
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.anim.CrossFade(psm.TransitionStateHash, 0.1f, psm.FullBodyLayer);
        psm.anim.ResetTrigger(psm.isLandingHash);
    }
}
