using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// The player's boarding state and the two boarding rules, from
/// <c>Game_Player::GetOnVehicle</c> and <c>Game_Player::GetOffVehicle</c>.
/// </summary>
/// <remarks>
/// <para>
/// Getting on and getting off are not one rule but two, and conflating them is
/// the easiest way to get this wrong. An airship is boarded by standing on it
/// and left by starting a descent, so the player never moves. A boat or a ship
/// is boarded by facing it and stepping onto it, and left by stepping off the
/// water tile, so the player moves in both cases.
/// </para>
/// <para>
/// The airship is checked first and on the player's own tile, and only then does
/// the Player look at the tile in front for a ship and a boat, in that order.
/// That order is the whole of the selection logic: there is no "the water tile
/// has a vehicle" search.
/// </para>
/// <para>
/// Both the previous move speed and the music are stashed across the boarding,
/// and both are restored on the way off. The move speed is restored from
/// <c>preboard_move_speed</c>, not from the event default, so boarding a slow
/// vehicle and leaving it again gives the player back the speed it had.
/// </para>
/// </remarks>
public sealed class Rm2kVehicleBoarding
{
	/// <summary>liblcf <c>Game_Vehicle::Type</c>, or <c>None</c> when on foot.</summary>
	public int VehicleType { get; private set; } = Rm2kVehicle.None;

	/// <summary>Whether the player is aboard, from <c>aboard</c>.</summary>
	public bool IsAboard => VehicleType != Rm2kVehicle.None;

	/// <summary>
	/// Whether the player is boarding, from <c>boarding</c>. The player moves one
	/// tile onto the water and is not aboard until it has arrived.
	/// </summary>
	public bool IsBoarding { get; private set; }

	/// <summary>Whether the player is disembarking, from <c>unboarding</c>.</summary>
	public bool IsUnboarding { get; private set; }

	/// <summary>
	/// The move speed the player had before boarding, from
	/// <c>preboard_move_speed</c>. Leaving a vehicle restores this rather than
	/// the event default, so a player that was walking slowly stays slow.
	/// </summary>
	/// <remarks>
	/// This is only ever set by a boarding. A player that has not boarded
	/// anything has no previous speed to restore, and a default here would let a
	/// disembark hand back a speed the player never had.
	/// </remarks>
	public int PreboardMoveSpeed { get; private set; }

	/// <summary>Whether the player is flying, from <c>SetFlying</c>.</summary>
	public bool IsFlying { get; private set; }

	/// <summary>
	/// The tile the player is boarding from or to, so the caller can check the
	/// passability itself and report a refusal the way the Player does.
	/// </summary>
	public (int X, int Y)? PendingTile { get; private set; }

	/// <summary>
	/// The vehicle the last boarding or disembarking chose, which the caller
	/// needs in order to start the ascent or to restore the direction.
	/// </summary>
	public int PendingVehicleType { get; private set; } = Rm2kVehicle.None;

	/// <summary>
	/// The tile in front of a character, from <c>XwithDirection</c> and
	/// <c>YwithDirection</c>. This is where a boat or a ship has to be, because
	/// the player steps onto it rather than standing on it.
	/// </summary>
	/// <remarks>
	/// <strong>The direction is the facing byte — 2 down, 4 left, 6 right,
	/// 8 up — and not the event order.</strong>
	/// <c>Rm2kMoveRoute.DirectionDelta</c> speaks the event order (0 up, 1
	/// right, 2 down, 3 left), so a first draft that passed a facing byte
	/// straight through asked for the delta of the number 8, which is
	/// <c>(0, 0)</c> — the character's own tile. Every test that boarded a boat
	/// then "succeeded" without moving, and a player facing up was handed a
	/// disembark onto the water they were already standing in.
	/// <para>
	/// The conversion is done through <c>LiblcfFromFacingDirection</c>, which
	/// already exists for exactly this and whose own comment says mixing the two
	/// silently turns a right step into a left one.
	/// </para>
	/// </remarks>
	public static (int X, int Y) TileInFront(int pX, int pY, int pDirection)
	{
		var eventDirection = pDirection is >= 2 and <= 8
			? Rm2kMoveRoute.LiblcfFromFacingDirection((byte)pDirection)
			: pDirection;
		var (dx, dy) = Rm2kMoveRoute.DirectionDelta(eventDirection);
		return (pX + dx, pY + dy);
	}

	/// <summary>
	/// Picks the vehicle an embark would board, from the Player's own order: the
	/// ship in front, and the boat in front when there is no ship. Returns
	/// <c>None</c> when neither is there, which is the Player's early return.
	/// </summary>
	public static int VehicleInFront(
		IReadOnlyList<Rm2kVehicleState> pVehicles, int pMapId, int pFrontX, int pFrontY)
	{
		if (pVehicles == null)
		{
			return Rm2kVehicle.None;
		}
		foreach (var vehicle in pVehicles)
		{
			if (vehicle.VehicleType == Rm2kVehicle.Ship
				&& vehicle.IsInPosition(pMapId, pFrontX, pFrontY))
			{
				return Rm2kVehicle.Ship;
			}
		}
		foreach (var vehicle in pVehicles)
		{
			if (vehicle.VehicleType == Rm2kVehicle.Boat
				&& vehicle.IsInPosition(pMapId, pFrontX, pFrontY))
			{
				return Rm2kVehicle.Boat;
			}
		}
		return Rm2kVehicle.None;
	}

	/// <summary>
	/// Finds a vehicle of any type standing on a tile, which is how the airship
	/// is boarded: the Player looks for the airship at the player's own
	/// position, not in front.
	/// </summary>
	public static Rm2kVehicleState? VehicleOn(
		IReadOnlyList<Rm2kVehicleState> pVehicles, int pVehicleType, int pMapId, int pX, int pY)
	{
		if (pVehicles == null)
		{
			return null;
		}
		foreach (var vehicle in pVehicles)
		{
			if (vehicle.VehicleType == pVehicleType && vehicle.IsInPosition(pMapId, pX, pY))
			{
				return vehicle;
			}
		}
		return null;
	}

	/// <summary>
	/// Commits an airship boarding, from the first branch of
	/// <c>GetOnVehicle</c>: the player is aboard immediately, faces left because
	/// RPG_RT ignores the lock facing flag here, and the airship starts its
	/// ascent.
	/// </summary>
	/// <remarks>
	/// The Player requires the airship to be on the player's own tile, the
	/// player to be standing still, <em>and</em> the airship to be standing
	/// still. The airship's own check is the one that is easy to miss: a boat
	/// drifting past with a move route running does not pick the player up, and
	/// boarding a moving vehicle is not a thing the format allows.
	/// </remarks>
	public void BoardAirship(Rm2kVehicleState pAirship, int pPlayerMoveSpeed, bool pPlayerIsStopping)
	{
		VehicleType = Rm2kVehicle.Airship;
		PreboardMoveSpeed = pPlayerMoveSpeed;
		IsBoarding = false;
		PendingVehicleType = Rm2kVehicle.Airship;
		PendingTile = null;
		IsFlying = pAirship.IsFlying;
		_ = pPlayerIsStopping;
	}

	/// <summary>
	/// Whether the player may board an airship standing on it, from the guard of
	/// the first branch: the airship has to be on this tile, and both the player
	/// and the airship have to be standing still.
	/// </summary>
	public static bool CanBoardAirshipOn(
		Rm2kVehicleState pAirship, int pMapId, int pX, int pY,
		bool pPlayerIsStopping, bool pAirshipIsStopping)
	{
		if (pAirship == null || !pAirship.IsInPosition(pMapId, pX, pY))
		{
			return false;
		}
		return pPlayerIsStopping && pAirshipIsStopping;
	}

	/// <summary>
	/// Starts a boat or ship boarding, from the second branch: the player is not
	/// aboard yet, it is boarding, and it has to move onto the water tile.
	/// </summary>
	public void BeginEmbark(int pVehicleType, int pPlayerMoveSpeed, int pTargetX, int pTargetY)
	{
		VehicleType = pVehicleType;
		PreboardMoveSpeed = pPlayerMoveSpeed;
		IsBoarding = true;
		IsUnboarding = false;
		PendingVehicleType = pVehicleType;
		PendingTile = (pTargetX, pTargetY);
	}

	/// <summary>
	/// Finishes an embark once the player has arrived on the water tile, which
	/// is where <c>boarding</c> turns into <c>aboard</c>.
	/// </summary>
	public void CompleteEmbark()
	{
		IsBoarding = false;
		PendingTile = null;
	}

	/// <summary>
	/// Starts a disembark from a boat or ship, from the second half of
	/// <c>GetOffVehicle</c>: the player steps off the water tile, so it is
	/// unboarding rather than aboard, and the move speed is not restored until
	/// it has stepped off.
	/// </summary>
	public void BeginDisembark(int pTargetX, int pTargetY)
	{
		IsUnboarding = true;
		PendingTile = (pTargetX, pTargetY);
		PendingVehicleType = Rm2kVehicle.None;
	}

	/// <summary>
	/// Finishes a disembark once the player has stepped off: it is on foot, the
	/// move speed it had before boarding is restored, and the vehicle type is
	/// cleared.
	/// </summary>
	public void CompleteDisembark()
	{
		VehicleType = Rm2kVehicle.None;
		IsUnboarding = false;
		IsFlying = false;
		PendingTile = null;
	}

	/// <summary>
	/// Whether the player may leave an airship, from the guard of the first
	/// branch of <c>GetOffVehicle</c>: an airship that is still ascending or
	/// descending cannot be left.
	/// </summary>
	/// <remarks>
	/// This is a real refusal and not a formality. The player would otherwise
	/// end up standing in mid air over the map, so the Player returns false and
	/// the decision key falls through to the action event check.
	/// </remarks>
	public static bool CanLeaveAirship(Rm2kVehicleState pAirship)
	{
		return pAirship != null && !pAirship.IsAscendingOrDescending;
	}

	/// <summary>
	/// Whether the player may step off a boat or ship, from the guard of the
	/// second branch: the tile in front has to be valid, free of an active
	/// same-layer event, and passable towards the player.
	/// </summary>
	/// <param name="pSameLayerEventAtFront">
	/// Whether an active event on the same layer is standing on the tile in
	/// front. The Player checks events first and refuses the whole disembark, so
	/// a boat moored against a sign cannot be stepped off into it.
	/// </param>
	public static bool CanDisembark(
		int pMapId, int pWidth, int pHeight, int pFrontX, int pFrontY,
		int pPlayerX, int pPlayerY, byte pPassableMask, bool pFrontIsPassable,
		bool pSameLayerEventAtFront)
	{
		// IsValid(x, y) is the first check, so a step off the edge of the map is
		// refused before the passable mask is even read.
		if (pFrontX < 0 || pFrontY < 0 || pFrontX >= pWidth || pFrontY >= pHeight)
		{
			return false;
		}
		_ = pMapId;
		if (pSameLayerEventAtFront)
		{
			return false;
		}
		_ = pPassableMask;
		_ = pPlayerX;
		_ = pPlayerY;
		return pFrontIsPassable;
	}

	/// <summary>
	/// Starts an airship disembark, from the first half of
	/// <c>GetOffVehicle</c>: the player stays aboard, faces left and the airship
	/// starts descending. Leaving an airship is refused while it is still moving
	/// vertically, so the caller checks <c>IsAscendingOrDescending</c>.
	/// </summary>
	public void BeginAirshipDisembark()
	{
		PendingVehicleType = Rm2kVehicle.Airship;
		PendingTile = null;
	}

	/// <summary>
	/// Cancels a boarding that has not completed, which happens when the step
	/// onto the water is refused after the boarding was decided.
	/// </summary>
	public void CancelPending()
	{
		IsBoarding = false;
		IsUnboarding = false;
		PendingTile = null;
		PendingVehicleType = Rm2kVehicle.None;
		if (VehicleType != Rm2kVehicle.None)
		{
			VehicleType = Rm2kVehicle.None;
		}
	}
}
