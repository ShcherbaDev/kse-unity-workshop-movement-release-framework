# KSE 2D Platformer Framework

2D platformer framework for player and enemies: state-driven movement, environment checks and Inspector-tuned parameters.

## Requirements

- Unity 6000.3 or later
- Git installed (needed by the Package Manager for git dependencies)
- Read access to this repository (it is private): you must be a collaborator, and git must be able to authenticate to GitHub non-interactively (Git Credential Manager for HTTPS, bundled with Git for Windows, or an SSH key loaded in ssh-agent if you use the `git@github.com:` URL)

## Installation

Window > Package Manager > + > Install package from git URL:

```
https://github.com/ShcherbaDev/kse-unity-workshop-movement-release-framework.git#v0.3.0
```

Or add it to `Packages/manifest.json`:

```json
"com.yenotsoftware.kse2dplatformerframework": "https://github.com/ShcherbaDev/kse-unity-workshop-movement-release-framework.git#v0.3.0"
```

### Updating

Change the tag after `#` to the new version. See [CHANGELOG.md](CHANGELOG.md) for what changed.

## Quick start

1. Window > Package Manager > KSE 2D Platformer Framework > Samples > **Demo** > Import.
2. Add layers in Project Settings > Tags and Layers: **8 = Ground**, **9 = Player**, **10 = Enemy** (the demo prefabs use these numbers).
3. Open `Assets/Samples/KSE 2D Platformer Framework/<version>/Demo/Demo.unity` and press Play. Move with WASD / left stick, jump with Space / gamepad South. Press H to test the Hurt reaction.

The demo was made with the Universal 2D template (URP); in a Built-in render pipeline project the sprites need a different material.

The demo prefabs (`Player`, `Patrol Enemy`) are the easiest starting point for your own characters: duplicate them and add your states.

## How it works

Every character runs a **state machine** made of components:

- A **controller** (`PlayerController` or `EnemyController`) collects all **state** components on the same GameObject and ticks one of them every `FixedUpdate`.
- Each state decides on its own when it wants to be active. There is no transition table, so adding a mechanic never requires editing existing states or the package.
- Every physics tick the controller activates the **highest-priority state whose `CanEnter()` returns true**. The active state can refuse to be interrupted with `CanExit()` (e.g. a dash in progress); only higher-priority states with `CanInterrupt => true` (Hurt, Death) get through.

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
| `CharacterAnimator` | Plays the active state's animation on the Animator, writes `Speed`/`VerticalSpeed`/`Grounded`, routes Animation Events. See [Animation](#animation) |
| `AnimationEventRelay` | Sits next to the Animator and passes its Animation Events to `CharacterAnimator`. Added automatically |

Included states: `GroundState` (run), `AirState` (air control, fall gravity, variable jump height), `JumpState` (jump buffer, coyote time), `PatrolState` (enemy walks and turns at walls and ledges).

## Setting up a character

1. Add a `Collider2D` (e.g. `BoxCollider2D`) with a **frictionless Physics Material 2D** (friction 0), otherwise characters stick to walls. The demo includes one.
2. Add `PlayerController` or `EnemyController`. It adds `Rigidbody2D`, `PlatformerMotor2D` and `EnvironmentSensor2D` (and `PlayerInputReader` for the player).
3. On `EnvironmentSensor2D`, set **Ground Layers** to your level layer only (e.g. Ground). Never include Player or Enemy layers, or characters will stand on each other's checks as ground. To make other characters block like walls (enemies turn at each other, a charge stops at the enemy in its way), put their layers into **Obstacle Layers** instead (e.g. Enemy on enemy prefabs): those count as walls only.
4. Add states. Player: `GroundState`, `AirState`, `JumpState`. Enemy: `PatrolState`.
5. Player input: drag actions from an `.inputactions` asset into the fields: `Move Action` on `PlayerInputReader`, `Jump Action` on `AirState` and `JumpState`.
6. Optional: put sprites on a child object and add `FacingFlipper` pointing to it.
7. Optional: animations, see [Animation](#animation).

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
| 20-29 | Abilities that override movement: dash, ground pound, charge attack |
| 30 | Reactions: Hurt. `CanInterrupt => true`, so they cut abilities that cannot exit yet |
| 40 | Death: `CanInterrupt => true` and never exits, so it cuts Hurt and abilities and nothing interrupts it |

`CanExit() => false` blocks every state except higher-priority ones with `CanInterrupt => true`. `CanInterrupt` is false by default, so a state never gets cut off unless a stronger state asks for it.

## Animation

Characters show their active state on an Animator. The state machine decides when states start and end (`CanEnter()`, `CanExit()`, `Priority`), the Animator only displays them, so the Animator graph has no transitions and can never disagree with gameplay.

### Setup

1. Put the sprites on a child (e.g. `Visual`, the one `FacingFlipper` flips) and add an **Animator** with your Animator Controller to it.
2. Add **`CharacterAnimator`** to the character root. When added, it fills in the first Animator in children (inactive ones included) and adds an `AnimationEventRelay` next to it. If you assign the Animator later, the relay is added when the game starts.
3. In each state's Inspector set **Animation State** to the Animator state to play while it is active. Leave it empty to keep the current animation (e.g. `JumpState`, which is active for one physics tick).

Switches are instant (`Animator.Play`), as sprite sheets need: sprites cannot be blended. A state with the same Animation State as the previous state continues the animation instead of restarting it.

### The Animator Controller

States at the root of the Base Layer (no sub-state machines, other layers are not played), named by their short name; an unknown name logs a warning once. Do not add transitions: `CharacterAnimator` keeps the Animator in the active state's animation, so a transition would be undone. For example for the player:

| Animator state | Content | Played by |
|---|---|---|
| `Locomotion` | 1D blend tree by `Speed`: Idle at 0, Run at 1 | `GroundState` |
| `Air` | 1D blend tree by `VerticalSpeed`: Fall at -1, Rise at 1 | `AirState` |
| `Hurt`, `Death`, `Cast` | single clips | your states |

`CharacterAnimator` writes these parameters every frame if the controller has them (rename them in its Inspector; missing ones are skipped):

| Parameter | Type | Value |
|---|---|---|
| `Speed` | Float | absolute horizontal speed, units per second |
| `VerticalSpeed` | Float | vertical speed, positive when rising |
| `Grounded` | Bool | `EnvironmentSensor2D.IsGrounded` |

### Hurt and Death

`IsAnimationFinished` is true once the state's clip has played to the end, so a reaction or an action ends with its clip and changing the clip changes the timing. It is also true when there is nothing to wait for (no `CharacterAnimator` or Animator, Animator disabled, empty or unknown Animation State), so a misconfigured character never gets stuck.

Hurt is started from code (a test key now, damage later) through a flag that `CanEnter()` checks:

```csharp
using Yenotsoftware.KSE2DPlatformerFramework;

public class PlayerHurtState : PlayerState
{
	private bool _requested;
	private bool _active;

	// Reactions override movement and abilities
	public override int Priority => 30;
	// Also cuts abilities that cannot exit yet (dash, attack)
	public override bool CanInterrupt => true;

	// Ignored while already hurt: one hit = one Hurt
	public void Trigger()
	{
		if (!_active)
			_requested = true;
	}

	public override bool CanEnter() => _requested;
	public override bool CanExit() => IsAnimationFinished;

	public override void Enter()
	{
		_requested = false;
		_active = true;
	}

	public override void Exit() => _active = false;

	public override void Tick(float deltaTime) => Motor.SetVelocityX(0f);
}
```

`Trigger()` checks `_active` instead of clearing the flag in `Exit()` on purpose: a flag raised during Hurt would keep `CanEnter()` true after the clip, the machine would select the same state again, and it would never exit.

Death is the same with `Priority => 40`, `CanInterrupt => true` and `CanExit() => false`: it starts at once even during Hurt or a dash, and nothing interrupts it. Turn off **Loop Time** on its clip so it holds the last frame.

A state cannot be shared between `PlayerController` and `EnemyController`, so the enemy gets its own copies inheriting `EnemyState`.

### Animation Events

1. In the clip add an event at the moment the action happens, pick the function **Raise** and put the event name in the **String** field.
2. Override `OnAnimationEvent` in the state that plays the clip. It receives only events from its own animation, so a clip still playing for another state cannot repeat its action.

For example a cast with the event `Cast` 0.3 s into a 0.7 s clip:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using Yenotsoftware.KSE2DPlatformerFramework;

public class CastState : PlayerState
{
	[SerializeField] private InputActionReference _castAction;

	// An ability: above movement, below reactions
	public override int Priority => 20;

	public override bool CanEnter() => Sensor.IsGrounded && InputReader.WasPressed(_castAction.action);
	public override bool CanExit() => IsAnimationFinished;

	public override void Enter()
	{
		InputReader.ConsumePress(_castAction.action);
		Motor.SetVelocityX(0f);
	}

	public override void OnAnimationEvent(string eventName)
	{
		if (eventName == "Cast")
			Debug.Log("Cast");
	}
}
```

`OnAnimationEvent` runs inside the Animator update. Do not switch states there: set a flag that `CanEnter()` checks, and the switch happens on the next physics tick.

Every event, from any clip, also goes to `CharacterAnimator.AnimationEventRaised`, e.g. for dust or sounds:

```csharp
GetComponent<CharacterAnimator>().AnimationEventRaised += eventName =>
{
	if (eventName == "Footstep")
		_dust.Play();
};
```

### Animation limitations

- While the active state's `CanExit()` is false (a dash, a cast), only higher-priority states with `CanInterrupt => true` can start. Others (e.g. a Hurt without it) begin on the first tick after it ends; the trigger is not lost.
- An impulse state active for one tick (`JumpState`) cannot hold its own animation: the next state replaces it. Show the rise through the `Air` blend tree by `VerticalSpeed` instead (see the table above).
- Blink with `SpriteRenderer.enabled`, not by deactivating the visual: while the Animator is inactive `IsAnimationFinished` is true, so Hurt would end at once.
- Keep the Animator's **Culling Mode** at Always Animate (the default). With Cull Completely an off-screen Animator stops, and a state waiting for its animation stays in it; a warning is logged.

## Known limitations

- No slopes: ground is expected to be flat.
- Moving platforms that carry the character upward are not supported yet.
