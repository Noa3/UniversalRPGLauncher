using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// The RPG Maker 2000 move route, reproduced from
/// <c>Game_Character::UpdateMoveRoute</c> in the EasyRPG Player.
/// </summary>
/// <remarks>
/// <para>
/// A move route is not a queue of moves that runs to completion. It is
/// advanced one command per update, and a command that starts a step returns
/// immediately: the character then spends sixteen updates walking before the
/// next command is even looked at. That is what makes a route look like a
/// character walking rather than teleporting along a list.
/// </para>
/// <para>
/// The command ids come from liblcf's own <c>generator/csv/enums.csv</c>, where
/// <c>rpg::MoveCommand::Code</c> is defined from <c>move_up = 0</c> to
/// <c>decrease_transp = 41</c>. They are not sequential in a way that could be
/// guessed: the movement commands are 0 to 11, the facing commands 12 to 22,
/// and everything else is 23 upwards.
/// </para>
/// </remarks>
public static class Rm2kMoveRoute
{
    /// <summary>liblcf <c>rpg::MoveCommand::Code::move_up</c>.</summary>
    public const int MoveUp = 0;

    public const int MoveRight = 1;
    public const int MoveDown = 2;
    public const int MoveLeft = 3;
    public const int MoveUpRight = 4;
    public const int MoveDownRight = 5;
    public const int MoveDownLeft = 6;
    public const int MoveUpLeft = 7;
    public const int MoveRandom = 8;
    public const int MoveTowardsHero = 9;
    public const int MoveAwayFromHero = 10;
    public const int MoveForward = 11;
    public const int FaceUp = 12;
    public const int FaceRight = 13;
    public const int FaceDown = 14;
    public const int FaceLeft = 15;
    public const int Turn90DegreeRight = 16;
    public const int Turn90DegreeLeft = 17;
    public const int Turn180Degree = 18;
    public const int Turn90DegreeRandom = 19;
    public const int FaceRandomDirection = 20;
    public const int FaceHero = 21;
    public const int FaceAwayFromHero = 22;
    public const int Wait = 23;
    public const int BeginJump = 24;
    public const int EndJump = 25;
    public const int LockFacing = 26;
    public const int UnlockFacing = 27;
    public const int IncreaseMovementSpeed = 28;
    public const int DecreaseMovementSpeed = 29;
    public const int IncreaseMovementFrequence = 30;
    public const int DecreaseMovementFrequence = 31;
    public const int SwitchOn = 32;
    public const int SwitchOff = 33;
    public const int ChangeGraphic = 34;
    public const int PlaySoundEffect = 35;
    public const int WalkEverywhereOn = 36;
    public const int WalkEverywhereOff = 37;
    public const int StopAnimation = 38;
    public const int StartAnimation = 39;
    public const int IncreaseTransp = 40;
    public const int DecreaseTransp = 41;

    /// <summary>Highest defined command id.</summary>
    public const int MaxCommandId = DecreaseTransp;

    /// <summary>
    /// <c>GetDirection90DegreeRight</c>: <c>(dir + 1) % 4</c> on the liblcf
    /// direction order up, right, down, left. Named apart from the command
    /// constant of the same id, because the constant is the stored value and
    /// this is the rotation.
    /// </summary>
    public static int TurnRight(int pDirection)
    {
        return (pDirection + 1) % 4;
    }

    /// <summary>
    /// <c>GetDirection90DegreeLeft</c>: <c>(dir + 3) % 4</c>, the mirror of
    /// the right turn.
    /// </summary>
    public static int TurnLeft(int pDirection)
    {
        return (pDirection + 3) % 4;
    }

    /// <summary>
    /// <c>GetDirection180Degree</c>.
    /// </summary>
    public static int TurnHalf(int pDirection)
    {
        return (pDirection + 2) % 4;
    }

    /// <summary>
    /// The direction a movement command selects, or -1 when the command is not
    /// a movement command. Movement commands are the contiguous range
    /// <c>move_up</c> to <c>move_forward</c>, which is how the Player
    /// recognises them: <c>cmd &gt;= move_up &amp;&amp; cmd &lt;= move_forward</c>.
    /// </summary>
    public static bool IsMovementCommand(int pCommandId)
    {
        return pCommandId >= MoveUp && pCommandId <= MoveForward;
    }

    /// <summary>
    /// The direction a facing command selects, or -1 when the command is not a
    /// facing command. This is the contiguous range <c>face_up</c> to
    /// <c>face_away_from_hero</c>.
    /// </summary>
    public static bool IsFacingCommand(int pCommandId)
    {
        return pCommandId >= FaceUp && pCommandId <= FaceAwayFromHero;
    }

    /// <summary>
    /// The direction a facing command sets, for the commands that name one.
    /// The turning and hero relative commands need the surrounding state and
    /// are applied by the caller.
    /// </summary>
    public static int FacingCommandDirection(int pCommandId)
    {
        return pCommandId switch
        {
            FaceUp => 0,
            FaceRight => 1,
            FaceDown => 2,
            FaceLeft => 3,
            _ => -1,
        };
    }

    /// <summary>
    /// The direction a movement command sets, for the commands that name one.
    /// The eight named directions map straight onto the liblcf order, which is
    /// why the command ids 0 to 7 can be cast straight to a direction.
    /// </summary>
    public static int MovementCommandDirection(int pCommandId)
    {
        return pCommandId switch
        {
            MoveUp => 0,
            MoveRight => 1,
            MoveDown => 2,
            MoveLeft => 3,
            MoveUpRight => 4,
            MoveDownRight => 5,
            MoveDownLeft => 6,
            MoveUpLeft => 7,
            _ => -1,
        };
    }

    /// <summary>
    /// The delta a movement direction contributes per tile, from
    /// <c>GetDxFromDirection</c> and <c>GetDyFromDirection</c>. A diagonal
    /// moves one tile on each axis, never half a tile on both.
    /// </summary>
    public static (int Dx, int Dy) DirectionDelta(int pDirection)
    {
        var dx = (pDirection == 1 || pDirection == 4 || pDirection == 5) ? 1 : 0;
        var dy = (pDirection == 2 || pDirection == 5 || pDirection == 6) ? 1 : 0;
        if (pDirection == 3 || pDirection == 6 || pDirection == 7)
        {
            dx = -1;
        }
        if (pDirection == 0 || pDirection == 4 || pDirection == 7)
        {
            dy = -1;
        }
        return (dx, dy);
    }

    /// <summary>
    /// The bridge between the two direction orders in this project. A move route
    /// and the liblcf <c>EventPage::Direction</c> both use up 0, right 1, down 2,
    /// left 3, while <c>Rm2kMap.Event.Direction</c> and the passability masks
    /// use the RPG Maker byte 2 down, 4 left, 6 right, 8 up. Mixing the two
    /// silently turns a right step into a left one, so the conversion is named
    /// rather than inlined.
    /// </summary>
    public static byte FacingFromLiblcfDirection(int pDirection)
    {
        return pDirection switch
        {
            0 => 8,   // up
            1 => 6,   // right
            2 => 2,   // down
            3 => 4,   // left
            _ => 8,
        };
    }

    /// <summary>
    /// The inverse of <see cref="FacingFromLiblcfDirection"/>, so a route that
    /// turns can be stored back on the event in the order the rest of this
    /// project uses.
    /// </summary>
    public static int LiblcfFromFacingDirection(byte pDirection)
    {
        return pDirection switch
        {
            8 => 0,
            6 => 1,
            2 => 2,
            4 => 3,
            _ => 0,
        };
    }

    /// <summary>
    /// The passability bit a direction uses, from the RM2K bit order where down
    /// is 0x01, left 0x02, right 0x04 and up 0x08. The route's liblcf order has
    /// to be mapped onto it, because <c>IsPassableInDirection</c> speaks the bit
    /// order.
    /// </summary>
    public static byte PassabilityBitFromLiblcfDirection(int pDirection)
    {
        return pDirection switch
        {
            0 => 0x08, // up
            1 => 0x04, // right
            2 => 0x01, // down
            3 => 0x02, // left
            _ => 0x08,
        };
    }

    /// <summary>
    /// The passability bit for the direction a step is leaving through, which is
    /// the opposite side of the tile it enters. RM2K stores a step as the bit
    /// the source may exit by on one side and the bit the target may be entered
    /// on the other, so both are checked and they are different bits.
    /// </summary>
    public static byte OppositePassabilityBit(int pDirection)
    {
        return pDirection switch
        {
            0 => 0x01, // leaving up means the down bit
            1 => 0x02, // leaving right means the left bit
            2 => 0x08, // leaving down means the up bit
            3 => 0x04, // leaving left means the right bit
            // A diagonal is refused by the caller, but a bit is still needed:
            // down is the conservative choice for an axis it cannot express.
            _ => 0x01,
        };
    }

    /// <summary>
    /// The highest move speed a route can raise, from
    /// <c>min(GetMoveSpeed() + 1, 6)</c>. The lowest is 1, from
    /// <c>max(GetMoveSpeed() - 1, 1)</c>.
    /// </summary>
    public static int ClampMoveSpeed(int pSpeed)
    {
        return Math.Clamp(pSpeed, 1, 6);
    }

    /// <summary>
    /// The highest move frequency a route can raise, from
    /// <c>min(GetMoveFrequency() + 1, 8)</c>. The lowest is 1.
    /// </summary>
    public static int ClampMoveFrequency(int pFrequency)
    {
        return Math.Clamp(pFrequency, 1, 8);
    }

    /// <summary>
    /// The name of a command, for diagnostics. An unknown id is reported by
    /// number rather than guessed, because a move route with a wrong command
    /// length desynchronises every following command.
    /// </summary>
    public static string Describe(int pCommandId)
    {
        return pCommandId switch
        {
            MoveUp => "MoveUp",
            MoveRight => "MoveRight",
            MoveDown => "MoveDown",
            MoveLeft => "MoveLeft",
            MoveUpRight => "MoveUpRight",
            MoveDownRight => "MoveDownRight",
            MoveDownLeft => "MoveDownLeft",
            MoveUpLeft => "MoveUpLeft",
            MoveRandom => "MoveRandom",
            MoveTowardsHero => "MoveTowardsHero",
            MoveAwayFromHero => "MoveAwayFromHero",
            MoveForward => "MoveForward",
            FaceUp => "FaceUp",
            FaceRight => "FaceRight",
            FaceDown => "FaceDown",
            FaceLeft => "FaceLeft",
            Turn90DegreeRight => "Turn90DegreeRight",
            Turn90DegreeLeft => "Turn90DegreeLeft",
            Turn180Degree => "Turn180Degree",
            Turn90DegreeRandom => "Turn90DegreeRandom",
            FaceRandomDirection => "FaceRandomDirection",
            FaceHero => "FaceHero",
            FaceAwayFromHero => "FaceAwayFromHero",
            Wait => "Wait",
            BeginJump => "BeginJump",
            EndJump => "EndJump",
            LockFacing => "LockFacing",
            UnlockFacing => "UnlockFacing",
            IncreaseMovementSpeed => "IncreaseMovementSpeed",
            DecreaseMovementSpeed => "DecreaseMovementSpeed",
            IncreaseMovementFrequence => "IncreaseMovementFrequence",
            DecreaseMovementFrequence => "DecreaseMovementFrequence",
            SwitchOn => "SwitchOn",
            SwitchOff => "SwitchOff",
            ChangeGraphic => "ChangeGraphic",
            PlaySoundEffect => "PlaySoundEffect",
            WalkEverywhereOn => "WalkEverywhereOn",
            WalkEverywhereOff => "WalkEverywhereOff",
            StopAnimation => "StopAnimation",
            StartAnimation => "StartAnimation",
            IncreaseTransp => "IncreaseTransp",
            DecreaseTransp => "DecreaseTransp",
            _ => $"0x{pCommandId:X}",
        };
    }

    /// <summary>
    /// Whether a command id is one the format defines. An unknown id is not a
    /// runtime error to swallow: a route stores no per-command length, so an
    /// unknown command still consumes exactly one slot, and refusing it is the
    /// honest answer.
    /// </summary>
    public static bool IsKnownCommand(int pCommandId)
    {
        return pCommandId >= 0 && pCommandId <= MaxCommandId;
    }
}
