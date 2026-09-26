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

    /// <summary>The A block quarter value meaning "this quarter comes from the B block".</summary>
    public const int FromBlockB = -1;

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
            // Each block D variant stores four quarters as x/y pairs, so a
            // variant spans eight table values.
            var offset = variant * QuarterCount * 2 + quarter * 2;
            quarters[quarter] = new Rm2kChipsetSource.ChipsetRect(
                blockX + BlockDSubtileIds[offset],
                blockY + BlockDSubtileIds[offset + 1]);
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