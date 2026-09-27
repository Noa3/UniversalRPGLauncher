using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the RPG Maker 2000 character walk animation against
/// <c>Game_Character::UpdateAnim</c> and <c>IncAnimFrame</c>. The thresholds
/// and the modulo rotation are transcribed from the upstream source, and the
/// tests check the transitions rather than only the end state, because the
/// difference between a three frame and a four frame cycle shows up in how
/// long a step takes, not in the final pose.
/// </summary>
public partial class TestRm2kCharacterAnimation : TestBase
{
    public void Test_TheSpeedTablesMatchTheUpstreamValues()
    {
        AssertEq(Rm2kCharacterAnimation.StationaryAnimFrames(1), 12, "stationary speed 1");
        AssertEq(Rm2kCharacterAnimation.StationaryAnimFrames(2), 10, "stationary speed 2");
        AssertEq(Rm2kCharacterAnimation.StationaryAnimFrames(3), 8, "stationary speed 3");
        AssertEq(Rm2kCharacterAnimation.StationaryAnimFrames(4), 6, "stationary speed 4");
        AssertEq(Rm2kCharacterAnimation.StationaryAnimFrames(5), 5, "stationary speed 5");
        AssertEq(Rm2kCharacterAnimation.StationaryAnimFrames(6), 4, "stationary speed 6");

        AssertEq(Rm2kCharacterAnimation.ContinuousAnimFrames(1), 16, "continuous speed 1");
        AssertEq(Rm2kCharacterAnimation.ContinuousAnimFrames(2), 12, "continuous speed 2");
        AssertEq(Rm2kCharacterAnimation.ContinuousAnimFrames(3), 10, "continuous speed 3");
        AssertEq(Rm2kCharacterAnimation.ContinuousAnimFrames(4), 8, "continuous speed 4");
        AssertEq(Rm2kCharacterAnimation.ContinuousAnimFrames(5), 7, "continuous speed 5");
        AssertEq(Rm2kCharacterAnimation.ContinuousAnimFrames(6), 6, "continuous speed 6");

        AssertEq(Rm2kCharacterAnimation.SpinAnimFrames(1), 24, "spin speed 1");
        AssertEq(Rm2kCharacterAnimation.SpinAnimFrames(6), 4, "spin speed 6");
    }

    public void Test_AStoppedCharacterAdvancesAfterTheStationaryLimit()
    {
        // The upstream guard increments the count when the count is below
        // stationary_limit - 1, so the count reaches stationary_limit - 1 and
        // only advances when stop_count is zero. A character standing with a
        // non zero stop count therefore never advances.
        // Speed 3 has a stationary limit of 8 and a continuous limit of 10. With
        // stop_count zero both guards can fire, and the stationary one is
        // reached first: the count hits 8 on the eighth tick and the frame
        // advances there, not on the ninth.
        var frame = Rm2kCharacterAnimation.FrameMiddle;
        var count = 0;
        for (var tick = 0; tick < 7; tick++)
        {
            (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 0, 3);
        }
        AssertEq(frame, Rm2kCharacterAnimation.FrameMiddle, "after seven ticks the frame has not moved");
        AssertEq(count, 7, "the count is one below the stationary limit");

        (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 0, 3);
        AssertEq(frame, Rm2kCharacterAnimation.FrameRight, "the eighth tick advances the frame");
        AssertEq(count, 0, "the count resets when the frame advances");
    }

    public void Test_ACharacterWithANonZeroStopCountDoesNotAdvanceWhileStationary()
    {
        var frame = Rm2kCharacterAnimation.FrameMiddle;
        var count = 0;
        for (var tick = 0; tick < 60; tick++)
        {
            (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 3, 3);
        }
        AssertEq(frame, Rm2kCharacterAnimation.FrameMiddle, "the frame never advances");
        AssertEq(count, 7, "the count saturates at stationary_limit - 1, which is 7 at speed 3");
    }

    public void Test_AnEventWithNoAnimationNeverWalks()
    {
        // AnimType_non_continuous_graphic events hold a fixed pose. They must
        // not animate, or every decorative event in a game would walk.
        var frame = Rm2kCharacterAnimation.FrameMiddle;
        var count = 0;
        for (var tick = 0; tick < 30; tick++)
        {
            (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 0, 3, pAnimated: false);
        }
        AssertEq(frame, Rm2kCharacterAnimation.FrameMiddle, "a non animated event keeps its frame");
        AssertEq(count, 0, "a non animated event does not count");
    }

    public void Test_APausedOrJumpingCharacterResetsItsCount()
    {
        var stepped = Rm2kCharacterAnimation.Update(
            Rm2kCharacterAnimation.FrameLeft, 5, 0, 3);
        AssertEq(stepped.Count, 6, "the count advanced");

        var paused = Rm2kCharacterAnimation.Update(
            stepped.Frame, stepped.Count, 0, 3, pPaused: true);
        AssertEq(paused.Count, 0, "a paused character resets its count");
        AssertEq(paused.Frame, Rm2kCharacterAnimation.FrameLeft, "a paused character keeps its frame");
    }

    public void Test_TheFrameRotatesOverFourValuesAndTheFourthDrawsAsTheMiddle()
    {
        // A character cell has three columns, so a four value rotation is only
        // correct because Frame_middle2 is drawn as Frame_middle. Cycling over
        // three instead would change the step rate.
        var seen = new System.Collections.Generic.List<int>();
        var frame = Rm2kCharacterAnimation.FrameLeft;
        var count = 0;
        for (var step = 0; step < 8; step++)
        {
            // One step at the default speed 3 is eight ticks, so stepping
            // eight ticks at a time makes the rotation the only thing observed.
            for (var tick = 0; tick < 8; tick++)
            {
                (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 0, 3);
            }
            seen.Add(frame);
        }
        AssertEq(seen.Count, 8, "eight steps produced eight frames");
        AssertEq(seen[0], 1, "the first advance is the middle frame");
        AssertEq(seen[1], 2, "the second is right");
        AssertEq(seen[2], 3, "the third is the fourth rotation value");
        AssertEq(seen[3], 0, "the fourth wraps to left");
        AssertEq(seen[4], 1, "the rotation repeats");

        // And the fourth value is what the sprite clamps to the middle frame.
        AssertEq(Rm2kCharacterAnimation.FrameMiddle2, 3, "Frame_middle2 is the fourth value");
        AssertEq(Rm2kCharset.ClampFrame(Rm2kCharacterAnimation.FrameMiddle2),
            Rm2kCharacterAnimation.FrameMiddle, "and it is drawn as the middle frame");
    }

    public void Test_AFasterSpeedAnimatesInFewerTicks()
    {
        // Speed 6 has a continuous limit of 6 and speed 1 of 16, so the same
        // step must take fewer ticks at the faster speed. This is the property
        // that makes the tables observable at all.
        var ticks = 0;
        var frame = Rm2kCharacterAnimation.FrameLeft;
        var count = 0;
        while (frame == Rm2kCharacterAnimation.FrameLeft && ticks < 200)
        {
            (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 0, 6, pContinuous: true);
            ticks++;
        }
        var fastTicks = ticks;

        ticks = 0;
        frame = Rm2kCharacterAnimation.FrameLeft;
        count = 0;
        while (frame == Rm2kCharacterAnimation.FrameLeft && ticks < 200)
        {
            (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 0, 1, pContinuous: true);
            ticks++;
        }

        AssertTrue(fastTicks < ticks,
            $"speed 6 must advance in fewer ticks than speed 1, but got {fastTicks} and {ticks}");
    }

    public void Test_TheEventFacingRowIsTheFacingTheRuntimeStores()
    {
        // liblcf stores 1 up, 2 down, 3 left, 4 right. The charset row is what
        // the sprite actually reads, so the whole conversion has to hold for
        // every direction or a character faces the wrong way. Converting twice
        // or mixing the two encodings silently draws the wrong pose, which is
        // why both halves are checked.
        // FacingFromLiblcfDirection turns the liblcf value into the direction
        // this project stores, and FacingToRow turns that into the charset row.
        // Both steps together must land on the same row the Player draws.
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(0), 8, "liblcf 0 up is stored as 8");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(1), 6, "liblcf 1 right is stored as 6");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(2), 2, "liblcf 2 down is stored as 2");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(3), 4, "liblcf 3 left is stored as 4");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(9), 8, "a corrupt direction falls back to up");

        // The mapping has to be a permutation. A one based axis maps four liblcf
        // directions onto only three stored facings, which is not detectable
        // from a few spot checks, so the whole image of the function is pinned.
        var seen = new System.Collections.Generic.HashSet<int>();
        for (var liblcf = 0; liblcf < 4; liblcf++)
        {
            AssertTrue(seen.Add(Rm2kCharacterSprite.FacingFromLiblcfDirection(liblcf)),
                $"liblcf {liblcf} must map to a facing no other direction uses");
        }
        AssertEq(seen.Count, 4, "all four liblcf directions stay distinct");
        // Both 0 and anything out of range have to land on up. Dropping the
        // explicit zero arm happens to keep working because the default catches
        // it, and the permutation checks still pass, so the value is pinned
        // here: a change to the fallback must not silently move liblcf 0.
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(0), 8,
            "liblcf 0 is up, not the fallback by accident");
        // And each of them must reach a different charset row, or two
        // directions draw the same pose.
        var rows = new System.Collections.Generic.HashSet<int>();
        for (var liblcf = 0; liblcf < 4; liblcf++)
        {
            rows.Add(Rm2kCharset.FacingToRow(Rm2kCharacterSprite.FacingFromLiblcfDirection(liblcf)));
        }
        AssertEq(rows.Count, 4, "all four liblcf directions draw different rows");

        AssertEq(Rm2kCharset.FacingToRow(8), Rm2kCharset.DirectionUp, "stored 8 is the up row");
        AssertEq(Rm2kCharset.FacingToRow(2), Rm2kCharset.DirectionDown, "stored 2 is the down row");
        AssertEq(Rm2kCharset.FacingToRow(4), Rm2kCharset.DirectionLeft, "stored 4 is the left row");
        AssertEq(Rm2kCharset.FacingToRow(6), Rm2kCharset.DirectionRight, "stored 6 is the right row");
    }

    public void Test_ASpeedOutsideTheDefinedRangeIsRefused()
    {
        // The upstream tables are indexed unchecked, so an out of range speed
        // is corrupt data and must not be silently clamped.
        foreach (var speed in new[] { 0, -1, 7, 99 })
        {
            var error = "";
            try
            {
                Rm2kCharacterAnimation.StationaryAnimFrames(speed);
            }
            catch (Rm2kAnimationDataException exception)
            {
                error = exception.Message;
            }
            AssertTrue(error.Contains("outside the defined range", System.StringComparison.OrdinalIgnoreCase),
                $"speed {speed} must be refused, but the reader said: {error}");
        }
    }

    public void Test_MovementSpeedThreeGivesTheDocumentedStepLength()
    {
        // The default move speed in an RPG Maker 2000 database is 3, so this
        // is the rate an unmodified game uses. It is pinned here because the
        // whole walk animation hangs off it.
        var frame = Rm2kCharacterAnimation.FrameMiddle;
        var count = 0;
        var advances = 0;
        var previous = frame;
        for (var tick = 0; tick < 100; tick++)
        {
            (frame, count) = Rm2kCharacterAnimation.Update(frame, count, 0, 3);
            if (frame != previous)
            {
                advances++;
                previous = frame;
            }
        }
        // 100 ticks at the default speed 3 advance the frame 12 times. The
        // value is pinned because the step rate is what a player sees, and it
        // is the thing a three frame cycle would get wrong.
        AssertEq(advances, 12, "100 ticks at the default speed 3 advance the frame 12 times");
    }
}
