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
