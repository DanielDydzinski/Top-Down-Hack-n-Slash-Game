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

    [Header("Directional info")]
    public bool movingBottomLeft { get; set; }
    public bool movingBottomRight { get; set; }
    public bool movingTopRight { get; set; }
    public bool movingTopLeft { get; set; }

    private Vector3 moveDirection;
    private CharacterController controller;
    private Stats stats;

    // DEBUG - remove once fall-speed investigation is done
    private PlayerMovement _debugPlayerMovement;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        moveDirection = Vector3.zero;
        stats = GetComponent<Stats>();
        _debugPlayerMovement = GetComponent<PlayerMovement>();
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
        // DEBUG - remove once fall-speed investigation is done
        bool groundedBefore = controller.isGrounded;
        bool wasClamped = false;
        if (useGravity)
        {
            if (controller.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f;
                wasClamped = true;
            }
            verticalVelocity.y += gravityValue * Time.deltaTime;
            frameVelocity += verticalVelocity;
        }

        controller.Move(frameVelocity * Time.deltaTime);

        // DEBUG - remove once fall-speed investigation is done
        Debug.Log($"[GravityDebug] t={Time.time:F3} isGroundedBefore={groundedBefore} clamped={wasClamped} playerMovementEnabled={(_debugPlayerMovement != null ? _debugPlayerMovement.enabled : (bool?)null)} moveDirection={moveDirection} finalSpeed={finalSpeed:F2} locomotionContribution={moveDirection * finalSpeed} vVel={verticalVelocity} frameVelocity={frameVelocity} controllerVelAfterMove={controller.velocity} isGroundedAfterMove={controller.isGrounded}");
    }


    public void SetSpeedMultiplier(float mult) => speedMultiplier = mult;

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