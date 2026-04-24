using System;
using UnityEngine;

public class ActionState : IPlayerState
{
    private PlayerStateMachine psm;
    private Ability ability;
    private int layerIndex;
    public static readonly int attackStateHash = Animator.StringToHash("AttackState");
    // Store this once
    private readonly int attackTagHash = Animator.StringToHash("Attack");
    private float startTime = 0f;
    private float GRACE_PERIOD = 0.05f;

    private Quaternion mouseRotation;
    private float rotationSpeed = 700;
    Vector3 initialAim;

    LayerMask enemyLayer = LayerMask.GetMask("Enemy");

    private float totalAnimLength;


    public ActionState(PlayerStateMachine stateMachine, Ability abilityToRun)
    {
        psm = stateMachine;
        ability = abilityToRun;
        // Determine which layer we are watching based on the ability
        layerIndex = (ability.animLayer == AnimationLayer.FullBody) ? 2 : 1;
    }

    public void EnterState()
    {

        //make sure legs not moving when we are still and upperbody is attacking
        if (!ability.canMoveAttack )
        {
            //psm.playerMovement.enabled = false;
            psm.anim.SetBool(psm.IsMovingHash, false);
            psm.anim.CrossFade(psm.CombatStanceStateHash,0.15f, psm.BaseLayer);
        }
        psm.rotator.enabled = ability.canRotateDuringCast;
        psm.rotator.StopAllRotation();
      

        //CAPTURE THE AIM TARGET IMMEDIATELY
        initialAim = psm.playerToMouse.playerToMouseDir;
        if (initialAim != Vector3.zero)
        {
            initialAim.y = 0;
            mouseRotation = Quaternion.LookRotation(initialAim.normalized);
            psm.transform.rotation = mouseRotation;
        }


        // Lock movement if the AbilityManager says so
        psm.abilityManager.SetMovementLock(!ability.canMoveAttack);

        // Tell AbilityManager to start (handles Cooldowns & activeAbility)
        // We pass null for target because Player usually aims via PlayerToMouse
        psm.abilityManager.StartCastingAbility(ability, null);

        //  Configure Mover & Rotator
        // If we can't move, multiplier is 0. If we can, use the ability's multiplier.
        float moveMult = ability.canMoveAttack ? ability.movementMultiplier : 0f;
        psm.mover.SetSpeedMultiplier(moveMult);
    }

    public void UpdateState()
    {
        startTime += Time.deltaTime;

        //all heavy attacks live in FullBodyAttack animation layer
        if (!ability.canMoveAttack && ability.track == ComboTrack.Heavy)
        {

            AnimatorStateInfo stateInfo2 = psm.anim.GetCurrentAnimatorStateInfo(psm.FullBodyLayer);
            float currentClipLength = stateInfo2.length;

            // Calculate the Dash Window using Ability parameters
            // Convert normalized dashStartTime (0.0 to 1.0) into seconds
            float dashStartInSeconds = currentClipLength * ability.dashStartTime;
            float dashEndInSeconds = currentClipLength * ability.dashEndTime;

            Collider[] hitEnemies = Physics.OverlapSphere(psm.transform.position + psm.transform.forward * 1f, 0.75f, enemyLayer);

          

            // Only move during the dash frames
            if ((startTime >= dashStartInSeconds && startTime <= dashEndInSeconds))
            {
                if (ability.canRotateDuringCast) 
                {
                    initialAim = psm.playerToMouse.playerToMouseDir;
                    mouseRotation = Quaternion.LookRotation(initialAim.normalized);
                    psm.transform.rotation = mouseRotation;

                }



                float totalMass = 0f;

                foreach (var col in hitEnemies)
                {
                    if (col.gameObject != psm.gameObject)
                    {
                        // Use TryGetComponent - it's faster and safer than GetComponent
                        if (col.TryGetComponent<Stats>(out var stats))
                        {
                            totalMass += stats.GetMass();
                        }
                    }
                }
                // A higher weightInfluence (e.g., 0.8) makes hitting enemies feel much "heavier"
                float weightInfluence = 0.2f;
                float adjustedSpeed = ability.dashPower / (1f + (totalMass * weightInfluence)); // account for enemies mass we dragging along

               // float lungeSpeed = ability.dashPower; // Adjust this for "oomph"
                Vector3 dashDelta = mouseRotation * Vector3.forward * adjustedSpeed * Time.deltaTime;

                // Add a bit of gravity so they don't float if they walk off a ledge
                dashDelta.y -= 9.81f * Time.deltaTime;

                //move all enemies along
                foreach (var col in hitEnemies)
                {
                    // Make sure we aren't "gluing" the player to themselves
                    if (col.gameObject != psm.gameObject )
                    {
                      

                        // Move the enemy by the same moveDelta as the player
                        // This keeps them perfectly in front of you
                        if (col.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent) && agent.enabled)
                        {
                            if (agent.isOnNavMesh) agent.Move(dashDelta);
                        }
                        else
                        {
                            col.transform.position += dashDelta;
                        }
                    }
                }

                psm.characterController.Move(dashDelta);
            }

            if (ability.canRotateDuringCast)
            {
                RotateTowardsMouse(rotationSpeed, mouseRotation);
            }
        }

        // NEW: Don't even check for completion until the grace period is over
        if (startTime < GRACE_PERIOD) return;



        // Use your Animator Tag "Attack" to see when we are done
        AnimatorStateInfo stateInfo = psm.anim.GetCurrentAnimatorStateInfo(layerIndex);
        //bool isTransitioning = psm.anim.IsInTransition(layerIndex);

        // If the animator is no longer playing an "Attack" tagged clip, return to move
        // OR if the Manager manually cleared the state (via CancelAbility)

        bool isAttacking = stateInfo.tagHash == attackTagHash;

        if (psm.anim.GetInteger(attackStateHash) == -1)
        {
            psm.SwitchState(new LocomotionState(psm));
        }
    }

    public void ExitState()
    {
        // Hand control back to the player scripts
        psm.mover.enabled = true;
        psm.rotator.enabled = true;
        psm.abilityManager.SetMovementLock(false);
        psm.abilityManager.CancelAbility();
        //psm.characterController.enabled = true;
        psm.anim.SetBool(psm.IsMovingHash, true);
        psm.anim.SetInteger(attackStateHash,-1);
        //psm.mover.SetSpeed(psm.mover.GetMaxSpeed());
        //psm.anim.SetInteger(attackStateHash, -1);//default state
    }

    private void RotateTowardsMouse(float force, Quaternion targetRotation)
    {
        psm.transform.rotation = Quaternion.RotateTowards(
                psm.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

}
    
