using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Pins the autotile quarter tables transcribed from EasyRPG Player
/// <c>src/tilemap_layer.cpp</c> (<c>BlockA_Subtiles_IDS</c>,
/// <c>BlockD_Subtiles_IDS</c>, <c>GenerateAutotileD</c>). The Player composes
/// every autotile from four 16x16 quarters, so a tile id resolves to four
/// chipset rectangles rather than one.
/// </summary>
public partial class TestRm2kAutotileQuarters : TestBase
{
    public void Test_BlockDVariantTableMatchesTheVerifiedAnchors()
    {
        // BlockD_Subtiles_IDS is [50][2][2][2] in top-left, top-right,
        // bottom-left, bottom-right order.
        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(0, out var a0), true);
        AssertEq(a0.Length, 4);
        AssertEq(a0[0], -1, "variant 0 takes every quarter from the B block");
        AssertEq(a0[3], -1);

        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(1, out var a1), true);
        AssertEq(a1[0], 3, "variant 1 is a single top-left corner");
        AssertEq(a1[1], -1);
        AssertEq(a1[2], -1);
        AssertEq(a1[3], -1);

        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(15, out var a15), true);
        AssertEq(a15[0], 3);
        AssertEq(a15[1], 3);
        AssertEq(a15[2], 3);
        AssertEq(a15[3], 3, "variant 15 is the full four quarter shape");

        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(20, out var a20), true);
        AssertEq(a20[0], 2);
        AssertEq(a20[1], 2);
        AssertEq(a20[2], -1);
        AssertEq(a20[3], -1);

        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(46, out var a46), true);
        AssertEq(a46[0], 0);
        AssertEq(a46[3], 0, "the last variant is the full zero shape");

        for (var variant = 0; variant < Rm2kAutotileQuarters.BlockAVariants; variant++)
        {
            AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(variant, out var quarters), true,
                $"variant {variant} exists");
            foreach (var value in quarters)
            {
                AssertTrue(value is >= Rm2kAutotileQuarters.FromBlockB and <= 3,
                    $"variant {variant} holds a valid quarter id {value}");
            }
        }
    }

    public void Test_BlockAVariantLookupRefusesUnknownIds()
    {
        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(-1, out _), false);
        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(
            Rm2kAutotileQuarters.BlockAVariants, out _), false, "47 variants, so 47 is out of range");
    }

    public void Test_BlockDResolvesTheFirstVariantOfEachBlock()
    {
        // GenerateAutotileD: block = (id - BLOCK_D) / 50,
        // variant = id - BLOCK_D - block * 50.
        // block < 4: block_x = (block % 2) * 3, block_y = 8 + (block / 2) * 4
        // block >= 4: block_x = 6 + (block % 2) * 3, block_y = ((block - 4) / 2) * 4
        // Variant 0 is {{1,2},{1,2}},{{1,2},{1,2}} in the Player table.
        var expectedOrigins = new (int BlockX, int BlockY)[]
        {
            (0, 8), (3, 8), (0, 12), (3, 12),
            (6, 0), (9, 0), (6, 4), (9, 4),
            (6, 8), (9, 8), (6, 12), (9, 12),
        };
        for (var block = 0; block < Rm2kAutotileQuarters.BlockDBlocks; block++)
        {
            AssertEq(Rm2kAutotileQuarters.TryResolveBlockD(Rm2kChipset.BlockD + block * 50, out var quarters), true,
                $"block {block} variant 0 resolves");
            AssertEq(quarters.Length, 4);
            var origin = expectedOrigins[block];
            // Variant 0 is {{1,2},{1,2}},{{1,2},{1,2}} in the Player table and the
            // second value of a pair is the column, so every quarter sits at
            // column origin + 2 and row origin + 1.
            AssertEq(quarters[0].Column, origin.BlockX + 2, $"block {block} top-left column");
            AssertEq(quarters[0].Row, origin.BlockY + 1, $"block {block} top-left row");
            AssertEq(quarters[3].Column, origin.BlockX + 2, $"block {block} bottom-right column");
            AssertEq(quarters[3].Row, origin.BlockY + 1, $"block {block} bottom-right row");
        }
    }

    public void Test_BlockABQuarterAxesFollowTheVerifiedHashOrder()
    {
        // GenerateAutotiles packs the quarter pairs with the last quarter on top
        // and unpacks x first, so the second value of a pair is the column and
        // the first is the row. A variant 0 takes every quarter from the B block,
        // and b_subtile is a four bit pattern with one bit per quarter,
        // top-left first, so b_subtile n lives at tile id n * 50.
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(0, 0, out var b0), true);
        AssertEq(b0[0].Column, 4, "b_subtile 0 selects the first B variant");
        AssertEq(b0[0].Row, 0, "the animation step is the row");

        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(1 * 50, 0, out var b1), true);
        AssertEq(b1[0].Column, 5, "bit 0 set selects the second B variant");
        AssertEq(b1[0].Row, 0);
        AssertEq(b1[1].Column, 4, "the other quarters keep the first B variant");

        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(2 * 50, 0, out var b2), true);
        AssertEq(b2[1].Column, 5, "bit 1 set moves the top-right quarter");
        AssertEq(b2[0].Column, 4);

        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(4 * 50, 0, out var b4), true);
        AssertEq(b4[2].Column, 5, "bit 2 set moves the bottom-left quarter");

        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(8 * 50, 0, out var b8), true);
        AssertEq(b8[3].Column, 5, "bit 3 set moves the bottom-right quarter");

        // The animation step is the row of the B quarters.
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(0, 2, out var bStep2), true);
        AssertEq(bStep2[0].Row, 2, "the animation step advances the row");
    }

    public void Test_BlockABSecondBlockFlipsTheBPattern()
    {
        // block == 2 flips the B variant with `t ^= 3`, which swaps the two bits:
        // a cleared bit 0 becomes 3 and a set bit 0 becomes 2.
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(1 * 50, 0, out var block1), true);
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(2000 + 1 * 50, 0, out var block2), true);
        AssertEq(block1[0].Column, 5, "block 1 bit 0 gives the second B variant");
        AssertEq(block2[0].Column, 6, "block 2 flips a set bit 0 to bit 1");

        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(2000, 0, out var block2Zero), true,
            "b_subtile 0 has every bit cleared");
        AssertEq(block2Zero[0].Column, 7, "block 2 flips a cleared bit 0 to the fourth variant");

        // Every quarter of a pattern takes its own bit, so the reachable B
        // columns are exactly the four variants.
        var columns = new HashSet<int>();
        var blocks = new[] { 0, 1000, 2000 };
        for (var bSubtile = 0; bSubtile < Rm2kAutotileQuarters.BlockBSubtiles; bSubtile++)
        {
            foreach (var blockOffset in blocks)
            {
                AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(blockOffset + bSubtile * 50, 0, out var quarters), true,
                    $"b_subtile {bSubtile} in block {blockOffset} resolves");
                foreach (var quarter in quarters)
                {
                    AssertTrue(quarter.Column >= 4 && quarter.Column <= 7,
                        $"B column {quarter.Column} is in the reachable range");
                    columns.Add(quarter.Column);
                }
            }
        }
        AssertEq(columns.Count, 4, "exactly the B columns 4, 5, 6 and 7 are reachable");
    }

    public void Test_BlockABSuppliesAQuartersFromTheATable()
    {
        // A variant 1 is {{3, N}, {N, N}}, so only the top-left quarter comes
        // from the A table. Tile id 1 is b_subtile 0 and a_subtile 1.
        AssertEq(Rm2kAutotileQuarters.TryGetBlockAQuarters(1, out var a1), true);
        AssertEq(a1[0], 3);
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(1, 0, out var quarters), true);
        AssertEq(quarters[0].Column, 3, "the A table column");
        AssertEq(quarters[0].Row, 0, "block 0 uses the animation step as the row");
        AssertEq(quarters[1].Column, 4, "the other quarters stay in the B range");
    }

    public void Test_BlockABSecondBlockShiftsTheARows()
    {
        // block == 1 adds 3 to the A row, so the same A variant is drawn three
        // rows further down.
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(1, 0, out var block0), true);
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(1000 + 1, 0, out var block1), true);
        AssertEq(block1[0].Row, block0[0].Row + 3, "block 1 shifts the A rows by three");
        AssertEq(block1[0].Column, block0[0].Column, "the column does not change");
    }

    public void Test_BlockABCombinationPassWins()
    {
        // With both subtiles set the combination pass overwrites the quarter, and
        // it runs last, so it wins over the A table. Tile id 51 is b_subtile 1
        // and a_subtile 1.
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(1, 0, out var before), true);
        AssertEq(before[0].Column, 3, "with b_subtile 0 the A table supplies the quarter");

        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(1 * 50 + 1, 0, out var after), true,
            "b_subtile 1 and a_subtile 1 trigger the combination pass");
        AssertEq(after[0].Column, 5, "the combination pass replaced the A quarter");
    }

    public void Test_BlockABRefusesIdsOutsideTheVerifiedRange()
    {
        // b_subtile >= TILE_SIZE (16) and a_subtile >= 47 are refused.
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(16 * 50, 0, out _), false, "b_subtile 16 is too large");
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(15 * 50 + 46, 0, out _), true, "b_subtile 15, a_subtile 46");
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(47, 0, out _), false, "a_subtile 47 is too large");
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(0, 4, out _), false, "the animation step is 0 to 3");
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(0, -1, out _), false);
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(Rm2kChipset.BlockC, 0, out _), false,
            "block C is not an A/B autotile");
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(-1, 0, out _), false);
    }

    public void Test_BlockABAndBlockDUseDifferentChipsetColumns()
    {
        // The A table uses columns 0 to 3 and the B pattern columns 4 to 7, so
        // the two sources of an A/B autotile must not overlap. Getting the pair
        // axes backwards would still be self consistent, so this checks the
        // layout the Player implies instead of the table alone.
        var aColumns = new HashSet<int>();
        var bColumns = new HashSet<int>();
        for (var aSubtile = 0; aSubtile < Rm2kAutotileQuarters.BlockAVariants; aSubtile++)
        {
            AssertEq(Rm2kAutotileQuarters.TryResolveBlockAB(aSubtile, 0, out var quarters), true,
                $"a_subtile {aSubtile} resolves with b_subtile 0");
            for (var quarter = 0; quarter < 4; quarter++)
            {
                if (Rm2kAutotileQuarters.TryGetBlockAQuarters(aSubtile, out var aQuarters)
                    && aQuarters[quarter] != Rm2kAutotileQuarters.FromBlockB)
                {
                    aColumns.Add(quarters[quarter].Column);
                }
                else
                {
                    bColumns.Add(quarters[quarter].Column);
                }
            }
        }
        foreach (var column in aColumns)
        {
            AssertTrue(column <= 3, $"A table column {column} is in the A range");
        }
        foreach (var column in bColumns)
        {
            AssertTrue(column >= 4, $"B pattern column {column} is in the B range");
        }
        AssertTrue(aColumns.Count >= 1 && bColumns.Count >= 1, "both sources are exercised");
    }

    public void Test_BlockDResolvesEveryVariant()
    {
        var resolved = 0;
        for (var chipId = Rm2kChipset.BlockD; chipId < Rm2kChipset.BlockDEnd; chipId++)
        {
            if (!Rm2kAutotileQuarters.TryResolveBlockD(chipId, out var quarters))
            {
                AssertTrue(false, $"chip {chipId} must resolve");
                continue;
            }
            resolved++;
            AssertEq(quarters.Length, 4, $"chip {chipId} has four quarters");
            foreach (var quarter in quarters)
            {
                AssertTrue(quarter.Column >= 0 && quarter.Column < Rm2kChipsetSource.Columns,
                    $"column {quarter.Column} of chip {chipId} is inside the chipset");
                AssertTrue(quarter.Row >= 0 && quarter.Row < Rm2kChipsetSource.Rows,
                    $"row {quarter.Row} of chip {chipId} is inside the chipset");
            }
        }
        AssertEq(resolved, 12 * 50, "12 blocks of 50 variants");
    }

    public void Test_BlockDRefusesIdsOutsideTheVerifiedRange()
    {
        foreach (var chipId in new[]
        {
            Rm2kChipset.BlockD - 1, Rm2kChipset.BlockD - 50, Rm2kChipset.BlockDEnd,
            Rm2kChipset.BlockDEnd + 1, -1, int.MinValue, int.MaxValue,
        })
        {
            AssertEq(Rm2kAutotileQuarters.TryResolveBlockD(chipId, out _), false,
                $"chip {chipId} is not a block D autotile");
        }
    }

    public void Test_BlockDVariantAnchorsMatchThePlayerTable()
    {
        // Spot checks of BlockD_Subtiles_IDS rows read directly from the Player
        // source, so a transcription error cannot pass unnoticed.
        AssertBlockDRow(0, 1, 2, 1, 2, 1, 2, 1, 2);
        AssertBlockDRow(1, 2, 0, 1, 2, 1, 2, 1, 2);
        AssertBlockDRow(8, 1, 2, 1, 2, 2, 0, 1, 2);
        AssertBlockDRow(12, 1, 2, 1, 2, 2, 0, 2, 0);
        AssertBlockDRow(16, 0, 2, 0, 2, 0, 2, 0, 2);
        AssertBlockDRow(20, 1, 1, 1, 1, 1, 1, 1, 1);
        AssertBlockDRow(28, 1, 3, 1, 3, 1, 3, 1, 3);
        AssertBlockDRow(34, 0, 1, 0, 1, 0, 1, 0, 1);
        AssertBlockDRow(40, 0, 3, 0, 3, 0, 3, 0, 3);
        AssertBlockDRow(49, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    private void AssertBlockDRow(int pVariant, params int[] pExpected)
    {
        AssertEq(pExpected.Length, 8, "a block D row holds four quarter pairs");
        AssertEq(Rm2kAutotileQuarters.TryResolveBlockD(Rm2kChipset.BlockD + pVariant, out var quarters), true,
            $"variant {pVariant} resolves");
        var labels = new[] { "top-left", "top-right", "bottom-left", "bottom-right" };
        for (var quarter = 0; quarter < 4; quarter++)
        {
            // The Player unpacks the pair as x first, so the second table value
            // is the column and the first value is the row. Variant n without a
            // block offset lives in block 0, whose origin is (0, 8).
            AssertEq(quarters[quarter].Column, 0 + pExpected[quarter * 2 + 1],
                $"row {pVariant} {labels[quarter]} column (table {pExpected[quarter * 2]},{pExpected[quarter * 2 + 1]}, " +
                $"got column {quarters[quarter].Column})");
            AssertEq(quarters[quarter].Row, 8 + pExpected[quarter * 2],
                $"row {pVariant} {labels[quarter]} row (table {pExpected[quarter * 2]},{pExpected[quarter * 2 + 1]}, " +
                $"got row {quarters[quarter].Row})");
        }
    }
}
