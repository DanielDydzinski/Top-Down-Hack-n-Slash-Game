using UnityEngine;

public class CarcassData : MonoBehaviour
{
    // A simple enum to choose the size in the Unity Inspector
    public enum CarcassSize { Small, Large }

    [Header("Carcass Settings")]
    public CarcassSize size = CarcassSize.Small;
}