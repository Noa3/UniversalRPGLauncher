using System;

namespace UniversalRPG.Web;

/// <summary>
/// The clock that moves a figure's walking pattern along.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every formula here was read out of the finished game's own
/// engine, and not invented.</strong> Measured in
/// <c>CamelliaCoronation-Win/js/rmmz_objects.js</c>:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <c>animationWait</c> is <c>(9 − realMoveSpeed) × 3</c>, and
/// <c>realMoveSpeed</c> is <c>moveSpeed + (isDashing ? 1 : 0)</c>.
/// </description></item>
/// <item><description>
/// <c>updateAnimationCount</c> adds <c>1.5</c> while the figure moves and
/// walks, and <c>1</c> while it stands still or is not in its first
/// pattern.
/// </description></item>
/// <item><description>
/// <c>updatePattern</c> runs when the counter has reached that wait, and
/// then sets the counter back to zero and adds one to the pattern.
/// </description></item>
/// <item><description>
/// <c>maxPattern</c> is <c>4</c>, and <c>pattern()</c> returns
/// <c>_pattern &lt; 3 ? _pattern : 1</c> — <strong>so the fourth pattern
/// is drawn as the first</strong>, <strong>and the sheet needs only three
/// step columns.</strong>
/// </description></item>
/// </list>
/// <para>
/// <strong>And a standing figure whose pattern is not the first keeps
/// counting at one per frame</strong>, <strong>which is how a figure
/// stops mid-stride and stays there</strong> — <strong>and a reader that
/// counted at 1.5 unconditionally made a room of standing people
/// shuffle.</strong>
/// </para>
/// </remarks>
public sealed class MzWalkClock
{
    /// <summary>
    /// How fast the figure moves: 1 is slowest, 6 is the default.
    /// </summary>
    /// <remarks>
    /// <strong>And the default is measured.</strong> The engine's own
    /// <c>initMembers</c> sets <c>_moveSpeed = 4</c> and
    /// <c>_moveFrequency = 6</c>, and every page of this project that
    /// does not say otherwise carries <c>moveSpeed: 5</c> and
    /// <c>moveFrequency: 3</c>.
    /// </remarks>
    public int MoveSpeed { get; set; } = 4;

    /// <summary>How fast the pattern advances: 1 is slow, 5 is fastest.</summary>
    /// <remarks>
    /// <strong>And the measured project uses 3 on every page that says
    /// anything</strong>, and 6 by the engine's own default.
    /// </remarks>
    public int MoveFrequency { get; set; } = 6;

    /// <summary>Which of the three step columns is shown.</summary>
    /// <remarks>
    /// <strong>And this is the engine's number, not a column.</strong> It
    /// runs 0, 1, 2, 3, and <strong>the engine draws 3 as 1</strong> — see
    /// <see cref="Column"/>.
    /// </remarks>
    public int Pattern { get; set; } = 1;

    /// <summary>The counter, in halves of a step.</summary>
    public double Count { get; private set; }

    /// <summary>Whether the figure is walking right now.</summary>
    public bool Moving { get; set; }

    /// <summary>Whether the figure shows a walk at all.</summary>
    public bool WalkAnime { get; set; } = true;

    /// <summary>Whether the figure shows a step when it stands.</summary>
    public bool StepAnime { get; set; }

    /// <summary>
    /// Which of the sheet's three step columns to draw.
    /// </summary>
    /// <remarks>
    /// <strong>And this is where the engine's four patterns become the
    /// sheet's three columns.</strong> The engine counts 0, 1, 2, 3 and
    /// draws pattern 3 as pattern 1, <strong>and a reader that drew four
    /// columns walked into the second direction</strong> — <strong>and
    /// one that drew three columns from pattern 3 drew the wrong
    /// direction's stance.</strong>
    /// </remarks>
    public int Column
    {
        get => Pattern < 3 ? Pattern : 1;
    }

    /// <summary>How many patterns the engine counts before it starts over.</summary>
    public const int MaxPattern = 4;

    /// <summary>
    /// How many steps the engine takes at this speed.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the engine's own formula, verbatim.</strong>
    /// Speed 1 waits twenty-four steps, speed 4 waits fifteen, speed 6
    /// waits nine — <strong>and a reader that used the speed directly as
    /// a frame count walked fast figures at the pace of slow ones.</strong>
    /// </remarks>
    public double AnimationWait()
    {
        var tempo = MoveSpeed + 0;
        var wert = (9 - tempo) * 3;
        return wert <= 0 ? 3 : wert;
    }

    /// <summary>
    /// Advances the clock by one frame.
    /// </summary>
    /// <returns>Whether the pattern changed.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the two rates are the engine's, and not one rate.</strong>
    /// Moving and walking adds one and a half, and standing still adds
    /// one — <strong>so a figure that stops keeps ticking, and a figure
    /// that walks ticks twice as fast.</strong>
    /// </para>
    /// <para>
    /// <strong>And a standing figure in its first pattern does not tick
    /// at all</strong>, unless it has a step animation —
    /// <c>!isOriginalPattern()</c> is the engine's own test — <strong>and
    /// that is what holds a still pose still.</strong>
    /// </para>
    /// </remarks>
    public bool Tick()
    {
        if (Moving && WalkAnime)
        {
            Count += 1.5;
        }
        else if (StepAnime || Pattern != 1)
        {
            Count += 1;
        }
        else
        {
            return false;
        }

        if (Count < AnimationWait())
        {
            return false;
        }

        Pattern = (Pattern + 1) % MaxPattern;
        Count = 0;
        return true;
    }

    /// <summary>
    /// Turns the figure to face a way.
    /// </summary>
    /// <param name="pDirection">2, 4, 6 or 8.</param>
    /// <returns>Whether the turn was one the engine has.</returns>
    /// <remarks>
    /// <strong>And the route codes are not the direction numbers.</strong>
    /// Measured: <c>ROUTE_TURN_DOWN</c> is 16, <c>ROUTE_TURN_LEFT</c> is
    /// 17, <c>ROUTE_TURN_RIGHT</c> is 18, <c>ROUTE_TURN_UP</c> is 19 —
    /// <strong>and <c>ROUTE_MOVE_DOWN</c> is 1, not 16</strong>. <strong>A
    /// reader that read 16 as a direction turned a figure 16
    /// degrees</strong>, <strong>and one that read it as "down" because
    /// it is the first of the four turned the figure the wrong
    /// way.</strong>
    /// </remarks>
    public static bool Turn(MzWalkClock pClock, int pRouteCode)
    {
        if (pClock == null)
        {
            return false;
        }

        switch (pRouteCode)
        {
            case MzRouteCode.TurnDown:
                pClock.Direction = MzCharacter.Down;
                return true;
            case MzRouteCode.TurnLeft:
                pClock.Direction = MzCharacter.Left;
                return true;
            case MzRouteCode.TurnRight:
                pClock.Direction = MzCharacter.Right;
                return true;
            case MzRouteCode.TurnUp:
                pClock.Direction = MzCharacter.Up;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Which way the figure faces.</summary>
    public int Direction { get; set; } = MzCharacter.Down;
}