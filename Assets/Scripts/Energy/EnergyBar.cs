using UnityEngine;
using UnityEngine.UI; // Required for Image components

public class EnergyBarUI : MonoBehaviour
{
    [SerializeField] private PlayerEnergy playerEnergySource;
    [SerializeField] private Image energyFillImage;

    void Awake()
    {
      
    }

    void OnEnable()
    {
        if (playerEnergySource != null)
        {
            playerEnergySource.OnEnergyChanged += HandleEnergyChanged;
        }
    }

    void OnDisable()
    {
        if (playerEnergySource != null)
        {
            playerEnergySource.OnEnergyChanged -= HandleEnergyChanged;
        }
    }

    private void HandleEnergyChanged(float currentEnergy, float maxEnergy)
    {
        if (energyFillImage != null && maxEnergy > 0)
        {
            // Set the fill amount between 0.0f and 1.0f
            energyFillImage.fillAmount = currentEnergy / maxEnergy;
        }
    }
}