using UnityEngine;

public class PositionReset : MonoBehaviour
{
    [SerializeField] private Transform objectToReset;
    [SerializeField] private Transform resetTo;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            objectToReset.position = resetTo.position;
            objectToReset.rotation = resetTo.rotation;
        }
    }
}
