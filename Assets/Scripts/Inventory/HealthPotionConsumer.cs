using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthPotionConsumer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerInventory inventory;
    [Tooltip("The ghost/pending-heal fill Image on the HP bar, sits behind the real fill")]
    [SerializeField] private Image ghostFillImage;

    [Header("Feedback")]
    [Tooltip("the sound played when a potion is consumed")]
    public AudioClip potionUseSound;

    // One entry per potion currently draining; its heal amount/duration/tick rate
    // came from that potion's own PotionData, so different potions can heal differently
    private class ActiveHeal
    {
        public float remainingAmount;
        public float perTickAmount;
        public float tickInterval;
        public float tickTimer;
    }

    private readonly List<ActiveHeal> activeHeals = new List<ActiveHeal>();

    void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
    }

    void Start()
    {
        UpdateGhostFill();
    }

    void Update()
    {
        if (activeHeals.Count == 0) return;

        for (int i = activeHeals.Count - 1; i >= 0; i--)
        {
            ActiveHeal heal = activeHeals[i];
            heal.tickTimer += Time.deltaTime;

            while (heal.tickTimer >= heal.tickInterval && heal.remainingAmount > 0f)
            {
                heal.tickTimer -= heal.tickInterval;

                float tickAmount = Mathf.Min(heal.perTickAmount, heal.remainingAmount);
                playerHealth.Heal(tickAmount);
                heal.remainingAmount -= tickAmount;
            }

            if (heal.remainingAmount <= 0f)
                activeHeals.RemoveAt(i);
        }

        UpdateGhostFill();
    }

    public void TryConsumePotion(int slotIndex)
    {
        if (playerHealth == null || inventory == null) return;
        if (playerHealth.GetisDead()) return;

        // Don't waste a potion if already topped up and no heal is pending
        if (playerHealth.Gethealth() >= playerHealth.GetMaxHealth() && GetTotalPending() <= 0f)
            return;

        if (!inventory.UsePotion(slotIndex, out PotionData potion)) return; // no potion in that slot

        activeHeals.Add(new ActiveHeal
        {
            remainingAmount = potion.healAmount,
            perTickAmount = potion.healAmount / (potion.healDuration / potion.tickInterval),
            tickInterval = potion.tickInterval,
            tickTimer = 0f
        });

        if (potionUseSound != null)
            AudioSource.PlayClipAtPoint(potionUseSound, transform.position);

        UpdateGhostFill();
    }

    private float GetTotalPending()
    {
        float total = 0f;
        for (int i = 0; i < activeHeals.Count; i++)
            total += activeHeals[i].remainingAmount;
        return total;
    }

    private void UpdateGhostFill()
    {
        if (ghostFillImage == null || playerHealth == null) return;

        float totalPending = GetTotalPending();

        // Hide the ghost bar entirely while no heal is in progress
        ghostFillImage.transform.localScale = totalPending > 0f ? Vector3.one : Vector3.zero;
        if (totalPending <= 0f) return;

        float maxHp = playerHealth.GetMaxHealth();
        if (maxHp <= 0f) return;

        float projected = playerHealth.Gethealth() + totalPending;
        ghostFillImage.fillAmount = Mathf.Clamp01(projected / maxHp);
    }
}
