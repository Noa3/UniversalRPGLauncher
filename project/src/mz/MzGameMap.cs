using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Mz;

/// <summary>
/// The map object the games scripts ask about, as the engine declares
/// it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every method in here was read out of the game's own
/// <c>rpg_objects.js</c></strong>, -- <strong>not from a doc and
/// not from memory</strong>, and that file writes its classes as
/// <c>var Game_Map = class {</c>, which is why a search for
/// <c>function Game_Map</c> finds nothing.
/// </para>
/// <para>
/// <strong>And the three methods that carry 1000 of the 1187
/// <c>$gameMap</c> calls in this game are one line each</strong>:
///
/// <code>
/// event(eventId) { return this._events[eventId]; }
/// events()        { return this._events.filter(e =&gt; !!e); }
/// mapId()         { return this._mapId; }
/// </code>
///
/// <strong>And <c>mapId()</c> is written 130 times and
/// <c>event()</c> 870 times.</strong>
/// </para>
/// <para>
/// <strong>And what <c>event()</c> returns is
/// <c>undefined</c> when the id has no event</strong>, -- <strong>and
/// the game writes <c>$gameMap.event(18)?.start()</c> with the safe
/// navigation operator on exactly that line</strong>, -- <strong>and
/// a reader that answered <c>null</c> where the engine answers
/// <c>undefined</c> would break that line.</strong>
/// </para>
/// </remarks>
public sealed class MzGameMap
{
    private readonly Dictionary<int, MzGameEvent> _ereignisse = new();

    /// <summary>And the map's own number.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is <c>this._mapId</c> and nothing
    /// else</strong>, -- <strong>and <c>setup(mapId)</c> writes it and
    /// nothing else does.</strong>
    /// </para>
    /// </remarks>
    public int MapId { get; set; }

    /// <summary>And the map's own name.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>And its width in tiles.</summary>
    public int Width { get; set; }

    /// <summary>And its height in tiles.</summary>
    public int Height { get; set; }

    /// <summary>
    /// And <c>event(eventId)</c>, and it is what the engine writes.
    /// </summary>
    /// <param name="pId">The event id.</param>
    /// <returns>
    /// The event, or null when the id has none, -- <strong>and null is
    /// <c>undefined</c> here</strong>, because that is what
    /// <c>this._events[id]</c> returns.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And a JavaScript array gives <c>undefined</c> for an
    /// index it does not have</strong>, -- <strong>and the game
    /// writes <c>$gameMap.event(18)?.start()</c></strong>, --
    /// <strong>which is safe navigation and is exactly the call that
    /// would throw on null.</strong>
    /// </para>
    /// </remarks>
    public MzGameEvent? Event(int pId) =>
        _ereignisse.TryGetValue(pId, out var e) ? e : null;

    /// <summary>
    /// And <c>events()</c>, and it filters what is not there.
    /// </summary>
    /// <returns>
    /// The events that exist, -- <strong>and the engine filters
    /// falsy ones out</strong>, -- <strong>and that is why the count
    /// here is smaller than the array length.</strong>
    /// </returns>
    public IReadOnlyList<MzGameEvent> Events() =>
        _ereignisse.Values.Where(e => e != null).ToList();

    /// <summary>And every id that has an event.</summary>
    /// <returns>The ids, and they are the keys.</returns>
    public IReadOnlyCollection<int> EventIds() => _ereignisse.Keys;

    /// <summary>And puts one event in, and the map is built by this.</summary>
    /// <param name="pId">Its id.</param>
    /// <param name="pEreignis">The event.</param>
    public void Setze(int pId, MzGameEvent pEreignis) => _ereignisse[pId] = pEreignis;

    /// <summary>
    /// And the ones a script can answer without asking further.
    /// </summary>
    /// <returns>The method names this class answers.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured against the game's own
    /// scripts</strong>, -- <strong>and it is written here so that a
    /// name this class does not answer is visible instead of
    /// silently wrong.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<string> BekannteMethoden() =>
        new[] { "event", "events", "mapId", "displayName", "width", "height" };
}

/// <summary>
/// One event on a map, as far as a script can see it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the fields here are the ones the game's scripts
/// write</strong>, -- <strong>and not the ones RPG Maker documents</strong>,
/// -- <strong>because the game writes
/// <c>$gameMap.event(this._eventId)._opacity = 255</c> 23 times.</strong>
/// </para>
/// </remarks>
public sealed class MzGameEvent
{
    /// <summary>And its own number.</summary>
    public int Id { get; set; }

    /// <summary>And the name a script may read.</summary>
    public string Name { get; set; } = "";

    /// <summary>And which page the game last read from it.</summary>
    public int ActivePage { get; set; }

    /// <summary>And the transparency, 0 to 255.</summary>
    public int Opacity { get; set; } = 255;

    /// <summary>And the movement speed, and 0 is "stopped".</summary>
    public int MoveSpeed { get; set; }

    /// <summary>And whether its pages run without being touched.</summary>
    public bool Parallel { get; set; }

    /// <summary>And whether it starts when the map is entered.</summary>
    public bool Autorun { get; set; }

    /// <summary>
    /// And the script of one page, and null when it has none.
    /// </summary>
    /// <param name="pIndex">Which page, counting from one.</param>
    /// <returns>The list, and it may be empty.</returns>
    public IReadOnlyList<string>? Seite(int pIndex) =>
        pIndex >= 1 && pIndex <= Seiten.Count ? Seiten[pIndex - 1] : null;

    /// <summary>And every page's commands, as the game's own texts.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Seiten { get; init; } =
        Array.Empty<IReadOnlyList<string>>();
}
