using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KeyDoor : MonoBehaviour
{
    [Header("Door Status")]
    public bool requiresKey = true;

    [Tooltip("Drag the matching KeyData asset that opens this specific door.")]
    public KeyData requiredKey;

    [Header("Settings")]
    public bool consumeKeyOnUse = true;

    [Header("Animation Reference")]
    [Tooltip("Drag the child object containing the Animator component here.")]
    public Animator doorAnimator;

    [Tooltip("The exact name of the Trigger parameter inside your Animator Controller.")]
    public string openTriggerName = "Open";

    [Header("Audio Settings")]
    [Tooltip("Toggle this off if you want the door to open instantly without an unlocking phase.")]
    public bool useUnlockDelaySequence = true;

    [Tooltip("Sound played the exact moment the key is validated (e.g., latch click). Only plays if 'Use Unlock Delay Sequence' is checked.")]
    public AudioClip unlockSound;

    [Tooltip("Sound played when the door physically begins swinging open (e.g., creaking).")]
    public AudioClip doorOpenSound;

    [Tooltip("How many seconds to wait after the unlock sound before the door starts moving. Only applies if 'Use Unlock Delay Sequence' is checked.")]
    public float delayBeforeOpening = 0.5f;

    private AudioSource audioSource;
    private bool isOpened = false;

    private void Start()
    {
        // Automatically fetch the AudioSource component on this object
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened) return;

        if (other.CompareTag("Player"))
        {
            // If the door doesn't need a key, skip straight to the opening sequence
            if (!requiresKey)
            {
                StartCoroutine(OpenDoorSequence(shouldPlayUnlock: false));
                return;
            }

            PlayerInventory inventory = other.GetComponent<PlayerInventory>();

            if (inventory != null && inventory.HasKey(requiredKey))
            {
                if (consumeKeyOnUse)
                {
                    inventory.RemoveKey(requiredKey);
                }

                // Run the sequence using your choice from the inspector checkbox
                StartCoroutine(OpenDoorSequence(shouldPlayUnlock: useUnlockDelaySequence));
            }
            else
            {
                string lockedMessage = requiredKey != null ? requiredKey.keyName : "a key";
                Debug.Log($"The door is locked! You need: {lockedMessage}");
            }
        }
    }

    private IEnumerator OpenDoorSequence(bool shouldPlayUnlock)
    {
        isOpened = true;

        // Only play the unlock sound and apply the delay if the option is enabled AND a clip is provided
        if (shouldPlayUnlock && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
            yield return new WaitForSeconds(delayBeforeOpening);
        }

        // Play the door creak/sliding sound
        if (doorOpenSound != null)
        {
            audioSource.PlayOneShot(doorOpenSound);
        }

        // Fire the animator trigger to physically move the door
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(openTriggerName);
        }
        else
        {
            Debug.LogWarning("DoorAnimator reference is missing on " + gameObject.name);
        }
    }
}
