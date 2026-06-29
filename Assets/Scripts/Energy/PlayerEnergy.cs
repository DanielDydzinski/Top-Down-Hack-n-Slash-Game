using UnityEngine;
using System;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField]private float currentEnergy;

    // Action that the UI can listen to whenever energy levels shift
    public event Action<float, float> OnEnergyChanged;

    void Start()
    {
        currentEnergy = maxEnergy;
        UpdateUI();
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