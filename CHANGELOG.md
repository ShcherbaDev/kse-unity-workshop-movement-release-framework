# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-10-13

### Added

- `CharacterAnimator`: plays the active state's animation on an Animator instantly (`Animator.Play`, for sprite sheets), writes `Speed`, `VerticalSpeed` and `Grounded` parameters, puts the active state's animation back whenever the Animator leaves it (re-enable, pooling, `Rebind`, a swapped Animator Controller, whose parameters are looked up again), warns about unknown states, missing Animator and Cull Completely. The Animator Controller must have no transitions. When added, it fills in the Animator from its children (inactive ones included) and adds the relay next to it, with Undo.
- `ComponentState.AnimationState` (Inspector field), `IsAnimationFinished` (end states with their clip, never stuck) and `OnAnimationEvent` (events from the state's own animation only).
- `AnimationEventRelay`: forwards Animation Events (function `Raise`, String parameter) from the Animator's GameObject; `CharacterAnimator.AnimationEventRaised` for VFX and sound.
- Demo sample: Animator controllers and clips (locomotion blend tree, air, hurt, patrol), `PlayerHurtState` with a test key (H).
- README: Animation section with Hurt and Cast examples, priorities 30 (reactions) and 40 (death).

### Changed

- Package depends on `com.unity.modules.animation`.
- `StateController` passes each new state to a `CharacterAnimator` on the same GameObject. Characters without one behave as in 0.1.0.

### Upgrade notes

- `ComponentState` now serializes a field named `_animationState`. If one of your states declares a serialized field with the same name, rename it: Unity cannot serialize the same field name in a class and its base class.
- Abilities belong at priorities 20-29, below reactions (30) and death (40), so Hurt and Death can follow them.

## [0.1.0] - 2026-10-04

### Added

- Package skeleton: Runtime, Editor and Tests assemblies.
- `StateMachine<TState>` / `IState`: priority-based state selection; each state declares `CanEnter`/`CanExit`, no transition table.
- `ComponentState` and `StateController<TState>`: states as components on a character, ticked every physics step.
- `PlatformerMotor2D`: movement API over Rigidbody2D (acceleration/deceleration, jump by height, gravity multiplier, fall speed clamp, facing).
- `EnvironmentSensor2D`: ground/wall/ceiling checks, ledge-ahead check, last grounded time, gizmos.
- `PlayerController`, `PlayerState`: player state system; `PlayerState` gives mechanics access to `Motor`, `Sensor` and `InputReader`.
- `PlayerInputReader`: move input (`InputActionReference`) and timestamped, bufferable button presses (`WasPressed(action, buffer)`, `ConsumePress`). States take their buttons as `InputActionReference` fields, no string names.
- `GroundState`, `AirState`, `JumpState`: run with acceleration/deceleration, air control, fall and jump-release gravity, max fall speed, jump by height with jump buffer and coyote time.
- `EnemyController`, `EnemyState`: enemy state system; an enemy type is a prefab with the controller and a set of states (the spec's "enemy behaviours").
- `TargetSensor2D`: finds a target (e.g. the player) by layer within a radius, so enemy prefabs never reference scene objects.
- `PatrolState`: walks back and forth, turns at walls and ledges, stands still when both sides are blocked.
- `FacingFlipper`: mirrors a visual child to match the motor's facing direction.
- Demo sample: playable scene with player and patrolling enemies, prefabs, input actions (WASD composite in Digital mode), frictionless material.
- README: setup, layers, input, adding player and enemy states, priority convention.
