using UnityEngine;

public class ChaseState : IState
{
    private EnemyAIController _controller;
    private Ability _nextAbility;

    // -- Sidestep sub-mode parameters --
    private bool _isSidestepping;      // If true, the AI is currently moving sideways, ignoring the player's direct position.
    private float _sidestepTimer;     // How much longer the AI will force the sideways movement.
    private float _sidestepCooldown;  // Time to wait after a sidestep before the AI is allowed to try another one.
    private Vector3 _sidestepDestination; // The specific world point the AI is trying to reach during a sidestep.
    private float sideStepDistance = 4f; // how far from player do we care to side steps enemies



    // -- Gizmo state --
    private bool _gizmoActive;
    private Vector3 _gizmoCastOrigin;
    private Vector3 _gizmoToTarget;
    private bool _gizmoForwardBlocked;
    private Vector3[] _gizmoDirPositions = new Vector3[4];
    private bool[] _gizmoDirClear = new bool[4];
    private int _gizmoChosenIndex = -1;

    public ChaseState(EnemyAIController controller) => _controller = controller;

    public void Enter()
    {
        _controller.SetObstacleMode(false);
        _isSidestepping = false;
        _sidestepTimer = 0f;
        _sidestepCooldown = 0f;
        _gizmoActive = false;
        _controller.abilityManager.OnAbilityReady += ReevaluatePlan;
        ReevaluatePlan();
    }

    private void ReevaluatePlan()
    {
        float distance = Vector3.Distance(_controller.transform.position, _controller.target.position);

        if (distance > _controller.rangedDistance)
            _nextAbility = _controller.abilityManager.GetHighestPriorityReady(true);
        else
            _nextAbility = _controller.abilityManager.GetHighestPriorityReady(false);

        if (_nextAbility == null)
            _nextAbility = _controller.abilityManager.GetHighestPriorityAbility();

        _controller.nav.stoppingDistance = _nextAbility != null
            ? _nextAbility.requiredRange
            : _controller.defaultChaseDist;
    }

    public void UpdateState()
    {
        if (_controller.stats.isPushed) return;

       

        float distance = Vector3.Distance(_controller.transform.position, _controller.target.position);

        if(distance > _controller.engagedDistance)
        {
            _controller.ChangeState(_controller.patrolState); //go back to patrol when too far away
        }

        if (_sidestepCooldown > 0f)
            _sidestepCooldown -= Time.deltaTime;

        // ── Sidestepping sub-mode ─────────────────────────────────────────
        if (_isSidestepping)
        {
            _sidestepTimer -= Time.deltaTime;

            // FIX 1: Force stopping distance to near-zero so we actually reach the destination
            _controller.nav.stoppingDistance = 0.1f;
            _controller.nav.SetDestination(_sidestepDestination);

            float distToSidestep = Vector3.Distance(_controller.transform.position, _sidestepDestination);

            // If we reach the spot or time runs out
            if (_sidestepTimer <= 0f || distToSidestep < 0.4f)
            {
                _isSidestepping = false;
                _sidestepCooldown = _controller.SidestepCooldown;

                // FIX 2: Restore the stopping distance immediately so we don't nose-bump the player
                ReevaluatePlan();
            }
            return;
        }

        // ── Normal chase ──────────────────────────────────────────────────
        if (distance > _controller.nav.stoppingDistance + 0.2f)
        {
            // During normal chase, ReevaluatePlan() has already set the correct stopping distance
            _controller.nav.SetDestination(_controller.target.position);

            if (_sidestepCooldown <= 0f && distance < sideStepDistance)
                TryStartSidestep();
        }

        _controller.LookAtTarget();

        // Ability transition logic...
        if (_nextAbility != null && distance <= _controller.nav.stoppingDistance + 0.2f)
        {
            _controller.attackState.SetAbility(_nextAbility);
            _controller.ChangeState(_controller.attackState);
        }
    }

    private void TryStartSidestep()
    {
        Vector3 pos = _controller.transform.position;
        Vector3 toTarget = (_controller.target.position - pos);
        toTarget.y = 0;
        if (toTarget.sqrMagnitude < 0.01f) return;
        toTarget.Normalize();

        // --- 1. THE PROXIMITY CHECK (For enemies "Right in front") ---
        // This looks in a small circle immediately in front of the AI's face.
        Vector3 facePoint = pos + toTarget * 0.8f + Vector3.up * 0.5f;
        Collider[] closeEnemies = Physics.OverlapSphere(facePoint, 0.6f, _controller.enemyLayer);

        bool isCrowded = false;
        foreach (var col in closeEnemies)
        {
            if (col.transform != _controller.transform) // Don't detect myself
            {
                isCrowded = true;
                break;
            }
        }

        // --- 2. THE LOOK-AHEAD CHECK (For enemies further away) ---
        Vector3 castOrigin = pos + Vector3.up * 0.5f + toTarget * 0.5f;
        _gizmoForwardBlocked = Physics.SphereCast(
            castOrigin, _controller.BlockCastRadius, toTarget,
            out RaycastHit hit, _controller.BlockLookAhead, _controller.enemyLayer);

        // If no one is far away AND no one is touching our face, we don't need to sidestep
        if (!_gizmoForwardBlocked && !isCrowded)
        {
            _gizmoActive = false;
            return;
        }

        // --- 3. CALCULATE DIRECTIONS ---
        _gizmoActive = true;
        Vector3 left = new Vector3(-toTarget.z, 0, toTarget.x);
        Vector3 right = -left;

        // We try 45-degree angles first (Forward-Sides) so they don't stop moving forward
        Vector3[] dirs = {
        (toTarget + left).normalized,
        (toTarget + right).normalized,
        left,
        right
    };

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector3 checkPos = pos + dirs[i] * _controller.SidestepDistance;

            // Is this spot on the NavMesh?
            bool onNav = UnityEngine.AI.NavMesh.SamplePosition(checkPos, out var navHit, 1.0f, UnityEngine.AI.NavMesh.AllAreas);
            // Is this spot free of other enemies?
            bool clear = !Physics.CheckSphere(checkPos, _controller.SideClearRadius, _controller.enemyLayer);

            _gizmoDirPositions[i] = checkPos;
            _gizmoDirClear[i] = onNav && clear;
        }

        // Pick a direction...
        int startIdx = Random.value < 0.5f ? 0 : 1;
        for (int offset = 0; offset < 4; offset++)
        {
            int i = (startIdx + offset) % 4;
            if (_gizmoDirClear[i])
            {
                _sidestepDestination = _gizmoDirPositions[i];
                _sidestepTimer = _controller.SidestepDuration;
                _isSidestepping = true;
                _gizmoChosenIndex = i;
                return;
            }
        }
    }

    public void Exit()
    {
        _isSidestepping = false;
        _gizmoActive = false;
        _controller.abilityManager.OnAbilityReady -= ReevaluatePlan;
    }

    public void DrawGizmos()
    {
        if (_controller == null) return;
        Vector3 pos = _controller.transform.position;

        if (_gizmoActive)
        {
            // Red/Green line showing the forward check
            Gizmos.color = _gizmoForwardBlocked ? Color.red : Color.green;
            Vector3 castEnd = _gizmoCastOrigin + _gizmoToTarget * _controller.BlockLookAhead;
            Gizmos.DrawLine(_gizmoCastOrigin, castEnd);
            Gizmos.DrawWireSphere(castEnd, _controller.BlockCastRadius);

            for (int i = 0; i < 4; i++)
            {
                if (_gizmoDirPositions[i] == Vector3.zero) continue;

                if (i == _gizmoChosenIndex) Gizmos.color = Color.cyan;
                else Gizmos.color = _gizmoDirClear[i] ? Color.green : Color.red;

                Gizmos.DrawWireSphere(_gizmoDirPositions[i], _controller.SideClearRadius);
                Gizmos.DrawLine(pos, _gizmoDirPositions[i]);
            }
        }

        if (_isSidestepping)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_sidestepDestination, 0.5f);
            Gizmos.DrawLine(pos, _sidestepDestination);
        }
    }
}