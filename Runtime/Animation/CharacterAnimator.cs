using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Shows the active state on an Animator.
	/// On every state switch it plays the state's <see cref="ComponentState.AnimationState"/> instantly
	/// (sprite sheets cannot be blended).
	/// Every frame it also writes Speed, VerticalSpeed and Grounded for blend trees (Idle/Run, Rise/Fall).
	/// Animation Events go to the active state (see <see cref="ComponentState.OnAnimationEvent"/>) and to <see cref="AnimationEventRaised"/>.
	///
	/// The Animator graph must have no transitions:
	/// the state machine decides, the Animator only displays.
	/// Whenever the Animator leaves the active state's animation (re-enable, pooling, Rebind, a new controller)
	/// it is put back.
	///
	/// <see cref="PlayOneShot(string)"/> plays one more animation over the active state's animation
	/// </summary>
	[RequireComponent(typeof(PlatformerMotor2D))]
	[DisallowMultipleComponent]
	public class CharacterAnimator : MonoBehaviour
	{
		[SerializeField, Tooltip("Animator on the visual child")]
		private Animator _animator;

		[SerializeField, Tooltip("Float parameter set to the absolute horizontal speed. Skipped if the controller has no such parameter")]
		private string _speedParameter = "Speed";

		[SerializeField, Tooltip("Float parameter set to the vertical speed, positive when rising. Skipped if missing")]
		private string _verticalSpeedParameter = "VerticalSpeed";

		[SerializeField, Tooltip("Bool parameter set to EnvironmentSensor2D.IsGrounded. Skipped if missing")]
		private string _groundedParameter = "Grounded";

		private readonly HashSet<int> _warnedStates = new HashSet<int>();
		private PlatformerMotor2D _motor;
		private EnvironmentSensor2D _sensor;
		// Controller the parameters and the played animation belong to
		private RuntimeAnimatorController _boundController;
		private bool _parametersFound;
		// 0 = the controller has no such parameter
		private int _speedHash;
		private int _verticalSpeedHash;
		private int _groundedHash;
		private ComponentState _state;
		// Last state hash actually played, 0 when the animation must be applied again
		private int _playedHash;
		// One-shot playing over the active state's animation, 0 when none
		private int _oneShotHash;
		// The one-shot came from a state without its own animation, so the next state's animation goes under it
		private bool _oneShotCarries;

		/// <summary>
		/// Raised for every Animation Event with its String parameter,
		/// whichever clip fired it. Use it for VFX or sound
		/// </summary>
		public event Action<string> AnimationEventRaised;

		private void Reset()
		{
			_animator = GetComponentInChildren<Animator>(true);
#if UNITY_EDITOR
			// Present at edit time, so the Animation window lists Raise for clip events.
			// Through Undo, so undoing the add of this component removes the relay too
			if (_animator && !_animator.GetComponent<AnimationEventRelay>())
				UnityEditor.Undo.AddComponent<AnimationEventRelay>(_animator.gameObject);
#endif
		}

		private void Awake()
		{
			_motor = GetComponent<PlatformerMotor2D>();
			_sensor = GetComponent<EnvironmentSensor2D>();
			if (!_animator)
			{
				Debug.LogWarning($"{nameof(CharacterAnimator)} on '{name}' has no Animator assigned", this);
				return;
			}

			if (!_animator.runtimeAnimatorController)
				Debug.LogWarning($"Animator on '{_animator.name}' has no Animator Controller", _animator);
			if (_animator.cullingMode == AnimatorCullingMode.CullCompletely)
				Debug.LogWarning($"Animator on '{_animator.name}' uses Cull Completely: off screen it stops, and a state waiting for its animation stays in it. Use Always Animate", _animator);
			_boundController = _animator.runtimeAnimatorController;
			AddRelay();
		}

		private void Update()
		{
			if (!_animator || !_animator.isActiveAndEnabled)
				return;

			// A new controller may have other parameters and other states
			if (_animator.runtimeAnimatorController != _boundController)
			{
				_boundController = _animator.runtimeAnimatorController;
				_parametersFound = false;
				_playedHash = 0;
			}

			if (_oneShotHash != 0 && !IsPlaying(_oneShotHash))
				StopOneShot();

			// The active state's animation comes back when the one-shot ends
			if (_oneShotHash == 0)
			{
				if (_playedHash == 0)
				{
					if (_state)
						ApplyState(_state);
				}
				// Unity puts the Animator back to its default state on re-enable or Rebind;
				// without transitions nothing else moves it away
				else if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash != _playedHash)
					Play(_playedHash);
			}

			WriteParameters();
		}

		/// <summary>
		/// Plays the Animator state <paramref name="stateName"/> once over the active state's animation
		/// (an air jump flip, a landing puff).
		/// It ends with its clip or when a state with another animation becomes active;
		/// after the clip the active state's animation starts again.
		/// Can be called from a UnityEvent
		/// </summary>
		public void PlayOneShot(string stateName)
		{
			PlayOneShot(stateName, false);
		}

		/// <summary>
		/// <paramref name="carryIntoNextState"/> is for states without their own animation (<see cref="JumpState"/>).
		/// The animation of the state that follows then goes under the one-shot instead of cutting it
		/// </summary>
		internal void PlayOneShot(string stateName, bool carryIntoNextState)
		{
			if (string.IsNullOrEmpty(stateName) || !_animator || !_animator.isActiveAndEnabled || !_animator.runtimeAnimatorController)
				return;

			int hash = Animator.StringToHash(stateName);
			if (!HasState(hash, stateName, "a one-shot"))
				return;

			_animator.Play(hash, 0, 0f);
			_animator.Update(0f);
			_oneShotHash = hash;
			_oneShotCarries = carryIntoNextState;
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

			// Checked before HasState, which is false on an inactive Animator or without a controller
			// (Awake already warns about a missing controller)
			if (!_animator || !_animator.isActiveAndEnabled || !_animator.runtimeAnimatorController)
			{
				_playedHash = 0;
				StopOneShot();
				return;
			}

			if (!HasState(hash, stateName, state.GetType().Name))
				return;

			// The one-shot stays over the same animation, and over the first one after a state without its own
			// (air jump, then AirState). Any other animation cuts it
			if (_oneShotHash != 0 && (_oneShotCarries || hash == _playedHash))
			{
				_oneShotCarries = false;
				_playedHash = hash;
				return;
			}

			Play(hash);
		}

		internal bool IsAnimationFinished(ComponentState state)
		{
			if (!_animator || !_animator.isActiveAndEnabled || string.IsNullOrEmpty(state.AnimationState))
				return true;

			int hash = Animator.StringToHash(state.AnimationState);
			// Never played (unknown name, Animator was inactive): nothing to wait for
			if (hash != _playedHash)
				return true;
			// Its own animation starts again after the one-shot playing over it
			if (_oneShotHash != 0)
				return false;

			AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
			return info.shortNameHash != hash || info.normalizedTime >= 1f;
		}

		/// <summary>
		/// Called by <see cref="AnimationEventRelay"/>
		/// </summary>
		internal void HandleAnimationEvent(AnimationEvent animationEvent)
		{
			string eventName = animationEvent.stringParameter;
			// Only events from the active state's own animation reach it,
			// so a clip that is still playing for another state cannot repeat its action
			if (_state && !string.IsNullOrEmpty(_state.AnimationState)
				&& animationEvent.animatorStateInfo.shortNameHash == Animator.StringToHash(_state.AnimationState))
				_state.OnAnimationEvent(eventName);

			AnimationEventRaised?.Invoke(eventName);
		}

		private void WriteParameters()
		{
			// Looked up once the Animator is active: before that its parameters may be unavailable
			if (!_parametersFound)
			{
				_speedHash = FindParameter(_speedParameter, AnimatorControllerParameterType.Float);
				_verticalSpeedHash = FindParameter(_verticalSpeedParameter, AnimatorControllerParameterType.Float);
				_groundedHash = _sensor ? FindParameter(_groundedParameter, AnimatorControllerParameterType.Bool) : 0;
				_parametersFound = true;
			}

			Vector2 velocity = _motor.Velocity;
			if (_speedHash != 0)
				_animator.SetFloat(_speedHash, Mathf.Abs(velocity.x));
			if (_verticalSpeedHash != 0)
				_animator.SetFloat(_verticalSpeedHash, velocity.y);
			if (_groundedHash != 0)
				_animator.SetBool(_groundedHash, _sensor.IsGrounded);
		}

		// Missing parameters are skipped silently, so a game uses only the ones it needs
		private int FindParameter(string parameterName, AnimatorControllerParameterType type)
		{
			if (string.IsNullOrEmpty(parameterName))
				return 0;

			foreach (AnimatorControllerParameter parameter in _animator.parameters)
			{
				if (parameter.name == parameterName && parameter.type == type)
					return parameter.nameHash;
			}

			return 0;
		}

		private void Play(int hash)
		{
			_animator.Play(hash, 0, 0f);
			// Applies the switch now, so the Animator's state info is never stale,
			// whatever its Update Mode or the caller's update phase
			_animator.Update(0f);
			_playedHash = hash;
			StopOneShot();
		}

		private void StopOneShot()
		{
			_oneShotHash = 0;
			_oneShotCarries = false;
		}

		private bool IsPlaying(int hash)
		{
			AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
			return info.shortNameHash == hash && info.normalizedTime < 1f;
		}

		private bool HasState(int hash, string stateName, string neededBy)
		{
			if (_animator.HasState(0, hash))
				return true;

			if (_warnedStates.Add(hash))
				Debug.LogWarning($"Animator on '{_animator.name}' has no state '{stateName}' at the root of the Base Layer, needed by {neededBy}", _animator);
			return false;
		}

		private void AddRelay()
		{
			if (_animator && !_animator.GetComponent<AnimationEventRelay>())
				_animator.gameObject.AddComponent<AnimationEventRelay>();
		}
	}
}
