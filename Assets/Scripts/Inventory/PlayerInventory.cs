using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // A collection tracking the exact ScriptableObject references collected
    [SerializeField] private HashSet<KeyData> collectedKeys = new HashSet<KeyData>();

    public void AddKey(KeyData key)
    {
        if (key == null) return;

        collectedKeys.Add(key);
        //Debug.Log($"Picked up key: {key.keyName}");
    }

    public bool HasKey(KeyData key)
    {
        if (key == null) return false;

        return collectedKeys.Contains(key);
    }

    public void RemoveKey(KeyData key)
    {
        if (key == null) return;

        collectedKeys.Remove(key);
    }
}
