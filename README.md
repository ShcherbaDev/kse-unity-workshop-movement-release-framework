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
| `CharacterAnimator` | Plays the active state's animation on the Animator, writes `Speed`/`VerticalSpeed`/`Grounded`, routes Animation Events, plays one-shots with `PlayOneShot(stateName)`. See [Animation](#animation) |
| `AnimationEventRelay` | Sits next to the Animator and passes its Animation Events to `CharacterAnimator`. Added automatically |
| `Health` | Health with `TakeDamage(amount)`, invulnerability after a hit and the events On Damaged, On Died, On Changed. See [Combat](#combat) |
| `Hitbox2D` | Attack area. Damages every `Health` on the target layers inside its box, by itself on an Animation Event or through `Hit()` |
| `ContactDamage2D` | Damages every `Health` on the target layers that touches this object's colliders |
| `HealthBar` | Shows a `Health` by scaling a sprite |

Included states: `GroundState` (run), `AirState` (air control, fall gravity, variable jump height), `JumpState` (jump buffer, coyote time, optional [air jumps](#double-jump)), `PatrolState` (enemy walks and turns at walls and ledges).

## Setting up a character

1. Add a `Collider2D` (e.g. `BoxCollider2D`) with a **frictionless Physics Material 2D** (friction 0), otherwise characters stick to walls. The demo includes one.
2. Add `PlayerController` or `EnemyController`. It adds `Rigidbody2D`, `PlatformerMotor2D` and `EnvironmentSensor2D` (and `PlayerInputReader` for the player).
3. On `EnvironmentSensor2D`, set **Ground Layers** to your level layer only (e.g. Ground). Never include Player or Enemy layers, or characters will stand on each other's checks as ground. To make other characters block like walls (enemies turn at each other, a charge stops at the enemy in its way), put their layers into **Obstacle Layers** instead (e.g. Enemy on enemy prefabs): those count as walls only.
4. Add states. Player: `GroundState`, `AirState`, `JumpState`. Enemy: `PatrolState`.
5. Player input: drag actions from an `.inputactions` asset into the fields: `Move Action` on `PlayerInputReader`, `Jump Action` on `AirState` and `JumpState`.
6. Optional: put sprites on a child object and add `FacingFlipper` pointing to it.
7. Optional: animations, see [Animation](#animation).
8. Optional: health and damage, see [Combat](#combat).

Rigidbody2D **Gravity Scale** shapes the jump arc together with the states' settings; the demo uses 3.

### Input actions

Deadzones and keyboard behaviour live in the `.inputactions` asset, not in code. For WASD / arrows set the 2D Vector composite **Mode = Digital**; with the default "Digital Normalized", pressing two keys (e.g. W + D) runs at 71% speed.

### Double jump

On `JumpState` set **Air Jumps** to the number of extra jumps in the air (1 is a double jump) and **Air Jump Height**. A jump within the coyote time still counts as the ground jump, and landing gives the air jumps back. `JumpState.AirJumpsUsed` tells your effects which jump it was.

To give the air jump a clip of its own, add an Animator state for it and put its name into **Air Jump Animation State**. It plays once over the air animation, see [One-shot animations](#one-shot-animations).

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

Hurt is started from outside (a test key, or the On Damaged event of `Health`, see [Combat](#combat)) through a flag that `CanEnter()` checks:

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

### One-shot animations

A one-shot is an Animator state played once over the active state's animation, for a moment that is not a gameplay state of its own, such as the flip of an air jump or a landing puff.

- From a state call `PlayOneShotAnimation("Flip")`. `JumpState` does it for **Air Jump Animation State**.
- From anywhere else call `CharacterAnimator.PlayOneShot("Flip")`. It also fits a UnityEvent.

The one-shot ends with its clip, and then the active state's animation starts again. A state with another animation cuts it at once (landing, a dash, Hurt). One exception makes the air jump work. A one-shot from a state without its own Animation State (`JumpState`) stays over the state that follows (`AirState`).

### Animation limitations

- While the active state's `CanExit()` is false (a dash, a cast), only higher-priority states with `CanInterrupt => true` can start. Others (e.g. a Hurt without it) begin on the first tick after it ends; the trigger is not lost.
- An impulse state active for one tick (`JumpState`) cannot hold its own animation: the next state replaces it. Show the rise through the `Air` blend tree by `VerticalSpeed` instead (see the table above), or play a [one-shot](#one-shot-animations).
- Blink with `SpriteRenderer.enabled`, not by deactivating the visual: while the Animator is inactive `IsAnimationFinished` is true, so Hurt would end at once.
- Keep the Animator's **Culling Mode** at Always Animate (the default). With Cull Completely an off-screen Animator stops, and a state waiting for its animation stays in it; a warning is logged.

## Combat

Combat is four components and no code. `Health` holds the health, `Hitbox2D` and `ContactDamage2D` take it away, and `HealthBar` shows it. The reactions stay your own states, and `Health` starts them through events that you connect in the Inspector.

```
attack clip -> Animation Event "Hit" -> Hitbox2D -> Health.TakeDamage -> On Damaged -> your Hurt state
                    touching a ContactDamage2D ->                     -> On Died    -> your Death state
                                                                      -> On Changed -> HealthBar
```

### Combat setup

1. **Health.** Add `Health` to the root of every character that can be hurt. Set **Max Health** and **Invulnerability Time**, the seconds after a hit during which further hits are ignored.
2. **Reactions.** In **On Damaged** press +, drag the character itself into the object field and pick the `Trigger` method of its Hurt state, e.g. `PlayerHurtState > Trigger ()`. Do the same in **On Died** with the Death state. These are the states from [Hurt and Death](#hurt-and-death). A deadly hit raises On Died only, so Hurt and Death never start together.
3. **Attack.** Add `Hitbox2D` to the attacker's root. Set **Target Layers**, place the red box with **Offset** and **Size** (select the character to see it) and put the name of the attack clip's Animation Event into **Animation Event**. The box mirrors with the character's facing. The attack state keeps its own `OnAnimationEvent`, so a `Debug.Log` there still works.
4. **Touch.** For an enemy that hurts by touch, or for spikes, add `ContactDamage2D` next to the collider and set **Target Layers**. The collider can be solid or a trigger. The damage repeats while the touch lasts, once per Invulnerability Time of the target.
5. **Indicator.** Add an empty child `Health Bar` above the character and put `HealthBar` on it. Under it add an empty `Fill` at the left end of the bar, and under `Fill` a sprite shifted right by half its width. Drag `Fill` into the **Fill** field. Keep the bar outside the child that `FacingFlipper` flips, or it mirrors with the character.

### Combat layers

Targets are found by the layer of their colliders, and their `Health` is looked up in the collider's parents.

| Target Layers of | On the player | On an enemy | On spikes |
|---|---|---|---|
| `Hitbox2D` | Enemy | Player | |
| `ContactDamage2D` | | Player | Player |

A solid `ContactDamage2D` collider needs its layer to collide with the target's layer in Project Settings > Physics 2D > Layer Collision Matrix. If characters pass through each other in your game, give the enemy a trigger collider for the touch.

### Combat from code

- `Health.TakeDamage(amount)` returns false for an ignored hit. `Current`, `Max`, `Normalized` and `IsDead` describe the health.
- `Health.Damaged`, `Died` and `Changed` are UnityEvents, so `health.Died.AddListener(...)` works too. `Changed` passes the remaining part from 0 to 1, which also fits `Slider.value` if you prefer a UI bar.
- `Hitbox2D.Hit()` hits at once and returns the number of damaged targets. Leave Animation Event empty and call it from your state for an attack without a clip event.

### Combat limitations

- Unity stops sending touches for a Rigidbody2D that fell asleep, so a character standing still on spikes stops taking damage. Set **Sleeping Mode** to Never Sleep on the player's Rigidbody2D.
- `Hitbox2D` hits at one moment. An attack that damages during a whole swing needs several Animation Events.
- `Hitbox2D` reacts to its Animation Event from any clip of the character, so give every attack its own event name.
- There is no healing and no respawn with full health yet. Reload the scene on death.

## Known limitations

- No slopes: ground is expected to be flat.
- Moving platforms that carry the character upward are not supported yet.
