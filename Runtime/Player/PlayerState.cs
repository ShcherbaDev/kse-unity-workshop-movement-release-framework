using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Base class for player mechanics.
	///
	/// Inherit from it, override <see cref="ComponentState.CanEnter"/>
	/// and add the component to the player
	/// </summary>
	[RequireComponent(typeof(PlayerController))]
	public abstract class PlayerState : ComponentState
	{
		private PlatformerMotor2D _motor;
		private EnvironmentSensor2D _sensor;
		private PlayerInputReader _inputReader;

		// Looked up on first use, so subclasses can define
		// their own Awake without calling base
		protected PlatformerMotor2D Motor => _motor ? _motor : _motor = GetComponent<PlatformerMotor2D>();
		protected EnvironmentSensor2D Sensor => _sensor ? _sensor : _sensor = GetComponent<EnvironmentSensor2D>();
		protected PlayerInputReader InputReader => _inputReader ? _inputReader : _inputReader = GetComponent<PlayerInputReader>();
	}
}
