using UnityEngine;

public class HealthPotionItem : MonoBehaviour
{
    [Header("Potion Configuration")]
    [Tooltip("Drag the PotionData asset that defines this potion's heal amount/duration here.")]
    public PotionData potionType;
    [Tooltip("the sound played when picked up")]
    public AudioClip potionPickUpSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();

            if (inventory != null && potionType != null)
            {
                if (inventory.AddPotion(potionType))
                {
                    if (potionPickUpSound != null)
                        AudioSource.PlayClipAtPoint(potionPickUpSound, transform.position);

                    Destroy(gameObject); // Remove the physical potion from the scene
                }
                // else: inventory is full, leave the potion in the world
            }
        }
    }
}
