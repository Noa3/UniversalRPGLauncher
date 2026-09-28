using System.Collections.Generic;

using UniversalRPG.Rm2k.Input;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// K-094: the decision key tries a vehicle before it looks for an action event.
/// </summary>
/// <remarks>
/// <para>
/// The card said "this runtime has no vehicles, so nothing can be toggled and
/// the action event check always run". <c>Rm2kDecisionTurn.Run</c> was written
/// and tested against that sentence, and the sentence then stayed in
/// <c>Rm2kPlayerTurn.Apply</c> after the vehicles were built — so the turn was
/// implemented, mutation checked, and never called.
/// </para>
/// <para>
/// <strong>These tests call the player turn, not the decision turn.</strong> A
/// test that calls <c>Rm2kDecisionTurn.Run</c> directly proves the helper
/// works; it does not prove the key does anything, and that is the exact gap
/// this card existed to close.
/// </para>
/// <para>
/// <strong>Boarding is two phases and the tests follow it.</strong>
/// <c>GetOnVehicle</c> sets <c>boarding</c> and the player moves one tile onto
/// the water; <c>aboard</c> only becomes true when it arrives. A test that
/// asserted <c>IsAboard</c> one keypress after <c>BeginEmbark</c> would be
/// asserting a turn that EasyRPG does not implement.
/// </para>
/// </remarks>
// partial, because TestBase derives from GodotObject and Godot 4.7's
// source generator refuses a class it cannot split.
public sealed partial class TestRm2kVehicleDecisionTurn : TestBase
{
	/// <summary>
	/// A player standing on a 20x20 map with every tile passable in all four
	/// directions, so a refusal can only come from a rule and not from an empty
	/// map. <c>ConfigureMap</c> is the map loader's own entry point rather than
	/// a property set, because the dimensions and the masks have to arrive
	/// together or the passability read is out of range.
	/// </summary>
	private static GameSimulationState NewState(int pPlayerX, int pPlayerY)
	{
		var state = new GameSimulationState
		{
			MapId = 1,
			MapX = pPlayerX,
			MapY = pPlayerY,
		};
		var tiles = new List<bool>();
		for (var i = 0; i < 20 * 20; i++)
		{
			tiles.Add(true);
		}
		state.ConfigureMap(1, 20, 20, tiles);
		return state;
	}

	private static void PutVehicle(
		GameSimulationState pState, int pType, int pX, int pY)
	{
		foreach (var vehicle in pState.Vehicles)
		{
			if (vehicle.VehicleType == pType)
			{
				vehicle.MapId = pState.MapId;
				vehicle.X = pX;
				vehicle.Y = pY;
				return;
			}
		}
		pState.Vehicles.Add(new Rm2kVehicleState(pType, pState.MapId, pX, pY));
	}

	/// <summary>
	/// The direction numbers this runtime uses, named so a test cannot read
	/// <c>FacingDirection = 0</c> and mean "down".
	/// </summary>
	/// <remarks>
	/// <strong>2 down, 4 left, 6 right, 8 up</strong> — the order
	/// <c>GetDxFromDirection</c> and <c>TileInFront</c> use. A first draft
	/// wrote <c>0</c> for up and <c>2</c> for down, which is the *event* order
	/// (0 up, 1 right, 2 down, 3 left) and not the *facing* order. Both number
	/// sets exist in this codebase, and on a real map they name different
	/// tiles.
	/// </remarks>
	private const byte Down = 2;
	private const byte Up = 8;

	/// <summary>
	/// Sets one tile's passability mask from two directions, because the
	/// disembark and the embark read opposite ones and a fixture where both are
	/// open cannot tell the two rules apart.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The parameter names are about the direction the tile is
	/// <em>left</em> in, not about where you can walk to.</strong> Passability
	/// is stored per leaving direction, so <c>PassDown</c> on a tile means
	/// "you may step off this tile heading down".
	/// </para>
	/// <para>
	/// <strong>A first draft named these <c>allowUp</c> and
	/// <c>allowDown</c></strong> and wired the first to <c>PassDown</c> — which
	/// reads as the opposite of what it does. Two tests then asserted the wrong
	/// polarity and failed against correct code. <em>A fixture whose names lie
	/// about its own bits is worse than no fixture</em>, because the failure
	/// points at the reader.
	/// </para>
	/// </remarks>
	private static void SetPassability(
		GameSimulationState pState, int pX, int pY,
		bool pCanLeaveDownwards, bool pCanLeaveUpwards)
	{
		var index = pY * pState.MapWidth + pX;
		var mask = 0;
		if (pCanLeaveDownwards)
		{
			mask |= Rm2kChipset.PassDown;
		}
		if (pCanLeaveUpwards)
		{
			mask |= Rm2kChipset.PassUp;
		}
		pState.PassabilityMasks[index] = (byte)mask;
	}

	private static Rm2kVehicleState Vehicle(
		GameSimulationState pState, int pType)
	{
		foreach (var vehicle in pState.Vehicles)
		{
			if (vehicle.VehicleType == pType)
			{
				return vehicle;
			}
		}
		throw new System.InvalidOperationException(
			$"no {Rm2kVehicle.DescribeType(pType)} in the state");
	}

	/// <summary>
	/// The card's own claim as a test: a boat in front of the player is boarded,
	/// and the action event check never runs.
	/// </summary>
	public void Test_ABoatInFrontIsBoardedAndTheActionEventIsNotChecked()
	{
		var state = NewState(5, 5);
		PutVehicle(state, Rm2kVehicle.Boat, 5, 6);
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);
		state.FacingDirection = Down; // 2, not 0: the order is 2/4/6/8

		// **Boarding is null, and the runtime creates it.** A first draft set it
		// here, and a mutation that removed the creation passed every test in
		// this file — because the tests were doing the runtime's job. A reader
		// that needs a state the caller must pre-build is a reader whose first
		// keypress in a real game does nothing.
		AssertEq(
			state.Boarding, null,
			"and the player starts off the vehicles, because a game begins on"
			+ $" foot; the boarding is {state.Boarding?.VehicleType.ToString() ?? "null"}");

		turn.Apply(Rm2kInputAction.Confirm);

		AssertTrue(
			state.Boarding!.IsBoarding,
			"and the player is boarding, because GetOnVehicle sets `boarding`"
			+ " and the player is not aboard until it has stepped onto the"
			+ $" water; IsBoarding is {state.Boarding.IsBoarding}");
		AssertEq(
			state.Boarding.VehicleType, Rm2kVehicle.Boat,
			"and the vehicle is the boat that was in front, because the Player"
			+ " checks the ship first and the boat only when there is no ship;"
			+ " the type is"
			+ $" {Rm2kVehicle.DescribeType(state.Boarding.VehicleType)}");
		AssertEq(
			state.Boarding.PendingTile, (5, 6),
			"and the player will arrive on the boat's tile, which is one below"
			+ $" the player; the pending tile is {state.Boarding.PendingTile}");
		// **`IsAboard` is true from the moment of the decision, not on arrival.**
		// A first draft asserted it was false here, and the test was wrong:
		// `SetOnVehicle` assigns `aboard` itself and *then* sets `boarding`, so
		// the two flags overlap and `aboard` leads. **The two phases are
		// `boarding` and `unboarding`, not `aboard` and `not aboard`** — a
		// reader that waited for an arrival to grant `aboard` would refuse to
		// let a player step off, because they would already be on foot.
		AssertTrue(
			state.Boarding.IsAboard,
			"and the player counts as aboard from the decision itself, because"
			+ " SetOnVehicle assigns aboard first and sets boarding second, so"
			+ $" the flags overlap; IsAboard is {state.Boarding.IsAboard}");
		AssertEq(
			state.MapY, 5,
			"and the player has not moved yet, because BeginEmbark records where"
			+ " the step goes and the step itself is the next thing the runtime"
			+ $" does; y is {state.MapY}");
		AssertEq(
			state.Boarding.PreboardMoveSpeed, 3,
			"and the speed the player had is kept, so a disembark can hand it"
			+ $" back; it is {state.Boarding.PreboardMoveSpeed}");
	}

	/// <summary>
	/// The step onto the water finishes the boarding, and only then is the
	/// player aboard and moving at the vehicle's speed.
	/// </summary>
	public void Test_TheStepOntoTheWaterMakesThePlayerAboardAtTheVehiclesSpeed()
	{
		var state = NewState(5, 5);
		PutVehicle(state, Rm2kVehicle.Boat, 5, 6);
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);
		state.FacingDirection = Down;

		turn.Apply(Rm2kInputAction.Confirm);

		AssertTrue(
			state.Boarding != null
				&& (state.Boarding.IsBoarding || !state.Boarding.IsAboard),
			"and either the boarding is still on its way or it was cancelled by"
			+ " a refused step, because the two phases are separate and a reader"
			+ " that granted both at once would put the player on water it never"
			+ " walked to; IsBoarding is"
			+ $" {state.Boarding.IsBoarding} and IsAboard is"
			+ $" {state.Boarding.IsAboard}");

		// The arrival, which is what the runtime does once the step budget has
		// run out on the water tile.
		state.Boarding.CompleteEmbark();
		state.MapY = 6;
		state.HeroMoveSpeed = Rm2kVehicle.MoveSpeedForType(Rm2kVehicle.Boat);

		AssertTrue(
			state.Boarding.IsAboard,
			"and the player is aboard once the step has arrived, which is what"
			+ $" CompleteEmbark means; IsAboard is {state.Boarding.IsAboard}");
		AssertEq(
			state.HeroMoveSpeed, Rm2kVehicle.MoveSpeedNormal,
			"and the hero moves at 4, which is MoveSpeed_normal and not the 3"
			+ " the hero walks at, because that is what the constructor's switch"
			+ $" gives a boat; the speed is {state.HeroMoveSpeed}");
	}

	/// <summary>
	/// A vehicle behind the player is not boarded. The player faces down and the
	/// boat is above them, which is the tile they are not looking at.
	/// </summary>
	/// <remarks>
	/// A first draft put the boat at (5,4) and faced the player up — which is
	/// the tile *in front*, so the test asserted a refusal and got a boarding.
	/// **The test was pointing the wrong way**, and the failure read exactly
	/// like a broken reader.
	/// </remarks>
	public void Test_AVehicleBehindThePlayerIsNotBoarded()
	{
		var state = NewState(5, 5);
		PutVehicle(state, Rm2kVehicle.Boat, 5, 4); // above
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);
		state.FacingDirection = Down; // (5,4) is then above, i.e. behind

		turn.Apply(Rm2kInputAction.Confirm);

		AssertTrue(
			state.Boarding == null
				|| (!state.Boarding.IsBoarding && !state.Boarding.IsAboard),
			"and no boarding was started, because the boat is above the player"
			+ " while they face down, and GetOnVehicle only looks at the tile in"
			+ " front; IsBoarding is"
			+ $" {state.Boarding.IsBoarding} and IsAboard is"
			+ $" {state.Boarding.IsAboard}");
		AssertEq(
			state.Boarding.PendingTile, null,
			"and nothing is pending, because there was nothing to board;"
			+ $" the pending tile is {state.Boarding.PendingTile}");
	}

	/// <summary>
	/// The airship is boarded by standing on it, and it is the one vehicle that
	/// is aboard immediately.
	/// </summary>
	public void Test_AnAirshipIsBoardedByStandingOnIt()
	{
		var state = NewState(5, 5);
		PutVehicle(state, Rm2kVehicle.Airship, 5, 5); // the player's own tile
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);

		turn.Apply(Rm2kInputAction.Confirm);

		AssertTrue(
			state.Boarding != null && state.Boarding.IsAboard,
			"and the player is aboard at once, because the airship is boarded"
			+ " from the player's own tile and has no water step to make;"
			+ $" IsAboard is {state.Boarding.IsAboard}");
		AssertEq(
			state.Boarding.VehicleType, Rm2kVehicle.Airship,
			"and the vehicle is the airship, from the first branch of"
			+ " GetOnVehicle rather than the second; the type is"
			+ $" {Rm2kVehicle.DescribeType(state.Boarding.VehicleType)}");
		AssertTrue(
			Vehicle(state, Rm2kVehicle.Airship).IsAscending,
			"and the airship started its ascent, because GetOnVehicle calls"
			+ " StartAscent for the airship and not for a boat; the remaining"
			+ " ascent is"
			+ $" {Vehicle(state, Rm2kVehicle.Airship).RemainingAscent}");
	}

	/// <summary>
	/// A rising airship cannot be boarded, and a rising airship cannot be left.
	/// Both refusals are the same rule read from two ends.
	/// </summary>
	/// <remarks>
	/// A first draft started the ascent before boarding and asserted the player
	/// was aboard — <strong>two rules in one, both wrong</strong>. Boarding
	/// needs the airship to be stopping, so an ascent started first makes the
	/// board fail, and then the second keypress had nothing to leave. **The
	/// test asserted a state it had prevented itself from reaching.**
	/// </remarks>
	public void Test_AnAirshipThatIsStillRisingCannotBeLeft()
	{
		var state = NewState(5, 5);
		PutVehicle(state, Rm2kVehicle.Airship, 5, 5);
		var airship = Vehicle(state, Rm2kVehicle.Airship);

		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);

		// **First refusal: an airship that is already rising cannot be
		// boarded**, because GetOnVehicle needs the airship to be stopping as
		// well as the player. Boarding a drifting airship is not a thing the
		// format allows.
		airship.IsFlying = true;
		airship.StartAscent();
		turn.Apply(Rm2kInputAction.Confirm);
		AssertTrue(
			state.Boarding == null || !state.Boarding.IsAboard,
			"and the player is not aboard, because CanBoardAirshipOn requires"
			+ " the airship to be stopping and a rising airship is not; the"
			+ $" remaining ascent is {airship.RemainingAscent}");

		// Now let it land, board it, and start the ascent that boarding causes.
		airship.ForceLand();
		turn.Apply(Rm2kInputAction.Confirm);
		AssertTrue(
			state.Boarding != null && state.Boarding.IsAboard,
			"and once it has landed the player boards it, because the refusal"
			+ $" was about the airship and not about the player; IsAboard is"
			+ $" {state.Boarding.IsAboard}");
		AssertTrue(
			airship.IsAscending,
			"and boarding starts the ascent, which is what the first branch of"
			+ " GetOnVehicle does and the reason a landed airship is only"
			+ " boardable from the ground; the remaining ascent is"
			+ $" {airship.RemainingAscent}");

		// **Second refusal: a rising airship cannot be left.**
		turn.Apply(Rm2kInputAction.Confirm);

		AssertEq(
			airship.RemainingDescent, 0,
			"and no descent was started, because CanLeaveAirship refuses a"
			+ " vehicle that is still moving and the Player returns false so the"
			+ " action event check runs instead; RemainingDescent is"
			+ $" {airship.RemainingDescent}");
		AssertTrue(
			state.Boarding != null && state.Boarding.IsAboard,
			"and the player is still aboard, because a refused disembark leaves"
			+ $" the player where they were; IsAboard is {state.Boarding.IsAboard}");
	}

	/// <summary>
	/// A boat is left by stepping off the water tile, and the speed the player
	/// had is handed back only once they have stepped off.
	/// </summary>
	public void Test_ABoatIsLeftBySteppingOffTheWaterTile()
	{
		var state = NewState(5, 6);
		PutVehicle(state, Rm2kVehicle.Boat, 5, 6);
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);
		state.FacingDirection = Up; // 8, not 0
		state.Boarding = new Rm2kVehicleBoarding();
		state.Boarding.BeginEmbark(Rm2kVehicle.Boat, 3, 5, 6);
		state.Boarding.CompleteEmbark();
		state.HeroMoveSpeed = Rm2kVehicle.MoveSpeedNormal;
		AssertTrue(state.Boarding.IsAboard, "sanity: the player is aboard");

		turn.Apply(Rm2kInputAction.Confirm);

		AssertTrue(
			state.Boarding.IsUnboarding,
			"and the player is stepping off, because GetOffVehicle sets"
			+ " `unboarding` and the player is on foot only when the step has"
			+ $" arrived; IsUnboarding is {state.Boarding.IsUnboarding}");
		AssertEq(
			state.Boarding.PendingTile, (5, 5),
			"and the step goes to the tile in front, which is the land above the"
			+ $" water; the pending tile is {state.Boarding.PendingTile}");
		AssertTrue(
			state.Boarding.IsAboard,
			"and the player is still aboard until that step arrives, because"
			+ " the move speed is not restored while they are halfway off;"
			+ $" IsAboard is {state.Boarding.IsAboard}");

		// **The move speed is handed back the moment the disembark is decided,
		// not when the step arrives.** A mutation that removed that line
		// passed every test in this file, and the reason is that no test looked:
		// the disembark test checked the tile and the flag, and the speed was
		// never read. **A rule nobody asserts is a rule nobody wrote.**
		AssertEq(
			state.HeroMoveSpeed, 3,
			"and the hero is already back at the speed they had before boarding,"
			+ " because GetOffVehicle restores preboard_move_speed as it starts"
			+ " the step rather than after it, and a reader that restored it late"
			+ $" would walk the player at 4 across the land; the speed is {state.HeroMoveSpeed}");

		state.Boarding.CompleteDisembark();
		AssertTrue(
			!state.Boarding.IsAboard,
			"and once it arrives the player is on foot; IsAboard is"
			+ $" {state.Boarding.IsAboard}");
	}

	/// <summary>
	/// Stepping off reads the direction that points back at the player, and
	/// that is not the direction they face.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Passability is stored per leaving direction, so a disembark reads the
	/// bit on the tile in front that points <em>back</em> at the boat, while
	/// the embark reads the bit pointing away. **A mutation that swapped the two
	/// passed every test in this file**, because the fixture made every tile
	/// passable in all four directions — so both readings returned true and
	/// neither was wrong in a way anything could see.
	/// </para>
	/// <para>
	/// <strong>A fixture with one direction open cannot tell two rules
	/// apart.</strong> The tile in front is passable out of the boat and not
	/// back into it, which is what a quay is: you can step off the water onto
	/// the land, and the land does not let you walk back into the boat.
	/// </para>
	/// </remarks>
	public void Test_SteppingOffReadsTheWayBackAndNotTheWayThePlayerFaces()
	{
		var state = NewState(5, 6);
		PutVehicle(state, Rm2kVehicle.Boat, 5, 6);
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);
		state.FacingDirection = Up; // 8, not 0
		state.Boarding = new Rm2kVehicleBoarding();
		state.Boarding.BeginEmbark(Rm2kVehicle.Boat, 3, 5, 6);
		state.Boarding.CompleteEmbark();

		// **The tile in front (5,5) may be left heading UP — back towards the
		// boat the player is standing on — and not heading down.** The player
		// faces up, so the disembark reads `PassUp` on (5,5). A disembark that
		// read the direction the player faces instead would read
		// `PassDown`, find it shut, and refuse to let the player leave a boat
		// they are standing in.
		SetPassability(state, 5, 5, pCanLeaveDownwards: true, pCanLeaveUpwards: false);
		// The boat's own tile is open both ways, because the player came from
		// it.
		SetPassability(state, 5, 6, pCanLeaveDownwards: true, pCanLeaveUpwards: true);

		turn.Apply(Rm2kInputAction.Confirm);

		AssertTrue(
			state.Boarding.IsUnboarding,
			"and the player steps off, because CanDisembarkShip reads the"
			+ " direction that points back at the boat, which is the one the"
			+ " quay allows; IsUnboarding is"
			+ $" {state.Boarding.IsUnboarding}");

		// **And the embark reads the other bit, on the same tile.** The mirror
		// half of this rule is the embark, and it reads the direction the
		// player faces — not the way back. A mutation that swapped the two bits
		// in `CanEmbark` passed every test here, because the only embark test
		// used a map that was open in every direction. **Two rules, one tile,
		// one bit apart, and the fixture could not tell them apart.**
		state.Boarding.CompleteDisembark();
		state.MapY = 5;
		state.Boarding = new Rm2kVehicleBoarding();
		state.FacingDirection = Down;
		// **The tile in front is (5,6) and the player faces down, so the
		// embark reads `PassDown` on it.** A ship sitting on a tile that is
		// shut that way cannot be boarded — a quay wall, a cliff, a waterfall
		// you are not allowed to row up.
		SetPassability(state, 5, 6, pCanLeaveDownwards: false, pCanLeaveUpwards: true);
		PutVehicle(state, Rm2kVehicle.Ship, 5, 6);

		turn.Apply(Rm2kInputAction.Confirm);
		AssertTrue(
			!state.Boarding.IsBoarding,
			"and a water tile that is shut in the direction the player faces is"
			+ " not boarded into, because CanEmbarkShip reads the bit for the"
			+ $" way the player is going and not the way they came; IsBoarding is"
			+ $" {state.Boarding.IsBoarding}");

		// The mirror image: open the way the player is going, and it is boarded.
		SetPassability(state, 5, 6, pCanLeaveDownwards: true, pCanLeaveUpwards: true);
		state.Boarding = new Rm2kVehicleBoarding();
		turn.Apply(Rm2kInputAction.Confirm);
		AssertTrue(
			state.Boarding.IsBoarding,
			"and the same tile is boarded once it opens the way the player is"
			+ " going, which is the whole difference between the two bits;"
			+ $" IsBoarding is {state.Boarding.IsBoarding}");
	}

	/// <summary>
	/// A new game must not inherit a vehicle from the last one. This is the same
	/// class of fault as K-106's half finished step.
	/// </summary>
	public void Test_AHalfFinishedBoardingDoesNotSurviveANewGame()
	{
		var state = NewState(5, 6);
		PutVehicle(state, Rm2kVehicle.Boat, 5, 6);
		state.Boarding = new Rm2kVehicleBoarding();
		state.Boarding.BeginEmbark(Rm2kVehicle.Boat, 3, 5, 6);
		state.HeroMoveSpeed = Rm2kVehicle.MoveSpeedNormal;

		state.Reset();

		AssertEq(
			state.Boarding, null,
			"and the boarding is gone, because a new game starts on foot and a"
			+ " player halfway into a boat would start inside one; the boarding"
			+ $" is {state.Boarding?.VehicleType.ToString() ?? "null"}");
		AssertEq(
			state.Vehicles.Count, 0,
			"and the vehicles are cleared, because they belong to a map's start"
			+ " node and the last game's boat is not this game's boat; there"
			+ $" are {state.Vehicles.Count}");
		AssertEq(
			state.HeroMoveSpeed, 3,
			"and the hero's move speed is back to the default 3, because the"
			+ $" vehicle's 4 must not survive either; the speed is {state.HeroMoveSpeed}");
	}
}
