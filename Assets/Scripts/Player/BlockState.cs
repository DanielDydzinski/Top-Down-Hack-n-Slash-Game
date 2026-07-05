using UnityEngine;

public class BlockState : IPlayerState
{
    private PlayerStateMachine psm;

    private float reactionTimer;
    private bool isPlayingReaction;

    private Quaternion targetMouseRotation;

    public BlockState(PlayerStateMachine _psm)
    {
        psm = _psm;
    }

    public void EnterState()
    {
        psm.rotator.StopAllRotation();
        psm.rotator.enabled = false;
        psm.anim.SetBool(psm.IsMovingHash, false);
        psm.abilityManager.SetMovementLock(true);

        if (psm.mover != null) psm.mover.SetSpeedMultiplier(0f);

        Vector3 initialDir = psm.playerToMouse.playerToMouseDir;
        if (initialDir != Vector3.zero)
        {
            initialDir.y = 0;
            targetMouseRotation = Quaternion.LookRotation(initialDir.normalized);
        }
        else
        {
            targetMouseRotation = psm.transform.rotation;
        }

        psm.anim.CrossFade(psm.BlockLoopHash, 0.15f, psm.BaseLayer);
        isPlayingReaction = false;

        psm.playerEnergy?.BeginBlockRegen();
    }

    public void UpdateState()
    {
        if (!isPlayingReaction)
        {
            Vector3 currentMouseDir = psm.playerToMouse.playerToMouseDir;
            if (currentMouseDir != Vector3.zero)
            {
                currentMouseDir.y = 0;
                targetMouseRotation = Quaternion.LookRotation(currentMouseDir.normalized);
            }

            psm.transform.rotation = Quaternion.RotateTowards(
                psm.transform.rotation,
                targetMouseRotation,
                psm.blockRotationSpeed * Time.deltaTime
            );

            // PlayerRotate is disabled for the duration of the block (see EnterState), so its
            // facingDirVec3 would otherwise stay frozen at whatever it was before block started.
            // Dodge direction selection (Backflip vs Roll) reads facingDirVec3 while blocking, so
            // keep it live-synced to the mouse-facing rotation we're driving above every frame.
            psm.rotator.UpdateOrientation();
        }

        if (isPlayingReaction)
        {
            reactionTimer -= Time.deltaTime;
            if (reactionTimer <= 0f)
            {
                isPlayingReaction = false;
                psm.anim.CrossFade(psm.BlockLoopHash, 0.15f, psm.BaseLayer);
            }
        }
    }

    public void ExitState()
    {
        psm.playerEnergy?.EndBlockRegen();

        psm.anim.CrossFade(psm.CombatStanceStateHash, 0.15f);

        psm.mover.enabled = true;
        if (psm.mover != null) psm.mover.SetSpeedMultiplier(1f);

        psm.rotator.enabled = true;
        psm.rotator.UpdateOrientation();

        psm.abilityManager.SetMovementLock(false);
        psm.anim.SetBool(psm.IsMovingHash, true);
    }

    // --- UPDATED TO RECEIVE HITINFO ---
    public bool TryBlock(HitInfo info)
    {
        // 1. Guard clause: DoT status effects can NEVER trigger a physical shield flinch
        if (info.attackType == AttackType.DoT) return false;

        // 2. Safe Position Resolution
        Vector3 attackerPosition;
        if (info.attacker != null)
        {
            attackerPosition = info.attacker.transform.position;
        }
        else if (info.impactPoint != Vector3.zero)
        {
            attackerPosition = info.impactPoint;
        }
        else
        {
            // THE FIX: If there is no attacker and no physical impact point, 
            // do not auto-block it. Fail safely.
            return false;
        }

        // 3. Angle Verification
        Vector3 dirToAttacker = (attackerPosition - psm.transform.position);
        dirToAttacker.y = 0f;
        dirToAttacker.Normalize();

        Vector3 facingDir = psm.transform.forward;
        facingDir.y = 0f;
        facingDir.Normalize();

        float dot = Vector3.Dot(facingDir, dirToAttacker);
        float threshold = Mathf.Cos(psm.blockAngleLimit * Mathf.Deg2Rad);

        if (dot >= threshold)
        {
            PlayRandomReaction();
            return true;
        }

        return false;
    }

    private void PlayRandomReaction()
    {
        if (psm.blockReactionClipNames == null || psm.blockReactionClipNames.Count == 0) return;

        int randomIndex = Random.Range(0, psm.blockReactionClipNames.Count);
        string chosenClipName = psm.blockReactionClipNames[randomIndex];
        int reactionHash = Animator.StringToHash(chosenClipName);

        psm.anim.CrossFade(reactionHash, 0.05f, psm.BaseLayer);

        if (psm.AnimationLengths.TryGetValue(chosenClipName, out float animationLength))
        {
            reactionTimer = animationLength;
            isPlayingReaction = true;
        }
        else
        {
            reactionTimer = 0.35f;
            isPlayingReaction = true;
        }
    }
}
