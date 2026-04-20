using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Effects/Slow", fileName = "new Slow Effect")]
public class TakeSlow : Effect
{

    public float slowAmount; // 0.5 = 50% speed
    public float slowDuration;
    public bool fadedSlow;

    public override IEnumerator ApplyEffect(GameObject target, HitInfo info)
    {
        Stats stats = target.GetComponent<Stats>();
        if (stats == null) yield break;

        float maxSpeed = stats.GetMoveSpeed();
        float slowedSpeed = maxSpeed * slowAmount;

        // Get components directly
        NavMeshAgent nva = target.GetComponent<NavMeshAgent>();
        Mover mover = target.GetComponent<Mover>();

        // Set initial slow
        SetTargetSpeed(nva, mover, slowedSpeed);

        // Handle Particles with Anchors
        GameObject particlesObj = null;
        var anchors = target.GetComponent<EffectSpawnPossitions>();
        if (effectParticles != null && anchors != null)
        {
            particlesObj = Instantiate(effectParticles, anchors.feet.position, Quaternion.identity, anchors.feet);
            Destroy(particlesObj, slowDuration);
        }

        float elapsed = 0f;
        while (elapsed < slowDuration)
        {
            elapsed += Time.deltaTime; // Respects pause/timescale

            if (fadedSlow)
            {
                // Smoothly return to maxSpeed over time
                float currentSlowSpeed = Mathf.Lerp(slowedSpeed, maxSpeed, elapsed / slowDuration);
                SetTargetSpeed(nva, mover, currentSlowSpeed);
            }
            yield return null;
        }

        // Reset to full speed at the end
        SetTargetSpeed(nva, mover, maxSpeed);
    }

    private void SetTargetSpeed(NavMeshAgent nva, Mover mover, float speed)
    {
        if (nva != null) nva.speed = speed;
        if (mover != null) mover.SetSpeed(speed);
    }
}
