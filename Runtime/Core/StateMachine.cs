using System;
using System.Collections.Generic;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Priority-based state machine.
	///
	/// Every tick it activates the highest-priority state
	/// whose <see cref="IState.CanEnter"/> is true.
	/// While the current state blocks exiting, only higher-priority states
	/// with <see cref="IState.CanInterrupt"/> are considered.
	///
	/// Ties go to the state listed first.
	/// If no state can enter, the current state stays.
	///
	/// Selecting the current state again does not re-enter it
	/// </summary>
	public sealed class StateMachine<TState> where TState : class, IState
	{
		private readonly IReadOnlyList<TState> _states;

		/// <summary>
		/// The active state, or null before the first successful selection
		/// </summary>
		public TState Current { get; private set; }

		/// <summary>
		/// Raised after a switch with (previous, next).
		/// Previous is null on the first switch
		/// </summary>
		public event Action<TState, TState> StateChanged;

		public StateMachine(IReadOnlyList<TState> states)
		{
			_states = states;
			if (_states == null)
				throw new ArgumentNullException(nameof(states));
		}

		/// <summary>
		/// Selects the state for this tick, then ticks it
		/// </summary>
		public void Tick(float deltaTime)
		{
			TState next = Select();
			if (next != null && next != Current)
			{
				TState previous = Current;
				previous?.Exit();
				Current = next;
				next.Enter();
				StateChanged?.Invoke(previous, next);
			}

			Current?.Tick(deltaTime);
		}

		private TState Select()
		{
			bool locked = Current != null && !Current.CanExit();
			TState best = null;
			foreach (TState state in _states)
			{
				// Skipped before CanEnter, so without interrupters a locked state behaves as before
				if (locked && !(state.CanInterrupt && state.Priority > Current.Priority))
					continue;
				if ((best == null || state.Priority > best.Priority) && state.CanEnter())
					best = state;
			}

			return best;
		}
	}
}
