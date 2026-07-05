using UnityEngine;

//  right-click in the Project Window to create new potions
[CreateAssetMenu(fileName = "New Potion Data", menuName = "Inventory/Potion Data")]
public class PotionData : ScriptableObject
{
    [Header("Display Info")]
    public string potionName;
    public Sprite potionIcon;

    [Header("Heal Over Time")]
    public float healAmount = 40f;
    public float healDuration = 4f;
    public float tickInterval = 0.5f;
}
