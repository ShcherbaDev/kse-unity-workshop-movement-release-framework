using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Damages every <see cref="Health"/> on the target layers that touches this object's colliders,
	/// solid or trigger. For enemies that hurt by touch and for hazards such as spikes.
	///
	/// The damage repeats while the touch lasts,
	/// as often as the target's invulnerability time allows
	/// </summary>
	public class ContactDamage2D : MonoBehaviour
	{
		[SerializeField, Min(0), Tooltip("Health taken from a target that touches this object")]
		private int _damage = 1;

		[SerializeField, Tooltip("Layers of the targets, e.g. Player")]
		private LayerMask _targetLayers;

		private Health _ownHealth;

		private void Awake()
		{
			_ownHealth = GetComponentInParent<Health>();
		}

		private void OnCollisionStay2D(Collision2D collision)
		{
			Touch(collision.collider);
		}

		private void OnTriggerStay2D(Collider2D other)
		{
			Touch(other);
		}

		private void Touch(Collider2D other)
		{
			if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0)
				return;
			// A dead enemy is harmless while its Death animation plays
			if (_ownHealth && _ownHealth.IsDead)
				return;

			// ponytail: a component lookup per touching collider per physics tick, cache per collider if many targets touch at once
			Health health = other.GetComponentInParent<Health>();
			if (health && health != _ownHealth)
				health.TakeDamage(_damage);
		}
	}
}
