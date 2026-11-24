using System.Collections.Generic;
using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// The area of an attack.
	/// <see cref="Hit"/> damages every <see cref="Health"/> on the target layers inside the box.
	///
	/// With <b>Animation Event</b> set it hits by itself
	/// when the character's <see cref="CharacterAnimator"/> raises that event,
	/// so an attack state needs no damage code.
	///
	/// The box sits at an offset from this object and is mirrored by the character's facing
	/// </summary>
	public class Hitbox2D : MonoBehaviour
	{
		[SerializeField, Min(0), Tooltip("Health taken from every target in the box")]
		private int _damage = 1;

		[SerializeField, Tooltip("Layers of the targets, e.g. Enemy on the player and Player on an enemy")]
		private LayerMask _targetLayers;

		[SerializeField, Tooltip("Centre of the box relative to this object when facing right, in units")]
		private Vector2 _offset = new Vector2(0.8f, 0f);

		[SerializeField, Tooltip("Width and height of the box, in units")]
		private Vector2 _size = Vector2.one;

		[SerializeField, Tooltip("Animation Event that deals the damage, named by the String of its Raise function, e.g. Hit. Leave empty to call Hit() from code")]
		private string _animationEvent;

		private readonly HashSet<Health> _damaged = new HashSet<Health>();
		private PlatformerMotor2D _motor;
		private CharacterAnimator _characterAnimator;
		private Health _ownHealth;

		private Vector2 Center
		{
			get
			{
				int facing = _motor ? _motor.FacingDirection : 1;
				return (Vector2)transform.position + new Vector2(_offset.x * facing, _offset.y);
			}
		}

		private void Awake()
		{
			_motor = GetComponentInParent<PlatformerMotor2D>();
			_characterAnimator = GetComponentInParent<CharacterAnimator>();
			_ownHealth = GetComponentInParent<Health>();
		}

		private void OnEnable()
		{
			if (_characterAnimator)
				_characterAnimator.AnimationEventRaised += OnAnimationEvent;
		}

		private void OnDisable()
		{
			if (_characterAnimator)
				_characterAnimator.AnimationEventRaised -= OnAnimationEvent;
		}

		/// <summary>
		/// Damages every target in the box once, never the character itself.
		/// Returns how many targets took the damage
		/// </summary>
		public int Hit()
		{
			// A dead character deals no damage, even if its attack clip still raises the event
			if (_ownHealth && _ownHealth.IsDead)
				return 0;

			int hits = 0;
			_damaged.Clear();
			// ponytail: allocates an array per hit, fine for attack events. Use the List overload if hits become frequent
			foreach (Collider2D target in Physics2D.OverlapBoxAll(Center, _size, 0f, _targetLayers))
			{
				Health health = target.GetComponentInParent<Health>();
				// A target with several colliders is hit once
				if (health && health != _ownHealth && _damaged.Add(health) && health.TakeDamage(_damage))
					hits++;
			}

			return hits;
		}

		private void OnAnimationEvent(string eventName)
		{
			if (!string.IsNullOrEmpty(_animationEvent) && eventName == _animationEvent)
				Hit();
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(Center, _size);
		}
	}
}
