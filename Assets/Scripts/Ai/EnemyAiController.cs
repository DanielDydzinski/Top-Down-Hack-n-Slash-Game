using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public NavMeshAgent nav;
    public NavMeshObstacle obstacle;
    private bool _hasObstacle;
    public AbilityManager abilityManager;
    public Stats stats;
    public Animator aiAnim;
    public Health hp;
    private static readonly int SpeedHash = Animator.StringToHash("speed");
    public int emptyStateHash = Animator.StringToHash("Empty State"); // Ensure this state exists on your layers!
    public readonly int BaseLayer = 0;
    public readonly int GetHitLayer = 1;
    public readonly int AttackLayer = 2;
    public readonly int DeathLayer = 3;
    [SerializeField] public LayerMask enemyLayer;


    [Header("Settings")]
    public float engagedDistance = 10f;
    public float rotationSpeed = 5f;

    [Header("Patrol Settings")]
    public float patrolRate = 5f;
    public float patrolDistance = 10f;

    [Header("combatSettings")]
    public float rangedDistance;
    public float defaultChaseDist = 1f;

    [Header("player crowding")]
    // Inside EnemyAIController.cs, add:
    public PlayerSlotManager slotManager; // Assign via Inspector or FindFirstObjectByType in Awake
    [HideInInspector] public int MySlotIndex = -1;
    [HideInInspector] public bool HasSlot = false;
    [Header("enemy Side Stepping")]
    // -- Side Stepping enemy Collisions --
    public float SidestepDuration = 1.0f;  // How long the "sideways burst" lasts.
    public float SidestepCooldown = 0.8f;  // Prevents the AI from jittering back and forth constantly.
    public float SidestepDistance = 2.2f;  // How far to the side the AI tries to move.
    public float BlockLookAhead = 0.8f;  // How far in front the AI checks for other enemies.
    public float BlockCastRadius = 0.5f; // Size of the "detection beam." Keep this smaller than the AI's width to avoid self-hits.
    public float SideClearRadius = 0.5f;  // The size of the "safety bubble" checked at the destination.


    private IState currentState;

    // We initialize states here
    public PatrolState patrolState;
    public AttackState attackState;
    public ChaseState chaseState;
    public StunState stunState { get; private set; }


    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        slotManager = target.GetComponent<PlayerSlotManager>();

        nav = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        aiAnim = GetComponent<Animator>();
        abilityManager = GetComponent<AbilityManager>();
        hp = GetComponent<Health>();
        stats = GetComponent<Stats>();

        obstacle = GetComponent<NavMeshObstacle>();

        // Check if the component actually exists
        _hasObstacle = obstacle != null;

        if (_hasObstacle)
        {
            obstacle.enabled = false;
            obstacle.carving = true;
        }

        // Initialize concrete states
        patrolState = new PatrolState(this);
        attackState = new AttackState(this);
        stunState = new StunState(this);
        chaseState = new ChaseState(this);

        // Start in Patrol
        ChangeState(patrolState);
    }

    void Update()
    {
        if (hp != null && hp.GetisDead())
        {
            HandleDeath();
            return;
        }

        if (currentState != null)
            currentState.UpdateState();

        float currentSpeed =nav.velocity.magnitude / nav.speed;

        // 2. Feed it to the animator with a 'DampTime'
        // The 0.1f is the "smoothing" time. The higher this is, the slower the blend.
        aiAnim.SetFloat(SpeedHash, currentSpeed, 0.1f, Time.deltaTime);

        // Update animator speed for all states
        // aiAnim.SetFloat(SpeedHash, nav.velocity.magnitude);
    }

    public void CleanUpSlot()
    {
        if (HasSlot && slotManager != null)
        {
            slotManager.ReleaseSlot(MySlotIndex);
            HasSlot = false;
            MySlotIndex = -1;
        }
    }

    public void ChangeState(IState newState)
    {
        if (currentState != null)
            currentState.Exit();

        currentState = newState;
        currentState.Enter();
    }

    public void ApplyStun(float duration)
    {
        // If we are already stunned, only refresh if the NEW duration is longer 
        // than the REMAINING time on the current stun.
        if (currentState == stunState)
        {
            if (duration > stunState.RemainingTime)
            {
                stunState.SetDuration(duration);
            }
        }
        else
        {
            stunState.SetDuration(duration);
            ChangeState(stunState);
        }
    }
    //public void TogglePhysicsMode(bool usePhysics)
    //{
    //    nav.enabled = !usePhysics;
    //    GetComponent<Rigidbody>().isKinematic = !usePhysics;

    //    // If we are turning navigation back on, "snap" to the nearest navmesh point
    //    if (!usePhysics)
    //    {
    //        NavMeshHit hit;
    //        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
    //        {
    //            transform.position = hit.position;
    //        }
    //    }
    //}
    public void SetObstacleMode(bool isObstacle)
    {
        if (_hasObstacle)
        {
            // PRO APPROACH: Swap between Agent and Obstacle
            if (isObstacle && nav.enabled)
            {
                nav.enabled = false;
                obstacle.enabled = true;
            }
            else if (!isObstacle && !nav.enabled)
            {
                obstacle.enabled = false;
                nav.enabled = true;
            }
        }
    }

    public void LookAtTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    private void HandleDeath()
    {

        Rigidbody rb = GetComponent<Rigidbody>();
        if (nav.enabled || rb.isKinematic == false)
        {
            // 1. Shut down the effects first!
            EffectManager em = GetComponent<EffectManager>();
            if (em != null) em.CleanUpAllEffects();

            // 2. Shut down the AI
            nav.enabled = false;
            rb.isKinematic = true;

            // 3. Play death anim or destroy
            Destroy(gameObject, 3f); // Destroy almost instantly
        }
    }

    private void OnDrawGizmos() // Changed from OnDrawGizmosSelected for constant viewing
    {
        // 1. Safety check
        if (currentState == null) return;

        // 2. Check if the current state is ChaseState
        if (currentState is ChaseState chase)
        {
            // Draw a temporary yellow marker above the enemy to prove the state is active
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, new Vector3(0.5f, 0.5f, 0.5f));

            // 3. Call the state's gizmo logic
            chase.DrawGizmos();
        }
    }
}