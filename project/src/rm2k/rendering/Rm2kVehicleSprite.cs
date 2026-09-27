using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// A vehicle's sprite, from the system section's name and index fields.
/// </summary>
/// <remarks>
/// <para>
/// A vehicle is drawn from the same charset sheet as a character and with the
/// same cell geometry, which is <c>24 * 3</c> wide and <c>32 * 4</c> high. The
/// name and the index come from the LDB system section: <c>boat_name</c> with
/// <c>boat_index</c>, <c>ship_name</c> with <c>ship_index</c> and
/// <c>airship_name</c> with <c>airship_index</c>.
/// </para>
/// <para>
/// The two properties that are specific to a vehicle are the altitude and the
/// animation. A vehicle is drawn a whole number of tiles above the map while it
/// is flying, and the Player animates a vehicle more slowly than a character:
/// sixteen frames while it is standing still and twelve while it is moving,
/// where a character uses twelve and sixteen the other way round. An airship
/// that is ascending or descending is not animated at all.
/// </para>
/// </remarks>
public sealed class Rm2kVehicleSprite
{
    /// <summary>liblcf <c>Game_Vehicle::Type</c>.</summary>
    public int VehicleType { get; init; }

    /// <summary>The character name from the system section, which selects the charset.</summary>
    public required string CharacterName { get; init; }

    /// <summary>The cell index from the system section.</summary>
    public int CharacterIndex { get; init; }

    public int MapX { get; init; }
    public int MapY { get; init; }

    /// <summary>Facing as this project stores it: 2 down, 4 left, 6 right, 8 up.</summary>
    public byte FacingDirection { get; init; } = 4;

    /// <summary>liblcf frame; middle2 is clamped like the Player does.</summary>
    public int Frame { get; init; } = Rm2kCharset.FrameMiddle;

    /// <summary>
    /// The height in whole tiles the vehicle is drawn above the map, from
    /// <c>GetAltitude</c>. A grounded vehicle is at zero.
    /// </summary>
    public int Altitude { get; init; }

    /// <summary>Whether the vehicle is still travelling, which slows the animation.</summary>
    public bool IsMoving { get; init; }

    /// <summary>Whether the vehicle is climbing or sinking, which stops the animation.</summary>
    public bool IsAscendingOrDescending { get; init; }

    /// <summary>
    /// How many frames the vehicle is shown for before it advances, from
    /// <c>UpdateAnimation</c>: <c>GetStopCount() ? 16 : 12</c>. A moving
    /// vehicle therefore animates slightly faster than a standing one, which is
    /// the opposite of the character table and is easy to get backwards.
    /// </summary>
    public int AnimationLimit => IsMoving ? 12 : 16;

    /// <summary>Whether this vehicle is drawn as a boat, ship or airship sprite.</summary>
    public bool IsVehicle => VehicleType is 1 or 2 or 3;

    /// <summary>
    /// The animation frame a vehicle is showing, from the Player's update: an
    /// airship that is climbing or sinking holds its frame, and otherwise the
    /// counter advances and wraps at the limit.
    /// </summary>
    /// <remarks>
    /// The Player resets the animation while a vehicle is ascending or
    /// descending. <c>ResetAnimation</c> also puts the frame back to middle, but
    /// only when the animation type is not <c>fixed_graphic</c>, so a vehicle
    /// that lands does not resume on a stale frame unless it is a fixed graphic.
    /// </remarks>
    public static int AdvanceFrame(
        int pCurrentFrame, int pAnimCount, int pLimit, bool pAscendingOrDescending,
        bool pIsFixedGraphic, out int pNewCount)
    {
        if (pAscendingOrDescending)
        {
            pNewCount = 0;
            return pIsFixedGraphic ? pCurrentFrame : Rm2kCharset.FrameMiddle;
        }
        pNewCount = pAnimCount + 1;
        return pAnimCount + 1 >= pLimit
            ? NextFrame(pCurrentFrame)
            : pCurrentFrame;
    }

    /// <summary>
    /// The frame after a vehicle's current one, from
    /// <c>IncAnimFrame</c>: <c>anim_frame = (anim_frame + 1) % 4</c>.
    /// </summary>
    /// <remarks>
    /// The wrap is a modulo over all four liblcf frames, not a clamp at
    /// middle2. <c>Frame_middle2</c> is a real state that the sprite clamp turns
    /// into middle when it is drawn, so a vehicle walks left, middle, right,
    /// middle2 and then back to left.
    /// </remarks>
    public static int NextFrame(int pFrame)
    {
        return (pFrame + 1) % 4;
    }
}
