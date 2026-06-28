using UnityEngine;
using UnityEngine.UI;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Stats")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float energyPoints = 0f; // Starts empty or full based on your preference

    [Header("UI Reference")]
    [SerializeField] private Image energyFillImage;

    void Start()
    {
        UpdateEnergyBar();
    }

    public bool CanAfford(float cost)
    {
        return energyPoints >= cost;
    }

    public void UseEnergy(float amount)
    {
        energyPoints = Mathf.Max(0f, energyPoints - amount);
        UpdateEnergyBar();
    }

    public void AddEnergy(float amount)
    {
        energyPoints = Mathf.Min(maxEnergy, energyPoints + amount);
        UpdateEnergyBar();
    }

    private void UpdateEnergyBar()
    {
        if (energyFillImage != null)
        {
            energyFillImage.fillAmount = energyPoints / maxEnergy;
        }
    }

    public float GetCurrentEnergy() => energyPoints;
}