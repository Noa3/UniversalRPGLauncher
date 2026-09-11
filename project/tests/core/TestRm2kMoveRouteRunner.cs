using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRm2kMoveRouteRunner : TestBase
{
    public void Test_MovesRuntimeCoordinatesWithoutMutatingParsedEvent()
    {
        var f = Fixture();
        var runner = Route(false, false, Rm2kMoveCommandCode.MoveRight, Rm2kMoveCommandCode.MoveDown);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Advanced);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
        Position(f.Scheduler, 1, 1);
        AssertEq(f.Event.X, 0);
        AssertEq(f.Event.Y, 0);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
    }

    public void Test_UnskippableBlockedStepRetriesWithoutAdvancing()
    {
        var f = Fixture(blocker: true);
        var runner = Route(false, false, Rm2kMoveCommandCode.MoveRight);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Blocked);
        AssertEq(runner.CommandIndex, 0);
        Position(f.Scheduler, 0, 0);
        f.Scheduler.TrySetEventThrough(2, true);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
        Position(f.Scheduler, 1, 0);
    }

    public void Test_SkippableBlockedStepAdvancesExactlyOnce()
    {
        var f = Fixture(blocker: true);
        var runner = Route(false, true, Rm2kMoveCommandCode.MoveRight, Rm2kMoveCommandCode.MoveDown);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.SkippedBlockedMove);
        AssertEq(runner.CommandIndex, 1);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
        Position(f.Scheduler, 0, 1);
    }

    public void Test_PlayerContactStartsCollisionWithoutMovingEvent()
    {
        var f = Fixture(trigger: Rm2kEventTrigger.Collision);
        f.State.MapX = 1;
        f.State.MapY = 0;
        var runner = Route(false, false, Rm2kMoveCommandCode.MoveRight);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.CollisionTriggered);
        AssertTrue(f.Scheduler.ForegroundBusy);
        AssertTrue(f.State.PlayerInputLocked);
        AssertEq(runner.CommandIndex, 0);
        Position(f.Scheduler, 0, 0);
    }

    public void Test_SkippablePlayerContactCompletesAndDoesNotRepeatCollision()
    {
        var f = Fixture(trigger: Rm2kEventTrigger.Collision);
        f.State.MapX = 1;
        f.State.MapY = 0;
        var runner = Route(false, true, Rm2kMoveCommandCode.MoveRight);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.CollisionTriggered);
        AssertTrue(runner.IsCompleted);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
        Position(f.Scheduler, 0, 0);
    }

    public void Test_ThroughIgnoresTileCollisionButNeverMapBounds()
    {
        var f = Fixture(blockAllTiles: true);
        var runner = Route(false, false, Rm2kMoveCommandCode.WalkEverywhereOn,
            Rm2kMoveCommandCode.MoveRight, Rm2kMoveCommandCode.MoveUp);
        runner.Step(f.State, f.Scheduler, f.Map);
        AssertTrue(f.Scheduler.IsEventThrough(1));
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Advanced);
        Position(f.Scheduler, 1, 0);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Blocked);
        Position(f.Scheduler, 1, 0);
    }

    public void Test_FacingLockDoesNotPreventMovement()
    {
        var f = Fixture();
        f.Scheduler.TrySetEventPosition(1, 0, 1);
        var runner = Route(false, false, Rm2kMoveCommandCode.LockFacing,
            Rm2kMoveCommandCode.MoveRight, Rm2kMoveCommandCode.UnlockFacing,
            Rm2kMoveCommandCode.FaceUp, Rm2kMoveCommandCode.MoveForward);
        runner.Step(f.State, f.Scheduler, f.Map);
        runner.Step(f.State, f.Scheduler, f.Map);
        AssertTrue(f.Scheduler.TryGetEventFacing(1, out var facing));
        AssertEq(facing, 2);
        Position(f.Scheduler, 1, 1);
        runner.Step(f.State, f.Scheduler, f.Map);
        runner.Step(f.State, f.Scheduler, f.Map);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
        Position(f.Scheduler, 1, 0);
    }

    public void Test_FaceHeroUsesCanonicalPlayerMapCoordinates()
    {
        var f = Fixture();
        f.State.MapX = 3;
        f.State.MapY = 0;
        var runner = Route(false, false, Rm2kMoveCommandCode.FaceHero, Rm2kMoveCommandCode.FaceAwayFromHero);
        runner.Step(f.State, f.Scheduler, f.Map);
        f.Scheduler.TryGetEventFacing(1, out var towards);
        AssertEq(towards, 6);
        runner.Step(f.State, f.Scheduler, f.Map);
        f.Scheduler.TryGetEventFacing(1, out var away);
        AssertEq(away, 4);
    }

    public void Test_ValidSwitchIdsGrowLazilyAllocatedStorage()
    {
        var f = Fixture();
        var runner = new Rm2kMoveRouteRunner(1, new Rm2kMoveRoute
        {
            Repeat = false,
            Commands = new[]
            {
                new Rm2kMoveCommand { Code = Rm2kMoveCommandCode.SwitchOn, ParameterA = 11 },
                new Rm2kMoveCommand { Code = Rm2kMoveCommandCode.SwitchOff, ParameterA = 11 },
            },
        });
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Advanced);
        AssertEq(f.State.Switches.Count, 11);
        AssertTrue(f.State.Switches[10]);
        AssertFalse(f.State.Switches[0]);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
        AssertFalse(f.State.Switches[10]);
    }

    public void Test_InvalidSwitchDoesNotModifyState()
    {
        foreach (var id in new[] { int.MinValue, 0, GameSimulationState.MaxSwitches + 1 })
        {
            var f = Fixture();
            var runner = new Rm2kMoveRouteRunner(1, new Rm2kMoveRoute
            {
                Commands = new[] { new Rm2kMoveCommand { Code = Rm2kMoveCommandCode.SwitchOn, ParameterA = id } },
            });
            AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Faulted);
            AssertEq(f.State.Switches.Count, 0);
            AssertEq(runner.CommandIndex, 0);
        }
    }

    public void Test_RepeatingAndEmptyRoutesRemainBounded()
    {
        var f = Fixture();
        var repeating = Route(true, false, Rm2kMoveCommandCode.FaceRight);
        for (var i = 0; i < 4; i++)
        {
            AssertEq(repeating.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Advanced);
            AssertEq(repeating.CommandIndex, 0);
        }
        AssertFalse(repeating.IsCompleted);
        var empty = Route(true, false);
        AssertEq(empty.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
    }

    public void Test_UnsupportedCommandsStopWithoutAdvancing()
    {
        var f = Fixture();
        var runner = Route(false, true, Rm2kMoveCommandCode.Wait, Rm2kMoveCommandCode.MoveRight);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Unsupported);
        AssertEq(runner.CommandIndex, 0);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Faulted);
        Position(f.Scheduler, 0, 0);
    }

    public void Test_InvalidRuntimeCoordinatesFailBeforeArithmetic()
    {
        var f = Fixture();
        f.Scheduler.TrySetEventPosition(1, int.MaxValue, int.MinValue);
        var runner = Route(false, false, Rm2kMoveCommandCode.MoveRight);
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Faulted);
        AssertTrue(runner.LastError.Contains("outside", StringComparison.Ordinal));
    }

    public void Test_RunnerSnapshotsCallerOwnedCommandList()
    {
        var f = Fixture();
        var commands = new List<Rm2kMoveCommand> { new() { Code = Rm2kMoveCommandCode.MoveRight } };
        var runner = new Rm2kMoveRouteRunner(1, new Rm2kMoveRoute { Commands = commands, Repeat = false });
        commands.Clear();
        AssertEq(runner.Step(f.State, f.Scheduler, f.Map), Rm2kMoveRouteStepStatus.Completed);
        Position(f.Scheduler, 1, 0);
    }

    private void Position(Rm2kEventScheduler scheduler, int x, int y)
    {
        AssertTrue(scheduler.TryGetEventPosition(1, out var actualX, out var actualY));
        AssertEq(actualX, x);
        AssertEq(actualY, y);
    }

    private static Rm2kMoveRouteRunner Route(bool repeat, bool skippable, params Rm2kMoveCommandCode[] codes)
    {
        var commands = new List<Rm2kMoveCommand>();
        foreach (var code in codes) commands.Add(new Rm2kMoveCommand { Code = code });
        return new Rm2kMoveRouteRunner(1, new Rm2kMoveRoute { Commands = commands, Repeat = repeat, Skippable = skippable });
    }

    private static (GameSimulationState State, Rm2kEventScheduler Scheduler, Rm2kPassabilityMap Map, Rm2kMap.Event Event)
        Fixture(bool blocker = false, bool blockAllTiles = false, Rm2kEventTrigger trigger = Rm2kEventTrigger.Action)
    {
        var state = new GameSimulationState { MapX = 3, MapY = 3 };
        var e = new Rm2kMap.Event(1, 0, 0);
        e.Pages.Add(new Rm2kMap.EventPage { Layer = 1, Trigger = (int)trigger });
        var events = new List<Rm2kMap.Event> { e };
        if (blocker)
        {
            var other = new Rm2kMap.Event(2, 1, 0);
            other.Pages.Add(new Rm2kMap.EventPage { Layer = 1, Trigger = (int)Rm2kEventTrigger.Action });
            events.Add(other);
        }
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(events);
        var lower = new byte[Rm2kPassabilityMap.LowerPassageCount];
        var upper = new byte[Rm2kPassabilityMap.UpperPassageCount];
        Array.Fill(lower, blockAllTiles ? (byte)0 : (byte)0x0F);
        Array.Fill(upper, (byte)0x1F);
        var database = new Godot.Collections.Dictionary
        {
            { "chipsets", new Godot.Collections.Array<Godot.Collections.Dictionary>
                { new() { { "id", 1 }, { "passable_data_lower", lower }, { "passable_data_upper", upper } } } },
        };
        var down = new int[16]; var up = new int[16];
        Array.Fill(down, 2000); Array.Fill(up, 10000);
        var map = new Godot.Collections.Dictionary
        {
            { "width", 4 }, { "height", 4 }, { "chipset_id", 1 },
            { "lower_layer", down }, { "upper_layer", up },
        };
        if (!Rm2kPassabilityMap.TryCreate(database, map, out var passage, out var error) || passage == null)
            throw new InvalidOperationException(error);
        return (state, scheduler, passage, e);
    }
}
