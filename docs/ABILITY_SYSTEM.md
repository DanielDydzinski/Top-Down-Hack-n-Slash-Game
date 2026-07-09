# Ability System

Deep-dive reference for how player abilities are authored, cast, animated, and resolved into damage. Complements the top-level architecture notes in `CLAUDE.md` (Strategy/Observer patterns, `HitInfo` struct gotcha) — read that first if you haven't.

## 1. Overview

Abilities are ScriptableObject **data assets**, not behaviour. Casting one spawns a prefab carrying a matching `Behaviours/` MonoBehaviour that does the actual hit detection. The Animator and a thin `PlayerStateMachine` (PSM) state jointly orchestrate *when* that happens: the state locks movement/rotation and drives an Animator int, the Animator plays a clip, and an Animation Event embedded in that clip is what actually triggers the cast. No ability logic runs from state-machine code directly.

## 2. ScriptableObject architecture

### `Ability.cs` (base class)

The base-field migration described in earlier revisions of this doc is complete — `Ability` now has a single `BaseAbilitySettings baseSettings` struct (identity/faction, layer masks, combo settings, dash/vault settings, energy costs, visuals, spawn positioning, fall-distance damage scaling) and no parallel legacy fields. Every concrete subclass (`FireBall`, `Explosion`, `AoEoT`, `ShockWave`, `MeleeAttackAbility`) likewise has just its own `XSettings` struct (`fireBallSettings`, `explosionSettings`, `aoEotSettings`, `shockWaveSettings`, `meleeSettings`) — the old `MigrateEverything()` methods and `GlobalXMigratorShortcut` editor tools were removed once every ability asset had been migrated.

`abstract Cast(Vector3 pos, Quaternion rot, GameObject caster)` — every subclass instantiates `abilityPrefab`, grabs the matching `Behaviours/` component, and hands off settings via a single standardized call: `behaviour.Initialize(this, baseSettings, subSettings, abilityEffects, caster)`. All five behaviours (`FireBallBehaviour`, `ExplosionBehaviour`, `AoEoTBehaviour`, `ShockWaveBehaviour`, `MeleeAttackBehavaiour`) now share this convention (the older flat-parameter `UpdateValues(...)` calls that `FireBall`/`Explosion`/`AoEoT` used have been removed). Each `Initialize(...)` also computes its own `fallDistanceMultiplier` internally via `psm.GetFallDistanceDamageMultiplier(baseSettings)` rather than having the ability's `Cast()` compute and pass it in — this let `Ability.GetFallDistanceDamageMultiplier(GameObject)` (the old per-`Cast()` wrapper) be deleted as dead code.

`List<Effect> abilityEffects` — **not applied by the ability or behaviour**. It's stapled onto `HitInfo.effects` when a behaviour builds a hit, then executed later by `EffectManager` (see below) — this is how the ability system plugs into the Observer-pattern damage pipeline documented in `CLAUDE.md`.

### `Effect.cs` and subclasses

Abstract base: `IEnumerator ApplyEffect(GameObject target, HitInfo info)`. Seven subclasses, one per effect type (Strategy pattern — add a new effect by adding a new class, never a new case in an existing one): `TakeDamage`, `TakeDoT`, `TakeHeal`, `TakeHoT`, `TakeKnockBack`, `TakePush`, `TakeSlow`.

`EffectManager` (`Assets/Scripts/Abilities/Ability Effects/EffectManager.cs`) subscribes to `DamageReceiver.OnHitReceived` and runs each effect in `info.effects` as a coroutine:
- `TakeSlow` / `TakeKnockBack` / `TakePush` each get a single **tracked, replaceable** coroutine — a fresh hit cancels the previous one (stops the coroutine, destroys its particles) and starts a new one.
- Everything else (`TakeDamage`, `TakeDoT`, `TakeHeal`, `TakeHoT`) goes into a generic tracked coroutine list.
- `CleanUpAllEffects()` stops everything on death.

All effect coroutines correctly use `Time.deltaTime` / `WaitForSeconds`, so they **do** respect `Time.timeScale` and pause properly — this is the contrast case for the cooldown bug in §5.

## 3. Behaviours (`Assets/Scripts/Abilities/Behaviours/*.cs`)

Plain MonoBehaviours living on the instantiated `abilityPrefab`, populated via `Initialize`/`UpdateValues` immediately after `Instantiate` in `Cast()`. Most defer one frame in `Start()` before doing real work, so values set post-instantiate are guaranteed applied first.

| Behaviour | Hit detection | Notes |
|---|---|---|
| `MeleeAttackBehavaiour` (filename/class intentionally misspelled, see `CLAUDE.md`) | `Physics.OverlapBox` + `Physics.BoxCastAll` sweep, sorted by distance, per-target LOS raycast | Hits up to `howManyEnemiesToHit` targets, self-destroys after |
| `FireBallBehaviour` | Moving projectile/beam (`transform.Translate` each `Update`), `OnTriggerEnter` | Beam mode can parent/capture rigidbodies; chains into an explosion ability on wall hit |
| `ExplosionBehaviour` | One-shot `Physics.OverlapSphere` | Distance falloff (`damageByDistance`), per-target LOS raycast, self-destroys after |
| `ShockWaveBehaviour` | Expanding trigger `SphereCollider` (`radius += expansionSpeed * Time.deltaTime`) | Radial falloff multiplier, self-destroys once `radius >= maxRadius` |
| `AoEoTBehaviour` | Ticking DoT zone, `Update()` accumulates `lifeTimer`/`tickTimer` | Full-area (`OverlapSphere`) or partial random-strike mode; self-destroys after `duration` |

All converge on the same pattern: build a `HitInfo` struct, call `IDamageable.TakeDamage(info)`. **This is where `HitInfo` gets constructed** — remember it's a value type (`CLAUDE.md`'s critical gotcha): each downstream listener gets its own independent copy, so any per-hit resolved value (block/dodge) must already be baked in before `TakeDamage` fans it out.

## 4. `AbilityManager` — casting, cooldowns, energy

Per-entity `MonoBehaviour`. Storage: `List<Ability> abilities` → `Dictionary<string, CoolDown> cooldowns` keyed by `baseSettings.abilityName`; also split into `meleeAbilities`/`rangedAbilities` lists. Selection helpers `GetHighestPriorityAbility()` / `GetHighestPriorityReady(bool wantRanged)` (lowest `priority` int wins) are used by AI.

**`StartCastingAbility(Ability ab, Transform target)`** (`AbilityManager.cs:166-199`):
1. Checks `cooldowns[currentName].coolDownReady`.
2. If the entity has a `PlayerEnergy` component, checks affordability and deducts (`CanAfford`/`UseEnergy`); entities without one skip this check entirely.
3. Sets `activeAbility = ab`, writes the Animator int: `animator.SetInteger(AttackStateHash, ab.baseSettings.attackState)`.
4. Plays cast audio, starts `RunCoolDown(...)` coroutine.

**`ExecuteActiveAbility()`** (`AbilityManager.cs:121-162`):
```csharp
public void ExecuteActiveAbility()
{
    if (activeAbility != null)
    {
        // compute spawnPos/spawnRot from activeAbility.baseSettings.spawnLocation
        // (Onself / OnTarget / PlayerRoot / SpecifiedPoint)
        currentAbilityObject = activeAbility.Cast(spawnPos, spawnRot, this.gameObject);
    }
    else
    {
        Debug.LogWarning("Animation tried to cast, but no activeAbility was set! from " + this.gameObject.name);
    }
}
```
**This method is only ever invoked via Animation Events embedded directly in `.anim` clip files** — confirmed by grep, no C# script calls it. E.g. `Assets/Animations/axe_kick.anim` fires it mid-clip at a specific normalized time (the "hit frame"), typically followed later by a `CancelAbility` event. This is the seam between the state-machine/movement layer (which only sets up the cast) and the actual ability-cast logic (which the Animator timeline triggers).

`IsPerformingAction()` == `animator.GetInteger(AttackStateHash) != -1`. Cleared to `-1` by `SetMovementLock(false)` or `CancelAbility()` — both of these are what actually end a cast from the ability-manager side.

**Visual effects (`PlayVisuals()`, `AbilityManager.cs:259-290`)** follow the same Animation-Event-driven convention as `ExecuteActiveAbility`/`CancelAbility` — it's wired into `.anim` clips (e.g. `HeroPunchMidAIr.anim`, `JumpSlam.anim`, `HuricaneKick.anim`) rather than called from any script. Currently it reads a **single** `activeAbility.baseSettings.abilityVisualParticles` prefab + `visualEffectAudio` clip and instantiates it at a hand attach point (`VisualAttachPoint.RightHand`/`LeftHand`) or the caster root. This is hardcoded directly into `AbilityManager` and only supports one visual per ability — see §10 for the planned refactor (multi-phase visuals, e.g. Kamehameha needing a separate charge-up prefab vs. release prefab).

## 5. Cooldown mechanism — and a known pause bug

`CoolDown.cs`:
```csharp
public class CoolDown {
    public Ability ability { set; get; }
    public float timeLeft { set; get; }
    public bool coolDownReady { set; get; }
}
```

`AbilityManager.RunCoolDown` (`AbilityManager.cs:213-228`):
```csharp
private IEnumerator RunCoolDown(CoolDown cd)
{
    cd.coolDownReady = false;
    cd.timeLeft = cd.ability.baseSettings.cooldown;

    float elapsed = 0f;
    while (!cd.coolDownReady)
    {
        elapsed += Time.deltaTime;
        cd.timeLeft = cd.ability.baseSettings.cooldown - elapsed;
        cd.coolDownReady = (cd.timeLeft <= 0f);
        yield return null;
    }

    OnAbilityReady?.Invoke();
}
```

> **Fixed:** cooldowns previously used `System.Diagnostics.Stopwatch` — a real-wall-clock (OS-level) timer entirely outside Unity's time system — so `Time.timeScale = 0` (`TimeManager.Pause()`, `Assets/Scripts/GlobalSystems/TimeManager.cs:60`) had no effect on them and they kept counting down in real time while the game was paused. This was the *only* time-based outlier in the ability/effect pipeline — everything else (`AoEoTBehaviour`, `ShockWaveBehaviour`, all `Effect` coroutines) already used `Time.deltaTime`/`WaitForSeconds` and respected timescale/pause.
>
> Fixed by accumulating `elapsed += Time.deltaTime` each loop iteration instead of reading `Stopwatch.Elapsed`, and dropping the `Stopwatch`/`System.Diagnostics` dependency from `CoolDown.cs` entirely. `CoolDown`'s public surface (`ability`, `timeLeft`, `coolDownReady`, constructor) is unchanged, so the UI (`AbilitySlotUI.GetCooldownFraction`, `Assets/Scripts/UI/AbilitySlotUI.cs:74-82`) and gating logic (`ComboController.cs:105`, `AbilityManager.GetHighestPriorityAbility`/`GetHighestPriorityReady`) needed no changes. `Time.deltaTime` is `0` while `Time.timeScale = 0`, so `elapsed` stops advancing and cooldowns correctly freeze on pause.

## 6. Input → Animator → Ability trigger chain

`PlayerInputHandler.cs`: legacy Input Manager polling (`Input.GetKey*`, `Input.GetMouseButtonDown`) in `Update()`. It does **not** touch the Animator itself — every ability button delegates to `ComboController.OnAbilityInput(trackId, ref index, comboList, startAirborne)`.

`ComboController.cs` owns one `TrackState` (current ability + last-input-time) per `ComboTrackId` (`Light`, `Heavy`, `Magic`, `Q`, `E`, `R`, `F`, `DodgeHeavy`, `Counter`, `QMidAir`, `DodgeHeavyMidAir`) so pressing one button never resets another button's combo window.

```csharp
public void OnAbilityInput(ComboTrackId trackId, ref int index, List<Ability> comboList, bool startAirborne = false)
{
    if (psm.IsStunned()) return;
    if (psm.abilityManager.IsPerformingAction()) return;
    // advance combo index if within Ability.baseSettings.comboWindow of last input on this track, else reset to 0
    // check candidate ability's cooldown
    if (startAirborne)
        psm.SwitchState(new MidFallAttackState(psm, nextAb));
    else
        psm.SwitchState(new ActionState(psm, nextAb));
}
```

> **Naming disambiguation:** the C# state class is `ActionState` (`Assets/Scripts/Player/ActionState.cs`) — this is *not* the same thing as the Animator int parameter, which is literally named `"AttackState"`. Easy to conflate; they're related but distinct (a state object vs. an Animator Controller parameter).

`ActionState.EnterState()` locks movement per `ability.baseSettings.canMoveAttack`, then calls `psm.abilityManager.StartCastingAbility(ability, null)` — **this** is what actually writes the `AttackState` animator int and starts the cooldown coroutine, not `ComboController` or the state constructor.

Full chain:
```
PlayerInputHandler (Update, legacy Input.*)
  -> ComboController.OnAbilityInput (picks next Ability, combo/cooldown gating)
  -> psm.SwitchState(new ActionState / MidFallAttackState(psm, ability))
  -> ActionState.EnterState() -> AbilityManager.StartCastingAbility
       -> activeAbility = ability; animator.SetInteger("AttackState", ability.baseSettings.attackState)
  -> [Animator plays the matching attack clip]
  -> Animation Event mid-clip -> AbilityManager.ExecuteActiveAbility()
  -> activeAbility.Cast(spawnPos, spawnRot, gameObject)
  -> Behaviour spawned -> HitInfo -> IDamageable.TakeDamage
```

`ActionState`/`MidFallAttackState` independently poll `animator.GetInteger("AttackState") == -1` each `UpdateState()` to detect "cast finished," then self-transition back to `LocomotionState`. There is no direct callback from `AbilityManager` back into the state — it's a three-way handshake mediated entirely through the Animator int.

## 7. Attack tags and Animator layers / avatar masks

**Tags.** `Assets/Player/playerAnimCont.controller` tags many attack states `m_Tag: Attack` and dodge states `m_Tag: Dodge`. Only `Dodge` is actually read in code:
```csharp
// DamageReciever.cs:19
bool isDOdgeState = psm.anim.GetCurrentAnimatorStateInfo(psm.FullBodyLayer).IsTag("Dodge");
if (isDOdgeState) { /* damage negated — i-frames */ return; }
```
Any state tagged `Dodge` on the Full Body layer grants damage immunity automatically — new dodge clips get i-frames just by tagging the state, no per-clip code required.

`ActionState.cs:10` declares `attackTagHash = Animator.StringToHash("Attack")` but it's **never read anywhere in the file** — vestigial, likely a planned-but-unfinished mirror of the Dodge-tag check (see §10).

**Layers.** `playerAnimCont.controller` `m_AnimatorLayers`:

| Index | Name | Mask | Default weight |
|---|---|---|---|
| 0 | `Base Layer` | none | 0 |
| 1 | `Attack Layer` | `Attack.mask` (excludes/limits legs — functions as the de-facto upper-body layer, despite the name) | 1 |
| 2 | `Full Body Attack` | `FullBody.mask` (full skeleton) | 1 |
| 3 | `GetHit` | hit-reaction mask | — |

Weights are static (`m_DefaultWeight` in the controller asset) — no `animator.SetLayerWeight` calls exist anywhere in `Assets/Scripts`. Layer *selection* instead happens by choosing **which layer's state gets driven**, based on `Ability.baseSettings.animLayer` (`enum AnimationLayer { UpperBody, FullBody }`):
```csharp
// ActionState.cs:31 / MidFallAttackState.cs — identical pattern
layerIndex = (ability.baseSettings.animLayer == AnimationLayer.FullBody) ? 2 : 1;
```
This is why light melee combos can play upper-body-only (player keeps walking) while heavier/mid-air abilities lock the whole body via the Full Body layer. `FallingState.cs` unconditionally resets both attack layers (`Play(TransitionStateHash, AttackLayer)` and `..., FullBodyLayer)`) on entry/exit to force a clean state regardless of which layer the previous ability used.

## 8. `PlayerStateMachine` (PSM) relationship

Recap of the State pattern from `CLAUDE.md` (`IPlayerState`, `SwitchState`), specific to ability-relevant states:

- **`ActionState`** (grounded cast) and **`MidFallAttackState`** (airborne cast) are thin orchestrators. They own movement/rotation/physics locks and any procedural dash/vault movement (`ActionState.HandleDashMovement` calls `CharacterController.Move` directly, separate from `Mover`'s per-frame call) around whatever the Animator + `AbilityManager` are doing. **They never call `Ability.Cast()` themselves** — that only happens via the Animation Event seam in §6.
- Casting an ability always forces an immediate state transition (`LocomotionState`/`BlockState` → `ActionState`/`MidFallAttackState`), gated by `AbilityManager.IsPerformingAction()` so a second input can't interrupt an in-flight cast.
- `PlayerStateMachine.IsAirborne()` deliberately raycasts rather than checking `currentState is FallingState` — because `ActionState`/dodge states can exit back to `LocomotionState` while the player is still physically airborne, before `LocomotionState`'s own debounce (`fallDetectionDelay`) commits to `FallingState`. `PlayerInputHandler` uses this ground-truth raycast, not the state enum, to decide `ActionState` vs `MidFallAttackState`.
- Dodge states (`RollDodgeState`, `BackflipDodgeState`, and their "second" variants) call `psm.StartDodgeHeavyWindow()` on exit, opening a brief window for a follow-up `DodgeHeavy` combo track — small piece of inter-state memory living on `PlayerStateMachine` itself rather than passed between state objects.
- Closing the loop to the damage pipeline: `DamageReceiver.TakeDamage` reaches into the **target's own** `PlayerStateMachine` (checks the Dodge tag, calls `BlockState.TryBlock(info)` if `currentState is BlockState`) — so PSM state is consulted on both the *outgoing* (caster casting an ability) and *incoming* (target receiving a hit) side of combat.

## 9. Design patterns in play

- **Strategy** — `IPlayerState` states, `Effect` subclasses, `Ability` subclasses. New behavior = new class, not a new branch in shared code.
- **Observer** — `DamageReceiver.OnHitReceived` → `Health` + `EffectManager`, independently, per `CLAUDE.md`.
- **Animator-int as a state-machine signal channel** — not a standard GoF pattern, but a deliberate recurring technique here: `ActionState`/`MidFallAttackState` don't get a direct "cast finished" callback from `AbilityManager`; they poll an Animator int (`AttackState`) that `AbilityManager` clears. Useful to recognize since it's easy to mistake for a bug (a state watching itself) rather than the intended handshake mechanism.
- **Data-driven ScriptableObject config** — all ability settings live in `baseSettings`/the subclass settings struct. Prefer extending that struct convention (and the newer `Initialize(...)`-based `Cast()` handoff, see §10) rather than adding flat fields directly to a class.

## 10. Suggested improvements (ideas for future sessions)

- ~~Fix the cooldown pause bug (§5)~~ — **done**: `RunCoolDown` now accumulates `Time.deltaTime` instead of reading a real-wall-clock `Stopwatch`, so cooldowns correctly freeze when `Time.timeScale = 0`.
- ~~Retire the base/legacy field migration~~ — **done**: the legacy fields on `Ability` and all five subclasses, plus every `MigrateEverything()`/`GlobalXMigratorShortcut` editor tool, have been deleted. All 37 ability `.asset` files already carried real, populated `baseSettings`/subclass-settings data before removal (verified field-by-field), so no asset data was lost. Fixing this also closed a live bug: `AbilityManager.cooldowns` is keyed by `baseSettings.abilityName`, but `ComboController.cs` and `AbilitySlotUI.cs` were looking that dictionary up using the legacy `.abilityName` field — for `HeroAttackMidAir.asset`, whose two name fields had drifted apart (`"HeroMidAir"` vs. legacy `"HeroExplo"`), that meant casting/reading its cooldown either threw or silently read as "always ready." Both call sites now key off `baseSettings.abilityName` like the dictionary itself does.
- ~~Standardize the `Cast()` → behaviour handoff~~ — **done**: `FireBallBehaviour`, `ExplosionBehaviour`, and `AoEoTBehaviour` now expose `Initialize(this, baseSettings, subSettings, effects, caster)` matching `ShockWaveBehaviour`/`MeleeAttackBehavaiour`, replacing their old flat-parameter `UpdateValues(...)` calls; each now computes its own `fallDistanceMultiplier` internally instead of receiving it as a parameter, which let the now-unused `Ability.GetFallDistanceDamageMultiplier(GameObject)` wrapper be deleted. This surfaced (and fixed) a real gap: none of these three stamped `sourceAbility` onto their `HitInfo`, so `TakeDamage.ApplyEffect`'s energy-gain-on-hit/refund-on-kill logic (`info.attacker != null && info.sourceAbility != null` check, `Assets/Scripts/Abilities/Ability Effects/TakeDamage.cs:33`) silently no-opped for `FireBall`/`Explosion`/`AoEoT` casts even when `baseSettings.energyGainOnHit`/`energyRefundOnKillPercent` were configured. `FireBallBehaviour` additionally had `HitInfo.attacker` set to the projectile GameObject itself instead of `caster` — since `TakeDamage` looks up `info.attacker.GetComponent<PlayerEnergy>()`, that alone would have kept the feature dead even after wiring `sourceAbility`. Both are now fixed to match `Explosion`/`AoEoT`/`ShockWave`/`MeleeAttack`'s existing pattern (`attacker = caster != null ? caster : this.gameObject`, `sourceAbility = this.sourceAbility`).
- **Resolve the unused `attackTagHash`** in `ActionState.cs` — either wire up an `"Attack"` tag check (e.g. mirroring the `"Dodge"` i-frame pattern for a future gameplay need like attack-parrying) or remove the dead field.
- **Consider renaming `Attack Layer`/`Attack.mask`** to something like `UpperBody Layer`/`UpperBody.mask` for clarity, matching what they actually represent. This requires updating the `.controller` asset and any hardcoded layer-name lookups, not just a text rename — same caution as the `"Enviroment"` layer note in `CLAUDE.md`.
- **Move visual-effect spawning out of `AbilityManager` via an Observer-pattern cast-lifecycle** (not yet implemented — design agreed, pending an implementation pass):
  - Add `event Action<Ability> OnAbilityStarted`, `OnAbilityExecuted`, `OnAbilityCanceled` on `AbilityManager`, raised from the existing `StartCastingAbility()`, `ExecuteActiveAbility()`, and `CancelAbility()` methods respectively (not from `PlayerInputHandler` — `AbilityManager` is the single choke point already shared by both player and AI casters, so firing from `PlayerInputHandler` would silently skip AI-cast visuals).
  - Move `PlayVisuals()`'s body out of `AbilityManager` into a new sibling listener component (e.g. `AbilityVisualEffects`) that subscribes to those three events — mirrors how `Health`/`EffectManager` independently subscribe to `DamageReceiver.OnHitReceived` rather than being called directly.
  - Replace the single `abilityVisualParticles`/`visualEffectAudio` fields with a small list of named cues per ability (e.g. `List<AbilityVisualCue> visualCues`, each with `id`, `prefab`, `attachPoint`, `offset`, `audio`), so an ability like Kamehameha can define both a `"CastStart"` cue (played on `OnAbilityStarted` — the charge-up visual) and an `"Execute"` cue (played on `OnAbilityExecuted` — the beam), without new event types. `OnAbilityCanceled` should also destroy/stop any still-running cast-start effect if the cast is interrupted mid-charge.
  - If an ability needs an additional visual beat that isn't tied to start/execute/cancel (e.g. a mid-clip impact flash), add one more parameterized Animation Event method (`PlayAbilityVisualCue(string id)`) that looks up and fires a cue by id — same mechanism, no new event required.
  - Tradeoff: adds one layer of indirection (event → listener → cue lookup) versus today's direct field read; worth it once more than one or two abilities need multi-phase visuals, but the data migration (moving every ability asset's visual prefab from one field into a cue list) is the main cost.
