namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// A state that a <see cref="StateMachine{TState}"/> can select.
	/// States decide on their own when they want to be active, so adding a new state
	/// never requires editing a transition table.
	/// </summary>
	public interface IState
	{
		/// <summary>
		/// When several states can enter, the one with the highest priority wins
		/// </summary>
		public int Priority { get; }

		/// <summary>
		/// True when the conditions for this state to be active hold
		/// (checked every tick, also while active)
		/// </summary>
		public bool CanEnter();

		/// <summary>
		/// False blocks switching to any other state
		/// (for example, a dash that is still in progress)
		/// </summary>
		public bool CanExit();

		/// <summary>
		/// Called once when the state becomes active
		/// </summary>
		public void Enter();

		/// <summary>
		/// Called once when another state replaces this one
		/// </summary>
		public void Exit();

		/// <summary>
		/// Called every physics tick while the state is active
		/// </summary>
		public void Tick(float deltaTime);
	}
}
