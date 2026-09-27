using System;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// The airship shadow, from <c>Sprite_AirshipShadow</c>.
/// </summary>
/// <remarks>
/// <para>
/// The shadow is not part of the airship sprite and not a generic drop shadow.
/// It is its own sprite, built from two 16 by 16 patches of the System graphic
/// at <c>(128, 32)</c> and <c>(144, 32)</c>, blitted together and drawn at 26
/// percent opacity. The Player notes in a comment that 26 percent looks right
/// but is not what RPG_RT does, and this repository reproduces the Player's
/// value rather than guessing at an inaccuracy that cannot be measured without
/// a real game.
/// </para>
/// <para>
/// It is only visible while the player is in the airship, and it is drawn one
/// below the airship's own screen z so the airship covers it. Without the
/// shadow an airship a screen tile up looks like it is floating over the wrong
/// part of the map.
/// </para>
/// </remarks>
public static class Rm2kAirshipShadow
{
    /// <summary>The System graphic patch count: two 16 by 16 cells side by side.</summary>
    public const int PatchCount = 2;

    /// <summary>One patch, <c>16x16</c> tiles, which is the <c>TILE_SIZE</c> square.</summary>
    public const int PatchSize = 16;

    /// <summary>The left edge of the first patch in the System graphic.</summary>
    public const int FirstPatchX = 128;

    /// <summary>The row the patches sit in.</summary>
    public const int PatchY = 32;

    /// <summary>
    /// The opacity the Player uses, <c>Opacity(0.26 * 255)</c> truncated to an
    /// integer, which is 66 and not 67. Rounding up would make the shadow
    /// slightly darker than the Player draws it.
    /// </summary>
    public const int Opacity255 = 66;

    /// <summary>The source rectangles of the two patches, in draw order.</summary>
    public static (int X, int Y, int Width, int Height) PatchFor(int pIndex)
    {
        if (pIndex < 0 || pIndex >= PatchCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pIndex), pIndex, $"An airship shadow has {PatchCount} patches.");
        }
        return (FirstPatchX + pIndex * PatchSize, PatchY, PatchSize, PatchSize);
    }

    /// <summary>
    /// The screen z the shadow is drawn at, from <c>SetZ(airship-&gt;GetScreenZ()
    /// - 1)</c>. The minus one is what puts the shadow behind the airship
    /// instead of in front of it.
    /// </summary>
    public static int ScreenZ(int pAirshipScreenZ)
    {
        return pAirshipScreenZ - 1;
    }

    /// <summary>
    /// Whether the shadow is drawn, from <c>Sprite_AirshipShadow::Update</c>:
    /// only while the player is in the airship.
    /// </summary>
    public static bool IsVisible(bool pPlayerInAirship)
    {
        return pPlayerInAirship;
    }
}
