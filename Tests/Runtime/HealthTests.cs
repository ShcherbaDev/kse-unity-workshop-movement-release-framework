#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	public class HealthTests
	{
		private readonly List<float> _changed = new List<float>();
		private GameObject _object;
		private Health _health;
		private int _damaged;
		private int _died;

		[SetUp]
		public void SetUp()
		{
			// Built inactive so the fields are set before Awake
			_object = new GameObject("Health");
			_object.SetActive(false);
			_health = _object.AddComponent<Health>();
			AnimatorTestUtility.SetField(_health, "_maxHealth", 3);
			AnimatorTestUtility.SetField(_health, "_invulnerabilityTime", 0f);
			_object.SetActive(true);

			_damaged = 0;
			_died = 0;
			_changed.Clear();
			_health.Damaged.AddListener(() => _damaged++);
			_health.Died.AddListener(() => _died++);
			_health.Changed.AddListener(_changed.Add);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_object);
		}

		[Test]
		public void SurvivedHit_RaisesDamagedAndChanged()
		{
			Assert.IsTrue(_health.TakeDamage(1));

			Assert.AreEqual(2, _health.Current);
			Assert.AreEqual(1, _damaged);
			Assert.AreEqual(0, _died);
			Assert.That(_changed, Is.EqualTo(new[] { 2f / 3f }).Within(1e-4f));
		}

		[Test]
		public void DeadlyHit_RaisesDiedOnly_AndLaterHitsAreIgnored()
		{
			Assert.IsTrue(_health.TakeDamage(5));
			Assert.IsFalse(_health.TakeDamage(1));

			Assert.AreEqual(0, _health.Current);
			Assert.IsTrue(_health.IsDead);
			Assert.AreEqual(1, _died);
			Assert.AreEqual(0, _damaged, "Hurt would start together with Death");
		}

		[UnityTest]
		public IEnumerator HitDuringInvulnerability_IsIgnored()
		{
			AnimatorTestUtility.SetField(_health, "_invulnerabilityTime", 0.2f);

			Assert.IsTrue(_health.TakeDamage(1));
			Assert.IsFalse(_health.TakeDamage(1), "Hit right after a hit");
			yield return new WaitForSeconds(0.3f);
			Assert.IsTrue(_health.TakeDamage(1), "Hit after the invulnerability");

			Assert.AreEqual(1, _health.Current);
		}
	}
}
#endif
