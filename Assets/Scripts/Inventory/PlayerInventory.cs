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

    [System.Serializable]
    public class PotionSlot
    {
        public PotionData potionType;
        public int count;
    }

    [Header("Health Potions - 4 Fixed Belt Slots")]
    [SerializeField]
    private PotionSlot[] potionSlots = new PotionSlot[4]
    {
        new PotionSlot(), new PotionSlot(), new PotionSlot(), new PotionSlot()
    };
    [SerializeField] private int maxStackPerSlot = 5;

    public int SlotCount => potionSlots.Length;

    public PotionSlot GetSlot(int index)
    {
        if (index < 0 || index >= potionSlots.Length) return null;
        return potionSlots[index];
    }

    // Stacks into a matching slot first, otherwise fills the first empty slot.
    // Returns false if the belt is entirely full.
    public bool AddPotion(PotionData potion)
    {
        if (potion == null) return false;

        for (int i = 0; i < potionSlots.Length; i++)
        {
            if (potionSlots[i].potionType == potion && potionSlots[i].count < maxStackPerSlot)
            {
                potionSlots[i].count++;
                return true;
            }
        }

        for (int i = 0; i < potionSlots.Length; i++)
        {
            if (potionSlots[i].potionType == null)
            {
                potionSlots[i].potionType = potion;
                potionSlots[i].count = 1;
                return true;
            }
        }

        return false;
    }

    // Consumes one potion from a specific belt slot
    public bool UsePotion(int slotIndex, out PotionData usedType)
    {
        usedType = null;
        PotionSlot slot = GetSlot(slotIndex);
        if (slot == null || slot.potionType == null || slot.count <= 0) return false;

        usedType = slot.potionType;
        slot.count--;
        if (slot.count <= 0)
        {
            slot.potionType = null;
            slot.count = 0;
        }
        return true;
    }

    // For drag-and-drop reordering between belt slots
    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= potionSlots.Length || b < 0 || b >= potionSlots.Length || a == b) return;
        (potionSlots[a], potionSlots[b]) = (potionSlots[b], potionSlots[a]);
    }

    public int GetPotionCount()
    {
        int total = 0;
        foreach (var s in potionSlots) total += s.count;
        return total;
    }
}
