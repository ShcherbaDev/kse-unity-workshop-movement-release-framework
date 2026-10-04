using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Finds a target (usually the player) around an enemy by layer,
	/// so enemy prefabs never reference scene objects directly
	/// </summary>
	[DisallowMultipleComponent]
	public class TargetSensor2D : MonoBehaviour
	{
		[SerializeField, Tooltip("Layers of possible targets, e.g. Player")]
		private LayerMask _targetLayers;

		[SerializeField, Min(0f), Tooltip("Targets closer than this are detected, in units")]
		private float _detectionRadius = 6f;

		/// <summary>
		/// A target within the detection radius, or null.
		/// With several targets in range, any one of them may be returned
		/// </summary>
		public Transform FindTarget()
		{
			Collider2D hit = Physics2D.OverlapCircle(transform.position, _detectionRadius, _targetLayers);
			if (!hit)
				return null;

			return hit.attachedRigidbody ? hit.attachedRigidbody.transform : hit.transform;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(transform.position, _detectionRadius);
		}
	}
}
