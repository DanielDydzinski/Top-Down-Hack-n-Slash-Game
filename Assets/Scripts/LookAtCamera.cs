using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Makes the canvas match the camera's rotation exactly, preventing mirroring issues
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}