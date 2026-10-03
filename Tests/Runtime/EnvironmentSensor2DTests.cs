using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	public class EnvironmentSensor2DTests
	{
		private GameObject _ground;
		private GameObject _wall;
		private GameObject _character;
		private EnvironmentSensor2D _sensor;

		[SetUp]
		public void SetUp()
		{
			// 10x1 floor with its top at y = 0, spanning x = -5..5, and a wall on its right end.
			_ground = CreateBox("Ground", new Vector2(0f, -0.5f), new Vector2(10f, 1f));
			_wall = CreateBox("Wall", new Vector2(5.5f, 2f), new Vector2(1f, 4f));

			_character = CreateBox("Character", new Vector2(0f, 0.5f), Vector2.one);
			_character.AddComponent<Rigidbody2D>().freezeRotation = true;
			_sensor = _character.AddComponent<EnvironmentSensor2D>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.Destroy(_ground);
			Object.Destroy(_wall);
			Object.Destroy(_character);
		}

		[UnityTest]
		public IEnumerator StandingOnFloor_IsGrounded_WithGroundAheadBothWays()
		{
			yield return SettlePhysics();
			_sensor.Refresh();

			Assert.IsTrue(_sensor.IsGrounded);
			Assert.AreEqual(0, _sensor.WallDirection);
			Assert.IsTrue(_sensor.HasGroundAhead(1));
			Assert.IsTrue(_sensor.HasGroundAhead(-1));
		}

		[UnityTest]
		public IEnumerator AtFloorEdge_DetectsLedge()
		{
			_character.transform.position = new Vector2(-4.5f, 0.5f);
			yield return SettlePhysics();
			_sensor.Refresh();

			Assert.IsFalse(_sensor.HasGroundAhead(-1));
			Assert.IsTrue(_sensor.HasGroundAhead(1));
		}

		[UnityTest]
		public IEnumerator NextToWall_DetectsWallOnThatSide()
		{
			_character.transform.position = new Vector2(4.5f, 0.5f);
			yield return SettlePhysics();
			_sensor.Refresh();

			Assert.IsTrue(_sensor.IsTouchingWallRight);
			Assert.IsFalse(_sensor.IsTouchingWallLeft);
			Assert.AreEqual(1, _sensor.WallDirection);
		}

		[UnityTest]
		public IEnumerator MovingUpward_IsNotGrounded()
		{
			yield return SettlePhysics();
			_character.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0f, 5f);
			_sensor.Refresh();

			Assert.IsFalse(_sensor.IsGrounded);
		}

		private static IEnumerator SettlePhysics()
		{
			for (int i = 0; i < 5; i++)
				yield return new WaitForFixedUpdate();
		}

		private static GameObject CreateBox(string name, Vector2 position, Vector2 size)
		{
			var go = new GameObject(name);
			go.transform.position = position;
			go.AddComponent<BoxCollider2D>().size = size;
			return go;
		}
	}
}
