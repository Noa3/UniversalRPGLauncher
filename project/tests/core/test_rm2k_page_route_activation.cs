using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Rm2k.Interpreter;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kActiveEventGraphic
{
    private static byte[] AuthoredRoute(bool repeat, params Rm2kMap.MoveCommand[] commands)
    {
        var vector = new List<byte>();
        foreach (var command in commands)
        {
            vector.AddRange(TestRm2kParser.Ber(command.CommandId));
            if (command.CommandId is 34 or 35)
            {
                var text = Encoding.ASCII.GetBytes(command.ParameterString);
                vector.AddRange(TestRm2kParser.Ber(text.Length));
                vector.AddRange(text);
            }
            if (command.CommandId is 32 or 33 or 34 or 35) vector.AddRange(TestRm2kParser.Ber(command.ParameterA));
            if (command.CommandId == 35)
            {
                vector.AddRange(TestRm2kParser.Ber(command.ParameterB));
                vector.AddRange(TestRm2kParser.Ber(command.ParameterC));
            }
        }
        return TestRm2kParser.Struct(
            TestRm2kParser.Chunk(0x0B, TestRm2kParser.Ber(vector.Count)),
            TestRm2kParser.Chunk(0x0C, vector.ToArray()),
            TestRm2kParser.Chunk(0x15, TestRm2kParser.Ber(repeat ? 1 : 0)));
    }

    private static byte[] RoutePage(int? moveType, int frequency, bool repeat, params Rm2kMap.MoveCommand[] commands)
    {
        var chunks = new List<byte[]>
        {
            TestRm2kParser.Chunk(0x15, Encoding.ASCII.GetBytes("Chara1")),
            TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x20, TestRm2kParser.Ber(frequency)),
            TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(2)),
            TestRm2kParser.Chunk(0x25, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x29, AuthoredRoute(repeat, commands))
        };
        if (moveType.HasValue) chunks.Add(TestRm2kParser.Chunk(0x1F, TestRm2kParser.Ber(moveType.Value)));
        return TestRm2kParser.Struct(chunks.ToArray());
    }

    private void StartAuthoredRouteMap(params byte[][] pages)
    {
        _host.Dispose();
        File.WriteAllBytes(Path.Combine(_root, "Map0001.lmu"), TestRm2kEventPagePoseParser.MapWithPages(pages));
        _host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = _host.Start(new PluginGameInfo
        {
            GameDirectory = _root, EngineId = EnginePluginIds.RpgMaker2000, Generation = "rm2k", DetectorScore = 3
        });
        AssertTrue(started.Success, started.Error?.Message ?? "The authored map starts");
        _runtime = (Rm2kEngineRuntime)_host.Runtime!;
        _event = ((List<Rm2kMap.Event>)typeof(Rm2kEngineRuntime)
            .GetField("_mapEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!).Single();
        for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
            _runtime.Simulation.PassabilityMasks[index] = 15;
    }

    private Dictionary<int, Rm2kMoveRouteState> PageRoutes() =>
        (Dictionary<int, Rm2kMoveRouteState>)typeof(Rm2kEngineRuntime)
            .GetField("_eventPageRoutes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!;

    private Dictionary<int, Rm2kMoveRouteState> ForcedRoutes() =>
        (Dictionary<int, Rm2kMoveRouteState>)typeof(Rm2kEngineRuntime)
            .GetField("_eventRoutes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!;

    private void RouteTicks(int count)
    {
        for (var tick = 0; tick < count; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
    }

    private void HigherRouteRequiresSwitch()
    {
        _event.Pages[1].Conditions["switch_a_enabled"] = true;
        _event.Pages[1].Conditions["switch_a_id"] = 1;
        while (_runtime.Simulation.Switches.Count < 1) _runtime.Simulation.Switches.Add(false);
        _runtime.Simulation.Switches[0] = false;
    }

    public void Test_CustomRouteCannotLeaveATileWithABlockedExit()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = 0;
        RouteTicks(1);
        AssertEq(_event.X, 8, "The source exit, not only the destination, must permit movement");
        AssertEq(_runtime.EventRemainingStepForTest(1), -1);
    }

    public void Test_CustomRouteChecksTheCorrectSourceAndDestinationEdgeBits()
    {
        foreach (var (command, source, destination, dx, dy) in new[]
        {
            (Rm2kMoveRoute.MoveUp, 8, 1, 0, -1), (Rm2kMoveRoute.MoveRight, 4, 2, 1, 0),
            (Rm2kMoveRoute.MoveDown, 1, 8, 0, 1), (Rm2kMoveRoute.MoveLeft, 2, 4, -1, 0)
        })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(command)));
            _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = (byte)source;
            _runtime.Simulation.PassabilityMasks[8 + dx + (8 + dy) * 20] = (byte)destination;
            RouteTicks(1);
            AssertEq(_event.X, 8 + dx, $"Command{command} uses the outward source edge and inward destination edge");
            AssertEq(_event.Y, 8 + dy);
        }
    }

    public void Test_SkippableBlockedMovePreservesFacingAndAppliesStepFrequency()
    {
        StartAuthoredRouteMap(RoutePage(6, 7, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUp)));
        _event.Pages[0].MoveRouteSkippable = true;
        // Up is allowed, but the first command's left exit is blocked.
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = 8;
        RouteTicks(3);
        AssertEq(_event.X, 8);
        AssertEq(_event.Y, 8);
        AssertEq(PageRoutes()[1].CurrentIndex, 1, "A skippable refusal consumes only the refused command");
        AssertEq(_event.Direction, (byte)6, "A refused skippable move restores the previous direction");
        AssertEq(_event.FacingDirection, (byte?)6);
        AssertEq(_event.MaxStopCount, 4, "The refused command still selects step delay, not initial turn delay2");
        RouteTicks(1);
        AssertEq(_event.Y, 8, "The step-frequency pause is spent before the next command");
        RouteTicks(1);
        AssertEq(_event.Y, 7);
    }

    public void Test_CustomRepeatingRouteKeepsItsCursorAndNeverStopsOnTheFirstWrap()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)));
        RouteTicks(1);
        var route = PageRoutes()[1];
        AssertEq(_runtime.EventRemainingStepForTest(1), 256 - 16, "A newly started production step spends its first tick");
        RouteTicks(15);
        AssertEq(_event.X, 9, "The arrival tick cannot start a second move");
        RouteTicks(16);
        AssertEq(_event.X, 8);
        RouteTicks(1);
        AssertEq(_event.X, 9, "A repeating route runs another cycle");
        AssertTrue(ReferenceEquals(route, PageRoutes()[1]), "Unchanged pages retain the same live route");
        AssertTrue(route.Active);
        AssertFalse(route.Finished);
    }

    public void Test_CustomRouteUsesInitialTurnDelayAndThenMovementFrequency()
    {
        StartAuthoredRouteMap(RoutePage(6, 7, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)));
        // Pinned frequency7: initial turn limit2, step limit4.
        RouteTicks(2);
        AssertEq(_event.X, 8);
        RouteTicks(1);
        AssertEq(_event.X, 9);
        RouteTicks(15);
        AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        RouteTicks(4);
        AssertEq(_event.X, 9, "Step-frequency pause is not the shorter initial turn pause");
        RouteTicks(1);
        AssertEq(_event.X, 8);
    }

    public void Test_PageSwitchRebindsCommandsButSameCodesRetainTheCursor()
    {
        StartAuthoredRouteMap(
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)),
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight) { ParameterA = 99 }, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)));
        HigherRouteRequiresSwitch();
        RouteTicks(1);
        var old = PageRoutes()[1];
        // Direction commands have no parameters on wire. Exercise the native
        // cursor comparison independently on the controlled typed page model.
        _event.Pages[1].MoveRouteCommands[0].ParameterA = 99;
        _runtime.Simulation.Switches[0] = true;
        RouteTicks(1);
        var current = PageRoutes()[1];
        AssertFalse(ReferenceEquals(old, current), "The new page owns the route commands");
        AssertEq(current.Commands[0].ParameterA, 99, "Ignored-for-comparison parameters still rebind");
        AssertEq(current.CurrentIndex, 1, "Identical command codes retain the original cursor");
        RouteTicks(14);
        AssertEq(_event.X, 9);
        RouteTicks(1);
        AssertEq(_event.X, 8, "The second command executes, not a restarted first command");
    }

    public void Test_DifferentPageRouteCodesResetCursorWithoutAbortingTheCurrentStep()
    {
        StartAuthoredRouteMap(
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)),
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUp), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)));
        HigherRouteRequiresSwitch();
        RouteTicks(1);
        _runtime.Simulation.Switches[0] = true;
        RouteTicks(1);
        AssertEq(PageRoutes()[1].CurrentIndex, 0);
        AssertTrue(_runtime.EventRemainingStepForTest(1) > 0, "Page changes retain the ongoing step");
        RouteTicks(14);
        AssertEq(_event.Y, 8);
        RouteTicks(1);
        AssertEq(_event.Y, 7, "The new route starts at its first command after arrival");
    }

    public void Test_NoEligiblePageSuspendsCommandsButLetsTheCurrentStepArrive()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        _event.Pages[0].Conditions["switch_a_enabled"] = true;
        _event.Pages[0].Conditions["switch_a_id"] = 1;
        while (_runtime.Simulation.Switches.Count < 1) _runtime.Simulation.Switches.Add(false);
        _runtime.Simulation.Switches[0] = true;
        RouteTicks(1);
        _runtime.Simulation.Switches[0] = false;
        RouteTicks(30);
        AssertEq(_event.X, 9, "No page cannot launch more commands from an old route");
        AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        AssertTrue(EventSprite() == null);
        _runtime.Simulation.Switches[0] = true;
        RouteTicks(1);
        AssertEq(_event.X, 10, "A newly eligible page restarts its original route");
    }

    public void Test_ForcedRouteHasPriorityAndOriginalRouteResumesItsOwnCursor()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)));
        RouteTicks(16);
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        AssertEq(_runtime.StartEventMoveRouteForTest(1), 2);
        RouteTicks(1);
        AssertEq(_event.X, 10, "The forced first command takes priority over the page's second command");
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        for (var tick = 0; tick < 80 && ForcedRoutes().ContainsKey(1); tick++) RouteTicks(1);
        AssertFalse(ForcedRoutes().ContainsKey(1));
        AssertEq(_event.X, 9);
        AssertEq(PageRoutes()[1].CurrentIndex, 1, "A forced route does not consume the original cursor");
        RouteTicks(1);
        AssertEq(_event.X, 8, "The original route resumes at its second command");
    }

    public void Test_UnsupportedPageRouteCommandIsVisibleAndDoesNotPretendToExecute()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, true,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.SwitchOn) { ParameterA = 1 }, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        RouteTicks(40);
        AssertEq(_event.X, 8, "An unsupported prefix cannot silently disappear while later commands run");
        AssertEq(PageRoutes()[1].CurrentIndex, 0);
        AssertEq(_runtime.Simulation.Diagnostics.Count(message => message.Contains("unsupported command 32")), 1,
            "The unsupported command is diagnosed once, not every tick");
    }

    public void Test_MainInterpreterBlocksAutonomousStartsButParallelDoesNot()
    {
        foreach (var trigger in new[] { 3, 4 })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
            var waiting = new Rm2kMap.Event(900, 0, 0);
            var page = new Rm2kMap.EventPage { Trigger = trigger };
            page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 10, 0 }));
            page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
            waiting.Pages.Add(page);
            _runtime.EventScheduler.SetEvents(new[] { _event, waiting });
            RouteTicks(5);
            AssertEq(_runtime.EventScheduler.HasBlockingInterpreter, trigger == 3);
            AssertEq(_event.X, trigger == 3 ? 8 : 9, "Only the main interpreter suppresses autonomous command starts");
        }
    }

    public void Test_CustomNonMovementChainRunsInOneTickAndFeedsEffectiveSpeed()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.FaceUp), new Rm2kMap.MoveCommand(Rm2kMoveRoute.IncreaseMovementSpeed),
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        RouteTicks(1);
        AssertEq(_event.X, 9, "Zero-delay facing and speed commands precede the step in the same tick");
        AssertEq(PageRoutes()[1].MoveSpeed, 4);
        AssertEq(_runtime.EventRemainingStepForTest(1), 256 - 32);
        RouteTicks(5);
        AssertEq(EventSprite()!.Frame, 2, "Animation uses the live route's speed4 limit6, not page speed3 limit8");
        AssertPinnedCellPixels(3, 2, 1);
    }

    public void Test_PageChangeCannotReplaceAnActiveForcedRoute()
    {
        StartAuthoredRouteMap(
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)),
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveUp)));
        HigherRouteRequiresSwitch();
        AssertEq(_runtime.StartEventMoveRouteForTest(1), 1);
        RouteTicks(1);
        var forced = ForcedRoutes()[1];
        _runtime.Simulation.Switches[0] = true;
        RouteTicks(1);
        AssertTrue(ReferenceEquals(forced, ForcedRoutes()[1]), "The page only rebinds its original route, not the forced program");
        AssertEq(forced.Commands[0].CommandId, Rm2kMoveRoute.MoveRight);
        AssertEq(PageRoutes()[1].Commands[0].CommandId, Rm2kMoveRoute.MoveUp);
        AssertTrue(_runtime.EventRemainingStepForTest(1) > 0);
        for (var tick = 0; tick < 80 && ForcedRoutes().ContainsKey(1); tick++) RouteTicks(1);
        RouteTicks(1);
        AssertEq(_event.Y, 7, "After the forced step arrives, the new page's program runs");
    }

    public void Test_ForcedRouteAlsoSuspendsNewCommandsWithoutAnEligiblePage()
    {
        StartAuthoredRouteMap(RoutePage(0, 8, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        _event.Pages[0].Conditions["switch_a_enabled"] = true;
        _event.Pages[0].Conditions["switch_a_id"] = 1;
        while (_runtime.Simulation.Switches.Count < 1) _runtime.Simulation.Switches.Add(false);
        _runtime.Simulation.Switches[0] = true;
        AssertEq(_runtime.StartEventMoveRouteForTest(1), 2);
        RouteTicks(1);
        _runtime.Simulation.Switches[0] = false;
        RouteTicks(40);
        AssertEq(_event.X, 9, "A forced route cannot start its next command while the event has no page");
        AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        AssertTrue(ForcedRoutes().ContainsKey(1), "The forced route program survives the disappearance");
        if (ForcedRoutes().TryGetValue(1, out var retained))
            AssertEq(retained.CurrentIndex, 1, "The forced route cursor survives the disappearance");
    }

    public void Test_NonCustomModesCannotExecuteAnEmbeddedCustomRoute()
    {
        foreach (var mode in new int?[] { 0, null, 1, 2, 3, 4, 5, 99 })
        {
            StartAuthoredRouteMap(RoutePage(mode, 8, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
            RouteTicks(40);
            AssertEq(_event.X, 8);
            AssertEq(_runtime.EventRemainingStepForTest(1), -1);
            if (mode != 0) AssertEq(_runtime.Simulation.Diagnostics.Count(message => message.Contains("autonomous movement type")), 1);
        }
    }

    public void Test_AutonomousRouteStateIsReleasedOnStopDisposeAndMapReplacement()
    {
        foreach (var action in new[] { "stop", "dispose", "transfer" })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
            RouteTicks(1);
            var old = PageRoutes()[1];
            if (action == "stop") AssertTrue(_host.Stop().Success);
            else if (action == "dispose") _runtime.Dispose();
            else
            {
                File.WriteAllBytes(Path.Combine(_root, "Map0002.lmu"), TestRm2kEventPagePoseParser.MapWithPages(
                    RoutePage(0, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight))));
                _runtime.Simulation.PendingMapId = 2;
                _runtime.Simulation.PendingX = 0;
                _runtime.Simulation.PendingY = 0;
                _runtime.Simulation.IsTransferPending = true;
                RouteTicks(1);
                AssertEq(_runtime.Simulation.MapId, 2);
                AssertFalse(ReferenceEquals(old, PageRoutes()[1]));
            }
            AssertEq(_runtime.EventRemainingStepForTest(1), -1, $"{action} cannot retain an old step");
            if (action != "transfer") AssertEq(PageRoutes().Count, 0);
        }
    }

    public void Test_CustomRouteBatchMatchesScalarMovementCursorAndPixels()
    {
        var page = RoutePage(6, 7, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft));
        StartAuthoredRouteMap(page);
        RouteTicks(40);
        var scalar = (_event.X, _event.Y, _event.AnimationFrame, _event.AnimationCount, _event.StopCount,
            PageRoutes()[1].CurrentIndex, _runtime.EventRemainingStepForTest(1));
        var pixels = (byte[])_runtime.RenderedMap!.Pixels.Clone();
        StartAuthoredRouteMap(page);
        AssertTrue(_runtime.Update(40.0 / 60.0 + 1e-9).Success);
        AssertEq((_event.X, _event.Y, _event.AnimationFrame, _event.AnimationCount, _event.StopCount,
            PageRoutes()[1].CurrentIndex, _runtime.EventRemainingStepForTest(1)), scalar);
        AssertTrue(pixels.SequenceEqual(_runtime.RenderedMap!.Pixels));
    }

    public void Test_NativeWaitRouteCommandSpendsItsOwnReferenceDelayBeforeMoving()
    {
        foreach (var frequency in new[] { 8, 7 })
        {
            StartAuthoredRouteMap(RoutePage(6, frequency, false,
                new Rm2kMap.MoveCommand(Rm2kMoveRoute.Wait), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
            var initialDelay = frequency == 8 ? 0 : 2;
            RouteTicks(initialDelay + 1);
            AssertEq(PageRoutes()[1].CurrentIndex, 1, "Wait23 executes without being rejected or skipped");
            AssertEq(_event.MaxStopCount, frequency == 8 ? 20 : 22, "Wait has20 frames plus the turn-frequency delay");
            var delay = frequency == 8 ? 20 : 22;
            RouteTicks(delay - 1);
            AssertEq(_event.X, 8, "Wait spends its full frame budget before starting another command");
            RouteTicks(1);
            AssertEq(_event.X, 9);
            AssertEq(_runtime.EventRemainingStepForTest(1), 256 - 16);
            AssertFalse(_runtime.Simulation.Diagnostics.Any(message => message.Contains("unsupported command 23")));
            RouteTicks(7);
            AssertEq(EventSprite()!.Frame, 2);
            AssertPinnedCellPixels(3, 2, 1);
        }
    }

    public void Test_FileAuthoredMovementModeSurvivesTheHostBridge()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        var projected = _event.Pages[0].ToDict();
        AssertTrue(projected.ContainsKey("move_type"), "The host must retain page movement mode, not only its route");
        if (projected.TryGetValue("move_type", out var mode)) AssertEq((int)mode, 6);
        StartAuthoredRouteMap(RoutePage(null, 3, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        projected = _event.Pages[0].ToDict();
        AssertTrue(projected.ContainsKey("move_type"));
        if (projected.TryGetValue("move_type", out mode)) AssertEq((int)mode, 1,
            "An omitted movement mode does not silently activate a custom route");
    }

    public void Test_FileAuthoredCustomRouteWalksWithoutTheRouteTestEntryPoint()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        var pageData = _runtime.CurrentMapData!["events"].AsGodotArray()[0]
            .AsGodotDictionary()["pages"].AsGodotArray()[0].AsGodotDictionary();
        AssertTrue(pageData.ContainsKey("move_type"), "LMU0x1F must survive parsing, not merely the route vector");
        if (pageData.ContainsKey("move_type")) AssertEq(pageData["move_type"].AsInt32(), 6);
        AssertEq(_event.Pages[0].MoveRouteCommands.Count, 1, "The file contains a real decoded route");
        AssertEq(_event.X, 8);
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_event.X, 9, "A custom page starts its own route from the production update");
        AssertTrue(_runtime.EventRemainingStepForTest(1) > 0, "The event walks, rather than teleporting the whole route");
        for (var tick = 0; tick < 7; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 2, "The production route feeds normal moving animation at reference tick8");
        AssertPinnedCellPixels(3, 2, 1);
    }
}
