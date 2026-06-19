using UnityEngine;

public class WakeUpState : IState
{
    private EnemyAIController _controller;
    private float _wakeUpTimer;
    private float _duration;

    public WakeUpState(EnemyAIController controller, float duration)
    {
        _controller = controller;
        _duration = duration;
    }

    public void Enter()
    {
        // 1. Stop the NavMeshAgent from sliding around
        if (_controller.nav.isActiveAndEnabled)
        {
            _controller.nav.isStopped = true;
        }

        // 2. Trigger the animation. 
        // (Make sure you add a Trigger parameter called "WakeUp" in your Animator!)
        if (_controller.aiAnim != null)
        {
            _controller.aiAnim.SetTrigger("WakeUp");
        }

        // 3. Reset the timer
        _wakeUpTimer = 0f;
    }

    public void UpdateState()
    {
        // 4. Count up the timer
        _wakeUpTimer += Time.deltaTime;

        // 5. When the animation is done, decide what to do next
        if (_wakeUpTimer >= _duration)
        {
            // go straight to patrol state
                _controller.ChangeState(_controller.patrolState);
            
        }
    }

    public void Exit()
    {
        // Allow the NavMeshAgent to move again as they exit the wake-up sequence
        if (_controller.nav.isActiveAndEnabled)
        {
            _controller.nav.isStopped = false;
        }
    }
}