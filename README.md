# KSE 2D Platformer Framework

2D platformer framework for player and enemies: state-driven movement, environment checks and Inspector-tuned parameters.

## Requirements

- Unity 6000.3 or later
- Git installed (needed by the Package Manager for git dependencies)
- Read access to this repository (it is private): you must be a collaborator, and git must be able to authenticate to GitHub non-interactively (Git Credential Manager for HTTPS, bundled with Git for Windows, or an SSH key loaded in ssh-agent if you use the `git@github.com:` URL)

## Installation

Window > Package Manager > + > Install package from git URL:

```
https://github.com/ShcherbaDev/kse-unity-workshop-movement-release-framework.git#v0.1.0
```

Or add it to `Packages/manifest.json`:

```json
"com.yenotsoftware.kse2dplatformerframework": "https://github.com/ShcherbaDev/kse-unity-workshop-movement-release-framework.git#v0.1.0"
```

### Updating

Change the tag after `#` to the new version. See [CHANGELOG.md](CHANGELOG.md) for what changed.

## Quick start

1. Window > Package Manager > KSE 2D Platformer Framework > Samples > **Demo** > Import.
2. Add layers in Project Settings > Tags and Layers: **8 = Ground**, **9 = Player**, **10 = Enemy** (the demo prefabs use these numbers).
3. Open `Assets/Samples/KSE 2D Platformer Framework/<version>/Demo/Demo.unity` and press Play. Move with WASD / left stick, jump with Space / gamepad South.

The demo was made with the Universal 2D template (URP); in a Built-in render pipeline project the sprites need a different material.

The demo prefabs (`Player`, `Patrol Enemy`) are the easiest starting point for your own characters: duplicate them and add your states.

## How it works

Every character runs a **state machine** made of components:

- A **controller** (`PlayerController` or `EnemyController`) collects all **state** components on the same GameObject and ticks one of them every `FixedUpdate`.
- Each state decides on its own when it wants to be active. There is no transition table, so adding a mechanic never requires editing existing states or the package.
- Every physics tick the controller activates the **highest-priority state whose `CanEnter()` returns true**. The active state can refuse to be interrupted with `CanExit()` (e.g. a dash in progress).

```
State lifecycle:  CanEnter() -> Enter() -> Tick() every physics tick -> Exit()
```

The spec's "player states" and "enemy behaviours" are the same thing here: `PlayerState` and `EnemyState`.

### Building blocks

| Component | What it does |
|---|---|
| `PlatformerMotor2D` | Movement API over Rigidbody2D: `MoveHorizontal(targetSpeed, acceleration, deceleration, dt)`, `Jump(height)`, `GravityMultiplier`, `ClampFallSpeed`, `Velocity`, `SetVelocityX/Y`, `FacingDirection` |
| `EnvironmentSensor2D` | `IsGrounded`, `LastGroundedTime`, `IsTouchingWallLeft/Right`, `WallDirection`, `IsTouchingCeiling`, `HasGroundAhead(direction)`. Draws its checks as gizmos when selected |
| `PlayerInputReader` | `Move` (Vector2) and buffered button presses: `WasPressed(action, bufferTime)`, `ConsumePress(action)` |
| `TargetSensor2D` | `FindTarget()`: a target (e.g. the player) on the given layers within a radius |
| `FacingFlipper` | Mirrors a visual child to match `FacingDirection` |

Included states: `GroundState` (run), `AirState` (air control, fall gravity, variable jump height), `JumpState` (jump buffer, coyote time), `PatrolState` (enemy walks and turns at walls and ledges).

## Setting up a character

1. Add a `Collider2D` (e.g. `BoxCollider2D`) with a **frictionless Physics Material 2D** (friction 0), otherwise characters stick to walls. The demo includes one.
2. Add `PlayerController` or `EnemyController`. It adds `Rigidbody2D`, `PlatformerMotor2D` and `EnvironmentSensor2D` (and `PlayerInputReader` for the player).
3. On `EnvironmentSensor2D`, set **Ground Layers** to your level layer only (e.g. Ground). Never include Player or Enemy layers, or characters will treat each other as walls.
4. Add states. Player: `GroundState`, `AirState`, `JumpState`. Enemy: `PatrolState`.
5. Player input: drag actions from an `.inputactions` asset into the fields: `Move Action` on `PlayerInputReader`, `Jump Action` on `AirState` and `JumpState`.
6. Optional: put sprites on a child object and add `FacingFlipper` pointing to it.

Rigidbody2D **Gravity Scale** shapes the jump arc together with the states' settings; the demo uses 3.

### Input actions

Deadzones and keyboard behaviour live in the `.inputactions` asset, not in code. For WASD / arrows set the 2D Vector composite **Mode = Digital**; with the default "Digital Normalized", pressing two keys (e.g. W + D) runs at 71% speed.

## Adding a player mechanic

Create a class that inherits `PlayerState` in your project (never edit the package), then add it to the player as a component. `Motor`, `Sensor` and `InputReader` are ready to use.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using Yenotsoftware.KSE2DPlatformerFramework;

public class GlideState : PlayerState
{
	[SerializeField] private InputActionReference _jumpAction;
	[SerializeField, Min(0f)] private float _maxFallSpeed = 2f;
	[SerializeField, Min(0f)] private float _speed = 6f;
	[SerializeField, Min(0f)] private float _acceleration = 30f;

	// Above AirState (0) so it takes over while falling, below jumps (10)
	public override int Priority => 5;

	public override bool CanEnter()
	{
		return !Sensor.IsGrounded && Motor.Velocity.y < 0f && _jumpAction.action.IsPressed();
	}

	public override void Tick(float deltaTime)
	{
		Motor.MoveHorizontal(InputReader.Move.x * _speed, _acceleration, _acceleration, deltaTime);
		Motor.ClampFallSpeed(_maxFallSpeed);
	}
}
```

Tips:

- Read button presses with `InputReader.WasPressed(action)` and call `InputReader.ConsumePress(action)` in `Enter()`, so one press triggers the mechanic once. A generated actions class works too: `InputReader.WasPressed(_input.Player.Dash)`.
- If your state changes `Motor.GravityMultiplier`, reset it to 1 in `Exit()`.
- Return `false` from `CanExit()` while the mechanic must not be interrupted (dash, ground pound), and `true` once it is done.

## Adding an enemy state

Same idea with `EnemyState`. States that need the player add `[RequireComponent(typeof(TargetSensor2D))]` and set its **Target Layers** to the Player layer.

```csharp
using UnityEngine;
using Yenotsoftware.KSE2DPlatformerFramework;

[RequireComponent(typeof(TargetSensor2D))]
public class ChaseState : EnemyState
{
	[SerializeField, Min(0f)] private float _speed = 4f;
	[SerializeField, Min(0f)] private float _acceleration = 30f;

	// Interrupts PatrolState (0) while the player is close
	public override int Priority => 10;

	public override bool CanEnter()
	{
		return Sensor.IsGrounded && TargetSensor.FindTarget();
	}

	public override void Tick(float deltaTime)
	{
		Transform target = TargetSensor.FindTarget();
		if (!target)
			return;

		int direction = target.position.x > transform.position.x ? 1 : -1;
		// Stop at the edge instead of running off the platform
		float speed = Sensor.HasGroundAhead(direction) ? direction * _speed : 0f;
		Motor.MoveHorizontal(speed, _acceleration, _acceleration, deltaTime);
	}
}
```

An enemy type is a prefab: `EnemyController` + the states it should have (e.g. `PatrolState` + `ChaseState`).

## Priorities

Override `Priority` in code. Higher wins when several states can enter at once.

| Priority | Use for |
|---|---|
| 0 (default) | Base movement: `GroundState`, `AirState`, `PatrolState` |
| 1-9 | Variations of base movement: glide, wall slide, crouch |
| 10 | Jumps and reactions: `JumpState`, double jump, wall jump, chase |
| 20+ | Abilities that override everything: dash, ground pound, charge attack |

## Known limitations

- No slopes: ground is expected to be flat.
- Moving platforms that carry the character upward are not supported yet.
