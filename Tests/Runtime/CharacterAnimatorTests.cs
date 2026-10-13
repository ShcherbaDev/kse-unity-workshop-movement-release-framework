#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	public class CharacterAnimatorTests
	{
		private const float ClipLength = AnimatorTestUtility.ClipLength;

		private readonly List<Object> _created = new List<Object>();
		private readonly List<string> _warnings = new List<string>();
		private GameObject _root;
		private GameObject _visual;
		private Animator _animator;
		private CharacterAnimator _characterAnimator;

		[SetUp]
		public void SetUp()
		{
			Application.logMessageReceived += OnLog;
		}

		[TearDown]
		public void TearDown()
		{
			Application.logMessageReceived -= OnLog;
			_warnings.Clear();
			foreach (Object created in _created)
				Object.DestroyImmediate(created);
			_created.Clear();
		}

		[UnityTest]
		public IEnumerator ApplyState_PlaysItsAnimationImmediately()
		{
			CreateCharacter();
			TestAnimatedState run = AddState("Run");
			yield return Activate();

			_characterAnimator.ApplyState(run);

			// No frame passed: the state info must already be fresh
			Assert.AreEqual(Animator.StringToHash("Run"), CurrentStateHash());
		}

		[UnityTest]
		public IEnumerator EmptyAnimationState_KeepsCurrentAnimation()
		{
			CreateCharacter();
			TestAnimatedState run = AddState("Run");
			TestAnimatedState empty = AddState("");
			yield return Activate();

			_characterAnimator.ApplyState(run);
			_characterAnimator.ApplyState(empty);
			yield return null;

			Assert.AreEqual(Animator.StringToHash("Run"), CurrentStateHash());
		}

		[UnityTest]
		public IEnumerator SameAnimationAsPreviousState_DoesNotRestart()
		{
			CreateCharacter();
			TestAnimatedState first = AddState("Run");
			TestAnimatedState second = AddState("Run");
			yield return Activate();

			_characterAnimator.ApplyState(first);
			yield return new WaitForSeconds(ClipLength / 2f);
			float before = NormalizedTime();
			_characterAnimator.ApplyState(second);

			Assert.Greater(before, 0.1f, "Animation did not advance before the switch");
			Assert.That(NormalizedTime(), Is.EqualTo(before).Within(1e-4f), "Restarted");
		}

		[UnityTest]
		public IEnumerator SameAnimationAfterEmptyState_Restarts()
		{
			CreateCharacter();
			TestAnimatedState run = AddState("Run");
			TestAnimatedState empty = AddState("");
			yield return Activate();

			_characterAnimator.ApplyState(run);
			yield return new WaitForSeconds(ClipLength / 2f);
			_characterAnimator.ApplyState(empty);
			_characterAnimator.ApplyState(run);

			Assert.Less(NormalizedTime(), 0.01f);
		}

		[UnityTest]
		public IEnumerator IsAnimationFinished_FalseOnEntry_TrueAfterClip()
		{
			CreateCharacter();
			TestAnimatedState hurt = AddState("Hurt");
			yield return Activate();

			_characterAnimator.ApplyState(hurt);
			Assert.IsFalse(hurt.Finished, "Finished on entry");

			yield return new WaitForSeconds(ClipLength + 0.1f);
			Assert.IsTrue(hurt.Finished, "Not finished after the clip");
		}

		[UnityTest]
		public IEnumerator ReenteringAfterFinish_IsNotFinishedImmediately()
		{
			CreateCharacter();
			TestAnimatedState hurt = AddState("Hurt");
			TestAnimatedState run = AddState("Run");
			yield return Activate();

			_characterAnimator.ApplyState(hurt);
			yield return new WaitForSeconds(ClipLength + 0.1f);
			Assert.IsTrue(hurt.Finished);

			// No Animator update between the switches, so stale state info would still say Hurt at its end
			_characterAnimator.ApplyState(run);
			_characterAnimator.ApplyState(hurt);

			Assert.IsFalse(hurt.Finished);
		}

		[UnityTest]
		public IEnumerator UnknownAnimationState_WarnsOnce_AndCountsAsFinished()
		{
			CreateCharacter();
			TestAnimatedState missing = AddState("Missing");
			yield return Activate();

			_characterAnimator.ApplyState(missing);
			_characterAnimator.ApplyState(missing);
			yield return null;

			Assert.AreEqual(1, _warnings.FindAll(w => w.Contains("'Missing'")).Count);
			Assert.IsTrue(missing.Finished);
		}

		[UnityTest]
		public IEnumerator NoAnimatorAssigned_WarnsAndCountsAsFinished()
		{
			CreateCharacter(assignAnimator: false);
			TestAnimatedState hurt = AddState("Hurt");
			yield return Activate();

			_characterAnimator.ApplyState(hurt);
			yield return null;

			Assert.AreEqual(1, _warnings.FindAll(w => w.Contains("no Animator")).Count);
			Assert.IsTrue(hurt.Finished);
		}

		[UnityTest]
		public IEnumerator StateWithoutCharacterAnimator_CountsAsFinished()
		{
			GameObject go = new GameObject("No Animator");
			_created.Add(go);
			TestAnimatedState hurt = go.AddComponent<TestAnimatedState>();
			AnimatorTestUtility.SetField(hurt, "_animationState", "Hurt");
			yield return null;

			Assert.IsTrue(hurt.Finished);
		}

		[UnityTest]
		public IEnumerator ReenablingVisual_ReappliesAnimation()
		{
			CreateCharacter();
			TestAnimatedState run = AddState("Run");
			yield return Activate();
			_characterAnimator.ApplyState(run);

			// Unity resets the Animator to its default state (Idle) on disable
			_visual.SetActive(false);
			yield return null;
			_visual.SetActive(true);
			yield return null;
			yield return null;

			Assert.AreEqual(Animator.StringToHash("Run"), CurrentStateHash());
		}

		[UnityTest]
		public IEnumerator ReenablingRoot_ReappliesAnimation()
		{
			CreateCharacter();
			TestAnimatedState run = AddState("Run");
			yield return Activate();
			_characterAnimator.ApplyState(run);

			_root.SetActive(false);
			yield return null;
			_root.SetActive(true);
			yield return null;
			yield return null;

			Assert.AreEqual(Animator.StringToHash("Run"), CurrentStateHash());
		}

		[UnityTest]
		public IEnumerator WritesMovementParameters()
		{
			CreateCharacter();
			yield return Activate();
			_root.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(-2f, 3f);
			// The sensor never refreshes without a controller, so it reports not grounded
			_animator.SetBool("Grounded", true);

			yield return null;
			yield return null;

			Assert.That(_animator.GetFloat("Speed"), Is.EqualTo(2f).Within(1e-3f));
			Assert.That(_animator.GetFloat("VerticalSpeed"), Is.EqualTo(3f).Within(1e-3f));
			Assert.IsFalse(_animator.GetBool("Grounded"));
		}

		[UnityTest]
		public IEnumerator ControllerWithoutParameters_LogsNothing()
		{
			CreateCharacter(withParameters: false);
			yield return Activate();

			yield return null;
			yield return null;

			CollectionAssert.IsEmpty(_warnings);
		}

		[UnityTest]
		public IEnumerator EventFromActiveStatesClip_ReachesStateOnce()
		{
			CreateCharacter();
			TestAnimatedState hurt = AddState("Hurt");
			List<string> raised = new List<string>();
			yield return Activate();
			_characterAnimator.AnimationEventRaised += raised.Add;

			_characterAnimator.ApplyState(hurt);
			yield return new WaitForSeconds(ClipLength + 0.1f);

			CollectionAssert.AreEqual(new[] { "Hit" }, hurt.Events);
			CollectionAssert.AreEqual(new[] { "Hit" }, raised);
		}

		[UnityTest]
		public IEnumerator EventFromAnotherStatesClip_OnlyRaisesCSharpEvent()
		{
			CreateCharacter();
			TestAnimatedState hurt = AddState("Hurt");
			TestAnimatedState empty = AddState("");
			List<string> raised = new List<string>();
			yield return Activate();
			_characterAnimator.AnimationEventRaised += raised.Add;

			// The Animator keeps playing Hurt while a state without animation is active
			_characterAnimator.ApplyState(hurt);
			_characterAnimator.ApplyState(empty);
			yield return new WaitForSeconds(ClipLength + 0.1f);

			CollectionAssert.IsEmpty(hurt.Events);
			CollectionAssert.IsEmpty(empty.Events);
			CollectionAssert.AreEqual(new[] { "Hit" }, raised);
		}

		[UnityTest]
		public IEnumerator EventFromAnotherStatesClip_DoesNotReachStateWithOtherAnimation()
		{
			CreateCharacter();
			TestAnimatedState hurt = AddState("Hurt");
			TestAnimatedState missing = AddState("Missing");
			yield return Activate();

			// Missing is not in the Animator, so Hurt keeps playing while Missing is active
			_characterAnimator.ApplyState(hurt);
			_characterAnimator.ApplyState(missing);
			yield return new WaitForSeconds(ClipLength + 0.1f);

			CollectionAssert.IsEmpty(missing.Events);
		}

		[UnityTest]
		public IEnumerator EventAtTimeZero_FiresOnce()
		{
			CreateCharacter();
			TestAnimatedState start = AddState("Start");
			yield return Activate();

			_characterAnimator.ApplyState(start);
			yield return new WaitForSeconds(ClipLength + 0.1f);

			// Guards against a double fire from Update(0f) right after Play
			CollectionAssert.AreEqual(new[] { "Begin" }, start.Events);
		}

		[UnityTest]
		public IEnumerator AnimatorOnRoot_RoutesEvents()
		{
			CreateCharacter(animatorOnRoot: true);
			TestAnimatedState hurt = AddState("Hurt");
			yield return Activate();

			_characterAnimator.ApplyState(hurt);
			yield return new WaitForSeconds(ClipLength + 0.1f);

			CollectionAssert.AreEqual(new[] { "Hit" }, hurt.Events);
		}

		[Test]
		public void RelayWithoutCharacterAnimator_WarnsOnce()
		{
			GameObject go = new GameObject("Lonely Relay");
			_created.Add(go);
			AnimationEventRelay relay = go.AddComponent<AnimationEventRelay>();

			relay.Raise(new AnimationEvent { stringParameter = "Hit" });
			relay.Raise(new AnimationEvent { stringParameter = "Hit" });

			Assert.AreEqual(1, _warnings.FindAll(w => w.Contains(nameof(CharacterAnimator))).Count);
		}

		private void CreateCharacter(bool withParameters = true, bool assignAnimator = true, bool animatorOnRoot = false)
		{
			// Built inactive so every component exists before Awake
			_root = new GameObject("Character");
			_root.SetActive(false);
			_created.Add(_root);
			_root.AddComponent<BoxCollider2D>();
			_root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
			_root.AddComponent<EnvironmentSensor2D>();

			_visual = animatorOnRoot ? _root : new GameObject("Visual");
			if (!animatorOnRoot)
				_visual.transform.SetParent(_root.transform, false);
			_animator = _visual.AddComponent<Animator>();
			_animator.runtimeAnimatorController = AnimatorTestUtility.CreateController(withParameters, _created);

			_characterAnimator = _root.AddComponent<CharacterAnimator>();
			AnimatorTestUtility.SetField(_characterAnimator, "_animator", assignAnimator ? _animator : null);
		}

		private TestAnimatedState AddState(string animationState)
		{
			TestAnimatedState state = _root.AddComponent<TestAnimatedState>();
			AnimatorTestUtility.SetField(state, "_animationState", animationState);
			return state;
		}

		private IEnumerator Activate()
		{
			_root.SetActive(true);
			yield return null;
		}

		private int CurrentStateHash()
		{
			return _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
		}

		private float NormalizedTime()
		{
			return _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
		}

		private void OnLog(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Warning)
				_warnings.Add(condition);
		}
	}

	// Never enters on its own: tests switch states through CharacterAnimator.ApplyState
	public class TestAnimatedState : ComponentState
	{
		public readonly List<string> Events = new List<string>();

		public bool Finished => IsAnimationFinished;

		public override bool CanEnter()
		{
			return false;
		}

		public override void OnAnimationEvent(string eventName)
		{
			Events.Add(eventName);
		}
	}
}
#endif
