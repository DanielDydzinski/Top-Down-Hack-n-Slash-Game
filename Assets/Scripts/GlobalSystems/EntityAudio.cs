using UnityEngine;

[RequireComponent(typeof(Health))] // Changed to guarantee a Health component exists
public class EntityAudio : MonoBehaviour
{
    private Health health; // Changed from DamageReceiver
    private AudioSource audioSource;

    [Header("Hit Sounds")]
    [Tooltip("Array of clips to choose from randomly")]
    public AudioClip[] hitSounds;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float playChance = 0.5f;

    [Range(0f, 0.3f)]
    public float pitchVariation = 0.1f;

    private float originalPitch;

    void Awake()
    {
        health = GetComponent<Health>(); // Cache Health instead
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        originalPitch = audioSource.pitch;
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.OnDamageTaken += TryPlayHitSound; // Hook into the finalized damage loop
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnDamageTaken -= TryPlayHitSound; // Safe clean up
        }
    }

    private void TryPlayHitSound(HitInfo info)
    {
        // This will now print the true, finalized damage calculated by your scriptable objects!
       // Debug.Log($"[AUDIO PLAYBACK] Source: {gameObject.name} | Confirmed Dmg: {info.damage}");

        if (hitSounds.Length == 0) return;

        // 1. Roll the dice
        float roll = Random.value;
        if (roll > playChance) return;

        // 2. Randomize Pitch
        audioSource.pitch = originalPitch + Random.Range(-pitchVariation, pitchVariation);

        // 3. Pick a random clip
        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];

        // 4. Play
        audioSource.PlayOneShot(clip);
    }
}