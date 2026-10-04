using System.Collections.Generic;
using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework
{
	/// <summary>
	/// Ground, wall, ceiling and ledge checks for a character.
	/// A <see cref="StateController{TState}"/> on the same GameObject calls <see cref="Refresh"/>
	/// every physics tick before its states run
	/// </summary>
	[RequireComponent(typeof(Rigidbody2D))]
	[DisallowMultipleComponent]
	public class EnvironmentSensor2D : MonoBehaviour
	{
		// Moving up faster than this
		// means "just jumped / passing through a platform",
		// not grounded
		private const float MaxGroundedUpwardSpeed = 0.1f;
		private const float LedgeRayStartHeight = 0.05f;

		[SerializeField, Tooltip("Layers that count as ground, walls and ceilings. Do not include characters")]
		private LayerMask _groundLayers = 1;

		[SerializeField, Min(0.001f)] private float _groundCheckDistance = 0.05f;
		[SerializeField, Min(0.001f)] private float _wallCheckDistance = 0.05f;
		[SerializeField, Min(0.001f)] private float _ceilingCheckDistance = 0.05f;

		[SerializeField, Min(0f), Tooltip("How far past the collider's edge the ledge check looks")]
		private float _ledgeCheckForwardOffset = 0.1f;

		[SerializeField, Min(0f), Tooltip("A drop deeper than this counts as a ledge")]
		private float _ledgeCheckDepth = 0.5f;

		private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>(4);
		private Rigidbody2D _body;
		private Collider2D _collider;

		public LayerMask GroundLayers => _groundLayers;
		public bool IsGrounded { get; private set; }

		/// <summary>
		/// Time.time of the last tick the character was grounded
		/// (useful for coyote time)
		/// </summary>
		public float LastGroundedTime { get; private set; } = float.NegativeInfinity;

		public bool IsTouchingWallLeft { get; private set; }
		public bool IsTouchingWallRight { get; private set; }
		public bool IsTouchingCeiling { get; private set; }

		/// <summary>
		/// -1 for a wall on the left,
		/// 1 on the right,
		/// 0 for none (right wins if both)
		/// </summary>
		public int WallDirection => IsTouchingWallRight ? 1 : IsTouchingWallLeft ? -1 : 0;

		private void Awake()
		{
			_body = GetComponent<Rigidbody2D>();
			_collider = GetComponent<Collider2D>();
			if (!_collider)
				Debug.LogWarning($"{nameof(EnvironmentSensor2D)} on '{name}' needs a Collider2D for ledge checks", this);
		}

		/// <summary>
		/// True when there is ground within the ledge depth
		/// just past the collider's edge in <paramref name="direction"/>
		/// (1 or -1)
		/// </summary>
		public bool HasGroundAhead(int direction)
		{
			Bounds bounds = _collider.bounds;
			float edgeX = direction > 0 ? bounds.max.x : bounds.min.x;
			Vector2 origin = new Vector2(
				edgeX + direction * _ledgeCheckForwardOffset,
				bounds.min.y + LedgeRayStartHeight
			);

			return Physics2D.Raycast(
				origin,
				Vector2.down,
				_ledgeCheckDepth + LedgeRayStartHeight,
				_groundLayers
			);
		}

		/// <summary>
		/// Re-runs the ground, wall and ceiling checks.
		/// Called by <see cref="StateController{TState}"/>; call it yourself only when using the sensor without one
		/// </summary>
		public void Refresh()
		{
			ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
			filter.SetLayerMask(_groundLayers);

			// Subtract the hit rigidbody's velocity
			// if moving platforms are added
			IsGrounded = CastForSurface(filter, Vector2.down, _groundCheckDistance)
				&& _body.linearVelocity.y <= MaxGroundedUpwardSpeed;
			if (IsGrounded)
				LastGroundedTime = Time.time;

			IsTouchingWallLeft = CastForSurface(filter, Vector2.left, _wallCheckDistance);
			IsTouchingWallRight = CastForSurface(filter, Vector2.right, _wallCheckDistance);
			IsTouchingCeiling = CastForSurface(filter, Vector2.up, _ceilingCheckDistance);
		}

		// Casts all the body's colliders (ignoring its own)
		private bool CastForSurface(ContactFilter2D filter, Vector2 direction, float distance)
		{
			return _body.Cast(direction, filter, _hits, distance) > 0;
		}

		private void OnDrawGizmosSelected()
		{
			Collider2D col = _collider != null ? _collider : GetComponent<Collider2D>();
			if (!col)
				return;

			Bounds bounds = col.bounds;
			Gizmos.color = IsGrounded ? Color.green : Color.red;
			Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.min.y - _groundCheckDistance / 2f), new Vector3(bounds.size.x, _groundCheckDistance));
			Gizmos.color = IsTouchingWallLeft ? Color.green : Color.red;
			Gizmos.DrawWireCube(new Vector3(bounds.min.x - _wallCheckDistance / 2f, bounds.center.y), new Vector3(_wallCheckDistance, bounds.size.y));
			Gizmos.color = IsTouchingWallRight ? Color.green : Color.red;
			Gizmos.DrawWireCube(new Vector3(bounds.max.x + _wallCheckDistance / 2f, bounds.center.y), new Vector3(_wallCheckDistance, bounds.size.y));

			Gizmos.color = Color.yellow;
			float top = bounds.min.y + LedgeRayStartHeight;
			float bottom = bounds.min.y - _ledgeCheckDepth;
			foreach (float x in new[] { bounds.max.x + _ledgeCheckForwardOffset, bounds.min.x - _ledgeCheckForwardOffset })
				Gizmos.DrawLine(new Vector3(x, top), new Vector3(x, bottom));
		}
	}
}
