using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	// InputTestFixture isolates the Input System per test (virtual devices, no editor focus rules)
	public class PlayerJumpTests : InputTestFixture
	{
		private const float JumpHeight = 3f; // JumpState default
		// Physics steps in discrete ticks, so the peak lands slightly below the exact formula
		private const float HeightTolerance = 0.25f;

		private GameObject _ground;
		private GameObject _player;
		private InputActionAsset _actions;
		private Keyboard _keyboard;

		public override void Setup()
		{
			base.Setup();

			_ground = new GameObject("Ground");
			_ground.transform.position = new Vector2(0f, -0.5f);
			_ground.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);

			_keyboard = InputSystem.AddDevice<Keyboard>();
			_actions = ScriptableObject.CreateInstance<InputActionAsset>();
			InputActionMap map = _actions.AddActionMap("Player");
			InputAction move = map.AddAction("Move");
			move.AddCompositeBinding("2DVector")
				.With("Left", "<Keyboard>/a")
				.With("Right", "<Keyboard>/d");
			InputAction jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");

			// Built inactive so the input reader binds to the test actions on enable
			_player = new GameObject("Player");
			_player.SetActive(false);
			_player.transform.position = new Vector2(0f, 0.5f);
			_player.AddComponent<BoxCollider2D>();
			_player.AddComponent<PlayerController>();
			Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
			body.freezeRotation = true;
			body.gravityScale = 3f;
			_player.AddComponent<GroundState>();
			// Stand-in for dragging actions into the Inspector fields
			SetActionReference(_player.GetComponent<PlayerInputReader>(), "_moveAction", move);
			SetActionReference(_player.AddComponent<AirState>(), "_jumpAction", jump);
			SetActionReference(_player.AddComponent<JumpState>(), "_jumpAction", jump);
			_player.SetActive(true);
		}

		public override void TearDown()
		{
			// Immediate, so this test's player cannot collide with the next test's player
			Object.DestroyImmediate(_player);
			Object.DestroyImmediate(_ground);
			Object.DestroyImmediate(_actions);
			base.TearDown();
		}

		[UnityTest]
		public IEnumerator HeldJump_ReachesJumpHeight()
		{
			yield return Wait(0.2f);
			float startY = _player.transform.position.y;

			Press(_keyboard.spaceKey);
			float peak = 0f;
			yield return TrackPeak(1.5f, startY, h => peak = h);

			Assert.AreEqual(JumpHeight, peak, HeightTolerance);
		}

		[UnityTest]
		public IEnumerator TappedJump_IsLowerThanHeldJump()
		{
			yield return Wait(0.2f);
			float startY = _player.transform.position.y;

			Press(_keyboard.spaceKey);
			yield return Wait(0.05f);
			Release(_keyboard.spaceKey);
			float peak = 0f;
			yield return TrackPeak(1.5f, startY, h => peak = h);

			Assert.Greater(peak, 0.2f);
			Assert.Less(peak, JumpHeight * 0.7f);
		}

		[UnityTest]
		public IEnumerator SecondPressRightAfterJump_DoesNotJumpAgain()
		{
			yield return Wait(0.2f);
			float startY = _player.transform.position.y;

			// Re-press within the coyote window: must not count as a second ground jump
			Press(_keyboard.spaceKey);
			yield return Wait(0.04f);
			Release(_keyboard.spaceKey);
			yield return new WaitForFixedUpdate();
			Press(_keyboard.spaceKey);
			float peak = 0f;
			yield return TrackPeak(1.5f, startY, h => peak = h);

			// A second jump would relaunch from mid-air and overshoot the single jump height
			Assert.Greater(peak, 0.2f);
			Assert.Less(peak, JumpHeight + HeightTolerance);
		}

		private static void SetActionReference(Component component, string fieldName, InputAction action)
		{
			FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			field.SetValue(component, InputActionReference.Create(action));
		}

		private static IEnumerator Wait(float seconds)
		{
			float end = Time.time + seconds;
			while (Time.time < end)
				yield return new WaitForFixedUpdate();
		}

		private IEnumerator TrackPeak(float seconds, float startY, System.Action<float> report)
		{
			float peak = 0f;
			float end = Time.time + seconds;
			while (Time.time < end)
			{
				yield return new WaitForFixedUpdate();
				peak = Mathf.Max(peak, _player.transform.position.y - startY);
			}

			report(peak);
		}
	}
}
