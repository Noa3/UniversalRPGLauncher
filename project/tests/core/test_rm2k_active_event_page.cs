using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Only the highest condition-matching page owns an event's activation rules.</summary>
public partial class TestRm2kActiveEventPage : TestBase
{
    public void Test_HigherEligiblePageShadowsEveryOtherTriggerKind()
    {
        foreach (var lowerTrigger in Enum.GetValues<Rm2kEventTrigger>())
        {
            var state = NewState();
            var higherTrigger = lowerTrigger == Rm2kEventTrigger.Action
                ? Rm2kEventTrigger.Parallel : Rm2kEventTrigger.Action;
            var mapEvent = new Rm2kMap.Event(7, 2, 3);
            var lower = Page(lowerTrigger, Rm2kEventScheduler.LayerSame, 7);
            var higher = Page(higherTrigger, Rm2kEventScheduler.LayerSame, 8);
            mapEvent.Pages.Add(lower);
            mapEvent.Pages.Add(higher);

            AssertTrue(Rm2kEventPageSelector.Select(mapEvent, state, lowerTrigger) == null,
                $"The eligible {higherTrigger} page hides the lower {lowerTrigger} page");
            AssertTrue(ReferenceEquals(Rm2kEventPageSelector.Select(mapEvent, state, higherTrigger), higher),
                "The highest eligible page is the sole activation candidate");
        }
    }

    public void Test_ActionLayerComesOnlyFromTheActivePage()
    {
        foreach (var higherIsSameLayer in new[] { false, true })
        {
            var state = NewState();
            var mapEvent = new Rm2kMap.Event(7, 2, higherIsSameLayer ? 2 : 3);
            mapEvent.Pages.Add(Page(Rm2kEventTrigger.Action,
                higherIsSameLayer ? Rm2kEventScheduler.LayerBelow : Rm2kEventScheduler.LayerSame, 7));
            mapEvent.Pages.Add(Page(Rm2kEventTrigger.Action,
                higherIsSameLayer ? Rm2kEventScheduler.LayerSame : Rm2kEventScheduler.LayerBelow, 8));
            var scheduler = new Rm2kEventScheduler(state);
            scheduler.SetEvents(new[] { mapEvent });
            var turn = new Rm2kPlayerTurn(state, scheduler);

            AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "A hidden page's layer cannot make the active page reachable");
            AssertEq(scheduler.ActiveInterpreterCount, 0, "The wrong-side action did not start any page");
            state.MapY = higherIsSameLayer ? 1 : 3;
            AssertTrue(turn.Apply(Rm2kInputAction.Confirm), "The active page is reachable from its own correct layer rule");
            scheduler.ExecuteFrame();
            AssertTrue(Switch(state, 8), "Only the highest active page's command executes");
            AssertFalse(Switch(state, 7), "The hidden page's command never executes");
        }
    }

    public void Test_LiveConditionsChangeWhichPageOwnsTheTrigger()
    {
        var state = NewState();
        state.Switches.Add(false);
        var mapEvent = new Rm2kMap.Event(7, 2, 3);
        var lower = Page(Rm2kEventTrigger.Action, Rm2kEventScheduler.LayerSame, 7);
        var higher = Page(Rm2kEventTrigger.Parallel, Rm2kEventScheduler.LayerSame, 8);
        higher.Conditions["switch_a_enabled"] = true;
        higher.Conditions["switch_a_id"] = 1;
        mapEvent.Pages.Add(lower);
        mapEvent.Pages.Add(higher);

        AssertTrue(ReferenceEquals(Rm2kEventPageSelector.Select(mapEvent, state, Rm2kEventTrigger.Action), lower),
            "An unsatisfied higher page leaves the lower eligible page active");
        state.Switches[0] = true;
        AssertTrue(Rm2kEventPageSelector.Select(mapEvent, state, Rm2kEventTrigger.Action) == null,
            "Enabling the higher page immediately hides the lower trigger");
        AssertTrue(ReferenceEquals(Rm2kEventPageSelector.Select(mapEvent, state, Rm2kEventTrigger.Parallel), higher),
            "The newly eligible higher page owns the trigger");
        state.Switches[0] = false;
        AssertTrue(ReferenceEquals(Rm2kEventPageSelector.Select(mapEvent, state, Rm2kEventTrigger.Action), lower),
            "Disabling the condition restores the correct fallback");
    }

    public void Test_HigherActionPageSuppressesLowerAutomaticPages()
    {
        foreach (var lowerTrigger in new[] { Rm2kEventTrigger.AutoStart, Rm2kEventTrigger.Parallel })
        {
            var state = NewState();
            var mapEvent = new Rm2kMap.Event(7, 2, 3);
            mapEvent.Pages.Add(Page(lowerTrigger, Rm2kEventScheduler.LayerSame, 7));
            mapEvent.Pages.Add(Page(Rm2kEventTrigger.Action, Rm2kEventScheduler.LayerSame, 8));
            var scheduler = new Rm2kEventScheduler(state);
            scheduler.SetEvents(new[] { mapEvent });
            scheduler.ExecuteFrame();
            AssertEq(scheduler.ActiveInterpreterCount, 0, "A hidden automatic page never starts");
            AssertFalse(Switch(state, 7), "The hidden automatic page has no effect");
            AssertTrue(new Rm2kPlayerTurn(state, scheduler).Apply(Rm2kInputAction.Confirm),
                "The active action page remains available to the player");
            scheduler.ExecuteFrame();
            AssertTrue(Switch(state, 8), "The active action page executes its own command");
            AssertFalse(Switch(state, 7), "Starting the active page does not execute the hidden one");
        }
    }

    public void Test_OnlyTheHighestAutomaticPageRunsWithItsOwnRole()
    {
        foreach (var higherTrigger in new[] { Rm2kEventTrigger.AutoStart, Rm2kEventTrigger.Parallel })
        {
            var state = NewState();
            var lowerTrigger = higherTrigger == Rm2kEventTrigger.AutoStart
                ? Rm2kEventTrigger.Parallel : Rm2kEventTrigger.AutoStart;
            var mapEvent = new Rm2kMap.Event(7, 0, 0);
            mapEvent.Pages.Add(Page(lowerTrigger, Rm2kEventScheduler.LayerSame, 7));
            mapEvent.Pages.Add(Page(higherTrigger, Rm2kEventScheduler.LayerSame, 8));
            var scheduler = new Rm2kEventScheduler(state);
            scheduler.SetEvents(new[] { mapEvent });
            scheduler.ExecuteFrame();
            AssertTrue(Switch(state, 8), "The highest automatic page is the one that actually executes");
            AssertFalse(Switch(state, 7), "The lower automatic page does not win through scheduler scan order");
            AssertEq(scheduler.ActiveInterpreterCount, 1, "One event has only one current execution");
            AssertEq(scheduler.HasBlockingInterpreter, higherTrigger == Rm2kEventTrigger.AutoStart,
                "The current page also determines its main-versus-parallel execution role");
        }
    }

    public void Test_TouchLayerComesOnlyFromTheActivePage()
    {
        foreach (var trigger in new[] { Rm2kEventTrigger.Touched, Rm2kEventTrigger.Collision })
        {
            foreach (var higherIsSameLayer in new[] { false, true })
            {
                var state = NewState();
                var mapEvent = new Rm2kMap.Event(7, 2, higherIsSameLayer ? 2 : 3);
                mapEvent.Pages.Add(Page(trigger,
                    higherIsSameLayer ? Rm2kEventScheduler.LayerBelow : Rm2kEventScheduler.LayerSame, 7));
                mapEvent.Pages.Add(Page(trigger,
                    higherIsSameLayer ? Rm2kEventScheduler.LayerSame : Rm2kEventScheduler.LayerBelow, 8));
                var scheduler = new Rm2kEventScheduler(state);
                scheduler.SetEvents(new[] { mapEvent });
                AssertFalse(higherIsSameLayer ? scheduler.TriggerTouchOrCollisionHere() : scheduler.TriggerTouchOrCollisionFacing(),
                    "A hidden touch page's layer cannot authorize the active page from the wrong side");
                AssertEq(scheduler.ActiveInterpreterCount, 0, "No wrong-side touch interpreter started");
                state.MapY = higherIsSameLayer ? 1 : 3;
                AssertTrue(higherIsSameLayer ? scheduler.TriggerTouchOrCollisionFacing() : scheduler.TriggerTouchOrCollisionHere(),
                    "The active touch page can start from its actual layer rule");
                scheduler.ExecuteFrame();
                AssertTrue(Switch(state, 8), "The active touch page runs");
                AssertFalse(Switch(state, 7), "The hidden touch page stays inactive");
            }
        }
    }

    public void Test_SameTriggerStillSelectsTheHighestEligiblePage()
    {
        foreach (var trigger in Enum.GetValues<Rm2kEventTrigger>())
        {
            var mapEvent = new Rm2kMap.Event(7, 2, 3);
            mapEvent.Pages.Add(Page(trigger, Rm2kEventScheduler.LayerSame, 7));
            var higher = Page(trigger, Rm2kEventScheduler.LayerSame, 8);
            mapEvent.Pages.Add(higher);
            AssertTrue(ReferenceEquals(Rm2kEventPageSelector.Select(mapEvent, NewState(), trigger), higher),
                "Matching the trigger does not change highest-first page priority");
        }
    }

    public void Test_PinnedNativeHostsDoNotExecuteShadowedAutoruns()
    {
        // The host/assets are real pinned fixtures; this command list is reference-derived.
        foreach (var rm2003 in new[] { false, true })
        {
            using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
            var result = host.Start(new PluginGameInfo
            {
                GameDirectory = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/" + (rm2003 ? "rm2003" : "rm2000")),
                EngineId = rm2003 ? EnginePluginIds.RpgMaker2003 : EnginePluginIds.RpgMaker2000,
                Generation = rm2003 ? "rm2k3" : "rm2k",
                DetectorScore = 3
            });
            AssertTrue(result.Success, $"Pinned native host starts: {result.Error?.Message}");
            if (!result.Success || host.Runtime is not Rm2kEngineRuntime runtime) { continue; }
            runtime.Simulation.Switches.Clear();
            runtime.Simulation.Vehicles.Clear();
            runtime.Simulation.MapX = 2;
            runtime.Simulation.MapY = 2;
            runtime.Simulation.FacingDirection = 2;
            var mapEvent = new Rm2kMap.Event(7, 2, 3);
            mapEvent.Pages.Add(Page(Rm2kEventTrigger.AutoStart, Rm2kEventScheduler.LayerSame, 7));
            mapEvent.Pages.Add(Page(Rm2kEventTrigger.Action, Rm2kEventScheduler.LayerSame, 8));
            runtime.EventScheduler.SetEvents(new[] { mapEvent });

            AssertTrue(host.Update(1.0 / 60.0).Success, "The real runtime advances without starting the hidden autorun");
            AssertFalse(Switch(runtime.Simulation, 7), "The imported native runtime does not execute the shadowed page");
            AssertEq(runtime.EventScheduler.ActiveInterpreterCount, 0, "The hidden autorun does not hold the player");
            AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm), "A real host decision starts the active action page");
            AssertTrue(host.Update(1.0 / 60.0).Success, "The real host runs the active page");
            AssertTrue(Switch(runtime.Simulation, 8), "The host applies the active page's command");
            AssertFalse(Switch(runtime.Simulation, 7), "The hidden page remains unexecuted");
        }
    }

    private static GameSimulationState NewState()
    {
        var state = new GameSimulationState();
        state.ConfigureMap(1, 5, 5, Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 25));
        state.MapX = 2;
        state.MapY = 2;
        state.FacingDirection = 2;
        return state;
    }

    private static Rm2kMap.EventPage Page(Rm2kEventTrigger trigger, int layer, int switchId)
    {
        var page = new Rm2kMap.EventPage { Trigger = (int)trigger, Layer = layer };
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches,
            new List<int> { EventInterpreter.TargetEvalSingle, switchId, switchId, EventInterpreter.SwitchModeOn }));
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        return page;
    }

    private static bool Switch(GameSimulationState state, int id) => state.Switches.Count >= id && state.Switches[id - 1];
}
