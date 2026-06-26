using UnityEngine;

public class SpinOnString : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Number of full 360-degree turns before reversing.")]
    public float totalTurns = 3f;

    [Tooltip("Speed of the oscillation. Higher values mean faster back-and-forth movement.")]
    public float speed = 1f;

    private float startYRotation;
    private float timeAccumulator;

    void Start()
    {
        // Store the starting Y rotation of the object
        startYRotation = transform.localEulerAngles.y;
    }

    void Update()
    {
        // Increment time using DeltaTime for framerate independence
        timeAccumulator += Time.deltaTime * speed;

        // Calculate total target angle amplitude (360 degrees * number of turns)
        float maxAngle = totalTurns * 360f;

        // Calculate the sine wave modifier (-1 to 1)
        float sineWave = Mathf.Sin(timeAccumulator);

        // Calculate the final Y angle relative to the start position
        float targetY = startYRotation + (sineWave * maxAngle);

        // Apply the rotation directly to the local transform
        transform.localRotation = Quaternion.Euler(
            transform.localEulerAngles.x,
            targetY,
            transform.localEulerAngles.z
        );
    }
}
