using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Walks back and forth on a platform, turning at walls and ledges.
	/// Stands still if both directions are blocked, so it never jitters on a tiny platform.
	///
	/// Always able to enter, so it works as the default state other states interrupt
	/// </summary>
	public class PatrolState : EnemyState
	{
		[SerializeField, Min(0f), Tooltip("Walking speed, units per second")]
		private float _speed = 2f;

		[SerializeField, Min(0f), Tooltip("Units per second squared when starting to walk")]
		private float _acceleration = 20f;

		[SerializeField, Tooltip("Walk left first instead of right")]
		private bool _startMovingLeft;

		private int _direction;

		private void Awake()
		{
			_direction = _startMovingLeft ? -1 : 1;
		}

		public override bool CanEnter()
		{
			return true;
		}

		public override void Tick(float deltaTime)
		{
			if (IsBlocked(_direction))
			{
				_direction = -_direction;
				// Stop at once, so momentum cannot carry the enemy over the ledge
				Motor.SetVelocityX(0f);
			}

			if (IsBlocked(_direction))
			{
				Motor.SetVelocityX(0f);
				return;
			}

			Motor.MoveHorizontal(_direction * _speed, _acceleration, _acceleration, deltaTime);
		}

		private bool IsBlocked(int direction)
		{
			if (!Sensor.IsGrounded)
				return false;

			bool wallAhead = direction > 0 ? Sensor.IsTouchingWallRight : Sensor.IsTouchingWallLeft;
			return wallAhead || !Sensor.HasGroundAhead(direction);
		}
	}
}
