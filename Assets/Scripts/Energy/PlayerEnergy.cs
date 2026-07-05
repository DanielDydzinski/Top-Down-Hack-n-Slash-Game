using UnityEngine;
using System;
using System.Collections;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField]private float currentEnergy;

    [Header("Block Energy Generation")]
    [Tooltip("Energy granted instantly whenever a hit is successfully blocked.")]
    [SerializeField] private float energyOnBlockSuccess = 10f;
    [Tooltip("Energy granted per tick while block is held, independent of being hit.")]
    [SerializeField] private float blockHoldRegenAmount = 2f;
    [Tooltip("Seconds between each passive regen tick while block is held.")]
    [SerializeField] private float blockHoldRegenRate = 1f;

    private Health health;
    private Coroutine blockRegenCo;

    // Action that the UI can listen to whenever energy levels shift
    public event Action<float, float> OnEnergyChanged;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (health != null) health.OnBlockSuccess += HandleBlockSuccess;
    }

    void OnDisable()
    {
        if (health != null) health.OnBlockSuccess -= HandleBlockSuccess;
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        UpdateUI();
    }

    private void HandleBlockSuccess(HitInfo info)
    {
        if (energyOnBlockSuccess > 0f) GainEnergy(energyOnBlockSuccess);
    }

    /// <summary>Called by BlockState while block is held; starts the passive regen tick.</summary>
    public void BeginBlockRegen()
    {
        if (blockRegenCo != null) StopCoroutine(blockRegenCo);
        blockRegenCo = StartCoroutine(BlockRegenLoop());
    }

    /// <summary>Called by BlockState when block is released; stops the passive regen tick.</summary>
    public void EndBlockRegen()
    {
        if (blockRegenCo != null)
        {
            StopCoroutine(blockRegenCo);
            blockRegenCo = null;
        }
    }

    private IEnumerator BlockRegenLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(blockHoldRegenRate);
            if (blockHoldRegenAmount > 0f) GainEnergy(blockHoldRegenAmount);
        }
    }

    public bool CanAfford(float cost)
    {
        return currentEnergy >= cost;
    }

    public void UseEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy - amount, 0f, maxEnergy);
        UpdateUI();
    }

    public void GainEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Passes current energy and max energy to any registered UI listener
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }
}