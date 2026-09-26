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
            AssertEq(quarters[0].Column, origin.BlockX + 1, $"block {block} top-left column");
            AssertEq(quarters[0].Row, origin.BlockY + 2, $"block {block} top-left row");
            AssertEq(quarters[3].Column, origin.BlockX + 1, $"block {block} bottom-right column");
            AssertEq(quarters[3].Row, origin.BlockY + 2, $"block {block} bottom-right row");
        }
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
            // Variant n without a block offset lives in block 0, whose origin is
            // (0, 8), so subtract it to read the raw table values back.
            AssertEq(quarters[quarter].Column, 0 + pExpected[quarter * 2],
                $"row {pVariant} {labels[quarter]} x (expected table {pExpected[quarter * 2]}, " +
                $"got {quarters[quarter].Column})");
            AssertEq(quarters[quarter].Row, 8 + pExpected[quarter * 2 + 1],
                $"row {pVariant} {labels[quarter]} y (expected table {pExpected[quarter * 2 + 1]}, " +
                $"got {quarters[quarter].Row})");
        }
    }
}
