using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRm2kEventRuntimePositions : TestBase
{
    public void Test_SchedulerTracksRuntimePositionsWithoutMutatingMapEvents()
    {
        var state = new GameSimulationState();
        var scheduler = new Rm2kEventScheduler(state);
        var mapEvent = Event(7, 2, 3, Rm2kEventTrigger.Action);
        scheduler.SetEvents(new[] { mapEvent });

        AssertTrue(scheduler.TryGetEventPosition(7, out var x, out var y));
        AssertEq(x, 2);
        AssertEq(y, 3);

        AssertTrue(scheduler.TrySetEventPosition(7, 5, 6));
        AssertTrue(scheduler.TryGetEventPosition(7, out x, out y));
        AssertEq(x, 5);
        AssertEq(y, 6);
        AssertEq(mapEvent.X, 2, "parsed/start event X remains immutable runtime source metadata");
        AssertEq(mapEvent.Y, 3, "parsed/start event Y remains immutable runtime source metadata");
    }

    public void Test_TriggerAtUsesRuntimePosition()
    {
        var state = new GameSimulationState();
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[] { Event(1, 1, 1, Rm2kEventTrigger.Action) });
        AssertTrue(scheduler.TrySetEventPosition(1, 4, 2));

        AssertFalse(scheduler.TriggerAt(1, 1, Rm2kEventTrigger.Action));
        AssertTrue(scheduler.TriggerAt(4, 2, Rm2kEventTrigger.Action));
        AssertTrue(state.PlayerInputLocked, "foreground event at runtime coordinate locks input");
    }

    public void Test_BlockingCollisionUsesRuntimePositionAndCanIgnoreMover()
    {
        var state = new GameSimulationState();
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[]
        {
            Event(1, 1, 1, Rm2kEventTrigger.Action, layer: 1),
            Event(2, 3, 3, Rm2kEventTrigger.Action, layer: 1),
        });
        AssertTrue(scheduler.TrySetEventPosition(1, 2, 2));

        AssertFalse(scheduler.HasBlockingSameLayerEventAt(1, 1));
        AssertTrue(scheduler.HasBlockingSameLayerEventAt(2, 2));
        AssertFalse(scheduler.HasBlockingSameLayerEventAt(2, 2, pIgnoreEventId: 1));
        AssertTrue(scheduler.HasBlockingSameLayerEventAt(3, 3, pIgnoreEventId: 1));
    }

    public void Test_SnapshotIsDetachedFromSchedulerState()
    {
        var scheduler = new Rm2kEventScheduler(new GameSimulationState());
        scheduler.SetEvents(new[] { Event(3, 1, 2, Rm2kEventTrigger.Action) });

        var first = scheduler.SnapshotEventPositions();
        AssertEq(first[3].X, 1);
        AssertTrue(scheduler.TrySetEventPosition(3, 6, 7));

        AssertEq(first[3].X, 1, "previous snapshot remains immutable from later scheduler movement");
        var second = scheduler.SnapshotEventPositions();
        AssertEq(second[3].X, 6);
        AssertEq(second[3].Y, 7);
    }

    public void Test_SpriteAdapterUsesRuntimeEventPositionOverride()
    {
        var map = new Godot.Collections.Dictionary
        {
            { "width", 10 },
            { "height", 8 },
            {
                "events",
                new Godot.Collections.Array<Godot.Collections.Dictionary>
                {
                    new()
                    {
                        { "id", 9 },
                        { "name", "Walker" },
                        { "x", 1 },
                        { "y", 1 },
                    },
                }
            },
        };
        var runtimePositions = new Dictionary<int, (int X, int Y)>
        {
            [9] = (4, 5),
        };

        var result = new Rm2kSpriteAdapter().BuildDescriptors(map, 2, 2, runtimePositions);

        AssertTrue(result.Success, result.Error);
        AssertEq(result.Descriptors.Count, 2);
        var eventSprite = result.Descriptors.Find(pItem => pItem.Kind == Rm2kSpriteKind.Event && pItem.EventId == 9);
        AssertTrue(eventSprite != null);
        if (eventSprite == null) return;
        AssertEq(eventSprite.X, 4);
        AssertEq(eventSprite.Y, 5);
        AssertEq(eventSprite.Name, "Walker");
    }

    public void Test_SetEventsRejectsDuplicateEventIdsDeterministically()
    {
        var state = new GameSimulationState();
        var scheduler = new Rm2kEventScheduler(state);
        scheduler.SetEvents(new[]
        {
            Event(5, 1, 1, Rm2kEventTrigger.Action),
            Event(5, 7, 7, Rm2kEventTrigger.Action),
        });

        AssertEq(scheduler.EventCount, 1);
        AssertTrue(scheduler.TryGetEventPosition(5, out var x, out var y));
        AssertEq(x, 1);
        AssertEq(y, 1);
        AssertTrue(state.Diagnostics.Count > 0, "duplicate event ID is diagnosed rather than silently replacing the first event");
    }

    private static Rm2kMap.Event Event(
        int pId,
        int pX,
        int pY,
        Rm2kEventTrigger pTrigger,
        int layer = 1)
    {
        var mapEvent = new Rm2kMap.Event(pId, pX, pY);
        mapEvent.Pages.Add(new Rm2kMap.EventPage
        {
            Trigger = (int)pTrigger,
            Layer = layer,
        });
        return mapEvent;
    }
}
