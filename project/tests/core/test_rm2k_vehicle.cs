using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the RM2K vehicles against <c>Game_Vehicle</c> and
/// <c>Game_Player::GetOnVehicle</c>.
/// </summary>
/// <remarks>
/// The vehicle types and the move speeds are the part most likely to be
/// invented, so they are asserted against the values liblcf and the Player
/// actually use: <c>None = 0, Boat = 1, Ship = 2, Airship = 3</c> and
/// <c>MoveSpeed_normal = 4</c>, <c>MoveSpeed_double = 5</c>. The default event
/// speed of 3 is <c>MoveSpeed_half</c> and is <em>not</em> what a vehicle gets,
/// which is the easiest thing to get wrong here.
/// </remarks>
public partial class TestRm2kVehicle : TestBase
{
	public void Test_TheVehicleTypesAreTheLiblcfValues()
	{
		// These numbers are stored in the player's save data, so they are part of
		// the format rather than an internal detail.
		AssertEq(Rm2kVehicle.None, 0, "Game_Vehicle::Type::None");
		AssertEq(Rm2kVehicle.Boat, 1, "Game_Vehicle::Type::Boat");
		AssertEq(Rm2kVehicle.Ship, 2, "Game_Vehicle::Type::Ship");
		AssertEq(Rm2kVehicle.Airship, 3, "Game_Vehicle::Type::Airship");
		AssertTrue(Rm2kVehicle.IsKnownType(Rm2kVehicle.Airship), "the airship is a known type");
		AssertTrue(!Rm2kVehicle.IsKnownType(4), "and four is not one the format defines");
	}

	public void Test_AVehicleGetsTheMoveSpeedThePlayerGivesIt()
	{
		// SetMoveSpeed(MoveSpeed_normal) for a boat and a ship,
		// SetMoveSpeed(MoveSpeed_double) for the airship. liblcf's
		// EventPage_MoveSpeed is eighth 1, quarter 2, half 3, normal 4,
		// double 5, fourfold 6, so the default event speed of 3 is half and a
		// vehicle is faster than an ordinary event.
		AssertEq(Rm2kVehicle.MoveSpeedNormal, 4, "MoveSpeed_normal is 4, not 3");
		AssertEq(Rm2kVehicle.MoveSpeedDouble, 5, "MoveSpeed_double is 5");
		AssertEq(Rm2kVehicle.MoveSpeedForType(Rm2kVehicle.Boat), 4, "a boat moves at normal speed");
		AssertEq(Rm2kVehicle.MoveSpeedForType(Rm2kVehicle.Ship), 4, "and so does a ship");
		AssertEq(Rm2kVehicle.MoveSpeedForType(Rm2kVehicle.Airship), 5, "the airship is faster still");
		AssertEq(Rm2kVehicle.MoveSpeedForType(Rm2kVehicle.None), 0, "and no vehicle has no speed");
	}

	public void Test_AVehicleStartsFacingLeft()
	{
		// The constructor calls SetDirection(Left) and SetFacing(Left), and the
		// liblcf order is up 0, right 1, down 2, left 3.
		var boat = new Rm2kVehicleState(Rm2kVehicle.Boat, 0, 10, 10);
		AssertEq(boat.Direction, 3, "a new boat faces left");
		AssertEq(boat.MapId, 0, "on its start map");
		AssertEq(boat.X, 10, "at its start x");
		AssertEq(boat.Y, 10, "and its start y");
		AssertEq(boat.MoveSpeed, 4, "and it already has the normal speed");
	}

	public void Test_IsInPositionNeedsTheSameMapNotJustTheSameTile()
	{
		// IsInCurrentMap() && IsInPosition(x, y): a vehicle whose start map is
		// another map is not here even when its coordinates match, which is what
		// stops a boat from being boarded on a map it is not on.
		var boat = new Rm2kVehicleState(Rm2kVehicle.Boat, 3, 10, 10);
		AssertTrue(boat.IsInPosition(3, 10, 10), "the boat is on its own map at its position");
		AssertTrue(!boat.IsInPosition(4, 10, 10), "but not on another map at the same tile");
		AssertTrue(!boat.IsInPosition(3, 10, 11), "nor one tile away");
	}

	public void Test_AnAscentTakesThirtyTwoUpdatesBecauseEightIsSpentEachTime()
	{
		// StartAscent fills remaining_ascent with SCREEN_TILE_SIZE, which is
		// 256, and AnimateAscentDescent subtracts 8 per update. A full ascent is
		// therefore 32 updates, not 2: treating the counter as pixels would make
		// the airship leap instead of rise.
		var airship = new Rm2kVehicleState(Rm2kVehicle.Airship, 0, 5, 5);
		airship.StartAscent();
		AssertEq(airship.RemainingAscent, 256, "the ascent budget is a full screen tile");
		AssertTrue(airship.IsAscending, "and the airship is ascending");
		AssertTrue(!airship.IsDescending, "and not descending at the same time");

		var updates = 0;
		while (airship.IsAscending && updates < 1000)
		{
			airship.AnimateAscentDescent(pCanLand: true);
			updates++;
		}
		AssertEq(updates, 32, "a full ascent takes 32 updates at 8 per update");
		AssertEq(airship.GetAltitude(), 0,
			"and a finished ascent is not flying by itself, so it is drawn at ground level");
	}

	public void Test_AFinishedAscentOnlyFliesOnceThePlayerSaysSo()
	{
		// GetOnVehicle calls SetFlying(vehicle->IsFlying()) after the ascent is
		// started, so the altitude is only non zero once flying is set. Getting
		// this wrong draws the airship a tile high the instant it starts rising.
		var airship = new Rm2kVehicleState(Rm2kVehicle.Airship, 0, 5, 5);
		AssertEq(airship.GetAltitude(), 0, "a grounded airship has no altitude");

		airship.StartAscent();
		AssertEq(airship.GetAltitude(), 0, "and neither has one that is only ascending");
		airship.IsFlying = true;
		AssertEq(airship.GetAltitude(), 0, "the first update of the ascent is still at ground level");

		// The altitude is (SCREEN_TILE_SIZE - remaining) / (SCREEN_TILE_SIZE /
		// TILE_SIZE) in whole tiles, so each eight units of ascent is half a
		// tile and the height only moves once sixteen units are spent.
		airship.AnimateAscentDescent(pCanLand: true);
		AssertEq(airship.GetAltitude(), 0, "after one update the airship has not risen a whole tile");
		for (var update = 0; update < 3; update++)
		{
			airship.AnimateAscentDescent(pCanLand: true);
		}
		AssertEq(airship.GetAltitude(), 2, "and after four updates it is two tiles up");
		for (var update = 0; update < 28; update++)
		{
			airship.AnimateAscentDescent(pCanLand: true);
		}
		AssertEq(airship.GetAltitude(), 16,
			"a finished ascent puts the airship a full screen tile up, which is the upstream altitude");
	}

	public void Test_ADescentStopsFlyingAndLandsOnlyWhereItCan()
	{
		// When the descent runs out the airship stops flying, and then either
		// lands, if its tile allows it, or starts another ascent. An airship that
		// cannot land has to keep trying rather than hover forever.
		var landable = new Rm2kVehicleState(Rm2kVehicle.Airship, 0, 5, 5);
		landable.IsFlying = true;
		landable.StartDescent();
		AssertTrue(landable.IsDescending, "the airship is descending");
		while (landable.IsDescending)
		{
			landable.AnimateAscentDescent(pCanLand: true);
		}
		AssertTrue(!landable.IsFlying, "a landed airship is not flying");
		AssertEq(landable.Direction, 3, "and it turned to its default direction");
		AssertTrue(!landable.IsAscending, "and it did not start rising again");

		var blocked = new Rm2kVehicleState(Rm2kVehicle.Airship, 0, 5, 5);
		blocked.IsFlying = true;
		blocked.StartDescent();
		blocked.Direction = 2;
		while (blocked.IsDescending)
		{
			blocked.AnimateAscentDescent(pCanLand: false);
		}
		AssertTrue(!blocked.IsFlying, "an airship that could not land is still not flying");
		AssertTrue(blocked.IsAscending, "and it starts another ascent rather than hovering");
	}

	public void Test_AFlightUpdateThatIsNeitherAscendingNorDescendingDoesNothing()
	{
		// AnimateAscentDescent returns false when neither counter is running, so
		// a parked airship does not consume the animation.
		var airship = new Rm2kVehicleState(Rm2kVehicle.Airship, 0, 5, 5);
		AssertTrue(!airship.AnimateAscentDescent(pCanLand: true), "a parked airship animates nothing");
		AssertEq(airship.RemainingAscent, 0, "and has no ascent budget");
		AssertEq(airship.RemainingDescent, 0, "and no descent budget");
	}

	public void Test_ForceLandingEndsBothCountersAtOnce()
	{
		// ForceLand clears the ascent, the descent and the flying flag together,
		// which is what a save load or a teleport needs.
		var airship = new Rm2kVehicleState(Rm2kVehicle.Airship, 0, 5, 5);
		airship.IsFlying = true;
		airship.StartAscent();
		airship.AnimateAscentDescent(pCanLand: true);
		AssertTrue(airship.IsAscending, "the airship is mid ascent");
		airship.ForceLand();
		AssertTrue(!airship.IsAscending, "forcing a landing ends the ascent");
		AssertTrue(!airship.IsDescending, "and the descent");
		AssertTrue(!airship.IsFlying, "and it is no longer flying");
		AssertEq(airship.GetAltitude(), 0, "so it is drawn at ground level");
	}

	public void Test_StartingOneCounterClearsTheOther()
	{
		// StartAscent and StartDescent each clear the opposite counter, so an
		// airship can never be ascending and descending at once. A state that
		// allowed both would have no defined altitude.
		var airship = new Rm2kVehicleState(Rm2kVehicle.Airship, 0, 5, 5);
		airship.StartAscent();
		airship.StartDescent();
		AssertTrue(!airship.IsAscending, "starting a descent ends the ascent");
		AssertTrue(airship.IsDescending, "and starts the descent");
		AssertTrue(!airship.IsAscendingOrDescending == false, "so exactly one of them is running");

		airship.StartAscent();
		AssertTrue(airship.IsAscending, "and going back the other way is symmetric");
		AssertTrue(!airship.IsDescending, "with the descent cleared");
	}

	public void Test_AnUnknownVehicleTypeIsReportedRatherThanTreatedAsNone()
	{
		// A vehicle type comes from a file. Treating an unreadable value as "no
		// vehicle" would put the player somewhere they never chose, so it is
		// named and refused instead.
		AssertEq(Rm2kVehicle.DescribeType(Rm2kVehicle.Airship), "Airship", "a known type is named");
		AssertEq(Rm2kVehicle.DescribeType(7), "0x7", "an unknown type is reported in hex");
		AssertEq(Rm2kVehicle.MoveSpeedForType(7), 0,
			"and gets no speed, because there is no verified speed for it");
	}
}
