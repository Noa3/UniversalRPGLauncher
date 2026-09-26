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
        // Mismatched layer lengths must never invent tiles.
        AssertEq(Rm2kChipset.BuildDirectionMasks(new[] { 0, 0 }, upper, lowerFlags, upperFlags).Length, 2,
            "mismatched layers truncate to the shorter layer");
        AssertEq(Rm2kChipset.BuildDirectionMasks(lower, new[] { 10000 }, lowerFlags, upperFlags).Length, 1,
            "a short upper layer cannot extend the map");
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

    public void Test_AnimationSpeedMapsChipsetFlagToFrames()
    {
        // Game_Map::GetAnimationSpeed(): only "animated or not" is stored.
        AssertEq(Rm2kChipset.AnimationSpeed(0), 24, "a chipset without animation is not animated");
        AssertEq(Rm2kChipset.AnimationSpeed(1), 12, "an animated chipset steps every 12 frames");
        AssertEq(Rm2kChipset.AnimationSpeed(99), 12, "only the zero/non-zero distinction matters");
        AssertEq(Rm2kChipset.AnimationSpeed(-3), 12, "a negative flag still counts as animated");
    }

    public void Test_ReciprocatingStepSkipsTheFourthFrame()
    {
        // animation_type == 0: (frames / speed) % 4, with 3 replaced by 1.
        const int speed = 24;
        AssertEq(Rm2kChipset.ReciprocatingStep(0, speed), 0);
        AssertEq(Rm2kChipset.ReciprocatingStep(23, speed), 0);
        AssertEq(Rm2kChipset.ReciprocatingStep(24, speed), 1);
        AssertEq(Rm2kChipset.ReciprocatingStep(47, speed), 1);
        AssertEq(Rm2kChipset.ReciprocatingStep(48, speed), 2);
        AssertEq(Rm2kChipset.ReciprocatingStep(71, speed), 2);
        AssertEq(Rm2kChipset.ReciprocatingStep(72, speed), 1, "the fourth step shows frame one");
        AssertEq(Rm2kChipset.ReciprocatingStep(95, speed), 1);
        AssertEq(Rm2kChipset.ReciprocatingStep(96, speed), 0, "the cycle returns to frame zero");
        AssertEq(Rm2kChipset.ReciprocatingStep(0, 0), 0, "a zero speed cannot divide by zero");
    }

    public void Test_CyclicStepUsesThreeEvenFrames()
    {
        const int speed = 12;
        AssertEq(Rm2kChipset.CyclicStep(0, speed), 0);
        AssertEq(Rm2kChipset.CyclicStep(11, speed), 0);
        AssertEq(Rm2kChipset.CyclicStep(12, speed), 1);
        AssertEq(Rm2kChipset.CyclicStep(24, speed), 2);
        AssertEq(Rm2kChipset.CyclicStep(35, speed), 2);
        AssertEq(Rm2kChipset.CyclicStep(36, speed), 0, "the cycle returns to frame zero");
    }

    public void Test_CBlockAnimatesOnItsOwnFixedCycle()
    {
        // Block C ignores the chipset animation type and speed entirely.
        AssertEq(Rm2kChipset.CBlockStep(0), 0);
        AssertEq(Rm2kChipset.CBlockStep(5), 0);
        AssertEq(Rm2kChipset.CBlockStep(6), 1);
        AssertEq(Rm2kChipset.CBlockStep(17), 2);
        AssertEq(Rm2kChipset.CBlockStep(18), 3);
        AssertEq(Rm2kChipset.CBlockStep(23), 3);
        AssertEq(Rm2kChipset.CBlockStep(24), 0);
        for (var frame = 0; frame < 24; frame++)
        {
            foreach (var animationType in new[] { 0, 1 })
            {
                foreach (var animationSpeed in new[] { 0, 7 })
                {
                    AssertEq(
                        Rm2kChipset.ChipAnimationStep(Rm2kChipset.BlockC, frame, animationType, animationSpeed),
                        Rm2kChipset.CBlockStep(frame),
                        "block C is independent of the chipset animation settings");
                }
            }
        }
    }

    public void Test_ChipAnimationStepDispatchesPerBlock()
    {
        // Blocks A/B animate with the chipset settings.
        AssertEq(Rm2kChipset.ChipAnimationStep(Rm2kChipset.BlockA, 0, 0, 0), 0);
        AssertEq(Rm2kChipset.ChipAnimationStep(Rm2kChipset.BlockA, 24, 0, 0), 1);
        AssertEq(Rm2kChipset.ChipAnimationStep(Rm2kChipset.BlockB + 999, 24, 0, 0), 1);
        AssertEq(Rm2kChipset.ChipAnimationStep(Rm2kChipset.BlockA, 24, 1, 1), 2, "cyclic step with speed 12");
        AssertEq(Rm2kChipset.ChipAnimationStep(Rm2kChipset.BlockA, 24, 1, 0), 1, "speed 24 keeps the same step later");

        // Blocks D, E and F never animate.
        foreach (var chipId in new[] { Rm2kChipset.BlockD, Rm2kChipset.BlockDEnd - 1, Rm2kChipset.BlockE, Rm2kChipset.BlockF, Rm2kChipset.BlockFEnd - 1 })
        {
            for (var frame = 0; frame < 120; frame++)
            {
                foreach (var animationType in new[] { 0, 1 })
                {
                    AssertEq(Rm2kChipset.ChipAnimationStep(chipId, frame, animationType, 1), 0,
                        $"chip {chipId} never animates");
                }
            }
        }
    }

    public void Test_SubstitutionDefaultsToIdentity()
    {
        // Game_Map::Setup fills both tables with std::iota, and liblcf
        // rpg::SaveMapInfo stores 144 identity entries.
        var substitution = new Rm2kTileSubstitution(null, null);
        for (var index = 0; index < 144; index++)
        {
            AssertEq(substitution.SubstituteUpper(index), index, "upper identity");
            AssertEq(substitution.SubstituteLower(index), index + Rm2kChipset.BlockEIndex, "lower identity");
        }
    }

    public void Test_SubstitutionReplacesPassabilityLookups()
    {
        var lowerFlags = new byte[162];
        var upperFlags = new byte[144];
        Array.Fill(lowerFlags, Rm2kChipset.AllDirections);
        Array.Fill(upperFlags, Rm2kChipset.AllDirections);
        // Block E entry 0 and upper entry 0 are both impassable, and the tables
        // redirect them to the fully passable entries at the end.
        lowerFlags[Rm2kChipset.BlockEIndex] = 0;
        upperFlags[0] = 0;

        AssertFalse(Rm2kChipset.IsPassableTile(
            Rm2kChipset.BlockE, Rm2kChipset.BlockF, lowerFlags, upperFlags, Rm2kChipset.PassRight),
            "without substitution the blocked entries decide");

        var lowerTable = Identity(144);
        var upperTable = Identity(144);
        lowerTable[0] = 143;
        upperTable[0] = 143;
        var substitution = new Rm2kTileSubstitution(lowerTable, upperTable);

        AssertTrue(Rm2kChipset.IsPassableTile(
            Rm2kChipset.BlockE, Rm2kChipset.BlockF, lowerFlags, upperFlags, Rm2kChipset.PassRight, substitution),
            "the upper table redirects the blocked upper entry");
        AssertTrue(Rm2kChipset.IsPassableLowerTile(
            Rm2kChipset.BlockE, lowerFlags, Rm2kChipset.PassRight, substitution),
            "the lower table redirects the blocked block E entry");

        // The substitution is only applied to the verified ranges: a lower tile
        // from block A/B/C is never remapped.
        var lowerBlockedA = new byte[162];
        Array.Fill(lowerBlockedA, Rm2kChipset.AllDirections);
        lowerBlockedA[1] = 0;
        AssertFalse(Rm2kChipset.IsPassableLowerTile(1000, lowerBlockedA, Rm2kChipset.PassRight, substitution),
            "block A entries are not substituted");
    }

    public void Test_SubstitutionFailsClosedOnOutOfRangeTables()
    {
        var lowerFlags = new byte[162];
        var upperFlags = new byte[144];
        Array.Fill(lowerFlags, Rm2kChipset.AllDirections);
        Array.Fill(upperFlags, Rm2kChipset.AllDirections);

        // A malformed table falls back to identity rather than clamping.
        var brokenLower = Identity(144);
        brokenLower[0] = 200;
        var brokenUpper = Identity(144);
        brokenUpper[0] = -1;
        var substitution = new Rm2kTileSubstitution(brokenLower, brokenUpper);
        AssertEq(substitution.SubstituteLower(0), 0 + Rm2kChipset.BlockEIndex, "a broken table falls back to identity");
        AssertEq(substitution.SubstituteUpper(0), 0);

        // Requests outside the table range cannot be resolved.
        AssertEq(substitution.SubstituteUpper(144), -1);
        AssertEq(substitution.SubstituteUpper(-1), -1);
        AssertEq(substitution.SubstituteLower(144), -1);

        var passable = new byte[162];
        Array.Fill(passable, Rm2kChipset.AllDirections);
        AssertFalse(Rm2kChipset.IsPassableLowerTile(
            Rm2kChipset.BlockE + 144, passable, Rm2kChipset.PassRight, substitution),
            "a block E id beyond the table fails closed");
        AssertFalse(Rm2kChipset.IsPassableTile(
            0, Rm2kChipset.BlockF + 144, passable, upperFlags, Rm2kChipset.PassRight, substitution),
            "an upper id beyond the table fails closed");
    }

    public void Test_ChipIndexResolutionAppliesLowerSubstitution()
    {
        // Verified Game_Map::GetChipId: the raw id becomes a chip index first,
        // and only indices in [BLOCK_E_INDEX, NUM_LOWER_TILES) are remapped.
        var lowerTable = Identity(144);
        lowerTable[0] = 5;
        var substitution = new Rm2kTileSubstitution(lowerTable, null);
        AssertEq(substitution.ResolveChipIndex(Rm2kChipset.BlockE), Rm2kChipset.BlockEIndex + 5,
            "a block E tile follows the lower table");
        AssertEq(substitution.ResolveChipIndex(Rm2kChipset.BlockD), Rm2kChipset.BlockDIndex,
            "a block D tile is not substituted");
        AssertEq(substitution.ResolveChipIndex(Rm2kChipset.BlockA + 500), 0,
            "a block A tile is not substituted");
        AssertEq(substitution.ResolveChipIndex(Rm2kChipset.BlockF), Rm2kChipset.NumLowerTiles,
            "an upper tile keeps its index outside the lower range");
    }

    public void Test_SimulationAdvancesChipAnimationWithTheFrameCount()
    {
        var state = new GameSimulationState();
        state.ChipsetAnimationType = Rm2kChipset.AnimTypeReciprocating;
        state.ChipsetAnimationSpeed = 0;

        // The simulation frame counter is the Player's frame counter, so the
        // step must follow it instead of a wall clock.
        var seen = new List<int>();
        for (var frame = 0; frame < 120; frame++)
        {
            state.FrameCount = frame;
            seen.Add(state.GetChipAnimationStep(Rm2kChipset.BlockA));
        }

        AssertEq(seen[0], 0);
        AssertEq(seen[1], 0, "the first 24 frames hold the first autotile frame");
        AssertEq(seen[24], 1);
        AssertEq(seen[48], 2);
        AssertEq(seen[72], 1, "the reciprocating cycle skips the fourth frame");
        AssertEq(seen[96], 0);

        // Block C uses its own fixed cycle, blocks D-F never animate.
        state.FrameCount = 6;
        AssertEq(state.GetChipAnimationStep(Rm2kChipset.BlockC), 1);
        state.FrameCount = 0;
        AssertEq(state.GetChipAnimationStep(Rm2kChipset.BlockF), 0);
        AssertEq(state.GetChipAnimationStep(Rm2kChipset.BlockE), 0);

        // A cyclic chipset advances every 12 frames instead.
        state.ChipsetAnimationType = Rm2kChipset.AnimTypeCyclic;
        state.ChipsetAnimationSpeed = 1;
        state.FrameCount = 11;
        AssertEq(state.GetChipAnimationStep(Rm2kChipset.BlockA), 0);
        state.FrameCount = 12;
        AssertEq(state.GetChipAnimationStep(Rm2kChipset.BlockA), 1);
    }
}
