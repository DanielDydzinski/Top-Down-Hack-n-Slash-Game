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
    private bool _verticalJumpApplied;
    private bool _airFreezeChecked;
    private bool _isFrozen;
    private bool _airDriftCaptured;
    private bool _driftStopped;
    private float _airDriftSpeed;

    public ActionState(PlayerStateMachine stateMachine, Ability abilityToRun)
    {
        psm = stateMachine;
        ability = abilityToRun;
        layerIndex = (ability.baseSettings.animLayer == AnimationLayer.FullBody) ? 2 : 1;
    }

    public void EnterState()
    {
        if (!ability.baseSettings.canMoveAttack)
        {
            psm.anim.SetBool(psm.IsMovingHash, false);
            psm.anim.CrossFade(psm.CombatStanceStateHash, 0.15f, psm.BaseLayer);
        }

        // 1. CRITICAL: Kill the other script so it doesn't fight us
        psm.rotator.StopAllRotation();
        psm.rotator.enabled = false;

        // Vault-style abilities own movement entirely while airborne - PlayerMovement.Update() calls
        // its own Mover.Move() whenever a direction key is held, a second CharacterController.Move()
        // in the same frame as Mover's gravity Move(), which makes isGrounded unreliable (see
        // Mover.SetHorizontalVelocity) and was preventing the air-freeze landing check from ever
        // passing while a key was held.
        if (ability.baseSettings.verticalJumpForce > 0f)
            psm.playerMovement.enabled = false;

        // 2. FRAME ONE SNAP (As you requested)
        Vector3 initialDir = psm.playerToMouse.playerToMouseDir;
        if (initialDir != Vector3.zero)
        {
            initialDir.y = 0;
            targetMouseRotation = Quaternion.LookRotation(initialDir.normalized);
            psm.transform.rotation = targetMouseRotation; // The Snap
        }

        psm.abilityManager.SetMovementLock(!ability.baseSettings.canMoveAttack);
        psm.abilityManager.StartCastingAbility(ability, null);

        float moveMult = ability.baseSettings.canMoveAttack ? ability.baseSettings.movementMultiplier : 0f;
        psm.mover.SetSpeedMultiplier(moveMult);

        _verticalJumpApplied = false;
        _airFreezeChecked = false;
        _isFrozen = false;
        _airDriftCaptured = false;
        _driftStopped = false;
        _airDriftSpeed = 0f;
        if (ability.baseSettings.abilityControllerHeight > 0f)
            psm.ShrinkController(ability.baseSettings.abilityControllerHeight);
    }

    public void UpdateState()
    {
        // Optional: kill the carried-over horizontal drift the instant we touch ground, rather than
        // letting it run until the ability's own exit. Checked before the freeze branch below so it
        // still fires on the same frame landing unfreezes the animation.
        if (_airDriftCaptured && !_driftStopped && ability.baseSettings.stopDashOnGrounded && psm.characterController.isGrounded)
        {
            psm.mover.SetHorizontalVelocity(Vector3.zero);
            _driftStopped = true;
        }

        // --- UNIVERSAL ROTATION (Respects the Boolean) - hoisted above the freeze check so it still
        // runs while frozen, exactly like it already steers the forward dash below. Since the drift
        // feed further down re-derives its direction from transform.forward every frame instead of
        // a one-time snapshot, rotating here is all "air steering" needs - no separate system.
        if (ability.baseSettings.canRotateDuringCast)
        {
            UpdateTargetRotation();
            ApplySteering();
        }

        // Re-feed the captured drift speed from the current facing every frame (not just once) so the
        // rotation above actually redirects momentum while airborne, the same way it bends the dash
        // itself via transform.forward in HandleDashMovement.
        if (_airDriftCaptured && !_driftStopped)
        {
            psm.mover.SetHorizontalVelocity(psm.transform.forward * _airDriftSpeed);
        }

        // Mid-air freeze: once landed, resume playback and fall through to the normal update below
        // (startTime keeps advancing from where it left off, so the rest of the clip still plays out).
        if (_isFrozen)
        {
            if (psm.characterController.isGrounded)
            {
                _isFrozen = false;
                psm.anim.speed = 1f;
            }
            else
            {
                return;
            }
        }

        startTime += Time.deltaTime;

        // We need state info for both Dash Timing and Rotation length
        AnimatorStateInfo stateInfo = psm.anim.GetCurrentAnimatorStateInfo(psm.FullBodyLayer);
        float currentClipLength = stateInfo.length;

        // --- 2. MOVEMENT WINDOW (Universal for all, but Drag is Heavy only) ---
        float dashStart = currentClipLength * ability.baseSettings.dashStartTime;
        float dashEnd = currentClipLength * ability.baseSettings.dashEndTime;

        if (startTime >= dashStart && startTime <= dashEnd)
        {
            // One-shot: fires exactly as the dash window opens, not every frame within it.
            if (!_verticalJumpApplied && ability.baseSettings.verticalJumpForce > 0f)
            {
                psm.mover.SetVerticalVelocity(ability.baseSettings.verticalJumpForce);
                _verticalJumpApplied = true;
            }

            HandleDashMovement();
        }
        else if (!_airDriftCaptured && ability.baseSettings.verticalJumpForce > 0f && startTime > dashEnd)
        {
            // The dash window (which was manually Move()'ing us forward every frame) just closed -
            // capture how fast we were going so the re-feed above can keep carrying it via
            // transform.forward, instead of dropping straight down once HandleDashMovement stops running.
            Vector3 horizontalVelocity = psm.characterController.velocity;
            horizontalVelocity.y = 0f;
            _airDriftSpeed = horizontalVelocity.magnitude;
            _airDriftCaptured = true;
        }

        // --- 2.5 MID-AIR FREEZE CHECK: one-shot, at the configured point into the clip, freeze the
        // animation in place if we're still airborne beyond airbornHeightThreshold - UpdateState keeps
        // being called (see _isFrozen branch above) so it can detect landing and resume.
        if (ability.baseSettings.freezeInAir && !_airFreezeChecked)
        {
            float freezeCheckTime = currentClipLength * ability.baseSettings.airFreezeCheckPoint;
            if (startTime >= freezeCheckTime)
            {
                _airFreezeChecked = true;
                if (!psm.IsGroundedWithinDistance(ability.baseSettings.airbornHeightThreshold))
                {
                    _isFrozen = true;
                    psm.anim.speed = 0f;

                    // Covers the (unusual) case where the freeze point falls inside the dash window,
                    // before the branch above would otherwise have captured it.
                    if (!_airDriftCaptured)
                    {
                        Vector3 horizontalVelocity = psm.characterController.velocity;
                        horizontalVelocity.y = 0f;
                        _airDriftSpeed = horizontalVelocity.magnitude;
                        _airDriftCaptured = true;
                    }
                }
            }
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
            ability.baseSettings.rotationOomph * Time.deltaTime
        );
    }

    private void HandleDashMovement()
    {
        Vector3 dashDelta = Vector3.zero;

        if (ability.baseSettings.track == ComboTrack.Heavy)
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
            float adjustedSpeed = ability.baseSettings.dashPower / (1f + (totalMass * weightInfluence));
            dashDelta = psm.transform.forward * adjustedSpeed * Time.deltaTime;

            // 3. Drag the enemies
            MoveEntities(hitEnemies, dashDelta);
        }
        else
        {
            // --- LIGHT / MAGIC DASH STOP LOGIC ---
            float dashDistance = ability.baseSettings.dashPower * Time.deltaTime;

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
        if (psm.playerMovement) psm.playerMovement.enabled = true;
        psm.rotator.enabled = true; // Give control back to PlayerRotate
        psm.rotator.UpdateOrientation(); // Make sure it knows where we ended up

        psm.abilityManager.SetMovementLock(false);
        psm.abilityManager.CancelAbility();
        psm.anim.SetBool(psm.IsMovingHash, true);
        psm.anim.SetInteger(attackStateHash, -1);
        psm.RestoreControllerSize();

        // Don't carry vault drift into locomotion/whatever state comes next.
        if (_airDriftCaptured) psm.mover.SetHorizontalVelocity(Vector3.zero);

        // Safety net: if this state is interrupted while frozen (e.g. hit/stagger), don't leave the
        // Animator permanently paused for whatever state comes next.
        if (_isFrozen) psm.anim.speed = 1f;
    }
}