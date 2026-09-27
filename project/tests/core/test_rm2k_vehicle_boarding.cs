using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the boarding rules from <c>Game_Player::GetOnVehicle</c> and
/// <c>Game_Player::GetOffVehicle</c>.
/// </summary>
/// <remarks>
/// The two rules are not symmetric, and that is the point of these tests. An
/// airship is boarded by standing on it and left by descending, so the player
/// never moves. A boat or a ship is boarded by stepping onto it and left by
/// stepping off, so the player moves both times. A single "get on or off"
/// implementation would get one of the two pairs wrong.
/// </remarks>
public partial class TestRm2kVehicleBoarding : TestBase
{
	/// <summary>Three vehicles, each on its own tile of the same map.</summary>
	private static List<Rm2kVehicleState> Fleet(int pMapId = 3)
	{
		return new List<Rm2kVehicleState>
		{
			new Rm2kVehicleState(Rm2kVehicle.Boat, pMapId, 10, 5),
			new Rm2kVehicleState(Rm2kVehicle.Ship, pMapId, 20, 5),
			new Rm2kVehicleState(Rm2kVehicle.Airship, pMapId, 30, 5),
		};
	}

	public void Test_TheTileInFrontIsTheStepThePlayerWouldTake()
	{
		// XwithDirection and YwithDirection, on the liblcf order up 0, right 1,
		// down 2, left 3.
		AssertEq(Rm2kVehicleBoarding.TileInFront(10, 10, 2).X, 10, "facing down keeps x");
		AssertEq(Rm2kVehicleBoarding.TileInFront(10, 10, 2).Y, 11, "and moves y down");
		AssertEq(Rm2kVehicleBoarding.TileInFront(10, 10, 1).X, 11, "facing right moves x");
		AssertEq(Rm2kVehicleBoarding.TileInFront(10, 10, 0).Y, 9, "facing up moves y up");
		AssertEq(Rm2kVehicleBoarding.TileInFront(10, 10, 3).X, 9, "facing left moves x back");
	}

	public void Test_AShipIsPreferredOverABoatOnTheSameTile()
	{
		// The Player looks for the ship first and only falls back to the boat
		// when there is no ship, so a tile with both boards the ship.
		var both = new List<Rm2kVehicleState>
		{
			new Rm2kVehicleState(Rm2kVehicle.Boat, 3, 10, 5),
			new Rm2kVehicleState(Rm2kVehicle.Ship, 3, 10, 5),
		};
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(both, 3, 10, 5), Rm2kVehicle.Ship,
			"a tile with both a ship and a boat boards the ship");
	}

	public void Test_ABoatIsChosenOnlyWhenThereIsNoShip()
	{
		var fleet = Fleet();
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(fleet, 3, 20, 5), Rm2kVehicle.Ship, "the ship's tile");
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(fleet, 3, 10, 5), Rm2kVehicle.Boat, "the boat's tile");
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(fleet, 3, 15, 5), Rm2kVehicle.None,
			"an empty tile has no vehicle to board");
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(fleet, 3, 30, 5), Rm2kVehicle.None,
			"and the airship is not boarded from in front of it");
	}

	public void Test_AVehicleOnAnotherMapIsNotThereToBeBoarded()
	{
		// IsInPosition needs the same map, so a boat whose start map is another
		// one is not on the water tile the player is looking at.
		var fleet = Fleet(pMapId: 3);
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(fleet, 4, 10, 5), Rm2kVehicle.None,
			"a vehicle on another map is not in front of the player");
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(fleet, 3, 10, 5), Rm2kVehicle.Boat,
			"and on its own map it is");
	}

	public void Test_AnAirshipIsBoardedByStandingOnItRatherThanByFacingIt()
	{
		// The first branch of GetOnVehicle tests the airship at the player's own
		// position. This is the asymmetry: a boat is in front, an airship is under
		// the player, and conflating them makes an airship unboardable.
		var fleet = Fleet();
		var airship = Rm2kVehicleBoarding.VehicleOn(fleet, Rm2kVehicle.Airship, 3, 30, 5);
		AssertTrue(airship != null, "the airship is found on its own tile");
		AssertEq(airship!.VehicleType, Rm2kVehicle.Airship, "and it is the airship");
		AssertEq(Rm2kVehicleBoarding.VehicleOn(fleet, Rm2kVehicle.Airship, 3, 29, 5), null,
			"but not one tile away from it");
	}

	public void Test_BoardingAnAirshipPutsThePlayerAboardAtOnce()
	{
		// The airship branch sets aboard immediately: the player does not move,
		// so there is no boarding step to wait for.
		var boarding = new Rm2kVehicleBoarding();
		var airship = new Rm2kVehicleState(Rm2kVehicle.Airship, 3, 30, 5);
		boarding.BoardAirship(airship, pPlayerMoveSpeed: 3, pPlayerIsStopping: true);

		AssertTrue(boarding.IsAboard, "the player is aboard at once");
		AssertEq(boarding.VehicleType, Rm2kVehicle.Airship, "aboard the airship");
		AssertEq(boarding.PendingTile, null, "with no tile to step onto");
		AssertTrue(!boarding.IsBoarding, "and not in the middle of boarding");
		AssertEq(boarding.PreboardMoveSpeed, 3, "and the previous move speed is kept");
	}

	public void Test_BoardingABoatMakesThePlayerStepOntoTheWater()
	{
		// The second branch sets boarding rather than aboard and moves the player
		// onto the water tile, so the boarding is only finished when it arrives.
		var boarding = new Rm2kVehicleBoarding();
		boarding.BeginEmbark(Rm2kVehicle.Boat, pPlayerMoveSpeed: 4, pTargetX: 10, pTargetY: 5);

		AssertTrue(boarding.IsBoarding, "the player is boarding");
		AssertEq(boarding.PendingTile!.Value.X, 10, "onto the boat's tile in x");
		AssertEq(boarding.PendingTile.Value.Y, 5, "and in y");
		AssertEq(boarding.PreboardMoveSpeed, 4, "and the previous move speed is kept");

		boarding.CompleteEmbark();
		AssertTrue(!boarding.IsBoarding, "arriving finishes the boarding");
		AssertTrue(boarding.IsAboard, "and the player is aboard");
		AssertEq(boarding.PendingTile, null, "with no tile left to move to");
	}

	public void Test_LeavingABoatPutsThePlayerBackOnFootWithItsOwnSpeedBack()
	{
		// The disembark restores preboard_move_speed rather than the event
		// default, so a player that was walking slowly does not come back fast.
		var boarding = new Rm2kVehicleBoarding();
		boarding.BeginEmbark(Rm2kVehicle.Boat, pPlayerMoveSpeed: 2, pTargetX: 10, pTargetY: 5);
		boarding.CompleteEmbark();
		AssertTrue(boarding.IsAboard, "aboard the boat");

		boarding.BeginDisembark(pTargetX: 10, pTargetY: 6);
		AssertTrue(boarding.IsUnboarding, "the player is stepping off");
		AssertEq(boarding.PendingTile!.Value.Y, 6, "onto the tile it stepped off to");
		AssertTrue(boarding.IsAboard, "and is still aboard until it has stepped off");

		boarding.CompleteDisembark();
		AssertTrue(!boarding.IsAboard, "once off the boat the player is on foot");
		AssertEq(boarding.VehicleType, Rm2kVehicle.None, "with no vehicle");
		// The Player reads preboard_move_speed back into the character, so the
		// value has to survive the whole boarding and must not be replaced by
		// the vehicle's own 4 or by the event default of 3.
		AssertEq(boarding.PreboardMoveSpeed, 2,
			"and the speed it had before boarding, not the vehicle's or the event default");

		// A player that boards twice keeps each speed separately, because the
		// second boarding overwrites the stash with its own.
		boarding.BeginEmbark(Rm2kVehicle.Ship, pPlayerMoveSpeed: 5, pTargetX: 20, pTargetY: 5);
		AssertEq(boarding.PreboardMoveSpeed, 5, "a second boarding stashes its own speed");
		boarding.CompleteEmbark();
		boarding.BeginDisembark(20, 6);
		boarding.CompleteDisembark();
		AssertEq(boarding.PreboardMoveSpeed, 5, "and stepping off restores that one, not the first");
	}

	public void Test_LeavingAnAirshipKeepsThePlayerAboardBecauseItOnlyStartsADescent()
	{
		// The airship branch of GetOffVehicle returns after StartDescent: the
		// player never moves off the tile, so treating it like a boat would put
		// the player on water it never left.
		var boarding = new Rm2kVehicleBoarding();
		boarding.BoardAirship(new Rm2kVehicleState(Rm2kVehicle.Airship, 3, 30, 5), 3, true);
		boarding.BeginAirshipDisembark();

		AssertTrue(boarding.IsAboard, "the player is still aboard the airship");
		AssertEq(boarding.PendingTile, null, "and has no tile to step onto");
		AssertEq(boarding.PendingVehicleType, Rm2kVehicle.Airship,
			"the pending vehicle is the airship that has to descend");
		AssertTrue(!boarding.IsUnboarding, "and is not stepping off");
	}

	public void Test_ARefusedBoardingLeavesThePlayerAsItWas()
	{
		// CanEmbarkShip runs before anything is changed, so a boarding that is
		// refused afterwards has to be undone rather than left half applied.
		var boarding = new Rm2kVehicleBoarding();
		AssertTrue(!boarding.IsAboard, "the player starts on foot");
		boarding.BeginEmbark(Rm2kVehicle.Ship, 3, 20, 5);
		boarding.CancelPending();

		AssertTrue(!boarding.IsAboard, "a cancelled boarding leaves the player on foot");
		AssertTrue(!boarding.IsBoarding, "and not in the middle of boarding");
		AssertEq(boarding.PendingTile, null, "and with no tile to move to");
		AssertEq(boarding.PendingVehicleType, Rm2kVehicle.None, "and no vehicle chosen");
	}

	public void Test_AFleetWithNoVehiclesBoardsNothing()
	{
		var empty = new List<Rm2kVehicleState>();
		AssertEq(Rm2kVehicleBoarding.VehicleInFront(empty, 3, 10, 5), Rm2kVehicle.None,
			"an empty fleet has nothing in front");
		AssertEq(Rm2kVehicleBoarding.VehicleOn(empty, Rm2kVehicle.Airship, 3, 10, 5), null,
			"and no airship to stand on");
	}
}
