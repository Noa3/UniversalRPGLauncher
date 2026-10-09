using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Rm2k.Interpreter;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kActiveEventGraphic
{
    public void Test_TowardPageCursorIsNotConsumedByTheExistingForcedRouteSeam()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
        _runtime.Simulation.MapX = 13;
        _runtime.Simulation.MapY = 8;
        AssertEq(_runtime.StartEventMoveRouteForTest(1), 1);
        RouteTicks(1);
        AssertEq(_event.X, 9);
        AssertEq(PageRoutes()[1].CurrentIndex, 0);
        RouteTicks(16);
        AssertFalse(ForcedRoutes().ContainsKey(1));
        AssertEq(PageRoutes()[1].CurrentIndex, 0);
        RouteTicks(1);
        AssertEq(_event.X, 10);
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        // This seam's legacy timing is not a claim of production forced-route parity.
    }

    public void Test_TowardCommandDoesNotBypassMainInterpreterButAllowsParallel()
    {
        foreach (var trigger in new[] { 3, 4 })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
            _runtime.Simulation.MapX = 13;
            _runtime.Simulation.MapY = 8;
            var waiting = new Rm2kMap.Event(900, 0, 0);
            var page = new Rm2kMap.EventPage { Trigger = trigger };
            page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 10, 0 }));
            page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
            waiting.Pages.Add(page);
            _runtime.EventScheduler.SetEvents(new[] { _event, waiting });
            RouteTicks(5);
            AssertEq(_runtime.EventScheduler.HasBlockingInterpreter, trigger == 3);
            AssertEq(_event.X, trigger == 3 ? 8 : 9);
            AssertEq(PageRoutes()[1].CurrentIndex, trigger == 3 ? 0 : 1);
        }
    }

    public void Test_NonSkippableCardinalRefusalsShareTheAttemptedPoseRule()
    {
        foreach (var (command, facing) in new[]
        {
            (Rm2kMoveRoute.MoveUp, 8), (Rm2kMoveRoute.MoveRight, 6),
            (Rm2kMoveRoute.MoveDown, 2), (Rm2kMoveRoute.MoveLeft, 4)
        })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(command)));
            _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = 0;
            RouteTicks(1);
            AssertEq((_event.X, _event.Y), (8, 8));
            AssertEq(_event.Direction, (byte)facing);
            AssertEq(_event.FacingDirection, (byte?)facing);
            AssertEq(EventSprite()!.FacingDirection, (byte)facing);
            AssertEq(PageRoutes()[1].CurrentIndex, 0);
        }
    }

    public void Test_TowardHeroUsesDominantAxisAndVerticalTiesIncludingCoincidence()
    {
        foreach (var (x, y, dx, dy, facing) in new[]
        {
            (12, 9, 1, 0, 6), (4, 9, -1, 0, 4), (9, 3, 0, -1, 8), (9, 13, 0, 1, 2),
            (5, 5, 0, -1, 8), (11, 11, 0, 1, 2), (11, 5, 0, -1, 8), (5, 11, 0, 1, 2),
            (8, 8, 0, 1, 2)
        })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
            _runtime.Simulation.MapX = x;
            _runtime.Simulation.MapY = y;
            RouteTicks(1);
            AssertEq((_event.X, _event.Y), (8 + dx, 8 + dy), $"Hero({x},{y}) follows pinned axis selection");
            AssertEq(_event.Direction, (byte)facing);
            AssertEq(_event.FacingDirection, (byte?)facing);
            AssertEq(PageRoutes()[1].CurrentIndex, 1);
        }
    }

    public void Test_SkippableTowardFailureRestoresPoseAndSpendsStepFrequency()
    {
        StartAuthoredRouteMap(RoutePage(6, 7, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight)));
        _event.Pages[0].MoveRouteSkippable = true;
        _runtime.Simulation.MapX = 7;
        _runtime.Simulation.MapY = 3;
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = 4;
        RouteTicks(3);
        AssertEq((_event.X, _event.Y), (8, 8));
        AssertEq(_event.Direction, (byte)6);
        AssertEq(_event.FacingDirection, (byte?)6);
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        AssertEq(_event.MaxStopCount, 4);
        AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        AssertPinnedCellPixels(3, 1, 1);
        RouteTicks(1);
        AssertEq(_event.X, 8, "A refused skippable step cannot launch the next command too early");
        RouteTicks(1);
        AssertEq(_event.X, 9);
        AssertEq(_runtime.EventRemainingStepForTest(1), 240);
    }

    public void Test_TowardHeroReevaluatesOnlyAfterArrivalAndStepDelay()
    {
        StartAuthoredRouteMap(RoutePage(6, 7, false,
            new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero), new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
        _runtime.Simulation.MapX = 13;
        _runtime.Simulation.MapY = 8;
        RouteTicks(2);
        AssertEq(_event.X, 8);
        RouteTicks(1);
        AssertEq(_event.X, 9);
        _runtime.Simulation.MapX = 4;
        RouteTicks(15);
        AssertEq(_event.X, 9, "An in-flight step is not redirected by a moving target");
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        RouteTicks(4);
        AssertEq(_event.X, 9);
        RouteTicks(1);
        AssertEq(_event.X, 8, "The next command reads the target's current position");
        AssertEq(_event.Direction, (byte)4);
        AssertEq(PageRoutes()[1].CurrentIndex, 2);
    }

    public void Test_TowardHeroChecksBothTileEdgesInEverySelectedDirection()
    {
        foreach (var (x, y, dx, dy) in new[] { (8, 3, 0, -1), (13, 8, 1, 0), (8, 13, 0, 1), (3, 8, -1, 0) })
        foreach (var blockSource in new[] { false, true })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
            _runtime.Simulation.MapX = x;
            _runtime.Simulation.MapY = y;
            var index = blockSource ? 8 + 8 * 20 : 8 + dx + (8 + dy) * 20;
            _runtime.Simulation.PassabilityMasks[index] = 0;
            RouteTicks(1);
            AssertEq((_event.X, _event.Y), (8, 8));
            AssertEq(PageRoutes()[1].CurrentIndex, 0);
            AssertEq(_runtime.EventRemainingStepForTest(1), -1);
        }
    }

    public void Test_TowardHeroKeepsFixedFacingIndependentFromItsMovementAxis()
    {
        // Pose is authored before loading; changing the already-bound page's
        // dictionary would test an unsupported mutable-definition path.
        StartAuthoredRouteMap(TestRm2kParser.Struct(
            TestRm2kParser.Chunk(0x15, Encoding.ASCII.GetBytes("Chara1")),
            TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x1F, TestRm2kParser.Ber(6)),
            TestRm2kParser.Chunk(0x20, TestRm2kParser.Ber(8)),
            TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(2)),
            TestRm2kParser.Chunk(0x24, TestRm2kParser.Ber(2)),
            TestRm2kParser.Chunk(0x29, AuthoredRoute(false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)))));
        _runtime.Simulation.MapX = 8;
        _runtime.Simulation.MapY = 3;
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (8, 7));
        AssertEq(_event.Direction, (byte)8);
        AssertEq(_event.FacingDirection, (byte?)4);
        RouteTicks(7);
        AssertEq(EventSprite()!.FacingDirection, (byte)4);
        AssertPinnedCellPixels(3, 2, 3);
    }

    public void Test_TowardHeroPageSwitchPreservesTheOldStepBeforeRebinding()
    {
        StartAuthoredRouteMap(
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)),
            RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveLeft)));
        HigherRouteRequiresSwitch();
        _runtime.Simulation.MapX = 13;
        _runtime.Simulation.MapY = 8;
        RouteTicks(1);
        _runtime.Simulation.Switches[0] = true;
        RouteTicks(15);
        AssertEq((_event.X, _event.Y), (9, 8));
        AssertEq(PageRoutes()[1].CurrentIndex, 0);
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (8, 8));
    }

    public void Test_TowardRouteCannotSurviveStopDisposeOrMapIdReuse()
    {
        foreach (var action in new[] { "stop", "dispose", "transfer" })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
            _runtime.Simulation.MapX = 13;
            _runtime.Simulation.MapY = 8;
            RouteTicks(1);
            var old = PageRoutes()[1];
            if (action == "stop") AssertTrue(_host.Stop().Success);
            else if (action == "dispose") _runtime.Dispose();
            else
            {
                File.WriteAllBytes(Path.Combine(_root, "Map0002.lmu"), TestRm2kEventPagePoseParser.MapWithPages(
                    RoutePage(0, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero))));
                _runtime.Simulation.PendingMapId = 2;
                _runtime.Simulation.PendingX = 13;
                _runtime.Simulation.PendingY = 8;
                _runtime.Simulation.IsTransferPending = true;
                RouteTicks(1);
                AssertEq(_runtime.Simulation.MapId, 2);
                AssertFalse(ReferenceEquals(old, PageRoutes()[1]));
                RouteTicks(20);
            }
            AssertEq(_runtime.EventRemainingStepForTest(1), -1);
            if (action != "transfer") AssertEq(PageRoutes().Count, 0);
            else AssertEq(PageRoutes()[1].CurrentIndex, 0, "The new stationary page cannot inherit old pursuit");
        }
    }

    public void Test_TowardRouteScalarAndBatchHaveIdenticalStateAndPixels()
    {
        var page = RoutePage(6, 7, true, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero));
        StartAuthoredRouteMap(page);
        _runtime.Simulation.MapX = 13;
        _runtime.Simulation.MapY = 9;
        RouteTicks(60);
        var scalar = (_event.X, _event.Y, _event.Direction, _event.FacingDirection, _event.AnimationFrame,
            _event.AnimationCount, _event.StopCount, PageRoutes()[1].CurrentIndex, _runtime.EventRemainingStepForTest(1));
        var pixels = (byte[])_runtime.RenderedMap!.Pixels.Clone();
        StartAuthoredRouteMap(page);
        _runtime.Simulation.MapX = 13;
        _runtime.Simulation.MapY = 9;
        AssertTrue(_runtime.Update(1.0 + 1e-9).Success);
        AssertEq((_event.X, _event.Y, _event.Direction, _event.FacingDirection, _event.AnimationFrame,
            _event.AnimationCount, _event.StopCount, PageRoutes()[1].CurrentIndex, _runtime.EventRemainingStepForTest(1)), scalar);
        AssertTrue(pixels.SequenceEqual(_runtime.RenderedMap!.Pixels));
    }

    public void Test_TowardExtensionDoesNotWhitelabelOtherRelativeOrRandomCommands()
    {
        foreach (var command in new[] { Rm2kMoveRoute.MoveRandom, Rm2kMoveRoute.MoveAwayFromHero, Rm2kMoveRoute.MoveForward })
        {
            StartAuthoredRouteMap(RoutePage(6, 8, true, new Rm2kMap.MoveCommand(command)));
            _runtime.Simulation.MapX = 13;
            _runtime.Simulation.MapY = 8;
            RouteTicks(20);
            AssertEq((_event.X, _event.Y), (8, 8));
            AssertEq(_runtime.Simulation.Diagnostics.Count(message => message.Contains($"unsupported command {command},")), 1);
        }
    }

    public void Test_BlockedTowardHeroFacesItsAttemptWithoutAlternateAxisFallback()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
        _runtime.Simulation.MapX = 7;
        _runtime.Simulation.MapY = 3;
        // Both horizontal exits are open, but the dominant upward exit is not.
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = 6;
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (8, 8));
        AssertEq(PageRoutes()[1].CurrentIndex, 0);
        AssertEq(PageRoutes()[1].MoveFailureCount, 1);
        AssertEq(_event.Direction, (byte)8, "A non-skippable blocked attempt retains its selected direction");
        AssertEq(_event.FacingDirection, (byte?)8, "A failed attempt still updates visible facing");
        AssertEq(EventSprite()!.FacingDirection, (byte)8, "The failed turn reaches the actual composed frame");
        AssertPinnedCellPixels(3, 1, 0);
        _runtime.Simulation.MapX = 13;
        _runtime.Simulation.MapY = 9;
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (9, 8), "A retry reselects direction from the hero's current position");
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        AssertEq(PageRoutes()[1].MoveFailureCount, 0, "A completed command clears the previous failure streak");
    }

    public void Test_FileAuthoredTowardHeroStartsARealStepAndDrawsItsFacing()
    {
        StartAuthoredRouteMap(RoutePage(6, 8, false, new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveTowardsHero)));
        _runtime.Simulation.MapX = 12;
        _runtime.Simulation.MapY = 9;
        RouteTicks(1);
        AssertEq((_event.X, _event.Y), (9, 8), "Opcode9 moves one step on the dominant axis, not to the hero's tile");
        AssertEq(PageRoutes()[1].CurrentIndex, 1);
        AssertEq(_runtime.EventRemainingStepForTest(1), 240, "The new step spends its first simulation tick");
        AssertEq(_event.Direction, (byte)6);
        AssertFalse(_runtime.Simulation.Diagnostics.Any(message => message.Contains("unsupported command 9")));
        RouteTicks(7);
        AssertEq(EventSprite()!.Frame, 2);
        AssertPinnedCellPixels(3, 2, 1);
    }
}
