using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class KeyFloorLanding : MonoBehaviour
{
    [Header("Landing Audio")]
    [Tooltip("The sound that plays exactly when the key hits the floor.")]
    public AudioClip landSound;

    private Rigidbody _rb;
    private Collider _col;
    private bool _hasLanded = false;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Prevent this from firing multiple times if the key bounces
        if (_hasLanded) return;

        // Check if the thing we collided with is on the Floor layer
        // (Matches the 'Floor' layer name seen in your Project Settings)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Floor"))
        {
            _hasLanded = true;

            // 1. Play the landing sound right at the key's position
            if (landSound != null)
            {
                AudioSource.PlayClipAtPoint(landSound, transform.position);
            }

            // 2. Freeze it in place completely so it doesn't slide or jitter
            _rb.isKinematic = true;

            // 3. Turn it back into a trigger so the player can walk through it to collect it
            _col.isTrigger = true;
        }
    }
}