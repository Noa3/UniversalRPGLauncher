using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Timed Wait follows EasyRPG Player SetupWait (commit
/// 0de2a9ab466a133ac6e192a84bd761a1d81f5f14): zero waits one frame;
/// positive durations use duration * 60 / 10 without a ten-second cap.
/// These are reference-derived command fixtures, not a full-game parity test.
/// </summary>
public partial class TestRm2kWaitDuration : TestBase
{
    public void Test_LongWaitHoldsTheFollowingCommandForTheWholeDuration()
    {
        var state = new GameSimulationState();
        var interpreter = Create(state, new List<int> { 200 });
        interpreter.ExecuteFrame();
        AssertEq(interpreter.WaitFramesRemaining, 1200, "Twenty seconds is 1200 simulation frames, not 600");
        for (var frame = 0; frame < 1200; frame++)
        {
            interpreter.ExecuteFrame();
            AssertEq(interpreter.CurrentCommandIndex, 1, $"Next command stays blocked through waiting frame {frame}");
        }
        AssertEq(interpreter.WaitFramesRemaining, 0, "The full duration expires");
        interpreter.ExecuteFrame();
        AssertEq(interpreter.CurrentCommandIndex, 2, "The following command runs only after all wait frames");
    }

    public void Test_LargestRepresentableDurationDoesNotOverflowOrClip()
    {
        var tenths = int.MaxValue / 6;
        var interpreter = Create(new GameSimulationState(), new List<int> { tenths });
        interpreter.ExecuteFrame();
        AssertEq(interpreter.WaitFramesRemaining, tenths * 6, "Frame counter preserves a representable duration exactly");
        interpreter.ExecuteFrame();
        AssertEq(interpreter.WaitFramesRemaining, tenths * 6 - 1, "One frame decrements the long counter normally");
    }

    public void Test_InvalidDurationsAreReportedWithoutCrashingTheEvent()
    {
        foreach (var parameters in new[] { new List<int>(), new List<int> { -1 }, new List<int> { int.MaxValue } })
        {
            var state = new GameSimulationState();
            var interpreter = Create(state, parameters);
            interpreter.ExecuteFrame();
            AssertEq(interpreter.WaitFramesRemaining, 0, "Missing, negative or overflowing duration invents no wait");
            AssertTrue(state.Diagnostics.Any(x => x.Contains("Wait") && x.Contains("invalid")),
                "Rejected duration is explicitly diagnosed");
            interpreter.ExecuteFrame();
            AssertEq(interpreter.CurrentCommandIndex, 2, "One bad duration does not discard the rest of the event");
        }
    }

    public void Test_ZeroDurationStillWaitsOneFrame()
    {
        var interpreter = Create(new GameSimulationState(), new List<int> { 0 });
        interpreter.ExecuteFrame();
        AssertEq(interpreter.WaitFramesRemaining, 1, "Zero duration waits one frame");
        interpreter.ExecuteFrame();
        AssertEq(interpreter.CurrentCommandIndex, 1, "That frame does not execute the following command");
        interpreter.ExecuteFrame();
        AssertEq(interpreter.CurrentCommandIndex, 2, "The next command runs on the following frame");
    }

    private static EventInterpreter Create(GameSimulationState state, List<int> parameters) =>
        new(state, 1, new List<Rm2kMap.EventCommand>
        {
            new(EventInterpreter.Wait, parameters),
            new(EventInterpreter.Comment, null, "After the wait"),
            new(EventInterpreter.End)
        });
}
