using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Resolves a map tile id to its source rectangle inside the RM2K chipset
/// bitmap, following EasyRPG Player <c>src/tilemap_layer.cpp</c> (Draw).
/// </summary>
/// <remarks>
/// <para>
/// Only the blocks the Player blits directly from the chipset are resolved
/// here, and each formula is taken from the Player:
/// <list type="bullet">
/// <item>Block C: <c>col = 3 + (id - BLOCK_C) / 50</c> and
/// <c>row = 4 + animation_step_c</c>, so block C occupies columns 3 to 5 and
/// rows 4 to 7.</item>
/// <item>Block E: the substitution table is applied first
/// (<c>id = substitutions[tile.ID - BLOCK_E]</c>), then
/// <c>col = 12 + id % 6, row = id / 6</c> for <c>id &lt; 96</c> and
/// <c>col = 18 + (id - 96) % 6, row = (id - 96) / 6</c> afterwards.</item>
/// <item>Block F: the substitution table is applied first
/// (<c>id = substitutions[tile.ID - BLOCK_F]</c>), then
/// <c>col = 18 + id % 6, row = 8 + id / 6</c> for <c>id &lt; 48</c> and
/// <c>col = 24 + (id - 48) % 6, row = (id - 48) / 6</c> afterwards.</item>
/// </list>
/// </para>
/// <para>
/// The Player guards block C with <c>id &gt;= BLOCK_C &amp;&amp; id &lt; BLOCK_D</c>,
/// not with the block C end, so ids between the end of block C and the start of
/// block D still resolve to a rectangle. That matches its passability lookup,
/// which applies the same range, and is kept here on purpose: the renderer and
/// the simulation must resolve a tile id identically.
/// </para>
/// <para>
/// Blocks A, B and D are deliberately not resolved: the Player draws them from
/// a generated autotile cache (<c>autotiles_ab_screen</c>,
/// <c>autotiles_d_screen</c>) rather than from one chipset rectangle.
/// </para>
/// <para>
/// The formulas require at least 30 columns and 16 rows of 16 pixel tiles,
/// which follows from the largest computed column (24 + 5) and row ((143 - 48) / 6).
/// </para>
/// </remarks>
public static class Rm2kChipsetSource
{
    /// <summary>Tiles per chipset row, fixed by the Player formulas.</summary>
    public const int Columns = 30;

    /// <summary>Tile rows the formulas address, the largest is (143 - 48) / 6.</summary>
    public const int Rows = 16;

    /// <summary>First chipset column used by block C.</summary>
    public const int BlockCFirstColumn = 3;

    /// <summary>First chipset row used by the four block C animation frames.</summary>
    public const int BlockCFirstRow = 4;

    public readonly record struct ChipsetRect(int Column, int Row);

    /// <summary>
    /// The identity tables, matching the <c>std::iota</c> default the Player
    /// uses in <c>Game_Map::Setup</c>, so an unknown substitution behaves like a
    /// freshly loaded map instead of failing every block E and F lookup.
    /// </summary>
    private static readonly Rm2kTileSubstitution IdentitySubstitution = new(null, null);

    /// <summary>
    /// Resolves the chipset rectangle for a tile id. Returns false for the
    /// autotile cache blocks and for any id that cannot be resolved, so callers
    /// fail closed instead of drawing a wrong tile.
    /// </summary>
    public static bool TryResolve(
        int pChipId,
        int pFrameCount,
        Rm2kTileSubstitution? pSubstitution,
        out ChipsetRect pRect)
    {
        pRect = default;
        if (pChipId >= Rm2kChipset.BlockC && pChipId < Rm2kChipset.BlockD)
        {
            pRect = new ChipsetRect(
                BlockCFirstColumn + (pChipId - Rm2kChipset.BlockC) / Rm2kChipset.BlockCStride,
                BlockCFirstRow + Rm2kChipset.CBlockStep(pFrameCount));
            return true;
        }

        if (pChipId >= Rm2kChipset.BlockE && pChipId < Rm2kChipset.BlockEEnd)
        {
            // Game_Map::Setup fills both tables with std::iota, so an absent table
            // is the identity and must not make a block E or F tile unresolvable.
            var substituted = (pSubstitution ?? IdentitySubstitution).SubstituteLower(pChipId - Rm2kChipset.BlockE);
            if (substituted < 0)
            {
                return false;
            }
            var id = substituted - Rm2kChipset.BlockEIndex;
            pRect = id < 96
                ? new ChipsetRect(12 + id % 6, id / 6)
                : new ChipsetRect(18 + (id - 96) % 6, (id - 96) / 6);
            return true;
        }

        if (pChipId >= Rm2kChipset.BlockF && pChipId < Rm2kChipset.BlockFEnd)
        {
            var substituted = (pSubstitution ?? IdentitySubstitution).SubstituteUpper(pChipId - Rm2kChipset.BlockF);
            if (substituted < 0)
            {
                return false;
            }
            pRect = substituted < 48
                ? new ChipsetRect(18 + substituted % 6, 8 + substituted / 6)
                : new ChipsetRect(24 + (substituted - 48) % 6, (substituted - 48) / 6);
            return true;
        }

        // Blocks A, B and D come from a generated autotile cache, not from a
        // single chipset rectangle, and unknown ids must not be guessed.
        return false;
    }

    /// <summary>
    /// Resolves with the identity substitution, which is the verified default
    /// for a freshly loaded map.
    /// </summary>
    public static bool TryResolve(int pChipId, int pFrameCount, out ChipsetRect pRect)
    {
        return TryResolve(pChipId, pFrameCount, new Rm2kTileSubstitution(null, null), out pRect);
    }
}
