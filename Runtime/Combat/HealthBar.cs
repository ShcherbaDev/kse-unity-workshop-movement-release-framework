using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Shows a <see cref="Health"/> as a bar.
	/// It scales <b>Fill</b> along X from its starting scale (full health) down to zero.
	///
	/// A sprite shrinks towards its pivot.
	/// For a bar that empties to one side make Fill an empty object at that edge of the bar
	/// and put the sprite under it
	/// </summary>
	public class HealthBar : MonoBehaviour
	{
		[SerializeField, Tooltip("Health to show. Empty looks for one in the parents")]
		private Health _health;

		[SerializeField, Tooltip("Object scaled along X by the remaining health")]
		private Transform _fill;

		private float _fullScaleX;

		private void Awake()
		{
			if (!_health)
				_health = GetComponentInParent<Health>();
			if (!_health || !_fill)
			{
				Debug.LogWarning($"{nameof(HealthBar)} on '{name}' needs a Health and a Fill", this);
				enabled = false;
				return;
			}

			_fullScaleX = _fill.localScale.x;
		}

		private void OnEnable()
		{
			_health.Changed.AddListener(Show);
		}

		// Start runs after every Awake, so the Health already has its starting value
		private void Start()
		{
			Show(_health.Normalized);
		}

		private void OnDisable()
		{
			if (_health)
				_health.Changed.RemoveListener(Show);
		}

		private void Show(float normalized)
		{
			Vector3 scale = _fill.localScale;
			scale.x = _fullScaleX * normalized;
			_fill.localScale = scale;
		}
	}
}
