# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Package skeleton: Runtime, Editor and Tests assemblies.
- `StateMachine<TState>` / `IState`: priority-based state selection; each state declares `CanEnter`/`CanExit`, no transition table.
- `ComponentState` and `StateController<TState>`: states as components on a character, ticked every physics step.
- `PlatformerMotor2D`: movement API over Rigidbody2D (acceleration/deceleration, jump by height, gravity multiplier, fall speed clamp, facing).
- `EnvironmentSensor2D`: ground/wall/ceiling checks, ledge-ahead check, last grounded time, gizmos.
- `PlayerController`, `PlayerState`: player state system; `PlayerState` gives mechanics access to `Motor`, `Sensor` and `InputReader`.
- `PlayerInputReader`: move input (`InputActionReference`) and timestamped, bufferable button presses (`WasPressed(action, buffer)`, `ConsumePress`). States take their buttons as `InputActionReference` fields, no string names.
- `GroundState`, `AirState`, `JumpState`: run with acceleration/deceleration, air control, fall and jump-release gravity, max fall speed, jump by height with jump buffer and coyote time.
