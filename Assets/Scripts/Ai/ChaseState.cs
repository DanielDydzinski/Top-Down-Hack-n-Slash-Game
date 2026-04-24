using UnityEngine;


public class ChaseState : IState
{
    private EnemyAIController _controller;
    private Ability _nextAbility;

    public ChaseState(EnemyAIController controller) => _controller = controller;

    public void Enter()
    {
        _controller.SetObstacleMode(false); // We are a walker now
        // Subscribe to the event
        _controller.abilityManager.OnAbilityReady += ReevaluatePlan;
        ReevaluatePlan(); // Determine initial plan
    }

    private void ReevaluatePlan()
    {
        float distance = Vector3.Distance(_controller.transform.position, _controller.target.position);

        // 3. Selection Logic (Melee vs Ranged)
        // You can decide to go for Melee if close, Ranged if far.
        
        if (distance > _controller.rangedDistance) //  threshold for ranged
            _nextAbility = _controller.abilityManager.GetHighestPriorityReady(true);
        else
            _nextAbility = _controller.abilityManager.GetHighestPriorityReady(false);

        // Fallback: If no ranged is ready, try melee (and vice versa)
        if (_nextAbility == null)
            _nextAbility = _controller.abilityManager.GetHighestPriorityAbility();

        // Update NavMesh based on what we chose
        if (_nextAbility != null)
            _controller.nav.stoppingDistance = _nextAbility.requiredRange;
        else
            _controller.nav.stoppingDistance = _controller.defaultChaseDist; // Default "Get close" distance
    }

    public void UpdateState()
    {
        // If we are being shoved, don't calculate paths or try to move
        if (_controller.stats.isPushed) return;

        float distance = Vector3.Distance(_controller.transform.position, _controller.target.position);

        // 1. Move
        if (distance > _controller.nav.stoppingDistance + 0.2f)
        {
            _controller.nav.SetDestination(_controller.target.position);
        }
        _controller.LookAtTarget();

        // 2. Transition to Attack
        if (_nextAbility != null && distance <= _controller.nav.stoppingDistance + 0.2f)
        {
            _controller.attackState.SetAbility(_nextAbility);
            _controller.ChangeState(_controller.attackState);
        }

        // 3. Transition back to Patrol if player escaped
        if (distance > _controller.engagedDistance)
            _controller.ChangeState(_controller.patrolState);
    }

    public void Exit()
    {
        // CRITICAL: Unsubscribe to prevent memory leaks and errors!
        _controller.abilityManager.OnAbilityReady -= ReevaluatePlan;
    }
}