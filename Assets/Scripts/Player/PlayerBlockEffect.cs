using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages cosmetic and audio feedback for successful block interactions. 
/// Listens to the central Health script, prioritizes physical impact points,
/// and plays targeted sound elements based on the incoming damage type.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(AudioSource))] // Guarantees an AudioSource exists on the player
public class PlayerBlockEffects : MonoBehaviour
{
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject blockSparkPrefab;

    [Header("Fallback Spawn Settings")]
    [SerializeField] private Transform shieldHookTransform;
    [SerializeField] private float forwardOffset = 0.6f;
    [SerializeField] private float heightOffset = 1.2f;

    [Header("SFX Audio Lists")]
    [Tooltip("Pool of sounds to randomly pick from when successfully blocking a melee strike.")]
    [SerializeField] private List<AudioClip> meleeBlockSounds = new List<AudioClip>();

    // Future placeholders for your upcoming systems:
    // [SerializeField] private List<AudioClip> rangedBlockSounds = new List<AudioClip>();
    // [SerializeField] private List<AudioClip> magicBlockSounds = new List<AudioClip>();

    private Health playerHealth;
    private AudioSource audioSource;

    void Awake()
    {
        playerHealth = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();

        // Optional: Optimize the AudioSource settings for clean sound overlapping
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // Sets to 3D spatial sound (0 is 2D, 1 is 3D)
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnBlockSuccess += HandleBlockFeedback;
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnBlockSuccess -= HandleBlockFeedback;
        }
    }

    private void HandleBlockFeedback(HitInfo info)
    {
        // --- 1. AUDIO HANDLING LAYOUT ---
      
            // Detect if the hit type matches a melee attack patterns
            if (info.attackType == AttackType.Melee)
            {
                PlayRandomSound(meleeBlockSounds);
            }
            // Future extension ready:
            // else if (info.attackType == AttackType.Ranged) { PlayRandomSound(rangedBlockSounds); }
          // else if (info.attackType == AttackType.Magic) { PlayRandomSound(magicBlockSounds); }
        

        // --- 2. VFX POSITION SELECTION (Impact Point Prioritized) ---
        if (blockSparkPrefab == null) return;

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if ( info.impactPoint != Vector3.zero)
        {
            spawnPosition = info.impactPoint;
        }
        else if (shieldHookTransform != null)
        {
            spawnPosition = shieldHookTransform.position;
        }
        else
        {
            spawnPosition = transform.position + (transform.forward * forwardOffset) + (Vector3.up * heightOffset);
        }

        // --- ROTATION TRACKING ---
        if ( info.attacker != null)
        {
            Vector3 dirToAttacker = (info.attacker.transform.position - spawnPosition).normalized;
            spawnRotation = Quaternion.LookRotation(dirToAttacker);
        }
        else
        {
            spawnRotation = transform.rotation;
        }

        // Spawn visual instance and clean up scene memory
        GameObject sparkInstance = Instantiate(blockSparkPrefab, spawnPosition, spawnRotation);
        Destroy(sparkInstance, 2f);
    }

    /// <summary>
    /// Selects a completely random audio clip from a provided collection and plays it cleanly.
    /// </summary>
    private void PlayRandomSound(List<AudioClip> clipList)
    {
        if (clipList == null || clipList.Count == 0)
        {
            Debug.LogWarning($"[AUDIO] Attempted to play block sound, but the audio clip list is empty!");
            return;
        }

        // Grab a random index from the list bounds
        int randomIndex = Random.Range(0, clipList.Count);
        AudioClip chosenClip = clipList[randomIndex];

        if (chosenClip != null)
        {
            // PlayOneShot allows sound effects to overlap naturally without cutting each other off!
            audioSource.PlayOneShot(chosenClip);
        }
    }
}