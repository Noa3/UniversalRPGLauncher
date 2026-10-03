using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace UniversalRPG.Mz;

/// <summary>
/// The number array, as the game's own plugin declares it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not engine code.</strong>
/// <c>Game_NumberArray</c> is defined in
/// <c>js/plugins/Drill_CoreOfNumberArray.js</c>, -- <strong>and that
/// file has 55 178 bytes of which the first 29 774 are comments in
/// Chinese</strong>, -- <strong>and the class starts at line
/// 1614.</strong>
/// </para>
/// <para>
/// <strong>And the plugin says what it is:</strong>
///
/// <code>
/// 1. 刚开始使用的是集群存储法: this._data 中存储的是 {"name":"…"}
///    现在使用的是映射法: 使用 json 根据名称映射到指定的索引
/// </code>
///
/// <strong>And the two methods that carry 224 calls are short, and
/// both differ from what a reader would guess.</strong>
/// </para>
/// <para>
/// <strong>And <c>value</c> answers an empty array, not zero:</strong>
/// </para>
/// <code>
/// value( na_id ) {
///     var index = this.drill_CONA_getArrayIndex( na_id );
///     return this._data[ index ] || [];
/// }
/// </code>
///
/// <para>
/// <strong>And a reader that answered <c>0</c> here would break
/// every <c>for (let i = 0; i &lt; ids.length; i++)</c> the game
/// writes</strong>, -- <strong>because <c>0.length</c> is
/// undefined and the loop never runs</strong>, -- <strong>while the
/// engine gives an array of length 0 and the loop runs zero times
/// cleanly.</strong>
/// </para>
/// <para>
/// <strong>And <c>setValue</c> refuses anything that is not an
/// array</strong>, -- <strong>silently</strong>:
///
/// <code>
/// setValue( na_id, value ) {
///     if( Array.isArray( value ) == false ){ return; }
///     var index = this.drill_CONA_getArrayIndex( na_id );
///     if( index != -1 ){ this._data[ index ] = value; }
/// }
/// </code>
/// </remarks>
public sealed class MzGameNumberArray
{
    private readonly Dictionary<int, IReadOnlyList<int>> _daten = new();
    private readonly Dictionary<string, int> _namenZuIndex = new(
        StringComparer.Ordinal);

    private static readonly Regex NurZiffern =
        new(@"^\d+$", RegexOptions.Compiled);

    /// <summary>And <c>value(na_id)</c>.</summary>
    /// <param name="pId">
    /// A number or a name, -- <strong>and the plugin accepts
    /// both</strong>, -- <strong>and a numeric string is treated as
    /// a number</strong>.
    /// </param>
    /// <returns>
    /// The array, -- <strong>or an empty one</strong>, -- <strong>and
    /// never null and never zero</strong>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>this._data[index] || []</c> is the whole
    /// body</strong>, -- <strong>and the empty array is what makes
    /// <c>ids.length</c> a number</strong>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<int> Value(object? pId)
    {
        var index = Index(pId);
        if (index >= 0 && _daten.TryGetValue(index, out var a))
        {
            return a;
        }

        return Array.Empty<int>();
    }

    /// <summary>And <c>setValue(na_id, value)</c>.</summary>
    /// <param name="pId">A number or a name.</param>
    /// <param name="pWert">The array, -- <strong>and anything else is
    /// refused</strong>.</param>
    /// <returns>
    /// Whether it stored anything, -- <strong>and a refusal is
    /// silent in the plugin</strong>, -- <strong>so this reader says
    /// it out loud instead.</strong>
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And there are two ways to refuse</strong>, -- <strong>a
    /// value that is not an array</strong> and <strong>an id that
    /// resolves to <c>-1</c></strong>.
    /// </para>
    /// </remarks>
    public bool SetValue(object? pId, IReadOnlyList<int>? pWert)
    {
        if (pWert is null)
        {
            return false;
        }

        var index = Index(pId);
        if (index < 0)
        {
            return false;
        }

        _daten[index] = pWert;
        return true;
    }

    /// <summary>And <c>drill_CONA_getArrayIndex(na_id)</c>.</summary>
    /// <param name="pId">A number or a name.</param>
    /// <returns>The index, or -1.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>0</c> is refused</strong>, -- <strong>the
    /// plugin writes <c>if( index &gt; 0 ){ return index; }</c></strong>,
    /// -- <strong>and that is unusual enough to be worth stating,
    /// because every other reader in this repository would accept
    /// zero.</strong> -- <strong>And <c>""</c> and
    /// <c>undefined</c> are refused too.</strong>
    /// </para>
    /// </remarks>
    public int Index(object? pId)
    {
        if (pId is null)
        {
            return -1;
        }

        if (pId is string s)
        {
            if (s.Length == 0)
            {
                return -1;
            }

            // **Und  eine  Ziffernzeichenkette  ist  eine  Zahl.**
            if (!NurZiffern.IsMatch(s))
            {
                return _namenZuIndex.TryGetValue(s, out var n) ? n : -1;
            }
        }

        var zahl = pId switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            string txt => int.Parse(txt, CultureInfo.InvariantCulture),
            _ => -1,
        };

        // **Und  hier  steht  `index > 0`** -- **und  das  heisst:
        //  die  Nummer 0  gibt es nicht.**
        return zahl > 0 ? zahl : -1;
    }

    /// <summary>And <c>drill_CONA_init</c> gives a name its index.</summary>
    /// <param name="pName">The name the game writes.</param>
    /// <param name="pIndex">The number behind it.</param>
    public void Nenne(string pName, int pIndex) => _namenZuIndex[pName] = pIndex;

    /// <summary>And <c>clear()</c>, and it is one line in the plugin.</summary>
    public void Clear()
    {
        _daten.Clear();
        _namenZuIndex.Clear();
    }

    /// <summary>And how many arrays are stored.</summary>
    /// <returns>The count, -- <strong>and for a test, and the engine
    /// keeps no such number.</strong></returns>
    public int Anzahl => _daten.Count;

    /// <summary>And the names that resolve.</summary>
    /// <returns>The names, -- <strong>and the plugin maps them from
    /// JSON at load time.</strong></returns>
    public IReadOnlyCollection<string> Namen() => _namenZuIndex.Keys;

    /// <summary>And the reader names what it answers.</summary>
    /// <returns>The method names.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the plugin has six</strong>, -- <strong>and the
    /// two the game calls are <c>value</c> and <c>setValue</c></strong>,
    /// -- <strong>and the private <c>drill_CONA_</c> ones are
    /// internal.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<string> BekannteMethoden() =>
        new[] { "value", "setValue" };
}
