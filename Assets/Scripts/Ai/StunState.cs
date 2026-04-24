using UnityEngine;
using UnityEngine.UI;

public class StunState : IState
{
    private EnemyAIController _ctrl;
    private float _duration;
    private float _timer;



    public StunState(EnemyAIController controller) => _ctrl = controller;

    // We allow the state to be initialized with a specific duration
    public void SetDuration(float duration)
    {
        _duration = duration;
        _timer = 0; 
    }
    public float RemainingTime => _duration - _timer;

    public void Enter()
    {
        // 1. Stop Navigation
        if (_ctrl.nav.isActiveAndEnabled)
        {
            _ctrl.nav.ResetPath();
            _ctrl.nav.isStopped = true;
            _ctrl.nav.velocity = Vector3.zero;
            
        }
        _ctrl.abilityManager.CancelAbility();
       // _ctrl.TogglePhysicsMode(true);
        _ctrl.aiAnim.Play(_ctrl.emptyStateHash, _ctrl.AttackLayer); // Attack Layer

        // 2. Play Stun Animation
       // _ctrl.aiAnim.SetBool("GetHit", true);
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
      

        _ctrl.TogglePhysicsMode(false);
    }
}