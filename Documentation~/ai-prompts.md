# AI prompt and change history

Log of AI-assisted changes to the framework, as required by the Movement Release spec.
One entry per prompt/session: what was asked, which constraints were given, what changed, and how the result was verified.

## 2026-10-03 - Package hygiene

- **Tool:** Claude Code (Claude Opus 5.5)
- **Prompt:** Clean up the UPM template: remove example classes and template docs, modernize test asmdefs, reference Input System from the runtime asmdef, fix package.json metadata, write README install instructions.
- **Changes:** Removed template placeholders (example scripts, Samples, Documentation, Third Party Notices). Added `Unity.InputSystem` reference, `physics2d` dependency, `rootNamespace` to asmdefs. Test asmdefs moved from deprecated `optionalUnityReferences` to `UnityEngine/UnityEditor.TestRunner` + `nunit.framework.dll` + `UNITY_INCLUDE_TESTS`. Dropped `unityRelease` and placeholder URLs.
- **Verification:** Dev project re-resolves the package with the new dependencies, no console errors or warnings. Test asmdefs are not exercised yet (no test scripts); verify discovery when the first tests land.

## 2026-10-03 - Core: state machine, motor, sensor (feat/core)

- **Tool:** Claude Code (Claude Opus 5.5)
- **Design (by architect, before generation):** One state machine shared by player and enemies. No central transition table: each state declares `Priority`, `CanEnter()` (re-checked while active) and `CanExit()` (blocks switching, e.g. an unfinished dash). The highest-priority enterable state wins, ties go to the first listed, the current state stays if nothing can enter. States are MonoBehaviour components (`ComponentState`) so adding a mechanic = Add Component, with Inspector config for free. `PlatformerMotor2D` is the only thing that writes Rigidbody2D velocity. `StateController` refreshes `EnvironmentSensor2D` explicitly before ticking states (no script execution order changes). Review changes: renamed `Approach` -> `AccelerateTowards`, `Probe` -> `CastForSurface` for readability; kept states as MonoBehaviour components (rejected plain `[SerializeReference]` classes: no built-in type picker, renaming a class loses data).
- **Prompt:** Implement the above plus: motor `MoveHorizontal(target, accel, decel, dt)` (accelerate only when speeding up in the same direction, otherwise decelerate), `Jump(height)` from `v = sqrt(2gh)`, gravity multiplier relative to the startup gravity scale, fall speed clamp. Sensor: casts via `Rigidbody2D.Cast` (ignores own colliders), slope limit splits ground vs wall normals, grounded only when not moving away from the ground normal (handles jump start and one-way platforms), ledge raycast past the collider edge, gizmos. XML docs on public API.

## 2026-10-04 - Player state system (feat/player)

- **Tool:** Claude Code (Claude Opus 5.5)
- **Design (by architect):** Minimal player layer the spec expects ("Ground/Air/Wall states", Move and Jump done so teammates add new mechanics). `PlayerState` exposes Motor/Sensor/InputReader via lazy lookups (subclass `Awake` cannot break them). Input read by action name so new mechanics need no package changes.
- **Prompt:** `PlayerInputReader` (timestamped presses with buffer, `WasPressed(name, buffer)`, `ConsumePress`, `IsHeld`, digital `Horizontal`/`Vertical`, project-wide actions fallback), `PlayerController`, `GroundState`, `AirState` (air control, fall gravity, jump-release gravity for variable height, max fall speed), `JumpState` (priority 10, jump buffer, coyote time that cannot grant a second jump).
- **Bug found by tests:** priority set in `ComponentState.Reset()` is skipped for components added from code, so `JumpState` lost ties to `GroundState`. Fixed by making `Priority` a code-defined virtual property.
- **Review changes:** input switched from string map/action names to `InputActionReference` fields (architect's call: no strings; a generated actions class cannot be used because package code cannot reference the game's Assembly-CSharp, and a package-owned actions asset would be read-only for teammates). Presses are recorded via `InputSystem.onActionChange`, so any button can be buffered without registering it. Tests assign references via reflection to avoid test-only public setters. Removed digital Horizontal/Vertical conversion: deadzones and keyboard diagonals belong in the actions asset (composite Mode = Digital), states read raw `Move.x` (analog stick = analog speed). Gravity scale default removed (left to library users); slope support removed (no slopes in the game): sensor uses vertical velocity instead of ground normal.
