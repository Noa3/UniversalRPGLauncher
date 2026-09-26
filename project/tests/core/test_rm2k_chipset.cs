using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Locks the verified RM2K tile-id and passability rules from EasyRPG Player
/// <c>map_data.h</c> and the Game_Map passability helpers.
/// </summary>
public partial class TestRm2kChipset : TestBase
{
    public void Test_VerifiedPassabilityFlagValues()
    {
        AssertEq(Rm2kChipset.PassDown, 0x01);
        AssertEq(Rm2kChipset.PassLeft, 0x02);
        AssertEq(Rm2kChipset.PassRight, 0x04);
        AssertEq(Rm2kChipset.PassUp, 0x08);
        AssertEq(Rm2kChipset.PassAbove, 0x10);
        AssertEq(Rm2kChipset.PassWall, 0x20);
        AssertEq(Rm2kChipset.PassCounter, 0x40);
        AssertEq(Rm2kChipset.AllDirections, 15, "liblcf default lower entry is 15");
    }

    public void Test_VerifiedTileBlockConstants()
    {
        AssertEq(Rm2kChipset.BlockA, 0);
        AssertEq(Rm2kChipset.BlockAEnd, 2000);
        AssertEq(Rm2kChipset.BlockB, 2000);
        AssertEq(Rm2kChipset.BlockBEnd, 3000);
        AssertEq(Rm2kChipset.BlockC, 3000);
        AssertEq(Rm2kChipset.BlockCEnd, 3150);
        AssertEq(Rm2kChipset.BlockD, 4000);
        AssertEq(Rm2kChipset.BlockDEnd, 4600);
        AssertEq(Rm2kChipset.BlockE, 5000);
        AssertEq(Rm2kChipset.BlockEEnd, 5144);
        AssertEq(Rm2kChipset.BlockF, 10000);
        AssertEq(Rm2kChipset.BlockFEnd, 10144);
        AssertEq(Rm2kChipset.NumLowerTiles, 162);
        AssertEq(Rm2kChipset.NumUpperTiles, 144);
    }

    public void Test_ChipIdToIndexCoversEveryBlock()
    {
        AssertEq(Rm2kChipset.ChipIdToIndex(0), 0, "block A start");
        AssertEq(Rm2kChipset.ChipIdToIndex(1000), 1, "block A second tile");
        AssertEq(Rm2kChipset.ChipIdToIndex(2000), 2, "block B");
        AssertEq(Rm2kChipset.ChipIdToIndex(3000), 3, "block C start");
        AssertEq(Rm2kChipset.ChipIdToIndex(3050), 4, "block C stride");
        AssertEq(Rm2kChipset.ChipIdToIndex(4000), 6, "block D start");
        AssertEq(Rm2kChipset.ChipIdToIndex(4050), 7, "block D stride");
        AssertEq(Rm2kChipset.ChipIdToIndex(5000), 18, "block E start");
        AssertEq(Rm2kChipset.ChipIdToIndex(5143), 161, "block E end");
        AssertEq(Rm2kChipset.ChipIdToIndex(10000), 162, "block F start");
        AssertEq(Rm2kChipset.ChipIdToIndex(10143), 305, "block F end");
        AssertEq(Rm2kChipset.ChipIdToIndex(-1), 0, "unknown ids fall back to index zero");
    }

    public void Test_IndexToChipIdRoundTripsChipIds()
    {
        foreach (var chipId in new[] { 0, 1000, 2000, 3000, 3050, 4000, 4050, 5000, 5143, 10000, 10143 })
        {
            var index = Rm2kChipset.ChipIdToIndex(chipId);
            AssertEq(Rm2kChipset.IndexToChipId(index), chipId, $"chip id {chipId} round trips");
        }
    }

    public void Test_DirectionBitMatchesSingleCardinalStep()
    {
        AssertEq(Rm2kChipset.DirectionBit(1, 0), Rm2kChipset.PassRight);
        AssertEq(Rm2kChipset.DirectionBit(-1, 0), Rm2kChipset.PassLeft);
        AssertEq(Rm2kChipset.DirectionBit(0, 1), Rm2kChipset.PassDown);
        AssertEq(Rm2kChipset.DirectionBit(0, -1), Rm2kChipset.PassUp);
        AssertEq(Rm2kChipset.DirectionBit(0, 0), (byte)0, "no step has no direction");
        AssertEq(Rm2kChipset.DirectionBit(1, 1), (byte)0, "diagonal steps have no direction");
    }

    public void Test_UpperTileDecidesPassabilityAndFallsThroughWhenAbove()
    {
        var lower = new byte[162];
        var upper = new byte[144];
        Array.Fill(lower, Rm2kChipset.AllDirections);
        Array.Fill(upper, Rm2kChipset.AllDirections);

        // An upper tile without the Above flag is passable on its own.
        AssertTrue(Rm2kChipset.IsPassableTile(0, 10000, lower, upper, Rm2kChipset.PassRight),
            "upper tile without Above decides the step");

        // With the Above flag the lower layer decides, so a blocked lower tile blocks.
        upper[0] |= Rm2kChipset.PassAbove;
        lower[0] = 0;
        AssertFalse(Rm2kChipset.IsPassableTile(0, 10000, lower, upper, Rm2kChipset.PassRight),
            "an above upper tile falls through to a blocked lower tile");

        lower[0] = Rm2kChipset.PassRight;
        AssertTrue(Rm2kChipset.IsPassableTile(0, 10000, lower, upper, Rm2kChipset.PassRight),
            "a direction-open lower tile passes again");
        AssertFalse(Rm2kChipset.IsPassableTile(0, 10000, lower, upper, Rm2kChipset.PassLeft),
            "a one-way lower tile keeps the other direction closed");
    }

    public void Test_WallAutotileExceptionAllowsEnteringWallTile()
    {
        var lower = new byte[162];
        Array.Fill(lower, Rm2kChipset.AllDirections);
        // Block D autotile 20 with the Wall flag: Game_Map lets a character enter.
        const int wallAutotileChipId = 4000 + 20;
        lower[Rm2kChipset.ChipIdToIndex(wallAutotileChipId)] = Rm2kChipset.PassWall;
        AssertTrue(Rm2kChipset.IsPassableLowerTile(wallAutotileChipId, lower, Rm2kChipset.PassUp),
            "wall autotile 20 is enterable");

        // Autotile 10 carries no Wall flag, so it stays closed.
        const int plainAutotileChipId = 4000 + 10;
        lower[Rm2kChipset.ChipIdToIndex(plainAutotileChipId)] = 0;
        AssertFalse(Rm2kChipset.IsPassableLowerTile(plainAutotileChipId, lower, Rm2kChipset.PassUp),
            "a plain autotile without the Wall flag stays closed");
    }

    public void Test_MasksFailClosedOnUnknownAndMissingData()
    {
        var lower = new[] { 0, 0, 0 };
        // Tile 0 uses upper chip 10000 (self-sufficient), tile 1 upper chip
        // 10001 (needs the lower layer), tile 2 an unknown upper id. Flags
        // live per chip id, so the two upper ids must differ.
        var upper = new[] { 10000, 10001, -5 };
        var lowerFlags = new byte[162];
        var upperFlags = new byte[144];
        Array.Fill(lowerFlags, Rm2kChipset.AllDirections);
        Array.Fill(upperFlags, Rm2kChipset.AllDirections);
        upperFlags[1] |= Rm2kChipset.PassAbove;

        var masks = Rm2kChipset.BuildDirectionMasks(lower, upper, lowerFlags, upperFlags);
        AssertEq(masks.Length, 3);
        AssertEq(masks[0], Rm2kChipset.AllDirections, "a plain upper tile needs no lower table");
        AssertEq(masks[1], Rm2kChipset.AllDirections, "an above upper tile falls through to a passable lower tile");
        AssertEq(masks[2], (byte)0, "an unknown upper tile id fails closed");

        // Without the lower table only the self-sufficient upper tile stays open.
        var withoutLowerTable = Rm2kChipset.BuildDirectionMasks(lower, upper, [], upperFlags);
        AssertEq(withoutLowerTable.Length, 3, "mask shape still follows the map size");
        AssertEq(withoutLowerTable[0], Rm2kChipset.AllDirections, "the self-sufficient upper tile stays open");
        AssertEq(withoutLowerTable[1], (byte)0, "an above upper tile without a lower table fails closed");
        AssertEq(withoutLowerTable[2], (byte)0, "an unknown upper tile id still fails closed");

        // Mismatched layer lengths must never invent tiles.
        AssertEq(Rm2kChipset.BuildDirectionMasks(new[] { 0, 0 }, upper, lowerFlags, upperFlags).Length, 2,
            "mismatched layers truncate to the shorter layer");
        AssertEq(Rm2kChipset.BuildDirectionMasks(lower, new[] { 10000 }, lowerFlags, upperFlags).Length, 1,
            "a short upper layer cannot extend the map");
    }
}
