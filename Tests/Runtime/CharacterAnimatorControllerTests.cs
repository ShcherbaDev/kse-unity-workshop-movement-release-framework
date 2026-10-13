#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	public class CharacterAnimatorControllerTests
	{
		private readonly List<Object> _created = new List<Object>();

		[TearDown]
		public void TearDown()
		{
			foreach (Object created in _created)
				Object.DestroyImmediate(created);
			_created.Clear();
		}

		[UnityTest]
		public IEnumerator Hurt_PlaysOnce_ThenReturnsToBaseState()
		{
			GameObject enemy = new GameObject("Enemy");
			enemy.SetActive(false);
			_created.Add(enemy);
			enemy.AddComponent<BoxCollider2D>();
			EnemyController controller = enemy.AddComponent<EnemyController>();
			enemy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

			GameObject visual = new GameObject("Visual");
			visual.transform.SetParent(enemy.transform, false);
			Animator animator = visual.AddComponent<Animator>();
			animator.runtimeAnimatorController = AnimatorTestUtility.CreateController(true, _created);
			AnimatorTestUtility.SetField(enemy.AddComponent<CharacterAnimator>(), "_animator", animator);

			BaseTestState baseState = enemy.AddComponent<BaseTestState>();
			AnimatorTestUtility.SetField(baseState, "_animationState", "Run");
			HurtTestState hurt = enemy.AddComponent<HurtTestState>();
			AnimatorTestUtility.SetField(hurt, "_animationState", "Hurt");

			enemy.SetActive(true);
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			Assert.AreSame(baseState, controller.CurrentState);
			Assert.AreEqual(Animator.StringToHash("Run"), animator.GetCurrentAnimatorStateInfo(0).shortNameHash, "Controller did not play the state's animation");

			hurt.Trigger();
			yield return new WaitForFixedUpdate();
			Assert.AreSame(hurt, controller.CurrentState);
			Assert.AreEqual(Animator.StringToHash("Hurt"), animator.GetCurrentAnimatorStateInfo(0).shortNameHash);

			// Ignored while hurt: one hit = one Hurt
			hurt.Trigger();
			yield return new WaitForSeconds(AnimatorTestUtility.ClipLength + 0.2f);

			Assert.AreSame(baseState, controller.CurrentState, "Stuck in Hurt");
			Assert.AreEqual(1, hurt.EnterCount);
		}
	}

	public class BaseTestState : EnemyState
	{
		public override bool CanEnter()
		{
			return true;
		}
	}

	// Same code as the README / demo Hurt state, plus an enter counter
	public class HurtTestState : EnemyState
	{
		private bool _requested;
		private bool _active;

		public int EnterCount { get; private set; }

		public override int Priority => 30;

		public void Trigger()
		{
			if (!_active)
				_requested = true;
		}

		public override bool CanEnter() => _requested;
		public override bool CanExit() => IsAnimationFinished;

		public override void Enter()
		{
			_requested = false;
			_active = true;
			EnterCount++;
		}

		public override void Exit() => _active = false;

		public override void Tick(float deltaTime) => Motor.SetVelocityX(0f);
	}
}
#endif
