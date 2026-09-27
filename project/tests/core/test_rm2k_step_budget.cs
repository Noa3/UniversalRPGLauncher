using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the RPG Maker 2000 per frame step budget. The point of this card is
/// that a tile is not crossed in one call: at the default move speed 3 it takes
/// exactly sixteen updates, and the drawn position passes through the tile in
/// between. A runtime that snaps produces the same final picture and a
/// completely different animation, so the tests check the ticks and the
/// intermediate positions, not the destination.
/// </summary>
public partial class TestRm2kStepBudget : TestBase
{
    public void Test_TheMovementAmountMatchesTheVerifiedFormula()
    {
        // Game_Player::Update uses 1 << (1 + GetMoveSpeed()).
        AssertEq(Rm2kStepBudget.MovementAmount(1), 4, "speed 1 moves 4 of 256");
        AssertEq(Rm2kStepBudget.MovementAmount(2), 8, "speed 2 moves 8");
        AssertEq(Rm2kStepBudget.MovementAmount(3), 16, "speed 3 moves 16");
        AssertEq(Rm2kStepBudget.MovementAmount(4), 32, "speed 4 moves 32");
        AssertEq(Rm2kStepBudget.MovementAmount(5), 64, "speed 5 moves 64");
        AssertEq(Rm2kStepBudget.MovementAmount(6), 128, "speed 6 moves 128");
    }

    public void Test_TheDefaultMoveSpeedTakesSixteenUpdatesPerTile()
    {
        // This is the number a player sees. A single step call is not the same
        // behaviour even though it reaches the same tile.
        AssertEq(Rm2kStepBudget.UpdatesPerTile(3), 16, "speed 3 takes 16 updates per tile");
        AssertEq(Rm2kStepBudget.UpdatesPerTile(1), 64, "speed 1 takes 64 updates per tile");
        AssertEq(Rm2kStepBudget.UpdatesPerTile(6), 2, "speed 6 takes 2 updates per tile");
    }

    public void Test_AFullStepTakesTheDocumentedNumberOfUpdates()
    {
        var remaining = Rm2kStepBudget.ScreenTileSize;
        var ticks = 0;
        var completed = false;
        while (!completed)
        {
            (remaining, completed) = Rm2kStepBudget.Advance(remaining, 3);
            ticks++;
            AssertTrue(ticks < 100, "the loop terminates");
        }
        AssertEq(ticks, 16, "a tile at the default speed takes exactly sixteen updates");
        AssertEq(remaining, 0, "the budget is clamped at zero, never negative");
    }

    public void Test_TheStepBudgetNeverGoesNegative()
    {
        // UpdateMovement clamps with SetRemainingStep(0) when the subtraction
        // underflows, so a partially spent budget cannot wrap into a huge
        // number and throw the sprite across the map.
        var (remaining, completed) = Rm2kStepBudget.Advance(5, 3);
        AssertEq(remaining, 0, "an overshoot clamps to zero");
        AssertTrue(completed, "and it reports the tile as completed");
    }

    public void Test_TheDrawnPositionSlidesThroughTheTileInsteadOfSnapping()
    {
        // GetSpriteX is X * 256 - remaining_step for a move to the right, so
        // the sprite starts one tile left of its new logical tile and reaches
        // it exactly when the budget runs out. This is the difference between
        // walking and teleporting.
        const int directionRight = 1;
        var logicalX = 10;
        var offsets = new System.Collections.Generic.List<int>();
        var remaining = Rm2kStepBudget.ScreenTileSize;
        for (var tick = 0; tick < 16; tick++)
        {
            offsets.Add(Rm2kStepBudget.PixelOffsetX(logicalX, remaining, directionRight));
            (remaining, _) = Rm2kStepBudget.Advance(remaining, 3);
        }

        AssertEq(offsets.Count, 16, "sixteen updates produced sixteen positions");
        AssertEq(offsets[0], -16, "the first update is one whole tile back");
        AssertEq(offsets[1], -15, "and the sprite moves one pixel per update");
        AssertEq(offsets[7], -9, "half a tile in after eight updates");
        AssertEq(offsets[15], -1, "one pixel short of the tile after fifteen updates");
        AssertEq(Rm2kStepBudget.PixelOffsetX(logicalX, 0, directionRight), 0,
            "an exhausted budget puts the sprite exactly on its logical tile");
    }

    public void Test_EachDirectionDrawsTheStepOnItsOwnAxis()
    {
        // GetSpriteX only offsets for the horizontal directions and GetSpriteY
        // only for the vertical ones, so a character walking down does not
        // drift horizontally.
        AssertEq(Rm2kStepBudget.PixelOffsetX(5, 128, 1), -8, "moving right offsets x");
        AssertEq(Rm2kStepBudget.PixelOffsetX(5, 128, 3), 8, "moving left offsets the other way");
        AssertEq(Rm2kStepBudget.PixelOffsetX(5, 128, 2), 0, "moving down does not touch x");
        AssertEq(Rm2kStepBudget.PixelOffsetX(5, 128, 0), 0, "moving up does not touch x");

        AssertEq(Rm2kStepBudget.PixelOffsetY(5, 128, 2), -8, "moving down offsets y");
        AssertEq(Rm2kStepBudget.PixelOffsetY(5, 128, 0), 8, "moving up offsets the other way");
        AssertEq(Rm2kStepBudget.PixelOffsetY(5, 128, 1), 0, "moving right does not touch y");
        AssertEq(Rm2kStepBudget.PixelOffsetY(5, 128, 3), 0, "moving left does not touch y");
    }

    public void Test_TheStopCountTableMatchesTheVerifiedValues()
    {
        // GetMaxStopCountForStep is 1 << (9 - freq), and 8 or more means no wait.
        AssertEq(Rm2kStepBudget.MaxStopCountForStep(1), 256, "frequency 1 waits 256");
        AssertEq(Rm2kStepBudget.MaxStopCountForStep(2), 128, "frequency 2 waits 128");
        AssertEq(Rm2kStepBudget.MaxStopCountForStep(4), 32, "frequency 4 waits 32");
        AssertEq(Rm2kStepBudget.MaxStopCountForStep(7), 4, "frequency 7 waits 4");
        AssertEq(Rm2kStepBudget.MaxStopCountForStep(8), 0, "frequency 8 does not wait");
        AssertEq(Rm2kStepBudget.MaxStopCountForStep(12), 0, "a higher frequency does not wait either");

        AssertEq(Rm2kStepBudget.MaxStopCountForTurn(1), 128, "turn frequency 1 waits 128");
        AssertEq(Rm2kStepBudget.MaxStopCountForTurn(8), 0, "turn frequency 8 does not wait");
        AssertEq(Rm2kStepBudget.MaxStopCountForWait(1), 148, "a wait is 20 plus the turn count");
    }

    public void Test_TheStopCountIsSeparateFromTheMoveSpeed()
    {
        // A character can move slowly and still start the next step
        // immediately. Tying the two together would make a slow character also
        // wait between steps, which is not what either table says.
        AssertEq(Rm2kStepBudget.MaxStopCountForStep(3), 64, "frequency 3 waits 64 updates");
        AssertEq(Rm2kStepBudget.UpdatesPerTile(3), 16, "while move speed 3 still takes 16 per tile");
    }

    public void Test_AMoveSpeedOutsideTheDefinedRangeIsRefused()
    {
        foreach (var speed in new[] { 0, -1, 7, 200 })
        {
            var error = "";
            try
            {
                Rm2kStepBudget.MovementAmount(speed);
            }
            catch (UniversalRPG.Rm2k.Rendering.Rm2kAnimationDataException exception)
            {
                error = exception.Message;
            }
            AssertTrue(error.Contains("outside the defined range", System.StringComparison.OrdinalIgnoreCase),
                $"move speed {speed} must be refused, but the reader said: {error}");
        }
    }

    public void Test_TheSimulationStateDrivesTheStepBudgetOverSeveralUpdates()
    {
        // The state has to hold a remaining step, not just a tile position, or
        // the animation has nothing to advance and every move is a snap.
        var state = new GameSimulationState();
        state.Reset();
        // Every tile passable in every direction, so the step is refused by
        // nothing but the budget.
        // The map id comes first, then width and height: a 4 by 4 map is
        // 4 tiles wide and 4 tall, so sixteen masks.
        state.ConfigureMap(1, 4, 4, AllPassable(16));
        state.HeroMoveSpeed = 3;

        AssertEq(state.RemainingStep, 0, "a character that is not moving has no budget");

        var moved = state.TryMove(0, 1);
        AssertTrue(moved, "the move is accepted");
        AssertEq(state.RemainingStep, Rm2kStepBudget.ScreenTileSize,
            "starting a move fills the budget to SCREEN_TILE_SIZE");

        var ticks = 0;
        while (state.RemainingStep > 0 && ticks < 100)
        {
            state.UpdateCharacterAnimation(pMoving: true);
            ticks++;
        }
        AssertEq(ticks, 16, "the move completes after sixteen updates, not one call");
        AssertEq(state.RemainingStep, 0, "the budget is exhausted");
    }

    /// <summary>
    /// <see cref="Rm2kChipset.AllDirections"/> for every tile, so a step is
    /// refused by nothing but the step budget.
    /// </summary>
    private static System.Collections.Generic.IEnumerable<byte> AllPassable(int pCount)
    {
        var masks = new byte[pCount];
        for (var index = 0; index < masks.Length; index++)
        {
            masks[index] = UniversalRPG.Rm2k.Simulation.Rm2kChipset.AllDirections;
        }
        return masks;
    }

    public void Test_ResetClearsTheStepBudget()
    {
        var state = new GameSimulationState();
        state.Reset();
        // Every tile passable in every direction, so the step is refused by
        // nothing but the budget.
        // The map id comes first, then width and height: a 4 by 4 map is
        // 4 tiles wide and 4 tall, so sixteen masks.
        state.ConfigureMap(1, 4, 4, AllPassable(16));
        state.TryMove(0, 1);
        AssertEq(state.RemainingStep, Rm2kStepBudget.ScreenTileSize, "the move filled the budget");

        state.Reset();
        AssertEq(state.RemainingStep, 0, "a new game does not inherit a half finished step");
    }
}
