using UnityEngine;

public class CarcassCruncher : MonoBehaviour
{
    [Header("Health Potion Prefabs")]
    [SerializeField] private GameObject smallHealthPotionPrefab;
    [SerializeField] private GameObject largeHealthPotionPrefab;

    [Header("Drop Chances (0 to 100)")]
    [Range(0f, 100f)][SerializeField] private float smallPotionDropChance = 50f;
    [Range(0f, 100f)][SerializeField] private float largePotionDropChance = 25f;

    [Header("Global Fallback Sounds")]
    [SerializeField] private AudioClip[] crunchSounds;

    [Header("Layer Names")]
    [SerializeField] private string ragdollLayerName = "Ragdoll";
    [SerializeField] private string floorLayerName = "Floor";

    private bool isGrounded;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(floorLayerName))
        {
            isGrounded = true;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer(ragdollLayerName))
        {
            CrunchCarcass(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(floorLayerName))
        {
            isGrounded = false;
        }
    }

    private void CrunchCarcass(GameObject carcassWindow)
    {
        // ─── PIVOT FIX: CHOOSE GEOMETRIC CENTER ───
        Vector3 spawnPosition = carcassWindow.transform.position;

        if (carcassWindow.TryGetComponent<Renderer>(out Renderer meshRenderer))
        {
            spawnPosition = meshRenderer.bounds.center;
        }
        else if (carcassWindow.TryGetComponent<Collider>(out Collider col))
        {
            spawnPosition = col.bounds.center;
        }

        // Look for the CarcassData component on the object we hit, or its parent
        CarcassData carcassData = carcassWindow.GetComponentInParent<CarcassData>();

        // ─── STEP 1: SPAWN CUSTOM DEBRIS ───
        if (carcassData != null && carcassData.customDebrisPrefab != null)
        {
            // Instantiates respecting the prefab's exact native layout/rotation completely
            Instantiate(carcassData.customDebrisPrefab, spawnPosition, carcassData.customDebrisPrefab.transform.rotation);
        }

        // ─── STEP 2: ROLL FOR POTION DROPS BASED ON SIZE ───
        if (carcassData != null)
        {
            float randomRoll = Random.Range(0f, 100f);

            if (carcassData.size == CarcassData.CarcassSize.Large)
            {
                if (randomRoll <= largePotionDropChance && largeHealthPotionPrefab != null)
                {
                    Instantiate(largeHealthPotionPrefab, spawnPosition+Vector3.up, Quaternion.identity);
                }
            }
            else if (carcassData.size == CarcassData.CarcassSize.Small)
            {
                if (randomRoll <= smallPotionDropChance && smallHealthPotionPrefab != null)
                {
                    Instantiate(smallHealthPotionPrefab, spawnPosition+Vector3.up, Quaternion.identity);
                }
            }
        }

        PlayRandomCrunchSound(crunchSounds, spawnPosition);

        // ─── STEP 3: CLEANUP OLD CORPSE ───
        GameObject corpseRoot = (carcassData != null ? carcassData.transform.root : carcassWindow.transform.root).gameObject;

        // A multi-piece gib burst (ZombieExplodingParts, ZombieDebris) has one CarcassData PER
        // PIECE, all under one pooled container root - crunching one piece should only remove that
        // piece, not the whole group. A single-body ragdoll has exactly one CarcassData in its
        // hierarchy, so crunching it destroys/returns the whole corpse as before.
        bool isMultiPieceBurst = carcassData != null && corpseRoot.GetComponentsInChildren<CarcassData>(true).Length > 1;

        if (isMultiPieceBurst)
        {
            // The container's own pooled lifetime (DeathHandler/ObjectPooler) still returns the
            // whole group later; PoolInfo re-activates this piece automatically next time the
            // container is reused, so simply hiding it now is enough.
            carcassData.gameObject.SetActive(false);
        }
        else if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(corpseRoot);
        }
        else
        {
            Destroy(corpseRoot);
        }
    }

    private void PlayRandomCrunchSound(AudioClip[] clips, Vector3 position)
    {
        if (clips != null && clips.Length > 0)
        {
            int randomIndex = Random.Range(0, clips.Length);
            AudioClip randomClip = clips[randomIndex];
            AudioSource.PlayClipAtPoint(randomClip, position);
        }
    }
}