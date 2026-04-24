
using System.Collections.Generic;
using UnityEngine;

public class ComboController : MonoBehaviour
{
   [SerializeField] private Ability currentAbility;
   public int lightCombosIndex = 0;
   public  int heavyCombosIndex = 0;
   public  int magicCombosIndex = 0;
    private float lastInputTime;
    private PlayerStateMachine psm;

    // This would be your "Equipped" sequence
    public List<Ability> equippedSequence;

    public List<Ability> lightCombos = new();
    public List<Ability> heavyCombos = new();
    public List<Ability> magicCombos = new();

    private void Start()
    {


        psm = GetComponent<PlayerStateMachine>();
        equippedSequence = GetComponent<AbilityManager>().abilities;

       InitilizeComboSequences();
    }

    private void InitilizeComboSequences()
    {
        // Automatically sort into sequences
        foreach (var a in equippedSequence) 
        {
            if (a.track == ComboTrack.Light) lightCombos.Add(a);
            else if (a.track == ComboTrack.Heavy) heavyCombos.Add(a);
            else if (a.track == ComboTrack.Magic) magicCombos.Add(a);
        }
    }


    public void OnAbilityInput(ref int index,List<Ability> comboList)
    {
        if (psm.IsStunned()) return;


        Debug.Log(psm.abilityManager.IsPerformingAction());
        if (psm.abilityManager.IsPerformingAction()) return; //dont continue if we already casting

        // If we are within the combo window, go to next ability
        if (Time.time - lastInputTime < currentAbility?.comboWindow)
        {
            index++;
            if (index >= comboList.Count) index = 0;
        }
        else
        {
            index = 0; // Reset if too slow
        }

        Ability nextAb = comboList[index];

        // Check cooldown through the Manager
        if (psm.abilityManager.cooldowns[nextAb.abilityName].coolDownReady )
        {
            currentAbility = nextAb;
            lastInputTime = Time.time;

            //i want to chck if we not in stun state here then execute switch to action state?
            // Execute!
            psm.SwitchState(new ActionState(psm, nextAb));
        }
    }

    private void PreloadAbility(Ability ab)
    {
        currentAbility = ab;
        
    }

    private void ExecuteCombo(Ability ab)
    {
        currentAbility = ab;
        lastInputTime = Time.time;

        // Tell State Machine to enter ActionState for this specific ability
        GetComponent<PlayerStateMachine>().SwitchState(new ActionState(psm, ab));
    }
}