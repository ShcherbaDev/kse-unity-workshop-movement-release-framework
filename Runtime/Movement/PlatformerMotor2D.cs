using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Movement API over a dynamic <see cref="Rigidbody2D"/>.
	///
	/// States call these methods
	/// instead of touching the Rigidbody2D directly,
	/// so the feel stays consistent
	/// </summary>
	[RequireComponent(typeof(Rigidbody2D))]
	[DisallowMultipleComponent]
	public class PlatformerMotor2D : MonoBehaviour
	{
		private float _baseGravityScale;

		public Rigidbody2D Body { get; private set; }

		public Vector2 Velocity
		{
			get => Body.linearVelocity;
			set => Body.linearVelocity = value;
		}

		/// <summary>
		/// 1 when facing right, -1 when facing left.
		/// Updated by <see cref="MoveHorizontal"/>
		/// </summary>
		public int FacingDirection { get; private set; } = 1;

		/// <summary>
		/// Gravity magnitude (positive)
		/// with the Rigidbody2D's gravity scale at startup
		/// </summary>
		public float Gravity => Mathf.Abs(Physics2D.gravity.y * _baseGravityScale);

		/// <summary>
		/// Multiplies the startup gravity scale.
		/// 1 is normal. Use more than 1 for faster falling
		/// </summary>
		public float GravityMultiplier
		{
			get => _baseGravityScale == 0f ? 1f : Body.gravityScale / _baseGravityScale;
			set => Body.gravityScale = _baseGravityScale * value;
		}

		protected virtual void Awake()
		{
			Body = GetComponent<Rigidbody2D>();
			_baseGravityScale = Body.gravityScale;
		}

		protected virtual void Reset()
		{
			Rigidbody2D body = GetComponent<Rigidbody2D>();
			body.freezeRotation = true;
			body.interpolation = RigidbodyInterpolation2D.Interpolate;
			body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		}

		/// <summary>
		/// Changes horizontal speed toward <paramref name="targetSpeed"/>.
		///
		/// Speeding up uses <paramref name="acceleration"/>.
		/// Slowing down, stopping and turning use <paramref name="deceleration"/>.
		/// Call it every physics tick with the tick's delta time
		/// </summary>
		public void MoveHorizontal(float targetSpeed, float acceleration, float deceleration, float deltaTime)
		{
			SetVelocityX(AccelerateTowards(Velocity.x, targetSpeed, acceleration, deceleration, deltaTime));
			if (targetSpeed != 0f)
				FacingDirection = targetSpeed > 0f ? 1 : -1;
		}

		/// <summary>
		/// Sets vertical speed
		/// so that the jump peaks at <paramref name="height"/> units
		/// under normal gravity
		/// </summary>
		public void Jump(float height)
		{
			GravityMultiplier = 1f;
			SetVelocityY(JumpVelocity(height, Gravity));
		}

		/// <summary>
		/// Limits downward speed to <paramref name="maxFallSpeed"/>
		/// </summary>
		public void ClampFallSpeed(float maxFallSpeed)
		{
			if (Velocity.y < -maxFallSpeed)
				SetVelocityY(-maxFallSpeed);
		}

		public void SetVelocityX(float x)
		{
			Velocity = new Vector2(x, Velocity.y);
		}

		public void SetVelocityY(float y)
		{
			Velocity = new Vector2(Velocity.x, y);
		}

		/// <summary>
		/// Turns to face <paramref name="direction"/>
		/// (any positive or negative number).
		///
		/// Zero is ignored
		/// </summary>
		public void Face(float direction)
		{
			if (direction == 0f)
				return;
			FacingDirection = direction > 0f ? 1 : -1;
		}

		/// <summary>
		/// One acceleration step from <paramref name="current"/>
		/// toward <paramref name="target"/>
		/// </summary>
		public static float AccelerateTowards(float current, float target, float acceleration, float deceleration, float deltaTime)
		{
			bool speedingUp = current * target >= 0f && Mathf.Abs(target) > Mathf.Abs(current);
			float rate = speedingUp ? acceleration : deceleration;
			return Mathf.MoveTowards(current, target, rate * deltaTime);
		}

		/// <summary>
		/// Initial upward speed that reaches <paramref name="height"/>
		/// under <paramref name="gravity"/>
		/// </summary>
		public static float JumpVelocity(float height, float gravity)
		{
			return Mathf.Sqrt(2f * gravity * height);
		}
	}
}
