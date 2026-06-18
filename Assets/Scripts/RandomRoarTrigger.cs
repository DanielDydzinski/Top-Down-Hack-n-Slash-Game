using UnityEngine;

public class RandomRoarTrigger : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] roarClips; // Array to hold multiple roar sounds

    [Header("Timer Settings")]
    [SerializeField] private float minTime = 5f;
    [SerializeField] private float maxTime = 15f;

    [Header("Animator Parameter")]
    [SerializeField] private string parameterName = "doRoar";

    private float timer;
    private float nextRoarTime;

    private void Start()
    {
        // Cache components automatically if left empty in the Inspector
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        CalculateNextRoarTime();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextRoarTime)
        {
            TriggerRoar();
        }
    }

    private void TriggerRoar()
    {
        // Trigger the animation
        if (animator != null)
        {
            animator.SetTrigger(parameterName);
        }

        // Play a random sound from the list
        PlayRandomRoar();

        // Reset the cycle
        timer = 0f;
        CalculateNextRoarTime();
    }

    private void PlayRandomRoar()
    {
        // Safety check to ensure the list is assigned and not empty
        if (audioSource != null && roarClips != null && roarClips.Length > 0)
        {
            // Pick a random index between 0 and the size of the array
            int randomIndex = Random.Range(0, roarClips.Length);

            // Play the selected clip
            audioSource.PlayOneShot(roarClips[randomIndex]);
        }
    }

    private void CalculateNextRoarTime()
    {
        nextRoarTime = Random.Range(minTime, maxTime);
    }
}
