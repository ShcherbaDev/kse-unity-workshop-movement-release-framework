using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Base class for states that live as components on a character.
	/// Adding the component adds the state; its fields are tuned in the Inspector
	/// </summary>
	public abstract class ComponentState : MonoBehaviour, IState
	{
		/// <summary>
		/// When several states can enter, the highest priority wins.
		/// Override it in code, e.g. <c>public override int Priority => 10</c>
		/// </summary>
		public virtual int Priority => 0;

		public abstract bool CanEnter();
		public virtual bool CanExit() => true;

		public virtual void Enter()
		{ }

		public virtual void Exit()
		{ }

		public virtual void Tick(float deltaTime)
		{ }
	}
}
