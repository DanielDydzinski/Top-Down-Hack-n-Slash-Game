using Unity;
using UnityEngine;

public class LocomotionState : IPlayerState
{
    private PlayerStateMachine psm;
    public static readonly int dotPHash = Animator.StringToHash("MoveDirMouseDirDotP");
    public static readonly int isMovingHash = Animator.StringToHash("isMoving");
    public static readonly int runBlendHash = Animator.StringToHash("RunBlend");

    private float _ungroundedTimer;
    private bool _loggedAirborneStart; // DEBUG - remove once fall-speed investigation is done

    public LocomotionState(PlayerStateMachine stateMachine)
    {
        psm = stateMachine;
    }

    public void EnterState()
    {
        // Ensure everything is turned back on when we return to moving
        psm.mover.enabled = true;
        psm.rotator.enabled = true;

        psm.mover.SetSpeedMultiplier(1.0f);

        // Reset animator parameters
        psm.anim.SetBool("isMoving", false);

        _ungroundedTimer = 0f;
        _loggedAirborneStart = false;
    }

    public void UpdateState()
    {
        // Checked every frame (not just on enter) so walking off a ledge mid-locomotion is caught,
        // not just re-entering this state while already off the ground. Uses a raycast-measured
        // distance to the ground rather than CharacterController.isGrounded, which flickers false
        // on stairs/platform edges regardless of how small the actual drop is - the sustained
        // delay on top of that only guards against a single-frame raycast miss.
        bool looseGrounded = psm.IsGroundedWithinDistance(psm.fallHeightThreshold);

        // DEBUG - remove once fall-speed investigation is done
        Debug.Log($"[GroundDebug] t={Time.time:F3} height={psm.transform.position.y:F2} strictGrounded={psm.characterController.isGrounded} looseGrounded={looseGrounded} ungroundedTimer={_ungroundedTimer:F3}");

        if (!looseGrounded)
        {
            // DEBUG - remove once fall-speed investigation is done
            if (!_loggedAirborneStart)
            {
                _loggedAirborneStart = true;
                bool inputHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
                Debug.Log($"[FallDebug] Left ground: height={psm.transform.position.y:F2} time={Time.time:F3} velocity={psm.characterController.velocity} speed={psm.characterController.velocity.magnitude:F2} inputHeld={inputHeld}");
            }

            _ungroundedTimer += Time.deltaTime;
            if (_ungroundedTimer >= psm.fallDetectionDelay)
            {
                psm.SwitchState(new FallingState(psm));
                return;
            }
        }
        else
        {
            _ungroundedTimer = 0f;
            _loggedAirborneStart = false;

            // Safety net: a dodge that chained into an airborne vault leaves playerMovement disabled
            // on exit (see RollDodgeState.ExitState etc.) until grounded, so held WASD can't stack on
            // top of the carried drift. Re-enable it here once we're actually back on the ground.
            if (psm.playerMovement != null && !psm.playerMovement.enabled) psm.playerMovement.enabled = true;
        }

        UpdateAnimator();
        // Here, we could listen for inputs to switch to an AttackState.
        // But for now, your existing PlayerMovement script handles the WASD.
        // We just stay in this state until an ability is triggered.
    }

    public void ExitState()
    {
        // Clean up or stop sounds if necessary
    }

    private void UpdateAnimator()
    {
        Vector3 playerToMouseDir = psm.playerToMouse.playerToMouseDir.normalized;
        float dotP = Vector3.Dot(psm.mover.GetDirection(), playerToMouseDir);
        psm.anim.SetFloat(dotPHash, dotP);

        if (psm.mover.GetDirection() == Vector3.zero)
        {
            psm.anim.SetBool(isMovingHash, false);
        }
        else
        {
            psm.anim.SetBool(isMovingHash, true);

            if (dotP > 0.707f)
            {
                psm.anim.SetFloat(runBlendHash, 1.0f);
            }
            else if (dotP < -0.707)
            {
                psm.anim.SetFloat(runBlendHash, -1.0f);
            }
        }
    }
}