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
public readonly record struct MzHpOrder(
    int Actor, int Value, bool AllowDeath)
{
    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        "actor " + Actor + (Value < 0 ? " loses " : " gains ")
        + Math.Abs(Value) + " hp"
        + (AllowDeath ? "" : ", and may not die of it");
}

public readonly record struct MzStateChange(
    int Actor, int State, bool Added)
{
    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        "actor " + Actor + (Added ? " gains " : " loses ") + "state " + State;
}
