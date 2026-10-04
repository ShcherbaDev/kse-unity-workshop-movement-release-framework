using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Running on the ground
	/// </summary>
	public class GroundState : PlayerState
	{
		[SerializeField, Min(0f), Tooltip("Top running speed, units per second")]
		private float _maxSpeed = 8f;

		[SerializeField, Min(0f), Tooltip("Units per second squared when speeding up")]
		private float _acceleration = 80f;

		[SerializeField, Min(0f), Tooltip("Units per second squared when stopping or turning")]
		private float _deceleration = 80f;

		public override bool CanEnter()
		{
			return Sensor.IsGrounded;
		}

		public override void Tick(float deltaTime)
		{
			Motor.MoveHorizontal(InputReader.Move.x * _maxSpeed, _acceleration, _deceleration, deltaTime);
		}
	}
}
