using System;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// The RPG Maker 2000 per frame step budget for character movement, reproduced
/// from <c>Game_Character::Update</c>, <c>UpdateMovement</c>, <c>Move</c> and
/// <c>GetSpriteX</c> / <c>GetSpriteY</c>.
/// </summary>
/// <remarks>
/// <para>
/// Movement is not a tile snap. Starting a move sets the logical tile position
/// to the target immediately and sets <c>remaining_step</c> to
/// <c>SCREEN_TILE_SIZE</c>, 256. Every update subtracts
/// <c>1 &lt;&lt; (1 + move_speed)</c> from it, and the drawn position is
/// <c>GetX() * 256 - remaining_step</c> for a move to the right, so the sprite
/// slides across the tile it just logically entered and appears to walk.
/// </para>
/// <para>
/// The consequence that matters: at the default move speed 3 the amount is
/// 16, so a tile takes exactly 16 updates. A runtime that moves a character in
/// one call renders the same final picture and a completely different
/// animation, which is why the budget is a value of its own and not a detail of
/// the position.
/// </para>
/// <para>
/// Direction and speed are separate. <c>GetMaxStopCountForStep</c> uses the
/// move frequency, while the per frame amount uses the move speed, so a
/// character can be frequent but slow.
/// </para>
/// </remarks>
public static class Rm2kStepBudget
{
    /// <summary>
    /// liblcf <c>SCREEN_TILE_SIZE</c>: the sub tile unit a remaining step is
    /// counted in. It is 256 for a 16 pixel tile, which is why the drawn
    /// position divides by <see cref="PixelsPerTile"/> further down.
    /// </summary>
    public const int ScreenTileSize = 256;

    /// <summary>The verified tile edge in pixels.</summary>
    public const int PixelsPerTile = 16;

    /// <summary>Highest defined move speed, since the tables are one based.</summary>
    public const int MaxMoveSpeed = 6;

    /// <summary>
    /// <c>Game_Player::Update</c>: the per update movement amount,
    /// <c>1 &lt;&lt; (1 + GetMoveSpeed())</c>.
    /// </summary>
    public static int MovementAmount(int pMoveSpeed)
    {
        if (pMoveSpeed < 1 || pMoveSpeed > MaxMoveSpeed)
        {
            throw new Rm2kAnimationDataException(
                $"A move speed of {pMoveSpeed} is outside the defined range 1..{MaxMoveSpeed}.");
        }
        return 1 << (1 + pMoveSpeed);
    }

    /// <summary>
    /// <c>Game_Character::GetMaxStopCountForStep</c>: how many updates a
    /// character waits before the next move. Note this is the move
    /// <em>frequency</em>, one based, and 8 or more means no wait at all.
    /// </summary>
    public static int MaxStopCountForStep(int pMoveFrequency)
    {
        return pMoveFrequency >= 8 ? 0 : 1 << (9 - pMoveFrequency);
    }

    /// <summary>
    /// <c>Game_Character::GetMaxStopCountForTurn</c>.
    /// </summary>
    public static int MaxStopCountForTurn(int pMoveFrequency)
    {
        return pMoveFrequency >= 8 ? 0 : 1 << (8 - pMoveFrequency);
    }

    /// <summary>
    /// <c>Game_Character::GetMaxStopCountForWait</c>: 20 plus the turn count.
    /// </summary>
    public static int MaxStopCountForWait(int pMoveFrequency)
    {
        return 20 + MaxStopCountForTurn(pMoveFrequency);
    }

    /// <summary>
    /// The number of updates a full tile takes at this move speed, which is
    /// <c>SCREEN_TILE_SIZE / amount</c>.
    /// </summary>
    public static int UpdatesPerTile(int pMoveSpeed)
    {
        return ScreenTileSize / MovementAmount(pMoveSpeed);
    }

    /// <summary>
    /// <c>Game_Character::UpdateMovement</c>: subtracts the amount, clamps at
    /// zero, and reports whether the tile has been completed on this update.
    /// </summary>
    public static (int Remaining, bool Completed) Advance(int pRemainingStep, int pMoveSpeed)
    {
        var remaining = pRemainingStep - MovementAmount(pMoveSpeed);
        if (remaining <= 0)
        {
            return (0, true);
        }
        return (remaining, false);
    }

    /// <summary>
    /// <c>Game_Character::GetSpriteX</c>: the logical tile in screen tile
    /// units, offset by the unspent part of the current step in the direction of
    /// travel. This is the value that makes the character appear between two
    /// tiles instead of teleporting.
    /// </summary>
    /// <param name="pX">The logical tile x, which a started move already set to the target.</param>
    /// <param name="pRemainingStep">The unspent step budget.</param>
    /// <param name="pDirection">
    /// liblcf <c>Game_Character::Direction</c>: 0 up, 1 right, 2 down, 3 left.
    /// </param>
    public static int SpriteX(int pX, int pRemainingStep, int pDirection)
    {
        var x = pX * ScreenTileSize;
        if (pRemainingStep <= 0)
        {
            return x;
        }
        return pDirection switch
        {
            1 => x - pRemainingStep,  // right: the tile has not been reached yet
            3 => x + pRemainingStep,  // left: the sprite trails towards the old tile
            _ => x,
        };
    }

    /// <summary>
    /// <c>Game_Character::GetSpriteY</c>.
    /// </summary>
    public static int SpriteY(int pY, int pRemainingStep, int pDirection)
    {
        var y = pY * ScreenTileSize;
        if (pRemainingStep <= 0)
        {
            return y;
        }
        return pDirection switch
        {
            2 => y - pRemainingStep,  // down
            0 => y + pRemainingStep,  // up
            _ => y,
        };
    }

    /// <summary>
    /// The drawn offset in pixels from the character's logical tile, which is
    /// what the renderer needs: the sprite position divided by
    /// <see cref="PixelsPerTile"/>, with the step's remaining part scaled to
    /// pixels.
    /// </summary>
    public static int PixelOffsetX(int pLogicalTileX, int pRemainingStep, int pDirection)
    {
        return (SpriteX(pLogicalTileX, pRemainingStep, pDirection) / PixelsPerTile)
            - (pLogicalTileX * PixelsPerTile);
    }

    /// <summary>
    /// The drawn offset in pixels from the character's logical tile.
    /// </summary>
    public static int PixelOffsetY(int pLogicalTileY, int pRemainingStep, int pDirection)
    {
        return (SpriteY(pLogicalTileY, pRemainingStep, pDirection) / PixelsPerTile)
            - (pLogicalTileY * PixelsPerTile);
    }
}
