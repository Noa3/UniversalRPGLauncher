using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the decision turn order from the tail of
/// <c>Game_Player::UpdateMove</c>.
/// </summary>
/// <remarks>
/// The two things worth proving are the precedence and the early return. Vehicle
/// boarding has to win over an action event on the same tile, and the turn has
/// to end either way so the step counter is not touched. An implementation that
/// checks the action event first makes a boat with an event beside it
/// unboardable; one that falls through to the step counter lets the player farm
/// encounters by boarding.
/// </remarks>
public partial class TestRm2kDecisionTurn : TestBase
{
	private static List<Rm2kVehicleState> Fleet(int pMapId = 3)
	{
		return new List<Rm2kVehicleState>
		{
			new Rm2kVehicleState(Rm2kVehicle.Boat, pMapId, 11, 10),
			new Rm2kVehicleState(Rm2kVehicle.Ship, pMapId, 11, 9),
			new Rm2kVehicleState(Rm2kVehicle.Airship, pMapId, 40, 10),
		};
	}

	/// <summary>Runs the turn with the two refusals switched off.</summary>
	private static Rm2kDecisionTurn.DecisionOutcome Decision(
		Rm2kVehicleBoarding pBoarding, IReadOnlyList<Rm2kVehicleState> pFleet,
		int pX, int pY, int pDirection,
		bool pAirshipIsStopping = true, bool pCanEmbark = true, bool pCanDisembark = true,
		int pFacing = 2)
	{
		return Rm2kDecisionTurn.Run(
			pBoarding, pFleet, 3, pX, pY, pDirection, pFacing,
			pPlayerMoveSpeed: 3, pPlayerIsStopping: true,
			pAirshipIsStopping: pAirshipIsStopping,
			pCanEmbark: pCanEmbark, pCanDisembark: pCanDisembark);
	}

	public void Test_ADiagonalBecomesTheFacingBeforeAnythingElseHappens()
	{
		// GetOnOffVehicle starts with the diagonal correction, because
		// GetOffVehicle asserts there is no diagonal. A diagonal of 4 to 7 is
		// therefore never acted on directly.
		AssertTrue(Rm2kDecisionTurn.IsDirectionDiagonal(4), "4 is diagonal");
		AssertTrue(Rm2kDecisionTurn.IsDirectionDiagonal(7), "and so is 7");
		AssertTrue(!Rm2kDecisionTurn.IsDirectionDiagonal(0), "0 is up and cardinal");
		AssertTrue(!Rm2kDecisionTurn.IsDirectionDiagonal(3), "and so is left");

		AssertEq(Rm2kDecisionTurn.ResolveDirection(2, 1, out var keptReplaced), 2,
			"a cardinal direction is used as it is");
		AssertTrue(!keptReplaced, "and is not replaced");

		AssertEq(Rm2kDecisionTurn.ResolveDirection(4, 3, out var replaced), 3,
			"a diagonal is replaced by the facing");
		AssertTrue(replaced, "and the caller can see that it was");
	}

	public void Test_ADiagonalFacesTheBoatRatherThanTheDiagonalNeighbour()
	{
		// A boat down-right of the player, the player facing down-right. The
		// correction turns the direction down, so the boat straight below is what
		// gets boarded. Without the correction the tile in front would be the
		// diagonal neighbour, which is not where XwithDirection points.
		var fleet = Fleet();
		var boarding = new Rm2kVehicleBoarding();
		var outcome = Decision(boarding, fleet, pX: 10, pY: 10, pDirection: 4, pFacing: 3);

		AssertEq(outcome, Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"with the facing left there is no boat to the left, so nothing is boarded");

		// With the facing right from 10,10 the tile in front is 11,10, where the
		// boat is, so the corrected direction is what finds it. Without the
		// correction the diagonal neighbour would be looked at instead and the
		// boat missed.
		var right = new Rm2kVehicleBoarding();
		AssertEq(Decision(right, fleet, 10, 10, pDirection: 4, pFacing: 1),
			Rm2kDecisionTurn.DecisionOutcome.HandledByVehicle,
			"with the facing right the corrected direction does find the boat");
	}

	public void Test_ABoardingVehicleWinsOverTheActionEventCheck()
	{
		// The Player runs GetOnOffVehicle first and only calls CheckActionEvent
		// when it returns false, so a boat is boarded even when an action event
		// stands on the same tile.
		var fleet = Fleet();
		var boarding = new Rm2kVehicleBoarding();
		var boarded = Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent;
		var began = string.Empty;

		var outcome = Rm2kDecisionTurn.Run(
			boarding, fleet, 3, 10, 10, pDirection: 1, pFacing: 2,
			pPlayerMoveSpeed: 3, pPlayerIsStopping: true, pAirshipIsStopping: true,
			pCanEmbark: true, pCanDisembark: true,
			pBeginEmbark: (type, speed, x, y) =>
			{
				began = $"type={type} speed={speed} tile={x},{y}";
			});

		AssertEq(outcome, Rm2kDecisionTurn.DecisionOutcome.HandledByVehicle,
			"the boat is boarded, so the action event check does not run");
		AssertEq(began, "type=1 speed=3 tile=11,10",
			"and the embark is a boat, with the player's own speed and the boat's tile");
		AssertEq(boarded, Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"the other outcome is the one for an empty tile");
	}

	public void Test_NoVehicleAndNoRefusalHandsTheTurnToTheActionEvent()
	{
		// Both GetOnVehicle and GetOffVehicle return false without changing
		// anything, and then the Player runs CheckActionEvent.
		var fleet = Fleet();
		var boarding = new Rm2kVehicleBoarding();
		AssertEq(Decision(boarding, fleet, 10, 10, pDirection: 0),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"facing up towards an empty tile is an action event turn");
		AssertTrue(!boarding.IsAboard, "and the player is still on foot");
		AssertEq(boarding.PendingTile, null, "with nothing pending");
	}

	public void Test_AFacingTheBoatButNotTheWaterIsStillAnActionEventTurn()
	{
		// CanEmbarkShip is checked after the vehicle is found, so a boat that is
		// there but whose water is not passable in that direction is not boarded
		// and the action event check still runs.
		var boarding = new Rm2kVehicleBoarding();
		AssertEq(Decision(boarding, Fleet(), 10, 10, pDirection: 1, pCanEmbark: false),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"the water is not enterable, so nothing is boarded");
		AssertTrue(!boarding.IsBoarding, "and no boarding was left pending");
	}

	public void Test_TheShipInFrontIsBoardedRatherThanTheBoat()
	{
		// The Player tries the ship, and only the boat when there is no ship, so
		// a tile with both boards the ship. Standing at 11,10 and facing up looks
		// at 11,9 where the ship is, while facing right looks at 11,10 where the
		// boat is: the two must not be confused.
		var fleet = Fleet();
		var up = Rm2kVehicle.None;
		Rm2kDecisionTurn.Run(
			new Rm2kVehicleBoarding(), fleet, 3, 11, 10, pDirection: 0, pFacing: 2,
			pPlayerMoveSpeed: 3, pPlayerIsStopping: true, pAirshipIsStopping: true,
			pCanEmbark: true, pCanDisembark: true,
			pBeginEmbark: (t, speed, x, y) => { up = t; });
		AssertEq(up, Rm2kVehicle.Ship, "facing up from 11,10 boards the ship at 11,9");

		var right = Rm2kVehicle.None;
		Rm2kDecisionTurn.Run(
			new Rm2kVehicleBoarding(), fleet, 3, 10, 10, pDirection: 1, pFacing: 2,
			pPlayerMoveSpeed: 3, pPlayerIsStopping: true, pAirshipIsStopping: true,
			pCanEmbark: true, pCanDisembark: true,
			pBeginEmbark: (t, speed, x, y) => { right = t; });
		AssertEq(right, Rm2kVehicle.Boat, "and facing right from 10,10 boards the boat at 11,10");

		// A tile with both would board the ship, because the Player looks for the
		// ship first and only falls back to the boat.
		var both = new List<Rm2kVehicleState>
		{
			new Rm2kVehicleState(Rm2kVehicle.Boat, 3, 11, 9),
			new Rm2kVehicleState(Rm2kVehicle.Ship, 3, 11, 9),
		};
		var stacked = Rm2kVehicle.None;
		Rm2kDecisionTurn.Run(
			new Rm2kVehicleBoarding(), both, 3, 11, 10, pDirection: 0, pFacing: 2,
			pPlayerMoveSpeed: 3, pPlayerIsStopping: true, pAirshipIsStopping: true,
			pCanEmbark: true, pCanDisembark: true,
			pBeginEmbark: (t, speed, x, y) => { stacked = t; });
		AssertEq(stacked, Rm2kVehicle.Ship, "a tile with both boards the ship");
	}

	public void Test_AnAirshipIsOnlyBoardedWhileItIsStandingStill()
	{
		// GetOnVehicle requires the airship to be stopping as well as the player.
		// A drifting airship with a move route running does not pick the player
		// up, and a caller that hardcoded this check would board it anyway.
		var fleet = Fleet();
		var moving = new Rm2kVehicleBoarding();
		AssertEq(Decision(moving, fleet, 40, 10, pDirection: 2, pAirshipIsStopping: false),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"an airship that is still moving is not boarded");
		AssertTrue(!moving.IsAboard, "and the player stays on foot");

		var still = new Rm2kVehicleBoarding();
		AssertEq(Decision(still, fleet, 40, 10, pDirection: 2, pAirshipIsStopping: true),
			Rm2kDecisionTurn.DecisionOutcome.HandledByVehicle,
			"but a standing airship is");
	}

	public void Test_AnAirshipIsBoardedOnThePlayersOwnTileNotInFront()
	{
		// The airship is checked at the player's position, so standing next to it
		// and facing it is not enough.
		var boarding = new Rm2kVehicleBoarding();
		AssertEq(Decision(boarding, Fleet(), 39, 10, pDirection: 1),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"facing an airship from the next tile does not board it");
		AssertTrue(!boarding.IsAboard, "and the player stays on foot");
	}

	public void Test_AnAirshipCannotBeLeftWhileItIsStillFlyingUpOrDown()
	{
		// GetOffVehicle returns false while the airship is ascending or
		// descending, and the Player then runs CheckActionEvent. Without the guard
		// the player would end up standing in mid air.
		var fleet = Fleet();
		var boarding = new Rm2kVehicleBoarding();
		boarding.BoardAirship(fleet[2], pPlayerMoveSpeed: 3, pPlayerIsStopping: true);
		fleet[2].StartAscent();

		AssertEq(Decision(boarding, fleet, 40, 10, pDirection: 2),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"a rising airship cannot be left");
		AssertTrue(boarding.IsAboard, "and the player is still aboard it");

		while (fleet[2].IsAscending)
		{
			fleet[2].AnimateAscentDescent(pCanLand: true);
		}
		AssertEq(Decision(boarding, fleet, 40, 10, pDirection: 2),
			Rm2kDecisionTurn.DecisionOutcome.HandledByVehicle,
			"but a landed airship can");
		AssertTrue(boarding.IsAboard, "and the player is still aboard it");
		AssertEq(boarding.PendingVehicleType, Rm2kVehicle.Airship,
			"with the airship pending, because leaving it is a descent");
		AssertEq(boarding.PendingTile, null, "and no tile to step onto");
	}

	public void Test_LeavingABoatIsRefusedWhenTheWayOffIsBlocked()
	{
		// CanDisembarkShip is checked before anything changes, so a blocked way
		// off falls through to the action event with the player still aboard.
		var fleet = Fleet();
		var boarding = new Rm2kVehicleBoarding();
		boarding.BeginEmbark(Rm2kVehicle.Boat, 3, 11, 10);
		boarding.CompleteEmbark();

		AssertEq(Decision(boarding, fleet, 11, 10, pDirection: 1, pCanDisembark: false),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"a blocked way off does not disembark");
		AssertTrue(boarding.IsAboard, "and the player is still aboard");
		AssertTrue(!boarding.IsUnboarding, "with no disembark pending");

		var disembarked = string.Empty;
		var outcome = Rm2kDecisionTurn.Run(
			boarding, fleet, 3, 11, 10, pDirection: 1, pFacing: 2,
			pPlayerMoveSpeed: 4, pPlayerIsStopping: true, pAirshipIsStopping: true,
			pCanEmbark: true, pCanDisembark: true,
			pBeginDisembark: (type, x, y) =>
			{
				disembarked = $"type={type} tile={x},{y}";
				boarding.BeginDisembark(x, y);
			});
		AssertEq(outcome, Rm2kDecisionTurn.DecisionOutcome.HandledByVehicle,
			"but an open way off does");
		AssertEq(disembarked, "type=1 tile=12,10",
			"and it is the boat the player is on, stepping off to the tile in front");
		AssertTrue(boarding.IsUnboarding, "and the player is stepping off");
		AssertEq(boarding.PendingTile!.Value.X, 12, "onto the tile in front of it");
		AssertEq(boarding.PendingTile.Value.Y, 10, "and the row it is on");
	}

	public void Test_SteppingOffIsRefusedOffTheEdgeOfTheMapOrIntoAnEvent()
	{
		// CanDisembarkShip checks IsValid first and then walks the events for an
		// active same-layer one on the target tile.
		AssertTrue(!Rm2kVehicleBoarding.CanDisembark(
				3, 20, 15, 20, 5, 19, 5, pPassableMask: 0, pFrontIsPassable: true, pSameLayerEventAtFront: false),
			"stepping off the right edge of a 20 wide map is refused");
		AssertTrue(!Rm2kVehicleBoarding.CanDisembark(
				3, 20, 15, 5, -1, 5, 0, pPassableMask: 0, pFrontIsPassable: true, pSameLayerEventAtFront: false),
			"and so is stepping off the top edge");
		AssertTrue(!Rm2kVehicleBoarding.CanDisembark(
				3, 20, 15, 5, 5, 5, 6, pPassableMask: 0, pFrontIsPassable: true, pSameLayerEventAtFront: true),
			"an active same-layer event on the tile blocks the step off");
		AssertTrue(Rm2kVehicleBoarding.CanDisembark(
				3, 20, 15, 5, 5, 5, 6, pPassableMask: 0, pFrontIsPassable: true, pSameLayerEventAtFront: false),
			"a clear passable tile allows it");
		AssertTrue(!Rm2kVehicleBoarding.CanDisembark(
				3, 20, 15, 5, 5, 5, 6, pPassableMask: 0, pFrontIsPassable: false, pSameLayerEventAtFront: false),
			"and a tile that is not passable towards the player does not");
	}

	public void Test_ANullOrEmptyFleetHandsTheTurnOverRatherThanFailing()
	{
		// The vehicles are looked up rather than assumed, and a map with no
		// vehicles at all has to behave like any other empty tile.
		var boarding = new Rm2kVehicleBoarding();
		AssertEq(Decision(boarding, new List<Rm2kVehicleState>(), 10, 10, pDirection: 1),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"an empty fleet is an action event turn");
		AssertEq(Rm2kDecisionTurn.Run(
				boarding, null, 3, 10, 10, 1, 2, 3, true, true, true, true),
			Rm2kDecisionTurn.DecisionOutcome.HandledByActionEvent,
			"and so is no fleet at all");
	}
}
