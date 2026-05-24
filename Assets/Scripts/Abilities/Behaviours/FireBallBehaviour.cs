using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FireBallBehaviour : MonoBehaviour
{
    [Header("Beam Functionality")]
    public bool isBeam = false;
    public float beamEnginePower = 100f;
    public float pushForceMultiplier = 1f;

    [Header("Original Settings")]
    public GameObject projectile;
    public Ability explosionAbility;
    public AudioClip fireBallAudioClip;
    public string targetTag;
    public string friendTag;
    public Faction myFaction;
    public DamageType damageType;
    public float projectileSpeed;
    public float projectileSize;
    public float projectileRange;
    public GameObject caster;

    public SphereCollider sc;
    public List<Effect> effects = new List<Effect>();

    private Vector3 spawnPos;
    private int howManyExplosions;
    private AudioSource audioSource;
    private float currentEffectiveSpeed;

    // The single list tracking everything via Rigidbody
    private List<Rigidbody> capturedMasses = new List<Rigidbody>();

    void Start()
    {
        howManyExplosions = 0;
        spawnPos = this.transform.position;
        currentEffectiveSpeed = projectileSpeed;

        if (isBeam)
        {
            SphereCollider oldSc = GetComponent<SphereCollider>();
            if (oldSc != null) Destroy(oldSc);

            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(projectileSize * 2f, projectileSize, projectileSize);
        }
        else
        {
            sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = projectileSize;
            sc.isTrigger = true;
        }

        if (caster != null)
        {
            CharacterController playerCC = caster.GetComponent<CharacterController>();
            Collider fireballCollider = GetComponent<Collider>();

            if (playerCC != null && fireballCollider != null)
            {
                Physics.IgnoreCollision(playerCC, fireballCollider);
            }
        }

        audioSource = GetComponent<AudioSource>();
        StartCoroutine(DelayedInstanciate());
    }

    void Update()
    {
        if (isBeam) CalculateBeamSpeed();
        else currentEffectiveSpeed = projectileSpeed;

        transform.Translate(Vector3.forward * currentEffectiveSpeed * Time.deltaTime);
        DestroyByDistance();
    }

    private void CalculateBeamSpeed()
    {
        float totalMass = 0;
        capturedMasses.RemoveAll(rb => rb == null);
        foreach (Rigidbody rb in capturedMasses) totalMass += rb.mass;
        float weightFactor = beamEnginePower / (beamEnginePower + totalMass);
        currentEffectiveSpeed = projectileSpeed * weightFactor;
    }

    void OnTriggerEnter(Collider col)
    {


        if (col.gameObject.layer == LayerMask.NameToLayer("Enviroment"))
        {
            if (explosionAbility != null && howManyExplosions < 1)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                howManyExplosions++;
            }
            ReleaseAllCaptured();

            Destroy(this.gameObject);
            return;
        }

        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EntityIdentity victimIdentity = col.GetComponent<EntityIdentity>();
            if (victimIdentity != null && victimIdentity.faction == myFaction) return;

            if (isBeam)
            {
                if (col.TryGetComponent<Rigidbody>(out var rb))
                {
                    CaptureEnemy(rb);
                }
            }
            else if (howManyExplosions < 1) // Original single fireball logic
            {

                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    HitInfo info = new HitInfo
                    {
                        faction = myFaction,
                        multiplier = 1.0f,
                        type = damageType,
                        effects = this.effects,
                        attacker = this.gameObject,
                        isExplosion = false
                    };
                    damageable.TakeDamage(info);
                }

                if (explosionAbility != null)
                {
                    explosionAbility.Cast(transform.position, Quaternion.identity, caster);                    
                }

                howManyExplosions++;
                Destroy(this.gameObject);
            }
        }
    }

    private void DealDamage()
    {

    }

    void OnTriggerStay(Collider col)
    {
        if (!isBeam) return;

        CheckForEnvironmentWallAhead();

        // Continuous stun evaluation while they remain inside the beam volume
        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            if (col.TryGetComponent<EnemyAIController>(out var controller))
            {
                // Only apply stun if the enemy isn't already actively stunned
                if (!controller.IsCurrentlyStunned)
                {
                    controller.ApplyStun(1.0f);
                }
            }
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (isBeam && col.TryGetComponent<Rigidbody>(out var rb))
        {
            ReleaseEnemy(rb);
        }
    }

    private void CaptureEnemy(Rigidbody rb)
    {
        if (capturedMasses.Contains(rb)) return;

        capturedMasses.Add(rb);

        // Utilize your existing EnemyAIController to trigger the StunState natively
        if (rb.gameObject.TryGetComponent<EnemyAIController>(out var controller))
        {
            controller.ApplyStun(1.0f);
        }
        if (rb.gameObject.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.enabled = false;
        }

            // Parent directly to the fireball beam for a zero-jitter, smooth sweep
            rb.transform.SetParent(this.transform, true);
    }

    private void ReleaseEnemy(Rigidbody rb)
    {
        if (rb == null) return;

        // 1. Break the parent connection
        rb.transform.SetParent(null);

        // 2. Safely re-anchor the NavMeshAgent to the closest valid navigation point
        if (rb.gameObject.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            agent.enabled = true;
            // If the agent drifted off the mesh while traveling with the fireball,
            // this forces it back onto walkable ground before it tries to resume pathfinding.
            if (UnityEngine.AI.NavMesh.SamplePosition(rb.transform.position, out UnityEngine.AI.NavMeshHit hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        if (capturedMasses.Contains(rb))
        {
            capturedMasses.Remove(rb);
        }
    }

    private void ReleaseAllCaptured()
    {
        for (int i = capturedMasses.Count - 1; i >= 0; i--)
        {
            ReleaseEnemy(capturedMasses[i]);
        }
        capturedMasses.Clear();
    }

    private void DestroyByDistance()
    {
        if (Vector3.Distance(spawnPos, transform.position) > projectileRange)
        {
            if (explosionAbility != null && howManyExplosions < 1)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                howManyExplosions++;
            }
            ReleaseAllCaptured();
            Destroy(this.gameObject);
        }
    }

    private void CheckForEnvironmentWallAhead()
    {
        if (!isBeam) return;

        // Cast a sphere or ray forward from the beam center
        // Adjust the distance threshold based on your projectileSize/enemy spacing
        float checkDistance = projectileSize + 1f;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, checkDistance, 1 << LayerMask.NameToLayer("Enviroment")))
        {

            // Hit a wall ahead! Detonate right now before pushing enemies through it
            if (explosionAbility != null && howManyExplosions <1)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity, caster);
                howManyExplosions++;
            }

            ReleaseAllCaptured();
            Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        // Safely unparent everyone before the beam object is completely cleaned up
        ReleaseAllCaptured();
    }

    public void UpdateValues(List<Effect> aeffects, float aprojSpeed, float aprojSize, float aprojRange, GameObject aproj, Ability aexplo, Faction faction, DamageType dmgType,
                             AudioClip aclip, bool isbeam, GameObject whoCasted)
    {
        effects = aeffects; projectileSpeed = aprojSpeed; projectileSize = aprojSize; projectileRange = aprojRange;
        projectile = aproj; explosionAbility = aexplo; myFaction = faction; damageType = dmgType; fireBallAudioClip = aclip;
        isBeam = isbeam; caster = whoCasted;
    }

    private IEnumerator DelayedInstanciate()
    {
        yield return null;
        if (projectile != null)
        {
            Instantiate(projectile, this.gameObject.transform);
            if (fireBallAudioClip != null && audioSource != null) audioSource.PlayOneShot(fireBallAudioClip);
        }
    }
}