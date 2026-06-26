using UnityEngine;

//  right-click in the Project Window to create new keys
[CreateAssetMenu(fileName = "New Key Data", menuName = "Inventory/Key Data")]
public class KeyData : ScriptableObject
{
    [Header("Display Info")]
    public string keyName;
    public Sprite keyIcon; 

    [TextArea]
    public string description;
}
