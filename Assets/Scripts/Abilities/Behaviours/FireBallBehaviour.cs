using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public GameObject caster; // who cast the fireball

    public SphereCollider sc;
    public List<Effect> effects = new List<Effect>();

    private Vector3 spawnPos;
    private int howManyExplosions;
    private AudioSource audioSource;
    private float currentEffectiveSpeed;
    private List<Rigidbody> capturedMasses = new List<Rigidbody>();


   

    void Start()
    {
        howManyExplosions = 0;
        spawnPos = this.transform.position;
        currentEffectiveSpeed = projectileSpeed;

        if (isBeam)
        {
            // Remove the SphereCollider
            SphereCollider oldSc = GetComponent<SphereCollider>();
            if (oldSc != null) Destroy(oldSc);

            // Add and setup the BoxCollider (The "Bulldozer")
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;

            // We set the box size. Width and Height = size * 2. 
            // We keep Depth (Z) a bit longer so it catches enemies easier.
            bc.size = new Vector3(projectileSize *2f, projectileSize , projectileSize  );
        }
        else
        {
            // Original Sphere setup
            sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = projectileSize;
            sc.isTrigger = true;
        }


        // ignore collision with who casted it to avoid character controller 'popping'
        if (caster != null)
        {
            CharacterController playerCC = caster.GetComponent<CharacterController>();
            Collider fireballCollider = GetComponent<Collider>();

            if (playerCC != null && fireballCollider != null)
            {
                Physics.IgnoreCollision(playerCC, fireballCollider);
            }

        }

        //sc = GetComponent<SphereCollider>();
        //sc.radius = projectileSize;
        //sc.isTrigger = true;
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
            if (explosionAbility != null)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity,caster);
            }
            Destroy(this.gameObject);
        }

        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
        {

            // 1. Identity Check (Always check first so friends don't trigger anything)
            EntityIdentity victimIdentity = col.GetComponent<EntityIdentity>();

            if (victimIdentity != null && victimIdentity.faction == myFaction) return;

            // 2. If it's a Beam, add to mass list immediately
            if (isBeam && col.TryGetComponent<Rigidbody>(out var rb))
            {
                if (!capturedMasses.Contains(rb)) capturedMasses.Add(rb);
            }

            // 3. The "One-Time" Logic (Damage + Explosion + Destroy)
            // If it's NOT a beam, we do your original explosion logic once.
            // If it IS a beam, we skip this so it doesn't destroy itself.
            if (!isBeam && howManyExplosions < 1)
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
                    explosionAbility.Cast(transform.position, Quaternion.identity,caster);
                }

                howManyExplosions++;
                Destroy(this.gameObject);
            }
            else if (isBeam)
            {
                // OPTIONAL: If it's a beam, you can still apply a one-time damage hit 
                // when it first touches someone here if you want.
            }
        }
    }

    void OnTriggerStay(Collider col)
    {
        if (!isBeam) return;

        // 1. STANDARD PUSH (For things already inside the trigger)
        HandlePush(col);

        // 2. THE "SNOWBALL" EXTENSION
        // We cast a box forward from the front of the beam to find enemies in the way
        Vector3 boxCenter = transform.position + (transform.forward * projectileSize);
        // Half-extents match the size you set in Start()
        Vector3 halfExtents = new Vector3(projectileSize, projectileSize / 2f, projectileSize);

        // Find anyone in front of the current "pile"
        Collider[] hitInFront = Physics.OverlapBox(boxCenter, halfExtents, transform.rotation, LayerMask.GetMask("Enemy", "Player"));

        foreach (var otherCol in hitInFront)
        {
            // Don't push yourself or your faction
            EntityIdentity identity = otherCol.GetComponent<EntityIdentity>();
            if (identity != null && identity.faction == myFaction) continue;

            HandlePush(otherCol);
        }
    }




    void OnTriggerExit(Collider col)
    {
        if (isBeam && col.TryGetComponent<Rigidbody>(out var rb))
        {
            if (capturedMasses.Contains(rb)) capturedMasses.Remove(rb);
        }
    }

    // Move the push logic to a helper function so we can call it for both groups
    private void HandlePush(Collider col)
    {
        // Layer & Faction Check
        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EntityIdentity victimIdentity = col.GetComponent<EntityIdentity>();
            if (victimIdentity != null && victimIdentity.faction == myFaction) return;

            // Add to mass if not already there (helps with the slowdown effect)
            if (col.TryGetComponent<Rigidbody>(out var rb) && !capturedMasses.Contains(rb))
            {
                capturedMasses.Add(rb);
            }



            Vector3 toEnemy = col.transform.position - transform.position;
            float dot = Vector3.Dot(toEnemy, transform.forward);

            if (col.TryGetComponent<PushPropagator>(out var propagator))
            {
                // Calculate how much we need to shove them to keep them at the tip of the beam
                Vector3 pushDelta = transform.forward * (projectileSize - dot);

                // This starts the chain reaction!
                propagator.PropagatePush(pushDelta, this.gameObject);
            }

            // If they are behind the "tip" of the beam, snap them to the tip
            if (dot < projectileSize)
            {


                Vector3 forwardMove = transform.forward * (projectileSize - dot);

                if (col.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent) && agent.enabled)
                {
                    if (agent.isOnNavMesh) agent.Move(forwardMove);
                }
                else
                {
                    col.transform.position += forwardMove;
                }
            }
        }
    }

    // --- Keep your original helper functions ---
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

    private void DestroyByDistance()
    {
        if (Vector3.Distance(spawnPos, transform.position) > projectileRange)
        {

            // If it's a beam, we trigger the explosion ONE time before it dies
            if (isBeam && explosionAbility != null)
            {
                explosionAbility.Cast(transform.position, Quaternion.identity, caster);
            }

            Destroy(this.gameObject);
        }
    }


}
