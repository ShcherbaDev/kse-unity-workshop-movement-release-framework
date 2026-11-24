#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	public class Hitbox2DTests
	{
		private const int TargetLayer = 9;

		private readonly List<Object> _created = new List<Object>();
		private GameObject _attacker;
		private Health _attackerHealth;
		private Hitbox2D _hitbox;
		private Health _target;

		[SetUp]
		public void SetUp()
		{
			// Built inactive so the fields are set before Awake.
			// The attacker stands at the origin on the target layer, so the box also covers its own collider
			_attacker = new GameObject("Attacker") { layer = TargetLayer };
			_attacker.SetActive(false);
			_created.Add(_attacker);
			_attacker.AddComponent<BoxCollider2D>();
			_attacker.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
			_attacker.AddComponent<PlatformerMotor2D>();
			_attackerHealth = _attacker.AddComponent<Health>();
			_hitbox = _attacker.AddComponent<Hitbox2D>();
			AnimatorTestUtility.SetField(_hitbox, "_targetLayers", (LayerMask)(1 << TargetLayer));
			AnimatorTestUtility.SetField(_hitbox, "_offset", new Vector2(0.5f, 0f));
			AnimatorTestUtility.SetField(_hitbox, "_size", Vector2.one);

			GameObject target = new GameObject("Target") { layer = TargetLayer };
			_created.Add(target);
			target.transform.position = new Vector2(1f, 0f);
			target.AddComponent<BoxCollider2D>();
			_target = target.AddComponent<Health>();
		}

		[TearDown]
		public void TearDown()
		{
			foreach (Object created in _created)
				Object.DestroyImmediate(created);
			_created.Clear();
		}

		[Test]
		public void Hit_DamagesTargetInBox_NotItself()
		{
			_attacker.SetActive(true);
			Physics2D.SyncTransforms();

			Assert.AreEqual(1, _hitbox.Hit());

			Assert.AreEqual(_target.Max - 1, _target.Current);
			Assert.AreEqual(_attackerHealth.Max, _attackerHealth.Current, "Hit itself");
		}

		[Test]
		public void Hit_FollowsFacing()
		{
			_attacker.SetActive(true);
			Physics2D.SyncTransforms();
			_attacker.GetComponent<PlatformerMotor2D>().Face(-1f);

			Assert.AreEqual(0, _hitbox.Hit());

			Assert.AreEqual(_target.Max, _target.Current, "Hit a target behind its back");
		}

		[UnityTest]
		public IEnumerator AnimationEvent_HitsOnce()
		{
			GameObject visual = new GameObject("Visual");
			visual.transform.SetParent(_attacker.transform, false);
			Animator animator = visual.AddComponent<Animator>();
			animator.runtimeAnimatorController = AnimatorTestUtility.CreateController(false, _created);
			CharacterAnimator characterAnimator = _attacker.AddComponent<CharacterAnimator>();
			AnimatorTestUtility.SetField(characterAnimator, "_animator", animator);
			// The Hurt clip raises "Hit" at its half
			TestAnimatedState attack = _attacker.AddComponent<TestAnimatedState>();
			AnimatorTestUtility.SetField(attack, "_animationState", "Hurt");
			AnimatorTestUtility.SetField(_hitbox, "_animationEvent", "Hit");
			_attacker.SetActive(true);
			yield return null;

			characterAnimator.ApplyState(attack);
			Assert.AreEqual(_target.Max, _target.Current, "Hit before the event");
			yield return new WaitForSeconds(AnimatorTestUtility.ClipLength + 0.1f);

			Assert.AreEqual(_target.Max - 1, _target.Current);
		}
	}
}
#endif
