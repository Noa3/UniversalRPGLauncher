using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// The three RM2K vehicles and the boarding rules, reproduced from
/// <c>Game_Player::GetOnVehicle</c> and <c>Game_Player::GetOffVehicle</c>.
/// </summary>
/// <remarks>
/// <para>
/// The types are liblcf's own values, from <c>Game_Vehicle::Type</c>:
/// <c>None = 0, Boat = 1, Ship = 2, Airship = 3</c>. They are stored in the
/// player's save data, so the numbers are part of the format rather than an
/// internal detail.
/// </para>
/// <para>
/// Boarding is not a single rule but two. An airship is boarded by standing on
/// it, while a boat or a ship is boarded by facing it and stepping onto it, and
/// the water tile the vehicle is on has to be passable in the direction of the
/// step. Getting on and getting off are not symmetric: getting off an airship
/// starts a descent rather than moving the player, and the player leaves a boat
/// or ship by stepping off the water tile.
/// </para>
/// </remarks>
public static class Rm2kVehicle
{
	/// <summary>liblcf <c>Game_Vehicle::Type::None</c>.</summary>
	public const int None = 0;

	/// <summary>liblcf <c>Game_Vehicle::Type::Boat</c>.</summary>
	public const int Boat = 1;

	/// <summary>liblcf <c>Game_Vehicle::Type::Ship</c>.</summary>
	public const int Ship = 2;

	/// <summary>liblcf <c>Game_Vehicle::Type::Airship</c>.</summary>
	public const int Airship = 3;

	/// <summary>
	/// liblcf <c>rpg::EventPage::MoveSpeed_normal</c> is 4, and the Player gives
	/// a boat and a ship that speed. This is not the same as the default event
	/// speed of 3, which is <c>MoveSpeed_half</c>.
	/// </summary>
	public const int MoveSpeedNormal = 4;

	/// <summary>
	/// liblcf <c>rpg::EventPage::MoveSpeed_double</c> is 5, and the Player gives
	/// the airship that speed.
	/// </summary>
	public const int MoveSpeedDouble = 5;

	/// <summary>
	/// The move speed the Player gives a vehicle of this type, from the
	/// constructor's switch: normal for a boat and a ship, double for the
	/// airship, and nothing for <c>None</c>.
	/// </summary>
	public static int MoveSpeedForType(int pVehicleType)
	{
		return pVehicleType switch
		{
			Boat => MoveSpeedNormal,
			Ship => MoveSpeedNormal,
			Airship => MoveSpeedDouble,
			_ => 0,
		};
	}

	/// <summary>
	/// The name of a vehicle type, for diagnostics. An unknown value is reported
	/// by number, because a vehicle type comes from a file and a guess here
	/// would put the player on the wrong sprite.
	/// </summary>
	public static string DescribeType(int pVehicleType)
	{
		return pVehicleType switch
		{
			None => "None",
			Boat => "Boat",
			Ship => "Ship",
			Airship => "Airship",
			_ => $"0x{pVehicleType:X}",
		};
	}

	/// <summary>
	/// Whether a vehicle type is one the format defines. The player's save data
	/// carries this number, so a corrupted value has to be refused rather than
	/// treated as "no vehicle", which would silently put the player somewhere
	/// they never chose.
	/// </summary>
	public static bool IsKnownType(int pVehicleType)
	{
		return pVehicleType >= None && pVehicleType <= Airship;
	}
}

/// <summary>
/// One vehicle's live state, from <c>Game_Vehicle</c>.
/// </summary>
/// <remarks>
/// The airship carries two counters the other vehicles do not: the remaining
/// ascent and the remaining descent, both drawn down by 8 per update from
/// <c>AnimateAscentDescent</c>. The airship is not drawn at all while it is
/// ascending or descending, which is why the sprite is not simply the charset
/// cell.
/// </remarks>
public sealed class Rm2kVehicleState
{
	/// <summary>liblcf <c>Game_Vehicle::Type</c>.</summary>
	public int VehicleType { get; }

	public int MapId { get; set; }
	public int X { get; set; }
	public int Y { get; set; }

	/// <summary>The direction the vehicle is drawn facing, in the liblcf order.</summary>
	public int Direction { get; set; } = 3;

	/// <summary>The sprite cell index, from the system section's index field.</summary>
	public int SpriteIndex { get; set; }

	/// <summary>The character name, from the system section's name field.</summary>
	public string CharacterName { get; set; } = "";

	/// <summary>liblcf <c>EventPage::move_speed</c> for this vehicle.</summary>
	public int MoveSpeed { get; }

	/// <summary>Whether the airship is in the air, from <c>IsFlying</c>.</summary>
	public bool IsFlying { get; set; }

	/// <summary>
	/// Remaining ascent, from <c>remaining_ascent</c>. The counter is a
	/// <c>SCREEN_TILE_SIZE</c> budget of 256, not a pixel height, and the
	/// altitude the sprite is drawn at is derived from it in whole tiles.
	/// </summary>
	public int RemainingAscent { get; private set; }

	/// <summary>Remaining descent, from <c>remaining_descent</c>.</summary>
	public int RemainingDescent { get; private set; }

	/// <summary>
	/// <c>SCREEN_TILE_SIZE</c>, the unit both ascent and descent are counted in.
	/// </summary>
	public const int ScreenTileSize = 256;

	/// <summary>
	/// How much of the ascent or descent budget one update consumes, from
	/// <c>remaining_ascent - 8</c> in <c>AnimateAscentDescent</c>. At 8 per
	/// update a full ascent or descent takes 32 updates.
	/// </summary>
	public const int AscentDescentStep = 8;

	public Rm2kVehicleState(int pVehicleType, int pMapId, int pX, int pY)
	{
		VehicleType = pVehicleType;
		MapId = pMapId;
		X = pX;
		Y = pY;
		MoveSpeed = Rm2kVehicle.MoveSpeedForType(pVehicleType);

		// The constructor sets both direction and facing to left, from
		// SetDirection(Left) and SetFacing(Left) in Game_Vehicle's constructor.
		Direction = 3;
	}

	/// <summary>
	/// <c>IsInPosition</c>: the vehicle has to be on the current map before its
	/// position means anything, so a vehicle whose start map is another map is
	/// not "here" even when its coordinates happen to match.
	/// </summary>
	public bool IsInPosition(int pMapId, int pX, int pY)
	{
		return MapId == pMapId && X == pX && Y == pY;
	}

	/// <summary><c>IsAscending</c>.</summary>
	public bool IsAscending => RemainingAscent > 0;

	/// <summary><c>IsDescending</c>.</summary>
	public bool IsDescending => RemainingDescent > 0;

	/// <summary><c>IsAscendingOrDescending</c>.</summary>
	public bool IsAscendingOrDescending => IsAscending || IsDescending;

	/// <summary><c>StartAscent</c>.</summary>
	public void StartAscent()
	{
		RemainingAscent = ScreenTileSize;
		RemainingDescent = 0;
	}

	/// <summary><c>StartDescent</c>.</summary>
	public void StartDescent()
	{
		RemainingDescent = ScreenTileSize;
		RemainingAscent = 0;
	}

	/// <summary><c>ForceLand</c>: ends both counters at once.</summary>
	public void ForceLand()
	{
		RemainingAscent = 0;
		RemainingDescent = 0;
		IsFlying = false;
	}

	/// <summary>
	/// <c>AnimateAscentDescent</c>: the ascent and descent counters drop by 8
	/// per update, and the airship stops flying when the descent runs out. The
	/// Player then lands if it can and otherwise starts another ascent, so an
	/// airship that cannot land keeps trying rather than hovering forever.
	/// </summary>
	/// <param name="pCanLand">Whether the airship's tile allows landing.</param>
	/// <returns>True when an ascent or descent was in progress.</returns>
	public bool AnimateAscentDescent(bool pCanLand)
	{
		if (IsAscending)
		{
			RemainingAscent -= AscentDescentStep;
			return true;
		}
		if (IsDescending)
		{
			RemainingDescent -= AscentDescentStep;
			if (!IsDescending)
			{
				IsFlying = false;
				if (pCanLand)
				{
					SetDefaultDirection();
				}
				else
				{
					StartAscent();
				}
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// <c>GetAltitude</c>: the height in whole tiles the vehicle is drawn at,
	/// which is only non zero while it is flying. The airship is one tile high at
	/// the top of its ascent, so a vehicle that has finished ascending is drawn
	/// a tile above the map and only comes down when it starts descending.
	/// </summary>
	public int GetAltitude()
	{
		if (!IsFlying)
		{
			return 0;
		}
		if (IsAscending)
		{
			return (ScreenTileSize - RemainingAscent) / (ScreenTileSize / TilesPerScreenTile);
		}
		if (IsDescending)
		{
			return RemainingDescent / (ScreenTileSize / TilesPerScreenTile);
		}
		return ScreenTileSize / (ScreenTileSize / TilesPerScreenTile);
	}

	/// <summary>
	/// <c>TILE_SIZE</c> is 16 pixels and <c>SCREEN_TILE_SIZE</c> is 256, so one
	/// screen tile is sixteen map tiles. The Player divides by that to express
	/// the altitude in whole tiles.
	/// </summary>
	public const int TilesPerScreenTile = 16;

	/// <summary>
	/// <c>SetDefaultDirection</c>: the Player turns a landing vehicle to face
	/// left, which in the liblcf order is 3.
	/// </summary>
	public void SetDefaultDirection()
	{
		Direction = 3;
	}
}
