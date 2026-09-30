using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The clock that moves a figure's pattern, measured against the
/// finished game's own engine.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every number here was read out of
/// <c>CamelliaCoronation-Win/js/rmmz_objects.js</c>.</strong> The test
/// does not assert what seems right; <strong>it asserts what the game
/// does.</strong>
/// </para>
/// </remarks>
public partial class TestMzWalkClock : TestBase
{
    /// <summary>
    /// The wait between steps is the engine's own formula.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the formula is <c>(9 − realMoveSpeed) × 3</c>.</strong>
    /// Measured: <c>Game_CharacterBase.prototype.animationWait</c>
    /// returns <c>(9 - this.realMoveSpeed()) * 3</c>.
    /// </para>
    /// <para>
    /// <strong>And it runs backwards.</strong> Speed 1 waits twenty-four
    /// steps and speed 6 waits nine — <strong>and a reader that used the
    /// speed itself as a frame count made the slowest figures walk
    /// fastest</strong>, <strong>which is the opposite of what a reader
    /// would guess from the name.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieWartezeitIstDieFormelDesMotors()
    {
        AssertEq((int)new MzWalkClock { MoveSpeed = 1 }.AnimationWait(), 24,
            "**and speed 1 waits twenty-four steps**");
        AssertEq((int)new MzWalkClock { MoveSpeed = 4 }.AnimationWait(), 15,
            "**and speed 4, which is the engine's own default, waits "
            + "fifteen**");
        AssertEq((int)new MzWalkClock { MoveSpeed = 6 }.AnimationWait(), 9,
            "**and speed 6 waits nine**");

        // **Und der gemessene Satz des Projekts ist 5 und 3.**
        AssertEq((int)new MzWalkClock { MoveSpeed = 5 }.AnimationWait(), 12,
            "**and this project's measured speed of 5 waits twelve**");
    }

    /// <summary>
    /// Walking ticks twice as fast as standing still.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And both rates are the engine's.</strong> Measured:
    /// <c>updateAnimationCount</c> adds <c>1.5</c> while
    /// <c>isMoving() &amp;&amp; hasWalkAnime()</c>, and <c>1</c> otherwise.
    /// </para>
    /// <para>
    /// <strong>And a standing figure in its first pattern does not tick
    /// at all</strong> — <strong>the engine's test is
    /// <c>!isOriginalPattern()</c></strong> — <strong>and that is what
    /// holds a still pose still.</strong> <strong>A clock that ticked at
    /// 1.5 unconditionally made a room of standing people shuffle.</strong>
    /// </para>
    /// </remarks>
    public void Test_GehenTicktDoppeltSoSchnellWieStehen()
    {
        // **Und eine stehende Figur im ersten Muster ruht.**
        var ruht = new MzWalkClock { MoveSpeed = 4 };
        var wechselte = false;
        for (var frame = 0; frame < 400; frame++)
        {
            wechselte |= ruht.Tick();
        }

        AssertTrue(!wechselte,
            "**and a standing figure in its first pattern never changes**"
                + " -- and the engine's test is !isOriginalPattern(), and a "
                + "clock that ticked anyway made a room of standing "
                + "people shuffle");

        // **Und eine gehende Figur schafft den Wechsel in halb so
        // vielen Bildern.**
        var gehend = new MzWalkClock { MoveSpeed = 4, Moving = true };
        var wechsel = 0;
        for (var frame = 0; frame < 10; frame++)
        {
            if (gehend.Tick())
            {
                wechsel++;
            }
        }

        AssertTrue(wechsel >= 1,
            "**and a walking figure changes within ten frames** -- and it "
                + "takes fifteen steps at speed 4 at one and a half a "
                + "frame, and the engine's own arithmetic gives ten");
    }

    /// <summary>
    /// The engine counts four patterns and draws three columns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured, and it is the sort of thing a reader
    /// gets wrong twice.</strong> The engine's <c>maxPattern()</c> is
    /// <c>4</c>, and <c>pattern()</c> returns <c>_pattern &lt; 3 ?
    /// _pattern : 1</c> — <strong>so the fourth pattern is drawn as the
    /// first</strong>.
    /// </para>
    /// <para>
    /// <strong>And the sheet has three step columns, not four</strong> —
    /// measured <c>576 × 384</c> with <c>144 × 192</c> cells, a
    /// <c>4 × 2</c> grid, of which only the first row carries
    /// anything — <strong>and a reader that drew four columns walked into
    /// the second direction.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerMotorZaehltVierUndZeichnetDrei()
    {
        var uhr = new MzWalkClock { MoveSpeed = 4, Moving = true };
        var spalten = new List<int>();
        for (var frame = 0; frame < 200; frame++)
        {
            uhr.Tick();
            spalten.Add(uhr.Column);
        }

        AssertEq(uhr.Pattern < MzWalkClock.MaxPattern, true,
            "**and the pattern stays under the engine's four**");
        AssertTrue(spalten.Contains(2),
            "**and the second column is reached**");
        AssertTrue(!spalten.Contains(3),
            "**and the fourth pattern is never drawn as a fourth column**"
                + " -- and the engine draws pattern 3 as pattern 1, and the "
                + "sheet has three step columns, and a reader that drew "
                + "four walked into the second direction");
    }

    /// <summary>
    /// Sixteen is a turn and not a move.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the finding that cost the most to get
    /// right.</strong> Measured in the engine: <c>ROUTE_MOVE_DOWN</c> is
    /// <c>1</c>, and <c>ROUTE_TURN_DOWN</c> is <c>16</c>.
    /// <strong>Sixteen is not a direction and it is not a step — it turns
    /// a figure.</strong>
    /// </para>
    /// <para>
    /// <strong>And this project uses only turns.</strong> Seven pages
    /// carry <c>16, 18, 17, 19</c> and nothing else, all with
    /// <c>moveType: 0</c> — <strong>which means "fixed"</strong>, and the
    /// route is never walked at all. <strong>A runtime that read 16 as a
    /// move walked seven figures off their tiles; a runtime that
    /// ignored <c>moveType</c> walked 253.</strong>
    /// </para>
    /// </remarks>
    public void Test_SechzehnIstEineDrehungUndKeinSchritt()
    {
        AssertEq(MzRouteCode.TurnDown, 16,
            "**and 16 turns a figure down** -- and ROUTE_TURN_DOWN is 16 in "
                + "the engine's own source");
        AssertEq(MzRouteCode.MoveDown, 1,
            "**and moving down is 1, and not 16** -- and a reader that read "
                + "16 as a direction turned a figure sixteen degrees, and "
                + "one that read it as \"down\" because it is the first of "
                + "the four walked a figure off its tile");

        AssertTrue(MzRouteCode.IsTurn(16) && !MzRouteCode.IsMove(16),
            "**and sixteen is a turn and not a move**");
        AssertTrue(MzRouteCode.IsMove(1) && !MzRouteCode.IsTurn(1),
            "**and one is a move and not a turn**");

        // **Und die vier Drehungen kommen auf die vier Richtungen.**
        var uhr = new MzWalkClock();
        AssertTrue(MzWalkClock.Turn(uhr, MzRouteCode.TurnDown), "down");
        AssertEq(uhr.Direction, MzCharacter.Down, "**and 16 faces down**");
        AssertTrue(MzWalkClock.Turn(uhr, MzRouteCode.TurnLeft), "left");
        AssertEq(uhr.Direction, MzCharacter.Left, "**and 17 faces left**");
        AssertTrue(MzWalkClock.Turn(uhr, MzRouteCode.TurnRight), "right");
        AssertEq(uhr.Direction, MzCharacter.Right, "**and 18 faces right**");
        AssertTrue(MzWalkClock.Turn(uhr, MzRouteCode.TurnUp), "up");
        AssertEq(uhr.Direction, MzCharacter.Up, "**and 19 faces up**");
        AssertTrue(!MzWalkClock.Turn(uhr, MzRouteCode.Wait),
            "**and fifteen waits and does not turn**");
    }
}