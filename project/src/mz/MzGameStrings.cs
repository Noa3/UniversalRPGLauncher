using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Mz;

/// <summary>
/// The string table, as the game's own plugin declares it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not engine code.</strong> <c>Game_Strings</c>
/// is in <c>js/plugins/Drill_CoreOfString.js</c>, -- <strong>and in
/// none of the nine <c>*.js</c> files that make up the
/// engine</strong>. -- <strong>And it appears in twenty-four files
/// of this game and in no engine file at all.</strong>
/// </para>
/// <para>
/// <strong>And the value semantics are the third distinct one in
/// this repository</strong>:
///
/// <code>
/// value( stringId )   { return this._data[stringId] || ""; }
/// setValue( id, value ) { if( id > 0 ) { this._data[id] = String(value); } }
/// </code>
///
/// <list type="bullet">
/// <item><strong><c>$gameMap.event()</c> answers
/// <c>undefined</c></strong>, -- <strong>and the game writes a
/// question mark.</strong></item>
/// <item><strong><c>$gameSelfVariables.value()</c> answers
/// <c>0</c></strong>, -- <strong>and the game writes none.</strong></item>
/// <item><strong><c>$gameStrings.value()</c> answers <c>""</c></strong>,
/// -- <strong>and the game writes none.</strong></item>
/// </list>
/// <para>
/// <strong>And <c>setValue</c> refuses the number zero</strong>, --
/// <strong>because the plugin writes <c>if( stringId &gt; 0 )</c></strong>,
/// -- <strong>and that is the same refusal
/// <c>$gameNumberArray</c> makes.</strong>
/// </para>
/// <para>
/// <strong>And slot zero holds the empty string on purpose</strong>, --
/// <strong>the plugin writes <c>var temp_tank = [""];</c> and the
/// comment says so</strong>: <c>（第0个为空字符串）</c>.
/// </para>
/// </remarks>
public sealed class MzGameStrings
{
    private readonly Dictionary<int, string> _daten = new();

    /// <summary>And <c>value(stringId)</c>.</summary>
    /// <param name="pId">The number.</param>
    /// <returns>
    /// The text, -- <strong>or an empty string</strong>, -- <strong>and
    /// never null.</strong>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>this._data[stringId] || ""</c> is the whole
    /// body</strong>, -- <strong>and the empty string is what makes
    /// <c>.length</c> a number.</strong>
    /// </para>
    /// </remarks>
    public string Value(int pId) =>
        _daten.TryGetValue(pId, out var w) ? w : "";

    /// <summary>And <c>setValue(stringId, value)</c>.</summary>
    /// <param name="pId">The number, -- <strong>and zero is
    /// refused</strong>.</param>
    /// <param name="pWert">The text, -- <strong>and it is turned into
    /// a string whatever it is</strong>.</param>
    /// <returns>Whether it stored anything.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the plugin writes <c>String(value)</c></strong>, --
    /// <strong>and the game hands it numbers</strong>, --
    /// <strong>and a reader that stored the raw number would answer
    /// something the engine never stored.</strong>
    /// </para>
    /// </remarks>
    public bool SetValue(int pId, object? pWert)
    {
        if (pId <= 0)
        {
            return false;
        }

        _daten[pId] = WieText(pWert);
        return true;
    }

    /// <summary>And <c>convertedValue(stringId)</c>, unprepared.</summary>
    /// <param name="pId">The number.</param>
    /// <returns>
    /// The text with the game's own placeholders untouched, -- <strong>
    /// and this reader does not run the replacement</strong>, --
    /// <strong>and says so rather than pretending.</strong>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And the plugin calls
    /// <c>DataManager.drill_COSt_replaceChar(value)</c></strong>, --
    /// <strong>and that is a Drill function this repository has not
    /// read.</strong> -- <strong>And the plugin answers <c>""</c> for
    /// a falsy value</strong>, -- <strong>and that part is
    /// reproduced.</strong>
    /// </para>
    /// </remarks>
    public string ConvertedValue(int pId)
    {
        var wert = Value(pId);
        return string.IsNullOrEmpty(wert) ? "" : wert;
    }

    /// <summary>And <c>clear()</c>, and it is one line in the plugin.</summary>
    public void Clear() => _daten.Clear();

    /// <summary>And how many strings are stored.</summary>
    /// <returns>The count, -- <strong>and for a test.</strong></returns>
    public int Anzahl => _daten.Count;

    private static string WieText(object? pWert) => pWert switch
    {
        null => "",
        string s => s,
        int i => i.ToString(CultureInfo.InvariantCulture),
        long l => l.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        _ => pWert.ToString() ?? "",
    };
}
