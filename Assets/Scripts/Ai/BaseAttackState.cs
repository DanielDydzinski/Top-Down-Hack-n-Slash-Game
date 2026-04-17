using UnityEngine;

public abstract class BaseAttackState : IState
{
    protected EnemyAIController _controller;

    public BaseAttackState(EnemyAIController controller) => _controller = controller;

    public virtual void Enter() { } 
    public abstract void UpdateState(); // Subclasses MUST implement this
    public virtual void Exit()
    {
        // give control back to the NavMeshAgent when leaving this state
        _controller.SetObstacleMode(false);
    }

    protected void LookAtTarget()
    {
        Vector3 direction = (_controller.target.position - _controller.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        _controller.transform.rotation = Quaternion.Slerp(_controller.transform.rotation, lookRotation, Time.deltaTime * _controller.rotationSpeed);
    }
}