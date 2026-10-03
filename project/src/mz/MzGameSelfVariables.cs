using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace UniversalRPG.Mz;

/// <summary>
/// The self variables of one event, as the game's own plugin declares
/// them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not engine code.</strong>
/// <c>Game_SelfVariables</c> is defined in
/// <c>js/plugins/Iavra Self Variables.js</c>, -- <strong>not in any
/// of the ten <c>rpg_*.js</c> files</strong>, -- <strong>and that
/// plugin's own comment says so:</strong>
/// </para>
/// <code>
/// This is basically a copy of Game_SelfSwitches,
/// mixed with Game_Variables for correct value handling.
/// </code>
/// <para>
/// <strong>And that matters twice.</strong> First, the semantics
/// come from that file and not from RPG Maker's documentation. Second,
/// <strong>a game without this plugin has no <c>$gameSelfVariables</c>
/// at all</strong>, -- <strong>and this repository must not answer
/// a name that the game did not declare.</strong>
/// </para>
/// <para>
/// <strong>And the value semantics are the whole point:</strong>
/// </para>
/// <code>
/// value(key)       { return this._data[key] || 0; }
/// setValue(k, v)   { v = _parseInt(v); this._data[k] = v; }
/// get(it, key)     { return this.value([it._mapId, it._eventId, key]); }
/// </code>
/// <para>
/// <strong>And <c>value</c> answers <c>0</c> for a key that is not
/// there</strong>, -- <strong>and never <c>undefined</c></strong>,
/// -- <strong>and that is the opposite of
/// <c>$gameMap.event()</c></strong>, -- <strong>which answers
/// <c>undefined</c> and makes the game write a question
/// mark.</strong>
/// </para>
/// </remarks>
public sealed class MzGameSelfVariables
{
    private readonly Dictionary<string, int> _daten = new(StringComparer.Ordinal);

    /// <summary>And the last map id this object was told about.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is set from outside</strong>, -- <strong>and
    /// the plugin writes <c>$gameMap.requestRefresh()</c> in
    /// <c>onChange</c></strong>, -- <strong>so the object does not
    /// read the map itself.</strong>
    /// </para>
    /// </remarks>
    public int MapId { get; set; }

    /// <summary>
    /// And <c>value(key)</c>, and a missing key is zero.
    /// </summary>
    /// <param name="pKey">The key, which the game writes as an array.</param>
    /// <returns>
    /// The number, or zero, -- <strong>and never -1 and never
    /// nothing</strong>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>this._data[key] || 0</c> is the whole
    /// body</strong>, -- <strong>and <c>||</c> also turns a stored
    /// zero into zero</strong>, -- <strong>which is harmless here
    /// because zero is what a missing key answers anyway.</strong>
    /// </para>
    /// </remarks>
    public int Value(IReadOnlyList<object> pKey) =>
        _daten.TryGetValue(Schluessel(pKey), out var w) ? w : 0;

    /// <summary>
    /// And <c>setValue(key, value)</c>.
    /// </summary>
    /// <param name="pKey">The key.</param>
    /// <param name="pWert">The number to store.</param>
    /// <remarks>
    /// <para>
    /// <strong>And the plugin parses the value first</strong>, --
    /// <strong>and a reader that stored the raw value would answer
    /// differently for a string the game handed
    /// it.</strong>
    /// </para>
    /// </remarks>
    public void SetValue(IReadOnlyList<object> pKey, object pWert) =>
        _daten[Schluessel(pKey)] = Zahl(pWert);

    /// <summary>
    /// And <c>addValue(key, value)</c>.
    /// </summary>
    /// <param name="pKey">The key.</param>
    /// <param name="pWert">How much to add, and it may be negative.</param>
    /// <remarks>
    /// <para>
    /// <strong>And it reads the current value first</strong>, --
    /// <strong>so adding to a key that is not there adds to
    /// zero</strong>.
    /// </para>
    /// </remarks>
    public void AddValue(IReadOnlyList<object> pKey, object pWert) =>
        SetValue(pKey, Value(pKey) + Zahl(pWert));

    /// <summary>
    /// And <c>get(interpreter, key)</c>, and that is a map id, an event
    /// id and a name.
    /// </summary>
    /// <param name="pMapId">The map the event is on.</param>
    /// <param name="pEventId">The event's own number.</param>
    /// <param name="pName">What the game calls it.</param>
    /// <returns>The number, and zero when there is none.</returns>
    public int Get(int pMapId, int pEventId, string pName) =>
        Value(new object[] { pMapId, pEventId, pName });

    /// <summary>And <c>set(interpreter, key, value)</c>.</summary>
    /// <param name="pMapId">The map the event is on.</param>
    /// <param name="pEventId">The event's own number.</param>
    /// <param name="pName">What the game calls it.</param>
    /// <param name="pWert">The number to store.</param>
    public void Set(int pMapId, int pEventId, string pName, object pWert) =>
        SetValue(new object[] { pMapId, pEventId, pName }, pWert);

    /// <summary>And <c>add(interpreter, key, value)</c>.</summary>
    /// <param name="pMapId">The map the event is on.</param>
    /// <param name="pEventId">The event's own number.</param>
    /// <param name="pName">What the game calls it.</param>
    /// <param name="pWert">How much to add.</param>
    public void Add(int pMapId, int pEventId, string pName, object pWert) =>
        AddValue(new object[] { pMapId, pEventId, pName }, pWert);

    /// <summary>And how many keys are stored.</summary>
    /// <returns>The count.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is for a test and not for the
    /// game</strong>, -- <strong>and the engine keeps no such
    /// number.</strong>
    /// </para>
    /// </remarks>
    public int Anzahl => _daten.Count;

    /// <summary>And the key the game writes, joined with commas.</summary>
    /// <param name="pKey">The three or one parts.</param>
    /// <returns>The stored key.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And JavaScript turns an array into a string with
    /// commas</strong>, -- <strong>and that is exactly what
    /// <c>clearVariablesForEvent</c> splits again</strong> --
    /// <c>var storedKey = key.split(',')</c> -- <strong>and this
    /// reader writes the same string so that both
    /// agree.</strong>
    /// </para>
    /// </remarks>
    private static string Schluessel(IReadOnlyList<object> pKey) =>
        string.Join(",", pKey.Select(WieText).ToArray());

    private static string WieText(object pWert) => pWert switch
    {
        int i => i.ToString(CultureInfo.InvariantCulture),
        string s => s,
        _ => pWert?.ToString() ?? "",
    };

    private static int Zahl(object pWert) => pWert switch
    {
        int i => i,
        long l => (int)l,
        double d => (int)d,
        string s => int.TryParse(s, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var g) ? g : 0,
        _ => 0,
    };
}
