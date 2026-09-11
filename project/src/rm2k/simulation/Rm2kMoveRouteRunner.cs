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
/// Executes one decoded RM2K move-route command at a time against runtime-owned
/// event state. Only commands with verified semantics are executed. Unsupported
/// commands stop the runner with an actionable error instead of being silently
/// ignored, because skipping them can alter event timing and progression.
/// </summary>
public sealed class Rm2kMoveRouteRunner
{
    public Rm2kMoveRouteRunner(int pEventId, Rm2kMoveRoute pRoute)
    {
        if (pEventId <= 0) throw new ArgumentOutOfRangeException(nameof(pEventId));
        EventId = pEventId;
        Route = pRoute ?? throw new ArgumentNullException(nameof(pRoute));
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
        {
            return Fault($"Move route references unknown event {EventId}.");
        }
        if (Route.Commands.Count == 0)
        {
            IsCompleted = true;
            return Rm2kMoveRouteStepStatus.Completed;
        }
        if (CommandIndex < 0 || CommandIndex >= Route.Commands.Count)
        {
            return Fault("Move-route command cursor is outside the decoded route.");
        }

        var command = Route.Commands[CommandIndex];
        switch (command.Code)
        {
            case Rm2kMoveCommandCode.MoveUp:
                return Move(pState, pScheduler, pPassability, 0, -1, 8);
            case Rm2kMoveCommandCode.MoveRight:
                return Move(pState, pScheduler, pPassability, 1, 0, 6);
            case Rm2kMoveCommandCode.MoveDown:
                return Move(pState, pScheduler, pPassability, 0, 1, 2);
            case Rm2kMoveCommandCode.MoveLeft:
                return Move(pState, pScheduler, pPassability, -1, 0, 4);
            case Rm2kMoveCommandCode.MoveForward:
                if (!pScheduler.TryGetEventFacing(EventId, out var facing))
                    return Fault($"Move route event {EventId} has no runtime facing state.");
                var (dx, dy) = DeltaForFacing(facing);
                if (dx == 0 && dy == 0) return Fault($"Move route event {EventId} has invalid facing {facing}.");
                return Move(pState, pScheduler, pPassability, dx, dy, facing);

            case Rm2kMoveCommandCode.FaceUp:
                return SetFacing(pScheduler, 8);
            case Rm2kMoveCommandCode.FaceRight:
                return SetFacing(pScheduler, 6);
            case Rm2kMoveCommandCode.FaceDown:
                return SetFacing(pScheduler, 2);
            case Rm2kMoveCommandCode.FaceLeft:
                return SetFacing(pScheduler, 4);
            case Rm2kMoveCommandCode.TurnRight90:
                return Turn(pScheduler, right: true);
            case Rm2kMoveCommandCode.TurnLeft90:
                return Turn(pScheduler, right: false);
            case Rm2kMoveCommandCode.Turn180:
                return Turn180(pScheduler);
            case Rm2kMoveCommandCode.FaceHero:
                return FaceHero(pState, pScheduler, away: false);
            case Rm2kMoveCommandCode.FaceAwayFromHero:
                return FaceHero(pState, pScheduler, away: true);

            case Rm2kMoveCommandCode.LockFacing:
                if (!pScheduler.TrySetEventFacingLocked(EventId, true)) return Fault("Could not lock event facing.");
                return Advance();
            case Rm2kMoveCommandCode.UnlockFacing:
                if (!pScheduler.TrySetEventFacingLocked(EventId, false)) return Fault("Could not unlock event facing.");
                return Advance();
            case Rm2kMoveCommandCode.WalkEverywhereOn:
                if (!pScheduler.TrySetEventThrough(EventId, true)) return Fault("Could not enable event through state.");
                return Advance();
            case Rm2kMoveCommandCode.WalkEverywhereOff:
                if (!pScheduler.TrySetEventThrough(EventId, false)) return Fault("Could not disable event through state.");
                return Advance();

            case Rm2kMoveCommandCode.SwitchOn:
                return SetSwitch(pState, command.ParameterA, true);
            case Rm2kMoveCommandCode.SwitchOff:
                return SetSwitch(pState, command.ParameterA, false);

            default:
                LastError = $"RM2K move command '{command.Code}' is decoded but not yet implemented by the route runner.";
                IsFaulted = true;
                pState.AddDiagnostic(LastError);
                return Rm2kMoveRouteStepStatus.Unsupported;
        }
    }

    private Rm2kMoveRouteStepStatus Move(
        GameSimulationState pState,
        Rm2kEventScheduler pScheduler,
        Rm2kPassabilityMap pPassability,
        int pDx,
        int pDy,
        int pFacing)
    {
        if (!pScheduler.TryGetEventPosition(EventId, out var x, out var y))
            return Fault($"Move route event {EventId} has no runtime position.");

        pScheduler.TrySetEventFacing(EventId, pFacing);
        var targetX = x + pDx;
        var targetY = y + pDy;
        var through = pScheduler.IsEventThrough(EventId);

        if (!through)
        {
            if (!pPassability.CanMove(x, y, targetX, targetY))
            {
                return HandleBlocked();
            }

            if (pScheduler.TryGetActiveLayer(EventId, out var layer) && layer == 1)
            {
                if (targetX == pState.PlayerX && targetY == pState.PlayerY)
                {
                    var triggered = pScheduler.TriggerCollision(EventId);
                    if (Route.Skippable)
                    {
                        AdvanceCursor();
                        if (triggered) return Rm2kMoveRouteStepStatus.CollisionTriggered;
                        return IsCompleted
                            ? Rm2kMoveRouteStepStatus.Completed
                            : Rm2kMoveRouteStepStatus.SkippedBlockedMove;
                    }
                    return triggered
                        ? Rm2kMoveRouteStepStatus.CollisionTriggered
                        : Rm2kMoveRouteStepStatus.Blocked;
                }
                if (pScheduler.HasBlockingSameLayerEventAt(targetX, targetY, EventId))
                {
                    return HandleBlocked();
                }
            }
        }
        else if (targetX < 0 || targetX >= pPassability.Width || targetY < 0 || targetY >= pPassability.Height)
        {
            return HandleBlocked();
        }

        if (!pScheduler.TrySetEventPosition(EventId, targetX, targetY))
            return Fault($"Could not update runtime position for event {EventId}.");
        return Advance();
    }

    private Rm2kMoveRouteStepStatus SetFacing(Rm2kEventScheduler pScheduler, int pFacing)
    {
        if (!pScheduler.TrySetEventFacing(EventId, pFacing)) return Fault("Could not update event facing.");
        return Advance();
    }

    private Rm2kMoveRouteStepStatus Turn(Rm2kEventScheduler pScheduler, bool right)
    {
        if (!pScheduler.TryGetEventFacing(EventId, out var facing)) return Fault("Event facing is unavailable.");
        var next = right
            ? facing switch { 2 => 4, 4 => 8, 8 => 6, 6 => 2, _ => 0 }
            : facing switch { 2 => 6, 6 => 8, 8 => 4, 4 => 2, _ => 0 };
        if (next == 0 || !pScheduler.TrySetEventFacing(EventId, next)) return Fault("Could not rotate event facing.");
        return Advance();
    }

    private Rm2kMoveRouteStepStatus Turn180(Rm2kEventScheduler pScheduler)
    {
        if (!pScheduler.TryGetEventFacing(EventId, out var facing)) return Fault("Event facing is unavailable.");
        var next = facing switch { 2 => 8, 8 => 2, 4 => 6, 6 => 4, _ => 0 };
        if (next == 0 || !pScheduler.TrySetEventFacing(EventId, next)) return Fault("Could not reverse event facing.");
        return Advance();
    }

    private Rm2kMoveRouteStepStatus FaceHero(
        GameSimulationState pState,
        Rm2kEventScheduler pScheduler,
        bool away)
    {
        if (!pScheduler.TryGetEventPosition(EventId, out var x, out var y)) return Fault("Event position is unavailable.");
        var dx = pState.PlayerX - x;
        var dy = pState.PlayerY - y;
        if (away)
        {
            dx = -dx;
            dy = -dy;
        }

        int facing;
        if (Math.Abs(dx) > Math.Abs(dy)) facing = dx >= 0 ? 6 : 4;
        else if (dy != 0) facing = dy >= 0 ? 2 : 8;
        else if (dx != 0) facing = dx >= 0 ? 6 : 4;
        else return Advance();

        if (!pScheduler.TrySetEventFacing(EventId, facing)) return Fault("Could not face event relative to hero.");
        return Advance();
    }

    private Rm2kMoveRouteStepStatus SetSwitch(GameSimulationState pState, int pSwitchId, bool pValue)
    {
        var index = pSwitchId - 1;
        if (pSwitchId < 1 || pSwitchId > GameSimulationState.MaxSwitches || index >= pState.Switches.Count)
        {
            return Fault($"Move route switch ID {pSwitchId} is outside the supported range.");
        }
        pState.Switches[index] = pValue;
        return Advance();
    }

    private Rm2kMoveRouteStepStatus HandleBlocked()
    {
        if (Route.Skippable)
        {
            AdvanceCursor();
            return IsCompleted ? Rm2kMoveRouteStepStatus.Completed : Rm2kMoveRouteStepStatus.SkippedBlockedMove;
        }
        return Rm2kMoveRouteStepStatus.Blocked;
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
        if (Route.Repeat)
        {
            CommandIndex = 0;
            return;
        }
        IsCompleted = true;
        CommandIndex = Route.Commands.Count;
    }

    private Rm2kMoveRouteStepStatus Fault(string pMessage)
    {
        LastError = pMessage;
        IsFaulted = true;
        return Rm2kMoveRouteStepStatus.Faulted;
    }

    private static (int X, int Y) DeltaForFacing(int pFacing) => pFacing switch
    {
        2 => (0, 1),
        4 => (-1, 0),
        6 => (1, 0),
        8 => (0, -1),
        _ => (0, 0),
    };
}
