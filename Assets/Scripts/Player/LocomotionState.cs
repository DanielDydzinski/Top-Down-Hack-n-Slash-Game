using Unity;
using UnityEngine;

public class LocomotionState : IPlayerState
{
    private PlayerStateMachine psm;
    public static readonly int dotPHash = Animator.StringToHash("MoveDirMouseDirDotP");
    public static readonly int isMovingHash = Animator.StringToHash("isMoving");
    public static readonly int runBlendHash = Animator.StringToHash("RunBlend");

    private float _ungroundedTimer;

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
    }

    public void UpdateState()
    {
        // Checked every frame (not just on enter) so walking off a ledge mid-locomotion is caught,
        // not just re-entering this state while already off the ground. Uses a raycast-measured
        // distance to the ground rather than CharacterController.isGrounded, which flickers false
        // on stairs/platform edges regardless of how small the actual drop is - the sustained
        // delay on top of that only guards against a single-frame raycast miss.
        bool looseGrounded = psm.IsGroundedWithinDistance(psm.fallHeightThreshold);

        if (!looseGrounded)
        {
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

            // Safety net: a dodge that chained into an airborne vault leaves playerMovement disabled
            // on exit (see RollDodgeState.ExitState etc.) until grounded, so held WASD can't stack on
            // top of the carried drift. Re-enable it here once we're actually back on the ground.
            if (psm.playerMovement != null && !psm.playerMovement.enabled) psm.playerMovement.enabled = true;

            // Locomotion is the terminal owner of "this drift is done" the moment it's actually
            // grounded - it never itself feeds Mover.SetHorizontalVelocity (WASD goes through the
            // separate moveDirection path), so any nonzero value here is a carryover from whatever
            // put us in this state. Normally FallingState.ExitState() is what clears carried momentum
            // on landing, but a dodge/vault whose exit-timer ends while technically still airborne
            // (per IsGroundedWithinDistance) can land for real before LocomotionState's own
            // fallDetectionDelay-gated check ever gets a chance to hand off to FallingState - so
            // FallingState never runs and never clears it. Without this, that drift silently keeps
            // pushing the player every frame until an unrelated state (e.g. the next dodge) happens
            // to overwrite it.
            Vector3 leftoverDrift = psm.mover.GetHorizontalDrift();
            if (leftoverDrift.magnitude > 0.01f) psm.mover.SetHorizontalVelocity(Vector3.zero);
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