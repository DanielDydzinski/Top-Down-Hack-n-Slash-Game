using UnityEngine;

public class PlayerStunState : IPlayerState
{
    private PlayerStateMachine psm;
    private float _duration;
    private float _timer;

    private static readonly int isMovingHash = Animator.StringToHash("isMoving");

    public PlayerStunState(PlayerStateMachine _psm, float duration)
    {
        psm = _psm;
        _duration = duration;
    }

    public void EnterState()
    {
        _timer = 0;
        psm.rotator.enabled = false;

        // ADD THIS: You must disable the script that handles WASD!
        // If your movement script is called 'PlayerMovement', do this:
        if (psm.playerMovement) psm.playerMovement.enabled = false;


        // This stops any pending Animation Events from firing
        psm.anim.Play(psm.TransitionStateHash, psm.AttackLayer);
        psm.anim.Play(psm.TransitionStateHash, psm.FullBodyLayer);
        psm.anim.Play(psm.CombatStanceStateHash,0);

        psm.anim.SetBool(isMovingHash, false);

        UnityEngine.Debug.Log("Disabled mover from StunState!!!!!!!!!!!!!!!!");
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        Debug.Log("PLAYER IS STUNNED");
        if (_timer >= _duration)
        {
            psm.SwitchState(psm.locomotionState);
        }
    }

    public void ExitState()
    {
        psm.rotator.enabled = true;

        // Re-enable it here
        if (psm.playerMovement)
        {
            psm.playerMovement.enabled = true;
            UnityEngine.Debug.Log("Reanabled Mover Exiting Stun State ##############");
        }


    }
}