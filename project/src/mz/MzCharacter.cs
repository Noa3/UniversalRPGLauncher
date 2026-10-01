using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// One walking thing: the player, an event, a vehicle. Position, facing, and
/// a move route being carried out.
/// </summary>
/// <remarks>
/// <para>
/// MZ 1.9.1 measures in <b>two coordinates, not one</b>, and this is the whole
/// of the class. <c>_x</c>/<c>_y</c> is the tile the character belongs to —
/// the one every rule asks about — and <c>_realX</c>/<c>_realY</c> is where
/// they are drawn, part-way across. <c>updateMove</c> walks the real one toward
/// the tile one, and when they meet, the character has arrived. A reader with
/// one coordinate has to invent a frame count, and an invented frame count is
/// how a game ends up sliding through walls or standing still.
/// </para>
///
/// <para>
/// <b>And moving sets the tile first and the drawing position second, from the
/// other side.</b> <c>moveStraight</c> is
/// <c>this._realX = $gameMap.xWithDirection(this._x, this.reverseDir(d))</c>
/// — the character is drawn <b>one tile behind</b> where they just arrived, so
/// the animation walks into the new tile rather than out of the old one. A
/// reader that snapped the drawing position to the tile would draw a character
/// that teleports once per step.
/// </para>
///
/// <para>
/// <b>Turn even when the step failed.</b> <c>moveStraight</c> turns in both
/// branches: on success and on failure, and on failure it additionally calls
/// <c>checkEventTriggerTouchFront</c> — a character that bumps into a wall
/// faces the wall, and that facing is what triggers the action button in
/// front of it. A reader that only turned on success would have a character
/// face the way they came when they walked into something.
/// </para>
///
/// <para>
/// <b>Diagonal movement has two halves and only sets the direction when the
/// step is refused.</b> <c>moveDiagonally</c> turns
/// <c>if (this._direction === this.reverseDir(horz))</c> — that is, it
/// corrects the facing from a diagonal to the axis just taken, and it does it
/// whether or not the move succeeded. A diagonal step between two walls moves
/// nobody and still straightens the facing.
/// </para>
///
/// <para>
/// <b>Directions are 2, 4, 6 and 8</b> — <b>down</b>, <b>left</b>,
/// <b>right</b>, <b>up</b>. Not 0 to 3, and not in the order this repository
/// uses for RM2K (<c>Up=0, Right=1, Down=2, Left=3</c>). The engine writes
/// <c>xWithDirection</c> as <c>x + (d === 6 ? 1 : d === 4 ? -1 : 0)</c> and
/// <c>reverseDir</c> as <c>10 - d</c>, and the four terms fall out of that:
/// 2↔8 for the vertical pair and 4↔6 for the horizontal one.
/// </para>
/// </remarks>
public sealed class MzCharacter
{
    /// <summary>MZ's four directions, as <c>Game_CharacterBase</c> numbers
    /// them. <b>Not 0..3</b>, and not the RM2K order.</summary>
    public const int Down = 2;

    public const int Left = 4;

    public const int Right = 6;

    public const int Up = 8;

    /// <summary>The tile this character is on, and the one every rule asks
    /// about.</summary>
    public int X { get; private set; }

    /// <summary>Which event this figure belongs to, or zero for none.</summary>
    /// <remarks>
    /// <strong>And a route needs to know who it walks, and that is the
    /// engine's <c>_characterId</c>.</strong> Measured at
    /// <c>updateWaitMode</c>: <c>case "route": character =
    /// this.character(this._characterId)</c> — **and that is how a page
    /// at a 205 finds out whether the figure arrived.</strong>
    /// </para>
    /// <para>
    /// <strong>And the player has no event</strong>, <strong>and its
    /// figure carries zero</strong> — <strong>and a reader that looked
    /// for a figure by event id found none for the player, and a page
    /// at a 205 for minus one waited for ever.</strong>
    /// </para>
    /// </remarks>
    public int EventId { get; set; }

    /// <summary>Which figure of its sheet this is.</summary>
    /// <remarks>
    /// <strong>And this belongs to the figure, and not to the
    /// page.</strong> Measured:
    /// <c>Game_CharacterBase.prototype.setImage</c> sets
    /// <c>_tileId</c>, <c>_characterName</c> and <c>_characterIndex</c> on
    /// the character itself — <strong>so every figure has one, the
    /// player's included</strong>, <strong>and a page that changes a
    /// figure's picture changes it here and nowhere else.</strong>
    /// </remarks>
    public int CharacterIndex { get; private set; }


    public int Y { get; private set; }

    /// <summary>Where the character is drawn, part-way across a tile.</summary>
    public double RealX { get; private set; }

    public double RealY { get; private set; }

    public int Direction { get; private set; } = Down;

    /// <summary>
    /// Puts this figure on a tile, and turns it, from <c>203 Set Event
    /// Location</c>.
    /// </summary>
    /// <param name="pX">The tile column.</param>
    /// <param name="pY">The tile row.</param>
    /// <param name="pDirection">Which way it faces afterwards.</param>
    /// <returns>What happened, in a sentence.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a tile is a jump, and not a step.</strong> The
    /// official help: *Changes the location of an event* and *To move an
    /// event to a specific location, select [Direct Designation]*.
    /// <strong>There is no walk and no route here</strong> — the figure
    /// is at the new tile on the next frame,
    /// <strong>and a reader that set only <c>X</c> and <c>Y</c> left
    /// <c>RealX</c> and <c>RealY</c> behind</strong>, **and a figure
    /// whose tile says one thing and whose drawn position says another
    /// walks back to the old tile on the next movement command.**
    /// </para>
    /// <para>
    /// <strong>And the direction is a fourth setting, and not part of
    /// the place.</strong> The help lists Event, Location and
    /// Direction; **and the direction is applied even when the location
    /// is "unchanged"**, because the engine's own
    /// <c>setLocation</c> takes all three.
    /// </para>
    /// </remarks>
    public string SetLocation(int pX, int pY, int pDirection)
    {
        X = pX;
        Y = pY;
        RealX = pX;
        RealY = pY;
        Direction = pDirection;
        return $"event moved to {pX},{pY} facing {pDirection}";
    }

    /// <summary>Whether the character is through things, which skips
    /// passability entirely.</summary>
    public bool Through { get; set; }

    /// <summary>Whether the character is drawn, from
    /// <c>ROUTE_TRANSPARENT_ON</c> / <c>_OFF</c>.</summary>
    public bool Transparent { get; set; }

    /// <summary>
    /// Whether the character is walking or waiting for a route, from
    /// <c>ROUTE_WAIT</c>.
    /// </summary>
    public bool Waiting { get; private set; }

    /// <summary>
    /// The balloon icon over this figure, and how long it has left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a balloon is a request with a clock, and not a
    /// picture.</strong> The official help for <c>213 Show Balloon
    /// Icon</c> says: *Display icons that express emotions in balloons
    /// that appear over a party member or event* and *When enabled, the
    /// event will be paused until the balloon icon being displayed has
    /// disappeared.*
    /// </para>
    /// <para>
    /// <strong>And the icon is a name, and not a number.</strong> The
    /// help says *There are ten types of icons available, such as a "!"
    /// for expressing surprise*, **and it is also possible for users to
    /// define their own** — **so the value in the file is a position
    /// into a list the game may have replaced, and this reader keeps the
    /// number and says what it cannot know.**
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <strong>And it starts at <c>NoBalloon</c>, and not at zero.</strong>
    /// Zero is the editor's first icon, **and a figure whose field starts
    /// at zero answers "yes, there is an icon"** — **and a game whose
    /// first balloon is icon 0 would be right by luck and every other
    /// figure would show one from the frame the map loaded.**
    /// </remarks>
    public int BalloonIcon { get; private set; } = NoBalloon;

    /// <summary>
    /// The animation over this figure, and how long it has left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And an animation is a second field, and not the
    /// balloon.</strong> The official help gives <c>221 Show
    /// Animation</c> and <c>213 Show Balloon Icon</c> the same three
    /// settings, **and the two commands are separate codes in every
    /// finished project**, **and one field for both would have let a
    /// game's balloon overwrite its animation and neither would have
    /// been visible for long enough to see.**
    /// </para>
    /// <para>
    /// <strong>And both fields carry the same three numbers</strong>,
    /// **and they are written out twice here rather than shared**,
    /// **because a shared field is the exact bug above.**
    /// </para>
    /// </remarks>
    /// <summary>
    /// Whether <c>222 Erase Event</c> has hidden this figure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And "erased" is not "gone", and not "transparent".</strong>
    /// The official help: *Temporarily removes the event currently being
    /// run.* <strong>The event is still on the map and still has its
    /// commands**; **it just is not drawn**, **and a reader that removed
    /// it from the dictionary made every later command that named it fail
    /// with "no such character"** — and a game's own event erases
    /// itself and then runs four more commands.
    /// </para>
    /// <para>
    /// <strong>And it ends at a map change, and not on its own.</strong>
    /// *The event will remain erased until the party moves to another
    /// map.* <strong>Nothing here clears it</strong>, **and a reader
    /// that ticked it off after N frames brought the event back while the
    /// player was looking straight at the place where it had been.**
    /// </para>
    /// </remarks>
    /// <summary>Hides this figure, from <c>222 Erase Event</c>.</summary>
    public void Erase() => Erased = true;


    public bool Erased { get; private set; }


    public int Animation { get; private set; } = NoAnimation;

    /// <summary>Whether an animation is playing over this figure.</summary>
    public bool HasAnimation => Animation != NoAnimation;

    /// <summary>Frames the animation has left.</summary>
    public int AnimationFramesLeft { get; private set; }

    /// <summary>What "no animation" is, and not zero.</summary>
    /// <remarks>
    /// <strong>And zero is the editor's first animation</strong>, **and a
    /// figure that started there would answer "yes, one is playing"**
    /// **before the map's first frame was drawn.**
    /// </remarks>
    public const int NoAnimation = -1;

    /// <summary>Plays an animation over this figure, from <c>221</c>.</summary>
    /// <param name="pAnimation">Which one, from the game's own list.</param>
    /// <param name="pFrames">How long it plays.</param>
    public void ShowAnimation(int pAnimation, int pFrames)
    {
        Animation = pAnimation;
        AnimationFramesLeft = pFrames;
    }

    /// <summary>Stops the animation at once.</summary>
    public void ClearAnimation()
    {
        Animation = NoAnimation;
        AnimationFramesLeft = 0;
    }

    /// <summary>Counts the animation's clock down.</summary>
    /// <param name="pFrames">How many frames passed.</param>
    public void TickAnimation(int pFrames)
    {
        if (AnimationFramesLeft <= 0)
        {
            return;
        }

        AnimationFramesLeft -= pFrames;
        if (AnimationFramesLeft <= 0)
        {
            AnimationFramesLeft = 0;
            Animation = NoAnimation;
        }
    }

    /// <summary>
    /// Whether an icon is showing over this figure.
    /// </summary>
    public bool HasBalloon => BalloonIcon != NoBalloon;

    /// <summary>
    /// Frames the balloon has left, and zero when none is showing.
    /// </summary>
    public int BalloonFramesLeft { get; private set; }

    /// <summary>What "no icon" is, and not zero.</summary>
    /// <remarks>
    /// <strong>And zero is the first icon, and not the absence of
    /// one.</strong> The editor's list starts at zero,
    /// **and a figure whose icon was cleared would be marked as showing
    /// the first one** — **and the official help says an event can
    /// choose to wait for the icon to disappear, which is a wait that
    /// never ends.**
    /// </remarks>
    public const int NoBalloon = -1;

    /// <summary>
    /// Shows an icon over this figure, from <c>213</c>.
    /// </summary>
    /// <param name="pIcon">Which icon, from the game's own list.</param>
    /// <param name="pFrames">How long it stays.</param>
    public void ShowBalloon(int pIcon, int pFrames)
    {
        BalloonIcon = pIcon;
        BalloonFramesLeft = pFrames;
    }

    /// <summary>
    /// Takes the icon away at once, which is what <c>214 Erase Event</c>
    /// does to everything it owns.
    /// </summary>
    public void ClearBalloon()
    {
        BalloonIcon = NoBalloon;
        BalloonFramesLeft = 0;
    }

    /// <summary>
    /// Counts the icon's clock down, and takes it away when it is gone.
    /// </summary>
    /// <param name="pFrames">How many frames passed.</param>
    public void TickBalloon(int pFrames)
    {
        if (BalloonFramesLeft <= 0)
        {
            return;
        }

        BalloonFramesLeft -= pFrames;
        if (BalloonFramesLeft <= 0)
        {
            BalloonFramesLeft = 0;
            BalloonIcon = NoBalloon;
        }
    }


    /// <summary>
    /// Whether the character has arrived, as <c>isStopping</c> reports it.
    /// </summary>
    /// <remarks>
    /// <c>return !this.isMoving() &amp;&amp; !this.isJumping();</c> and
    /// <c>isMoving</c> is <c>this._realX !== this._x || this._realY !==
    /// this._y</c>. **Two terms, and the first draft added a third.**
    ///
    /// A draft wrote <c>RealX == X &amp;&amp; RealY == Y &amp;&amp; !Waiting</c> and
    /// **the route stopped on every character a <c>ROUTE_WAIT</c> had ever
    /// touched**: a character told to wait is standing still, so the draft's
    /// answer was the engine's opposite, and a route with a wait in it ran
    /// backwards — re-issuing the same step for ever. **A character waiting is
    /// a character that has arrived**; the engine keeps handing out steps and
    /// the wait is a separate thing that stops frames, not movement.
    /// </remarks>
    public bool IsStopping => RealX == X && RealY == Y && !IsJumping;

    /// <summary>
    /// Whether the character is mid-jump, as <c>isJumping</c> reports it.
    /// </summary>
    /// <remarks>
    /// <c>return this._jumpCount > 0;</c> — a count that ticks down one per
    /// frame. A jumping character has already changed tile, so its drawing
    /// position matches and it would otherwise look arrived; <b>the jump
    /// count is what keeps the next step from being issued early</b>.
    /// </remarks>
    /// <summary>Whether a leap is still being drawn, as the engine asks.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a derived question, and it was a stored flag,
    /// and a stored flag is a second truth.</strong> Measured at
    /// <c>isJumping</c>: <c>return this._jumpCount &gt; 0;</c> -- <strong>and
    /// there is no setter, and <c>jump</c> writes <c>_jumpCount</c> and
    /// nothing else.</strong>
    /// </para>
    /// <para>
    /// <strong>And the flag was only ever updated inside <c>Tick</c>
    /// </strong>, <strong>so a figure that had just leapt was not
    /// <c>IsJumping</c> until the frame after the leap</strong>, <strong>and
    /// a leap of <c>[0, 0]</c> -- <strong>which is all 31 leaps of this
    /// game</strong> -- never reported as running at all.</strong>
    /// </para>
    /// </remarks>
    public bool IsJumping => _jumpFrames > 0;

    /// <summary>How many frames a jump still has, as <c>_jumpCount</c>.</summary>
    /// <summary>How fast this figure walks, 1 to 6.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>_moveSpeed</c>, and the engine's default is
    /// <strong>4</strong>:</strong> <c>initMembers</c> sets
    /// <c>this._moveSpeed = 4;</c> <strong>and route code 29 calls
    /// <c>this.setMoveSpeed(params[0])</c>, which is a plain assignment.</strong>
    /// </para>
    /// <para>
    /// <strong>And the measured game uses route code 29 twenty-six times
    /// and writes a real number into it: 22 times <c>5</c>, twice <c>6</c>,
    /// twice <c>4</c>.</strong> <strong>And the reader had no field for
    /// it at all and only wrote a sentence about it</strong>, <strong>so
    /// every one of those twenty-six steps left the figure walking at 4
    /// while the game had it walking at 5.</strong>
    /// </para>
    /// <para>
    /// <strong>And it is not cosmetic.</strong> A step takes
    /// <c>2^moveSpeed / 256</c> tiles per frame, so 4 and 5 differ by a
    /// factor of two -- <strong>the same route reaches a tile at half the
    /// time, and every wait measured against it halves with it.</strong>
    /// </para>
    /// </remarks>
    public int MoveSpeed { get; private set; } = 4;

    /// <summary>How often this figure repeats its walking animation, 1 to 3.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>_moveFrequency</c>, and the engine's default
    /// is <strong>6</strong>:</strong> <c>initMembers</c> sets
    /// <c>this._moveFrequency = 6;</c> <strong>and route code 30 calls
    /// <c>this.setMoveFrequency(params[0])</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And the custom-route start threshold is
    /// <c>30 * (5 - moveFrequency)</c></strong>, <strong>so at the default
    /// 6 a page's route does not begin until 30 * (5 - 6) frames</strong>
    /// -- <strong>which is negative, and so a route that is forced starts
    /// on the first frame</strong>, <strong>and a route that is not forced
    /// waits the walk clock out.</strong> <strong>The measured game uses
    /// code 30 zero times</strong>, <strong>so the default is what runs.</strong>
    /// </para>
    /// </remarks>
    public int MoveFrequency { get; private set; } = 6;

    /// <summary>Sets the walking speed, as route code 29 does.</summary>
    /// <remarks>
    /// **And the engine does not range-check this**
    /// (<c>this._moveSpeed = moveSpeed;</c>), **so a game that wrote a
    /// number outside 1 to 6 gets it stored as it is**, **and a reader
    /// that clamps would walk differently from the game.</strong>
    /// </remarks>
    public void SetMoveSpeed(int pSpeed) => MoveSpeed = pSpeed;

    /// <summary>Sets the walk animation rate, as route code 30 does.</summary>
    /// <remarks>
    /// **And the engine does not range-check this either**, **and the
    /// measured game never writes it, so this is here for the routes that
    /// do rather than guessed at.</strong>
    /// </remarks>
    public void SetMoveFrequency(int pFrequency) => MoveFrequency = pFrequency;

    public int JumpCount => IsJumping ? _jumpFrames : 0;

    private int _jumpFrames;

    /// <summary>
    /// The move route this character is carrying out, or has none.
    /// </summary>
    /// <remarks>
    /// The engine keeps the route on the character rather than in the
    /// interpreter, because a character can be walking while no event page is
    /// running — a route forced by a 205 keeps going after the page has moved
    /// on. A reader that kept the route in the interpreter would stop walking
    /// the moment the page went on.
    /// </remarks>
    public MzMoveRoute Route { get; } = new();

    /// <summary>Whether the last attempted step got through.</summary>
    public bool MovementSucceeded { get; private set; }

    /// <summary>
    /// A character, as <c>initialize</c> leaves it: on a tile, facing down,
    /// drawn exactly on it and therefore not moving.
    /// </summary>
    public MzCharacter(int pX = 0, int pY = 0, int pDirection = Down)
    {
        X = pX;
        Y = pY;
        RealX = pX;
        RealY = pY;
        Direction = pDirection;
    }

    /// <summary>
    /// <c>reverseDir</c>: the opposite direction, <c>10 - d</c>.
    /// </summary>
    /// <remarks>
    /// **Not <c>(d + 4) % 4</c>.** A first draft wrote that, and it is right
    /// for a 0..3 numbering and wrong for MZ's 2/4/6/8. Checked against the
    /// engine: <c>reverseDir(2) = 8</c> is **Up**, not Down — down's
    /// opposite is up, and the draft's mapping said otherwise, so every
    /// "one tile behind" position was placed in front of the character
    /// instead of behind it and **every character walked away from where it
    /// was going.**
    ///
    /// The four terms fall out of <c>10 - d</c>: 2↔8 (down, up) and 4↔6
    /// (left, right).
    /// </remarks>
    public static int ReverseDir(int pDir) => 10 - pDir;

    /// <summary>The x a direction leads to, as <c>xWithDirection</c>.</summary>
    public static int XWithDirection(int pX, int pDir) => pDir switch
    {
        Left => pX - 1,
        Right => pX + 1,
        _ => pX,
    };

    /// <summary>The y a direction leads to, as <c>yWithDirection</c>.</summary>
    public static int YWithDirection(int pY, int pDir) => pDir switch
    {
        Up => pY - 1,
        Down => pY + 1,
        _ => pY,
    };

    /// <summary>
    /// Whether a step in a direction is allowed, as <c>canPass</c> decides it.
    /// </summary>
    /// <remarks>
    /// <b>Four refusals, and the order matters</b>, because the engine tests
    /// them in this order:
    ///
    /// <list type="number">
    /// <item><b>Off the map.</b> <c>!$gameMap.isValid(x2, y2)</c> — a step
    /// that would leave the map is refused before anything else, and a
    /// character on the edge cannot walk off it.</item>
    /// <li><b>Through wins over everything.</b> <c>if (this.isThrough() ||
    /// isDebugThrough()) return true;</c> — a through character walks over
    /// walls, and it is checked <b>after</b> the map edge, so a through
    /// character is still stopped by the border.</item>
    /// <li><b>The map itself.</b> <c>isMapPassable</c> asks twice — the tile
    /// being left in the given direction and the tile being entered in the
    /// opposite one. <b>Both, not either.</b> A character cannot pass through
    /// a gap that is one tile wide if the tile before it faces the same
    /// way.</item>
    /// <li><b>Other characters.</b> <c>isCollidedWithCharacters</c> — an
    /// event or a boat or a ship standing there.</item>
    /// </list>
    /// </remarks>
    public bool CanPass(int pX, int pY, int pDir, IMzMapPassable? pMap = null)
    {
        var x2 = XWithDirection(pX, pDir);
        var y2 = YWithDirection(pY, pDir);

        // **Off the map is first**, and a through character is still stopped
        // by it — the engine tests the border before the through flag.
        if (pMap != null && !pMap.IsValid(x2, y2))
        {
            return false;
        }

        if (Through)
        {
            return true;
        }

        if (pMap != null && !IsMapPassable(pX, pY, pDir, pMap))
        {
            return false;
        }

        return pMap?.IsClearOfCharacters(x2, y2) ?? true;
    }

    /// <summary>
    /// <c>isMapPassable</c>: both the tile being left and the tile being
    /// entered.
    /// </summary>
    /// <remarks>
    /// <c>return $gameMap.isPassable(x, y, d) &amp;&amp; $gameMap.isPassable(x2,
    /// y2, d2);</c> where <c>d2 = this.reverseDir(d)</c>. **Both, not
    /// either** — and the second asks about the *entered* tile looking back.
    /// A reader that checked only the destination would let a character walk
    /// out of a wall and into open floor.
    /// </remarks>
    private static bool IsMapPassable(
        int pX, int pY, int pDir, IMzMapPassable pMap)
    {
        var x2 = XWithDirection(pX, pDir);
        var y2 = YWithDirection(pY, pDir);
        return pMap.IsPassable(pX, pY, pDir) && pMap.IsPassable(x2, y2, ReverseDir(pDir));
    }

    /// <summary>
    /// <c>canPassDiagonally</c>: either order of the two half-steps.
    /// </summary>
    /// <remarks>
    /// <c>if (canPass(x, y, vert) &amp;&amp; canPass(x, y2, horz)) return true;
    /// if (canPass(x, y, horz) &amp;&amp; canPass(x2, y, vert)) return true;
    /// return false;</c> — **horizontal first, then vertical, in both
    /// orders.** A diagonal move is two straight steps and either order will
    /// do; a reader that only tried one would have a character unable to
    /// squeeze through a diagonal gap that is open in the other order.
    /// </remarks>
    public bool CanPassDiagonally(
        int pHorz, int pVert, IMzMapPassable? pMap = null)
    {
        var x2 = XWithDirection(X, pHorz);
        var y2 = YWithDirection(Y, pVert);

        if (CanPass(X, Y, pVert, pMap) && CanPass(X, y2, pHorz, pMap))
        {
            return true;
        }
        return CanPass(X, Y, pHorz, pMap) && CanPass(x2, Y, pVert, pMap);
    }

    /// <summary>
    /// <c>moveStraight</c>: turn, and move if the step is allowed.
    /// </summary>
    /// <remarks>
    /// <b>Two rules a first reading gets wrong.</b> The character turns in
    /// <b>both</b> branches — a character that bumps a wall faces the wall,
    /// which is what the action button then triggers. And on success the
    /// drawing position is set to <b>one tile behind</b>, which is what makes
    /// the walk look like a walk instead of a jump cut.
    /// </remarks>
    public void MoveStraight(int pDir, IMzMapPassable? pMap = null)
    {
        MovementSucceeded = CanPass(X, Y, pDir, pMap);
        if (MovementSucceeded)
        {
            Direction = pDir;
            X = XWithDirection(X, pDir);
            Y = YWithDirection(Y, pDir);
            // **One tile behind**, in the direction just left. The character is
            // drawn where they came from and walks into the new tile.
            RealX = XWithDirection(X, ReverseDir(pDir));
            RealY = YWithDirection(Y, ReverseDir(pDir));
        }
        else
        {
            // **It still turns.** A character that cannot move faces the way
            // it tried to go, and that facing is what triggers the action
            // button in front of it.
            Direction = pDir;
        }
    }

    /// <summary><c>moveDiagonally</c>: two half-steps, and a straightening of
    /// the facing whether or not the move worked.</summary>
    public void MoveDiagonally(int pHorz, int pVert, IMzMapPassable? pMap = null)
    {
        MovementSucceeded = CanPassDiagonally(pHorz, pVert, pMap);
        if (MovementSucceeded)
        {
            X = XWithDirection(X, pHorz);
            Y = YWithDirection(Y, pVert);
            RealX = XWithDirection(X, ReverseDir(pHorz));
            RealY = YWithDirection(Y, ReverseDir(pVert));
        }

        // **The facing is straightened from the diagonal to the axis taken,
        // and it happens whether or not the move succeeded.** A diagonal step
        // between two walls moves nobody and still turns the character.
        if (Direction == ReverseDir(pHorz))
        {
            Direction = pHorz;
        }
        if (Direction == ReverseDir(pVert))
        {
            Direction = pVert;
        }
    }

    /// <summary>
    /// <c>updateMove</c>: one frame, the drawing position toward the tile.
    /// </summary>
    /// <remarks>
    /// <c>distancePerFrame</c> is <c>1 + (realMoveSpeed() &gt; 4 ? 1 : 0)</c>
    /// for a character — one tile per frame, or two above speed four. Walking
    /// the real position in whole tiles keeps a long walk from accumulating a
    /// rounding error, which is what <c>_realX</c> being a float in the
    /// engine is there to prevent.
    /// </remarks>
    public void PassFrame(int pSpeed = 4)
    {
        // **A jump runs on its own frames, not on the walking speed.**
        // `updateJump` counts `_jumpCount` down and moves the drawing
        // position over the leap; the walking code is not involved.
        // **Und die zweite Wahrheit ist weg.** Measured at
        // `updateJump`: `this._jumpCount--;` and nothing else, and
        // `isJumping` asks `_jumpCount > 0` where it is asked.
        // **A figure that leapt on this frame is therefore mid-leap
        // on the same frame**, which a flag set at the end of
        // `PassFrame` could not be.
        if (_jumpFrames > 0)
        {
            _jumpFrames--;
            return;
        }

        if (Waiting)
        {
            return;
        }

        // **Und der Schritt ist eine Bruchzahl, und nicht eine ganze
        // Kachel.** **Gemessen an
        // `Game_CharacterBase.prototype.updateMove`:**
        // **`_realX = Math.min(_realX + distancePerFrame(), _x)`,
        // und `distancePerFrame` ist `2^realMoveSpeed / 256`.**
        //
        // **Bei Tempo 4 ist das ein Viertel einer Kachel je Bild, bei
        // Tempo 5 ein Achttel und bei Tempo 6 ein Sechzehntel.**
        //
        // **Und diese Datei hatte hier zwei ganze Kacheln, je nachdem ob
        // Tempo 4 ueberschritten war oder nicht** -- **das heisst: der
        // Sprung von Tempo 4 auf 5 liess die Figur doppelt so schnell
        // gehen, und beide Geschwindigkeiten waren falsch.** **Ein
        // Leser, der die Bruchzahl nimmt, sieht einen Lauf;
        // einer, der ganze Kacheln nimmt, sieht zwei Spruenge.**
        var step = Math.Pow(2, pSpeed) / 256.0;

        if (X < RealX)
        {
            RealX = Math.Max(RealX - step, X);
        }
        if (X > RealX)
        {
            RealX = Math.Min(RealX + step, X);
        }
        if (Y < RealY)
        {
            RealY = Math.Max(RealY - step, Y);
        }
        if (Y > RealY)
        {
            RealY = Math.Min(RealY + step, Y);
        }
    }

    /// <summary>How many frames until the character has arrived, and the
    /// route is done with this step.</summary>
    public int FramesToArrival(int pSpeed = -1)
    {
        // **And the speed is this figure's own, and not a literal.**
        //
        // **Measured at the engine:** `getRealMoveSpeed() { return this._moveSpeed
        // + (this.isPlayer() ? 1 : 0); }` and the step size is
        // `Math.pow(2, this.moveSpeed()) / 256` in `updatePosition`. **And
        // `_moveSpeed` starts at 4 and route code 29 overwrites it.**
        //
        // **And a default of 4 in the signature was the reader's own**
        // -- **and a figure the game had set to 5 waited twice as long as
        // it should, because nobody passed the speed in.**
        var speed = pSpeed < 0 ? MoveSpeed : pSpeed;
        var step = Math.Pow(2, speed) / 256.0;
        var dx = Math.Abs(X - RealX);
        var dy = Math.Abs(Y - RealY);
        return (int)Math.Ceiling(Math.Max(dx, dy) / step);
    }

    /// <summary>Turns without moving, as the four <c>ROUTE_TURN_*</c> do.</summary>
    /// <summary>
    /// Puts this figure's own picture on it.
    /// </summary>
    /// <param name="pName">Which sheet.</param>
    /// <param name="pIndex">Which figure of that sheet.</param>
    /// <remarks>
    /// <strong>And this is the engine's <c>setImage</c>, and it sets
    /// three things</strong> — <strong>the tile id, the name and the
    /// index</strong> — <strong>and a reader that set only the name drew
    /// figure zero forever.</strong>
    /// </remarks>
    /// <summary>Which sheet this figure is drawn from.</summary>
    /// <remarks>
    /// <strong>And a figure with a sheet but no index is always the
    /// first one.</strong> <strong>Measured:
    /// <c>Game_Player.prototype.refresh</c> calls
    /// <c>setImage(actor.characterName(), actor.characterIndex())</c>
    /// with both</strong>, <strong>and a reader that kept only the name
    /// drew every actor as actor one.</strong>
    /// </remarks>
    public string CharacterName { get; private set; } = "";

    public void SetImage(string pName, int pIndex)
    {
        CharacterName = pName;
        CharacterIndex = pIndex < 0 ? 0 : pIndex;
    }

    public void TurnTo(int pDir) => Direction = pDir;

    /// <summary>
    /// <c>this._x += xPlus; this._y += yPlus;</c> — the whole leap.
    /// </summary>
    /// <remarks>
    /// **The drawing position is left behind**, so a jump is drawn as arriving
    /// rather than as gliding, and **the tile changes without a passability
    /// check on the tiles in between**: the engine checks the one step in the
    /// chosen direction and nothing else, and a jump over a wall is a jump
    /// over a wall.
    /// </remarks>
    public void LeapBy(int pXPlus, int pYPlus)
    {
        // **And both axes move, and this is the rule, and a first draft of
        // this method got it wrong in the most expensive direction.**
        //
        // Measured at `jump` in the game's js/rmmz_objects.js:
        //
        // ```js
        // if (Math.abs(xPlus) > Math.abs(yPlus)) {
        //     if (xPlus !== 0) { this.setDirection(xPlus < 0 ? 4 : 6); }
        // } else {
        //     if (yPlus !== 0) { this.setDirection(yPlus < 0 ? 8 : 2); }
        // }
        // this._x += xPlus;
        // this._y += yPlus;
        // ```
        //
        // **And the `if`/`else` chooses the facing and nothing else** -- it
        // is inside the direction block, and the position is two plain
        // additions after it. **A jump of 3,1 therefore lands three right
        // AND one down**, which is a diagonal tile and not a mistake, and a
        // reader that believed the `if` chose the axis landed the figure
        // three right and nowhere else, on the wrong tile.
        //
        // **And a jump is not checked for passability.** The engine adds
        // the offsets outright; there is no `canPass`, no `checkPassage` and
        // no event. A route step goes through `moveStraight` and is
        // checked; a jump is not.
        X += pXPlus;
        Y += pYPlus;

        // **And the leap's height comes from the diagonal, not from the
        // bigger axis:** `const distance = Math.round(Math.sqrt(xPlus *
        // xPlus + yPlus * yPlus)); this._jumpPeak = 10 + distance -
        // this._moveSpeed; this._jumpCount = this._jumpPeak * 2;`
        //
        // **And every one of the measured game's 31 jumps is `[0, 0]`**,
        // so the distance is 0, the peak is `10 - _moveSpeed` and at the
        // default speed of 4 the count is 12 -- **and the jump still runs.**
        // A first draft wrote `Math.Max(1, Math.Max(|x|,|y|) * 6)`, which
        // is 1 frame for `[0, 0]`, and a leap the engine draws over 12
        // frames was over in one.
        var distance = (int)Math.Round(Math.Sqrt(
            (pXPlus * pXPlus) + (pYPlus * pYPlus)));
        _jumpFrames = Math.Max(1, (10 + distance - MoveSpeed) * 2);
    }

    /// <summary>
    /// <c>ROUTE_WAIT</c>: stand still, and <b>do not draw the character
    /// walking</b> while doing it.
    /// </summary>
    /// <remarks>
    /// This is a wait and not a refusal: the route index has already moved
    /// on, so the next step comes as soon as this one is over, and a
    /// character released by nothing stays released for good.
    /// </remarks>
    public void WaitThisManySteps() => Waiting = true;

    /// <summary>Puts the character somewhere, drawing position and all.</summary>
    public void PlaceAt(int pX, int pY)
    {
        X = pX;
        Y = pY;
        RealX = pX;
        RealY = pY;
    }

    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        $"character on {X},{Y} facing {Direction} drawn at {RealX:0.##},{RealY:0.##}"
        + (MovementSucceeded ? "" : ", its last step was refused")
        + (Waiting ? ", waiting" : "");
}

/// <summary>
/// The two questions a character asks of the map, and nothing else.
/// </summary>
/// <remarks>
/// The engine asks more, but a reader with no renderer cannot answer
/// `bushDepth`, `terrainTag` or `regionId` honestly, and an invented answer
/// there would be a lie a game could act on.
/// </remarks>
public interface IMzMapPassable
{
    /// <summary>
    /// Whether a tile is on the map, as <c>$gameMap.isValid</c> answers it.
    /// </summary>
    public bool IsValid(int pX, int pY);

    /// <summary>
    /// Whether a tile lets a character leave it in a direction, as
    /// <c>$gameMap.isPassable(x, y, d)</c> answers it.
    /// </summary>
    /// <remarks>
    /// <b>This is the flag on the tile, and it is per direction, not per
    /// tile.</b> A map whose tile 5 blocks only the upward direction still
    /// lets a character walk out of it sideways.
    /// </remarks>
    public bool IsPassable(int pX, int pY, int pDir);

    /// <summary>Whether a tile is free of other characters.</summary>
    public bool IsClearOfCharacters(int pX, int pY);
}
