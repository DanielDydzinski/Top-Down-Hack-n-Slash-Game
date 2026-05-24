using UnityEngine;

public class HealPotion : MonoBehaviour
{
    [SerializeField] private float healAmount = 10f;

    [Header("Floating Settings")]
    [SerializeField] private float floatSpeed = 2f;      // How fast it bobs up and down
    [SerializeField] private float floatAmplitude = 0.2f; // How high/low it goes

    [Header("Expiration Settings")]
    [SerializeField] private float lifeDuration = 10f;    // Total seconds the potion lasts before destroying
    [SerializeField] private float flashThreshold = 3f;   // Seconds left when it starts flashing
    [SerializeField] private float flashSpeed = 15f;      // How fast it blinks (higher = faster)

    private Vector3 startPosition;
    private float lifeTimer;
    private MeshRenderer meshRenderer;

    void Start()
    {
        startPosition = transform.position;
        lifeTimer = lifeDuration;

        // Grab the MeshRenderer so we can toggle its visibility to flash
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    void Update()
    {
        FloatAnimation();
        HandleExpiration();
    }

    private void FloatAnimation()
    {
        float newY = startPosition.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void HandleExpiration()
    {
        // Count down the remaining life
        lifeTimer -= Time.deltaTime;

        // Time's up! Destroy the potion
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // If we are nearing the end of life, make it flash
        if (lifeTimer <= flashThreshold && meshRenderer != null)
        {
            // Mathf.Sin alternates between -1 and 1. If it's greater than 0, turn mesh ON, otherwise OFF.
            meshRenderer.enabled = Mathf.Sin(Time.time * flashSpeed) > 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Health hp = other.GetComponent<Health>();

        if (hp != null)
        {
            hp.Heal(healAmount);
            // Destroy immediately if collected
            Destroy(gameObject);
        }
    }
}