using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Receives Animation Events on the Animator's GameObject (Unity sends them only there)
	/// and passes them to the <see cref="CharacterAnimator"/> on the character.
	///
	/// In a clip's event pick the function <c>Raise</c> and put the event name in the String field.
	/// <see cref="CharacterAnimator"/> adds this component next to its Animator
	/// </summary>
	[DisallowMultipleComponent]
	public class AnimationEventRelay : MonoBehaviour
	{
		private CharacterAnimator _target;

		public void Raise(AnimationEvent animationEvent)
		{
			if (!_target)
				_target = GetComponentInParent<CharacterAnimator>();

			if (_target)
				_target.HandleAnimationEvent(animationEvent);
		}
	}
}
