using UnityEngine;
using Yenotsoftware.KSE2DPlatformerFramework;

namespace KSE2DPlatformerDemo
{
	/// <summary>
	/// Hurt reaction without health:
	/// <see cref="Trigger"/> starts it,
	/// the Hurt clip ends it
	/// </summary>
	public class PlayerHurtState : PlayerState
	{
		private bool _requested;
		private bool _active;

		// Reactions override movement and abilities
		public override int Priority => 30;
		// Also cuts abilities that cannot exit yet (dash, attack)
		public override bool CanInterrupt => true;

		/// <summary>
		/// Ignored while already hurt: one hit = one Hurt
		/// </summary>
		public void Trigger()
		{
			// Clearing the flag in Exit instead would not work: a flag raised during Hurt keeps
			// CanEnter true after the clip, the same state is selected again and never exits
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
}
