using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Runs the <see cref="EnemyState"/> components on an enemy.
	/// Adding it also adds everything the states need.
	///
	/// An enemy type is a prefab with this component and a set of states,
	/// e.g. a jumping enemy = EnemyController + PatrolState + JumpToPlayerState
	/// </summary>
	[RequireComponent(typeof(PlatformerMotor2D), typeof(EnvironmentSensor2D))]
	[DisallowMultipleComponent]
	public class EnemyController : StateController<EnemyState>
	{ }
}
