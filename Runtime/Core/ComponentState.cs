using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Base class for states that live as components on a character.
	/// Adding the component adds the state; its fields are tuned in the Inspector.
	/// </summary>
	public abstract class ComponentState : MonoBehaviour, IState
	{
		[SerializeField, Tooltip("When several states can enter, the highest priority wins.")]
		private int priority;

		public int Priority => priority;

		/// <summary>Priority assigned when the component is added in the editor.</summary>
		protected virtual int DefaultPriority => 0;

		public abstract bool CanEnter();
		public virtual bool CanExit() => true;

		public virtual void Enter()
		{ }

		public virtual void Exit()
		{ }

		public virtual void Tick(float deltaTime)
		{ }

		protected virtual void Reset()
		{
			priority = DefaultPriority;
		}
	}
}
