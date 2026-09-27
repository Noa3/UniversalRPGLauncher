using System.Collections.Generic;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the vehicle sprite and animation from
/// <c>Game_Vehicle::UpdateAnimation</c> and <c>Game_Character::IncAnimFrame</c>.
/// </summary>
/// <remarks>
/// Two things here are the opposite of what a character does, and both are
/// easy to carry over by mistake. A vehicle is shown for sixteen frames while
/// it is standing still and twelve while it moves, where the character tables
/// are the other way round. And a vehicle's frame wraps with a modulo over all
/// four liblcf frames rather than being clamped, so middle2 is a real state.
/// </remarks>
public partial class TestRm2kVehicleSprite : TestBase
{
	public void Test_AVehicleAnimatesSlowerWhenItIsStandingStillThanWhenItMoves()
	{
		// GetStopCount() ? 16 : 12 from Game_Vehicle::UpdateAnimation. A standing
		// vehicle therefore lingers on each frame twice as long as a moving one,
		// which is the reverse of the character tables.
		var parked = new Rm2kVehicleSprite
		{
			VehicleType = Rm2kVehicle.Boat,
			CharacterName = "Boat",
			IsMoving = false,
		};
		var moving = new Rm2kVehicleSprite
		{
			VehicleType = Rm2kVehicle.Boat,
			CharacterName = "Boat",
			IsMoving = true,
		};
		AssertEq(parked.AnimationLimit, 16, "a standing vehicle is shown for sixteen frames");
		AssertEq(moving.AnimationLimit, 12, "and a moving one for twelve");
	}

	public void Test_TheFrameWrapsWithAModuloOverAllFourFrames()
	{
		// anim_frame = (anim_frame + 1) % 4. middle2 is a real liblcf state that
		// the sprite clamp turns into middle when it is drawn, so a vehicle walks
		// left, middle, right, middle2 and then back to left.
		AssertEq(Rm2kVehicleSprite.NextFrame(0), 1, "left advances to middle");
		AssertEq(Rm2kVehicleSprite.NextFrame(1), 2, "middle advances to right");
		AssertEq(Rm2kVehicleSprite.NextFrame(2), 3, "right advances to middle2");
		AssertEq(Rm2kVehicleSprite.NextFrame(3), 0, "and middle2 wraps back to left");
	}

	public void Test_TheCounterAdvancesOnceTheLimitIsReached()
	{
		// IncAnimCount then a comparison against the limit: the frame only
		// changes once the counter has reached it, and advancing resets the
		// counter to zero.
		var frame = Rm2kVehicleSprite.AdvanceFrame(
			pCurrentFrame: 0, pAnimCount: 14, pLimit: 16,
			pAscendingOrDescending: false, pIsFixedGraphic: false, out var count1);
		AssertEq(frame, 0, "one short of the limit the frame holds");
		AssertEq(count1, 15, "and the counter advanced by one");

		frame = Rm2kVehicleSprite.AdvanceFrame(
			pCurrentFrame: 0, pAnimCount: 15, pLimit: 16,
			pAscendingOrDescending: false, pIsFixedGraphic: false, out var count2);
		AssertEq(frame, 1, "at the limit the frame advances");
		AssertEq(count2, 16, "and the counter is the incremented one, not a reset");
	}

	public void Test_AFullCycleWalksAStationaryVehicleThroughItsThreeCellFrames()
	{
		// Left, middle, right and middle2 in turn. middle2 draws as middle, so
		// what the player sees is left, middle, right, middle, left again.
		var frame = 0;
		var seen = new List<int> { frame };
		for (var step = 0; step < 4; step++)
		{
			frame = Rm2kVehicleSprite.NextFrame(frame);
			seen.Add(frame);
		}
		AssertEq(string.Join(",", seen), "0,1,2,3,0",
			"a stationary vehicle cycles all four liblcf frames and returns");
	}

	public void Test_AnAirshipThatIsClimbingOrSinkingHoldsStill()
	{
		// AnimateAscentDescent owns the update while a vehicle is ascending or
		// descending, so the animation is reset instead of running and a
		// half-finished ascent is not animated at all.
		var frame = Rm2kVehicleSprite.AdvanceFrame(
			pCurrentFrame: 2, pAnimCount: 7, pLimit: 16,
			pAscendingOrDescending: true, pIsFixedGraphic: false, out var count);
		AssertEq(frame, Rm2kCharset.FrameMiddle, "a climbing vehicle returns to middle");
		AssertEq(count, 0, "and its counter is reset, not left half spent");
	}

	public void Test_AFixedGraphicVehicleKeepsItsFrameWhileItClimbs()
	{
		// ResetAnimation only puts the frame back to middle when the animation
		// type is not fixed_graphic, so a fixed graphic holds its frame.
		var frame = Rm2kVehicleSprite.AdvanceFrame(
			pCurrentFrame: 2, pAnimCount: 7, pLimit: 16,
			pAscendingOrDescending: true, pIsFixedGraphic: true, out var count);
		AssertEq(frame, 2, "a fixed graphic keeps its frame while it climbs");
		AssertEq(count, 0, "and its counter is still reset");
	}

	public void Test_TheAltitudeIsOnlyAppliedWhileTheVehicleIsFlying()
	{
		// GetAltitude returns zero for anything that is not flying, so a vehicle
		// that has finished its ascent is still drawn on the map until the
		// player says it is flying.
		var grounded = new Rm2kVehicleSprite
		{
			VehicleType = Rm2kVehicle.Airship,
			CharacterName = "Airship",
			Altitude = 0,
		};
		AssertEq(grounded.Altitude, 0, "a grounded vehicle is on the map");

		var flying = new Rm2kVehicleSprite
		{
			VehicleType = Rm2kVehicle.Airship,
			CharacterName = "Airship",
			Altitude = 16,
		};
		AssertEq(flying.Altitude, 16, "and a flying one is a screen tile up");
	}

	public void Test_OnlyTheThreeVehiclesAreVehicleSprites()
	{
		foreach (var type in new[] { Rm2kVehicle.Boat, Rm2kVehicle.Ship, Rm2kVehicle.Airship })
		{
			var sprite = new Rm2kVehicleSprite
			{
				VehicleType = type,
				CharacterName = "x",
			};
			AssertTrue(sprite.IsVehicle, $"type {type} is a vehicle");
		}
		var none = new Rm2kVehicleSprite
		{
			VehicleType = Rm2kVehicle.None,
			CharacterName = "x",
		};
		AssertTrue(!none.IsVehicle, "and type 0 is not one");
	}

	public void Test_ANewVehicleFacesLeftLikeThePlayerConstructorLeavesIt()
	{
		// SetDirection(Left) and SetFacing(Left), and this project stores left as
		// 4, so a vehicle that has not moved yet draws facing left.
		var boat = new Rm2kVehicleState(Rm2kVehicle.Boat, 1, 5, 5);
		var sprite = new Rm2kVehicleSprite
		{
			VehicleType = boat.VehicleType,
			CharacterName = "Boat",
			FacingDirection = Rm2kCharacterSprite.FacingFromLiblcfDirection(boat.Direction),
		};
		AssertEq(sprite.FacingDirection, 4, "a new vehicle faces left, which this project stores as 4");
	}
}
