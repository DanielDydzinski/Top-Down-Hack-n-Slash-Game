using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("Key Configuration")]
    [Tooltip("Drag the specific KeyData(scriptableObject) asset from your project folder here.")]
    public KeyData keyType;
    [Tooltip("the sound played when picked up key")]
    public AudioClip keyPickUpSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();

            if (inventory != null && keyType != null)
            {
                inventory.AddKey(keyType);
                AudioSource.PlayClipAtPoint(keyPickUpSound, transform.position);
                Destroy(gameObject); // Remove the physical key from the scene
            }
        }
    }
}
