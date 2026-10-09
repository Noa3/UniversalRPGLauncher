using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kActiveEventGraphic
{
    public void Test_AllEventAnimationTypesHonorIdleAndMovingPredicates()
    {
        var first = true;
        foreach (var type in new[] { 0, 1, 2, 3, 4, 5, 6 })
        foreach (var moving in new[] { false, true })
        {
            if (!first) { Teardown(); Setup(); }
            first = false;
            var page = Page("Chara1", 3, 2);
            page.Graphic["animation_type"] = type;
            page.Graphic["character_pattern"] = 1;
            if (moving)
            {
                page.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight));
                page.MoveRouteRepeat = false;
                for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
                    _runtime.Simulation.PassabilityMasks[index] = 15;
            }
            _event.Pages.Add(page);
            _event.AnimationFrame = 1;
            Repaint();
            if (moving) AssertEq(_runtime.StartEventMoveRouteForTest(900), 1);
            for (var tick = 0; tick < 10; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
            var advances = type is 1 or 3 || (moving && type is 0 or 2);
            var expected = advances ? 2 : 1;
            AssertEq(EventSprite()!.Frame, expected, $"Type{type}/moving={moving} follows reference animation predicates");
            AssertPinnedCellPixels(3, expected, 1);
            if (type is 4 or 6) AssertEq(_event.AnimationCount, 0, "Unanimated types retain their counter too");
        }
    }

    public void Test_EventSpeedTablesUseIndependentReferenceThresholds()
    {
        foreach (var type in new[] { 1, 5 })
        foreach (var speed in new[] { 1, 2, 3, 4, 5, 6 })
        {
            _event.Pages.Clear();
            var page = Page("Chara1", 3, 2);
            page.Graphic["animation_type"] = type;
            page.Graphic["move_speed"] = speed;
            page.Graphic["character_pattern"] = 1;
            _event.Pages.Add(page);
            _event.AnimationFrame = 1;
            _event.AnimationCount = 0;
            _event.Direction = 6;
            _event.FacingDirection = 6;
            Repaint();
            var limits = type == 5 ? new[] { 24, 16, 12, 8, 6, 4 } : new[] { 16, 12, 10, 8, 7, 6 };
            var limit = limits[speed - 1];
            for (var tick = 0; tick < limit - 1; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
            AssertEq(EventSprite()!.Frame, 1, $"Type{type}/speed{speed} does not advance its frame early");
            AssertEq(EventSprite()!.FacingDirection, (byte)6, "No early spin turn");
            AssertTrue(_runtime.Update(1.0 / 60.0).Success);
            if (type == 5)
            {
                AssertEq(EventSprite()!.FacingDirection, (byte)2, $"Speed{speed} reaches its exact spin threshold");
                AssertPinnedCellPixels(3, 1, 2);
            }
            else
            {
                AssertEq(EventSprite()!.Frame, 2, $"Speed{speed} reaches its exact continuous threshold");
                AssertPinnedCellPixels(3, 2, 1);
            }
        }
    }

    public void Test_UnchangedPageAndPageTransitionRetainTheAnimationCounter()
    {
        var page = Page("Chara1", 3, 2);
        page.Graphic["animation_type"] = 1;
        _event.Pages.Add(page);
        _event.AnimationFrame = 1;
        Repaint();
        for (var tick = 0; tick < 5; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_event.AnimationCount, 5);
        Repaint();
        AssertEq(_event.AnimationCount, 5, "A render-only repaint does not spend or reset simulation animation count");
        var higher = Page("Chara1", 3, 2);
        higher.Graphic["animation_type"] = 3;
        _event.Pages.Add(higher);
        for (var tick = 0; tick < 5; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 2, "Changing to a fixed-continuous page retains the already elapsed ticks");
        AssertEq(_event.AnimationCount, 0);
        var frame = EventSprite()!.Frame;
        AssertTrue(_runtime.Update(0).Success);
        AssertEq(EventSprite()!.Frame, frame, "A render update without a simulation tick cannot animate an event");
    }

    public void Test_StopAndDisposeReleaseLiveEventMovementState()
    {
        foreach (var stop in new[] { true, false })
        {
            if (!stop) { Teardown(); Setup(); }
            var page = Page("Chara1", 3, 2);
            page.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight));
            _event.Pages.Add(page);
            for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
                _runtime.Simulation.PassabilityMasks[index] = 15;
            Repaint();
            AssertEq(_runtime.StartEventMoveRouteForTest(900), 1);
            AssertTrue(_runtime.Update(1.0 / 60.0).Success);
            AssertTrue(_runtime.EventRemainingStepForTest(900) > 0);
            if (stop) AssertTrue(_host.Stop().Success);
            else _runtime.Dispose();
            AssertEq(_runtime.EventRemainingStepForTest(900), -1, "Lifecycle termination releases an unspent step");
            var routes = (Dictionary<int, Rm2kMoveRouteState>)typeof(Rm2kEngineRuntime)
                .GetField("_eventRoutes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!;
            AssertEq(routes.Count, 0, "Lifecycle termination releases old event routes");
            AssertFalse(_runtime.Update(1.0 / 60.0).Success);
        }
    }

    public void Test_MapTransferCannotAttachOldRouteOrStepToAReusedEventId()
    {
        _event.Id = 1;
        var oldPage = Page("Chara1", 3, 2);
        oldPage.Graphic["move_speed"] = 6;
        oldPage.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight));
        oldPage.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight));
        oldPage.MoveRouteRepeat = true;
        _event.Pages.Add(oldPage);
        for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
            _runtime.Simulation.PassabilityMasks[index] = 15;
        Repaint();
        AssertEq(_runtime.StartEventMoveRouteForTest(1), 2);
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertTrue(_runtime.EventRemainingStepForTest(1) > 0, "The old map genuinely has a live route and unspent step");
        var newPage = TestRm2kParser.Struct(
            TestRm2kParser.Chunk(0x15, System.Text.Encoding.ASCII.GetBytes("Chara1")),
            TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(2)),
            TestRm2kParser.Chunk(0x24, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x25, TestRm2kParser.Ber(1)));
        File.WriteAllBytes(Path.Combine(_root, "Map0002.lmu"), TestRm2kEventPagePoseParser.MapWithPages(newPage));
        _runtime.Simulation.PendingMapId = 2;
        _runtime.Simulation.PendingX = 0;
        _runtime.Simulation.PendingY = 0;
        _runtime.Simulation.IsTransferPending = true;
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_runtime.Simulation.MapId, 2);
        _event = ((List<Rm2kMap.Event>)typeof(Rm2kEngineRuntime)
            .GetField("_mapEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!).Single();
        AssertEq(_runtime.EventRemainingStepForTest(1), -1, "A reused event ID cannot inherit the old map's step budget");
        AssertEq(EventSprite()!.FacingDirection, (byte)6, "Fresh page pose is applied as stopped, not suppressed by an old step");
        var routes = (Dictionary<int, Rm2kMoveRouteState>)typeof(Rm2kEngineRuntime)
            .GetField("_eventRoutes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!;
        AssertFalse(routes.ContainsKey(1), "A route from the previous map cannot resume on a new event");
        for (var tick = 0; tick < 14; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_event.X, 8, "The new event stays at its own authored position");
        AssertEq(EventSprite()!.Frame, 1, "The new event uses its own speed1 threshold16, not stale route speed6");
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 2);
        AssertPinnedCellPixels(3, 2, 1);
    }

    public void Test_MapTransferResetsNewEventCountersAndStopFreezesTheOldRuntime()
    {
        _event.Id = 1;
        var page = Page("Chara1", 3, 2);
        page.Graphic["animation_type"] = 1;
        _event.Pages.Add(page);
        _event.AnimationFrame = 1;
        Repaint();
        for (var tick = 0; tick < 5; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_event.AnimationCount, 5);
        var transferredPage = TestRm2kParser.Struct(
            TestRm2kParser.Chunk(0x15, System.Text.Encoding.ASCII.GetBytes("Chara1")),
            TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(2)),
            TestRm2kParser.Chunk(0x24, TestRm2kParser.Ber(1)));
        File.WriteAllBytes(Path.Combine(_root, "Map0002.lmu"), TestRm2kEventPagePoseParser.MapWithPages(transferredPage));
        _runtime.Simulation.PendingMapId = 2;
        _runtime.Simulation.PendingX = 0;
        _runtime.Simulation.PendingY = 0;
        _runtime.Simulation.IsTransferPending = true;
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_runtime.Simulation.MapId, 2, "The real runtime transfer loaded the owned map");
        _event = ((List<Rm2kMap.Event>)typeof(Rm2kEngineRuntime)
            .GetField("_mapEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!).Single();
        AssertEq(_event.Id, 1, "The new map deliberately reuses the old event ID");
        AssertEq(_event.AnimationCount, 1, "The new event starts a fresh counter and spends only this transfer tick");
        for (var tick = 0; tick < 9; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 2);
        AssertPinnedCellPixels(3, 2, 1);
        AssertTrue(_host.Stop().Success);
        var count = _event.AnimationCount;
        AssertTrue(_runtime.RenderedMap == null, "Stopping releases the composed frame");
        AssertFalse(_runtime.Update(1).Success, "A stopped runtime rejects further simulation");
        AssertEq(_event.AnimationCount, count, "No counter continues after teardown");
    }

    public void Test_FileBasedEventSpeedReachesTheActualAnimationTick()
    {
        _host.Dispose();
        var page = TestRm2kParser.Struct(
            TestRm2kParser.Chunk(0x15, System.Text.Encoding.ASCII.GetBytes("Chara1")),
            TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(2)),
            TestRm2kParser.Chunk(0x24, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x25, TestRm2kParser.Ber(6)));
        File.WriteAllBytes(Path.Combine(_root, "Map0001.lmu"), TestRm2kEventPagePoseParser.MapWithPages(page));
        _host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        AssertTrue(_host.Start(new PluginGameInfo
        {
            GameDirectory = _root, EngineId = EnginePluginIds.RpgMaker2000, Generation = "rm2k", DetectorScore = 3
        }).Success);
        _runtime = (Rm2kEngineRuntime)_host.Runtime!;
        _event = ((List<Rm2kMap.Event>)typeof(Rm2kEngineRuntime)
            .GetField("_mapEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!).Single();
        for (var tick = 0; tick < 5; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 1, "File-authored speed6 holds the frame through tick5");
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 2, "LMU0x25 speed6 reaches continuous limit6, not default-speed limit10");
        AssertPinnedCellPixels(3, 2, 1);
    }

    public void Test_BatchedTransferPreservesEventTimerAndScreenEffectTicks()
    {
        void PrepareTransfer()
        {
            var page = TestRm2kParser.Struct(
                TestRm2kParser.Chunk(0x15, System.Text.Encoding.ASCII.GetBytes("Chara1")),
                TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(3)),
                TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(1)),
                TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(2)),
                TestRm2kParser.Chunk(0x24, TestRm2kParser.Ber(1)));
            File.WriteAllBytes(Path.Combine(_root, "Map0002.lmu"), TestRm2kEventPagePoseParser.MapWithPages(page));
            AssertTrue(_runtime.Simulation.SetTimer(1, 4));
            AssertTrue(_runtime.Simulation.StartTimer(1, pVisible: true, pInBattle: true));
            AssertTrue(_runtime.Presentation.FlashOnce(255, 128, 64, 192, 30));
            AssertTrue(_runtime.Presentation.ShakeOnce(2, 3, 30));
            _runtime.Simulation.PendingMapId = 2;
            _runtime.Simulation.PendingX = 0;
            _runtime.Simulation.PendingY = 0;
            _runtime.Simulation.IsTransferPending = true;
        }
        Rm2kMap.Event TransferredEvent() => ((List<Rm2kMap.Event>)typeof(Rm2kEngineRuntime)
            .GetField("_mapEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!).Single();

        PrepareTransfer();
        for (var tick = 0; tick < 18; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        var scalar = TransferredEvent();
        var frame = scalar.AnimationFrame;
        var count = scalar.AnimationCount;
        var pixels = (byte[])_runtime.RenderedMap!.Pixels.Clone();
        for (var tick = 0; tick < 42; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_runtime.Simulation.Timer1Seconds, 3, "Scalar updates preserve the timer's fractional second through transfer");

        Teardown();
        Setup();
        PrepareTransfer();
        AssertTrue(_runtime.Update(18.0 / 60.0 + 1e-9).Success);
        AssertEq(_runtime.SimulationTicks, 18);
        AssertEq(_runtime.Simulation.FrameCount, 18L);
        AssertEq(_runtime.Simulation.MapId, 2);
        var batched = TransferredEvent();
        AssertEq(batched.AnimationFrame, frame, "Transfer occurs on the same simulation tick in a batch");
        AssertEq(batched.AnimationCount, count);
        AssertTrue(pixels.SequenceEqual(_runtime.RenderedMap!.Pixels));
        AssertEq(_runtime.Simulation.Timer1Seconds, 4);
        AssertTrue(_runtime.Simulation.Timer1Active);
        AssertEq(_runtime.Presentation.FlashFramesRemaining, 12);
        AssertEq(_runtime.Presentation.ShakeFramesRemaining, 12);
        AssertTrue(_runtime.Update(42.0 / 60.0 + 1e-9).Success);
        AssertEq(_runtime.Simulation.Timer1Seconds, 3, "A batch preserves the fractional timer remainder across calls");
        AssertFalse(_runtime.Presentation.IsFlashActive);
        AssertFalse(_runtime.Presentation.IsShakeActive);
    }

    public void Test_BatchedAndSingleTickUpdatesProduceIdenticalEventAnimationState()
    {
        void Prepare()
        {
            var page = Page("Chara1", 3, 2);
            page.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight));
            page.MoveRouteRepeat = false;
            _event.Pages.Add(page);
            _event.AnimationFrame = 1;
            for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
                _runtime.Simulation.PassabilityMasks[index] = 15;
            Repaint();
            AssertEq(_runtime.StartEventMoveRouteForTest(900), 1);
        }
        Prepare();
        for (var tick = 0; tick < 18; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        var scalarFrame = _event.AnimationFrame;
        var scalarCount = _event.AnimationCount;
        var scalarX = _event.X;
        var scalarStep = _runtime.EventRemainingStepForTest(900);
        var scalarPixels = (byte[])_runtime.RenderedMap!.Pixels.Clone();
        AssertEq(_runtime.SimulationTicks, 18);
        // A fresh owned host avoids reusing accumulated timing or animation state.
        Teardown();
        Setup();
        Prepare();
        AssertTrue(_runtime.Update(18.0 / 60.0 + 1e-9).Success);
        AssertEq(_runtime.SimulationTicks, 18, "The batch has exactly the same authoritative tick count");
        AssertEq(_event.AnimationFrame, scalarFrame, "Batching preserves the logical event animation frame");
        AssertEq(_event.AnimationCount, scalarCount, "Batching preserves the event's per-tick moving/idle counter");
        AssertEq(_event.X, scalarX);
        AssertEq(_runtime.EventRemainingStepForTest(900), scalarStep);
        AssertTrue(scalarPixels.SequenceEqual(_runtime.RenderedMap!.Pixels), "Batching preserves the actual composed pixels");
    }

    public void Test_NormalWalkingUsesMovingLimitThenSettlesWithoutContinuousAnimation()
    {
        var page = Page("Chara1", 3, 2);
        page.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight));
        page.MoveRouteRepeat = false;
        _event.Pages.Add(page);
        _event.AnimationFrame = 1;
        for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
            _runtime.Simulation.PassabilityMasks[index] = 15;
        Repaint();
        AssertEq(_runtime.StartEventMoveRouteForTest(900), 1);
        for (var tick = 0; tick < 7; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 1, "Normal walking waits for moving speed3 limit8");
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 2, "A moving normal event uses stationary_limit8, not continuous_limit10");
        AssertPinnedCellPixels(3, 2, 1);
        for (var tick = 0; tick < 80; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertTrue(_runtime.EventRemainingStepForTest(900) <= 0, "The genuine route step has finished");
        AssertTrue(_event.AnimationFrame is 1 or 3, "Normal idle animation settles on a middle pose");
        var settled = _runtime.RenderedMap!;
        for (var tick = 0; tick < 40; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertTrue(ReferenceEquals(settled, _runtime.RenderedMap), "A settled normal event does not keep animating at rest");
    }

    public void Test_SpinUsesReferenceFacingCycleWithoutChangingMovementOrPattern()
    {
        var page = Page("Chara1", 3, 2);
        page.Graphic["animation_type"] = 5;
        page.Graphic["character_direction"] = 0;
        page.Graphic["character_pattern"] = 1;
        _event.Pages.Add(page);
        Repaint();
        // Pinned speed3 spin limit12, facing order up/right/down/left.
        for (var tick = 0; tick < 11; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.FacingDirection, (byte)8, "Spin holds up through tick11");
        foreach (var expected in new byte[] { 6, 2, 4, 8 })
        {
            AssertTrue(_runtime.Update(1.0 / 60.0).Success);
            AssertEq(EventSprite()!.FacingDirection, expected, "Spin rotates visible facing at each twelfth tick");
            AssertEq(_event.Direction, (byte)8, "Spinning does not redirect the movement axis");
            AssertEq(EventSprite()!.Frame, 1, "Spinning retains the authored pattern rather than cycling walk frames");
            var row = expected switch { 6 => 1, 2 => 2, 4 => 3, _ => 0 };
            AssertPinnedCellPixels(3, 1, row);
            if (expected != 8)
                for (var tick = 0; tick < 11; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        }
        page.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(Rm2kMoveRoute.MoveRight));
        page.MoveRouteRepeat = false;
        for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
            _runtime.Simulation.PassabilityMasks[index] = 15;
        AssertEq(_runtime.StartEventMoveRouteForTest(900), 1);
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_event.Direction, (byte)6, "A spin event may move east");
        AssertEq(EventSprite()!.FacingDirection, (byte)8, "Ordinary movement cannot override the spin's visible facing");
        AssertTrue(_runtime.EventRemainingStepForTest(900) > 0);
    }

    public void Test_ContinuousEventAnimationUsesReferenceTickBoundaryAndRealPixels()
    {
        var page = Page("Chara1", 3, 2);
        page.Graphic["animation_type"] = 1;
        _event.Pages.Add(page);
        _event.AnimationFrame = 1;
        Repaint();
        var original = _runtime.RenderedMap!;
        // Pinned GetContinuousAnimFrames(speed3) =10, independently of the helper.
        for (var tick = 0; tick < 9; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 1, "Continuous animation does not advance before simulation tick10");
        AssertTrue(ReferenceEquals(original, _runtime.RenderedMap), "Counter-only ticks do not allocate a new rendered frame");
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_runtime.SimulationTicks, 10, "The test really reaches the authoritative tenth simulation tick");
        AssertEq(EventSprite()!.Frame, 2, "The real event sprite advances exactly at reference tick10");
        AssertFalse(original.Pixels.SequenceEqual(_runtime.RenderedMap!.Pixels), "The animation changes actual charset pixels");
        AssertPinnedCellPixels(3, 2, 1);
        for (var tick = 0; tick < 10; tick++) AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.Frame, 3, "The logical animation retains the reference fourth frame");
        AssertPinnedCellPixels(3, 1, 1); // Only the drawing clamps middle2 to middle.
    }
}
