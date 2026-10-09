using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Drives decision-key Wait through the real native host, input and scheduler.
/// Command lists are reference-derived; game assets come from pinned test games.
/// </summary>
public partial class TestRm2003DecisionWait : TestBase
{
    public void Test_FreshConfirmResumesButTheStartingKeyDoesNotLeak()
    {
        WithRuntime(true, (host, runtime) =>
        {
            Install(runtime, 0);
            runtime.SubmitInput(Rm2kInputAction.Confirm);
            Tick(host);
            for (var i = 0; i < 10; i++) { Tick(host); }
            AssertFalse(Switch(runtime, 7), "The pre-command key is not remembered as a future decision");
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm), "A fresh decision reaches the waiting interpreter");
            AssertFalse(Switch(runtime, 7), "Submitting a key is not a simulation frame");
            Tick(host);
            AssertTrue(Switch(runtime, 7), "The next frame resumes the waiting page");
        });
    }

    public void Test_DirectionsCancelAndPausedInputDoNotResume()
    {
        WithRuntime(true, (host, runtime) =>
        {
            Install(runtime, 0);
            Tick(host);
            AssertFalse(runtime.SubmitInput(Rm2kInputAction.MoveDown), "Waiting event blocks player movement");
            runtime.SubmitInput(Rm2kInputAction.Menu);
            runtime.Simulation.IsPaused = true;
            AssertFalse(runtime.SubmitInput(Rm2kInputAction.Confirm), "Paused input cannot release the decision wait");
            runtime.Simulation.IsPaused = false;
            for (var i = 0; i < 5; i++) { Tick(host); }
            AssertFalse(Switch(runtime, 7), "Unrelated and paused keys were not queued");
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm), "Fresh unpaused decision is accepted");
            Tick(host);
            AssertTrue(Switch(runtime, 7), "Page resumes only after the accepted decision");
        });
    }

    public void Test_OpenMessageBlocksSetupAndRelease()
    {
        WithRuntime(true, (host, runtime) =>
        {
            Install(runtime, 0);
            runtime.Presentation.ShowMessage("A message before the wait");
            Tick(host);
            runtime.SubmitInput(Rm2kInputAction.Confirm);
            Tick(host);
            AssertFalse(Switch(runtime, 7), "An active message prevents decision-wait setup");
            runtime.Presentation.DismissMessage();
            Tick(host);
            for (var i = 0; i < 3; i++) { Tick(host); }
            AssertFalse(Switch(runtime, 7), "The message key cannot be reused after wait setup");
            runtime.Presentation.ShowMessage("Another message during the wait");
            AssertFalse(runtime.SubmitInput(Rm2kInputAction.Confirm), "An open message also blocks decision-wait release");
            runtime.Presentation.DismissMessage();
            Tick(host);
            AssertFalse(Switch(runtime, 7), "Dismissal alone does not release the decision wait");
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm), "A separate new decision releases it");
            Tick(host);
            AssertTrue(Switch(runtime, 7), "The waiting page continues");
        });
    }

    public void Test_Rm2000UsesTimeEvenWithASecondParameter()
    {
        WithRuntime(false, (host, runtime) =>
        {
            Install(runtime, 5);
            Tick(host);
            runtime.SubmitInput(Rm2kInputAction.Confirm);
            for (var i = 0; i < 30; i++) { Tick(host); }
            AssertFalse(Switch(runtime, 7), "RM2000 still waits the full five tenths");
            Tick(host);
            AssertTrue(Switch(runtime, 7), "RM2000 then proceeds without any new key");
        });
    }

    public void Test_OneDecisionCannotReleaseTwoSequentialWaits()
    {
        WithRuntime(true, (host, runtime) =>
        {
            Install(runtime, 0, secondWait: true);
            Tick(host);
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm), "First decision is accepted");
            runtime.SubmitInput(Rm2kInputAction.Confirm);
            Tick(host);
            AssertTrue(Switch(runtime, 7), "First continuation runs");
            Tick(host);
            for (var i = 0; i < 5; i++) { Tick(host); }
            AssertFalse(Switch(runtime, 8), "No decision is carried over into the next wait");
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm), "Second wait requires a separate decision");
            Tick(host);
            AssertTrue(Switch(runtime, 8), "Second continuation runs after its own key");
        });
    }

    public void Test_OneFreshDecisionResumesAllCurrentlyWaitingParallelPages()
    {
        WithRuntime(true, (host, runtime) =>
        {
            var events = new List<Rm2kMap.Event>();
            foreach (var id in new[] { 7, 8 })
            {
                var page = new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.Parallel };
                page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 0, 1 }));
                page.Commands.Add(SetSwitch(id));
                page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
                var eventData = new Rm2kMap.Event(900 + id, 0, 0);
                eventData.Pages.Add(page);
                events.Add(eventData);
            }
            runtime.EventScheduler.SetEvents(events);
            Tick(host);
            AssertFalse(Switch(runtime, 7) || Switch(runtime, 8), "Both parallel pages wait without blocking each other's setup");
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm), "One fresh decision reaches the active waits");
            Tick(host);
            AssertTrue(Switch(runtime, 7) && Switch(runtime, 8), "All waits observing that fresh decision resume");
        });
    }

    private static void Install(Rm2kEngineRuntime runtime, int tenths, bool secondWait = false)
    {
        var page = new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.AutoStart };
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { tenths, 1 }));
        page.Commands.Add(SetSwitch(7));
        if (secondWait)
        {
            page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { tenths, 1 }));
            page.Commands.Add(SetSwitch(8));
        }
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        var eventData = new Rm2kMap.Event(900, 0, 0);
        eventData.Pages.Add(page);
        runtime.EventScheduler.SetEvents(new[] { eventData });
    }

    private static Rm2kMap.EventCommand SetSwitch(int id) => new(EventInterpreter.ControlSwitches,
        new List<int> { EventInterpreter.TargetEvalSingle, id, id, EventInterpreter.SwitchModeOn });

    private static bool Switch(Rm2kEngineRuntime runtime, int id) =>
        runtime.Simulation.Switches.Count >= id && runtime.Simulation.Switches[id - 1];

    private void Tick(EnginePluginHost host)
    {
        var result = host.Update(1.0 / 60.0);
        AssertTrue(result.Success, $"Runtime frame succeeds: {result.Error?.Message}");
    }

    private void WithRuntime(bool rm2003, Action<EnginePluginHost, Rm2kEngineRuntime> test)
    {
        var engine = rm2003 ? EnginePluginIds.RpgMaker2003 : EnginePluginIds.RpgMaker2000;
        var directory = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/" + (rm2003 ? "rm2003" : "rm2000"));
        using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var result = host.Start(new PluginGameInfo
        {
            GameDirectory = directory,
            EngineId = engine,
            Generation = rm2003 ? "rm2k3" : "rm2k",
            DetectorScore = 3
        });
        AssertTrue(result.Success, $"Pinned runtime fixture starts: {result.Error?.Message}");
        if (result.Success && host.Runtime is Rm2kEngineRuntime runtime)
        {
            AssertEq(runtime.Simulation.SupportsRpg2k3Commands, rm2003, "Selected engine determines the basic command set");
            AssertFalse(runtime.Simulation.SupportsRpg2k3ECommands, "RM2003 selection does not invent 2003E extension support");
            test(host, runtime);
        }
    }
}
