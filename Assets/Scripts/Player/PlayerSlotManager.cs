using System.Collections.Generic;
using UnityEngine;

public class PlayerSlotManager : MonoBehaviour
{
    [SerializeField] private int totalSlots = 6;
    [SerializeField] public float slotRadius = 2f;

    // Tracks which GameObject occupies which slot index
    private GameObject[] slots;

    void Awake()
    {
        slots = new GameObject[totalSlots];
    }

    void Update()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                // If the enemy walked away or was pushed far from their slot, free it automatically!
                float distanceToSlot = Vector3.Distance(slots[i].transform.position, GetSlotWorldPosition(i));
                if (distanceToSlot > 3.0f)
                {
                    slots[i] = null; // Free it, no triggers required
                }
            }
        }
    }

    // Enemies call this to get a world position destination
    public Vector3 RequestSlot(GameObject enemy, out int assignedSlotIndex)
    {
        assignedSlotIndex = -1;
        float closestDistance = float.MaxValue;
        int bestSlot = -1;

        // Find the closest FREE slot to the enemy to prevent criss-crossing
        for (int i = 0; i < totalSlots; i++)
        {
            if (slots[i] == null)
            {
                Vector3 slotPos = GetSlotWorldPosition(i);
                float dist = Vector3.Distance(enemy.transform.position, slotPos);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestSlot = i;
                }
            }
        }

        // If a slot is free, assign it
        if (bestSlot != -1)
        {
            slots[bestSlot] = enemy;
            assignedSlotIndex = bestSlot;
            return GetSlotWorldPosition(bestSlot);
        }

        // No slots available - tell the enemy to idle exactly where it already is, not path toward
        // world origin (which, on multi-level terrain, is very unlikely to be the local floor height).
        return enemy.transform.position;
    }

    // Dynamically updates the position for enemies already holding a slot
    public Vector3 GetSlotWorldPosition(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= totalSlots) return transform.position;

        // Calculate angle for this specific slot index
        float angle = slotIndex * (360f / totalSlots);

        // Convert polar coordinates to world space relative to player rotation
        Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * slotRadius;
        return transform.position + offset;
    }

    public void ReleaseSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < totalSlots)
        {
            slots[slotIndex] = null;
        }
    }

    // Visualise the slots in the Unity Editor Scene view
    void OnDrawGizmosSelected()
    {
        for (int i = 0; i < totalSlots; i++)
        {
            float angle = i * (360f / totalSlots);
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * slotRadius;
            Vector3 slotPos = transform.position + offset;

            Gizmos.color = (slots != null && slots[i] != null) ? Color.red : Color.green;
            Gizmos.DrawSphere(slotPos, 0.2f);
        }
    }
}
