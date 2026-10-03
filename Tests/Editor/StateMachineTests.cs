using System.Collections.Generic;
using NUnit.Framework;

namespace Yenotsoftware.KSE2DPlatformerFramework.Editor.Tests
{
	public class StateMachineTests
	{
		private class FakeState : IState
		{
			public readonly List<string> Log;
			public readonly string Name;
			public bool Enterable = true;
			public bool Exitable = true;

			public FakeState(string name, int priority, List<string> log)
			{
				Name = name;
				Priority = priority;
				Log = log;
			}

			public int Priority { get; }
			public bool CanEnter() => Enterable;
			public bool CanExit() => Exitable;
			public void Enter() => Log.Add($"enter {Name}");
			public void Exit() => Log.Add($"exit {Name}");
			public void Tick(float deltaTime) => Log.Add($"tick {Name}");
		}

		private List<string> _log;
		private FakeState _low;
		private FakeState _high;
		private StateMachine<FakeState> _machine;

		[SetUp]
		public void SetUp()
		{
			_log = new List<string>();
			_low = new FakeState("low", 0, _log);
			_high = new FakeState("high", 10, _log) { Enterable = false };
			_machine = new StateMachine<FakeState>(new[] { _low, _high });
		}

		[Test]
		public void FirstTick_EntersHighestEnterableState_AndTicksIt()
		{
			_machine.Tick(0.02f);

			Assert.AreSame(_low, _machine.Current);
			CollectionAssert.AreEqual(new[] { "enter low", "tick low" }, _log);
		}

		[Test]
		public void HigherPriorityState_TakesOver_WhenItCanEnter()
		{
			FakeState previous = null, next = null;
			_machine.StateChanged += (p, n) => (previous, next) = (p, n);
			_machine.Tick(0.02f);
			_log.Clear();

			_high.Enterable = true;
			_machine.Tick(0.02f);

			Assert.AreSame(_high, _machine.Current);
			Assert.AreSame(_low, previous);
			Assert.AreSame(_high, next);
			CollectionAssert.AreEqual(new[] { "exit low", "enter high", "tick high" }, _log);
		}

		[Test]
		public void CurrentState_BlocksSwitch_WhileItCannotExit()
		{
			_machine.Tick(0.02f);
			_low.Exitable = false;
			_high.Enterable = true;

			_machine.Tick(0.02f);

			Assert.AreSame(_low, _machine.Current);
		}

		[Test]
		public void CurrentState_Stays_WhenNothingCanEnter()
		{
			_machine.Tick(0.02f);
			_low.Enterable = false;

			_machine.Tick(0.02f);

			Assert.AreSame(_low, _machine.Current);
		}

		[Test]
		public void SelectingCurrentStateAgain_DoesNotReEnterIt()
		{
			_machine.Tick(0.02f);
			_machine.Tick(0.02f);

			CollectionAssert.AreEqual(new[] { "enter low", "tick low", "tick low" }, _log);
		}

		[Test]
		public void EqualPriority_FirstListedWins()
		{
			var first = new FakeState("first", 5, _log);
			var second = new FakeState("second", 5, _log);
			var machine = new StateMachine<FakeState>(new[] { first, second });

			machine.Tick(0.02f);

			Assert.AreSame(first, machine.Current);
		}

		[Test]
		public void NoEnterableStates_LeavesCurrentNull()
		{
			_low.Enterable = false;

			_machine.Tick(0.02f);

			Assert.IsNull(_machine.Current);
			CollectionAssert.IsEmpty(_log);
		}
	}
}
