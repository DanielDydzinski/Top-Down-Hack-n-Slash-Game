using UnityEngine;

public class CarcassCruncher : MonoBehaviour
{
    [System.Serializable]
    public struct CrunchEffects
    {
        public GameObject particlePrefab;
        public AudioClip[] sounds;
    }

    [Header("Carcass Size Effects")]
    [SerializeField] private CrunchEffects smallCarcassEffects;
    [SerializeField] private CrunchEffects largeCarcassEffects;

    [Header("Layer Names")]
    [SerializeField] private string ragdollLayerName = "Ragdoll";
    [SerializeField] private string floorLayerName = "Floor";

    private bool isGrounded;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Check if the foot has touched the floor
        if (other.gameObject.layer == LayerMask.NameToLayer(floorLayerName))
        {
            isGrounded = true;
        }

        // 2. Check if we stepped on a carcass AND the foot is on the floor
        if (/*isGrounded && */other.gameObject.layer == LayerMask.NameToLayer(ragdollLayerName))
        {
            CrunchCarcass(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Reset the floor check when the foot lifts up
        if (other.gameObject.layer == LayerMask.NameToLayer(floorLayerName))
        {
            isGrounded = false;
        }
    }

    private void CrunchCarcass(GameObject carcassWindow)
    {
        Vector3 spawnPosition = transform.position;

        // Look for the CarcassData component on the object we hit, or its parent
        CarcassData carcassData = carcassWindow.GetComponentInParent<CarcassData>();

        // Default to small if no component is found, otherwise use the selected size
        CarcassData.CarcassSize size = (carcassData != null) ? carcassData.size : CarcassData.CarcassSize.Small;

        // Choose effects based on the size
        CrunchEffects effectsToUse = (size == CarcassData.CarcassSize.Large) ? largeCarcassEffects : smallCarcassEffects;

        // Spawn the correct particle effect
        if (effectsToUse.particlePrefab != null)
        {
            Instantiate(effectsToUse.particlePrefab, spawnPosition, Quaternion.identity);
        }

        // Play a random sound from the correct sound array
        PlayRandomCrunchSound(effectsToUse.sounds, spawnPosition);

        // Destroy the carcass (Deletes the whole ragdoll structure if CarcassData is at the root)
        if (carcassData != null)
        {
            Destroy(carcassData.gameObject);
        }
        else
        {
            Destroy(carcassWindow);
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