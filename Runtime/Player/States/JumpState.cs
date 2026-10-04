using UnityEngine;
using UnityEngine.InputSystem;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Jump from the ground,
	/// with jump buffering and coyote time.
	///
	/// Active for one tick:
	/// applies the jump, then <see cref="AirState"/> takes over
	/// </summary>
	public class JumpState : PlayerState
	{
		[SerializeField, Min(0f), Tooltip("Peak height of a full (held) jump, in units")]
		private float _jumpHeight = 3f;

		[SerializeField, Min(0f), Tooltip("A press this many seconds before landing still jumps")]
		private float _jumpBufferTime = 0.1f;

		[SerializeField, Min(0f), Tooltip("Jumping still works this many seconds after walking off a ledge")]
		private float _coyoteTime = 0.1f;

		[SerializeField, Tooltip("Button that jumps, e.g. Player/Jump")]
		private InputActionReference _jumpAction;

		private float _lastJumpTime = float.NegativeInfinity;

		public override int Priority => 10;

		public override bool CanEnter()
		{
			// Grounded since the last jump,
			// so coyote time cannot give a second jump in the air
			bool groundedSinceLastJump = Sensor.LastGroundedTime > _lastJumpTime;
			bool withinCoyoteTime = Time.time - Sensor.LastGroundedTime <= _coyoteTime;
			return groundedSinceLastJump && withinCoyoteTime && InputReader.WasPressed(_jumpAction.action, _jumpBufferTime);
		}

		public override void Enter()
		{
			InputReader.ConsumePress(_jumpAction.action);
			Motor.Jump(_jumpHeight);
			_lastJumpTime = Time.time;
		}
	}
}
