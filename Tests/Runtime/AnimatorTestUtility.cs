#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yenotsoftware.KSE2DPlatformerFramework.Tests
{
	// Editor-only: controllers and clips are built in memory with UnityEditor.Animations
	internal static class AnimatorTestUtility
	{
		public const float ClipLength = 0.2f;

		/// <summary>
		/// Base Layer states without transitions: Idle (default, loop), Run (loop),
		/// Hurt (one-shot, event "Hit" at half), Start (one-shot, event "Begin" at time 0)
		/// </summary>
		public static AnimatorController CreateController(bool withParameters, List<Object> created)
		{
			AnimatorController controller = new AnimatorController();
			controller.AddLayer("Base Layer");
			if (withParameters)
			{
				controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
				controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
				controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
			}

			AnimatorStateMachine machine = controller.layers[0].stateMachine;
			AddState(machine, "Idle", true, created);
			AddState(machine, "Run", true, created);
			AddState(machine, "Hurt", false, created, new AnimationEvent { time = ClipLength / 2f, functionName = "Raise", stringParameter = "Hit" });
			AddState(machine, "Start", false, created, new AnimationEvent { time = 0f, functionName = "Raise", stringParameter = "Begin" });
			created.Add(controller);
			return controller;
		}

		/// <summary>
		/// Sets a private field declared on the target's type or any base type
		/// </summary>
		public static void SetField(object target, string fieldName, object value)
		{
			for (Type type = target.GetType(); type != null; type = type.BaseType)
			{
				FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
				if (field == null)
					continue;
				field.SetValue(target, value);
				return;
			}

			throw new MissingFieldException(target.GetType().Name, fieldName);
		}

		private static void AddState(AnimatorStateMachine machine, string name, bool loop, List<Object> created, params AnimationEvent[] events)
		{
			AnimationClip clip = new AnimationClip { name = name };

			// A constant dummy curve: the clip only needs a length
			clip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Constant(0f, ClipLength, 0f));
			AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
			settings.loopTime = loop;
			AnimationUtility.SetAnimationClipSettings(clip, settings);
			AnimationUtility.SetAnimationEvents(clip, events);
			machine.AddState(name).motion = clip;
			created.Add(clip);
		}
	}
}
#endif
