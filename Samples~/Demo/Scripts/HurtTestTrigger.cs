using UnityEngine;
using UnityEngine.InputSystem;

namespace KSE2DPlatformerDemo
{
	/// <summary>
	/// Test key for Hurt
	/// while there is no damage yet
	/// </summary>
	[RequireComponent(typeof(PlayerHurtState))]
	public class HurtTestTrigger : MonoBehaviour
	{
		[SerializeField, Tooltip("Button that triggers Hurt, e.g. Player/Hurt")]
		private InputActionReference _hurtAction;

		private PlayerHurtState _hurt;

		private void Awake()
		{
			_hurt = GetComponent<PlayerHurtState>();
		}

		private void OnEnable()
		{
			_hurtAction.action.Enable();
		}

		private void Update()
		{
			if (_hurtAction.action.WasPressedThisFrame())
				_hurt.Trigger();
		}
	}
}
