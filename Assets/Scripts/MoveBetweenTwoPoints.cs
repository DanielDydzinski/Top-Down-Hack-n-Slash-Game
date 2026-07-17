using UnityEngine;

public class MoveBetweenTwoPoints : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private Transform objectToMove;

    [SerializeField] private bool useSlerp = false;
    [SerializeField] private float speed = 1f;
    [SerializeField] private bool pingPong = true;

    private float t = 0f;
    private int direction = 1;

    private void Reset()
    {
        objectToMove = transform;
    }

    private void Update()
    {
        if (pointA == null || pointB == null || objectToMove == null) return;

        t += direction * speed * Time.deltaTime;

        if (t >= 1f)
        {
            t = 1f;
            if (pingPong) direction = -1;
            else t = 0f;
        }
        else if (t <= 0f)
        {
            t = 0f;
            direction = 1;
        }

        objectToMove.position = useSlerp
            ? SlerpMove(pointA.position, pointB.position, t)
            : LerpMove(pointA.position, pointB.position, t);
    }

    private Vector3 LerpMove(Vector3 a, Vector3 b, float t)
    {
        return Vector3.Lerp(a, b, t);
    }

    private Vector3 SlerpMove(Vector3 a, Vector3 b, float t)
    {
        Vector3 center = (a + b) * 0.5f;
        center -= Vector3.up; // pivot below the midpoint so the arc bulges upward
        return Vector3.Slerp(a - center, b - center, t) + center;
    }
}
