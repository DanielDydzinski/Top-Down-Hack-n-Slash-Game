using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    // With "Reload Domain" disabled in Enter Play Mode Settings, static fields survive across
    // Stop/Play in the Editor even though the GameObject they pointed to gets destroyed. Without
    // this, a stale Instance from the previous session can make the real, freshly-loaded
    // ObjectPooler wrongly think it's a duplicate and destroy itself in Awake(). This runs at the
    // start of every session regardless of domain reload, so Instance always starts clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticStateOnSessionStart()
    {
        Instance = null;
    }

    [System.Serializable]
    public class PrewarmEntry
    {
        public GameObject prefab;
        [Min(0)] public int prewarmCount = 5;
    }

    [System.Serializable]
    public class PoolStatus
    {
        public string prefabName;
        public int totalCreated;
        public int idleInPool;
        public int activeInUse;
    }

    [Header("Pre-Warming")]
    [Tooltip("Prefabs to instantiate inactive at startup, so the first SpawnFromPool call for them doesn't pay an Instantiate cost mid-gameplay.")]
    [SerializeField] private List<PrewarmEntry> prewarmPools = new List<PrewarmEntry>();

    [Header("Live Pool View (read-only, Play Mode)")]
    [Tooltip("Auto-updated snapshot of every pool's size. Not meant to be hand-edited - just watch it while playing.")]
    [SerializeField] private List<PoolStatus> poolDebugView = new List<PoolStatus>();

    // A dictionary linking a master Prefab asset to its queue of sleeping clones
    private Dictionary<GameObject, Queue<GameObject>> _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, int> _totalCreated = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, PoolStatus> _debugLookup = new Dictionary<GameObject, PoolStatus>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (PrewarmEntry entry in prewarmPools)
        {
            Prewarm(entry.prefab, entry.prewarmCount);
        }
    }

    /// <summary>
    /// Instantiates count inactive copies of prefab up front, so later SpawnFromPool calls
    /// dequeue an existing instance instead of paying an Instantiate cost during gameplay.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        if (!_poolDictionary.ContainsKey(prefab))
        {
            _poolDictionary.Add(prefab, new Queue<GameObject>());
        }

        for (int i = 0; i < count; i++)
        {
            GameObject instance = CreatePooledInstance(prefab, Vector3.zero, Quaternion.identity);
            instance.SetActive(false);
            instance.transform.SetParent(transform);
            _poolDictionary[prefab].Enqueue(instance);
        }

        RefreshDebugView(prefab);
    }

    /// <summary>
    /// Spawns an object from the pool, replacing Instantiate.
    /// Pass autoReturnDelay > 0 to have it recycle itself back into the pool after that many seconds,
    /// instead of the caller having to track and call ReturnToPool manually.
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, float autoReturnDelay = 0f)
    {
        GameObject objectToSpawn = SpawnInternal(prefab, position, rotation);
        if (objectToSpawn == null) return null;

        if (autoReturnDelay > 0f)
        {
            objectToSpawn.GetComponent<PoolInfo>().ScheduleAutoReturn(autoReturnDelay);
        }

        return objectToSpawn;
    }

    /// <summary>
    /// Same as above, but attaches the spawned instance to a parent afterwards (world position/rotation preserved) —
    /// e.g. a bleed effect that should keep following the wound anchor it was spawned at.
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, float autoReturnDelay = 0f)
    {
        GameObject objectToSpawn = SpawnInternal(prefab, position, rotation);
        if (objectToSpawn == null) return null;

        objectToSpawn.transform.SetParent(parent, worldPositionStays: true);

        if (autoReturnDelay > 0f)
        {
            objectToSpawn.GetComponent<PoolInfo>().ScheduleAutoReturn(autoReturnDelay);
        }

        return objectToSpawn;
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
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
            objectToSpawn = CreatePooledInstance(prefab, position, rotation);
        }

        PoolInfo info = objectToSpawn.GetComponent<PoolInfo>();

        // A dequeued instance is still parented under the pooler itself from its last ReturnToPool -
        // detach it back to scene-root now, before repositioning. Otherwise anything that walks up
        // via transform.root while it's active (e.g. CarcassCruncher) resolves to the ObjectPooler
        // GameObject instead of this instance, with destructive results.
        objectToSpawn.transform.SetParent(null);

        // Position the object cleanly before waking it up
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);
        info.RestoreInitialHierarchyState();

        RefreshDebugView(prefab);

        return objectToSpawn;
    }

    private GameObject CreatePooledInstance(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // Instantiate directly at the target position/rotation (not Instantiate(prefab) + reposition
        // after) so components' Awake/OnEnable - which run synchronously inside Instantiate - see the
        // real spawn position immediately, e.g. DestroyObject's byDistance tracking needs a correct
        // starting point from frame one.
        GameObject instance = Instantiate(prefab, position, rotation);

        // Optional: attach a helper component to remember who its parent prefab is
        PoolInfo info = instance.AddComponent<PoolInfo>();
        info.myPrefabSource = prefab;
        // Snapshot which nested parts start active/inactive, in case some of them retire
        // themselves independently over the effect's lifetime (see PoolInfo.RestoreInitialHierarchyState).
        info.CaptureInitialHierarchyState();

        _totalCreated[prefab] = _totalCreated.TryGetValue(prefab, out int existing) ? existing + 1 : 1;

        return instance;
    }

    private void RefreshDebugView(GameObject prefab)
    {
        if (!_debugLookup.TryGetValue(prefab, out PoolStatus status))
        {
            status = new PoolStatus { prefabName = prefab.name };
            _debugLookup[prefab] = status;
            poolDebugView.Add(status);
        }

        int idle = _poolDictionary.TryGetValue(prefab, out Queue<GameObject> queue) ? queue.Count : 0;
        int total = _totalCreated.TryGetValue(prefab, out int totalCount) ? totalCount : 0;

        status.totalCreated = total;
        status.idleInPool = idle;
        status.activeInUse = total - idle;
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
        // Re-home it under the pooler itself so it isn't at the mercy of whatever transient
        // transform it was parented to while active (e.g. an enemy that then gets destroyed).
        instanceToReturn.transform.SetParent(transform);

        // Push it back to the end of the waiting line
        if (_poolDictionary.ContainsKey(info.myPrefabSource))
        {
            if (!_poolDictionary[info.myPrefabSource].Contains(instanceToReturn))
            {
                _poolDictionary[info.myPrefabSource].Enqueue(instanceToReturn);
            }
        }

        RefreshDebugView(info.myPrefabSource);
    }
}

// Simple metadata tracker component injected into instantiated objects
public class PoolInfo : MonoBehaviour
{
    public GameObject myPrefabSource;

    private Transform[] _hierarchyTransforms;
    private bool[] _initialActiveStates;
    private Vector3[] _initialLocalPositions;
    private Quaternion[] _initialLocalRotations;

    // Lets SpawnFromPool's autoReturnDelay recycle this instance without an external timer/coroutine.
    public void ScheduleAutoReturn(float delay)
    {
        CancelInvoke(nameof(AutoReturn));
        Invoke(nameof(AutoReturn), delay);
    }

    private void AutoReturn()
    {
        // The singleton can outlive its own scene-placed GameObject (e.g. a scene reload that
        // doesn't preserve it) - if it's gone, there's no pool left to return to.
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(gameObject);
        }
    }

    // Cancel any pending auto-return if this gets returned early (or re-pooled) so a stale
    // Invoke can't fire later and yank a freshly respawned instance back out of active use.
    private void OnDisable()
    {
        CancelInvoke(nameof(AutoReturn));
    }

    // Some multi-part effect prefabs have nested pieces that retire themselves independently
    // (see DestroyObject) rather than the whole prefab having one lifetime - and physics-driven
    // prefabs (ragdoll gibs) have children that get scattered by forces during use. Snapshot each
    // descendant's starting active flag and local transform right after the pristine first Instantiate...
    public void CaptureInitialHierarchyState()
    {
        _hierarchyTransforms = GetComponentsInChildren<Transform>(true);
        int count = _hierarchyTransforms.Length;
        _initialActiveStates = new bool[count];
        _initialLocalPositions = new Vector3[count];
        _initialLocalRotations = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            Transform t = _hierarchyTransforms[i];
            _initialActiveStates[i] = t.gameObject.activeSelf;
            _initialLocalPositions[i] = t.localPosition;
            _initialLocalRotations[i] = t.localRotation;
        }
    }

    // ...and restore it on every reuse, so a part that retired itself or got knocked/exploded
    // somewhere last time isn't permanently missing/displaced on the pooled instance going forward.
    // Index 0 is always this GameObject itself (GetComponentsInChildren includes the root) - its
    // position/rotation/active state is set directly by ObjectPooler's spawn call, not restored here.
    public void RestoreInitialHierarchyState()
    {
        if (_hierarchyTransforms == null) return;

        for (int i = 1; i < _hierarchyTransforms.Length; i++)
        {
            Transform t = _hierarchyTransforms[i];
            if (t == null) continue;

            // Reset the transform BEFORE re-activating it, so anything that reads its own
            // position/rotation from OnEnable (as DestroyObject does) sees the restored pose,
            // not wherever physics left it last time.
            t.localPosition = _initialLocalPositions[i];
            t.localRotation = _initialLocalRotations[i];
            t.gameObject.SetActive(_initialActiveStates[i]);
        }
    }
}