using System.Collections.Generic;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRm2kCollisionTrigger : TestBase
{
    public void Test_CollisionTriggerStartsOnlyCollisionPageAndLocksInput()
    {
        var state = new GameSimulationState();
        var scheduler = new Rm2kEventScheduler(state);
        var eventData = EventWithPage(
            10,
            Rm2kEventTrigger.Collision,
            new Rm2kMap.EventCommand(
                EventInterpreter.ControlSwitches,
                new List<int> { 4, 4, 0, EventInterpreter.SwitchModeOn }),
            new Rm2kMap.EventCommand(EventInterpreter.End));
        scheduler.SetEvents(new[] { eventData });

        AssertFalse(scheduler.TriggerAt(2, 3, Rm2kEventTrigger.Action));
        AssertFalse(scheduler.TriggerAt(2, 3, Rm2kEventTrigger.Touch));
        AssertTrue(scheduler.TriggerCollisionAt(2, 3));
        AssertTrue(scheduler.ForegroundBusy);
        AssertTrue(state.PlayerInputLocked);

        scheduler.ExecuteFrame();

        AssertTrue(state.Switches.Count >= 4 && state.Switches[3]);
    }

    public void Test_CollisionTriggerIsSerializedWithOtherForegroundEvents()
    {
        var state = new GameSimulationState();
        var scheduler = new Rm2kEventScheduler(state);
        var collision = EventWithPage(
            10,
            Rm2kEventTrigger.Collision,
            new Rm2kMap.EventCommand(EventInterpreter.End));
        var action = EventWithPage(
            11,
            Rm2kEventTrigger.Action,
            new Rm2kMap.EventCommand(EventInterpreter.End));
        action.X = 4;
        action.Y = 3;
        scheduler.SetEvents(new[] { collision, action });

        AssertTrue(scheduler.TriggerAction(11));
        AssertFalse(scheduler.TriggerCollision(10),
            "collision event must wait while another foreground interpreter owns execution");

        scheduler.ExecuteFrame();
        AssertFalse(scheduler.ForegroundBusy);
        AssertTrue(scheduler.TriggerCollision(10));
    }

    private static Rm2kMap.Event EventWithPage(
        int pId,
        Rm2kEventTrigger pTrigger,
        params Rm2kMap.EventCommand[] pCommands)
    {
        var eventData = new Rm2kMap.Event(pId, 2, 3);
        var page = new Rm2kMap.EventPage
        {
            Trigger = (int)pTrigger,
            Layer = 1,
        };
        foreach (var command in pCommands) page.Commands.Add(command);
        eventData.Pages.Add(page);
        return eventData;
    }
}
