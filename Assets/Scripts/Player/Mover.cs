using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Mover : MonoBehaviour
{

    [Header("Speed Settings")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float minSpeed = 0f;
    [SerializeField] private float speed = 4f;
    private float speedMultiplier = 1f; // The multiplier from the current State (Action/Locomotion)

    [Header("Physics")]
    public bool useGravity = false; // Toggle this in Inspector
    [SerializeField] private float gravityValue = -9.81f;
    private Vector3 verticalVelocity;
    
    private Vector3 impactVelocity;
    [SerializeField] private float mass = 3f; // Higher mass = harder to push

    // Height (meters) dropped between leaving the ground and landing again - _fallStartY is captured
    // on the true->false isGrounded edge, _lastFallDistance on the matching false->true edge. Used by
    // PlayerStateMachine.GetFallDistanceDamageMultiplier for fall-distance-scaled damage.
    private bool _wasGrounded;
    private float _fallStartY;
    private float _lastFallDistance;
    private float _lastLandTime = -Mathf.Infinity;

    // How long (seconds) after landing GetLastFallDistance() still reports the real value. Without
    // this, one real fall would keep scoring bonus damage on every later grounded ability cast -
    // possibly minutes later - until the next big-enough fall happened to overwrite it.
    private const float FallDistanceValidWindow = 2f;

    // Fired once per qualifying landing (>0.3m, see the flicker guard below) with the distance fallen.
    // PlayerStateMachine subscribes to trigger the default ground impact for a plain fall - see its
    // HandleLanded. Ability-driven impacts are NOT routed through this: they resolve later (off an
    // animation event) and each ability's own Behaviour already computes its own scaled radius/
    // multiplier at that point, so spawning from here would mean re-deriving that math out of sync.
    public event Action<float> OnLanded;

    [Header("Directional info")]
    public bool movingBottomLeft { get; set; }
    public bool movingBottomRight { get; set; }
    public bool movingTopRight { get; set; }
    public bool movingTopLeft { get; set; }

    private Vector3 moveDirection;
    private CharacterController controller;
    private Stats stats;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        moveDirection = Vector3.zero;
        stats = GetComponent<Stats>();
    }

    // Everything that moves this character - knockback, WASD locomotion, gravity, and any drift fed
    // in by ability/dodge/fall states via SetHorizontalVelocity/SetVerticalVelocity - is folded into
    // ONE CharacterController.Move() call here. Splitting movement across multiple Move() calls in
    // the same frame (as this and PlayerMovement used to do independently) makes
    // CharacterController.isGrounded unreliable: a call that doesn't touch the ground can report
    // ungrounded even while standing still, which silently breaks the "-2f while grounded" gravity
    // clamp below and lets verticalVelocity accumulate unbounded until the character finally leaves
    // the ground and the whole backlog of speed dumps out at once.
    void Update()
    {
        Vector3 frameVelocity = Vector3.zero;

        // 1. Impact/Knockback (even when stunned!)
        if (impactVelocity.magnitude > 0.2f)
        {
            frameVelocity += impactVelocity;
        }
        impactVelocity = Vector3.Lerp(impactVelocity, Vector3.zero, 2f * Time.deltaTime);

        // 2. Regular WASD locomotion - PlayerMovement only sets the direction now (and clears it to
        // zero in OnDisable), it no longer calls Move() itself.
        float finalSpeed = stats.GetModifiedSpeed() * speedMultiplier;
        frameVelocity += moveDirection * finalSpeed;

        // 3. Gravity, plus any horizontal/vertical drift fed in by ability/dodge/fall states.
        bool groundedBefore = controller.isGrounded;

        // CharacterController.isGrounded flickers false for a single frame on stairs/uneven ground
        // (see fallHeightThreshold's tooltip in PlayerStateMachine) even while standing still - a
        // flicker's fake "fall" only lasts one frame, so it drops a few centimeters at most. The 0.3m
        // floor below filters that noise out instead of it clobbering a real landing's distance.
        if (!groundedBefore && _wasGrounded)
        {
            _fallStartY = transform.position.y;
        }
        else if (groundedBefore && !_wasGrounded)
        {
            float fallDistance = _fallStartY - transform.position.y;
            if (fallDistance > 0.3f)
            {
                _lastFallDistance = fallDistance;
                _lastLandTime = Time.time;
                Debug.Log($"[FallDistance] Landed after falling {fallDistance:F2}m - valid for {FallDistanceValidWindow:F1}s");
                OnLanded?.Invoke(fallDistance);
            }
        }
        _wasGrounded = groundedBefore;

        if (useGravity)
        {
            if (controller.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f;
            }
            verticalVelocity.y += gravityValue * Time.deltaTime;
            frameVelocity += verticalVelocity;
        }

        controller.Move(frameVelocity * Time.deltaTime);
    }


    public void SetSpeedMultiplier(float mult) => speedMultiplier = mult;

    // Height (meters) dropped by whatever fall most recently ended in a landing - readable for
    // FallDistanceValidWindow seconds after that landing (covers an ability's animation-event-driven
    // damage resolving a few frames late), then reports 0 so a much later grounded cast doesn't
    // silently inherit bonus damage from an old, unrelated fall.
    public float GetLastFallDistance()
    {
        float age = Time.time - _lastLandTime;
        if (age > FallDistanceValidWindow)
        {
            Debug.Log($"[FallDistance] Read requested {age:F2}s after last landing (> {FallDistanceValidWindow:F1}s window) - reporting 0");
            return 0f;
        }
        return _lastFallDistance;
    }

    public void AddForce(Vector3 direction, float force)
    {
        direction.Normalize();
        if (direction.y < 0) direction.y = 0; // Keep pushes horizontal
        impactVelocity += direction * force / mass;
    }

    // Injects an instantaneous vertical speed (e.g. a jump/vault impulse) - gravity in Update()
    // then arcs it back down every subsequent frame exactly like a normal fall.
    public void SetVerticalVelocity(float yVelocity)
    {
        verticalVelocity.y = yVelocity;
    }

    // Lets a state (e.g. FallingState) inject horizontal drift that gets folded into the single
    // per-frame Move() in Update() above, instead of the caller doing a separate Move() of its own.
    public void SetHorizontalVelocity(Vector3 horizontalVelocity)
    {
        horizontalVelocity.y = 0f;
        verticalVelocity.x = horizontalVelocity.x;
        verticalVelocity.z = horizontalVelocity.z;
    }

    public void SetDirection(Vector3 dir)
    {
        moveDirection = dir.normalized;
    }

    // --- Restored methods for Stats.cs and other scripts ---

    public void SetMaxSpeed(float mspeed)
    {
        maxSpeed = mspeed;
    }

    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    public void SetSpeed(float value)
    {
        speed = Mathf.Clamp(value, minSpeed, maxSpeed);
    }

    public float GetSpeed()
    {
        return speed;
    }

    public Vector3 GetDirection()
    {
        return moveDirection;
    }
}