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

        if (!psm.IsDodging()) // if we are not dodging we can attack
        {
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Only trigger if we aren't "Locked" or if your game allows queueing
                comboController.OnAbilityInput(ref comboController.magicCombosIndex,comboController.magicCombos);
            }
            if (Input.GetMouseButtonDown(0))
            {
                // Only trigger if we aren't "Locked" or if your game allows queueing
                comboController.OnAbilityInput(ref comboController.lightCombosIndex, comboController.lightCombos);
            }
            if (Input.GetMouseButtonDown(1))
                {
                // Only trigger if we aren't "Locked" or if your game allows queueing
                comboController.OnAbilityInput(ref comboController.heavyCombosIndex, comboController.heavyCombos);
            }

        }
        // You could add more here later:
        // if (Input.GetKeyDown(KeyCode.Space)) spaceAbility.OnInput();
    }

    void HandleDodgeInput()
    {
        if (psm.IsDodging()) return;
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {

            if ((psm.GetCurrentState() is PlayerStunState || psm.GetCurrentState() is BackflipDodgeState || psm.GetCurrentState() is ActionState)) return;

            // Get snapped 8-way vectors
            Vector3 moveDir = psm.playerMovement.movingDirection;
            Vector3 faceDir = psm.rotator.facingDirVec3;

            // Get the WASD direction
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 moveInput = new Vector3(h, 0, v).normalized;



            // If not moving, don't dodge (or do a stationary dodge)
            if (moveDir == Vector3.zero) return;

  

            // Normalize and Compare
            // We normalize because (0.5, 0, 0.5) has a different length than (1, 0, 0)
            float dot = Vector3.Dot(moveDir.normalized, faceDir.normalized);

            if (dot < -0.5f)
            {
                // MOVE IS OPPOSITE TO FACE -> BACKFLIP
                // We pass moveDir so the backflip follows the movement keys
                psm.SwitchState(new BackflipDodgeState(psm, moveDir));
            }
            else
            {
                // MOVE IS FORWARD/SIDEWAYS -> ROLL
                // psm.SwitchState(new RollDodgeState(psm, moveDir));
                Debug.Log("Forward or Side Dodge triggered!");
            }
        }
    }
    
}