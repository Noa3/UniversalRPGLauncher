using System;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// The map camera. The Player does not move a viewport rectangle; it scrolls
/// two static tile sprites by an offset. This type therefore computes the
/// scroll offset in screen tiles, which is what <c>Game_Map::SetPositionX</c>
/// and <c>Game_Map::SetPositionY</c> store in <c>map_info.position_x/y</c>.
/// </summary>
/// <remarks>
/// Verified against the Player in <c>src/game_map.cpp</c> and
/// <c>src/game_player.cpp</c>:
/// <list type="bullet">
/// <item><c>Game_Map::GetDisplayX</c> returns
/// <c>map_info.position_x + shake * 16</c>, so the stored position is already
/// the scroll offset and screen shakes are added on top of it, not folded in.</item>
/// <item><c>Game_Map::SetPositionX</c> clamps to
/// <c>[0, tiles_x * SCREEN_TILE_SIZE - screen_width]</c> when the map does not
/// loop, and applies a positive modulo when it does. The comment there is
/// explicit that <c>std::clamp</c> must not be used, because for a map smaller
/// than the screen the lower bound is larger than the upper bound.</item>
/// <item><c>Game_Player::GetDefaultPanX</c> is
/// <c>ceil(screen_width / TILE_SIZE / 2) - 1) * SCREEN_TILE_SIZE</c>, so the
/// player sits slightly left of centre, and the same shape for Y. The units are
/// screen tiles, so a 320 pixel screen is exactly 20 screen tiles wide.</item>
/// </list>
/// The screen shake term is deliberately not implemented: it is presentation
/// state, not simulation state, and adding it here would couple a cosmetic
/// effect to the deterministic core.
/// </remarks>
public static class Rm2kMapCamera
{
    /// <summary>Player sprite size, also the size of one map tile in pixels.</summary>
    public const int TileSize = 16;

    /// <summary>
    /// One map tile counts as this many screen tiles. Verified constant
    /// <c>SCREEN_TILE_SIZE = 256</c> in <c>src/game_map.h</c>.
    /// </summary>
    public const int ScreenTileSize = 256;

    /// <summary>The default RM2000/2003 resolution in pixels.</summary>
    public const int DefaultScreenWidth = 320;

    /// <summary>The default RM2000/2003 resolution in pixels.</summary>
    public const int DefaultScreenHeight = 240;

    /// <summary>
    /// <c>Game_Player::GetDefaultPanX</c>: the pan that centres the player with
    /// the same half-tile-left bias the Player uses. Pan is in screen tiles.
    /// </summary>
    public static int DefaultPanX(int pScreenWidth = DefaultScreenWidth)
    {
        return ((int)Math.Ceiling(pScreenWidth / (double)TileSize / 2) - 1) * ScreenTileSize;
    }

    /// <summary>
    /// <c>Game_Player::GetDefaultPanY</c>. Pan is in screen tiles.
    /// </summary>
    public static int DefaultPanY(int pScreenHeight = DefaultScreenHeight)
    {
        return ((int)Math.Ceiling(pScreenHeight / (double)TileSize / 2) - 1) * ScreenTileSize;
    }

    /// <summary>
    /// <c>Game_Map::SetPositionX</c> without the loop case. Returns the scroll
    /// offset in screen tiles, exactly as the Player stores it in
    /// <c>map_info.position_x</c>.
    /// </summary>
    /// <remarks>
    /// This is the Player's storage unit, not a pixel offset. The Player turns
    /// it into pixels in the tilemap's <c>SetOx</c>, which is
    /// <c>GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE)</c>, and one screen
    /// tile is exactly <c>TILE_SIZE</c> pixels. Use <see cref="OffsetPixelsX"/>
    /// for anything that reads a pixel buffer, because multiplying this by 256
    /// instead of by 16 would place the viewport far outside the map.
    /// </remarks>
    public static int PositionX(
        int pPlayerX, int pMapTilesX, int pScreenWidth = DefaultScreenWidth, bool pLoopHorizontal = false)
    {
        var mapWidth = pMapTilesX * ScreenTileSize;
        var x = pPlayerX * ScreenTileSize - DefaultPanX(pScreenWidth);
        if (pLoopHorizontal)
        {
            return PositiveModulo(x, mapWidth);
        }
        // Verbatim from Game_Map::SetPositionX, including its unit mix: the map
        // extent is counted in screen tiles (SCREEN_TILE_SIZE) while the screen
        // is in pixels. This is an upstream inconsistency, not an oversight
        // here: for a looping map the bound is never consulted, and for a
        // non-looping map the position is normally produced by UpdateScroll.
        // Reproducing it exactly is required for save compatible positioning,
        // so it is kept rather than "corrected" into pixels.
        var maximum = mapWidth - pScreenWidth;
        if (maximum <= 0)
        {
            return 0;
        }
        return Math.Max(0, Math.Min(maximum, x));
    }

    /// <summary>
    /// <c>Game_Map::SetPositionY</c> without the loop case. Returns the scroll
    /// offset in screen tiles.
    /// </summary>
    public static int PositionY(
        int pPlayerY, int pMapTilesY, int pScreenHeight = DefaultScreenHeight, bool pLoopVertical = false)
    {
        var mapHeight = pMapTilesY * ScreenTileSize;
        var y = pPlayerY * ScreenTileSize - DefaultPanY(pScreenHeight);
        if (pLoopVertical)
        {
            return PositiveModulo(y, mapHeight);
        }
        if (mapHeight <= pScreenHeight)
        {
            return 0;
        }
        return Math.Max(0, Math.Min(mapHeight - pScreenHeight, y));
    }

    /// <summary>
    /// Converts the stored screen-tile offset to the pixel offset the Player
    /// hands to the tilemap's <c>SetOx</c>.
    /// </summary>
    /// <remarks>
    /// Verified from <c>Spriteset_Map::Update</c>, which does
    /// <c>SetOx(GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE))</c>.
    /// <c>SCREEN_TILE_SIZE / TILE_SIZE</c> is 256 / 16 = 16, and the operation
    /// is a **division** by 16, not a multiplication. The bound confirms it: for
    /// the last player column of a 40 tile map the stored offset is 7680, and
    /// 7680 / 16 = 480, which is exactly 40 * 16 - 320, the furthest the
    /// viewport can legally move. Multiplying instead of dividing would produce
    /// 122880, sixteen times past the end of the map, and the frame would go
    /// black. So the position is a screen-tile count that is divided down to
    /// pixels, not scaled up.
    /// </remarks>
    public static int OffsetPixelsX(int pPositionScreenTiles)
    {
        return pPositionScreenTiles / (ScreenTileSize / TileSize);
    }

    /// <summary>
    /// Converts the stored screen-tile offset to a pixel offset, vertical.
    /// </summary>
    public static int OffsetPixelsY(int pPositionScreenTiles)
    {
        return pPositionScreenTiles / (ScreenTileSize / TileSize);
    }

    /// <summary>
    /// <c>Utils::PositiveModulo</c>: always a non-negative result, unlike the C
    /// remainder, which the Player uses deliberately for looping maps.
    /// </summary>
    public static int PositiveModulo(int pValue, int pModulus)
    {
        if (pModulus <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pModulus), "Modulus must be positive.");
        }
        var result = pValue % pModulus;
        return result < 0 ? result + pModulus : result;
    }
}
