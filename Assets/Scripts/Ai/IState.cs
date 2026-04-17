public interface IState
{
    void Enter();         // Called when entering the state
    void UpdateState();   // Called every frame (Logic)
    void Exit();          // Called when leaving the state
}