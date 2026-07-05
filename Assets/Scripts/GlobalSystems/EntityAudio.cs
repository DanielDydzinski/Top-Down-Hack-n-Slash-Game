using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))] // Changed to guarantee a Health component exists
public class EntityAudio : MonoBehaviour
{
    [System.Serializable]
    public class DamageSoundMapping
    {
        public DamageType damageType;
        [Tooltip("Clips to choose from randomly when this damage type lands")]
        public AudioClip[] clips;
    }

    private Health health; // Changed from DamageReceiver
    private AudioSource audioSource;

    [Header("Hit Sounds")]
    [Tooltip("Per damage-type sound sets. First matching entry with clips assigned wins.")]
    public List<DamageSoundMapping> damageSounds = new List<DamageSoundMapping>();

    [Tooltip("Used when no mapping matches the incoming damage type (or its clip list is empty)")]
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

        AudioClip clip = GetClipForDamageType(info.type);
        if (clip == null) return;

        // 1. Roll the dice
        float roll = Random.value;
        if (roll > playChance) return;

        // 2. Randomize Pitch
        audioSource.pitch = originalPitch + Random.Range(-pitchVariation, pitchVariation);

        // 3. Play
        if (!info.isBlocked)
        {
            audioSource.PlayOneShot(clip);
        }

    }

    private AudioClip GetClipForDamageType(DamageType type)
    {
        for (int i = 0; i < damageSounds.Count; i++)
        {
            DamageSoundMapping mapping = damageSounds[i];
            if (mapping.damageType == type && mapping.clips != null && mapping.clips.Length > 0)
                return mapping.clips[Random.Range(0, mapping.clips.Length)];
        }

        // Fallback: no specific mapping for this damage type
        if (hitSounds != null && hitSounds.Length > 0)
            return hitSounds[Random.Range(0, hitSounds.Length)];

        return null;
    }
}