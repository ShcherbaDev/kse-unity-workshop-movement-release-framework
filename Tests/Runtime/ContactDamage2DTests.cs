#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	public class ContactDamage2DTests
	{
		private const int TargetLayer = 9;

		private GameObject _spikes;
		private GameObject _target;
		private GameObject _bystander;

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_spikes);
			Object.DestroyImmediate(_target);
			Object.DestroyImmediate(_bystander);
		}

		[UnityTest]
		public IEnumerator Touch_DamagesTargetLayerOnly_OncePerInvulnerability()
		{
			_spikes = new GameObject("Spikes");
			_spikes.SetActive(false);
			_spikes.transform.position = new Vector2(0f, -0.5f);
			_spikes.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);
			AnimatorTestUtility.SetField(_spikes.AddComponent<ContactDamage2D>(), "_targetLayers", (LayerMask)(1 << TargetLayer));
			_spikes.SetActive(true);

			Health target = CreateBody(out _target, -2f, TargetLayer);
			Health bystander = CreateBody(out _bystander, 2f, 0);

			// Many physics ticks of touching, all within the invulnerability after the first hit
			yield return new WaitForSeconds(0.5f);

			Assert.AreEqual(target.Max - 1, target.Current);
			Assert.AreEqual(bystander.Max, bystander.Current, "Damaged a layer that is not a target");
		}

		// A body resting on the spikes
		private static Health CreateBody(out GameObject body, float x, int layer)
		{
			body = new GameObject("Body") { layer = layer };
			body.SetActive(false);
			body.transform.position = new Vector2(x, 0.5f);
			body.AddComponent<BoxCollider2D>();
			body.AddComponent<Rigidbody2D>().sleepMode = RigidbodySleepMode2D.NeverSleep;
			Health health = body.AddComponent<Health>();
			AnimatorTestUtility.SetField(health, "_invulnerabilityTime", 10f);
			body.SetActive(true);
			return health;
		}
	}
}
#endif
