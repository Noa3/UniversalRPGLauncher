using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the RPG Maker 2000 move route command set and direction arithmetic
/// against <c>Game_Character::UpdateMoveRoute</c> and liblcf's own
/// <c>rpg::MoveCommand::Code</c> enum.
/// </summary>
/// <remarks>
/// The command ids are not sequential in a way that could be invented: the
/// movement commands are 0 to 11, the facing commands 12 to 22, and everything
/// else follows from 23. The Player relies on exactly that contiguity, testing
/// <c>cmd &gt;= move_up &amp;&amp; cmd &lt;= move_forward</c> for movement and
/// <c>cmd &gt;= face_up &amp;&amp; cmd &lt;= face_away_from_hero</c> for facing,
/// so an id that lands in the wrong place is a behaviour change, not a label.
/// </remarks>
public partial class TestRm2kMoveRoute : TestBase
{
    public void Test_TheCommandIdsMatchTheLiblcfEnum()
    {
        AssertEq(Rm2kMoveRoute.MoveUp, 0, "move_up");
        AssertEq(Rm2kMoveRoute.MoveRight, 1, "move_right");
        AssertEq(Rm2kMoveRoute.MoveDown, 2, "move_down");
        AssertEq(Rm2kMoveRoute.MoveLeft, 3, "move_left");
        AssertEq(Rm2kMoveRoute.MoveUpRight, 4, "move_upright");
        AssertEq(Rm2kMoveRoute.MoveDownRight, 5, "move_downright");
        AssertEq(Rm2kMoveRoute.MoveDownLeft, 6, "move_downleft");
        AssertEq(Rm2kMoveRoute.MoveUpLeft, 7, "move_upleft");
        AssertEq(Rm2kMoveRoute.MoveRandom, 8, "move_random");
        AssertEq(Rm2kMoveRoute.MoveTowardsHero, 9, "move_towards_hero");
        AssertEq(Rm2kMoveRoute.MoveAwayFromHero, 10, "move_away_from_hero");
        AssertEq(Rm2kMoveRoute.MoveForward, 11, "move_forward");
        AssertEq(Rm2kMoveRoute.FaceUp, 12, "face_up");
        AssertEq(Rm2kMoveRoute.FaceRight, 13, "face_right");
        AssertEq(Rm2kMoveRoute.FaceDown, 14, "face_down");
        AssertEq(Rm2kMoveRoute.FaceLeft, 15, "face_left");
        AssertEq(Rm2kMoveRoute.Turn90DegreeRight, 16, "turn_90_degree_right");
        AssertEq(Rm2kMoveRoute.Turn90DegreeLeft, 17, "turn_90_degree_left");
        AssertEq(Rm2kMoveRoute.Turn180Degree, 18, "turn_180_degree");
        AssertEq(Rm2kMoveRoute.Turn90DegreeRandom, 19, "turn_90_degree_random");
        AssertEq(Rm2kMoveRoute.FaceRandomDirection, 20, "face_random_direction");
        AssertEq(Rm2kMoveRoute.FaceHero, 21, "face_hero");
        AssertEq(Rm2kMoveRoute.FaceAwayFromHero, 22, "face_away_from_hero");
        AssertEq(Rm2kMoveRoute.Wait, 23, "wait");
        AssertEq(Rm2kMoveRoute.BeginJump, 24, "begin_jump");
        AssertEq(Rm2kMoveRoute.EndJump, 25, "end_jump");
        AssertEq(Rm2kMoveRoute.LockFacing, 26, "lock_facing");
        AssertEq(Rm2kMoveRoute.UnlockFacing, 27, "unlock_facing");
        AssertEq(Rm2kMoveRoute.IncreaseMovementSpeed, 28, "increase_movement_speed");
        AssertEq(Rm2kMoveRoute.DecreaseMovementSpeed, 29, "decrease_movement_speed");
        AssertEq(Rm2kMoveRoute.IncreaseMovementFrequence, 30, "increase_movement_frequence");
        AssertEq(Rm2kMoveRoute.DecreaseMovementFrequence, 31, "decrease_movement_frequence");
        AssertEq(Rm2kMoveRoute.SwitchOn, 32, "switch_on");
        AssertEq(Rm2kMoveRoute.SwitchOff, 33, "switch_off");
        AssertEq(Rm2kMoveRoute.ChangeGraphic, 34, "change_graphic");
        AssertEq(Rm2kMoveRoute.PlaySoundEffect, 35, "play_sound_effect");
        AssertEq(Rm2kMoveRoute.WalkEverywhereOn, 36, "walk_everywhere_on");
        AssertEq(Rm2kMoveRoute.WalkEverywhereOff, 37, "walk_everywhere_off");
        AssertEq(Rm2kMoveRoute.StopAnimation, 38, "stop_animation");
        AssertEq(Rm2kMoveRoute.StartAnimation, 39, "start_animation");
        AssertEq(Rm2kMoveRoute.IncreaseTransp, 40, "increase_transp");
        AssertEq(Rm2kMoveRoute.DecreaseTransp, 41, "decrease_transp");
    }

    public void Test_TheMovementAndFacingRangesAreExactlyContiguous()
    {
        // The Player recognises a movement command by range, so a hole in the
        // range would silently reclassify a command.
        for (var id = Rm2kMoveRoute.MoveUp; id <= Rm2kMoveRoute.MoveForward; id++)
        {
            AssertTrue(Rm2kMoveRoute.IsMovementCommand(id), $"command {id} is a movement command");
            AssertTrue(!Rm2kMoveRoute.IsFacingCommand(id), $"command {id} is not a facing command");
        }
        for (var id = Rm2kMoveRoute.FaceUp; id <= Rm2kMoveRoute.FaceAwayFromHero; id++)
        {
            AssertTrue(Rm2kMoveRoute.IsFacingCommand(id), $"command {id} is a facing command");
            AssertTrue(!Rm2kMoveRoute.IsMovementCommand(id), $"command {id} is not a movement command");
        }
        AssertTrue(!Rm2kMoveRoute.IsMovementCommand(Rm2kMoveRoute.Wait),
            "wait is neither a movement nor a facing command");
        AssertTrue(!Rm2kMoveRoute.IsFacingCommand(Rm2kMoveRoute.LockFacing),
            "lock_facing is not a facing command");
    }

    public void Test_TheNamedDirectionsMapOntoTheLiblcfOrder()
    {
        AssertEq(Rm2kMoveRoute.MovementCommandDirection(Rm2kMoveRoute.MoveUp), 0, "up is 0");
        AssertEq(Rm2kMoveRoute.MovementCommandDirection(Rm2kMoveRoute.MoveRight), 1, "right is 1");
        AssertEq(Rm2kMoveRoute.MovementCommandDirection(Rm2kMoveRoute.MoveDown), 2, "down is 2");
        AssertEq(Rm2kMoveRoute.MovementCommandDirection(Rm2kMoveRoute.MoveLeft), 3, "left is 3");
        AssertEq(Rm2kMoveRoute.FacingCommandDirection(Rm2kMoveRoute.FaceUp), 0, "face up is 0");
        AssertEq(Rm2kMoveRoute.FacingCommandDirection(Rm2kMoveRoute.FaceLeft), 3, "face left is 3");
        AssertEq(Rm2kMoveRoute.FacingCommandDirection(Rm2kMoveRoute.Turn90DegreeRight), -1,
            "a turning command needs the surrounding state, not a fixed direction");
    }

    public void Test_ADirectionDeltaMovesOneWholeTileOnEachAxis()
    {
        // GetDxFromDirection and GetDyFromDirection give a diagonal one tile on
        // each axis, never half a tile on both, so a diagonal step covers the
        // same distance as two cardinal steps.
        var cases = new (int Direction, int Dx, int Dy)[]
        {
            (0, 0, -1),  // up
            (1, 1, 0),   // right
            (2, 0, 1),   // down
            (3, -1, 0),  // left
            (4, 1, -1),  // up right
            (5, 1, 1),   // down right
            (6, -1, 1),  // down left
            (7, -1, -1), // up left
        };
        foreach (var (direction, dx, dy) in cases)
        {
            var delta = Rm2kMoveRoute.DirectionDelta(direction);
            AssertEq(delta.Dx, dx, $"dx of direction {direction}");
            AssertEq(delta.Dy, dy, $"dy of direction {direction}");
        }
    }

    public void Test_EveryDirectionDeltaIsWithinOneTile()
    {
        // A delta outside plus or minus one would let a step skip a tile, and
        // no combination of the upstream formulas can produce that.
        for (var direction = 0; direction < 8; direction++)
        {
            var (dx, dy) = Rm2kMoveRoute.DirectionDelta(direction);
            AssertTrue(Math.Abs(dx) <= 1, $"dx of direction {direction} is within one tile");
            AssertTrue(Math.Abs(dy) <= 1, $"dy of direction {direction} is within one tile");
            AssertTrue(Math.Abs(dx) + Math.Abs(dy) > 0,
                $"direction {direction} moves somewhere rather than nowhere");
        }
    }

    public void Test_TheTurnsRotateOnTheLiblcfOrder()
    {
        // The order is up, right, down, left, so a right turn is (dir + 1) % 4.
        for (var direction = 0; direction < 4; direction++)
        {
            AssertEq(Rm2kMoveRoute.TurnRight(direction), (direction + 1) % 4,
                $"right turn of {direction}");
            AssertEq(Rm2kMoveRoute.TurnLeft(direction), (direction + 3) % 4,
                $"left turn of {direction}");
            AssertEq(Rm2kMoveRoute.TurnHalf(direction), (direction + 2) % 4,
                $"half turn of {direction}");
        }
        AssertEq(Rm2kMoveRoute.TurnRight(Rm2kMoveRoute.TurnRight(0)), 2,
            "two right turns from up is down");
        AssertEq(Rm2kMoveRoute.TurnHalf(Rm2kMoveRoute.TurnHalf(0)), 0,
            "two half turns from up is up again");
        AssertEq(Rm2kMoveRoute.TurnLeft(Rm2kMoveRoute.TurnRight(3)), 3,
            "a left turn undoes a right turn");
    }

    public void Test_TheSpeedAndFrequencyClampsMatchTheUpstreamBounds()
    {
        // min(GetMoveSpeed() + 1, 6) and max(GetMoveSpeed() - 1, 1), and the
        // same shape for frequency with an upper bound of 8.
        // The clamp takes the already incremented value, so a command raises
        // min(speed + 1, 6) and the clamp itself never raises anything.
        AssertEq(Rm2kMoveRoute.ClampMoveSpeed(Rm2kMoveRoute.ClampMoveSpeed(1) - 1), 1,
            "speed cannot drop below 1");
        AssertEq(Rm2kMoveRoute.ClampMoveSpeed(Rm2kMoveRoute.ClampMoveSpeed(6) + 1), 6,
            "speed cannot rise above 6");
        AssertEq(Rm2kMoveRoute.ClampMoveSpeed(5 + 1), 6, "raising from 5 saturates at 6");
        AssertEq(Rm2kMoveRoute.ClampMoveSpeed(2 - 1), 1, "lowering from 2 saturates at 1");
        AssertEq(Rm2kMoveRoute.ClampMoveSpeed(0), 1, "a zero speed is pulled up to 1");

        AssertEq(Rm2kMoveRoute.ClampMoveFrequency(Rm2kMoveRoute.ClampMoveFrequency(1) - 1), 1,
            "frequency cannot drop below 1");
        AssertEq(Rm2kMoveRoute.ClampMoveFrequency(Rm2kMoveRoute.ClampMoveFrequency(8) + 1), 8,
            "frequency cannot rise above 8");
        AssertEq(Rm2kMoveRoute.ClampMoveFrequency(7 + 1), 8, "raising from 7 saturates at 8");
    }

    public void Test_AnUnknownCommandIsReportedByNumberRatherThanGuessed()
    {
        // A move route stores no per-command length, so an unknown command
        // still consumes one slot. Naming it is more useful than skipping it.
        AssertEq(Rm2kMoveRoute.Describe(Rm2kMoveRoute.MoveDown), "MoveDown", "a known command is named");
        AssertEq(Rm2kMoveRoute.Describe(200), "0xC8", "an unknown command is reported in hex");
        AssertTrue(!Rm2kMoveRoute.IsKnownCommand(200), "and it is not claimed to be known");
        AssertTrue(!Rm2kMoveRoute.IsKnownCommand(-1), "a negative id is not known either");
        AssertTrue(Rm2kMoveRoute.IsKnownCommand(Rm2kMoveRoute.DecreaseTransp),
            "the last defined command is known");
        AssertTrue(!Rm2kMoveRoute.IsKnownCommand(Rm2kMoveRoute.DecreaseTransp + 1),
            "one past the last is not");
    }

    public void Test_TheSpeedCommandsChangeExactlyOneStepAtATime()
    {
        // The Player applies min(speed + 1, 6) and max(speed - 1, 1) in one
        // command, so a route that raises the speed twice moves two steps and
        // saturates rather than overflowing.
        var speed = 3;
        speed = Rm2kMoveRoute.ClampMoveSpeed(speed + 1);
        AssertEq(speed, 4, "raised once");
        speed = Rm2kMoveRoute.ClampMoveSpeed(speed + 1);
        speed = Rm2kMoveRoute.ClampMoveSpeed(speed + 1);
        speed = Rm2kMoveRoute.ClampMoveSpeed(speed + 1);
        AssertEq(speed, 6, "saturated at 6 rather than 7");

        // And the step budget follows, so a faster character covers the tile in
        // fewer updates.
        AssertEq(Rm2kStepBudget.UpdatesPerTile(3), 16, "speed 3 takes 16 updates");
        AssertEq(Rm2kStepBudget.UpdatesPerTile(speed), Rm2kStepBudget.UpdatesPerTile(6),
            "speed 6 takes the same eight updates the budget reports");
    }
}
