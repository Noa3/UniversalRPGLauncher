using System.Collections.Generic;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kEventSchedulerBehavior : TestBase
{
    public void Test_TriggerAtSkipsNonMatchingEventOnSharedCoordinate()
    {
        var state = new GameSimulationState();
        var first = EventWithPage(1, 4, 5, Rm2kEventTrigger.Touch, layer: 1, switchId: 1);
        var second = EventWithPage(2, 4, 5, Rm2kEventTrigger.Action, layer: 1, switchId: 2);
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { first, second });

        AssertTrue(scheduler.TriggerAt(4, 5, Rm2kEventTrigger.Action),
            "a non-matching first event must not mask a matching event at the same coordinate");
        scheduler.ExecuteFrame();

        AssertTrue(state.Switches.Count >= 2);
        AssertFalse(state.Switches[0], "touch-only event was not started by action");
        AssertTrue(state.Switches[1], "matching action event executed");
    }

    public void Test_BlockingQueryUsesHighestActivePageLayer()
    {
        var state = new GameSimulationState();
        state.Switches.Add(false);
        var eventData = new Rm2kMap.Event(3, 2, 3);
        eventData.Pages.Add(new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Action,
            Layer = 0,
        });
        eventData.Pages.Add(new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Action,
            Layer = 1,
            Conditions = new Dictionary<string, object>
            {
                ["switch_id"] = 1,
                ["switch_value"] = true,
            },
        });
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { eventData });

        AssertFalse(scheduler.HasBlockingSameLayerEventAt(2, 3),
            "inactive same-layer page does not block when lower active page wins");

        state.Switches[0] = true;
        AssertTrue(scheduler.HasBlockingSameLayerEventAt(2, 3),
            "highest eligible same-layer page blocks the coordinate");
    }

    public void Test_BlockingQueryIgnoresBelowAndAboveLayers()
    {
        var state = new GameSimulationState();
        var below = EventWithPage(4, 7, 8, Rm2kEventTrigger.Action, layer: 0, switchId: 1);
        var above = EventWithPage(5, 9, 10, Rm2kEventTrigger.Action, layer: 2, switchId: 2);
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { below, above });

        AssertFalse(scheduler.HasBlockingSameLayerEventAt(7, 8));
        AssertFalse(scheduler.HasBlockingSameLayerEventAt(9, 10));
    }

    public void Test_AutorunPageRestartsWhileConditionsRemainActive()
    {
        var state = new GameSimulationState();
        var eventData = new Rm2kMap.Event(6, 0, 0);
        var page = new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Autorun,
            Layer = 1,
        };
        page.Commands.Add(new Rm2kMap.EventCommand(
            EventInterpreter.ControlVars,
            new List<int>
            {
                1, 1, 0,
                EventInterpreter.VarOpAdd,
                EventInterpreter.VarOperandConstant,
                1,
            }));
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        eventData.Pages.Add(page);
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { eventData });

        scheduler.ExecuteFrame(); // starts autorun, executes +1
        scheduler.ExecuteFrame(); // executes End and removes interpreter
        scheduler.ExecuteFrame(); // active page starts again, executes +1

        AssertTrue(state.Variables.Count >= 1);
        AssertEq(state.Variables[0], 2,
            "autorun page restarts after completing while its page remains active");
    }

    public void Test_ForegroundAutorunsRunSerially()
    {
        var state = new GameSimulationState();
        state.Switches.Add(true); // condition for event 10

        var first = new Rm2kMap.Event(10, 0, 0);
        var firstPage = new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Autorun,
            Layer = 1,
            Conditions = new Dictionary<string, object>
            {
                ["switch_id"] = 1,
                ["switch_value"] = true,
            },
        };
        firstPage.Commands.Add(new Rm2kMap.EventCommand(
            EventInterpreter.ControlSwitches,
            new List<int> { 1, 1, 0, EventInterpreter.SwitchModeOff }));
        firstPage.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        first.Pages.Add(firstPage);

        var second = EventWithPage(11, 0, 0, Rm2kEventTrigger.Autorun, layer: 1, switchId: 2);
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { first, second });

        scheduler.ExecuteFrame();
        AssertFalse(state.Switches[0], "first autorun executed and disabled its own condition");
        AssertEq(state.Switches.Count, 1, "second foreground autorun did not run concurrently");
        AssertTrue(scheduler.ForegroundBusy, "first foreground interpreter remains active until End");

        scheduler.ExecuteFrame(); // End first interpreter
        AssertFalse(scheduler.ForegroundBusy, "foreground slot clears after End");

        scheduler.ExecuteFrame(); // second autorun may now start
        AssertTrue(state.Switches.Count >= 2 && state.Switches[1],
            "next eligible autorun starts only after the previous foreground event finishes");
    }

    public void Test_ForegroundEventLocksPlayerMovementUntilCompletion()
    {
        var state = new GameSimulationState();
        state.ConfigureMap(1, 2, 1, new[] { true, true });
        var eventData = EventWithPage(20, 0, 0, Rm2kEventTrigger.Action, layer: 1, switchId: 3);
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { eventData });

        AssertTrue(scheduler.TriggerAt(0, 0, Rm2kEventTrigger.Action));
        AssertTrue(scheduler.ForegroundBusy);
        AssertTrue(state.PlayerInputLocked, "foreground interpreter owns the player input lock");
        AssertFalse(state.TryMove(1, 0), "movement is rejected while foreground event is active");
        AssertEq(state.MapX, 0, "locked input does not move the player");

        scheduler.ExecuteFrame(); // switch command
        AssertTrue(state.PlayerInputLocked, "input stays locked until End is consumed");
        scheduler.ExecuteFrame(); // End

        AssertFalse(scheduler.ForegroundBusy);
        AssertFalse(state.PlayerInputLocked, "input lock is released when foreground execution completes");
        AssertTrue(state.TryMove(1, 0), "movement resumes after foreground completion");
        AssertEq(state.MapX, 1);
    }

    public void Test_ParallelEventDoesNotLockPlayerMovement()
    {
        var state = new GameSimulationState();
        state.ConfigureMap(1, 2, 1, new[] { true, true });
        var parallel = EventWithPage(21, 0, 0, Rm2kEventTrigger.Parallel, layer: 0, switchId: 4);
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { parallel });

        scheduler.ExecuteFrame();

        AssertTrue(scheduler.ActiveInterpreterCount >= 1, "parallel interpreter is active");
        AssertFalse(scheduler.ForegroundBusy, "parallel interpreter does not occupy foreground slot");
        AssertFalse(state.PlayerInputLocked, "parallel interpreter does not lock player input");
        AssertTrue(state.TryMove(1, 0), "player may move while parallel event runs");
    }

    private static Rm2kMap.Event EventWithPage(
        int pId,
        int pX,
        int pY,
        Rm2kEventTrigger pTrigger,
        int layer,
        int switchId)
    {
        var eventData = new Rm2kMap.Event(pId, pX, pY);
        var page = new Rm2kMap.EventPage
        {
            Trigger = (int)pTrigger,
            Layer = layer,
        };
        page.Commands.Add(new Rm2kMap.EventCommand(
            EventInterpreter.ControlSwitches,
            new List<int> { switchId, switchId, 0, EventInterpreter.SwitchModeOn }));
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        eventData.Pages.Add(page);
        return eventData;
    }
}
