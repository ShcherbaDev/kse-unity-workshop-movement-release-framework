using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	public class PatrolStateTests
	{
		private readonly List<GameObject> _created = new List<GameObject>();
		private GameObject _enemy;
		private float _minX;
		private float _maxX;
		private float _minY;

		[TearDown]
		public void TearDown()
		{
			// Immediate, so this test's objects cannot collide with the next test's
			foreach (GameObject go in _created)
				Object.DestroyImmediate(go);
			_created.Clear();
		}

		[UnityTest]
		public IEnumerator AtLedge_TurnsAround_AndStaysOnPlatform()
		{
			// Platform spans x = -5..5, top at y = 0
			CreateBox("Platform", new Vector2(0f, -0.5f), new Vector2(10f, 1f));
			CreateEnemy(new Vector2(3f, 0.5f));

			yield return Track(3f);

			Assert.Greater(_minY, -0.1f, "Fell off the platform");
			Assert.Less(_maxX, 5f, "Walked past the ledge");
			Assert.Less(_minX, 2f, "Did not turn around");
		}

		[UnityTest]
		public IEnumerator AtWall_TurnsAround()
		{
			// Floor continues past the wall, so only the wall can turn the enemy
			CreateBox("Floor", new Vector2(0f, -0.5f), new Vector2(20f, 1f));
			CreateBox("Wall", new Vector2(5.5f, 2f), new Vector2(1f, 4f));
			CreateEnemy(new Vector2(3f, 0.5f));

			yield return Track(3f);

			Assert.Less(_maxX, 4.6f, "Pushed into the wall");
			Assert.Less(_minX, 2f, "Did not turn around");
		}

		[UnityTest]
		public IEnumerator OnPlatformAsWideAsItself_StandsStill()
		{
			CreateBox("Pillar", new Vector2(0f, -0.5f), Vector2.one);
			CreateEnemy(new Vector2(0f, 0.5f));

			yield return Track(2f);

			Assert.Greater(_minY, -0.1f, "Fell off the pillar");
			Assert.Less(_maxX - _minX, 0.1f, "Jittered back and forth");
		}

		private void CreateEnemy(Vector2 position)
		{
			// Built inactive so all components exist before Awake
			_enemy = CreateBox("Enemy", position, Vector2.one);
			_enemy.SetActive(false);
			_enemy.AddComponent<EnemyController>();
			_enemy.GetComponent<Rigidbody2D>().freezeRotation = true;
			_enemy.AddComponent<PatrolState>();
			_enemy.SetActive(true);
		}

		private GameObject CreateBox(string name, Vector2 position, Vector2 size)
		{
			GameObject go = new GameObject(name);
			go.transform.position = position;
			go.AddComponent<BoxCollider2D>().size = size;
			_created.Add(go);
			return go;
		}

		private IEnumerator Track(float seconds)
		{
			Vector2 start = _enemy.transform.position;
			_minX = _maxX = start.x;
			_minY = start.y;
			float end = Time.time + seconds;
			while (Time.time < end)
			{
				yield return new WaitForFixedUpdate();
				Vector2 p = _enemy.transform.position;
				_minX = Mathf.Min(_minX, p.x);
				_maxX = Mathf.Max(_maxX, p.x);
				_minY = Mathf.Min(_minY, p.y);
			}
		}
	}
}
