using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;

public class MeleeAttackBehavaiour : MonoBehaviour
{
    public GameObject attackParticles;
    public float length;
    public Vector3 halfExtents;
    public LayerMask layerMask;
    public LayerMask wallLayer;
    public Faction myFaction;
    public DamageType damageType;
    public List<Effect> effects = new List<Effect>();
    public int howManyEnemiesToHit = 1;

    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    public void UpdateValues(Faction faction, List<Effect> aeffect, float alength, Vector3 ahalfExtents, GameObject aparticles, DamageType dmgType, int howManyHits, LayerMask targetLayer)
    {
        myFaction = faction;
        effects = aeffect;
        length = alength;
        halfExtents = ahalfExtents;
        attackParticles = aparticles;
        damageType = dmgType;
        howManyEnemiesToHit = howManyHits;
        layerMask = targetLayer;
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        CastHitBox();
    }

    private void CastHitBox()
    {
        List<Collider> allColliders = new List<Collider>();

        // Catch anything the box STARTS inside (distance = 0)
        Collider[] overlapping = Physics.OverlapBox(
            transform.position, halfExtents, transform.rotation, layerMask);
        allColliders.AddRange(overlapping);

        // Catch anything hit during the sweep
        RaycastHit[] hits = Physics.BoxCastAll(
            transform.position, halfExtents, transform.forward,
            transform.rotation, length, layerMask);

        foreach (RaycastHit h in hits)
            if (!allColliders.Contains(h.collider))
                allColliders.Add(h.collider);

        int hitCount = 0;

        foreach (Collider col in allColliders)
        {
            if (hitCount >= howManyEnemiesToHit) break;
            EntityIdentity identity = col.GetComponent<EntityIdentity>();
            if (identity != null && identity.faction == myFaction) continue;

            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                hitCount++; // Successfully hit an enemy

                if (attackParticles != null)
                {
                    Vector3 hitPoint = col.ClosestPoint(transform.position);
                    Instantiate(attackParticles, hitPoint, transform.rotation);
                }

                foreach (Effect e in effects)
                {
                    if (e is TakeKnockBack kb) kb.SetExplosionPos(transform.position);
                }

                HitInfo info = new HitInfo
                {
                    faction = myFaction,
                    type = damageType,
                    effects = effects,
                    attacker = this.gameObject,
                    multiplier = 1.0f,
                    forceDirection = transform.forward
                };

                damageable.TakeDamage(info);

            }
        }
    

    // Visual feedback for debugging: Green if something was hit, Red if not.
    DrawDebugBox(transform.position, halfExtents, transform.rotation, transform.forward, length, hitCount > 0 ? Color.green : Color.red, 2.0f);

        Destroy(gameObject);
    }



    // Helper for gameplay debugging
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
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
        Gizmos.DrawWireCube(Vector3.forward * length, halfExtents * 2);
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * length);
    }
}
