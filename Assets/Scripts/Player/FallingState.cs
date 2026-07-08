using UnityEngine;

// Entered from LocomotionState once the CharacterController has been continuously ungrounded
// for psm.fallDetectionDelay (see LocomotionState.UpdateState). Movement is fully locked - gravity
// itself is still applied every frame by Mover (independent of player state), this state just
// plays the falling loop and watches for the ground to reappear.
public class FallingState : IPlayerState
{
    private PlayerStateMachine psm;

    // DEBUG - remove once fall-speed investigation is done
    private Vector3 _startPosition;
    private float _startTime;

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

        // DEBUG - remove once fall-speed investigation is done
        _startPosition = psm.transform.position;
        _startTime = Time.time;
        Debug.Log($"[FallDebug] FallingState entered: height={_startPosition.y:F2} capturedVelocity={currentVelocity} speed={currentVelocity.magnitude:F2}");

        psm.playerMovement.enabled = false;
        //psm.rotator.enabled = false;

        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.CrossFade(psm.fallingHash, 0.15f, psm.FullBodyLayer);
        psm.anim.SetTrigger(psm.isFallingHash);
    }

    public void UpdateState()
    {
        // DEBUG - remove once fall-speed investigation is done
        Vector3 vel = psm.characterController.velocity;
        Debug.Log($"[FallDebug] t={Time.time - _startTime:F3} height={psm.transform.position.y:F2} vel={vel} speed={vel.magnitude:F2}");

        if (psm.characterController.isGrounded)
        {
            // DEBUG - remove once fall-speed investigation is done
            float duration = Time.time - _startTime;
            Vector3 delta = psm.transform.position - _startPosition;
            float heightDropped = -delta.y;
            float horizontalDistance = new Vector3(delta.x, 0f, delta.z).magnitude;
            float avgVerticalSpeed = heightDropped / Mathf.Max(duration, 0.0001f);
            Debug.Log($"[FallDebug] LANDED: duration={duration:F3}s heightDropped={heightDropped:F2} horizontalDistance={horizontalDistance:F2} avgVerticalSpeed={avgVerticalSpeed:F2}");

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
