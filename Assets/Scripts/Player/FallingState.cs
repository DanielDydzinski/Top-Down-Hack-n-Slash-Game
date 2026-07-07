using UnityEngine;

// Entered from LocomotionState once the CharacterController has been continuously ungrounded
// for psm.fallDetectionDelay (see LocomotionState.UpdateState). Movement is fully locked - gravity
// itself is still applied every frame by Mover (independent of player state), this state just
// plays the falling loop and watches for the ground to reappear.
public class FallingState : IPlayerState
{
    private PlayerStateMachine psm;

    public FallingState(PlayerStateMachine _psm)
    {
        psm = _psm;
    }

    public void EnterState()
    {
        // Preserve whatever horizontal momentum we had the instant the fall started (e.g. running
        // off a ledge) instead of freezing in place and dropping straight down. CharacterController
        // already tracks this automatically from its own Move() calls - no separate estimator needed.
        // Handed to Mover rather than Move()'d here directly, so gravity and drift combine into ONE
        // Move() call per frame - see SetHorizontalVelocity for why a second, separate, purely
        // horizontal Move() call breaks isGrounded.
        Vector3 currentVelocity = psm.characterController.velocity;
        psm.mover.SetHorizontalVelocity(currentVelocity);

        psm.playerMovement.enabled = false;
        psm.rotator.enabled = false;

        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.CrossFade(psm.fallingHash, 0.15f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isFallingHash);
    }

    public void UpdateState()
    {
        if (psm.characterController.isGrounded)
        {
            psm.SwitchState(new LandingState(psm));
        }
    }

    public void ExitState()
    {
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true;
        psm.anim.ResetTrigger(psm.isFallingHash);

        // Stop carrying fall drift into locomotion/landing.
        psm.mover.SetHorizontalVelocity(Vector3.zero);
    }
}
