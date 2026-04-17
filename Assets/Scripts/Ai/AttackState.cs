using UnityEngine;

public class AttackState : BaseAttackState
{
    public AttackState(EnemyAIController controller) : base(controller) { }
    private Ability _activeAbility;
    public void SetAbility(Ability ability) => _activeAbility = ability;

    public override void Enter()
    {
        _controller.abilityManager.StartCastingAbility(_activeAbility);

        if (!_activeAbility.canMoveAttack)
            _controller.SetObstacleMode(true); // cant move when attacking
    }

    public override void UpdateState()
    {


        // If we can move while attacking, keep pathing
        if (_activeAbility.canMoveAttack)
        {
            _controller.SetObstacleMode(false);
            _controller.nav.SetDestination(_controller.target.position);
        }

        LookAtTarget();

        // When the animation/cast is finished, go back to Chase
        if (!_controller.abilityManager.GetIsCastingAbility())
        {
            _controller.ChangeState(_controller.chaseState);
        }



        //// 4. Execution
        //if (nextAbility != null)
        //{
        //    _controller.nav.stoppingDistance = nextAbility.requiredRange;

        //    if (distance <= _controller.nav.stoppingDistance + 0.2f)
        //    {
        //        // Start the attack! 
        //        // If it can't move, swap to obstacle immediately.
        //        if (!nextAbility.canMoveAttack) _controller.SetObstacleMode(true);

        //        _controller.abilityManager.StartCastingAbility(nextAbility.abilityName);
        //    }
        //    else
        //    {
        //        _controller.SetObstacleMode(false);
        //        _controller.nav.SetDestination(_controller.target.position);
        //        LookAtTarget();
        //    }
        //}
    }

    public override void Exit()
    {
        _controller.SetObstacleMode(false); // Safety reset
    }

}