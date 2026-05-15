using UnityEngine;

[RequireComponent(typeof(DamageReceiver))]
public class EntityAudio : MonoBehaviour
{
    private DamageReceiver damageReceiver;
    private AudioSource audioSource;

    [Header("Hit Sounds")]
    [Tooltip("Array of clips to choose from randomly")]
    public AudioClip[] hitSounds;

    [Header("Settings")]
    [Range(0f, 1f)]
    [Tooltip("0 = Never play, 1 = Always play")]
    public float playChance = 0.5f;

    [Range(0f, 0.3f)]
    [Tooltip("Slightly varies the pitch so the same clip sounds different")]
    public float pitchVariation = 0.1f;

    private float originalPitch;

    void Awake()
    {
        damageReceiver = GetComponent<DamageReceiver>();
        audioSource = GetComponent<AudioSource>();

        // Ensure we have an AudioSource
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        originalPitch = audioSource.pitch;
    }

    void OnEnable()
    {
        damageReceiver.OnHitReceived += TryPlayHitSound;
    }

    void OnDisable()
    {
        damageReceiver.OnHitReceived -= TryPlayHitSound;
    }

    private void TryPlayHitSound(HitInfo info)
    {
        if (hitSounds.Length == 0) return;

        // 1. Roll the dice
        float roll = Random.value; // Returns 0.0 to 1.0
        if (roll > playChance) return;

        // 2. Randomize Pitch (makes 3 sounds feel like 10)
        audioSource.pitch = originalPitch + Random.Range(-pitchVariation, pitchVariation);

        // 3. Pick a random clip
        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];

        // 4. Play
        audioSource.PlayOneShot(clip);
    }
}