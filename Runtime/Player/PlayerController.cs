using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Runs the <see cref="PlayerState"/> components on the player.
	/// Adding it also adds everything the states need
	/// </summary>
	[RequireComponent(typeof(PlatformerMotor2D), typeof(EnvironmentSensor2D), typeof(PlayerInputReader))]
	[DisallowMultipleComponent]
	public class PlayerController : StateController<PlayerState>
	{ }
}
