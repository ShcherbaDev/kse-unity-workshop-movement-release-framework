using UnityEngine;
using UnityEngine.InputSystem;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Rising and falling:
	/// air control, faster fall, variable jump height
	/// </summary>
	public class AirState : PlayerState
	{
		[SerializeField, Min(0f), Tooltip("Top horizontal speed in the air, units per second")]
		private float _maxSpeed = 8f;

		[SerializeField, Min(0f), Tooltip("Units per second squared when speeding up in the air")]
		private float _acceleration = 50f;

		[SerializeField, Min(0f), Tooltip("Units per second squared when stopping or turning in the air")]
		private float _deceleration = 30f;

		[SerializeField, Min(1f), Tooltip("Gravity multiplier while falling. Above 1 makes jumps feel less floaty")]
		private float _fallGravityMultiplier = 1.8f;

		[SerializeField, Min(1f), Tooltip("Gravity multiplier while rising with the jump button released. Short tap = low jump")]
		private float _jumpReleaseGravityMultiplier = 3f;

		[SerializeField, Min(0f), Tooltip("Terminal fall speed, units per second")]
		private float _maxFallSpeed = 18f;

		[SerializeField, Tooltip("Held to keep the full jump height, e.g. Player/Jump")]
		private InputActionReference _jumpAction;

		public override bool CanEnter()
		{
			return !Sensor.IsGrounded;
		}

		public override void Tick(float deltaTime)
		{
			Motor.MoveHorizontal(InputReader.Move.x * _maxSpeed, _acceleration, _deceleration, deltaTime);

			if (Motor.Velocity.y < 0f)
				Motor.GravityMultiplier = _fallGravityMultiplier;
			else if (!_jumpAction.action.IsPressed())
				Motor.GravityMultiplier = _jumpReleaseGravityMultiplier;
			else
				Motor.GravityMultiplier = 1f;

			Motor.ClampFallSpeed(_maxFallSpeed);
		}

		public override void Exit()
		{
			Motor.GravityMultiplier = 1f;
		}
	}
}
