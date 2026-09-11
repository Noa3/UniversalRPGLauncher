using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kRuntimeInteraction : TestBase
{
    public void Test_SameLayerPlayerTouchBlocksMovementAndStartsEvent()
    {
        using var host = StartRuntime();
        var runtime = RequireRuntime(host);
        ConfigureOpenTwoTileMap(runtime);

        var blockingEvent = new Rm2kMap.Event(900, 1, 0);
        var page = new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Touch,
            Layer = 1,
        };
        page.Commands.Add(new Rm2kMap.EventCommand(
            EventInterpreter.ControlSwitches,
            new List<int> { 10, 10, 0, EventInterpreter.SwitchModeOn }));
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        blockingEvent.Pages.Add(page);
        runtime.EventScheduler.SetEvents(new[] { blockingEvent });

        var moved = runtime.TryMove(1, 0);

        AssertFalse(moved, "same-layer event blocks the player step");
        AssertEq(runtime.Simulation.MapX, 0, "player X remains unchanged on collision");
        AssertEq(runtime.Simulation.MapY, 0, "player Y remains unchanged on collision");
        AssertEq(runtime.Simulation.FacingDirection, (byte)6, "player still faces the collided event");
        AssertEq(runtime.EventScheduler.ActiveInterpreterCount, 1,
            "Player Touch event is queued even though movement was blocked");
        AssertTrue(runtime.Simulation.PlayerInputLocked,
            "foreground Player Touch event immediately owns the input lock");

        var updated = host.Update(1.0 / 60.0);
        AssertTrue(updated.Success, updated.Error?.Message ?? "runtime update failed");
        AssertTrue(runtime.Simulation.Switches.Count >= 10 && runtime.Simulation.Switches[9],
            "queued Player Touch event executes on the simulation tick");
    }

    public void Test_SameLayerActionEventBlocksWithoutStartingAsTouch()
    {
        using var host = StartRuntime();
        var runtime = RequireRuntime(host);
        ConfigureOpenTwoTileMap(runtime);

        var eventData = new Rm2kMap.Event(901, 1, 0);
        eventData.Pages.Add(new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Action,
            Layer = 1,
        });
        runtime.EventScheduler.SetEvents(new[] { eventData });

        AssertFalse(runtime.TryMove(1, 0), "same-layer action event blocks movement");
        AssertEq(runtime.EventScheduler.ActiveInterpreterCount, 0,
            "action event is not incorrectly started as Player Touch");
        AssertFalse(runtime.Simulation.PlayerInputLocked,
            "pure collision with an action event does not lock input");
    }

    public void Test_SuccessfulStepTriggersBelowLayerPlayerTouchInsideRuntime()
    {
        using var host = StartRuntime();
        var runtime = RequireRuntime(host);
        ConfigureOpenTwoTileMap(runtime);

        var touchEvent = new Rm2kMap.Event(902, 1, 0);
        var page = new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Touch,
            Layer = 0,
        };
        page.Commands.Add(new Rm2kMap.EventCommand(
            EventInterpreter.ControlSwitches,
            new List<int> { 11, 11, 0, EventInterpreter.SwitchModeOn }));
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        touchEvent.Pages.Add(page);
        runtime.EventScheduler.SetEvents(new[] { touchEvent });

        AssertTrue(runtime.TryMove(1, 0), "below-layer touch event does not block the step");
        AssertEq(runtime.Simulation.MapX, 1);
        AssertEq(runtime.EventScheduler.ActiveInterpreterCount, 1,
            "successful step queues Player Touch without UI assistance");
        AssertTrue(runtime.Simulation.PlayerInputLocked,
            "queued Player Touch foreground event locks further player movement");

        AssertTrue(host.Update(1.0 / 60.0).Success);
        AssertTrue(runtime.Simulation.Switches.Count >= 11 && runtime.Simulation.Switches[10],
            "runtime-owned post-step touch event executes");
    }

    public void Test_TryInteractTargetsFacingTileInsideRuntime()
    {
        using var host = StartRuntime();
        var runtime = RequireRuntime(host);
        ConfigureOpenTwoTileMap(runtime);
        runtime.Simulation.FacingDirection = 6;

        var actionEvent = new Rm2kMap.Event(903, 1, 0);
        var page = new Rm2kMap.EventPage
        {
            Trigger = (int)Rm2kEventTrigger.Action,
            Layer = 1,
        };
        page.Commands.Add(new Rm2kMap.EventCommand(
            EventInterpreter.ControlSwitches,
            new List<int> { 12, 12, 0, EventInterpreter.SwitchModeOn }));
        page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
        actionEvent.Pages.Add(page);
        runtime.EventScheduler.SetEvents(new[] { actionEvent });

        AssertTrue(runtime.TryInteract(), "runtime targets the tile in front of the player");
        AssertTrue(runtime.Simulation.PlayerInputLocked,
            "action event becomes the serialized foreground interpreter");
        AssertFalse(runtime.TryInteract(), "another action cannot start while foreground is busy");

        AssertTrue(host.Update(1.0 / 60.0).Success);
        AssertTrue(runtime.Simulation.Switches.Count >= 12 && runtime.Simulation.Switches[11]);
    }

    public void Test_ForegroundInputLockPreventsMoveAndFacingMutation()
    {
        using var host = StartRuntime();
        var runtime = RequireRuntime(host);
        ConfigureOpenTwoTileMap(runtime);
        runtime.Simulation.FacingDirection = 2;
        runtime.Simulation.PlayerInputLocked = true;

        AssertFalse(runtime.TryMove(1, 0));
        AssertEq(runtime.Simulation.MapX, 0);
        AssertEq(runtime.Simulation.FacingDirection, (byte)2,
            "locked input is rejected before runtime movement changes facing");
        AssertFalse(runtime.TryInteract(), "locked foreground state rejects map interaction");
    }

    private static EnginePluginHost StartRuntime()
    {
        var fixture = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/rm2000");
        var game = new PluginGameInfo
        {
            GameDirectory = fixture,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        };
        var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(game);
        if (!started.Success)
        {
            host.Dispose();
            throw new InvalidOperationException(started.Error?.Message ?? "RM2K runtime start failed");
        }
        return host;
    }

    private static Rm2kEngineRuntime RequireRuntime(EnginePluginHost pHost)
    {
        if (pHost.Runtime is not Rm2kEngineRuntime runtime)
        {
            throw new InvalidOperationException("RM2K runtime was not created.");
        }
        return runtime;
    }

    private static void ConfigureOpenTwoTileMap(Rm2kEngineRuntime pRuntime)
    {
        AssertMapIsLargeEnough(pRuntime);
        pRuntime.Simulation.ConfigureMap(
            pRuntime.Simulation.MapId,
            pRuntime.Simulation.MapWidth,
            pRuntime.Simulation.MapHeight,
            Enumerable.Repeat(true, pRuntime.Simulation.MapWidth * pRuntime.Simulation.MapHeight));
        pRuntime.Simulation.MapX = 0;
        pRuntime.Simulation.MapY = 0;
        pRuntime.Simulation.PlayerInputLocked = false;
    }

    private static void AssertMapIsLargeEnough(Rm2kEngineRuntime pRuntime)
    {
        if (pRuntime.Simulation.MapWidth < 2 || pRuntime.Simulation.MapHeight < 1)
        {
            throw new InvalidOperationException("RM2K fixture map is too small for interaction regression tests.");
        }
    }
}
