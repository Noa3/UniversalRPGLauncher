using UniversalRPG.Rm2k.Input;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Applies one player input to the simulation in the verified order of EasyRPG
/// Player <c>Game_Player::UpdateNextMovementAction</c>, <c>UpdateMovement</c> and
/// <c>CheckActionEvent</c>. The class is deliberately free of Godot types so the
/// ordering can be regression tested without a running game.
/// </summary>
/// <remarks>
/// Two verified details are easy to get wrong:
/// a successful step triggers touched/collision on the player's <em>own</em> tile
/// (layer not same), while a blocked step triggers them on the tile <em>in front</em>
/// (layer same). Touch and collision never walk counter tiles.
/// </remarks>
public sealed class Rm2kPlayerTurn
{
    private readonly GameSimulationState _state;
    private readonly Rm2kEventScheduler _scheduler;

    public Rm2kPlayerTurn(GameSimulationState pState, Rm2kEventScheduler pScheduler)
    {
        _state = pState ?? throw new System.ArgumentNullException(nameof(pState));
        _scheduler = pScheduler ?? throw new System.ArgumentNullException(nameof(pScheduler));
    }

    /// <summary>
    /// Applies one resolved input action. Returns true when an event was
    /// triggered or the player moved.
    /// </summary>
    public bool Apply(Rm2kInputAction pAction)
    {
        if (_state.IsPaused || _state.IsMenuOpen)
        {
            return false;
        }
        if (pAction == Rm2kInputAction.Confirm)
        {
            // The Player toggles a vehicle before looking for events. This
            // runtime has no vehicles, so nothing can be toggled and the action
            // event check always runs.
            return _scheduler.CheckActionEvent();
        }

        var (deltaX, deltaY) = ToStep(pAction);
        if (deltaX == 0 && deltaY == 0)
        {
            return false;
        }
        if (_scheduler.ActiveInterpreterCount > 0)
        {
            // A running event page blocks movement, like Game_Map::IsRunning.
            return false;
        }
        if (_state.TryMove(deltaX, deltaY))
        {
            return _scheduler.TriggerTouchOrCollisionHere();
        }
        // The step was blocked, so the Player is still stopping and evaluates
        // touched/collision on the tile in front.
        return _scheduler.TriggerTouchOrCollisionFacing();
    }

    private static (int DeltaX, int DeltaY) ToStep(Rm2kInputAction pAction)
    {
        return pAction switch
        {
            Rm2kInputAction.MoveUp => (0, -1),
            Rm2kInputAction.MoveDown => (0, 1),
            Rm2kInputAction.MoveLeft => (-1, 0),
            Rm2kInputAction.MoveRight => (1, 0),
            _ => (0, 0),
        };
    }
}
