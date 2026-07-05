using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using System.Linq; // Added for Sorting

public class MeleeAttackBehavaiour : MonoBehaviour
{
    // The structured settings containing data from the scriptable object asset instances
    private BaseAbilitySettings baseSettings;
    private MeleeAttackSettings meleeSettings;

    public List<Effect> effects = new List<Effect>();
    private GameObject caster; // who casted this ability

    private AudioSource audioSource;

    private Ability sourceAbility;

    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    // UPDATED: Now receives the structured packages cleanly from the cast sequence
    // UPDATED: Ensure this matches the 4-argument call from MeleeAttackAbility
    public void Initialize(Ability aSourceAbility,BaseAbilitySettings aBaseSettings, MeleeAttackSettings aMeleeSettings, List<Effect> aeffect, GameObject aCaster)
    {
        baseSettings = aBaseSettings;
        meleeSettings = aMeleeSettings;
        effects = aeffect;
        caster = aCaster;
        sourceAbility = aSourceAbility; 
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        CastHitBox();
    }

    private void CastHitBox()
    {
        List<Collider> allColliders = new List<Collider>();
        Dictionary<Collider, Vector3> hitPoints = new Dictionary<Collider, Vector3>();

        // 1. Catch anything the box STARTS inside
        Collider[] overlapping = Physics.OverlapBox(
            transform.position, meleeSettings.halfExtents, transform.rotation, baseSettings.targetLayer);

        foreach (Collider col in overlapping)
        {
            allColliders.Add(col);
            hitPoints[col] = GetTrueImpactPoint(col, transform.position, Vector3.zero);
        }

        // 2. Catch anything hit during the sweep (BoxCast)
        RaycastHit[] hits = Physics.BoxCastAll(
            transform.position, meleeSettings.halfExtents, transform.forward,
            transform.rotation, meleeSettings.length, baseSettings.targetLayer);

        foreach (RaycastHit h in hits)
        {
            if (!allColliders.Contains(h.collider))
            {
                allColliders.Add(h.collider);
                hitPoints[h.collider] = GetTrueImpactPoint(h.collider, transform.position, h.point);
            }
        }

        var sortedColliders = allColliders
            .OrderBy(c => Vector3.Distance(transform.position, c.transform.position))
            .ToList();

        int hitCount = 0;

        foreach (Collider col in sortedColliders)
        {
            if (hitCount >= meleeSettings.howManyEnemiesToHit) break;

            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable == null) continue;

            EntityIdentity identity = col.GetComponent<EntityIdentity>();
            if (identity != null && identity.faction == baseSettings.myFaction) continue;

            // --- SMART LINE-OF-SIGHT (WALL) CHECK ---
            Vector3 targetPoint = hitPoints.ContainsKey(col) ? hitPoints[col] : col.bounds.center;
            Vector3 originPoint = transform.position;
            Vector3 dirToTarget = targetPoint - originPoint;
            float distToTarget = dirToTarget.magnitude;

            if (distToTarget > 0.01f)
            {
                // Trace a line from attack origin to our targeted point looking for walls
                if (Physics.Raycast(originPoint, dirToTarget.normalized, out RaycastHit wallHit, distToTarget + 0.05f, baseSettings.wallLayer))
                {
                    // CRITICAL FILTER: If we hit an environmental wall object, but it is NOT the 
                    // target we are currently trying to process, something else is obscuring our view!
                    if (wallHit.collider != col)
                    {
                        continue; // Skip this target (it's safely behind a wall or pillar!)
                    }
                }
            }
            // ----------------------------------------

            hitCount++;

            // SPAWN PARTICLES USING SNAPPED SURFACE POINT DATA
            if (meleeSettings.attackParticles != null)
            {
                Vector3 hitPoint = hitPoints.ContainsKey(col) ? hitPoints[col] : col.transform.position;
                Instantiate(meleeSettings.attackParticles, hitPoint, transform.rotation);
            }

            foreach (Effect e in effects)
            {
                if (e is TakeKnockBack kb) kb.SetExplosionPos(transform.position);
            }

            HitInfo info = new HitInfo
            {
                faction = baseSettings.myFaction,
                type = baseSettings.damageType,
                effects = effects,
                attacker = caster != null ? caster : this.gameObject,
                multiplier = 1.0f,
                forceDirection = transform.forward,
                isExplosion = false,
                impactPoint = targetPoint,
                attackType = AttackType.Melee,
                sourceAbility = this.sourceAbility,
                // Retaining the energy properties directly down the hit payload track
                energyCostPaid = baseSettings.energyCost,
                energyGainOnHit = baseSettings.energyGainOnHit,
                energyRefundOnKillPercent = baseSettings.energyRefundOnKillPercent

            };

            damageable.TakeDamage(info);
        }

        if (hitCount < 1) // we missed play woosh sound
        {
            if (caster != null)
            {
                audioSource = caster.GetComponent<AudioSource>();
                if (meleeSettings.missHitAudio != null && audioSource != null)
                {
                    audioSource.PlayOneShot(meleeSettings.missHitAudio);
                }
            }
        }

        DrawDebugBox(transform.position, meleeSettings.halfExtents, transform.rotation, transform.forward, meleeSettings.length, hitCount > 0 ? Color.green : Color.red, 2.0f);

        Destroy(gameObject);
    }

    private Vector3 GetTrueImpactPoint(Collider col, Vector3 castOrigin, Vector3 castHitPoint)
    {
        if (castHitPoint != Vector3.zero && Vector3.Distance(castHitPoint, castOrigin) > 0.05f)
        {
            return castHitPoint;
        }

        Vector3 closest = col.ClosestPoint(castOrigin);
        if (closest != Vector3.zero && closest != castOrigin)
        {
            return closest;
        }

        Vector3 dirToCenter = (col.bounds.center - castOrigin).normalized;
        Ray skinRay = new Ray(castOrigin - (dirToCenter * 0.5f), dirToCenter);

        if (col.Raycast(skinRay, out RaycastHit skinHit, 20f))
        {
            return skinHit.point;
        }

        return col.bounds.ClosestPoint(castOrigin);
    }

    private void DrawDebugBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color, float duration)
    {
        direction.Normalize();
        Vector3 startCenter = origin;
        Vector3 endCenter = origin + (direction * distance);
        Vector3[] corners = new Vector3[8];
        Vector3 h = halfExtents;

        Vector3[] localCorners = {
            new Vector3( h.x,  h.y,  h.z), new Vector3(-h.x,  h.y,  h.z),
            new Vector3( h.x, -h.y,  h.z), new Vector3(-h.x, -h.y,  h.z),
            new Vector3( h.x,  h.y, -h.z), new Vector3(-h.x,  h.y, -h.z),
            new Vector3( h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z)
        };

        for (int i = 0; i < 8; i++) corners[i] = orientation * localCorners[i];

        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(startCenter + corners[i], startCenter + corners[(i + 1) % 4], color, duration);
            Debug.DrawLine(startCenter + corners[i + 4], startCenter + corners[((i + 1) % 4) + 4], color, duration);
            Debug.DrawLine(startCenter + corners[i], startCenter + corners[i + 4], color, duration);
            Debug.DrawLine(endCenter + corners[i], endCenter + corners[(i + 1) % 4], color, duration);
            Debug.DrawLine(endCenter + corners[i + 4], endCenter + corners[((i + 1) % 4) + 4], color, duration);
            Debug.DrawLine(endCenter + corners[i], endCenter + corners[i + 4], color, duration);
            Debug.DrawLine(startCenter + corners[i], endCenter + corners[i], color, duration);
            Debug.DrawLine(startCenter + corners[i + 4], endCenter + corners[i + 4], color, duration);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, meleeSettings.halfExtents * 2);
        Gizmos.DrawWireCube(Vector3.forward * meleeSettings.length, meleeSettings.halfExtents * 2);
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * meleeSettings.length);
    }
}