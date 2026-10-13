using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Yenotsoftware.KSE2DPlatformerFramework.Editor.Tests
{
	public class CharacterAnimatorEditorTests
	{
		private GameObject _root;

		[TearDown]
		public void TearDown()
		{
			if (_root)
				Object.DestroyImmediate(_root);
		}

		[Test]
		public void Adding_FindsInactiveAnimator_AndAddsRelayWithUndo()
		{
			_root = new GameObject("Character");
			GameObject visual = new GameObject("Visual");
			visual.transform.SetParent(_root.transform, false);
			visual.AddComponent<Animator>();
			visual.SetActive(false);

			Undo.IncrementCurrentGroup();
			Undo.AddComponent<CharacterAnimator>(_root);
			Assert.IsNotNull(visual.GetComponent<AnimationEventRelay>(), "Relay not added next to the inactive Animator");

			Undo.PerformUndo();
			Assert.IsNull(_root.GetComponent<CharacterAnimator>(), "Undo did not remove CharacterAnimator");
			Assert.IsNull(visual.GetComponent<AnimationEventRelay>(), "Undo left the relay behind");
		}
	}
}
