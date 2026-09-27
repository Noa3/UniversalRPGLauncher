using System;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// RM2K character sprite geometry, verified in EasyRPG Player
/// <c>src/sprite_character.cpp</c> and <c>src/cache.cpp</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>GetCharacterRect</c> builds a cell of <c>24 * (TILE_SIZE / 16) * 3</c> by
/// <c>32 * (TILE_SIZE / 16) * 4</c>, which is 72 by 128 pixels with
/// <c>TILE_SIZE = 16</c>, and places it at <c>(index % 4, index / 4)</c>. Each
/// cell holds a 3 by 4 grid of frames, so one frame is 24 by 32 pixels.
/// </para>
/// <para>
/// <c>Sprite_Character::Draw</c> takes <c>row = character->GetFacing()</c> and
/// <c>frame = character->GetAnimFrame()</c>, clamping anything from
/// <c>Frame_middle2</c> down to <c>Frame_middle</c>. liblcf
/// <c>rpg::EventPage::Frame</c> is <c>left = 0, middle = 1, right = 2,
/// middle2 = 3</c>. <c>Game_Character::UpdateFacing</c> sets the facing to the
/// direction directly for the four cardinal directions, so
/// <c>rpg::EventPage::Direction</c> <c>up = 0, right = 1, down = 2, left = 3</c>
/// is the row directly.
/// </para>
/// <para>
/// The Player offsets the sprite with <c>SetOx(chara_width / 2)</c> and
/// <c>SetOy(chara_height)</c>, which centres it horizontally on the tile and puts
/// its feet on the tile bottom.
/// </para>
/// </remarks>
public sealed class Rm2kCharset
{
    /// <summary>Width of one charset cell, from <c>24 * 3</c>.</summary>
    public const int CellWidth = 72;

    /// <summary>Height of one charset cell, from <c>32 * 4</c>.</summary>
    public const int CellHeight = 128;

    /// <summary>Frames per row inside a cell, from the 24 pixel divisor.</summary>
    public const int FramesPerRow = 3;

    /// <summary>Frames per column inside a cell, from the 32 pixel divisor.</summary>
    public const int FramesPerColumn = 4;

    /// <summary>Charset cells per image row, from the <c>index % 4</c> split.</summary>
    public const int CellsPerRow = 4;

    /// <summary>Width of one character frame.</summary>
    public const int FrameWidth = CellWidth / FramesPerRow;

    /// <summary>Height of one character frame.</summary>
    public const int FrameHeight = CellHeight / FramesPerColumn;

    /// <summary>liblcf <c>rpg::EventPage::Frame</c>.</summary>
    public const int FrameLeft = 0;
    public const int FrameMiddle = 1;
    public const int FrameRight = 2;
    public const int FrameMiddle2 = 3;

    /// <summary>liblcf <c>rpg::EventPage::Direction</c>, which is also the facing row.</summary>
    public const int DirectionUp = 0;
    public const int DirectionRight = 1;
    public const int DirectionDown = 2;
    public const int DirectionLeft = 3;

    private readonly Rm2kIndexedImage _image;

    public Rm2kCharset(Rm2kIndexedImage pImage)
    {
        _image = pImage ?? throw new ArgumentNullException(nameof(pImage));
    }

    public int Width => _image.Width;
    public int Height => _image.Height;

    /// <summary>
    /// Converts a facing direction as this project stores it (2 down, 4 left,
    /// 6 right, 8 up) into the liblcf direction index the Player uses as the row.
    /// Directions the Player cannot face map to <see cref="DirectionUp"/>, which
    /// is the value a freshly created character starts with.
    /// </summary>
    public static int FacingToRow(byte pFacingDirection)
    {
        return pFacingDirection switch
        {
            6 => DirectionRight,
            4 => DirectionLeft,
            2 => DirectionDown,
            _ => DirectionUp,
        };
    }

    /// <summary>
    /// Clamps a frame like the Player does: anything from
    /// <c>Frame_middle2</c> is shown as <c>Frame_middle</c>.
    /// </summary>
    public static int ClampFrame(int pFrame)
    {
        if (pFrame < FrameLeft)
        {
            return FrameLeft;
        }
        return pFrame >= FrameMiddle2 ? FrameMiddle : pFrame;
    }

    /// <summary>
    /// Top left pixel of the charset cell for a character index, or false when
    /// the index does not fit the image.
    /// </summary>
    public bool TryGetCell(int pCharacterIndex, out int pSourceX, out int pSourceY)
    {
        pSourceX = 0;
        pSourceY = 0;
        if (pCharacterIndex < 0)
        {
            return false;
        }
        pSourceX = (pCharacterIndex % CellsPerRow) * CellWidth;
        pSourceY = (pCharacterIndex / CellsPerRow) * CellHeight;
        return pSourceX + CellWidth <= Width && pSourceY + CellHeight <= Height;
    }

    /// <summary>
    /// Top left pixel of one character frame inside a charset cell.
    /// </summary>
    public static bool TryGetFrameRect(
        int pSourceX, int pSourceY, int pFacingRow, int pFrame,
        out int pFrameX, out int pFrameY)
    {
        pFrameX = 0;
        pFrameY = 0;
        if (pFacingRow < 0 || pFacingRow >= FramesPerColumn)
        {
            return false;
        }
        var frame = ClampFrame(pFrame);
        if (frame >= FramesPerRow)
        {
            return false;
        }
        pFrameX = pSourceX + frame * FrameWidth;
        pFrameY = pSourceY + pFacingRow * FrameHeight;
        return true;
    }

    /// <summary>
    /// Draws a character frame so its feet stand on the bottom of the map tile at
    /// <c>(pMapX, pMapY)</c>, using the Player's <c>SetOx(chara_width / 2)</c> and
    /// <c>SetOy(chara_height)</c> offsets. Transparent charset pixels leave the
    /// background untouched.
    /// </summary>
    /// <param name="pOffsetX">
    /// Horizontal scroll offset in pixels. The Player offsets a character sprite
    /// by the same value it offsets the tile layers with, so the character stays
    /// on its map tile while the map scrolls.
    /// </param>
    /// <param name="pOffsetY">Vertical scroll offset in pixels.</param>
    public bool TryDrawCharacter(
        int pCharacterIndex, byte pFacingDirection, int pFrame,
        Rm2kPixelBuffer pTarget, int pMapX, int pMapY, int pOffsetX = 0, int pOffsetY = 0)
    {
        ArgumentNullException.ThrowIfNull(pTarget);
        if (!TryGetCell(pCharacterIndex, out var cellX, out var cellY)
            || !TryGetFrameRect(cellX, cellY, FacingToRow(pFacingDirection), pFrame, out var frameX, out var frameY))
        {
            return false;
        }
        var targetX = (pMapX + 1) * Rm2kIndexedImage.MapTileSize - FrameWidth / 2 + pOffsetX;
        var targetY = (pMapY + 1) * Rm2kIndexedImage.MapTileSize - FrameHeight + pOffsetY;
        return _image.TryBlitRectangle(frameX, frameY, FrameWidth, FrameHeight, pTarget, targetX, targetY);
    }

    /// <summary>Number of complete charset cells the image holds.</summary>
    public int CellCapacity => (Width / CellWidth) * (Height / CellHeight);
}
