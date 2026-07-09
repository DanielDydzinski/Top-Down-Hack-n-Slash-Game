# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.0.42f1** (Unity 6) third-person action combat game. URP render pipeline. Legacy Input Manager (no `com.unity.inputsystem` — input is read via `Input.GetKey(...)`). No Cinemachine, no DOTween. NavMesh AI navigation (`com.unity.ai.navigation`) drives enemy movement.

Git LFS tracks binary assets, including `.mat`, `.asset`, and `.anim` (not just meshes/textures/audio) — see `.gitattributes`. No test framework is wired up for gameplay code (`com.unity.test-framework` is present but unused — no `Assets/Tests`).

An `.editorconfig` at the repo root defines C# naming/style conventions (advisory only, nothing enforces it yet). It only takes effect once Unity has generated the `.csproj`/`.sln` files (gitignored, created on first Editor open) — `dotnet format` can then be run against the generated `.sln`.

`Assets/Scripts/` is the project's own code. Most other top-level `Assets/` folders (`LeartesStudios`, `PolyLabs`, `PolyKebap`, `Devion Games`, etc.) are third-party Asset Store packages — `Devion Games` in particular is a large legacy inventory/stat/controller framework that looks largely superseded by the custom systems in `Scripts/` and unused by them.

## Architecture

The codebase deliberately favors two patterns — match them when adding new behavior instead of branching on type checks in shared code:

- **Strategy pattern for states and effects.** Player states implement `IPlayerState` (`EnterState`/`UpdateState`/`ExitState`) and are swapped via `PlayerStateMachine.SwitchState`. Enemy AI has a parallel, independent `IState` hierarchy (`Assets/Scripts/Ai/`) — not the same interface, don't conflate them. Ability payloads are ScriptableObject `Effect` subclasses (`TakeDamage`, `TakeDoT`, `TakeHeal`, `TakeHoT`, `TakeKnockBack`, `TakePush`, `TakeSlow`) implementing `ApplyEffect(GameObject target, HitInfo info)`. A new status effect or player state should be a new class implementing the interface/base, not a new case added to an existing effect/state.
- **Observer pattern for the damage pipeline.** `DamageReceiver.TakeDamage` is the single entry point (`IDamageable`); it fires `OnHitReceived`, which `Health` and `EffectManager` both subscribe to independently and react to without knowing about each other. `Health` additionally exposes `OnDeath`, `OnDamageTaken`, `OnBlockSuccess` for other systems (VFX, UI, energy) to hook into. When wiring up new reactions to combat events, subscribe to these events rather than adding direct calls between systems.

Ability flow: `Ability` (ScriptableObject) → `Cast()` instantiates a prefab running a matching `Behaviours/` MonoBehaviour (e.g. `MeleeAttackBehavaiour`, `FireBallBehaviour`, `ShockWaveBehaviour`, `AoEoTBehaviour`) → behaviour builds a `HitInfo` and calls `IDamageable.TakeDamage` on whatever it hits → `DamageReceiver` resolves dodge/block once, then fans out via `OnHitReceived` to `Health` (applies damage/mitigation, flinch, death) and `EffectManager` (runs the ability's `Effect` list as coroutines).

For a deep dive on the ability-casting pipeline (ScriptableObject abilities/effects, `AbilityManager` cooldowns, Animator `AttackState` int, attack tags/layers, `ComboController`, and how `PlayerStateMachine` states relate to casting), see `docs/ABILITY_SYSTEM.md`.

## Critical gotcha: `HitInfo` is a struct

`HitInfo` (`Assets/Scripts/GlobalSystems/CombatDataContainers.cs`) is a **value type**, not a class. Mutating a field inside one method (e.g. inside `Health.Damage`) does **not** propagate back to a caller's copy, and does not propagate sideways to a sibling subscriber's copy of the same event payload (`Health` and `EffectManager` each get their own copy from `OnHitReceived`).

This is why block-mitigation resolution lives in `DamageReceiver.TakeDamage`, resolved *once*, **before** `OnHitReceived` fires — every downstream listener (and every `Effect`, including `TakeDoT`, which caches `info.isBlocked` at the start of its tick loop to mitigate every subsequent tick) needs to see the same already-resolved flag. Don't move block/mitigation resolution into `Health.Damage` or any single `Effect` — anything computed there dies with that method call and won't reach sibling effects or later ticks. If you need a new per-hit resolved value to reach multiple listeners, resolve it in `DamageReceiver.TakeDamage` and stamp it onto `info` before invoking `OnHitReceived`.

## Naming/spelling quirks (grep-unfriendly — don't assume the "obvious" name)

- `Assets/Scripts/GlobalSystems/DamageReciever.cs` — filename misspelled ("Reciever"); class inside is correctly `DamageReceiver`.
- `Assets/Scripts/Abilities/Behaviours/MeleeAttackBehavaiour.cs` — both filename and class name are misspelled "Behavaiour" (not a typo to "fix", it's the real identifier).
- `Assets/Scripts/GlobalSystems/EffectSpawnPossitions.cs` — "Possitions" typo, class name matches.
- `Assets/Scripts/Ai/PushPropoganda.cs` — "Propoganda" typo.
- `Assets/Scripts/Health/healthBar.cs` — lowercase-first class/file name, inconsistent with PascalCase used elsewhere.
- The Unity layer used for environment raycasts is literally named `"Enviroment"` (missing the second "n") — `Ability.OnEnable()` hardcodes `LayerMask.GetMask("Enviroment", "InteractableEnvironment")` to match. Don't "fix" the spelling in code without also renaming the actual Unity layer, or raycasts will silently stop matching.
- `Assets/Scripts/pickUps/` (lowercase, under Scripts) and `Assets/PickUps/` (top-level Assets folder) are different folders — easy to grab the wrong one.
