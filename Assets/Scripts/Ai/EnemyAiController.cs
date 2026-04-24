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

    [Header("Settings")]
    public float engagedDistance = 10f;
    public float rotationSpeed = 5f;

    [Header("Patrol Settings")]
    public float patrolRate = 5f;
    public float patrolDistance = 10f;

    [Header("combatSettings")]
    public float rangedDistance;
    public float defaultChaseDist = 1f;

    private IState currentState;

    // We initialize states here
    public PatrolState patrolState;
    public AttackState attackState;
    public ChaseState chaseState;
    public StunState stunState { get; private set; }


    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;

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
    public void TogglePhysicsMode(bool usePhysics)
    {
        nav.enabled = !usePhysics;
        GetComponent<Rigidbody>().isKinematic = !usePhysics;

        // If we are turning navigation back on, "snap" to the nearest navmesh point
        if (!usePhysics)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }
    }
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
}