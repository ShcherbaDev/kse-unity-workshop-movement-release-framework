using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Mirrors a visual child to match <see cref="PlatformerMotor2D.FacingDirection"/>.
	/// Flips by scaling X, so child sprites (eyes, weapons) flip with it
	/// </summary>
	[RequireComponent(typeof(PlatformerMotor2D))]
	[DisallowMultipleComponent]
	public class FacingFlipper : MonoBehaviour
	{
		[SerializeField, Tooltip("Child that holds the sprites. Do not use the root: its collider must not flip")]
		private Transform _visual;

		private PlatformerMotor2D _motor;

		private void Awake()
		{
			_motor = GetComponent<PlatformerMotor2D>();
		}

		private void LateUpdate()
		{
			Vector3 scale = _visual.localScale;
			scale.x = Mathf.Abs(scale.x) * _motor.FacingDirection;
			_visual.localScale = scale;
		}
	}
}
