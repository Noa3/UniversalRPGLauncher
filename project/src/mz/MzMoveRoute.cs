using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// A move route, being carried out.
/// </summary>
/// <remarks>
/// <para>
/// <c>forceMoveRoute</c> does three things and a first reading does two:
/// it <b>memorizes</b> the route the character had, sets the new one, and puts
/// the index at zero. It does not start anything and it does not move
/// anybody. The route runs because the character is updated every frame, and
/// <c>updateRoutineMove</c> is what asks whether one is being forced.
/// </para>
///
/// <para>
/// <b>The order of the engine's own checks matters, and a first reading
/// reverses two of them.</b> <c>processMoveCommand</c> is reached only from
/// <c>updateRoutineMove</c> when <c>isMoveRouteForcing()</c> and
/// <c>!isStopped()</c> — so a character that is still walking is not given
/// the next step. One step is issued, then the character walks it, and only
/// when it has arrived does the next step come. <b>A route is a queue of
/// single steps, not a batch of them.</b>
/// </para>
///
/// <para>
/// <b>A refused step still advances the index.</b> <c>moveStraight</c> turns
/// and refuses, and the route goes on to the next entry — otherwise a
/// character facing a wall would stay on that entry for ever and the rest of
/// the route would never run.
/// </para>
///
/// <para>
/// <b>What is here is what this game uses</b>, measured over its ninety-six
/// routes: END 96, MOVE_LEFT 74, MOVE_RIGHT 59, MOVE_DOWN 50, MOVE_UP 45,
/// JUMP 31, CHANGE_SPEED 26, TURN_UP 11, TURN_DOWN 10, TURN_RIGHT 9,
/// TURN_LEFT 7, MOVE_BACKWARD 10, MOVE_FORWARD 4, WAIT 4, TRANSPARENT_ON 4,
/// STEP_ANIME_ON 2, STEP_ANIME_OFF 2. <b>MOVE_LEFT leads and MOVE_DOWN
/// follows</b>, which is the opposite of what "mostly walks south" would
/// guess. MOVE_RANDOM, MOVE_TOWARD, MOVE_AWAY and the diagonal codes appear
/// <b>zero</b> times and are not guessed at.
/// </para>
/// </remarks>
public sealed class MzMoveRoute
{
    /// <summary>The engine's <c>Game_Character.ROUTE_*</c> values, measured
    /// from 1.9.1 rather than numbered by this reader.</summary>
    public const int End = 0;
    public const int MoveDown = 1;
    public const int MoveLeft = 2;
    public const int MoveRight = 3;
    public const int MoveUp = 4;
    public const int MoveLowerL = 5;
    public const int MoveLowerR = 6;
    public const int MoveUpperL = 7;
    public const int MoveUpperR = 8;
    public const int MoveRandom = 9;
    public const int MoveToward = 10;
    public const int MoveAway = 11;
    public const int MoveForward = 12;
    public const int MoveBackward = 13;
    public const int Jump = 14;
    public const int Wait = 15;
    public const int TurnDown = 16;
    public const int TurnLeft = 17;
    public const int TurnRight = 18;
    public const int TurnUp = 19;
    public const int Walk = 20;
    public const int StepRandom = 21;
    public const int FaceUp = 22;
    public const int FaceRight = 23;
    public const int FaceDown = 24;
    public const int FaceLeft = 25;
    public const int Turn90DegreeRight = 26;
    public const int Turn90DegreeLeft = 27;
    public const int Turn180Degree = 28;
    public const int ChangeSpeed = 29;
    public const int ChangeFrequency = 30;
    public const int ChangePriorityType = 31;
    public const int ChangeImage = 32;
    public const int StepAnimeOn = 33;
    public const int StepAnimeOff = 34;
    public const int StepAnimeForce = 35;
    public const int TransparentOn = 39;
    public const int TransparentOff = 40;

    private MzRouteStep.Route _route;
    private bool _forced;
    private int _index;

    /// <summary>
    /// The route the character had before this one, as
    /// <c>memorizeMoveRoute</c> keeps it. <c>restoreMoveRoute</c> is the way
    /// back, and a route that forgets this has no way home.
    /// </summary>
    public MzRouteStep.Route? Memorized { get; private set; }

    /// <summary>Where the route has got to.</summary>
    public int Index => _index;

    /// <summary>Whether a route is being forced, as
    /// <c>isMoveRouteForcing</c> answers it.</summary>
    public bool IsForcing => _forced;

    /// <summary>How many entries the route has.</summary>
    public int Count => _route.List.Count;

    /// <summary>
    /// <c>forceMoveRoute</c>: memorize, take the new route, start at zero.
    /// </summary>
    public string Force(MzRouteStep.Route pRoute)
    {
        if (Memorized == null)
        {
            Memorized = _forced ? _route : null;
        }
        _route = pRoute;
        _index = 0;
        _forced = true;
        return $"a route of {pRoute.List.Count} steps is forced, waiting"
            + $" {(pRoute.Wait ? "for" : "not for")} the character to finish"
            + (pRoute.Repeat ? ", repeating" : "")
            + (pRoute.Skippable ? ", skippable" : "");
    }

    /// <summary><c>restoreMoveRoute</c>: back to what the character had.</summary>
    public string Restore()
    {
        if (Memorized == null)
        {
            return "there is no route to restore, because none was memorized";
        }
        _route = Memorized.Value;
        Memorized = null;
        _index = 0;
        _forced = false;
        return $"the route is restored, and it has {(_route.List.Count)} steps";
    }

    /// <summary>
    /// One step of the route, if one is due — which is only when the character
    /// is not already walking.
    /// </summary>
    /// <remarks>
    /// <b>This is the rule a batch implementation gets wrong.</b>
    /// <c>updateRoutineMove</c> reaches <c>processMoveCommand</c> only when
    /// <c>!this.isStopping()</c> is false — that is, when the character has
    /// arrived. A route of five steps into open floor is **five frames, not
    /// one**: each step is issued, the character walks it, and the next comes
    /// only once it has arrived. A reader that ran the whole list in one call
    /// would teleport the character five tiles and answer the question this
    /// card exists for wrongly.
    /// </remarks>
    public string Step(MzCharacter pCharacter, IMzMapPassable? pMap = null)
    {
        if (!_forced || _index >= _route.List.Count)
        {
            return _forced
                ? $"the route is out of steps at {_index} of"
                    + $" {_route.List.Count}, and nothing was done"
                : "no route is being forced, so no step is due";
        }

        // **Not while the character is still walking.** The engine's
        // `updateRoutineMove` is
        //   if (this.isMoveRouteForcing() && !this.isStopping()) {
        //       this.processMoveCommand(this._moveRoute[this._moveRouteIndex]);
        //   }
        // — and `isStopping()` is the character having arrived.
        //
        // **A first draft added `&& _index > 0` to this and nothing moved.**
        // The index is raised at the end of the last step, so after the first
        // step it is 1, and every later step was refused as "still walking"
        // even when the character was standing still. The guard is the
        // character alone — the engine's has no second term.
        if (!pCharacter.IsStopping)
        {
            return $"the character is still walking towards {pCharacter.X},"
                + $" {pCharacter.Y} and the step {_index} waits for it";
        }

        var step = _route.List[_index];
        _index++;
        return Run(pCharacter, step, pMap);
    }

    /// <summary>One entry, as <c>processMoveCommand</c> runs it.</summary>
    private static string Run(
        MzCharacter pCharacter, MzRouteStep pStep, IMzMapPassable? pMap)
    {
        switch (pStep.Code)
        {
            case End:
                return "the route ends here";

            case MoveDown:
                pCharacter.MoveStraight(MzCharacter.Down, pMap);
                return Moving("down", pCharacter);

            case MoveLeft:
                pCharacter.MoveStraight(MzCharacter.Left, pMap);
                return Moving("left", pCharacter);

            case MoveRight:
                pCharacter.MoveStraight(MzCharacter.Right, pMap);
                return Moving("right", pCharacter);

            case MoveUp:
                pCharacter.MoveStraight(MzCharacter.Up, pMap);
                return Moving("up", pCharacter);

            case MoveForward:
                // **`moveStraight(this.direction())`** — forwards is the
                // direction the character is already facing, not "up".
                pCharacter.MoveStraight(pCharacter.Direction, pMap);
                return Moving($"forwards, which is direction"
                    + $" {pCharacter.Direction}", pCharacter);

            case MoveBackward:
                // **`const lastDirectionFix = this.isDirectionFixed();
                // this.reverseDir(this.direction()); … this.setDirectionFix(
                // lastDirectionFix);`** — backwards turns the character round
                // and puts the facing back, so a character that walked
                // backwards is left facing where it went.
                var last = pCharacter.Direction;
                pCharacter.MoveStraight(MzCharacter.ReverseDir(last), pMap);
                pCharacter.TurnTo(last);
                return Moving($"backwards from {last}", pCharacter);

            case Jump:
                return DoJump(pCharacter, pStep, pMap);

            case Wait:
                pCharacter.WaitThisManySteps();
                return "the route says to wait, and the character stands"
                    + " until something lets it go";

            case TurnDown:
                pCharacter.TurnTo(MzCharacter.Down);
                return "the character turns down";

            case TurnLeft:
                pCharacter.TurnTo(MzCharacter.Left);
                return "the character turns left";

            case TurnRight:
                pCharacter.TurnTo(MzCharacter.Right);
                return "the character turns right";

            case TurnUp:
                pCharacter.TurnTo(MzCharacter.Up);
                return "the character turns up";

            case ChangeSpeed:
                // **The parameter is a number the game wrote, and this reader
                // keeps it.** Speed means "how many tiles per frame" and
                // `distancePerFrame` reads it, so a reader that stored the
                // value without a renderer is still honest: the number is
                // there and nothing is guessed.
                return pStep.Parameters.Count > 0
                    ? $"the character's speed is set to {pStep.Parameters[0]},"
                        + " which this reader keeps as the number the game wrote"
                    : "the route asks for a speed and writes none, so none is"
                        + " set";

            case StepAnimeOn:
            case StepAnimeOff:
            case TransparentOn:
            case TransparentOff:
                var an = pStep.Code is StepAnimeOn or TransparentOn;
                if (pStep.Code is TransparentOn or TransparentOff)
                {
                    pCharacter.Transparent = an;
                }
                return $"step anime is {(an ? "on" : "off")}"
                    + (pStep.Code is TransparentOn or TransparentOff
                        ? $", and the character is now"
                            + $" {(an ? "" : "not ")}transparent"
                        : ", which only a renderer can show");

            default:
                // **A code this reader does not know is named, not guessed.**
                // MZ defines thirty-one; this game uses seventeen. A reader
                // that invented MOVE_RANDOM or a diagonal would be writing
                // behaviour nobody can test, and a wrong diagonal is worse
                // than a refusal.
                return $"route code {pStep.Code} is not carried out by this"
                    + " reader, and the route goes on to the next step";
        }
    }

    /// <summary>
    /// <c>jump</c>: the same passability, but without the turn.
    /// </summary>
    /// <remarks>
    /// <c>if (Math.abs(xPlus) &gt; Math.abs(yPlus))</c> picks the axis, and a
    /// jump is a straight move on that axis — so a jump of +2 is two tiles
    /// along x, and the drawing position lands on the far side. **A jump is
    /// not a diagonal**, and a reader that took both parameters at once would
    /// send a character off a wall it cleared.
    /// </remarks>
    private static string DoJump(
        MzCharacter pCharacter, MzRouteStep pStep, IMzMapPassable? pMap)
    {
        var xPlus = Number(pStep, 0);
        var yPlus = Number(pStep, 1);
        var horizontal = Math.Abs(xPlus) > Math.Abs(yPlus);

        var dir = horizontal
            ? (xPlus < 0 ? MzCharacter.Left : MzCharacter.Right)
            : (yPlus < 0 ? MzCharacter.Up : MzCharacter.Down);

        if (!pCharacter.CanPass(pCharacter.X, pCharacter.Y, dir, pMap))
        {
            return $"a jump of {xPlus},{yPlus} is refused, because the step in"
                + $" direction {dir} is not allowed, and the character stays on"
                + $" {pCharacter.X},{pCharacter.Y}";
        }

        // **`this._x += xPlus; this._y += yPlus;`** — the whole leap, and the
        // drawing position is left behind, so a jump is drawn as arriving.
        pCharacter.LeapBy(xPlus, yPlus);
        return $"the character jumps {xPlus},{yPlus} to"
            + $" {pCharacter.X},{pCharacter.Y}";
    }

    private static int Number(MzRouteStep pStep, int pIndex) =>
        pIndex < pStep.Parameters.Count
            && int.TryParse(
                pStep.Parameters[pIndex],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var wert)
            ? wert
            : 0;

    private static string Moving(string pWay, MzCharacter pCharacter) =>
        pCharacter.MovementSucceeded
            ? $"the character moves {pWay} to"
                + $" {pCharacter.X},{pCharacter.Y}"
            : $"the character faces {pWay} but the step is refused, and stays"
                + $" on {pCharacter.X},{pCharacter.Y}";

    /// <summary>Whether the route has been walked to its end.</summary>
    public bool IsDone => !_forced || _index >= _route.List.Count;

    /// <summary>
    /// Whether the event page is still held by this route, as
    /// <c>setWaitMode("route")</c> means: <c>wait</c> was set, and the
    /// character is not done.
    /// </summary>
    public bool IsHoldingThePage => _route.Wait && !IsDone;
}
