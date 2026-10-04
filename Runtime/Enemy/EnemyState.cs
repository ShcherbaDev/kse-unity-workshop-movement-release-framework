using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Base class for enemy states (the "enemy behaviours" from the spec: Patrol, Charge, ...).
	///
	/// Inherit from it, override <see cref="ComponentState.CanEnter"/>
	/// and add the component to the enemy.
	///
	/// States that follow a target should also add
	/// <c>[RequireComponent(typeof(TargetSensor2D))]</c>
	/// </summary>
	[RequireComponent(typeof(EnemyController))]
	public abstract class EnemyState : ComponentState
	{
		private PlatformerMotor2D _motor;
		private EnvironmentSensor2D _sensor;
		private TargetSensor2D _targetSensor;

		// Looked up on first use, so subclasses can define
		// their own Awake without calling base
		protected PlatformerMotor2D Motor => _motor ? _motor : _motor = GetComponent<PlatformerMotor2D>();
		protected EnvironmentSensor2D Sensor => _sensor ? _sensor : _sensor = GetComponent<EnvironmentSensor2D>();
		protected TargetSensor2D TargetSensor => _targetSensor ? _targetSensor : _targetSensor = GetComponent<TargetSensor2D>();
	}
}
