using UnityEngine;
using UnityEngine.InputSystem;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Jump from the ground, with jump buffering and coyote time,
	/// and optional extra jumps in the air (double jump).
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

		[SerializeField, Min(0), Tooltip("Extra jumps in the air before landing. 1 = double jump")]
		private int _airJumps;

		[SerializeField, Min(0f), Tooltip("Peak height of an air jump above the point where it starts, in units")]
		private float _airJumpHeight = 2.5f;

		[SerializeField, Tooltip("Animator state played once over the air animation on an air jump. Empty keeps the current animation")]
		private string _airJumpAnimationState;

		private float _lastJumpTime = float.NegativeInfinity;
		private int _airJumpsUsed;

		public override int Priority => 10;

		/// <summary>
		/// Air jumps made since the character last stood on the ground
		/// </summary>
		public int AirJumpsUsed => GroundedSinceLastJump ? 0 : _airJumpsUsed;

		private bool GroundedSinceLastJump => Sensor.LastGroundedTime > _lastJumpTime;

		// Grounded since the last jump,
		// so coyote time cannot give a second ground jump in the air
		private bool CanGroundJump => GroundedSinceLastJump && Time.time - Sensor.LastGroundedTime <= _coyoteTime;

		public override bool CanEnter()
		{
			return (CanGroundJump || AirJumpsUsed < _airJumps) && InputReader.WasPressed(_jumpAction.action, _jumpBufferTime);
		}

		public override void Enter()
		{
			InputReader.ConsumePress(_jumpAction.action);
			if (CanGroundJump)
			{
				_airJumpsUsed = 0;
				Motor.Jump(_jumpHeight);
			}
			else
			{
				_airJumpsUsed = AirJumpsUsed + 1;
				Motor.Jump(_airJumpHeight);
				PlayOneShotAnimation(_airJumpAnimationState);
			}

			_lastJumpTime = Time.time;
		}
	}
}
