using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Reads player movement input and remembers button presses
	/// </summary>
	[DisallowMultipleComponent]
	public class PlayerInputReader : MonoBehaviour
	{
		/// <summary>
		/// Default buffer for <see cref="WasPressed"/>, in seconds
		/// </summary>
		public const float DefaultBufferTime = 0.1f;

		[SerializeField, Tooltip("Vector2 action used for movement, e.g. Player/Move")]
		private InputActionReference _moveAction;

		private readonly Dictionary<InputAction, float> _lastPressTimes = new Dictionary<InputAction, float>();

		/// <summary>
		/// Value of the move action
		/// </summary>
		public Vector2 Move => _moveAction.action.ReadValue<Vector2>();

		/// <summary>
		/// True if <paramref name="action"/> was pressed
		/// within the last <paramref name="bufferTime"/> seconds
		/// and the press was not consumed yet
		/// </summary>
		public bool WasPressed(InputAction action, float bufferTime = DefaultBufferTime)
		{
			if (!action.enabled)
				action.Enable();

			return _lastPressTimes.TryGetValue(action, out float pressTime)
				&& Time.time - pressTime <= bufferTime;
		}

		/// <summary>
		/// Forgets the last press of <paramref name="action"/>,
		/// so one press triggers it only once
		/// </summary>
		public void ConsumePress(InputAction action)
		{
			_lastPressTimes.Remove(action);
		}

		private void OnEnable()
		{
			_moveAction.action.Enable();
			InputSystem.onActionChange += OnActionChange;
		}

		private void OnDisable()
		{
			InputSystem.onActionChange -= OnActionChange;
			_lastPressTimes.Clear();
		}

		// Receives every action of every asset,
		// so any button can be buffered without registering it first
		private void OnActionChange(object target, InputActionChange change)
		{
			if (change == InputActionChange.ActionPerformed
				&& target is InputAction action
				&& action.type == InputActionType.Button)
				_lastPressTimes[action] = Time.time;
		}
	}
}
