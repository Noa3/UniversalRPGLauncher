using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Checks the native player turn against the main-versus-parallel interpreter rule.</summary>
public partial class TestRm2kPlayerInputIsolation : TestBase
{
    public void Test_TimedAutorunCannotTriggerAnActionPage()
    {
        var state = NewState();
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { WaitingPage(Rm2kEventTrigger.AutoStart), ActionPage() });
        scheduler.ExecuteFrame();
        var turn = new Rm2kPlayerTurn(state, scheduler);

        AssertEq(scheduler.ActiveInterpreterCount, 1, "The timed autorun has entered its wait");
        AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "A running main page suppresses the action-event turn");
        AssertEq(scheduler.ActiveInterpreterCount, 1, "The action page was not started beside the main page");
        AssertFalse(turn.Apply(Rm2kInputAction.MoveRight), "The same main page blocks manual movement");
        AssertEq(state.MapX, 2, "Blocked input does not move the player");
        AssertFalse(turn.Apply(Rm2kInputAction.Menu), "The same main page blocks the menu request");
        AssertFalse(state.IsMainMenuActive, "No main menu opened during the wait");
    }

    public void Test_ParallelPageDoesNotBlockOrdinaryPlayerInput()
    {
        var state = NewState();
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { WaitingPage(Rm2kEventTrigger.Parallel), ActionPage() });
        scheduler.ExecuteFrame();
        var turn = new Rm2kPlayerTurn(state, scheduler);

        turn.Apply(Rm2kInputAction.MoveRight);
        AssertEq(state.MapX, 3, "A background parallel wait does not freeze manual movement");
        AssertTrue(turn.Apply(Rm2kInputAction.Menu), "A background parallel wait does not suppress the menu");
        AssertTrue(state.IsMainMenuActive, "The menu request reaches the shared simulation state");
        state.IsMainMenuActive = false;
        state.WaitingFor = GameSimulationState.WaitReason.None;
        state.MapX = 2;
        state.FacingDirection = 2;
        AssertTrue(turn.Apply(Rm2kInputAction.Confirm), "An action event remains available beside a parallel wait");
        AssertEq(scheduler.ActiveInterpreterCount, 2, "The background and action pages run independently");
    }

    public void Test_MessageActivityBlocksMapInputDuringParallelPages()
    {
        foreach (var visibleWindow in new[] { true, false })
        {
            var state = NewState();
            var presentation = new PresentationState();
            var scheduler = new Rm2kEventScheduler(state, presentation);
            scheduler.SetEvents(new[] { WaitingPage(Rm2kEventTrigger.Parallel), ActionPage() });
            scheduler.ExecuteFrame();
            if (visibleWindow) { presentation.ShowMessage("A background message"); }
            else { state.WaitingFor = GameSimulationState.WaitReason.MessageOpen; }
            var turn = new Rm2kPlayerTurn(state, scheduler);

            turn.Apply(Rm2kInputAction.MoveRight);
            AssertEq(state.MapX, 2, "Either message representation blocks map movement");
            state.MapX = 2;
            state.FacingDirection = 2;
            AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "Reading a message cannot start an action page");
            AssertEq(scheduler.ActiveInterpreterCount, 1, "The message key did not leak into a new page");
            AssertFalse(turn.Apply(Rm2kInputAction.Menu), "An active message suppresses a menu request");
        }
    }

    public void Test_VehicleTurnsRespectMainAndParallelRoles()
    {
        foreach (var trigger in new[] { Rm2kEventTrigger.AutoStart, Rm2kEventTrigger.Parallel })
        {
            var state = NewState();
            state.Vehicles.Add(new Rm2kVehicleState(Rm2kVehicle.Airship, state.MapId, 2, 2));
            var scheduler = new Rm2kEventScheduler(state);
            scheduler.SetEvents(new[] { WaitingPage(trigger), ActionPage() });
            scheduler.ExecuteFrame();
            var turn = new Rm2kPlayerTurn(state, scheduler);
            var allowed = trigger == Rm2kEventTrigger.Parallel;

            AssertEq(turn.Apply(Rm2kInputAction.Confirm), allowed, "Only background work permits the vehicle turn");
            AssertEq(state.Boarding?.IsAboard == true, allowed, "The airship is boarded only when input is available");
            AssertEq(scheduler.ActiveInterpreterCount, 1, "Neither the blocked nor the vehicle-handled key starts an action page");
        }
    }

    public void Test_ParallelDecisionWaitAlsoLeavesTheActionTurnAvailable()
    {
        var state = NewState();
        state.SupportsRpg2k3Commands = true;
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { WaitingPage(Rm2kEventTrigger.Parallel, decision: true), ActionPage() });
        scheduler.ExecuteFrame();
        var turn = new Rm2kPlayerTurn(state, scheduler);

        AssertTrue(scheduler.HasDecisionWait, "The parallel page awaits a fresh decision");
        AssertTrue(turn.Apply(Rm2kInputAction.Confirm), "The same global decision reaches both consumers");
        AssertEq(scheduler.ActiveInterpreterCount, 2, "The action page starts while the parallel page resumes next frame");
        AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "The newly running main action page prevents a duplicate action turn");
        scheduler.ExecuteFrame();
        AssertFalse(scheduler.HasDecisionWait, "The parallel decision resumed and ended");
        AssertFalse(scheduler.HasBlockingInterpreter, "The action page ended without a stale blocking flag");
        turn.Apply(Rm2kInputAction.MoveRight);
        AssertEq(state.MapX, 3, "Input becomes available as soon as the main page finishes");
    }

    public void Test_MainDecisionCannotBoardAVehicleOrStartAnAction()
    {
        var state = NewState();
        state.SupportsRpg2k3Commands = true;
        state.Vehicles.Add(new Rm2kVehicleState(Rm2kVehicle.Airship, state.MapId, 2, 2));
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { WaitingPage(Rm2kEventTrigger.AutoStart, decision: true), ActionPage() });
        scheduler.ExecuteFrame();
        var turn = new Rm2kPlayerTurn(state, scheduler);

        AssertTrue(turn.Apply(Rm2kInputAction.Confirm), "The first key releases only the main wait");
        AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "A duplicate key remains consumed by the main page");
        AssertTrue(state.Boarding == null, "Neither key touches boarding state");
        AssertEq(scheduler.ActiveInterpreterCount, 1, "Neither key starts an action page");
    }

    public void Test_ParallelDecisionsDoNotUnlockATimedMainPage()
    {
        var state = NewState();
        state.SupportsRpg2k3Commands = true;
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[]
        {
            WaitingPage(Rm2kEventTrigger.AutoStart),
            WaitingPage(Rm2kEventTrigger.Parallel, decision: true, eventId: 902),
            ActionPage()
        });
        scheduler.ExecuteFrame();
        var turn = new Rm2kPlayerTurn(state, scheduler);

        AssertTrue(turn.Apply(Rm2kInputAction.Confirm), "The parallel wait observes the decision beside a busy main page");
        AssertEq(scheduler.ActiveInterpreterCount, 2, "The action page was not triggered");
        scheduler.ExecuteFrame();
        AssertEq(scheduler.ActiveInterpreterCount, 1, "The parallel page ends while the timed main page stays active");
        AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "The main wait still blocks action input");
        turn.Apply(Rm2kInputAction.MoveRight);
        AssertEq(state.MapX, 2, "Resuming parallel work cannot unlock main-page movement");
    }

    public void Test_ClearAndMapReplacementDropBlockingInterpreters()
    {
        foreach (var clear in new[] { true, false })
        {
            var state = NewState();
            var scheduler = new Rm2kEventScheduler(state);
            scheduler.SetEvents(new[] { WaitingPage(Rm2kEventTrigger.AutoStart) });
            scheduler.ExecuteFrame();
            AssertTrue(scheduler.HasBlockingInterpreter, "The old map has a blocking page");
            if (clear) { scheduler.Clear(); }
            else { scheduler.SetEvents(new[] { ActionPage() }); }
            AssertFalse(scheduler.HasBlockingInterpreter, "Removing the old interpreters drops their blocking role");
            var turn = new Rm2kPlayerTurn(state, scheduler);
            turn.Apply(Rm2kInputAction.MoveRight);
            AssertEq(state.MapX, 3, "The old map no longer freezes the player");
        }
    }

    public void Test_NestedCallsKeepTheOriginalExecutionRole()
    {
        foreach (var trigger in new[] { Rm2kEventTrigger.AutoStart, Rm2kEventTrigger.Parallel })
        {
            var state = NewState();
            var calledPage = new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.Action };
            calledPage.Commands.Add(SetSwitch(7));
            calledPage.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 10, 0 }));
            calledPage.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
            var called = new Rm2kMap.Event(902, 0, 0);
            called.Pages.Add(calledPage);
            var rootPage = new Rm2kMap.EventPage { Trigger = (int)trigger };
            rootPage.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.CallEvent,
                new List<int> { EventInterpreter.CallTargetMapEvent, 902, 1 }));
            rootPage.Commands.Add(SetSwitch(8));
            rootPage.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
            var root = new Rm2kMap.Event(900, 0, 0);
            root.Pages.Add(rootPage);
            var scheduler = new Rm2kEventScheduler(state);
            scheduler.SetEvents(new[] { root, called });

            scheduler.ExecuteFrame(); // Enter the called page.
            scheduler.ExecuteFrame(); // Observe its marker.
            scheduler.ExecuteFrame(); // Enter its timed wait.
            AssertTrue(Switch(state, 7), "The called page really executed, rather than merely being registered");
            AssertFalse(Switch(state, 8), "The caller has not resumed while its child waits");
            AssertEq(scheduler.ActiveInterpreterCount, 1, "A nested call belongs to its original execution instance");
            var parallel = trigger == Rm2kEventTrigger.Parallel;
            AssertEq(scheduler.HasBlockingInterpreter, !parallel, "The child retains the caller's execution role");
            new Rm2kPlayerTurn(state, scheduler).Apply(Rm2kInputAction.MoveRight);
            AssertEq(state.MapX, parallel ? 3 : 2, "Calling an action page does not turn a parallel interpreter into a main one");
            for (var i = 0; i < 70; i++) { scheduler.ExecuteFrame(); }
            AssertTrue(Switch(state, 8), "The nested timed wait completes and returns to its original caller");
        }
    }

    public void Test_OneDecisionResumesMainAndParallelWaitsTogether()
    {
        var state = NewState();
        state.SupportsRpg2k3Commands = true;
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[]
        {
            WaitingPage(Rm2kEventTrigger.AutoStart, decision: true),
            WaitingPage(Rm2kEventTrigger.Parallel, decision: true, eventId: 902)
        });
        scheduler.ExecuteFrame();
        scheduler.ExecuteFrame();
        AssertEq(scheduler.ActiveInterpreterCount, 2, "Both roles remain waiting before a fresh decision");
        var turn = new Rm2kPlayerTurn(state, scheduler);
        AssertTrue(turn.Apply(Rm2kInputAction.Confirm), "One fresh decision is broadcast to both roles");
        AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "Neither wait accepts a duplicate decision");
        scheduler.ExecuteFrame();
        AssertEq(scheduler.ActiveInterpreterCount, 0, "Both continuations reach End on the same frame");
        AssertFalse(scheduler.HasDecisionWait, "No parallel decision wait was left behind");
        AssertFalse(scheduler.HasBlockingInterpreter, "No main decision wait was left behind");
    }

    public void Test_MessageReleaseRestoresOrdinaryPlayerInput()
    {
        foreach (var visibleWindow in new[] { true, false })
        {
            var state = NewState();
            var presentation = new PresentationState();
            var scheduler = new Rm2kEventScheduler(state, presentation);
            scheduler.SetEvents(new[] { WaitingPage(Rm2kEventTrigger.Parallel) });
            scheduler.ExecuteFrame();
            if (visibleWindow) { presentation.ShowMessage("A background message"); }
            else { state.WaitingFor = GameSimulationState.WaitReason.MessageOpen; }
            var turn = new Rm2kPlayerTurn(state, scheduler);
            turn.Apply(Rm2kInputAction.MoveRight);
            AssertEq(state.MapX, 2, "Input stays blocked while the message is active");
            presentation.DismissMessage();
            if (!visibleWindow)
            {
                turn.Apply(Rm2kInputAction.MoveRight);
                AssertEq(state.MapX, 2, "Hiding a window does not clear an authoritative message wait");
                state.WaitingFor = GameSimulationState.WaitReason.None;
            }
            turn.Apply(Rm2kInputAction.MoveRight);
            AssertEq(state.MapX, 3, "Once the actual message state clears, background pages no longer block input");
            AssertFalse(scheduler.IsMessageActive, "The message gate reflects the completed release");
        }
    }

    private static Rm2kMap.EventCommand SetSwitch(int id) => new(EventInterpreter.ControlSwitches,
        new List<int> { EventInterpreter.TargetEvalSingle, id, id, EventInterpreter.SwitchModeOn });

    private static bool Switch(GameSimulationState state, int id) => state.Switches.Count >= id && state.Switches[id - 1];

    private static GameSimulationState NewState()
    {
        var state = new GameSimulationState();
        state.ConfigureMap(1, 5, 5, Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 25));
        state.MapX = 2;
        state.MapY = 2;
        state.FacingDirection = 2;
        return state;
    }

    private static Rm2kMap.Event WaitingPage(Rm2kEventTrigger trigger, bool decision = false, int eventId = 900)
    {
        var page = new Rm2kMap.EventPage { Trigger = (int)trigger };
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 10, decision ? 1 : 0 }));
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        var mapEvent = new Rm2kMap.Event(eventId, 0, 0);
        mapEvent.Pages.Add(page);
        return mapEvent;
    }

    private static Rm2kMap.Event ActionPage()
    {
        var page = new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Action,
            Layer = Rm2kEventScheduler.LayerSame
        };
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        var mapEvent = new Rm2kMap.Event(901, 2, 3);
        mapEvent.Pages.Add(page);
        return mapEvent;
    }
}
