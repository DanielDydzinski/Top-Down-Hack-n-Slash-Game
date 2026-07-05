using UnityEngine;

public class PanoramicCameraLook : MonoBehaviour
{
    [Header("Drag Look Settings")]
    public float sensitivity = 0.2f;
    public bool invertY = false;

    [Header("Vertical Clamp")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float yaw;
    private float pitch;
    private Vector3 lastMousePosition;
    private bool isDragging;

    private void OnEnable()
    {
        Vector3 startAngles = transform.eulerAngles;
        yaw = startAngles.y;
        pitch = startAngles.x > 180f ? startAngles.x - 360f : startAngles.x;
        isDragging = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            yaw += delta.x * sensitivity;
            pitch += (invertY ? delta.y : -delta.y) * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
