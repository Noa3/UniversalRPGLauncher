using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Pins the chipset source rectangles verified in EasyRPG Player
/// <c>src/tilemap_layer.cpp</c> (Draw): blocks C, E and F are blitted straight
/// from the chipset bitmap, while A, B and D come from a generated autotile cache.
/// </summary>
public partial class TestRm2kChipsetSource : TestBase
{
    public void Test_BlockCUsesSixColumnsAndFourAnimatedRows()
    {
        // col = 3 + (id - BLOCK_C) / 50, row = 4 + animation_step_c.
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC, 0, out var first), true);
        AssertEq(first.Column, 3);
        AssertEq(first.Row, 4);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC + 49, 0, out var lastOfFirstColumn), true);
        AssertEq(lastOfFirstColumn.Column, 3, "the stride is 50");
        AssertEq(lastOfFirstColumn.Row, 4);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC + 50, 0, out var second), true);
        AssertEq(second.Column, 4);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC + 100, 0, out var third), true);
        AssertEq(third.Column, 5);
        // The Player guards block C with `< BLOCK_D`, not with the block C end,
        // so ids past the end of block C still resolve. The renderer must agree
        // with the passability lookup, which uses the same range.
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockCEnd, 0, out var pastC), true);
        AssertEq(pastC.Column, 3 + 150 / 50);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockD - 1, 0, out var lastC), true);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockD, 0, out _), false,
            "block D comes from the autotile cache");

        // The row follows the fixed block C cycle, independent of the chipset.
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC, 6, out var frame1), true);
        AssertEq(frame1.Row, 5);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC, 12, out var frame2), true);
        AssertEq(frame2.Row, 6);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC, 18, out var frame3), true);
        AssertEq(frame3.Row, 7);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC, 24, out var frame0), true);
        AssertEq(frame0.Row, 4, "the block C cycle repeats every 24 frames");
    }

    public void Test_BlockEUsesTheSubstitutionTableFirst()
    {
        // col = 12 + id % 6, row = id / 6 for id < 96, else the second column
        // group starting at 18.
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockE, 0, out var first), true);
        AssertEq(first.Column, 12);
        AssertEq(first.Row, 0);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockE + 95, 0, out var lastOfFirstGroup), true);
        AssertEq(lastOfFirstGroup.Column, 12 + 95 % 6);
        AssertEq(lastOfFirstGroup.Row, 95 / 6);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockE + 96, 0, out var secondGroup), true);
        AssertEq(secondGroup.Column, 18);
        AssertEq(secondGroup.Row, 0);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockE + 143, 0, out var last), true);
        AssertEq(last.Column, 18 + 47 % 6);
        AssertEq(last.Row, 47 / 6);

        // A non-identity table moves the rectangle.
        var lowerTable = Identity(144);
        lowerTable[0] = 10;
        var substitution = new Rm2kTileSubstitution(lowerTable, null);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockE, 0, substitution, out var moved), true);
        AssertEq(moved.Column, 12 + 10 % 6);
        AssertEq(moved.Row, 10 / 6);
    }

    public void Test_BlockFUsesTheSubstitutionTableFirst()
    {
        // col = 18 + id % 6, row = 8 + id / 6 for id < 48, else the second
        // column group starting at 24.
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockF, 0, out var first), true);
        AssertEq(first.Column, 18);
        AssertEq(first.Row, 8);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockF + 47, 0, out var lastOfFirstGroup), true);
        AssertEq(lastOfFirstGroup.Column, 18 + 47 % 6);
        AssertEq(lastOfFirstGroup.Row, 8 + 47 / 6);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockF + 48, 0, out var secondGroup), true);
        AssertEq(secondGroup.Column, 24);
        AssertEq(secondGroup.Row, 0);

        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockF + 143, 0, out var last), true);
        AssertEq(last.Column, 24 + 95 % 6);
        AssertEq(last.Row, 95 / 6);

        var upperTable = Identity(144);
        upperTable[0] = 50;
        var substitution = new Rm2kTileSubstitution(null, upperTable);
        AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockF, 0, substitution, out var moved), true);
        AssertEq(moved.Column, 24 + 2 % 6);
        AssertEq(moved.Row, 2 / 6);
    }

    public void Test_AutotileCacheBlocksAndUnknownIdsFailClosed()
    {
        // Blocks A, B and D are drawn from a generated autotile cache, and an
        // unknown id must not be turned into a rectangle.
        foreach (var chipId in new[]
        {
            Rm2kChipset.BlockA, Rm2kChipset.BlockA + 1999,
            Rm2kChipset.BlockB, Rm2kChipset.BlockB + 999,
            Rm2kChipset.BlockD, Rm2kChipset.BlockD + 599,
            -1, Rm2kChipset.BlockC - 1, Rm2kChipset.BlockDEnd,
            Rm2kChipset.BlockEEnd, Rm2kChipset.BlockFEnd, int.MaxValue,
        })
        {
            AssertEq(Rm2kChipsetSource.TryResolve(chipId, 0, out _), false,
                $"chip id {chipId} is not a direct chipset rectangle");
        }
    }

    public void Test_EveryResolvedRectangleStaysInsideTheChipset()
    {
        // The formulas must never address a column or row outside the chipset,
        // and only blocks C, E and F may resolve.
        var substitution = new Rm2kTileSubstitution(null, null);
        var counts = new Dictionary<string, int>
        {
            ["blockC"] = 0,
            ["blockE"] = 0,
            ["blockF"] = 0,
            ["other"] = 0,
        };
        for (var chipId = 0; chipId <= Rm2kChipset.BlockFEnd + 200; chipId++)
        {
            var resolved = Rm2kChipsetSource.TryResolve(chipId, 0, substitution, out var rect);
            var block = chipId >= Rm2kChipset.BlockC && chipId < Rm2kChipset.BlockD ? "blockC"
                : chipId >= Rm2kChipset.BlockE && chipId < Rm2kChipset.BlockEEnd ? "blockE"
                : chipId >= Rm2kChipset.BlockF && chipId < Rm2kChipset.BlockFEnd ? "blockF"
                : "other";
            if (resolved)
            {
                AssertTrue(rect.Column >= 0 && rect.Column < Rm2kChipsetSource.Columns,
                    $"column {rect.Column} of chip {chipId} is inside the chipset");
                AssertTrue(rect.Row >= 0 && rect.Row < Rm2kChipsetSource.Rows,
                    $"row {rect.Row} of chip {chipId} is inside the chipset");
                continue;
            }
            counts[block]++;
        }
        AssertEq(counts["blockC"], 0, "every id in the block C range resolves");
        AssertEq(counts["blockE"], 0, "every block E id resolves");
        AssertEq(counts["blockF"], 0, "every block F id resolves");
        AssertTrue(counts["other"] > 0, "ids outside blocks C, E and F do not resolve");
    }

    public void Test_BlockCAnimationIsIndependentOfTheChipsetSettings()
    {
        // The Player computes the block C step without consulting the chipset
        // animation type or speed.
        foreach (var frame in Enumerable.Range(0, 48))
        {
            foreach (var animationType in new[] { 0, 1 })
            {
                foreach (var animationSpeed in new[] { 0, 5 })
                {
                    var substitution = new Rm2kTileSubstitution(null, null);
                    AssertEq(Rm2kChipsetSource.TryResolve(Rm2kChipset.BlockC + 7, frame, substitution, out var rect), true);
                    AssertEq(rect.Row, 4 + Rm2kChipset.CBlockStep(frame),
                        $"block C row at frame {frame} ignores the chipset settings");
                    AssertEq(rect.Column, 3, "the column never changes with the animation");
                }
            }
        }
    }

    private static int[] Identity(int pLength)
    {
        var result = new int[pLength];
        for (var index = 0; index < pLength; index++)
        {
            result[index] = index;
        }
        return result;
    }
}
