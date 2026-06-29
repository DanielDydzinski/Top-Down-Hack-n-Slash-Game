using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static Ability;

public class AbilityManager : MonoBehaviour
{

    GameObject currentAbilityObject;
    Coroutine castAbilityRoutine;
    [SerializeField]Animator animator;
    private static readonly int AttackStateHash = Animator.StringToHash("AttackState");

    public Dictionary<string, CoolDown> cooldowns;

    [SerializeField] private Transform spawnLocation;
    public List<Ability> abilities;

    private List<Ability> meleeAbilities = new List<Ability>();
    private List<Ability> rangedAbilities = new List<Ability>();

    private bool IsCastingAbility = false;
    public Ability activeAbility;
    private Transform activeTarget;
    public event Action OnAbilityReady;

    private EffectSpawnPossitions effectSpawnPossitions;
    private PlayerEnergy playerEnergy; // Reference to our energy tracker system

    public Transform VisualLeftHandAttachPoint, VisualRightHandAttachPoint;
    private GameObject activeVisualEffect;

    void Start()
    {
        effectSpawnPossitions = GetComponent<EffectSpawnPossitions>();
        playerEnergy = GetComponent<PlayerEnergy>(); // Cache the energy component locally

        if (animator == null && GetComponent<Animator>())
        {
            animator = GetComponent<Animator>();
        }
        else
        {
            Debug.Log(gameObject.name + " doesn't have Animator");
        }

        cooldowns = new Dictionary<string, CoolDown>();
        foreach (Ability ab in abilities)
        {
            // Migrated: Fetching clean identifiers directly out of the baseSettings container
            cooldowns.Add(ab.baseSettings.abilityName, new CoolDown(ab));
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.C))
        {
            CancelAbility();
        }
    }

    public void InitializeAbilities(List<Ability> allAbilities)
    {
        foreach (var a in allAbilities)
        {
            // Migrated: Evaluating tags via baseSettings container references safely
            if (a.baseSettings.isRanged) rangedAbilities.Add(a);
            else meleeAbilities.Add(a);
        }
    }

    public void SetMovementLock(bool locked)
    {
        IsCastingAbility = locked;
        if (!locked)
        {
            animator.SetInteger(AttackStateHash, -1);
        }
    }

    public bool GetIsCastingAbility() { return IsCastingAbility; }
    public bool IsPerformingAction() { return animator.GetInteger(AttackStateHash) != -1; }

    public void CancelAbility()
    {
        animator.SetInteger(AttackStateHash, -1);
        Debug.Log("Canceling ability");
        if (activeVisualEffect != null)
        {
            Destroy(activeVisualEffect);
        }
        CancelGetHitAnim();
        ClearActiveAbility();
    }

    public void CancelAbilityButtonUp()
    {
        if (activeAbility == null || !IsPerformingAction()) return;

        PlayerStateMachine psm = GetComponent<PlayerStateMachine>();
        int targetLayer = psm.BaseLayer;

        // Migrated: Target layer tracking updated to pull directly from structural container
        if (activeAbility.baseSettings.animLayer == AnimationLayer.UpperBody)
        {
            targetLayer = psm.AttackLayer;
        }
        else if (activeAbility.baseSettings.animLayer == AnimationLayer.FullBody)
        {
            targetLayer = psm.FullBodyLayer;
        }

        animator.CrossFade(psm.TransitionStateHash, 0.1f, targetLayer);
        CancelAbility();
    }

    public void CancelGetHitAnim() { animator.SetBool("GetHit", false); }

    public void ExecuteActiveAbility()
    {
        Vector3 spawnPos = spawnLocation.position;
        Quaternion spawnRot = spawnLocation.rotation;

        if (activeAbility != null)
        {
            // Pulled cleanly from our unified baseSettings!
            switch (activeAbility.baseSettings.spawnLocation)
            {
                case abilitySpawnType.Onself:
                    spawnPos += activeAbility.baseSettings.spawnLocationOffset;
                    spawnRot = Quaternion.identity;
                    break;

                case abilitySpawnType.OnTarget:
                    if (activeTarget != null)
                    {
                        spawnPos = activeTarget.position;
                        spawnPos += activeAbility.baseSettings.spawnLocationOffset;
                        spawnRot = Quaternion.identity;
                    }
                    break;

                case abilitySpawnType.PlayerRoot:
                    spawnPos = effectSpawnPossitions.abilitySpawnRootPos.position;
                    spawnRot = this.transform.rotation;
                    break;

                case abilitySpawnType.SpecifiedPoint:
                    spawnRot *= Quaternion.Euler(activeAbility.baseSettings.spawnRotationOffset);
                    break;
            }

            // Casts the ability prefab into the world
            currentAbilityObject = activeAbility.Cast(spawnPos, spawnRot, this.gameObject);
        }
        else
        {
            Debug.LogWarning("Animation tried to cast, but no activeAbility was set! from " + this.gameObject.name);
        }
    }

    // UPDATED: Now validates energy constraints cleanly via baseSettings structures before committing execution pipeline steps
    // UPDATED: Dynamic validation that bypasses resource constraints if the entity lacks an energy pool
    public void StartCastingAbility(Ability ab, Transform target)
    {
        string currentName = ab.baseSettings.abilityName;

        if (cooldowns[currentName].coolDownReady)
        {
            // 1. If they HAVE an energy script, check if they can afford it. If they DON'T, skip this entirely!
            if (playerEnergy != null && !playerEnergy.CanAfford(ab.baseSettings.energyCost))
            {
               // Debug.LogWarning($"[CAST FAILED] Insufficient resource pool values to cast: {currentName}");
                return;
            }

            // 2. Only deduct energy if the component actually exists on this entity
            if (playerEnergy != null)
            {
                playerEnergy.UseEnergy(ab.baseSettings.energyCost);
                Debug.Log("ABILITY JUST USED ENERGY of " + ab.baseSettings.energyCost);
                Debug.Log("current Energy = " + playerEnergy.GetCurrentEnergy());
            }

            // 3. Begin active setup states safely (Works for everyone!)
            activeTarget = target;
            activeAbility = ab;
            animator.SetInteger(AttackStateHash, cooldowns[currentName].ability.baseSettings.attackState);

            if (ab.baseSettings.audioClip != null)
            {
                GetComponent<AudioSource>().PlayOneShot(ab.baseSettings.audioClip);
            }

            StartCoroutine(RunCoolDown(cooldowns[currentName]));
        }
    }

    public void ClearActiveAbility() { activeAbility = null; }

    public void CastAbility(string name)
    {
        currentAbilityObject = cooldowns[name].TriggerAbility(spawnLocation.position, spawnLocation.rotation, this.gameObject);
    }

    public void AddAbility(Ability ab)
    {
        cooldowns.Add(ab.baseSettings.abilityName, new CoolDown(ab));
    }

    private IEnumerator RunCoolDown(CoolDown cd)
    {
        cd.stopwatch.Stop();
        cd.stopwatch.Reset();
        cd.stopwatch.Start();

        cd.coolDownReady = false;
        cd.timeLeft = cd.ability.baseSettings.cooldown;

        while (!cd.coolDownReady)
        {
            cd.timeLeft = cd.ability.baseSettings.cooldown - (float)cd.stopwatch.Elapsed.TotalSeconds;
            cd.coolDownReady = (cd.timeLeft <= 0f);
            yield return null;
        }

        cd.stopwatch.Stop();
        cd.stopwatch.Reset();
        OnAbilityReady?.Invoke();
    }

    public bool CurrentAbilityAllowsMovement()
    {
        if (activeAbility != null) return activeAbility.baseSettings.canMoveAttack;
        return true;
    }

    public Ability GetHighestPriorityAbility()
    {
        Ability bestAbility = null;
        int topPriority = int.MaxValue;

        foreach (var cd in cooldowns.Values)
        {
            if (cd.coolDownReady)
            {
                if (cd.ability.baseSettings.priority < topPriority)
                {
                    topPriority = cd.ability.baseSettings.priority;
                    bestAbility = cd.ability;
                }
            }
        }
        return bestAbility;
    }

    public void PlayVisuals()
    {
        if (activeAbility == null) return;

        Transform targetParent = transform;

        if (activeAbility.baseSettings.attachPoint == VisualAttachPoint.RightHand)
        {
            if (VisualRightHandAttachPoint != null) targetParent = VisualRightHandAttachPoint;
        }
        else if (activeAbility.baseSettings.attachPoint == VisualAttachPoint.LeftHand)
        {
            if (VisualLeftHandAttachPoint != null) targetParent = VisualLeftHandAttachPoint;
        }

        Vector3 finalPos = targetParent.position + activeAbility.baseSettings.spawnLocationOffset;
        Quaternion finalRot = targetParent.rotation * Quaternion.Euler(activeAbility.baseSettings.spawnRotationOffset);

        if (activeAbility.baseSettings.abilityVisualParticles != null)
        {
            activeVisualEffect = Instantiate(
                activeAbility.baseSettings.abilityVisualParticles,
                finalPos,
                finalRot,
                targetParent
            );
        }
        if (activeAbility.baseSettings.visualEffectAudio != null)
        {
            AudioSource.PlayClipAtPoint(activeAbility.baseSettings.visualEffectAudio, transform.position);
        }
    }

    public Ability GetHighestPriorityReady(bool wantRanged)
    {
        List<Ability> listToSearch = wantRanged ? rangedAbilities : meleeAbilities;

        Ability best = null;
        int topPriority = int.MaxValue;

        foreach (var a in listToSearch)
        {
            string nameKey = a.baseSettings.abilityName;
            if (cooldowns[nameKey].coolDownReady && a.baseSettings.priority < topPriority)
            {
                topPriority = a.baseSettings.priority;
                best = a;
            }
        }
        return best;
    }

    private Ability GetFirstReadyAbility()
    {
        foreach (var cd in cooldowns.Values)
        {
            if (cd.coolDownReady) return cd.ability;
        }
        return null;
    }
}