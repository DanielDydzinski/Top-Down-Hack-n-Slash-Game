using UnityEngine;

public class ChasingBeam : MonoBehaviour
{
    public LineRenderer line;
    public float beamLength = 5.0f; // Max length before tail starts chasing
    private Vector3 startPoint;

    void Start()
    {
        startPoint = transform.position;
        if (line == null) line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }

    void Update()
    {
        // 1. Calculate current distance
        float currentDist = Vector3.Distance(startPoint, transform.position);

        // 2. If we exceed the beam length, move the tail forward
        if (currentDist > beamLength)
        {
            // This makes the tail "chase" the head at a set distance
            startPoint = transform.position + (startPoint - transform.position).normalized * beamLength;
        }

        // 3. Update Line Renderer positions
        line.SetPosition(0, startPoint);      // The "Tail"
        line.SetPosition(1, transform.position); // The "Head"
    }
}
