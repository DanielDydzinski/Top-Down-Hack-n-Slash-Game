using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    private ComboController comboController;
    private PlayerStateMachine psm;

    void Start()
    {
        comboController = GetComponent<ComboController>();
        psm = GetComponent<PlayerStateMachine>();
    }

    void Update()
    {
        HandleDodgeInput();

        if (!psm.IsDodging())
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                comboController.OnAbilityInput(ref comboController.magicCombosIndex, comboController.magicCombos);
            }
             else if (Input.GetMouseButtonDown(0))
            {
                comboController.OnAbilityInput(ref comboController.lightCombosIndex, comboController.lightCombos);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                comboController.OnAbilityInput(ref comboController.heavyCombosIndex, comboController.heavyCombos);
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                comboController.OnAbilityInput(ref comboController.fIndex, comboController.fAbilities);
            }

            else if (Input.GetKeyDown(KeyCode.Q))
            {
                comboController.OnAbilityInput(ref comboController.qIndex, comboController.qAbilities);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                comboController.OnAbilityInput(ref comboController.eIndex, comboController.eAbilities);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                comboController.OnAbilityInput(ref comboController.rIndex, comboController.rAbilities);
            }

        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            // Only cancel if we are currently in an ActionState
            if (psm.abilityManager.IsPerformingAction())
            {
                psm.abilityManager.CancelAbilityButtonUp();
            }
        }

    }

    void HandleDodgeInput()
    {
        if (psm.IsDodging()) return;
        if (!Input.GetKeyDown(KeyCode.LeftShift)) return;

        if (psm.GetCurrentState() is PlayerStunState
            || psm.GetCurrentState() is BackflipDodgeState
            || psm.GetCurrentState() is RollDodgeState
            || psm.GetCurrentState() is ActionState) return;

        if (psm.IsStunned() || psm.IsDodging() || psm.GetCurrentState() is ActionState) return;

        Vector3 moveDir = psm.playerMovement.movingDirection;
        Vector3 faceDir = psm.rotator.facingDirVec3;

        if (moveDir == Vector3.zero) return;

        float dot = Vector3.Dot(moveDir.normalized, faceDir.normalized);
        float cross = Vector3.Cross(faceDir.normalized, moveDir.normalized).y;

        if (dot < -0.5f)
        {
            // Moving opposite to facing -> Backflip
            psm.SwitchState(new BackflipDodgeState(psm, moveDir));
        }
        else
        {
            // Forward, left, or right -> Roll (character snaps to face moveDir in EnterState)
            psm.SwitchState(new RollDodgeState(psm, moveDir));
        }
    }
}