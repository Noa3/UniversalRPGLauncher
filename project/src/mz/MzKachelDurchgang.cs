using System;
using System.Collections.Generic;

namespace UniversalRPG.Mz;

/// <summary>
/// And whether a tile lets the player through, as both web engines answer it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the rule is the engines' own, not RM2K's.</strong> Measured
/// in <c>rmmz_objects.js</c> and <c>rpg_objects.js</c>, where the code is
/// byte for byte the same:
/// <code>
/// Game_Map.prototype.checkPassage = function(x, y, bit) {
///     var flags = this.tilesetFlags();
///     var tiles = this.allTiles(x, y);
///     for (var i = 0; i &lt; tiles.length; i++) {
///         var flag = flags[tiles[i]];
///         if ((flag &amp; 0x10) !== 0) continue;      // [*] no effect on passage
///         if ((flag &amp; bit) === 0) return true;    // [o] passable
///         if ((flag &amp; bit) === bit) return false;// [x] impassable
///     }
///     return false;
/// };
/// </code>
/// </para>
/// <para>
/// <strong>And three details are easy to get wrong, and each of them
/// changes which way a player can walk.</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>One flag array, not seven.</strong>
/// <c>tilesetFlags()</c> returns <c>$dataTilesets[id].flags</c>: 8192
/// entries per tileset in every game measured, one integer per tile id.
/// Reading <c>passage1</c> .. <c>passage7</c> finds nothing, because those
/// keys are not in these games.</description></item>
/// <item><description><strong>0x10 wins before anything else.</strong> A
/// star-flagged tile is skipped, so it can never refuse a step and never
/// allow one -- an <c>o</c> on a star tile is a decorative circle, not
/// passability.</description></item>
/// <item><description><strong>The first tile that answers decides.</strong>
/// The loop returns on the first flag it can judge, and a cell with no
/// tiles at all returns false.</description></item>
/// </list>
/// </remarks>
public static class MzKachelDurchgang
{
    /// <summary>Down, the engine's direction 2.</summary>
    public const int Unten = 2;

    /// <summary>Left, the engine's direction 4.</summary>
    public const int Links = 4;

    /// <summary>Right, the engine's direction 6.</summary>
    public const int Rechts = 6;

    /// <summary>Up, the engine's direction 8.</summary>
    public const int Oben = 8;

    /// <summary>The <c>*</c> flag: this tile does not decide passability.</summary>
    public const int KeinEffekt = 0x10;

    /// <summary>The four directions a player can walk, as the engine numbers them.</summary>
    public static readonly int[] Richtungen = [Unten, Links, Rechts, Oben];

    /// <summary>
    /// And the bit one direction asks about.
    /// </summary>
    /// <remarks>
    /// The engine writes <c>(1 &lt;&lt; (d / 2 - 1)) &amp; 0x0f</c>, which for
    /// 2, 4, 6 and 8 gives 1, 2, 4 and 8 -- the four low bits. The mask is
    /// kept because the engine keeps it, and because the higher bits belong
    /// to the boat, ship and airship checks.
    /// </remarks>
    public static int Bit(int pRichtung) => (1 << (pRichtung / 2 - 1)) & 0x0f;

    /// <summary>
    /// And whether the cell's tiles let the player go that way.
    /// </summary>
    /// <param name="pFlags">
    /// The flag of every tile on the cell, in the order
    /// <c>allTiles</c> returns them.
    /// </param>
    /// <param name="pRichtung">One of the four direction constants.</param>
    public static bool IstBegehbar(IReadOnlyList<int> pFlags, int pRichtung)
    {
        var bit = Bit(pRichtung);
        for (var index = 0; index < pFlags.Count; index++)
        {
            var flag = pFlags[index];
            if ((flag & KeinEffekt) != 0)
            {
                continue;
            }
            if ((flag & bit) == 0)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    /// <summary>
    /// And the flag of one tile id, or zero when the id addresses nothing.
    /// </summary>
    /// <remarks>
    /// <strong>And a tile id past the end of the array is a wall, not a
    /// crash.</strong> The engine indexes without a guard and would hand
    /// back <c>undefined</c>, which reads as 0 and therefore as
    /// passable; <strong>this returns 0 as well, because a reader that
    /// invented a different answer here would walk the player through the
    /// edge of the sheet.</strong>
    /// </remarks>
    public static int FlagFuer(IReadOnlyList<UniversalRPG.Web.MzValue> pFlags, int pKachel)
        => pKachel >= 0 && pKachel < pFlags.Count ? pFlags[pKachel].IntOr(0) : 0;

    /// <summary>
    /// And whether the cell lets the player go that way, given the tile ids.
    /// </summary>
    /// <remarks>
    /// This is the shape a caller has: the map's four layer values for a
    /// cell, and the tileset's flag array. <strong>The layers are read
    /// top to bottom</strong>, because that is the order <c>layeredTiles</c>
    /// walks and therefore the order in which the first answering tile is
    /// found.
    /// </remarks>
    public static bool IstBegehbar(
        IReadOnlyList<UniversalRPG.Web.MzValue> pTilesetFlags,
        IReadOnlyList<int> pKacheln, int pRichtung)
    {
        var amZiel = new List<int>(pKacheln.Count);
        foreach (var kachel in pKacheln)
        {
            amZiel.Add(FlagFuer(pTilesetFlags, kachel));
        }
        return IstBegehbar(amZiel, pRichtung);
    }
}