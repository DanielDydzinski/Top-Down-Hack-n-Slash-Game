using UnityEngine;

public class ToggleAbilityLock : StateMachineBehaviour
{
    private AbilityManager _abilityManager;

    // Triggered the very first frame the animation starts
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_abilityManager == null)
            _abilityManager = animator.GetComponent<AbilityManager>();

        if (_abilityManager != null)
            _abilityManager.SetMovementLock(true);
            
    }

    // Triggered the frame the animation transitions out or finishes
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_abilityManager != null)
        {
            _abilityManager.SetMovementLock(false);
            _abilityManager.ClearActiveAbility();
        }
        

    }
}