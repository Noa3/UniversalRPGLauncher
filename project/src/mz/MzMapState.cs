using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// The events a map carries, and which of them have been taken away.
/// </summary>
/// <remarks>
/// <para>
/// <b>And this is the state <c>214 Erase Event</c> changes and nothing
/// else does.</b>
/// </para>
/// <para>
/// The engine keeps it in <c>$gameMap._events</c> and removes an entry:
///
/// <code>
/// Game_Map.prototype.eraseEvent = function(eventId) {
///     if (this._events[eventId]) {
///         delete this._events[eventId];
///         this.refresh();
///     }
/// };
/// </code>
///
/// <b>And two things in that line are easy to lose.</b> The
/// <c>delete</c> is a <b>delete</b> and not an assignment to nothing, so
/// the slot is gone and a later <c>findProperPageIndex</c> over the map's
/// events will not see it — <b>which is the point, because an erased
/// treasure chest must not answer the player a second time.</b> And the
/// <c>refresh()</c> happens, so a map whose event list changed redraws
/// <b>in the same command</b> and not on the next frame.
/// </para>
/// <para>
/// <b>And an event that is erased is not remembered across a map
/// change.</b> The engine rebuilds <c>$gameMap</c> from the map's own JSON
/// every time the party enters, so <b>leaving and returning brings the
/// chest back</b> — and that is measured behaviour and not a defect, and a
/// reader that made it permanent would be making a claim the engine does
/// not make.
/// </para>
/// </remarks>
public sealed class MzMapState
{
    /// <summary>
    /// The events the map had when the run began, and it is the set the
    /// engine rebuilds it from.
    /// </summary>
    private readonly Dictionary<int, string> _ereignisse = new();

    /// <summary>
    /// The events that have been erased, and only while the party stays on
    /// this map.
    /// </summary>
    private readonly HashSet<int> _entfernt = new();

    /// <summary>How many events the map carries.</summary>
    public int Count => _ereignisse.Count;

    /// <summary>How many of them have been erased.</summary>
    public int ErasedCount => _entfernt.Count;

    /// <summary>
    /// An event this map carries, by the number a page's <c>214</c> is
    /// standing on.
    /// </summary>
    public bool Has(int pEventId) =>
        pEventId > 0 && _ereignisse.ContainsKey(pEventId);

    /// <summary>
    /// Remove an event, and say what it was.
    /// </summary>
    /// <remarks>
    /// <b>And an event that is not there is not an error.</b> The engine
    /// guards with <c>if (this._events[eventId])</c>, and a page that
    /// erases itself twice — which a loop can do — erases nothing the
    /// second time and stops there.
    /// </remarks>
    /// <summary>
    /// Mark an event as erased, and say what it was.
    /// </summary>
    /// <param name="pEventId">The event.</param>
    /// <returns>Its name, and nothing when there is no such event.</returns>
    /// <remarks>
    /// <para>
    /// <b>And the engine does not remove anything.</b> Measured at
    /// <c>Game_Map.prototype.eraseEvent</c>: <c>this._events[eventId].erase()
    /// </c> -- and at <c>Game_Event.prototype.erase</c>: <c>this._erased =
    /// true;</c>. <strong>The event stays in the map and keeps its
    /// pages</strong>, and only <c>Game_Event.isErased</c> answers true,
    /// and the help says <i>until the party moves to another map</i>.
    /// </para>
    /// <para>
    /// <strong>And a reader that removed it from the map made every later
    /// command that named it say "no such character"</strong>, and a game's
    /// own event erases itself and then runs four more commands.
    /// </para>
    /// </remarks>
    public string? Erase(int pEventId)
    {
        if (pEventId <= 0)
        {
            return null;
        }

        var da = _ereignisse.TryGetValue(pEventId, out var name)
            ? name
            : "event " + pEventId;
        _entfernt.Add(pEventId);
        return da;
    }

    /// <summary>
    /// What the map looks like to a page that is choosing where to run.
    /// </summary>
    /// <remarks>
    /// <b>And this is the rule from <c>findProperPageIndex</c> again</b>:
    /// an event that is not there has no pages, and a page of a gone event
    /// cannot run and is not a page that does not match.
    /// </remarks>
    public bool IsErased(int pEventId) => _entfernt.Contains(pEventId);

    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        _ereignisse.Count + " events, " + _entfernt.Count + " erased";
}

/// <summary>
/// One state going onto one actor or coming off, and which of the two.
/// </summary>
/// <remarks>
/// <para>
/// <b>And this is what <c>313 Change Actor State</c> records.</b>
/// </para>
/// <para>
/// The engine calls <c>actor.addState(id)</c> or <c>actor.removeState(id)</c>
/// <b>on each actor it walks</b>, and the walk comes from
/// <c>iterateActorEx(target, wholeParty, fn)</c> -- <b>so the same state goes
/// onto every actor of the party when the second parameter says
/// so</b>, <b>and that is not the same as one actor getting it.</b>
/// </para>
/// <para>
/// <b>And nothing here decides whether the actor is dead.</b> The engine
/// reads <c>alreadyDead</c> before the change and collapses only an actor
/// who was alive and is now dead, <b>and this record keeps the change and not
/// the consequence</b> -- <b>because the consequence is a fact about the
/// party and the change is a fact about the command.</b>
/// </para>
/// </remarks>
/// <summary>
/// One HP order, the way <c>311 Change Actor HP</c> gives it.
/// </summary>
/// <remarks>
/// <para>
/// <c>command311</c> is <c>iterateActorEx(params[0], params[1], actor =&gt;
/// this.changeHp(actor, value, params[5]))</c>, and
/// <c>changeHp</c> is
/// <c>if (target.isAlive()) { if (!allowDeath &amp;&amp; target.hp &lt;= -value)
/// { value = 1 - target.hp; } target.gainHp(value); ... }</c>.
/// </para>
/// <para>
/// <b>And two of those five parameters are not what their names
/// suggest.</b> <b>The second is a variable when the first is not
/// zero</b> -- <c>iterateActorEx</c> is <c>if (param1 === 0)
/// { iterateActorId(param2) } else { iterateActorId($gameVariables.value(param2))
/// }</c> -- <b>and the third and fourth are the operation and its
/// operand</b>, <b>so <c>[0, 2, 1, 500]</c> is "every actor, minus
/// five hundred".</b>
/// </para>
/// <para>
/// <b>And the sixth is <c>allowDeath</c>, and it is the one that
/// decides whether a big loss is the loss or one point short of
/// death.</b> <b>This record keeps the order and not the result</b> --
/// <b>the result is a fact about the party's health, and this
/// repository does not keep a party's health.</b>
/// </para>
/// </remarks>
/// <summary>
/// One order to an actor's numbers that this repository records and does
/// not carry out.
/// </summary>
/// <typeparam name="TWert">What the command adds.</typeparam>
/// <remarks>
/// <para>
/// <strong>And <c>312</c>, <c>315</c>, <c>316</c> and <c>317</c> are
/// <c>311</c> with a different one-word method</strong>, <strong>and
/// this is that record without the flag.</strong>
/// </para>
/// <para>
/// <code>
/// command312: const value = this.operateValue(this._params[2],
///            this._params[3], this._params[4]);
///            this.iterateActorEx(this._params[0], this._params[1],
///                actor =&gt; { actor.gainMp(value); });
/// command315: ... actor.changeExp(actor.currentExp() + value, this._params[5]);
/// command316: ... actor.changeLevel(actor.level + value, this._params[5]);
/// command317: ... actor.addParam(this._params[2], value);
/// </code>
/// <para>
/// <strong>And two of the four read the actor's own state and add to
/// it</strong> -- <strong><c>currentExp()</c> and <c>level</c></strong>
/// -- <strong>and this repository keeps neither</strong>, <strong>so it
/// records the change and not the sum.</strong>
/// </para>
/// <para>
/// <strong>And <c>315</c> and <c>316</c> take a sixth parameter that
/// <c>317</c> does not have at all</strong>, <strong>and it is
/// <c>show</c></strong> -- <strong><c>changeExp(exp, show)</c> and
/// <c>changeLevel(level, show)</c> use it for <c>displayLevelUp</c>.</strong>
/// </para>
/// </remarks>
/// <param name="Actor">Which actor it was given to.</param>
/// <param name="Was">What it changed.</param>
/// <param name="Value">What it added.</param>
/// <param name="Show">The engine's own <c>show</c> flag, and zero when the
/// command has no sixth parameter.</param>
public readonly record struct MzActorOrder<TWas>(
    int Actor, TWas Was, int Value, bool Show)
{
    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        "actor " + Actor + " " + Was + (Value < 0 ? " loses " : " gains ")
        + Math.Abs(Value) + (Show ? ", and shows it" : "");
}

public readonly record struct MzHpOrder(
    int Actor, int Value, bool AllowDeath)
{
    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        "actor " + Actor + (Value < 0 ? " loses " : " gains ")
        + Math.Abs(Value) + " hp"
        + (AllowDeath ? "" : ", and may not die of it");
}

/// <summary>
/// A map that is scrolling, and how much is left of it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is <c>$gameMap._scrollRest</c>, and the whole of
/// <c>command204</c> is three numbers and one condition.</strong>
/// </para>
/// <code>
/// startScroll(direction, distance, speed) {
///     this._scrollDirection = direction;
///     this._scrollRest = distance;
///     this._scrollSpeed = speed;
/// }
/// isScrolling() {
///     return this._scrollRest > 0;
/// }
/// scrollDistance() {
///     return Math.pow(2, this._scrollSpeed) / 256;
/// }
/// </code>
/// <para>
/// <strong>And <c>scrollDistance</c> is a dyadic fraction</strong> --
/// <strong>speed one is a half, speed two a quarter, speed three an
/// eighth</strong> -- <strong>and the editor's speeds run from one to
/// eight</strong> -- <strong>and speed eight is 256/256, one whole tile
/// a frame.</strong>
/// </para>
/// <para>
/// <strong>And <c>updateScroll</c> stops when the display refuses to
/// move</strong>: <c>if (this._displayX === lastX &amp;&amp; this._displayY
/// === lastY) { this._scrollRest = 0; }</c> -- <strong>and that is
/// the map's edge, and not a distance.</strong>
/// </para>
/// </remarks>
public sealed class MzMapScroll
{
    /// <summary>The engine's own four directions, and its own values.</summary>
    public const int Down = 2;
    public const int Left = 4;
    public const int Right = 6;
    public const int Up = 8;

    /// <summary>
    /// <c>isScrolling</c>, and <c>this._scrollRest &gt; 0</c>.
    /// </summary>
    public bool Laeuft => _rest > 0;

    /// <summary>How much is left, and the engine's own number.</summary>
    public int Rest => _rest;

    private int _rest;

    private int _richtung;

    private int _geschwindigkeit;

    /// <summary>
    /// <c>startScroll</c>, and the three assignments are all of it.
    /// </summary>
    /// <param name="pRichtung">Two, four, six or eight.</param>
    /// <param name="pDistanz">How far, in tiles.</param>
    /// <param name="pGeschwindigkeit">The speed, and <c>scrollDistance</c>
    /// turns it into tiles per frame.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string Starte(int pRichtung, int pDistanz, int pGeschwindigkeit)
    {
        _richtung = pRichtung;
        _rest = pDistanz;
        _geschwindigkeit = pGeschwindigkeit;
        return "the map scrolls " + Richtung(pRichtung) + " over "
            + pDistanz + " tiles at speed " + pGeschwindigkeit
            + ", which is " + Schritt() + " tiles a frame";
    }

    /// <summary>
    /// <c>scrollDistance</c>, and <c>Math.pow(2, speed) / 256</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a fraction and not a whole number</strong> --
    /// <strong>and a reader that rounded it to a tile would scroll one
    /// tile a frame at every speed</strong>, <strong>and a reader that
    /// kept it as a fraction has to say so</strong>, <strong>because
    /// this reader does not move a display.</strong>
    /// </remarks>
    public double Schritt() =>
        System.Math.Pow(2, _geschwindigkeit) / 256.0;

    /// <summary>
    /// <c>updateScroll</c> for one frame, and whether it stopped because
    /// the display would not move.
    /// </summary>
    /// <param name="pBewegt">
    /// Whether the display actually changed, and this reader does not
    /// have one, so the caller says.
    /// </param>
    /// <returns>Whether it is still scrolling afterwards.</returns>
    public bool EinBild(bool pBewegt)
    {
        if (!Laeuft)
        {
            return false;
        }

        if (!pBewegt)
        {
            // **Und `this._scrollRest = 0` ohne ein Subtrakt** -- **denn
            // der Bildschirm stand still, und das ist die Kante der
            // Karte und nicht eine Entfernung.**
            _rest = 0;
            return false;
        }

        _rest -= (int)Schritt();
        return _rest > 0;
    }

    /// <summary>The engine's own direction names, or the number.</summary>
    /// <param name="pRichtung">The direction.</param>
    /// <returns>One word, or the number when it is not one of four.</returns>
    public static string Richtung(int pRichtung) => pRichtung switch
    {
        Down => "down",
        Left => "left",
        Right => "right",
        Up => "up",
        _ => "direction " + pRichtung,
    };
}

public readonly record struct MzStateChange(
    int Actor, int State, bool Added)
{
    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        "actor " + Actor + (Added ? " gains " : " loses ") + "state " + State;
}
