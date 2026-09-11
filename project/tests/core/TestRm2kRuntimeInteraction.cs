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
        var fixture = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/rm2000");
        var game = new PluginGameInfo
        {
            GameDirectory = fixture,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(game);
        AssertTrue(started.Success, started.Error?.Message ?? "RM2K runtime start failed");
        AssertTrue(host.Runtime is Rm2kEngineRuntime, "RM2K runtime created");
        if (host.Runtime is not Rm2kEngineRuntime runtime)
        {
            return;
        }

        AssertTrue(runtime.Simulation.MapWidth >= 2 && runtime.Simulation.MapHeight >= 1,
            "fixture map is large enough for interaction regression");
        runtime.Simulation.ConfigureMap(
            runtime.Simulation.MapId,
            runtime.Simulation.MapWidth,
            runtime.Simulation.MapHeight,
            Enumerable.Repeat(true, runtime.Simulation.MapWidth * runtime.Simulation.MapHeight));
        runtime.Simulation.MapX = 0;
        runtime.Simulation.MapY = 0;

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

        var updated = host.Update(1.0 / 60.0);
        AssertTrue(updated.Success, updated.Error?.Message ?? "runtime update failed");
        AssertTrue(runtime.Simulation.Switches.Count >= 10 && runtime.Simulation.Switches[9],
            "queued Player Touch event executes on the simulation tick");
    }

    public void Test_SameLayerActionEventBlocksWithoutStartingAsTouch()
    {
        var fixture = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/rm2000");
        var game = new PluginGameInfo
        {
            GameDirectory = fixture,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        AssertTrue(host.Start(game).Success);
        if (host.Runtime is not Rm2kEngineRuntime runtime)
        {
            throw new InvalidOperationException("RM2K runtime was not created.");
        }

        runtime.Simulation.ConfigureMap(
            runtime.Simulation.MapId,
            runtime.Simulation.MapWidth,
            runtime.Simulation.MapHeight,
            Enumerable.Repeat(true, runtime.Simulation.MapWidth * runtime.Simulation.MapHeight));
        runtime.Simulation.MapX = 0;
        runtime.Simulation.MapY = 0;

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
    }
}
