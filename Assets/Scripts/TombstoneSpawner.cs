using System.Collections;
using UnityEngine;

public class TombstoneSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The enemy prefab to instantiate.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("How often (in seconds) a new enemy should spawn.")]
    [SerializeField] private float spawnInterval = 5.0f;

    [Tooltip("Turn this off if you want to stop spawning temporarily.")]
    [SerializeField] private bool isSpawning = true;

    [Tooltip("If true, spawning only happens when a player stands inside the Box Collider trigger. If false, it spawns constantly.")]
    [SerializeField] private bool usePlayerTrigger = true;

    [Tooltip("The Tag assigned to your Player GameObject (usually 'Player').")]
    [SerializeField] private string playerTag = "Player";

    [Header("Tombstone Shake Settings")]
    [Tooltip("The Transform of the tombstone mesh. If left empty, it will use this GameObject's transform.")]
    [SerializeField] private Transform tombstoneTransform;

    [Tooltip("Maximum degrees to rotate on the X axis during the shake.")]
    [SerializeField] private float shakeMagnitudeDegrees = 5.0f;

    [Tooltip("How long the tombstone shakes before the enemy spawns.")]
    [SerializeField] private float shakeDuration = 1.5f;

    [Header("Particle Systems")]
    [Tooltip("Plays continuously while the tombstone shakes.")]
    [SerializeField] private ParticleSystem smallSmokeParticle;

    [Tooltip("Plays a burst exactly when the enemy spawns.")]
    [SerializeField] private ParticleSystem dirtExploParticle;

    [Header("Audio Settings")]
    [Tooltip("The Audio Source component used to play the sounds.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Sound clip that plays right when the tombstone starts shaking.")]
    [SerializeField] private AudioClip earthRumbleClip;

    [Tooltip("Sound clip that plays right when the zombie spawns.")]
    [SerializeField] private AudioClip dirtImpactClip;

    private Quaternion originalRotation;
    private bool isPlayerInside = false;
    private Coroutine spawnCoroutine;

    private void Start()
    {
        if (tombstoneTransform == null)
        {
            tombstoneTransform = transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        originalRotation = tombstoneTransform.localRotation;

        // If we don't care about the player trigger, start spawning immediately
        if (!usePlayerTrigger)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // Only proceed if active spawner state allows it
            if (isSpawning && enemyPrefab != null)
            {
                // 1. Start smoke particle system
                if (smallSmokeParticle != null)
                {
                    smallSmokeParticle.Play();
                }

                // 2. Play Earth Rumble audio clip
                if (audioSource != null && earthRumbleClip != null)
                {
                    audioSource.PlayOneShot(earthRumbleClip);
                }

                // 3. Shake the tombstone
                float elapsedTime = 0f;
                while (elapsedTime < shakeDuration)
                {
                    float randomXOffset = Random.Range(-shakeMagnitudeDegrees, shakeMagnitudeDegrees);
                    tombstoneTransform.localRotation = originalRotation * Quaternion.Euler(randomXOffset, 0f, 0f);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // 4. Reset tombstone rotation and stop smoke
                tombstoneTransform.localRotation = originalRotation;

                if (smallSmokeParticle != null)
                {
                    smallSmokeParticle.Stop();
                }

                // 5. Spawn enemy, explode dirt, and play Dirt Impact audio clip
                SpawnEnemy();

                if (dirtExploParticle != null)
                {
                    dirtExploParticle.Play();
                }

                if (audioSource != null && dirtImpactClip != null)
                {
                    audioSource.PlayOneShot(dirtImpactClip);
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        Instantiate(enemyPrefab, transform.position, transform.rotation);
    }

    // --- Trigger Detection ---

    private void OnTriggerEnter(Collider other)
    {
        if (!usePlayerTrigger) return;

        // Check if the object entering the zone is the player
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = true;

            // Start the loop only if it isn't already running
            if (spawnCoroutine == null)
            {
                spawnCoroutine = StartCoroutine(SpawnRoutine());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!usePlayerTrigger) return;

        if (other.CompareTag(playerTag))
        {
            isPlayerInside = false;

            // Stop the loop immediately when player leaves
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            // Clean reset to defaults in case player left mid-shake
            tombstoneTransform.localRotation = originalRotation;
            if (smallSmokeParticle != null) smallSmokeParticle.Stop();
        }
    }

    public void StartSpawner() => isSpawning = true;
    public void StopSpawner() => isSpawning = false;
}
