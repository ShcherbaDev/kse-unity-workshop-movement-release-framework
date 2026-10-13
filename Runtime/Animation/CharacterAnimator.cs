using System.Collections.Generic;
using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Shows the active state on an Animator.
	/// On every state switch it plays the state's <see cref="ComponentState.AnimationState"/> instantly
	/// (sprite sheets cannot be blended).
	///
	/// The Animator graph needs no transitions:
	/// the state machine decides, the Animator only displays
	/// </summary>
	[RequireComponent(typeof(PlatformerMotor2D))]
	[DisallowMultipleComponent]
	public class CharacterAnimator : MonoBehaviour
	{
		[SerializeField, Tooltip("Animator on the visual child")]
		private Animator _animator;

		private readonly HashSet<int> _warnedStates = new HashSet<int>();
		private ComponentState _state;
		// Last state hash actually played, 0 when the animation must be applied again
		private int _playedHash;
		private bool _animatorWasActive;

		private void Reset()
		{
			_animator = GetComponentInChildren<Animator>();
			// Present at edit time, so the Animation window lists Raise for clip events
			AddRelay();
		}

		private void Awake()
		{
			if (!_animator)
			{
				Debug.LogWarning($"{nameof(CharacterAnimator)} on '{name}' has no Animator assigned", this);
				return;
			}

			if (!_animator.runtimeAnimatorController)
				Debug.LogWarning($"Animator on '{_animator.name}' has no Animator Controller", _animator);
			if (_animator.cullingMode == AnimatorCullingMode.CullCompletely)
				Debug.LogWarning($"Animator on '{_animator.name}' uses Cull Completely: off screen it stops, and a state waiting for its animation stays in it. Use Always Animate", _animator);
			AddRelay();
		}

		private void OnDisable()
		{
			// While the root is disabled Update does not run and would miss the Animator coming back
			_animatorWasActive = false;
		}

		private void Update()
		{
			if (!_animator)
				return;

			bool active = _animator.isActiveAndEnabled;
			// Unity resets an Animator to its entry state when its GameObject is disabled
			if (active && !_animatorWasActive)
				_playedHash = 0;
			_animatorWasActive = active;
			if (!active)
				return;

			if (_playedHash == 0 && _state)
				ApplyState(_state);
		}

		/// <summary>
		/// Called by <see cref="StateController{TState}"/> when <paramref name="state"/> becomes active
		/// </summary>
		internal void ApplyState(ComponentState state)
		{
			ComponentState previous = _state;
			_state = state;

			string stateName = state.AnimationState;
			if (string.IsNullOrEmpty(stateName))
				return;

			int hash = Animator.StringToHash(stateName);
			// Decided from the gameplay states, not from the Animator's state info, which can be stale
			if (previous && previous.AnimationState == stateName && hash == _playedHash)
				return;

			// Checked before HasState, which can be false on an inactive Animator
			if (!_animator || !_animator.isActiveAndEnabled)
			{
				_playedHash = 0;
				return;
			}

			if (!_animator.HasState(0, hash))
			{
				if (_warnedStates.Add(hash))
					Debug.LogWarning($"Animator on '{_animator.name}' has no state '{stateName}' at the root of the Base Layer, needed by {state.GetType().Name}", _animator);
				return;
			}

			_animator.Play(hash, 0, 0f);
			// Applies the switch now, so the Animator's state info is never stale,
			// whatever its Update Mode or the caller's update phase
			_animator.Update(0f);
			_playedHash = hash;
			_animatorWasActive = true;
		}

		internal bool IsAnimationFinished(ComponentState state)
		{
			if (!_animator || !_animator.isActiveAndEnabled || string.IsNullOrEmpty(state.AnimationState))
				return true;

			int hash = Animator.StringToHash(state.AnimationState);
			// Never played (unknown name, Animator was inactive): nothing to wait for
			if (hash != _playedHash)
				return true;

			AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
			return info.shortNameHash != hash || info.normalizedTime >= 1f;
		}

		/// <summary>
		/// Called by <see cref="AnimationEventRelay"/>
		/// </summary>
		internal void HandleAnimationEvent(AnimationEvent animationEvent)
		{ }

		private void AddRelay()
		{
			if (_animator && !_animator.GetComponent<AnimationEventRelay>())
				_animator.gameObject.AddComponent<AnimationEventRelay>();
		}
	}
}
