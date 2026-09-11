using System;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Parser;

namespace UniversalRPG.Rm2k.Simulation;

public enum Rm2kMoveRouteStepStatus
{
    Advanced,
    SkippedBlockedMove,
    Blocked,
    CollisionTriggered,
    Completed,
    Unsupported,
    Faulted,
}

/// <summary>
/// Executes one decoded command per Step. The caller owns movement timing.
/// Runtime positions never modify the parsed LMU. Unsupported commands stop
/// this route rather than silently changing progression or timing.
/// </summary>
public sealed class Rm2kMoveRouteRunner
{
    public Rm2kMoveRouteRunner(int pEventId, Rm2kMoveRoute pRoute)
    {
        if (pEventId <= 0) throw new ArgumentOutOfRangeException(nameof(pEventId));
        if (pRoute == null) throw new ArgumentNullException(nameof(pRoute));
        if (pRoute.Commands == null || pRoute.Commands.Count > Rm2kMoveRouteDecoder.MaxCommands)
            throw new ArgumentException("Move route commands exceed the supported limit.", nameof(pRoute));
        var commands = new Rm2kMoveCommand[pRoute.Commands.Count];
        for (var index = 0; index < commands.Length; index++)
            commands[index] = pRoute.Commands[index] ?? throw new ArgumentException("Move route contains a null command.", nameof(pRoute));
        EventId = pEventId;
        Route = new Rm2kMoveRoute
        {
            Commands = Array.AsReadOnly(commands),
            Repeat = pRoute.Repeat,
            Skippable = pRoute.Skippable,
        };
    }

    public int EventId { get; }
    public Rm2kMoveRoute Route { get; }
    public int CommandIndex { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsFaulted { get; private set; }
    public string LastError { get; private set; } = "";

    public Rm2kMoveRouteStepStatus Step(
        GameSimulationState pState,
        Rm2kEventScheduler pScheduler,
        Rm2kPassabilityMap pPassability)
    {
        if (pState == null) throw new ArgumentNullException(nameof(pState));
        if (pScheduler == null) throw new ArgumentNullException(nameof(pScheduler));
        if (pPassability == null) throw new ArgumentNullException(nameof(pPassability));
        if (IsFaulted) return Rm2kMoveRouteStepStatus.Faulted;
        if (IsCompleted) return Rm2kMoveRouteStepStatus.Completed;
        if (!pScheduler.TryGetEventPosition(EventId, out _, out _))
            return Fault($"Move route references unknown event {EventId}.");
        if (Route.Commands.Count == 0)
        {
            IsCompleted = true;
            return Rm2kMoveRouteStepStatus.Completed;
        }
        if (CommandIndex < 0 || CommandIndex >= Route.Commands.Count)
            return Fault("Move-route cursor is outside the decoded route.");

        var command = Route.Commands[CommandIndex];
        switch (command.Code)
        {
            case Rm2kMoveCommandCode.MoveUp: return Move(pState, pScheduler, pPassability, 0, -1, 8);
            case Rm2kMoveCommandCode.MoveRight: return Move(pState, pScheduler, pPassability, 1, 0, 6);
            case Rm2kMoveCommandCode.MoveDown: return Move(pState, pScheduler, pPassability, 0, 1, 2);
            case Rm2kMoveCommandCode.MoveLeft: return Move(pState, pScheduler, pPassability, -1, 0, 4);
            case Rm2kMoveCommandCode.MoveForward:
                if (!pScheduler.TryGetEventFacing(EventId, out var facing)) return Fault("Event facing is unavailable.");
                var (dx, dy) = DeltaForFacing(facing);
                if (dx == 0 && dy == 0) return Fault($"Invalid event facing {facing}.");
                return Move(pState, pScheduler, pPassability, dx, dy, facing);
            case Rm2kMoveCommandCode.FaceUp: return SetFacing(pScheduler, 8);
            case Rm2kMoveCommandCode.FaceRight: return SetFacing(pScheduler, 6);
            case Rm2kMoveCommandCode.FaceDown: return SetFacing(pScheduler, 2);
            case Rm2kMoveCommandCode.FaceLeft: return SetFacing(pScheduler, 4);
            case Rm2kMoveCommandCode.TurnRight90: return Turn(pScheduler, true);
            case Rm2kMoveCommandCode.TurnLeft90: return Turn(pScheduler, false);
            case Rm2kMoveCommandCode.Turn180:
                if (!pScheduler.TryGetEventFacing(EventId, out var reverseFacing)) return Fault("Event facing is unavailable.");
                return SetFacing(pScheduler, reverseFacing switch { 2 => 8, 8 => 2, 4 => 6, 6 => 4, _ => 0 });
            case Rm2kMoveCommandCode.FaceHero: return FaceHero(pState, pScheduler, false);
            case Rm2kMoveCommandCode.FaceAwayFromHero: return FaceHero(pState, pScheduler, true);
            case Rm2kMoveCommandCode.LockFacing:
            case Rm2kMoveCommandCode.UnlockFacing:
                if (!pScheduler.TrySetEventFacingLocked(EventId, command.Code == Rm2kMoveCommandCode.LockFacing))
                    return Fault("Could not change event facing lock.");
                return Advance();
            case Rm2kMoveCommandCode.WalkEverywhereOn:
            case Rm2kMoveCommandCode.WalkEverywhereOff:
                if (!pScheduler.TrySetEventThrough(EventId, command.Code == Rm2kMoveCommandCode.WalkEverywhereOn))
                    return Fault("Could not change event through state.");
                return Advance();
            case Rm2kMoveCommandCode.SwitchOn: return SetSwitch(pState, command.ParameterA, true);
            case Rm2kMoveCommandCode.SwitchOff: return SetSwitch(pState, command.ParameterA, false);
            default:
                Fault($"RM2K move command '{command.Code}' is decoded but not yet implemented by the route runner.");
                pState.AddDiagnostic(LastError);
                return Rm2kMoveRouteStepStatus.Unsupported;
        }
    }

    private Rm2kMoveRouteStepStatus Move(
        GameSimulationState state, Rm2kEventScheduler scheduler, Rm2kPassabilityMap map,
        int dx, int dy, int facing)
    {
        if (!scheduler.TryGetEventPosition(EventId, out var x, out var y)) return Fault("Event position is unavailable.");
        // Validate before arithmetic: SDK/debug callers can supply invalid state.
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            return Fault("Move-route event position is outside the current map.");
        scheduler.TrySetEventFacing(EventId, facing);
        var targetX = x + dx;
        var targetY = y + dy;
        if (targetX < 0 || targetY < 0 || targetX >= map.Width || targetY >= map.Height)
            return HandleBlocked();

        if (!scheduler.IsEventThrough(EventId))
        {
            if (!map.CanMove(x, y, targetX, targetY)) return HandleBlocked();
            if (scheduler.TryGetActiveLayer(EventId, out var layer) && layer == 1)
            {
                if (targetX == state.MapX && targetY == state.MapY)
                {
                    var triggered = scheduler.TriggerCollision(EventId);
                    var blocked = HandleBlocked();
                    return triggered ? Rm2kMoveRouteStepStatus.CollisionTriggered : blocked;
                }
                if (scheduler.HasBlockingSameLayerEventAt(targetX, targetY, EventId)) return HandleBlocked();
            }
        }
        if (!scheduler.TrySetEventPosition(EventId, targetX, targetY)) return Fault("Could not update event position.");
        return Advance();
    }

    private Rm2kMoveRouteStepStatus SetFacing(Rm2kEventScheduler scheduler, int facing)
        => scheduler.TrySetEventFacing(EventId, facing) ? Advance() : Fault("Could not update event facing.");

    private Rm2kMoveRouteStepStatus Turn(Rm2kEventScheduler scheduler, bool right)
    {
        if (!scheduler.TryGetEventFacing(EventId, out var facing)) return Fault("Event facing is unavailable.");
        return SetFacing(scheduler, right
            ? facing switch { 2 => 4, 4 => 8, 8 => 6, 6 => 2, _ => 0 }
            : facing switch { 2 => 6, 6 => 8, 8 => 4, 4 => 2, _ => 0 });
    }

    private Rm2kMoveRouteStepStatus FaceHero(GameSimulationState state, Rm2kEventScheduler scheduler, bool away)
    {
        if (!scheduler.TryGetEventPosition(EventId, out var x, out var y)) return Fault("Event position is unavailable.");
        var dx = (long)state.MapX - x;
        var dy = (long)state.MapY - y;
        if (away) { dx = -dx; dy = -dy; }
        if (dx == 0 && dy == 0) return Advance();
        return SetFacing(scheduler, Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? 6 : 4) : (dy > 0 ? 2 : 8));
    }

    private Rm2kMoveRouteStepStatus SetSwitch(GameSimulationState state, int id, bool value)
    {
        if (id < 1 || id > GameSimulationState.MaxSwitches) return Fault($"Move route switch ID {id} is outside the supported range.");
        // Switch storage grows lazily elsewhere in the interpreter too. A valid
        // engine switch does not become invalid merely because it is still false.
        while (state.Switches.Count < id) state.Switches.Add(false);
        state.Switches[id - 1] = value;
        return Advance();
    }

    private Rm2kMoveRouteStepStatus HandleBlocked()
    {
        if (!Route.Skippable) return Rm2kMoveRouteStepStatus.Blocked;
        AdvanceCursor();
        return IsCompleted ? Rm2kMoveRouteStepStatus.Completed : Rm2kMoveRouteStepStatus.SkippedBlockedMove;
    }

    private Rm2kMoveRouteStepStatus Advance()
    {
        AdvanceCursor();
        return IsCompleted ? Rm2kMoveRouteStepStatus.Completed : Rm2kMoveRouteStepStatus.Advanced;
    }

    private void AdvanceCursor()
    {
        CommandIndex++;
        if (CommandIndex < Route.Commands.Count) return;
        if (Route.Repeat) CommandIndex = 0;
        else IsCompleted = true;
    }

    private Rm2kMoveRouteStepStatus Fault(string message)
    {
        LastError = message;
        IsFaulted = true;
        return Rm2kMoveRouteStepStatus.Faulted;
    }

    private static (int X, int Y) DeltaForFacing(int facing) => facing switch
    {
        2 => (0, 1), 4 => (-1, 0), 6 => (1, 0), 8 => (0, -1), _ => (0, 0),
    };
}
