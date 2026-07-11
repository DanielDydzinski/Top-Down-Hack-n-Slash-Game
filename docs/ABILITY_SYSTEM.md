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

**Visual effects** no longer live in `AbilityManager` at all — see §11 for the cue-based Observer system that replaced the old single-slot `PlayVisuals()` method.

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

`ActionState.cs:10` declares `attackTagHash = Animator.StringToHash("Attack")` but it's **never read anywhere in the file** — vestigial, likely a planned-but-unfinished mirror of the Dodge-tag check (see §11).

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

## 9. Ability Visual Effects — cue-based Observer system

Visual/audio effects are no longer spawned from `AbilityManager` at all. It only announces cast lifecycle via three events — `OnAbilityStarted`, `OnAbilityExecuted`, `OnAbilityCanceled` (all `Action<Ability>`), raised from `StartCastingAbility()`, `ExecuteActiveAbility()`, and `CancelAbility()` respectively. This fires for **both player and AI casters**, since `AbilityManager` is the one choke point both go through — deliberately not raised from `PlayerInputHandler`, which would silently miss AI casts.

`AbilityVisualEffects` (`Assets/Scripts/Abilities/AbilityVisualEffects.cs`) is a sibling `MonoBehaviour` (same GameObject as `AbilityManager`, same relationship `EffectManager` has to `DamageReceiver`) that subscribes to those three events in `OnEnable`/unsubscribes in `OnDisable`, and owns all cue spawning. `AbilityManager` has zero references to it — a second, independent listener (e.g. a future camera-shake or screen-flash reactor) could subscribe to the same three events without touching either file.

It also exposes one **static** helper unrelated to cues: `ResolveGroundImpactPosition(GameObject caster, Vector3 fallbackPosition)`, used by all five `Behaviours/*.cs` classes for the separate, older `BaseAbilitySettings.scalesWithFallDistance`/`groundImpactPrefab` fall-damage-scaling feature. Those five previously spawned `groundImpactPrefab` at their own `transform.position` — the ability prefab's own spawn point (`AbilityManager.spawnLocation`, a chest/hand-height socket for most abilities), not ground level. The helper resolves to `VisualFeetAttachPoint` if the caster has one configured, else the caster's own root transform (correct for a `CharacterController`-driven character, whose pivot sits at the feet), else the passed-in fallback if there's no caster at all.

**`AbilityVisualCue`** (`Assets/Scripts/Abilities/AbilityVisualCue.cs`) replaces the old single `abilityVisualParticles`/`visualEffectAudio` fields with a `List<AbilityVisualCue> visualCues` on `Ability` (sibling to `abilityEffects`, same list-based convention) — any ability can define as many cues as it needs:
- `id` (string) + `trigger` (`Manual`/`OnStart`/`OnExecute`/`OnCancel`) — `OnStart`/`OnExecute`/`OnCancel` cues fire automatically off the matching `AbilityManager` event, no Animation Event required. `Manual` cues only fire via `AbilityVisualEffects.PlayAbilityVisualCue(string id)`, called from an Animation Event for frame-accurate mid-clip beats (e.g. an impact flash) that don't correspond to a lifecycle boundary. This is how Kamehameha-style multi-phase abilities get expressed: a `{trigger: OnStart}` charge-glow cue and a separate `{trigger: OnExecute}` beam cue, no animation retiming needed for either.
- `prefab`/`attachPoint`/`positionOffset`/`rotationOffset`/`audio` — reuses the existing `VisualAttachPoint` enum (`Root, LeftHand, RightHand, Head, Weapon, Feet`) and offset-vector convention from `BaseAbilitySettings`. Per-cue, not per-ability, so one ability's cues can live on different attach points.
- `parentToAttachPoint` — true (default) parents the spawned instance to the resolved attach point so it follows that transform for its whole lifetime, matching the old system's always-parented behavior. False spawns it once at that world position/rotation, unparented — for an effect that should stay put (e.g. a ground-anchored burst) rather than track the caster.
- `delayStartTime` — 0 spawns the instant the cue's trigger fires (default); >0 defers the actual `Instantiate`/`SpawnFromPool` + audio by that many seconds, so a cue can appear a beat late without needing a Start Delay authored into every particle system that wants one. Applies to any cue regardless of trigger type. Position/rotation are resolved at the *actual* spawn moment (inside the delay coroutine), not when the delay started, so a delayed cue still lines up correctly with a moving attach point (e.g. a swinging hand) rather than spawning at a stale, pre-delay position.
- `persistUntilNextCue` — for a looping/sustained effect (a charge-up glow) that must be force-stopped rather than left to finish itself; `AbilityVisualEffects` tracks at most one such instance at a time and clears it the moment another persistent cue fires **or the cast reaches Execute/Cancel** (`HandleExecuted`/`HandleCanceled` both unconditionally call `ClearPersistentEffect()` before firing their own cues — Execute always ends the "in-progress cast" phase even when the Execute cue itself isn't persistent, otherwise a persistent Start-phase effect would never get cleaned up on a normal, uninterrupted cast). **Cancel is the one path that bypasses `delayedDestroyTime` entirely** — `HandleCanceled` calls `ClearPersistentEffect(immediate: true)` and also `FlushPendingRemoval()` (force-finishing any still-fading-out effect left over from an earlier Execute handoff), since a cancel is an abrupt stop with no next phase to hand off to, not a controlled transition like Execute.
- `autoReturnDelay` — for prefabs with no self-retiring `DestroyObject` component or particle Stop Action. Only meaningful for non-persistent cues (spawn-and-forget).
- `delayedDestroyTime` — only meaningful for `persistUntilNextCue` cues. `ClearPersistentEffect()` normally returns the tracked instance to the pool the instant it's told to (new persistent cue, Execute, or Cancel); setting this delays that specific removal by N seconds instead, so a hard particle burst/fade can finish playing instead of vanishing on the same frame the next cue starts (e.g. Kamehameha's charge orb lingering briefly instead of hard-cutting the instant the beam fires). Implemented as a tracked coroutine (`WaitForSeconds`, respects `Time.timeScale`) — a still-pending delayed removal is force-flushed in `OnDisable` so it can't leak if the component is disabled mid-delay. Not tied to any specific ability or phase — any persistent cue on any ability can use it.

**Spawning goes through `ObjectPooler`** (`Assets/Scripts/GlobalSystems/ObjectPooler.cs`), the same pool-first/`Instantiate`-fallback convention already used by `MeleeAttackBehavaiour` etc. — `ObjectPooler.Instance.SpawnFromPool(...)` when a pooler exists, otherwise a plain `Instantiate` fallback. Cleanup uses `ReturnToPool` (or `Destroy` in the no-pooler fallback case), not a raw `Destroy` call.

**Migration**: a one-time, disposable Editor tool (`Assets/Scripts/Abilities/Editor/AbilityVisualCueMigrator.cs`, `Tools/Migrate Ability Visuals To Cues`) converts every ability's old single visual into an equivalent `{id: "Execute", trigger: OnExecute}` cue, so the 7 abilities that used the old system (`HeroPunchMidAIr`, `JumpSlamMidAir`, `JumpSlam`, `HuricaneKick`, `FlyingBicycleKickAnimLong`, plus 2 FBX-embedded clips) keep working with zero manual re-entry — same disposable-migrator pattern already used and removed for the `baseSettings` migration (§2). `BaseAbilitySettings.abilityVisualParticles`/`visualEffectAudio`/`attachPoint`/`spawnLocationOffset`/`spawnRotationOffset` are intentionally **not** deleted yet — they remain only as the migration tool's read source, to be retired in a later pass once the cue system has been used in practice.

A backward-compatible `[Obsolete] PlayVisuals()` no-op stub lives on `AbilityVisualEffects` (not `AbilityManager`) so the still-present `PlayVisuals` Animation Events in those 7 clips don't log a missing-receiver warning — they're now redundant (the migrated cue already auto-fires off `OnAbilityExecuted` at the same moment) and should eventually be removed from the `.anim` clips via the Editor's Animation window, not by hand-editing `.anim` YAML.

## 10. Design patterns in play

- **Strategy** — `IPlayerState` states, `Effect` subclasses, `Ability` subclasses. New behavior = new class, not a new branch in shared code.
- **Observer** — `DamageReceiver.OnHitReceived` → `Health` + `EffectManager`, independently, per `CLAUDE.md`; and now `AbilityManager`'s three cast-lifecycle events → `AbilityVisualEffects` (§9), the same shape applied to the ability system.
- **Animator-int as a state-machine signal channel** — not a standard GoF pattern, but a deliberate recurring technique here: `ActionState`/`MidFallAttackState` don't get a direct "cast finished" callback from `AbilityManager`; they poll an Animator int (`AttackState`) that `AbilityManager` clears. Useful to recognize since it's easy to mistake for a bug (a state watching itself) rather than the intended handshake mechanism.
- **Data-driven ScriptableObject config** — all ability settings live in `baseSettings`/the subclass settings struct. Prefer extending that struct convention (and the newer `Initialize(...)`-based `Cast()` handoff, see §11) rather than adding flat fields directly to a class.

## 11. Suggested improvements (ideas for future sessions)

- ~~Fix the cooldown pause bug (§5)~~ — **done**: `RunCoolDown` now accumulates `Time.deltaTime` instead of reading a real-wall-clock `Stopwatch`, so cooldowns correctly freeze when `Time.timeScale = 0`.
- ~~Retire the base/legacy field migration~~ — **done**: the legacy fields on `Ability` and all five subclasses, plus every `MigrateEverything()`/`GlobalXMigratorShortcut` editor tool, have been deleted. All 37 ability `.asset` files already carried real, populated `baseSettings`/subclass-settings data before removal (verified field-by-field), so no asset data was lost. Fixing this also closed a live bug: `AbilityManager.cooldowns` is keyed by `baseSettings.abilityName`, but `ComboController.cs` and `AbilitySlotUI.cs` were looking that dictionary up using the legacy `.abilityName` field — for `HeroAttackMidAir.asset`, whose two name fields had drifted apart (`"HeroMidAir"` vs. legacy `"HeroExplo"`), that meant casting/reading its cooldown either threw or silently read as "always ready." Both call sites now key off `baseSettings.abilityName` like the dictionary itself does.
- ~~Standardize the `Cast()` → behaviour handoff~~ — **done**: `FireBallBehaviour`, `ExplosionBehaviour`, and `AoEoTBehaviour` now expose `Initialize(this, baseSettings, subSettings, effects, caster)` matching `ShockWaveBehaviour`/`MeleeAttackBehavaiour`, replacing their old flat-parameter `UpdateValues(...)` calls; each now computes its own `fallDistanceMultiplier` internally instead of receiving it as a parameter, which let the now-unused `Ability.GetFallDistanceDamageMultiplier(GameObject)` wrapper be deleted. This surfaced (and fixed) a real gap: none of these three stamped `sourceAbility` onto their `HitInfo`, so `TakeDamage.ApplyEffect`'s energy-gain-on-hit/refund-on-kill logic (`info.attacker != null && info.sourceAbility != null` check, `Assets/Scripts/Abilities/Ability Effects/TakeDamage.cs:33`) silently no-opped for `FireBall`/`Explosion`/`AoEoT` casts even when `baseSettings.energyGainOnHit`/`energyRefundOnKillPercent` were configured. `FireBallBehaviour` additionally had `HitInfo.attacker` set to the projectile GameObject itself instead of `caster` — since `TakeDamage` looks up `info.attacker.GetComponent<PlayerEnergy>()`, that alone would have kept the feature dead even after wiring `sourceAbility`. Both are now fixed to match `Explosion`/`AoEoT`/`ShockWave`/`MeleeAttack`'s existing pattern (`attacker = caster != null ? caster : this.gameObject`, `sourceAbility = this.sourceAbility`).
- **Resolve the unused `attackTagHash`** in `ActionState.cs` — either wire up an `"Attack"` tag check (e.g. mirroring the `"Dodge"` i-frame pattern for a future gameplay need like attack-parrying) or remove the dead field.
- **Consider renaming `Attack Layer`/`Attack.mask`** to something like `UpperBody Layer`/`UpperBody.mask` for clarity, matching what they actually represent. This requires updating the `.controller` asset and any hardcoded layer-name lookups, not just a text rename — same caution as the `"Enviroment"` layer note in `CLAUDE.md`.
- ~~Move visual-effect spawning out of `AbilityManager` via an Observer-pattern cast-lifecycle~~ — **done**, see §9. Remaining follow-ups from that work, not yet done:
  - Add `AbilityVisualEffects` to `Player.prefab` and the 6 enemy prefabs that had `VisualLeftHandAttachPoint`/`VisualRightHandAttachPoint` configured on the old `AbilityManager` fields (`Spider2Enemy`, `ZombieEnemy`, `MutantEnemy`, `SpiderEnemy`, `GoblinEnemy`, `Spartan`), and re-wire those two Transform references onto the new component (Unity has no mechanism to auto-migrate a serialized reference across components).
  - Run the `Tools/Migrate Ability Visuals To Cues` Editor menu item once to convert the 7 previously-visual abilities' data into cues.
  - Remove the now-redundant `PlayVisuals` Animation Event key from the 5 `.anim` files + 2 FBX-embedded clips listed in §9, via the Editor's Animation window.
  - Once the cue system has proven out in practice, delete `BaseAbilitySettings.abilityVisualParticles`/`visualEffectAudio`/`attachPoint`/`spawnLocationOffset`/`spawnRotationOffset` (kept for now only as the migration tool's read source) and the migrator itself.
