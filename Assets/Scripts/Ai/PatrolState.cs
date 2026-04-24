using UnityEngine;
using UnityEngine.AI;

public class PatrolState : IState
{
    private EnemyAIController _controller;
    private float _patrolTimer;
    private float _currentRandRate;

    public PatrolState(EnemyAIController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.nav.enabled = true;
        _controller.nav.isStopped = false;
        _controller.nav.stoppingDistance = 0;
       _currentRandRate = _controller.patrolRate;
        SetNewPatrolPoint();
    }

    public void UpdateState()
    {
        // If we are being shoved, don't calculate paths or try to move
        if (_controller.stats.isPushed) return;

        // Transition Condition: Player nearby?
        float distance = Vector3.Distance(_controller.transform.position, _controller.target.position);
        if (distance < _controller.engagedDistance)
        {
            _controller.ChangeState(_controller.chaseState);
            return;
        }

        // Logic: Keep patrolling
        _patrolTimer += Time.deltaTime;
        if (_patrolTimer >= _currentRandRate)
        {
            SetNewPatrolPoint();
            _patrolTimer = 0;
        }
    }

    public void Exit() { }

    private void SetNewPatrolPoint()
    {
        
        Vector3 randomDir = Random.insideUnitSphere * _controller.patrolDistance;
        randomDir += _controller.transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDir, out hit, _controller.patrolDistance, NavMesh.AllAreas);
        _controller.nav.SetDestination(hit.position);
    }
}