using UnityEngine;

public class AbilityDestructionReaction : MonoBehaviour
{
    public void HandleAbilityFizzle(HitInfo info)
    {
        // 1. Stop spawning hazards instantly
        if (TryGetComponent<AoEoTBehaviour>(out var behaviour))
        {
            behaviour.enabled = false;
        }

        // 2. You can stop particle systems cleanly instead of jarringly destroying them
        var systems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.loop = false; // Let existing particles fade out naturally
        }

        // 3. Destroy the actual root object after a brief delay so effects look natural
        Destroy(gameObject, 0.5f);
    }
}