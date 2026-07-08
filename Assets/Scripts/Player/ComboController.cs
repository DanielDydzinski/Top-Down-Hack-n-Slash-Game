
using System.Collections.Generic;
using UnityEngine;

public class ComboController : MonoBehaviour
{
    public enum ComboTrackId { Light, Heavy, Magic, Q, E, R, F, DodgeHeavy, Counter, QMidAir, DodgeHeavyMidAir }

    private class TrackState
    {
        public Ability currentAbility;
        public float lastInputTime;
    }

   public int lightCombosIndex = 0;
   public  int heavyCombosIndex = 0;
   public  int magicCombosIndex = 0;
    public int qIndex = 0;
    public int eIndex = 0;
    public int rIndex = 0;
    public int fIndex = 0;
    public int dodgeHeavyIndex = 0;
    public int counterIndex = 0;
    public int qMidAirIndex = 0;
    public int dodgeHeavyMidAirIndex = 0;
    private PlayerStateMachine psm;

    // Each track (left-click, right-click, Q/E/R/F, ...) needs its own "last fired ability" +
    // "last input time" pair - sharing a single pair across tracks let pressing one button
    // reset or continue another button's combo window.
    private readonly Dictionary<ComboTrackId, TrackState> trackStates = new();

    private TrackState GetTrackState(ComboTrackId id)
    {
        if (!trackStates.TryGetValue(id, out TrackState state))
        {
            state = new TrackState();
            trackStates[id] = state;
        }
        return state;
    }

    public Ability GetLastFiredAbility(ComboTrackId id) => GetTrackState(id).currentAbility;
    public float GetLastInputTime(ComboTrackId id) => GetTrackState(id).lastInputTime;

    // This would be your "Equipped" sequence
    public List<Ability> equippedSequence;

    public List<Ability> lightCombos = new();
    public List<Ability> heavyCombos = new();
    public List<Ability> magicCombos = new();

    [Header("Slotted Abilities (Manual Assignment)")]
    public List<Ability> qAbilities = new();
    public List<Ability> eAbilities = new();
    public List<Ability> rAbilities = new();
    public List<Ability> fAbilities = new();

    [Header("Dodge Heavy Attack (Manual Assignment)")]
    public List<Ability> dodgeHeavyAbilities = new();

    [Header("Mid-Air Variants (Manual Assignment)")]
    [Tooltip("Used instead of qAbilities when Q is pressed while falling - trimmed clips that already start at the freeze pose, see MidFallAttackState.")]
    public List<Ability> qMidAirAbilities = new();
    [Tooltip("Used instead of dodgeHeavyAbilities when Heavy (mouse 1) is pressed while falling.")]
    public List<Ability> dodgeHeavyMidAirAbilities = new();

    [Header("Counter Attack (Manual Assignment)")]
    public List<Ability> counterAbilities = new();

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


    public void OnAbilityInput(ComboTrackId trackId, ref int index, List<Ability> comboList, bool startAirborne = false)
    {
        if (psm.IsStunned()) return;
        if (psm.abilityManager.IsPerformingAction()) return; //dont continue if we already casting
        if (comboList == null || comboList.Count == 0) return;

        TrackState state = GetTrackState(trackId);
        int candidateIndex = GetCandidateIndex(state, index, comboList.Count);
        Ability nextAb = comboList[candidateIndex];

        // Check cooldown through the Manager. Bail out before mutating index/state so a
        // press that can't actually fire doesn't corrupt this track's combo position.
        if (!psm.abilityManager.cooldowns[nextAb.abilityName].coolDownReady) return;

        index = candidateIndex;
        state.currentAbility = nextAb;
        state.lastInputTime = Time.time;

        // Triggered while already falling (Q/DodgeHeavy only, see PlayerInputHandler) - MidFallAttackState
        // skips the grounded windup/dash and starts the ability already frozen at airFreezeCheckPoint.
        if (startAirborne)
            psm.SwitchState(new MidFallAttackState(psm, nextAb));
        else
            psm.SwitchState(new ActionState(psm, nextAb));
    }

    // The ability a press would trigger next on this track, without committing to it.
    // Used both by OnAbilityInput and by the ability-slot UI so both agree on what "next" means.
    public Ability PeekNextAbility(ComboTrackId trackId, int index, List<Ability> comboList)
    {
        if (comboList == null || comboList.Count == 0) return null;
        TrackState state = GetTrackState(trackId);
        int candidateIndex = GetCandidateIndex(state, index, comboList.Count);
        return comboList[candidateIndex];
    }

    private int GetCandidateIndex(TrackState state, int currentIndex, int count)
    {
        bool withinWindow = state.currentAbility != null
            && Time.time - state.lastInputTime < state.currentAbility.comboWindow;
        return withinWindow ? (currentIndex + 1) % count : 0;
    }
}