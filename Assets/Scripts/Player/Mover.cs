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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        moveDirection = Vector3.zero;
        stats = GetComponent<Stats>();
    }

    void Update()
    {
        // 1. Always process Impact/Knockback (even when stunned!)
        if (impactVelocity.magnitude > 0.2f)
        {
            controller.Move(impactVelocity * Time.deltaTime);
        }

        // 2. Consume the force over time (Damping)
        impactVelocity = Vector3.Lerp(impactVelocity, Vector3.zero, 2f * Time.deltaTime);

        if (useGravity)
        {
            ApplyGravity();
        }
    }


    public void SetSpeedMultiplier(float mult) => speedMultiplier = mult;

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
    public void AddForce(Vector3 direction, float force)
    {
        direction.Normalize();
        if (direction.y < 0) direction.y = 0; // Keep pushes horizontal
        impactVelocity += direction * force / mass;
    }

    public void Move()
    {
        // Calculation: (Base Stat Speed) * (Active Effects from Stats) * (Ability Multiplier)
        float finalSpeed = stats.GetModifiedSpeed() * speedMultiplier;

        // CharacterController handles the collision "slide" for us here
        Vector3 movement = moveDirection * finalSpeed * Time.deltaTime;
        controller.Move(movement);
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