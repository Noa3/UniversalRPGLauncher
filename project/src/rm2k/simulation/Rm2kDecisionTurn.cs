using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// The decision key turn order, from the tail of
/// <c>Game_Player::UpdateMove</c>.
/// </summary>
/// <remarks>
/// <para>
/// When the player is standing still and presses the decision key, the Player
/// tries to board or leave a vehicle first and only falls through to the action
/// event check when that did not happen:
/// </para>
/// <code>
/// if (Input::IsTriggered(Input::DECISION)) {
///     if (!GetOnOffVehicle()) {
///         CheckActionEvent();
///     }
/// }
/// return;
/// </code>
/// <para>
/// Vehicle boarding therefore wins over an action event on the same tile, and
/// the turn ends there either way: the step counter is not incremented, so
/// standing on a boat does not count as a step towards an encounter. Both halves
/// matter. Trying the action event first would make a boat that has an event
/// standing next to it impossible to board, and falling through to the step
/// counter would let the player farm encounters by boarding.
/// </para>
/// <para>
/// The diagonal correction happens before anything else, from
/// <c>GetOnOffVehicle</c>: a diagonal direction becomes the facing, because a
/// vehicle is only ever boarded or left on a cardinal direction and leaving the
/// diagonal in place would make <c>GetOffVehicle</c>'s assertion fail.
/// </para>
/// </remarks>
public static class Rm2kDecisionTurn
{
	/// <summary>
	/// The four cardinal directions, from <c>IsDirectionDiagonal</c>'s
	/// complement. The liblcf order is up 0, right 1, down 2, left 3, and a
	/// diagonal is 4 to 7.
	/// </summary>
	public const int CardinalCount = 4;

	/// <summary>
	/// Whether a direction is diagonal, from <c>IsDirectionDiagonal</c>, which is
	/// a plain range check against the four cardinal directions.
	/// </summary>
	public static bool IsDirectionDiagonal(int pDirection)
	{
		return pDirection >= CardinalCount;
	}

	/// <summary>
	/// Replaces a diagonal direction with the facing, from the first two lines of
	/// <c>GetOnOffVehicle</c>. Returns the direction to use, and reports whether
	/// it had to be replaced so a caller can decide whether that changed
	/// anything.
	/// </summary>
	public static int ResolveDirection(int pDirection, int pFacing, out bool pReplaced)
	{
		if (!IsDirectionDiagonal(pDirection))
		{
			pReplaced = false;
			return pDirection;
		}
		pReplaced = true;
		return pFacing;
	}

	/// <summary>
	/// What the decision key did on this turn.
	/// </summary>
	public enum DecisionOutcome
	{
		/// <summary>A vehicle was boarded or left.</summary>
		HandledByVehicle,

		/// <summary>
		/// No vehicle, so the action event check runs. Whether it found anything
		/// is up to the caller's event scan, which is why this is not reported
		/// here.
		/// </summary>
		HandledByActionEvent,
	}

	/// <summary>
	/// Runs the decision turn in the Player's order and reports which branch took
	/// it.
	/// </summary>
	/// <param name="pBoarding">The player's boarding state to act on.</param>
	/// <param name="pVehicles">The three vehicles on the current map.</param>
	/// <param name="pMapId">The current map.</param>
	/// <param name="pPlayerX">The player's tile in x.</param>
	/// <param name="pPlayerY">The player's tile in y.</param>
	/// <param name="pDirection">The player's direction, which may be diagonal.</param>
	/// <param name="pFacing">The player's facing, used for a diagonal.</param>
	/// <param name="pPlayerMoveSpeed">The player's current move speed.</param>
	/// <param name="pPlayerIsStopping">Whether the player is standing still.</param>
	/// <param name="pAirshipIsStopping">
	/// Whether the airship is standing still. The Player requires this as well as
	/// the player's own check, and a caller that always passes true would let a
	/// drifting airship be boarded.
	/// </param>
	/// <param name="pCanEmbark">
	/// Whether the water tile in front allows boarding, which is
	/// <c>Game_Map::CanEmbarkShip</c> and needs the map's passable mask.
	/// </param>
	/// <param name="pCanDisembark">
	/// Whether the tile in front allows stepping off, which is
	/// <c>Game_Map::CanDisembarkShip</c>.
	/// </param>
	/// <param name="pBoardAirship">Commits an airship boarding, if that is what happened.</param>
	public static DecisionOutcome Run(
		Rm2kVehicleBoarding pBoarding,
		IReadOnlyList<Rm2kVehicleState> pVehicles,
		int pMapId,
		int pPlayerX, int pPlayerY,
		int pDirection, int pFacing,
		int pPlayerMoveSpeed,
		bool pPlayerIsStopping,
		bool pAirshipIsStopping,
		bool pCanEmbark,
		bool pCanDisembark,
		Action<Rm2kVehicleState, int, int, int>? pBoardAirship = null,
		Action<int, int, int, int>? pBeginEmbark = null,
		Action<int, int, int>? pBeginDisembark = null,
		Action<int>? pBeginAirshipDisembark = null)
	{
		if (pBoarding == null)
		{
			return DecisionOutcome.HandledByActionEvent;
		}

		// The diagonal correction comes first: everything below works on a
		// cardinal direction, and GetOffVehicle asserts there is no diagonal.
		var direction = ResolveDirection(pDirection, pFacing, out _);

		if (pBoarding.IsAboard)
		{
			var aboard = FindVehicle(pVehicles, pBoarding.VehicleType);
			if (aboard == null)
			{
				return DecisionOutcome.HandledByActionEvent;
			}
			if (pBoarding.VehicleType == Rm2kVehicle.Airship)
			{
				// An airship that is still rising or sinking cannot be left, and
				// the Player returns false so the action event check still runs.
				if (!Rm2kVehicleBoarding.CanLeaveAirship(aboard))
				{
					return DecisionOutcome.HandledByActionEvent;
				}
				pBeginAirshipDisembark?.Invoke(pBoarding.VehicleType);
				return DecisionOutcome.HandledByVehicle;
			}
			if (!pCanDisembark)
			{
				return DecisionOutcome.HandledByActionEvent;
			}
			var (frontX, frontY) = Rm2kVehicleBoarding.TileInFront(pPlayerX, pPlayerY, direction);
			pBeginDisembark?.Invoke(pBoarding.VehicleType, frontX, frontY);
			return DecisionOutcome.HandledByVehicle;
		}

		// The airship is checked on the player's own tile and needs the airship
		// to be standing still as well, from IsInPosition && IsStopping() for
		// both the player and the vehicle.
		var airship = FindVehicle(pVehicles, Rm2kVehicle.Airship);
		if (airship != null
			&& Rm2kVehicleBoarding.CanBoardAirshipOn(airship, pMapId, pPlayerX, pPlayerY, pPlayerIsStopping, pAirshipIsStopping))
		{
			pBoardAirship?.Invoke(airship, pMapId, pPlayerX, pPlayerY);
			return DecisionOutcome.HandledByVehicle;
		}

		// Otherwise a ship or a boat in front, checked ship first, and then the
		// passable mask. The Player moves the player onto the water before it
		// records the boarding, so a refused step leaves nothing behind.
		var (lookX, lookY) = Rm2kVehicleBoarding.TileInFront(pPlayerX, pPlayerY, direction);
		var type = Rm2kVehicleBoarding.VehicleInFront(pVehicles, pMapId, lookX, lookY);
		if (type == Rm2kVehicle.None || !pCanEmbark)
		{
			return DecisionOutcome.HandledByActionEvent;
		}
		pBeginEmbark?.Invoke(type, pPlayerMoveSpeed, lookX, lookY);
		return DecisionOutcome.HandledByVehicle;
	}

	private static Rm2kVehicleState? FindVehicle(
		IReadOnlyList<Rm2kVehicleState> pVehicles, int pVehicleType)
	{
		if (pVehicles == null)
		{
			return null;
		}
		foreach (var vehicle in pVehicles)
		{
			if (vehicle.VehicleType == pVehicleType)
			{
				return vehicle;
			}
		}
		return null;
	}
}
