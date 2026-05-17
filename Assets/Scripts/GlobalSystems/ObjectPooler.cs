using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    // A dictionary linking a master Prefab asset to its queue of sleeping clones
    private Dictionary<GameObject, Queue<GameObject>> _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Spawns an object from the pool, replacing Instantiate.
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPooler] Attempted to spawn a null prefab!");
            return null;
        }

        // If this prefab hasn't been pooled yet, register a new queue for it dynamically
        if (!_poolDictionary.ContainsKey(prefab))
        {
            _poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        GameObject objectToSpawn = null;

        // Check if we have a sleeping object available in our queue
        if (_poolDictionary[prefab].Count > 0)
        {
            objectToSpawn = _poolDictionary[prefab].Dequeue();
        }

        // If the queue was empty, grow the pool dynamically by generating a fresh clone
        if (objectToSpawn == null)
        {
            objectToSpawn = Instantiate(prefab);
            // Optional: attach a helper component to remember who its parent prefab is
            PoolInfo info = objectToSpawn.AddComponent<PoolInfo>();
            info.myPrefabSource = prefab;
        }

        // Position the object cleanly before waking it up
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    /// <summary>
    /// Returns an active object back to its designated pool, replacing Destroy.
    /// </summary>
    public void ReturnToPool(GameObject instanceToReturn)
    {
        if (instanceToReturn == null) return;

        PoolInfo info = instanceToReturn.GetComponent<PoolInfo>();
        if (info == null || info.myPrefabSource == null)
        {
            // Fallback safety catch: If it wasn't spawned through the pooler, destroy it normally
            Destroy(instanceToReturn);
            return;
        }

        instanceToReturn.SetActive(false);

        // Push it back to the end of the waiting line
        if (_poolDictionary.ContainsKey(info.myPrefabSource))
        {
            if (!_poolDictionary[info.myPrefabSource].Contains(instanceToReturn))
            {
                _poolDictionary[info.myPrefabSource].Enqueue(instanceToReturn);
            }
        }
    }
}

// Simple metadata tracker component injected into instantiated objects
public class PoolInfo : MonoBehaviour
{
    public GameObject myPrefabSource;
}