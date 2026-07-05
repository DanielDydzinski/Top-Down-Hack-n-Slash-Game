using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PandoraBox : MonoBehaviour
{
    [Header("Visual Swap")]
    [SerializeField] private MeshRenderer idleBox;
    [SerializeField] private GameObject crashedBox;
    [SerializeField] private BoxCollider boxCollider;

    [Header("Loot Settings")]
    [SerializeField] private List<Rigidbody> lootItems = new List<Rigidbody>();
    [SerializeField] private float popForce = 5f;
    [SerializeField] private float upwardModifier = 3f;
    [SerializeField] private float delayBetweenPops = 0.15f;
    [SerializeField] private Vector3 itemSpawnOffset = Vector3.up;

    /// <summary>
    /// This is the main function you will link to your Unity Event dropdown menu.
    /// </summary>
    /// 
    public void OnEnable()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (idleBox != null) idleBox.enabled = true;
        if (crashedBox != null) crashedBox.SetActive(false);
    }

    public void PopBox()
    {
        // 1. Swap the meshes
        boxCollider.enabled = false;
        if (idleBox != null) idleBox.enabled = false;
        if (crashedBox != null) crashedBox.SetActive(true);
        // 2. Start popping items out one by one
        StartCoroutine(PopItemsRoutine());
    }

    private IEnumerator PopItemsRoutine()
    {
        foreach (Rigidbody item in lootItems)
        {
            if (item == null) continue;

            // Ensure the item is active in the scene
            item.gameObject.SetActive(true);
            item.gameObject.transform.position = transform.position + itemSpawnOffset;

            // Generate a random outward direction spread (X and Z coordinates)
            float randomX = Random.Range(-0.5f, 0.5f);
            float randomZ = Random.Range(-0.5f, 0.5f);

            // Combine outward directions with a strong upward burst
            Vector3 forceDirection = new Vector3(randomX, upwardModifier, randomZ).normalized;

            item.isKinematic = false;
            item.useGravity = true;
            // Apply physical force to shoot the item out
            item.AddForce(forceDirection * popForce, ForceMode.Impulse);

            // Wait a brief moment before launching the next item
            yield return new WaitForSeconds(delayBetweenPops);
        }
    }
}
