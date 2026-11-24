using UnityEngine;
using UnityEngine.Events;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Health of a character or an object.
	///
	/// <see cref="Hitbox2D"/> and <see cref="ContactDamage2D"/> call <see cref="TakeDamage"/>.
	/// Reactions and indicators listen to the events, connected in the Inspector
	/// or from code with <c>AddListener</c>
	/// </summary>
	[DisallowMultipleComponent]
	public class Health : MonoBehaviour
	{
		[SerializeField, Min(1), Tooltip("Health at the start, also the maximum")]
		private int _maxHealth = 3;

		[SerializeField, Min(0f), Tooltip("Seconds after a hit during which further damage is ignored")]
		private float _invulnerabilityTime = 0.5f;

		[SerializeField, Tooltip("A hit was taken and survived. Connect Hurt here, e.g. the Trigger method of your Hurt state")]
		private UnityEvent _onDamaged = new UnityEvent();

		[SerializeField, Tooltip("Health reached zero. Connect Death here, e.g. the Trigger method of your Death state")]
		private UnityEvent _onDied = new UnityEvent();

		[SerializeField, Tooltip("Health changed. Passes the remaining part from 0 to 1, e.g. to Slider.value")]
		private UnityEvent<float> _onChanged = new UnityEvent<float>();

		private float _invulnerableUntil = float.NegativeInfinity;

		public int Max => _maxHealth;
		public int Current { get; private set; }

		/// <summary>
		/// Remaining part of the health, from 0 to 1
		/// </summary>
		public float Normalized => (float)Current / _maxHealth;

		public bool IsDead => Current <= 0;

		/// <summary>
		/// Raised for a hit that was survived
		/// </summary>
		public UnityEvent Damaged => _onDamaged;

		/// <summary>
		/// Raised once, by the hit that takes the health to zero
		/// </summary>
		public UnityEvent Died => _onDied;

		/// <summary>
		/// Raised for every hit with <see cref="Normalized"/>
		/// </summary>
		public UnityEvent<float> Changed => _onChanged;

		private void Awake()
		{
			Current = _maxHealth;
		}

		/// <summary>
		/// Takes <paramref name="amount"/> off the health.
		/// Returns false when the hit is ignored
		/// (already dead, still invulnerable after the last hit, nothing to take)
		/// </summary>
		public bool TakeDamage(int amount)
		{
			if (amount <= 0 || IsDead || Time.time < _invulnerableUntil)
				return false;

			Current = Mathf.Max(Current - amount, 0);
			_invulnerableUntil = Time.time + _invulnerabilityTime;
			_onChanged.Invoke(Normalized);
			// A deadly hit raises Died only, so a Hurt request cannot outlive the death
			if (IsDead)
				_onDied.Invoke();
			else
				_onDamaged.Invoke();
			return true;
		}
	}
}
