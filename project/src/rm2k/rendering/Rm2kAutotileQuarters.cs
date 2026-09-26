using System;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Autotile quarter selection for the blocks the Player draws from a generated
/// autotile cache. Every table and formula in this file is transcribed from
/// EasyRPG Player <c>src/tilemap_layer.cpp</c> (<c>BlockA_Subtiles_IDS</c>,
/// <c>BlockD_Subtiles_IDS</c>, <c>GenerateAutotileD</c>).
/// </summary>
/// <remarks>
/// The Player composes each 32x32 autotile out of four 16x16 quarters taken from
/// the chipset, so a tile id resolves to four chipset rectangles rather than one.
/// The tables are stored flat: four values per entry in top-left, top-right,
/// bottom-left, bottom-right order, which is the loop order the Player uses.
/// </remarks>
/// <para>
/// Axis order inside a quarter pair: <c>GenerateAutotiles</c> packs the pairs
/// into a hash with the last quarter on top and unpacks <c>x</c> first, so the
/// second value of a pair is the chipset column and the first value is the row.
/// Getting this backwards produces a transposed but plausible looking image.
/// </para>
public static class Rm2kAutotileQuarters
{
    /// <summary>Quarter order used by the Player loops: top-left, top-right, bottom-left, bottom-right.</summary>
    public const int QuarterCount = 4;

    /// <summary>Block A autotile variants, transcribed from <c>BlockA_Subtiles_IDS</c>; -1 means the B block supplies the quarter.</summary>
    private static readonly sbyte[] BlockASubtileIds =
    {
        -1, -1, -1, -1,
        3, -1, -1, -1,
        -1, 3, -1, -1,
        3, 3, -1, -1,
        -1, -1, -1, 3,
        3, -1, -1, 3,
        -1, 3, -1, 3,
        3, 3, -1, 3,
        -1, -1, 3, -1,
        3, -1, 3, -1,
        -1, 3, 3, -1,
        3, 3, 3, -1,
        -1, -1, 3, 3,
        3, -1, 3, 3,
        -1, 3, 3, 3,
        3, 3, 3, 3,
        1, -1, 1, -1,
        1, 3, 1, -1,
        1, -1, 1, 3,
        1, 3, 1, 3,
        2, 2, -1, -1,
        2, 2, -1, 3,
        2, 2, 3, -1,
        2, 2, 3, 3,
        -1, 1, -1, 1,
        -1, 1, 3, 1,
        3, 1, -1, 1,
        3, 1, 3, 1,
        -1, -1, 2, 2,
        3, -1, 2, 2,
        -1, 3, 2, 2,
        3, 3, 2, 2,
        1, 1, 1, 1,
        2, 2, 2, 2,
        0, 2, 1, -1,
        0, 2, 1, 3,
        2, 0, -1, 1,
        2, 0, 3, 1,
        -1, 1, 2, 0,
        3, 1, 2, 0,
        1, -1, 0, 2,
        1, 3, 0, 2,
        0, 0, 1, 1,
        0, 2, 0, 2,
        1, 1, 0, 0,
        2, 0, 2, 0,
        0, 0, 0, 0,
    };

    /// <summary>Block D autotile variants, transcribed from <c>BlockD_Subtiles_IDS</c>.</summary>
    private static readonly byte[] BlockDSubtileIds =
    {
        1, 2, 1, 2, 1, 2, 1, 2,
        2, 0, 1, 2, 1, 2, 1, 2,
        1, 2, 2, 0, 1, 2, 1, 2,
        2, 0, 2, 0, 1, 2, 1, 2,
        1, 2, 1, 2, 1, 2, 2, 0,
        2, 0, 1, 2, 1, 2, 2, 0,
        1, 2, 2, 0, 1, 2, 2, 0,
        2, 0, 2, 0, 1, 2, 2, 0,
        1, 2, 1, 2, 2, 0, 1, 2,
        2, 0, 1, 2, 2, 0, 1, 2,
        1, 2, 2, 0, 2, 0, 1, 2,
        2, 0, 2, 0, 2, 0, 1, 2,
        1, 2, 1, 2, 2, 0, 2, 0,
        2, 0, 1, 2, 2, 0, 2, 0,
        1, 2, 2, 0, 2, 0, 2, 0,
        2, 0, 2, 0, 2, 0, 2, 0,
        0, 2, 0, 2, 0, 2, 0, 2,
        0, 2, 2, 0, 0, 2, 0, 2,
        0, 2, 0, 2, 0, 2, 2, 0,
        0, 2, 2, 0, 0, 2, 2, 0,
        1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 2, 0,
        1, 1, 1, 1, 2, 0, 1, 1,
        1, 1, 1, 1, 2, 0, 2, 0,
        2, 2, 2, 2, 2, 2, 2, 2,
        2, 2, 2, 2, 2, 0, 2, 2,
        2, 0, 2, 2, 2, 2, 2, 2,
        2, 0, 2, 2, 2, 0, 2, 2,
        1, 3, 1, 3, 1, 3, 1, 3,
        2, 0, 1, 3, 1, 3, 1, 3,
        1, 3, 2, 0, 1, 3, 1, 3,
        2, 0, 2, 0, 1, 3, 1, 3,
        0, 2, 2, 2, 0, 2, 2, 2,
        1, 1, 1, 1, 1, 3, 1, 3,
        0, 1, 0, 1, 0, 1, 0, 1,
        0, 1, 0, 1, 0, 1, 2, 0,
        2, 1, 2, 1, 2, 1, 2, 1,
        2, 1, 2, 1, 2, 0, 2, 1,
        2, 3, 2, 3, 2, 3, 2, 3,
        2, 0, 2, 3, 2, 3, 2, 3,
        0, 3, 0, 3, 0, 3, 0, 3,
        0, 3, 2, 0, 0, 3, 0, 3,
        0, 1, 2, 1, 0, 1, 2, 1,
        0, 1, 0, 1, 0, 3, 0, 3,
        0, 3, 2, 3, 0, 3, 2, 3,
        2, 1, 2, 1, 2, 3, 2, 3,
        0, 1, 2, 1, 0, 3, 2, 3,
        1, 2, 1, 2, 1, 2, 1, 2,
        1, 2, 1, 2, 1, 2, 1, 2,
        0, 0, 0, 0, 0, 0, 0, 0,
    };

    /// <summary>Number of block A autotile variants.</summary>
    public const int BlockAVariants = 47;

    /// <summary>Number of block D autotile variants.</summary>
    public const int BlockDVariants = 50;

    /// <summary>Number of block D chipset blocks.</summary>
    public const int BlockDBlocks = 12;

    /// <summary>
    /// The A block quarter value meaning "this quarter comes from the B block".
    /// </summary>
    public const int FromBlockB = -1;

    /// <summary>
    /// Number of B block subtiles. The Player refuses
    /// <c>b_subtile &gt;= TILE_SIZE</c> and <c>#define TILE_SIZE 16</c>
    /// (<c>src/options.h</c>), so the B pattern is a four bit value.
    /// </summary>
    public const int BlockBSubtiles = 16;

    /// <summary>Chipset column the four B block variants start at.</summary>
    public const int BlockBFirstColumn = 4;

    /// <summary>
    /// Resolves a block A/B autotile to its four chipset quarters, following
    /// <c>TilemapLayer::GenerateAutotileAB</c>. Returns false for ids outside
    /// the verified block, B subtile and A variant ranges.
    /// </summary>
    public static bool TryResolveBlockAB(int pChipId, int pAnimationStep, out Rm2kChipsetSource.ChipsetRect[] pQuarters)
    {
        pQuarters = [];
        if (pAnimationStep < 0 || pAnimationStep > 3)
        {
            return false;
        }
        var block = pChipId / 1000;
        if (block > 2)
        {
            return false;
        }
        var bSubtile = (pChipId - block * 1000) / 50;
        if (bSubtile >= BlockBSubtiles)
        {
            return false;
        }
        var aSubtile = pChipId - block * 1000 - bSubtile * 50;
        if (aSubtile >= BlockAVariants || aSubtile < 0)
        {
            return false;
        }

        // The Player stores each quarter as a (y, x) pair, so the first value of
        // a pair is the chipset row and the second value is the column.
        var rows = new int[QuarterCount];
        var columns = new int[QuarterCount];
        var quarters = new Rm2kChipsetSource.ChipsetRect[QuarterCount];

        // Pass one: quarters the A table leaves to the B block.
        for (var j = 0; j < 2; j++)
        {
            for (var i = 0; i < 2; i++)
            {
                var quarter = j * 2 + i;
                if (BlockASubtileIds[aSubtile * 4 + quarter] != FromBlockB)
                {
                    continue;
                }
                var t = (bSubtile >> (j * 2 + i)) & 1;
                if (block == 2)
                {
                    t ^= 3;
                }
                rows[quarter] = pAnimationStep;
                columns[quarter] = BlockBFirstColumn + t;
            }
        }

        // Pass two: quarters the A table supplies.
        for (var j = 0; j < 2; j++)
        {
            for (var i = 0; i < 2; i++)
            {
                var quarter = j * 2 + i;
                var fromATable = BlockASubtileIds[aSubtile * 4 + quarter];
                if (fromATable == FromBlockB)
                {
                    continue;
                }
                rows[quarter] = pAnimationStep + (block == 1 ? 3 : 0);
                columns[quarter] = fromATable;
            }
        }

        // Pass three: the A and B combination, applied last so it wins.
        if (bSubtile != 0 && aSubtile != 0)
        {
            for (var j = 0; j < 2; j++)
            {
                for (var i = 0; i < 2; i++)
                {
                    var t = (bSubtile >> (j * 2 + i)) & 1;
                    if (block == 2)
                    {
                        t *= 2;
                    }
                    if (t == 0)
                    {
                        continue;
                    }
                    var quarter = j * 2 + i;
                    rows[quarter] = pAnimationStep;
                    columns[quarter] = BlockBFirstColumn + t;
                }
            }
        }

        for (var quarter = 0; quarter < QuarterCount; quarter++)
        {
            quarters[quarter] = new Rm2kChipsetSource.ChipsetRect(columns[quarter], rows[quarter]);
        }
        pQuarters = quarters;
        return true;
    }

    /// <summary>
    /// Resolves a block D tile id to its four chipset quarters, following
    /// <c>TilemapLayer::GenerateAutotileD</c>. Returns false for an id outside
    /// the verified block and variant ranges so callers fail closed.
    /// </summary>
    public static bool TryResolveBlockD(int pChipId, out Rm2kChipsetSource.ChipsetRect[] pQuarters)
    {
        pQuarters = [];
        var block = (pChipId - Rm2kChipset.BlockD) / 50;
        var variant = pChipId - Rm2kChipset.BlockD - block * 50;
        if (block >= BlockDBlocks || variant >= BlockDVariants || block < 0 || variant < 0)
        {
            return false;
        }

        int blockX;
        int blockY;
        if (block < 4)
        {
            blockX = block % 2 * 3;
            blockY = 8 + block / 2 * 4;
        }
        else
        {
            blockX = 6 + block % 2 * 3;
            blockY = (block - 4) / 2 * 4;
        }

        var quarters = new Rm2kChipsetSource.ChipsetRect[QuarterCount];
        for (var quarter = 0; quarter < QuarterCount; quarter++)
        {
            // Each block D variant stores four quarters as pairs. The Player
            // packs the pairs into a hash with the last quarter on top and reads
            // them back as x first, so the second value of a pair is the column
            // and the first value is the row.
            var offset = variant * QuarterCount * 2 + quarter * 2;
            quarters[quarter] = new Rm2kChipsetSource.ChipsetRect(
                blockX + BlockDSubtileIds[offset + 1],
                blockY + BlockDSubtileIds[offset]);
        }
        pQuarters = quarters;
        return true;
    }

    /// <summary>
    /// The block A autotile quarter ids for a variant, where -1 marks a quarter
    /// the B block supplies. Exposed for the block A/B composition and for
    /// regression tests of the transcribed table.
    /// </summary>
    public static bool TryGetBlockAQuarters(int pVariant, out sbyte[] pQuarters)
    {
        pQuarters = [];
        if (pVariant < 0 || pVariant >= BlockAVariants)
        {
            return false;
        }
        var quarters = new sbyte[QuarterCount];
        for (var quarter = 0; quarter < QuarterCount; quarter++)
        {
            quarters[quarter] = BlockASubtileIds[pVariant * 4 + quarter];
        }
        pQuarters = quarters;
        return true;
    }
}