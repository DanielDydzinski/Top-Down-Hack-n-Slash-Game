using UnityEngine;

public class EnergyPulse : MonoBehaviour
{
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public float pulseSpeed = 5f;

    private Vector3 initialScale;

    void Start()
    {
        // Remember the size you set in the Inspector
        initialScale = transform.localScale;
    }

    void Update()
    {
        // Use a Sine wave to oscillate smoothly between 0 and 1
        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        // Calculate the new scale multiplier
        float currentMultiplier = Mathf.Lerp(minScale, maxScale, wave);

        // Apply it to the original scale so it keeps its shape!
        transform.localScale = initialScale * currentMultiplier;
    }
}