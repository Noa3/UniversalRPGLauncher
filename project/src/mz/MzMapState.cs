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