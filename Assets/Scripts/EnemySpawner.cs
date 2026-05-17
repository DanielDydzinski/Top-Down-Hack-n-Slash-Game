using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The enemy prefab to instantiate.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("How often (in seconds) a new enemy should spawn.")]
    [SerializeField] private float spawnInterval = 3.0f;

    [Tooltip("Turn this off if you want to stop spawning temporarily.")]
    [SerializeField] private bool isSpawning = true;

    private void Start()
    {
        // Start the infinite spawning loop
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // Optional: Add an initial delay before the very first spawn if needed
        // yield return new WaitForSeconds(1.0f);

        while (true)
        {
            // Only spawn if the prefab is set and spawning is active
            if (isSpawning && enemyPrefab != null)
            {
                SpawnEnemy();
            }

            // Wait for the specified interval before looping again
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        // Instantiate the prefab exactly at this script's position and rotation
        Instantiate(enemyPrefab, transform.position, transform.rotation);
    }

    // Optional helper methods to control the spawner from other scripts
    public void StartSpawner() => isSpawning = true;
    public void StopSpawner() => isSpawning = false;
}