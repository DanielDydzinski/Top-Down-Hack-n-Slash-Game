using UnityEngine;

public class CarcassData : MonoBehaviour
{
    public enum CarcassSize { Small, Large }

    [Header("Carcass Settings")]
    public CarcassSize size = CarcassSize.Small;

    [Header("Custom Effects")]
    [Tooltip("The specific debris chunks or blood burst prefab unique to this enemy type.")]
    public GameObject customDebrisPrefab;

}