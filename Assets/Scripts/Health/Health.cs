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

    // FIXED: This now properly caches via Awake/Start to prevent memory leaks in OnDisable
    private DamageReceiver _receiver;
    private static readonly int getHitHash = Animator.StringToHash("GetHit");

    [Header("UI & Animation")]
    [SerializeField] private Image hpFillImage;
    private Animator animator;

    public event Action<HitInfo> OnDeath;
    public event Action<HitInfo> OnDamageTaken;

    void Awake()
    {
        // Cache the reference immediately on initialization
        _receiver = GetComponent<DamageReceiver>();
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

        Damage(info.damage, info);
    }

    public void Damage(float amount, HitInfo dmgInfo)
    {
        if (amount < 0f) return;

        healthPoints -= amount;
        isDead = IsDead();

        if (isDead)
        {
            OnDeath?.Invoke(dmgInfo);
        }
        UpdateHealthBar();

        if (timeCombatCo != null)
        {
            StopCoroutine(timeCombatCo);
        }
        timeCombatCo = StartCoroutine(TimeCombat());

        // stops the running instance handle to clear animation states cleanly
        if (GetHitCo != null)
        {
            StopCoroutine(GetHitCo);
        }
        GetHitCo = StartCoroutine(SetGetHit());

        //call the event - we took damage
        if (amount > 0f)
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
        // FIXED: Safe subscription because _receiver isn't null anymore
        if (_receiver != null)
        {
            _receiver.OnHitReceived += HandleDamage;
        }
    }

    void OnDisable()
    {
        // FIXED: Unsubscribes properly when pooled or destroyed, preventing hidden memory leaks!
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






//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//[RequireComponent(typeof(healthBar))]
//public class Health : MonoBehaviour {

//    [SerializeField] private float maxHealth;
//    [SerializeField] private float healthPoints;
//    [SerializeField] private bool canRegen;
//    [SerializeField] private float regenRate;
//    [SerializeField] private float regenAmount;
//    [SerializeField] private float outCombatTime;
//	[SerializeField] private float regenOutCombatRate;
//    [SerializeField] private float regenOutCombatAmount;

//    private Coroutine timeCombatCo;
//	private Coroutine GetHitCo;
//    private bool inCombat;
//    private bool isDead;
//    private DamageReceiver _receiver;
//    private static readonly int getHitHash = Animator.StringToHash("GetHit");

//    [SerializeField]
//	private Image hpFillImage; // reference to hp sprite
//	private Animator animator;

//    public event Action<HitInfo> OnDeath;


//    // Use this for initialization
//    void Start () {


//         var receiver = GetComponent<DamageReceiver>();
//        if (receiver != null)
//        {
//            receiver.OnHitReceived += HandleDamage;
//        }


//        if (hpFillImage == null) 
//		{
//            Debug.LogError("no hpFill sprite found in " + gameObject.name);
//        }


//		if (GetComponent<Animator> ())
//		{
//			animator = GetComponent<Animator> ();
//		} 
//		else 
//		{
//			Debug.LogError ("no Animator found in " + gameObject.name);
//		}
//		healthPoints = maxHealth;


//        StartCoroutine(Regen());
//	}


//    public void Heal(float amount)
//    {
//		if (!isDead) {
//			healthPoints += amount;
//			if (healthPoints > maxHealth) {
//				healthPoints = maxHealth;
//			}
//			UpdateHealthBar ();
//		}
//    }

//    void HandleDamage(HitInfo info)
//    {
//        // Damage(info.damage); // Health only cares about the number

//        Debug.Log("we called HandleDamage from health script");
//        // Don't process damage if already dead
//        if (isDead) return;

//        // Process actual numerical damage
//        Damage(info.damage, info);


//    }

//    public void Damage(float amount, HitInfo dmgInfo)
//    {
//        if (amount < 0f)
//        {
//            return;
//        }
//        healthPoints -= amount;
//        isDead = IsDead();
//        if (isDead)
//        {
//            OnDeath?.Invoke(dmgInfo);
//           // animator.SetBool("isDead", true);
//        }
//        UpdateHealthBar();

//        if (timeCombatCo != null)
//        {
//            StopCoroutine(timeCombatCo);
//        }

//        timeCombatCo = StartCoroutine(TimeCombat());



//        if (GetHitCo != null)
//        {
//            StopCoroutine(SetGetHit());
//        }
//        GetHitCo = StartCoroutine(SetGetHit());


//    }

//    private bool IsDead()
//    {
//        if(healthPoints <= 0)
//        {
//            healthPoints = 0;
//            return true;
//        }
//        return false;
//    }

//    IEnumerator Regen()
//    {
//        while (canRegen && !isDead)
//        {
//            if (inCombat)
//            {
//                Heal(regenAmount);
//				yield return new WaitForSeconds(regenRate);
//            }
//            else
//            {
//                Heal(regenOutCombatAmount);
//				yield return new WaitForSeconds(regenOutCombatRate);
//            }
//        }

//    }

//    public bool GetisDead()
//    {
//        return isDead;
//    }

//    IEnumerator TimeCombat() // will need change this because can still be incombat without take dmg, but dealing dmg
//    {
//        inCombat = true;
//        yield return new WaitForSeconds(outCombatTime);
//        inCombat = false;

//    }

//	public void SetMaxHealth(float hp)
//	{
//		maxHealth = hp;
//	}

//	private void UpdateHealthBar()
//	{
//		hpFillImage.fillAmount = healthPoints / maxHealth; // value 0.0-1.0

//	}

//    public float Gethealth()
//    {
//        return healthPoints;
//    }
//    public float GetMaxHealth()
//    {
//        return maxHealth;
//    }

//    void OnEnable()
//    {
//        if (_receiver != null)
//        {
//            // Subscribe to the event
//            _receiver.OnHitReceived += HandleDamage;
//        }
//    }

//    void OnDisable()
//    {
//        if (_receiver != null)
//        {
//            // IMPORTANT: Unsubscribe to avoid memory leaks!
//            _receiver.OnHitReceived -= HandleDamage;
//        }
//    }

//    IEnumerator SetGetHit()
//	{
//		if (animator.GetBool (getHitHash)) 
//		{
//			animator.SetBool (getHitHash, false);
//		} 
//		else 
//		{
//			animator.SetBool (getHitHash, true);
//			yield break;
//		}
//		yield return new WaitForSeconds(0.02f);
//		animator.SetBool (getHitHash, true);

//	}
//}
