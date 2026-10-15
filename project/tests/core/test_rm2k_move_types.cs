using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The autonomous movement types of a page, and the table the engine walks.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the largest single gap the movement coverage measured:
/// 1,787 pages of the collection carry movement type 1, random</strong>, and
/// every one of them reported "not implemented" and then stood still.
/// </para>
/// <para>
/// <strong>And the rule is not invented.</strong> It is
/// <c>Game_Event::MoveTypeRandom</c> in EasyRPG Player: one draw from zero to
/// nine -- three tenths straight on, two tenths left, two tenths right, one
/// tenth around, two tenths waiting for a drawn stop count -- then the step,
/// then <c>SetMaxStopCountForRandom</c>, which is the step table times
/// <c>(Rand(0,3) + 3) / 5</c>.
/// </para>
/// <para>
/// <strong>And the draws are made predictable rather than assumed</strong>:
/// the runtime's own stream is seeded before the page is bound, and the
/// expected branch is computed from the reference's table by a second,
/// independent stream seeded the same way. A page binds on its first frame and
/// not at load, so the seed is applied before any tick.
/// </para>
/// </remarks>
public partial class TestRm2kActiveEventGraphic
{
    /// <summary>The frequency these tests author, whose step delay is eight.</summary>
    private const int MoveFrequency = 6;

    /// <summary>
    /// Binds a random page under a seed whose first action falls in a bucket.
    /// </summary>
    /// <param name="pBucketFrom">The lowest action draw to accept.</param>
    /// <param name="pBucketTo">The highest action draw to accept.</param>
    /// <returns>The draws the action was made of, and the delay before it.</returns>
    private (int Activation, int Action, int Third, int Delay) ArmRandomPage(
        int pBucketFrom, int pBucketTo)
    {
        for (var seed = 1; seed < 200000; seed++)
        {
            var probe = new Rm2kEventRandom();
            probe.Seed(seed);
            var activation = probe.NextInclusive(0, 3);
            var action = probe.NextInclusive(0, 9);
            if (action < pBucketFrom || action > pBucketTo)
            {
                continue;
            }

            StartAuthoredRouteMap(RoutePage(1, MoveFrequency, false));
            _runtime.EventRandom.Seed(seed);
            // The page binds on its first frame, which spends the activation
            // draw and not a stop count.
            for (var guard = 0; guard < 4 && _event.MaxStopCount == 0; guard++)
            {
                RouteTicks(1);
            }
            var step = Rm2kStepBudget.MaxStopCountForStep(MoveFrequency);
            AssertEq(_event.MaxStopCount, step * (activation + 3) / 5,
                "A random page's first delay is the step table times the first drawn factor");
            var delay = _event.MaxStopCount;
            var third = action >= 8
                ? probe.NextInclusive(0, delay)
                : probe.NextInclusive(0, 3);
            // The action runs on the frame after the count reaches the delay,
            // and that frame is the one that spends it.
            RouteTicks(delay + 1);
            return (activation, action, third, delay);
        }

        AssertTrue(false, $"a seed with an action draw in [{pBucketFrom}, {pBucketTo}] exists");
        return (-1, -1, -1, -1);
    }

    /// <summary>
    /// And the page that becomes active draws its own first delay.
    /// </summary>
    /// <remarks>
    /// <strong>Measured in <c>Game_Event::RefreshPage</c>:</strong> a random
    /// page takes the step table times <c>(Rand(0,3) + 3) / 5</c>, where every
    /// other type takes the table itself. At frequency six the table is eight,
    /// so the drawn delay lands between four and nine frames.
    /// </remarks>
    public void Test_RandomPageDrawsItsOwnFirstDelayWhenItBecomesActive()
    {
        StartAuthoredRouteMap(RoutePage(1, MoveFrequency, false));
        var probe = new Rm2kEventRandom();
        var activation = probe.NextInclusive(0, 3);
        for (var guard = 0; guard < 4 && _event.MaxStopCount == 0; guard++)
        {
            RouteTicks(1);
        }
        AssertEq(_event.MaxStopCount,
            Rm2kStepBudget.MaxStopCountForStep(MoveFrequency) * (activation + 3) / 5,
            "A random page's first delay is the step table times a drawn factor");
        AssertTrue(_event.MaxStopCount is >= 4 and <= 9,
            $"and at this frequency it lands between four and nine, and it is {_event.MaxStopCount}");
    }

    /// <summary>
    /// And the four moving branches step in the direction they name, counted
    /// from the direction the page starts with.
    /// </summary>
    /// <remarks>
    /// <strong>The page's authored direction is one, which is right</strong>,
    /// so straight on is a step to the right, a left turn is up, a right turn
    /// is down and a half turn is left -- and each lands on a different tile.
    /// </remarks>
    public void Test_RandomTableStepsStraightAndTurnsInTheEnginesFourDirections()
    {
        var cases = new[]
        {
            (From: 0, To: 2, Dx: 1, Dy: 0, Name: "straight on, three tenths"),
            (From: 3, To: 4, Dx: 0, Dy: -1, Name: "a left turn, two tenths"),
            (From: 5, To: 6, Dx: 0, Dy: 1, Name: "a right turn, two tenths"),
            (From: 7, To: 7, Dx: -1, Dy: 0, Name: "a half turn, one tenth"),
        };

        foreach (var item in cases)
        {
            var draws = ArmRandomPage(item.From, item.To);
            AssertTrue(draws.Action >= item.From && draws.Action <= item.To,
                $"{item.Name} was driven by draw {draws.Action}");
            AssertEq((_event.X, _event.Y), (8 + item.Dx, 8 + item.Dy),
                $"{item.Name} steps into the tile it names");
            AssertTrue(_runtime.EventRemainingStepForTest(1) >= 0,
                $"{item.Name} starts a real step and not only a turn");
            AssertEq(_event.MaxStopCount,
                Rm2kStepBudget.MaxStopCountForStep(MoveFrequency) * (draws.Third + 3) / 5,
                $"{item.Name} redraws its delay after the step, from the third draw");
        }
    }

    /// <summary>
    /// And the waiting branch moves nothing and spends a drawn stop count.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Two tenths of the draws are a wait</strong>, and the engine
    /// draws the count from zero to the page's own maximum -- so the wait can
    /// be shorter than the delay the page would otherwise have taken.
    /// </para>
    /// <para>
    /// <strong>And the count is one more than the draw</strong>, because the
    /// same frame that sets it is the frame that increments it: the reference
    /// increments a stopping character's count after its movement action.
    /// </para>
    /// </remarks>
    public void Test_RandomTableCanSpendADrawnWaitInsteadOfMoving()
    {
        // **And the wait is found by walking seeds, not by guessing which draw
        // the action used.** Two tenths of the draws wait, so a handful of
        // seeds reaches the branch -- and what gets asserted is the rule that
        // made the page wait, which is the observable part.
        for (var attempt = 0; attempt < 60; attempt++)
        {
            StartAuthoredRouteMap(RoutePage(1, MoveFrequency, false));
            _runtime.EventRandom.Seed(1000 + attempt);
            for (var guard = 0; guard < 4 && _event.MaxStopCount == 0; guard++)
            {
                RouteTicks(1);
            }
            var delay = _event.MaxStopCount;
            RouteTicks(delay + 1);
            if (_runtime.EventRemainingStepForTest(1) >= 0)
            {
                // This seed stepped instead of waiting.
                continue;
            }

            AssertEq((_event.X, _event.Y), (8, 8), "The waiting branch moves nothing");
            AssertTrue(_event.StopCount >= 1 && _event.StopCount <= delay + 1,
                "and it spends a count drawn from zero to the page's maximum,"
                    + " incremented by the same frame -- and it is "
                    + _event.StopCount + " against a maximum of " + delay);
            return;
        }

        AssertTrue(false,
            "one of sixty seeds made the page wait, as two tenths of the draws should");
    }

    /// <summary>
    /// And a refused step puts the pose back, which is the engine's own choice
    /// while the stop count is short of the sixty-frame grace.
    /// </summary>
    /// <remarks>
    /// <strong>Measured in <c>MoveTypeRandom</c>:</strong> after a refused move
    /// the engine clears the stop count when the page waits for a foreground
    /// interpreter or when the count has passed the maximum by sixty, and
    /// otherwise it restores the direction and -- unless the facing is locked --
    /// the facing.
    /// </remarks>
    public void Test_RefusedRandomStepRestoresThePose()
    {
        StartAuthoredRouteMap(RoutePage(1, MoveFrequency, false));
        // The tile to the right is walled off on both of its edges, so the
        // straight branch cannot take it.
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] = 0;
        _runtime.Simulation.PassabilityMasks[9 + 8 * 20] = 0;
        var (seed, _) = FindSeed(0, 2);
        AssertTrue(seed > 0, "a seed with a straight action draw exists");
        _runtime.EventRandom.Seed(seed);
        for (var guard = 0; guard < 4 && _event.MaxStopCount == 0; guard++)
        {
            RouteTicks(1);
        }
        var directionBefore = _event.Direction;
        var facingBefore = _event.FacingDirection;
        RouteTicks(_event.MaxStopCount + 1);

        AssertEq((_event.X, _event.Y), (8, 8), "the walled page does not move");
        AssertEq(_event.Direction, directionBefore,
            "and a refusal short of the sixty-frame grace restores the direction");
        AssertEq(_event.FacingDirection, facingBefore, "and the facing with it");
        AssertTrue(_runtime.EventRemainingStepForTest(1) < 0, "and no step is in flight");
    }

    /// <summary>
    /// And a random page is no longer reported as unimplemented, and it really
    /// leaves its tile over a run.
    /// </summary>
    public void Test_RandomPageWalksAndIsNoLongerReportedUnimplemented()
    {
        StartAuthoredRouteMap(RoutePage(1, MoveFrequency, false));
        RouteTicks(400);
        AssertTrue(!_runtime.Simulation.Diagnostics.Any(pLine =>
                pLine.Contains("movement type 1 is not implemented")),
            "A random page is implemented and says so by not complaining");
        AssertTrue(_event.X != 8 || _event.Y != 8,
            $"and over four hundred frames it left its tile, and it is at ({_event.X}, {_event.Y})");
    }

    /// <summary>Finds a seed whose first action draw falls in a bucket.</summary>
    /// <param name="pFrom">The lowest draw in the bucket.</param>
    /// <param name="pTo">The highest draw in the bucket.</param>
    /// <returns>The seed and the draw it produces.</returns>
    private static (int Seed, int Draw) FindSeed(int pFrom, int pTo)
    {
        for (var seed = 1; seed < 200000; seed++)
        {
            var probe = new Rm2kEventRandom();
            probe.Seed(seed);
            probe.NextInclusive(0, 3);
            var draw = probe.NextInclusive(0, 9);
            if (draw >= pFrom && draw <= pTo)
            {
                return (seed, draw);
            }
        }
        return (-1, -1);
    }

    /// <summary>
    /// Starts an authored map of a chosen size with its event on a chosen tile.
    /// </summary>
    /// <param name="pWidth">The map width in tiles.</param>
    /// <param name="pHeight">The map height in tiles.</param>
    /// <param name="pEventX">The event's tile column.</param>
    /// <param name="pEventY">The event's tile row.</param>
    /// <param name="pages">The event's pages.</param>
    private void StartSizedRouteMap(
        int pWidth, int pHeight, int pEventX, int pEventY, params byte[][] pages)
    {
        _host.Dispose();
        File.WriteAllBytes(Path.Combine(_root, "Map0001.lmu"),
            TestRm2kEventPagePoseParser.MapWithPages(pWidth, pHeight, pEventX, pEventY, pages));
        _host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = _host.Start(new PluginGameInfo
        {
            GameDirectory = _root,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        });
        AssertTrue(started.Success, started.Error?.Message ?? "The authored map starts");
        _runtime = (Rm2kEngineRuntime)_host.Runtime!;
        _event = ((List<Rm2kMap.Event>)typeof(Rm2kEngineRuntime)
            .GetField("_mapEvents", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_runtime)!).Single();
        for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
        {
            _runtime.Simulation.PassabilityMasks[index] = 15;
        }
    }

    /// <summary>The walkable distance from the event to the hero, in tiles.</summary>
    /// <returns>The Manhattan distance.</returns>
    private int DistanceToHero() =>
        Math.Abs(_event.X - _runtime.Simulation.MapX)
            + Math.Abs(_event.Y - _runtime.Simulation.MapY);

    /// <summary>
    /// And a vertical cycle walks its line, and turns round at a wall.
    /// </summary>
    /// <remarks>
    /// <strong>Measured in <c>Game_Event::MoveTypeCycle</c>:</strong> the page
    /// takes the default direction -- down for the vertical cycle -- and keeps
    /// the reverse only once it is already walking it, which is how it turns
    /// round. A wall is answered by a second attempt the other way, once the
    /// stop count has passed the maximum by twenty.
    /// </remarks>
    public void Test_VerticalCycleWalksItsLineAndTurnsRoundAtAWall()
    {
        StartAuthoredRouteMap(RoutePage(2, MoveFrequency, false));
        RouteTicks(60);
        AssertEq(_event.X, 8, "A vertical cycle never leaves its column");
        AssertTrue(_event.Y > 8,
            $"and it walks down while the way is open, and it is at {_event.Y}");
        AssertTrue(!_runtime.Simulation.Diagnostics.Any(pLine =>
                pLine.Contains("movement type 2 is not implemented")),
            "and it is implemented, so it does not complain");

        // The way down is walled off: no down exit on the event's tile, no up
        // entry on the one below it.
        StartAuthoredRouteMap(RoutePage(2, MoveFrequency, false));
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] &= unchecked((byte)~1);
        _runtime.Simulation.PassabilityMasks[8 + 9 * 20] &= unchecked((byte)~8);
        RouteTicks(60);
        AssertEq(_event.X, 8, "and a walled column still holds its column");
        AssertTrue(_event.Y < 8,
            $"and the second attempt walks the other way, and it is at {_event.Y}");
    }

    /// <summary>
    /// And a horizontal cycle walks its line the same way.
    /// </summary>
    public void Test_HorizontalCycleWalksItsLineAndTurnsRoundAtAWall()
    {
        StartAuthoredRouteMap(RoutePage(3, MoveFrequency, false));
        RouteTicks(60);
        AssertEq(_event.Y, 8, "A horizontal cycle never leaves its row");
        AssertTrue(_event.X > 8,
            $"and it walks right while the way is open, and it is at {_event.X}");

        StartAuthoredRouteMap(RoutePage(3, MoveFrequency, false));
        _runtime.Simulation.PassabilityMasks[8 + 8 * 20] &= unchecked((byte)~4);
        _runtime.Simulation.PassabilityMasks[9 + 8 * 20] &= unchecked((byte)~2);
        RouteTicks(60);
        AssertEq(_event.Y, 8, "and a walled row still holds its row");
        AssertTrue(_event.X < 8,
            $"and the second attempt walks the other way, and it is at {_event.X}");
    }

    /// <summary>
    /// And the player-seeking types walk closer, or farther, over a run of pure
    /// player-rule draws.
    /// </summary>
    /// <remarks>
    /// <strong>And the distance is the assertion, not the direction</strong>,
    /// because the rule depends on where the hero happens to stand: what the
    /// engine promises is that a towards page closes the gap and an away page
    /// opens it. The seeds are chosen so every one of the first four draws takes
    /// the player rule and none of them keeps a direction or draws at random.
    /// </remarks>
    public void Test_TowardsPlayerWalksCloserAndAwayWalksFarther()
    {
        foreach (var (mode, closer) in new[] { (4, true), (5, false) })
        {
            StartAuthoredRouteMap(RoutePage(mode, MoveFrequency, false));
            var seed = SeedWithActionDrawsAbove(4, 2);
            AssertTrue(seed > 0,
                "a seed whose first four draws all take the player rule exists");
            _runtime.EventRandom.Seed(seed);
            var before = DistanceToHero();
            Console.WriteLine($"mode {mode}: parsed page MoveType={_event.Pages[0].MoveType},"
                + $" hero at ({_runtime.Simulation.MapX}, {_runtime.Simulation.MapY}),"
                + $" event at ({_event.X}, {_event.Y}), distance {before}");
            RouteTicks(40);
            var after = DistanceToHero();
            Console.WriteLine($"mode {mode}: event at ({_event.X}, {_event.Y}),"
                + $" distance {after}, direction {_event.Direction}");
            if (closer)
            {
                AssertTrue(after < before,
                    $"mode {mode} closes the gap: {before} tiles become {after}");
            }
            else
            {
                AssertTrue(after > before,
                    $"mode {mode} opens it: {before} tiles become {after}");
            }
            AssertTrue(!_runtime.Simulation.Diagnostics.Any(pLine =>
                    pLine.Contains($"movement type {mode} is not implemented")),
                $"and mode {mode} is implemented, so it does not complain");
        }
    }

    /// <summary>
    /// And a player-seeking page that is off the screen takes a random
    /// direction instead of looking for the hero.
    /// </summary>
    /// <remarks>
    /// <strong>Measured in <c>MoveTypeTowardsOrAwayPlayer</c>:</strong> the
    /// engine first asks whether the event lies within the screen plus a margin
    /// of two tiles. This map is bigger than one screen on purpose: at twenty by
    /// fifteen every event is in sight, and the branch would be unreachable.
    /// The first draw of the rule is the direction, so a seed whose first draw
    /// is zero walks up -- while the player rule would have walked left, which
    /// is what the second assertion rules out.
    /// </remarks>
    public void Test_PlayerSeekingPageOutOfSightTakesARandomDirection()
    {
        StartSizedRouteMap(60, 40, 30, 20, RoutePage(4, MoveFrequency, false));
        AssertTrue(_event.X * Rm2kMapCamera.TileSize > 320 + 64,
            "the event stands beyond the screen's margin");
        AssertTrue(_runtime.Simulation.MapX * Rm2kMapCamera.TileSize < 320,
            "and the camera cannot follow it, because the hero is on the first screen");

        var seed = SeedWithFirstDraw(0);
        AssertTrue(seed > 0, "a seed whose first draw is zero exists");
        _runtime.EventRandom.Seed(seed);
        for (var guard = 0; guard < 4 && _event.MaxStopCount == 0; guard++)
        {
            RouteTicks(1);
        }
        var y = _event.Y;
        RouteTicks(_event.MaxStopCount + 1);
        AssertEq(_event.Y, y - 1, "and the out-of-sight draw of zero walks up");
        AssertEq(_event.X, 30,
            "and not the direction the player rule would have taken");
    }

    /// <summary>Finds a seed whose first draw from zero to three is a value.</summary>
    /// <param name="pWanted">The draw to look for.</param>
    /// <returns>The seed.</returns>
    private static int SeedWithFirstDraw(int pWanted)
    {
        for (var seed = 1; seed < 200000; seed++)
        {
            var probe = new Rm2kEventRandom();
            probe.Seed(seed);
            if (probe.NextInclusive(0, 3) == pWanted)
            {
                return seed;
            }
        }
        return -1;
    }

    /// <summary>
    /// Finds a seed whose first draws all stay above a bound.
    /// </summary>
    /// <param name="pCount">How many draws to check.</param>
    /// <param name="pMinimum">The lowest draw that is still the player rule.</param>
    /// <returns>The seed.</returns>
    private static int SeedWithActionDrawsAbove(int pCount, int pMinimum)
    {
        for (var seed = 1; seed < 500000; seed++)
        {
            var probe = new Rm2kEventRandom();
            probe.Seed(seed);
            var fits = true;
            for (var draw = 0; draw < pCount && fits; draw++)
            {
                fits = probe.NextInclusive(0, 9) >= pMinimum;
            }
            if (fits)
            {
                return seed;
            }
        }
        return -1;
    }
}
