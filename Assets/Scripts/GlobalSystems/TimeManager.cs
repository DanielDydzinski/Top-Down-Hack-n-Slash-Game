using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Slow Motion Settings")]
    [Tooltip("The lowest time scale reached during the effect (e.g., 0.2f is 20% normal speed).")]
    [SerializeField] private float slowMoTimeScale = 0.3f;

    [Tooltip("How long the game stays in slow motion before starting to recover.")]
    [SerializeField] private float slowMoDuration = 0.5f;

    [Tooltip("How fast time transitions back to 1.0f normal speed.")]
    [SerializeField] private float recoverySpeed = 2f;

    private Coroutine _slowMoCoroutine;
    private float _initialFixedDeltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cache Unity's default physics step size (usually 0.02)
        _initialFixedDeltaTime = Time.fixedDeltaTime;
    }

    /// <summary>
    /// Call this from anywhere to trigger a juicy slow-mo sequence.
    /// </summary>
    public void TriggerSlowMotion()
    {
        // If a slow-mo is already running, stop it so they don't stack up awkwardly
        if (_slowMoCoroutine != null)
        {
            StopCoroutine(_slowMoCoroutine);
        }

        _slowMoCoroutine = StartCoroutine(SlowMoSequence());
    }

    private IEnumerator SlowMoSequence()
    {
        // 1. Instantly snap into slow motion for that high-impact punch feel
        Time.timeScale = slowMoTimeScale;
        UpdatePhysicsDeltaTime();

        // 2. Hold it there for a brief moment using unscaled real-world time
        yield return new WaitForSecondsRealtime(slowMoDuration);

        // Wait using unscaled time, otherwise the countdown itself is slowed down!
        float elapsed = 0f;
        while (Time.timeScale < 1.0f)
        {
            // Smoothly slide time back up to normal
            Time.timeScale += Time.unscaledDeltaTime * recoverySpeed;
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1.0f);

            UpdatePhysicsDeltaTime();
            yield return null;
        }

        // 3. Guarantee a clean reset back to flawless normal states
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = _initialFixedDeltaTime;
    }

    private void UpdatePhysicsDeltaTime()
    {
        // Keeps your ragdolls and exploding physics buttery smooth while slowed down
        Time.fixedDeltaTime = _initialFixedDeltaTime * Time.timeScale;
    }
}