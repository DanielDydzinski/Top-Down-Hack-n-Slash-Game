using System;
using UnityEngine;

public class ActionState : IPlayerState
{
    private PlayerStateMachine psm;
    private Ability ability;
    private int layerIndex;
    public static readonly int attackStateHash = Animator.StringToHash("AttackState");
    private readonly int attackTagHash = Animator.StringToHash("Attack");
    private float startTime = 0f;
    private float GRACE_PERIOD = 0.05f;

    private Quaternion targetMouseRotation;
    private LayerMask enemyLayer = LayerMask.GetMask("Enemy");

    public ActionState(PlayerStateMachine stateMachine, Ability abilityToRun)
    {
        psm = stateMachine;
        ability = abilityToRun;
        layerIndex = (ability.animLayer == AnimationLayer.FullBody) ? 2 : 1;
    }

    public void EnterState()
    {
        if (!ability.canMoveAttack)
        {
            psm.anim.SetBool(psm.IsMovingHash, false);
            psm.anim.CrossFade(psm.CombatStanceStateHash, 0.15f, psm.BaseLayer);
        }

        // 1. CRITICAL: Kill the other script so it doesn't fight us
        psm.rotator.StopAllRotation();
        psm.rotator.enabled = false;

        // 2. FRAME ONE SNAP (As you requested)
        Vector3 initialDir = psm.playerToMouse.playerToMouseDir;
        if (initialDir != Vector3.zero)
        {
            initialDir.y = 0;
            targetMouseRotation = Quaternion.LookRotation(initialDir.normalized);
            psm.transform.rotation = targetMouseRotation; // The Snap
        }

        psm.abilityManager.SetMovementLock(!ability.canMoveAttack);
        psm.abilityManager.StartCastingAbility(ability, null);

        float moveMult = ability.canMoveAttack ? ability.movementMultiplier : 0f;
        psm.mover.SetSpeedMultiplier(moveMult);
    }

    public void UpdateState()
    {
        startTime += Time.deltaTime;

        // We need state info for both Dash Timing and Rotation length
        AnimatorStateInfo stateInfo = psm.anim.GetCurrentAnimatorStateInfo(psm.FullBodyLayer);
        float currentClipLength = stateInfo.length;

        // --- 1. UNIVERSAL ROTATION (Respects the Boolean) ---
        if (ability.canRotateDuringCast)
        {
            UpdateTargetRotation();
            ApplySteering();
        }

        // --- 2. MOVEMENT WINDOW (Universal for all, but Drag is Heavy only) ---
        float dashStart = currentClipLength * ability.dashStartTime;
        float dashEnd = currentClipLength * ability.dashEndTime;

        if (startTime >= dashStart && startTime <= dashEnd)
        {
            HandleDashMovement();
        }

        // --- 3. STATE EXIT ---
        if (startTime < GRACE_PERIOD) return;
        if (psm.anim.GetInteger(attackStateHash) == -1) psm.SwitchState(new LocomotionState(psm));
    }

    private void MoveEntities(Collider[] enemies, Vector3 delta)
    {
        foreach (var col in enemies)
        {
            if (col.gameObject == psm.gameObject) continue;
            if (col.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent) && agent.enabled)
            {
                if (agent.isOnNavMesh) agent.Move(delta);
            }
            else
            {
                col.transform.position += delta;
            }
        }
    }

    private void UpdateTargetRotation()
    {
        Vector3 currentMouseDir = psm.playerToMouse.playerToMouseDir;
        if (currentMouseDir != Vector3.zero)
        {
            currentMouseDir.y = 0;
            targetMouseRotation = Quaternion.LookRotation(currentMouseDir.normalized);
        }
    }
    private void ApplySteering()
    {
        // Use the Oomph from the ability SO
        psm.transform.rotation = Quaternion.RotateTowards(
            psm.transform.rotation,
            targetMouseRotation,
            ability.rotationOomph * Time.deltaTime
        );
    }

    private void HandleDashMovement()
    {
        Vector3 dashDelta = Vector3.zero;

        if (ability.track == ComboTrack.Heavy)
        {
            // 1. Detection for the "Drag"
            Collider[] hitEnemies = Physics.OverlapSphere(psm.transform.position + psm.transform.forward * 1f, 0.8f, enemyLayer);
            float totalMass = 0f;

            foreach (var col in hitEnemies)
            {
                if (col.gameObject != psm.gameObject && col.TryGetComponent<Stats>(out var stats))
                    totalMass += stats.GetMass();
            }

            // 2. Heavy Speed Calculation (Weight Influence)
            float weightInfluence = 0.2f;
            float adjustedSpeed = ability.dashPower / (1f + (totalMass * weightInfluence));
            dashDelta = psm.transform.forward * adjustedSpeed * Time.deltaTime;

            // 3. Drag the enemies
            MoveEntities(hitEnemies, dashDelta);
        }
        else
        {
            // --- LIGHT / MAGIC DASH STOP LOGIC ---
            float dashDistance = ability.dashPower * Time.deltaTime;

            // SphereCast: Think of this as throwing a ball forward to see if it hits a wall/enemy
            // Radius 0.4f roughly matches the Player's width
            if (Physics.SphereCast(psm.transform.position + Vector3.up, 0.4f, psm.transform.forward, out RaycastHit hit, dashDistance, enemyLayer))
            {
                // We hit an enemy! 
                // Calculate a smaller delta so we stop right in front of them instead of clipping through
                dashDelta = psm.transform.forward * hit.distance;
            }
            else
            {
                // Path is clear
                dashDelta = psm.transform.forward * dashDistance;
            }
        }

        // Move the Player (Applies to all types)
        psm.characterController.Move(dashDelta);
    }

    public void ExitState()
    {
        psm.mover.enabled = true;
        psm.rotator.enabled = true; // Give control back to PlayerRotate
        psm.rotator.UpdateOrientation(); // Make sure it knows where we ended up

        psm.abilityManager.SetMovementLock(false);
        psm.abilityManager.CancelAbility();
        psm.anim.SetBool(psm.IsMovingHash, true);
        psm.anim.SetInteger(attackStateHash, -1);
    }
}