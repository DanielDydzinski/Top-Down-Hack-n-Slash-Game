using UnityEngine;

public class SingleEnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The enemy prefab to instantiate.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Optional: Specific spawn point. If null, spawns at this script's position.")]
    [SerializeField] private Transform spawnPoint;

    /// <summary>
    /// Spawns a single enemy. Hook this function up to Unity Events.
    /// </summary>
    public void SpawnSingleEnemy()
    {
        // Safety check to prevent null reference errors
        if (enemyPrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] Cannot spawn enemy: Prefab is missing!", this);
            return;
        }

        // Determine spawn location and rotation
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        // Spawn the enemy
        GameObject spawnedEnemy = Instantiate(enemyPrefab, position, rotation);

        // Optional log to confirm functionality in the console
       // Debug.Log($"Successfully spawned: {spawnedEnemy.name}", spawnedEnemy);
    }
}
