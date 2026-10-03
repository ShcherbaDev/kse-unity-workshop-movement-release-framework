using System;
using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Runs a <see cref="StateMachine{TState}"/>
	/// over all <typeparamref name="TState"/> components
	/// on this GameObject, once per physics tick.
	///
	/// Refreshes the <see cref="EnvironmentSensor2D"/> (if present) first,
	/// so states always read this tick's checks
	/// </summary>
	public abstract class StateController<TState> : MonoBehaviour where TState : ComponentState
	{
		private StateMachine<TState> _machine;
		private EnvironmentSensor2D _sensor;

		/// <summary>
		/// The active state, or null if none could enter yet
		/// </summary>
		public TState CurrentState => _machine?.Current;

		/// <summary>
		/// Raised after a switch with (previous, next).
		/// Use it for animation, VFX or debugging
		/// </summary>
		public event Action<TState, TState> StateChanged;

		protected virtual void Awake()
		{
			TState[] states = GetComponents<TState>();
			if (states.Length == 0)
				Debug.LogWarning($"{GetType().Name} on '{name}' has no {typeof(TState).Name} components", this);

			_sensor = GetComponent<EnvironmentSensor2D>();
			_machine = new StateMachine<TState>(states);
			_machine.StateChanged += (previous, next) => StateChanged?.Invoke(previous, next);
		}

		protected virtual void FixedUpdate()
		{
			if (_sensor)
				_sensor.Refresh();
			_machine.Tick(Time.fixedDeltaTime);
		}
	}
}
