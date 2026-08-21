using System.Collections.Generic;
using UniversalRPG.Core;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestVirtualClock : TestBase
{
	private VirtualClock _clock = null!;
	private int _callbackCount;

	public override void Setup()
	{
		_clock = new VirtualClock();
		_callbackCount = 0;
	}

	private void OnCallback()
	{
		_callbackCount += 1;
	}

	public void Test_NormalSpeedAdvancesOneTickPerSixtieth()
	{
		_clock.ProcessFrame(1.0 / 60.0);
		AssertEq(_clock.GetSimulationTicks(), 1);
	}

	public void Test_FastForwardUsesSpeedFactor()
	{
		_clock.SetSpeedMode(VirtualClock.SpeedMode.FastForward, 2.0f);
		_clock.ProcessFrame(1.0 / 60.0);
		AssertEq(_clock.GetSimulationTicks(), 2);
		AssertEq(_clock.GetSpeedMultiplier(), 2.0f);
	}

	public void Test_SlowMotionHalfSpeed()
	{
		_clock.SetSpeedMode(VirtualClock.SpeedMode.SlowMotion, 0.5f);
		_clock.ProcessFrame(1.0 / 60.0);
		AssertEq(_clock.GetSimulationTicks(), 0);
		_clock.ProcessFrame(1.0 / 60.0);
		AssertEq(_clock.GetSimulationTicks(), 1);
		AssertEq(_clock.GetSpeedMultiplier(), 0.5f);
	}

	public void Test_PauseDoesNotAdvanceSimulation()
	{
		_clock.SetSpeedMode(VirtualClock.SpeedMode.Paused);
		_clock.ProcessFrame(10.0);
		AssertEq(_clock.GetSimulationTicks(), 0);
	}

	public void Test_OneShotCallbackFiresOnce()
	{
		_clock.ScheduleCallback(OnCallback, 2);
		FireCallbacks(_clock.StepSingleTick());
		AssertEq(_callbackCount, 0);
		FireCallbacks(_clock.StepSingleTick());
		AssertEq(_callbackCount, 1);
		FireCallbacks(_clock.StepSingleTick());
		AssertEq(_callbackCount, 1);
	}

	public void Test_RepeatingCallbackPreservesInterval()
	{
		_clock.ScheduleRepeatingCallback(OnCallback, 3);
		for (var index = 0; index < 9; index++)
		{
			FireCallbacks(_clock.StepSingleTick());
		}
		AssertEq(_callbackCount, 3);
	}

	public void Test_CallbackHandlesAreStableAfterOtherEventExpires()
	{
		_clock.ScheduleCallback(OnCallback, 1);
		var repeatingId = _clock.ScheduleRepeatingCallback(OnCallback, 10);
		_clock.StepSingleTick();
		AssertEq(_clock.GetRemainingTicks(repeatingId), 9);
		AssertTrue(_clock.CancelCallback(repeatingId));
		AssertEq(_clock.GetRemainingTicks(repeatingId), 0);
	}

	public void Test_ResetClearsEventsAndTicks()
	{
		_clock.ScheduleRepeatingCallback(OnCallback, 1);
		_clock.StepSingleTick();
		_clock.Reset();
		AssertEq(_clock.GetSimulationTicks(), 0);
		AssertEq((int)_clock.GetStats()["scheduled_events"], 0);
	}

	private static void FireCallbacks(List<System.Action> pCallbacks)
	{
		foreach (var callback in pCallbacks)
		{
			callback.Invoke();
		}
	}
}
