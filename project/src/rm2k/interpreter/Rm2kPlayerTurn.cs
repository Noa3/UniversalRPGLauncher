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
        if (_state.IsPaused || _state.IsMenuOpen || _scheduler.IsMessageActive)
        {
            return false;
        }
        if (pAction == Rm2kInputAction.Confirm)
        {
            // All waiting interpreters observe a fresh decision. Only a main
            // page consumes the player turn; parallel waits remain background work.
            var decisionAccepted = _scheduler.HasDecisionWait && _scheduler.SubmitDecision();
            if (_scheduler.HasBlockingInterpreter)
            {
                return decisionAccepted;
            }
            // The Player tries the vehicle before it looks for events:
            //   if (Input::IsTriggered(Input::DECISION)) {
            //       if (!GetOnOffVehicle()) { CheckActionEvent(); }
            //   }
            //
            // **The comment that used to sit here said "this runtime has no
            // vehicles, so nothing can be toggled and the action event check
            // always runs" — and that sentence was the bug.**
            // `Rm2kDecisionTurn` was built and tested against it, and the
            // vehicles were then loaded and drawn, and the sentence stayed. So
            // the decision key could not board a boat, could not leave a ship,
            // and the action event check always won — on a boat tile.
            //
            // **A vehicle that takes the turn suppresses the action event**,
            // and that is the whole point of the order: a boat moored beside a
            // sign has to be boardable, and the sign is on the tile the player
            // is facing.
            if (_state.Boarding == null
                && _state.Vehicles.Count > 0)
            {
                // The first boarding needs a boarding state to write into. One
                // is created here and not by every reader, so a game that never
                // touches a vehicle never allocates one.
                _state.Boarding = new Rm2kVehicleBoarding();
            }
            var outcome = Rm2kDecisionTurn.Run(
                _state.Boarding,
                _state.Vehicles,
                _state.MapId,
                _state.MapX, _state.MapY,
                _state.FacingDirection, _state.FacingDirection,
                _state.HeroMoveSpeed,
                pPlayerIsStopping: true,
                pAirshipIsStopping: IsVehicleStopping(
                    _state.Vehicles, Rm2kVehicle.Airship),
                pCanEmbark: CanEmbark(_state),
                pCanDisembark: CanDisembark(_state),
                pBoardAirship: (pVehicle, pMapId, pX, pY) =>
                {
                    _state.Boarding!.BoardAirship(
                        pVehicle, _state.HeroMoveSpeed,
                        pPlayerIsStopping: true);
                    pVehicle.StartAscent();
                },
                pBeginEmbark: (pType, pSpeed, pTargetX, pTargetY) =>
                    _state.Boarding!.BeginEmbark(
                        pType, pSpeed, pTargetX, pTargetY),
                pBeginDisembark: (pType, pTargetX, pTargetY) =>
                {
                    _state.Boarding!.BeginDisembark(pTargetX, pTargetY);
                    _state.HeroMoveSpeed =
                        _state.Boarding.PreboardMoveSpeed;
                },
                pBeginAirshipDisembark: (pType) =>
                {
                    _state.Boarding!.BeginAirshipDisembark();
                    FindVehicle(_state.Vehicles, pType)?.StartDescent();
                });
            if (outcome == Rm2kDecisionTurn.DecisionOutcome.HandledByVehicle)
            {
                // A boarding or a disembarking is a movement, not an event, and
                // the turn ends here — which is also why boarding does not count
                // as a step towards an encounter.
                return true;
            }
            return _scheduler.CheckActionEvent() || decisionAccepted;
        }

        // **Und  jetzt  die  Menuetaste** -- **und  sie  gehoert  vor
        //  die  Bewegung**, -- **weil  die  Referenz  sie  an  erster
        //  Stelle  behandelt.**
        //
        // **Und  die  Reihenfolge  kommt  aus  EasyRPG Players
        //  `Game_Player::UpdateNextMovementAction`:**
        //
        // <code>
        /// if (Game_Map::GetInterpreter().IsRunning()) { SetMenuCalling(false); return; }
        /// if (IsPaused() || IsMoveRouteOverwritten() || Game_Message::IsMessageActive()) { return; }
        /// ...
        /// if (IsMenuCalling()) {
        ///     SetMenuCalling(false);
        ///     ResetAnimation();
        ///     game_system->SePlay(game_system->GetSystemSE(SFX_Decision));
        ///     Game_Map::GetInterpreter().RequestMainMenuScene();
        ///     return;
        /// }
        /// </code>
        //
        // **Und  `RequestMainMenuScene`  ist  genau  das,  was  der
        //  Interpreter  hier  fuer  `11910`  bereits  tut**, -- **und
        //  damit  ist  der  Weg  der  Taste  derselbe  Weg  des
        //  Befehls  und  nicht  ein  zweiter.**
        if (pAction == Rm2kInputAction.Menu)
        {
            if (_scheduler.HasBlockingInterpreter
                || _state.WaitingFor
                    != GameSimulationState.WaitReason.None)
            {
                // **Und  eine  laufende  Seite  bricht  den  Menueaufruf
                //  ab** -- **denn  die  Referenz  setzt  das  Anfordern
                //  zurueck  und  kehrt  zurueck.**
                return false;
            }

            if (_state.IsPaused || _state.IsMenuOpen)
            {
                return false;
            }

            if (!_state.AllowMenu)
            {
                _state.AddDiagnostic(
                    "RM2K the menu key was pressed and the game"
                    + " forbade the menu, from 11960");
                return false;
            }

            _state.IsMainMenuActive = true;
            _state.WaitingFor =
                GameSimulationState.WaitReason.MainMenuOpen;
            _state.AddDiagnostic(
                "RM2K the menu key opened the main menu, the same"
                + " request 11910 makes");
            return true;
        }

        var (deltaX, deltaY) = ToStep(pAction);
        if (deltaX == 0 && deltaY == 0)
        {
            return false;
        }
        if (_scheduler.HasBlockingInterpreter)
        {
            // The main map interpreter blocks movement, not parallel pages.
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

    private static Rm2kVehicleState? FindVehicle(
        System.Collections.Generic.IReadOnlyList<Rm2kVehicleState> pVehicles,
        int pType)
    {
        foreach (var vehicle in pVehicles)
        {
            if (vehicle.VehicleType == pType)
            {
                return vehicle;
            }
        }
        return null;
    }

    /// <summary>
    /// Whether a vehicle is standing still, from <c>IsStopping</c>. The airship
    /// check needs this because boarding a drifting airship is not a thing the
    /// format allows.
    /// </summary>
    private static bool IsVehicleStopping(
        System.Collections.Generic.IReadOnlyList<Rm2kVehicleState> pVehicles,
        int pType)
    {
        var vehicle = FindVehicle(pVehicles, pType);
        // A vehicle that is not on this map is not moving through it, so it
        // counts as stopping rather than as moving.
        return vehicle == null || !vehicle.IsAscendingOrDescending;
    }

    /// <summary>
    /// Whether the water tile the player faces allows boarding, from
    /// <c>Game_Map::CanEmbarkShip</c>. A tile that is not a ship tile is
    /// refused by the caller; this is only the passability half.
    /// </summary>
    /// <remarks>
    /// The bit is <c>DirectionBit(0, 1)</c> — down, away from the player — and
    /// <strong>not a literal</strong>. A first draft wrote <c>0x08</c> for
    /// "down" and <c>0x08</c> for the opposite direction, which was wrong
    /// twice over: <c>Rm2kChipset.PassDown</c> is <c>0x01</c> and
    /// <c>PassUp</c> is <c>0x08</c>, and a bit that is written rather than
    /// derived is a bit that a future chip layout silently breaks.
    /// </remarks>
    private static bool CanEmbark(GameSimulationState pState)
    {
        var (frontX, frontY) = pState.FrontTile(
            pState.MapX, pState.MapY, pState.FacingDirection);
        return pState.IsPassableInDirection(
            frontX, frontY, Rm2kChipset.PassDown);
    }

    /// <summary>
    /// Whether the tile in front allows stepping off a boat or a ship, from
    /// <c>Game_Map::CanDisembarkShip</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Stepping off reads the direction that points back at the
    /// player</strong>, which is the opposite of the one they face — and a
    /// mutation that swapped the two passed every test in the file, because
    /// the fixture made every tile passable in all four directions. So the two
    /// rules were told apart only once the fixture had a tile that is open one
    /// way and shut the other.
    /// </remarks>
    private static bool CanDisembark(GameSimulationState pState)
    {
        var (frontX, frontY) = pState.FrontTile(
            pState.MapX, pState.MapY, pState.FacingDirection);
        return pState.IsPassableInDirection(
            frontX, frontY, OppositeBit(pState.FacingDirection));
    }

    /// <summary>
    /// The passability bit for the direction opposite to one the player faces,
    /// from the <c>XwithDirection</c> order.
    /// </summary>
    /// <remarks>
    /// The direction numbering is 2 down, 4 left, 6 right, 8 up — and
    /// <strong>not</strong> the liblcf event order 0 up, 1 right, 2 down,
    /// 3 left that <c>Rm2kDecisionTurn</c> documents. A first draft wrote a
    /// 0..3 table, and on a real map every one of its four cases named the
    /// wrong tile. <c>TileInFront</c> takes the 2/4/6/8 form, so a table in the
    /// other order is not a second opinion — it is a different direction.
    /// </remarks>
    private static byte OppositeBit(byte pDirection)
    {
        // The way back is the way the player came from, and it is found by
        // negating the delta rather than by adding to the direction number.
        //
        // **A first draft added 2**, because up is 0 and down is 2, and that
        // turns a step to the left (3) into 5 — which is down-right, a
        // diagonal the game does not have a passability bit for. Negating is
        // correct for all four cardinals and needs no table.
        //
        // The delta itself has to come through the bridge, because
        // <c>DirectionDelta</c> speaks the event order and the player speaks
        // the facing order. A first draft also wrote "down" as 0x08 when
        // <c>Rm2kChipset.PassDown</c> is 0x01, so the rule was wrong twice —
        // and a fixture with every tile open in every direction saw neither.
        var (dx, dy) = Rm2kMoveRoute.DirectionDelta(
            Rm2kMoveRoute.LiblcfFromFacingDirection(pDirection));
        if (dx > 0)
        {
            return Rm2kChipset.PassLeft;
        }
        if (dx < 0)
        {
            return Rm2kChipset.PassRight;
        }
        if (dy > 0)
        {
            return Rm2kChipset.PassUp;
        }
        if (dy < 0)
        {
            return Rm2kChipset.PassDown;
        }
        return 0;
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
