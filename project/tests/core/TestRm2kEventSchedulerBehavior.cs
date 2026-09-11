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
