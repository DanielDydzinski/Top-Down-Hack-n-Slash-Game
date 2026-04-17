using UnityEngine;

public class StunState : IState
{
    private EnemyAIController _ctrl;
    private float _duration;
    private float _timer;



    public StunState(EnemyAIController controller) => _ctrl = controller;

    // We allow the state to be initialized with a specific duration
    public void SetDuration(float duration) => _duration = duration;
    public float RemainingTime => _duration - _timer;

    public void Enter()
    {
        // 1. Stop Navigation
        if (_ctrl.nav.isActiveAndEnabled)
        {
            _ctrl.nav.isStopped = true;
            _ctrl.nav.velocity = Vector3.zero;
        }

        // 2. Play Stun Animation
        _ctrl.aiAnim.SetBool("GetHit", true);
        _timer = 0;
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        if (_timer >= _duration)
        {
            _ctrl.ChangeState(_ctrl.patrolState); // go to patrol state
        }
    }

    public void Exit()
    {
        _ctrl.aiAnim.SetBool("GetHit", false);

        if (_ctrl.nav.isActiveAndEnabled)
            _ctrl.nav.isStopped = false;
    }
}