using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Base class for states that live as components on a character.
	/// Adding the component adds the state; its fields are tuned in the Inspector
	/// </summary>
	public abstract class ComponentState : MonoBehaviour, IState
	{
		[SerializeField, Tooltip("Animator state played while this state is active: its short name at the root of the Base Layer. Empty keeps the current animation")]
		private string _animationState;

		private CharacterAnimator _characterAnimator;

		/// <summary>
		/// When several states can enter, the highest priority wins.
		/// Override it in code, e.g. <c>public override int Priority => 10</c>
		/// </summary>
		public virtual int Priority => 0;

		/// <summary>
		/// Animator state that <see cref="CharacterAnimator"/> plays while this state is active.
		/// Empty keeps the current animation
		/// </summary>
		public string AnimationState => _animationState;

		/// <summary>
		/// True once this state's animation has played to the end,
		/// and also when there is nothing to wait for
		/// (no <see cref="CharacterAnimator"/> or Animator,
		/// Animator disabled, empty or unknown animation state),
		/// so a state that waits for it can never get stuck.
		///
		/// Use it to end reactions and actions with their clip:
		/// <c>public override bool CanExit() => IsAnimationFinished;</c>
		/// </summary>
		protected bool IsAnimationFinished
		{
			get
			{
				if (!_characterAnimator)
					_characterAnimator = GetComponent<CharacterAnimator>();
				return !_characterAnimator || _characterAnimator.IsAnimationFinished(this);
			}
		}

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
