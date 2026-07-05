using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(healthBar))]
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float healthPoints;

    [Header("Regen Settings")]
    [SerializeField] private bool canRegen;
    [SerializeField] private float regenRate;
    [SerializeField] private float regenAmount;
    [SerializeField] private float outCombatTime;
    [SerializeField] private float regenOutCombatRate;
    [SerializeField] private float regenOutCombatAmount;

    private Coroutine timeCombatCo;
    private Coroutine GetHitCo;
    private bool inCombat;
    private bool isDead;

    private DamageReceiver _receiver;
    private static readonly int getHitHash = Animator.StringToHash("GetHit");

    [Header("UI & Animation")]
    [SerializeField] private Image hpFillImage;
    private Animator animator;

    // --- PIPELINE EVENTS ---
    public event Action<HitInfo> OnDeath;
    public event Action<HitInfo> OnDamageTaken;
    public event Action<HitInfo> OnBlockSuccess;

    // --- CORE ENGINE REFERENCES ---
    private Stats stats;

    void Awake()
    {
        _receiver = GetComponent<DamageReceiver>();
        stats = GetComponent<Stats>();
    }

    void Start()
    {
        if (hpFillImage == null)
        {
            Debug.LogError("no hpFill sprite found in " + gameObject.name);
        }

        if (GetComponent<Animator>())
        {
            animator = GetComponent<Animator>();
        }
        else
        {
            Debug.LogError("no Animator found in " + gameObject.name);
        }

        healthPoints = maxHealth;
        StartCoroutine(Regen());
    }

    public void Heal(float amount)
    {
        if (!isDead)
        {
            healthPoints += amount;
            if (healthPoints > maxHealth)
            {
                healthPoints = maxHealth;
            }
            UpdateHealthBar();
        }
    }

    void HandleDamage(HitInfo info)
    {
        Debug.Log("we called HandleDamage from health script");
        if (isDead) return;

        float rawDamage = info.damage;
        Damage(rawDamage, info);
    }

    /// <summary>
    /// Processes incoming damage from ANY source. Handles blocks, status bypasses, and flinch logic.
    /// </summary>
    public void Damage(float amount, HitInfo dmgInfo)
    {
        if (amount < 0f) return;
        if (isDead) return;

        // Use equality to check against the default empty state of the struct
        if (dmgInfo.Equals(default(HitInfo)))
        {
            dmgInfo = new HitInfo();
            dmgInfo.damage = amount;
            dmgInfo.attackType = AttackType.Melee;
        }
        else
        {
            dmgInfo.damage = amount;
        }


        float finalDamage = amount;
        bool wasBlocked = false;

        // Blocking is resolved once, upstream in DamageReceiver.TakeDamage, before this event
        // fans out to every listener (Health AND EffectManager -> Effects). By the time we get
        // here, dmgInfo.isBlocked already reflects whether the original hit was blocked — DoT
        // ticks carry that same flag forward from the initial hit (see TakeDoT.cs), so the same
        // mitigation math applies consistently to both the instant hit and every later tick.
        if (dmgInfo.isBlocked)
        {
            float mitigationPercentage = (stats != null) ? stats.blockDamageMitigation : 75f;
            float damageMultiplier = 1f - (Mathf.Clamp(mitigationPercentage, 0f, 100f) / 100f);

            finalDamage = amount * damageMultiplier;
            dmgInfo.damage = finalDamage;

            // Only the initial direct hit gets the physical shield flinch/VFX/SFX;
            // background DoT ticks stay quiet.
            if (dmgInfo.attackType != AttackType.DoT)
            {
                wasBlocked = true;

                // Melee hits reach Damage() twice: once as a zero-damage envelope via
                // HandleDamage (OnHitReceived), once with the real amount via the TakeDamage
                // effect. Only fire the VFX/SFX event on the call that carries real damage,
                // so a single block doesn't play its effect twice.
                if (amount > 0f)
                {
                    OnBlockSuccess?.Invoke(dmgInfo);
                }
            }
        }

        // Apply calculated health reductions
        healthPoints -= finalDamage;
        isDead = IsDead();

        if (isDead)
        {
            OnDeath?.Invoke(dmgInfo);
        }
        UpdateHealthBar();

        if (timeCombatCo != null) StopCoroutine(timeCombatCo);
        timeCombatCo = StartCoroutine(TimeCombat());

        // Flinch animation gate (Will not trigger for standard DoTs OR mitigated DoTs)
        if (!wasBlocked && dmgInfo.attackType != AttackType.DoT)
        {
            if (GetHitCo != null) StopCoroutine(GetHitCo);
            GetHitCo = StartCoroutine(SetGetHit());
        }

        if (finalDamage > 0f)
        {
            OnDamageTaken?.Invoke(dmgInfo);
        }
    }

    private bool IsDead()
    {
        if (healthPoints <= 0)
        {
            healthPoints = 0;
            return true;
        }
        return false;
    }

    IEnumerator Regen()
    {
        while (canRegen && !isDead)
        {
            if (inCombat)
            {
                Heal(regenAmount);
                yield return new WaitForSeconds(regenRate);
            }
            else
            {
                Heal(regenOutCombatAmount);
                yield return new WaitForSeconds(regenOutCombatRate);
            }
        }
    }

    public bool GetisDead() { return isDead; }

    IEnumerator TimeCombat()
    {
        inCombat = true;
        yield return new WaitForSeconds(outCombatTime);
        inCombat = false;
    }

    public void SetMaxHealth(float hp) { maxHealth = hp; }
    public float Gethealth() { return healthPoints; }
    public float GetMaxHealth() { return maxHealth; }

    private void UpdateHealthBar()
    {
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = healthPoints / maxHealth;
        }
    }

    void OnEnable()
    {
        if (_receiver != null)
        {
            _receiver.OnHitReceived += HandleDamage;
        }
    }

    void OnDisable()
    {
        if (_receiver != null)
        {
            _receiver.OnHitReceived -= HandleDamage;
        }
    }

    IEnumerator SetGetHit()
    {
        if (animator.GetBool(getHitHash))
        {
            animator.SetBool(getHitHash, false);
        }
        else
        {
            animator.SetBool(getHitHash, true);
            yield break;
        }

        yield return new WaitForSeconds(0.02f);
        animator.SetBool(getHitHash, true);
    }
}