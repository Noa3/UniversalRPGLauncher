using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UniversalRPG.Web;

namespace UniversalRPG.Mz;

/// <summary>
/// The lines of a script block that this repository answers, and the
/// ones it does not.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the engine writes <c>eval(script)</c></strong>, --
/// <strong>and this repository does not run the author's
/// JavaScript</strong>, -- <strong>and that is a boundary it keeps on
/// purpose, not a gap in it.</strong>
/// </para>
/// <code>
/// command355() {
///     let script = this.currentCommand().parameters[0] + '\n';
///     while (this.nextEventCode() === 655) {
///         this._index++;
///         script += this.currentCommand().parameters[0] + '\n';
///     }
///     eval(script);
///     return true;
/// }
/// </code>
/// <para>
/// <strong>And what is measured in this game:</strong>
///
/// <code>
/// 10111 script commands
///  5471 of them carry a 655 continuation line
///   587 lines mention $gameSelfVariables
///   371 of those fit three shapes
/// </code>
/// </para>
/// <para>
/// <strong>And the three shapes are the whole of the counter</strong>,
/// -- <strong>and they are what the game writes 371 times:</strong>
///
/// <code>
/// $gameSelfVariables.set(this, 'frames', 0);
/// let id = $gameSelfVariables.get(this, 'frames');
/// $gameSelfVariables.add(this, 'frames', 1)
/// </code>
/// </para>
/// </remarks>
public sealed class MzSkriptBlock
{
    private readonly Dictionary<string, long> _werte =
        new(StringComparer.Ordinal);

    private static readonly Regex Form = new(
        @"^[\s""]*(?:(?:let|var|const)\s+\w+\s*=\s*)?" +
        @"\$gameSelfVariables\.(get|set|add|value)\s*\(" +
        @"\s*this\s*,\s*'(?<schluessel>\w+)'\s*,?\s*" +
        @"(?<rest>[^""]*?)\s*\)\s*" +
        @"(?<ende>[+-]\s*\d+\s*)?;?\s*[\s""]*$",
        RegexOptions.Compiled);

    /// <summary>And the map the event is on.</summary>
    public int MapId { get; set; }

    /// <summary>And the event's own number.</summary>
    public int EventId { get; set; }

    /// <summary>How many lines this class understood.</summary>
    /// <returns>The count.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the number that matters</strong>, -- <strong>
    /// because 10111 lines are 355 commands and only 371 of the
    /// 587 self-variable lines are in a shape a reader can
    /// answer.</strong>
    /// </para>
    /// </remarks>
    public int Verstanden { get; private set; }

    /// <summary>How many lines this class did not understand.</summary>
    /// <returns>The count.</returns>
    public int NichtVerstanden { get; private set; }

    /// <summary>
    /// And it runs one line, and only the lines it knows.
    /// </summary>
    /// <param name="pZeile">The line, as the game wrote it.</param>
    /// <returns>Whether it did anything with it.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a line it does not know is counted and not
    /// guessed.</strong> -- <strong>And that is what makes the number
    /// above trustworthy.</strong>
    /// </para>
    /// </remarks>
    public bool Verarbeite(
        string pZeile, MzBranchFacts? pFakten = null)
    {
        var m = Form.Match(pZeile);
        if (!m.Success)
        {
            if (pZeile.Contains("$gameSelfVariables", StringComparison.Ordinal))
            {
                NichtVerstanden++;
            }

            return false;
        }

        var methode = m.Groups[1].Value;
        var schluessel = m.Groups["schluessel"].Value;
        var rest = m.Groups["rest"].Value.Trim();
        var ende = m.Groups["ende"].Value.Trim();

        var alter = Lese(_werte, schluessel);

        switch (methode)
        {
            case "set":
            {
                // **Und  das  Spiel  schreibt  auch
                //  `set(this, 'frames', get(this, 'frames') + 1)`,
                //  und  das  ist  kein  Ausdruck  fuer  `Zahl`,
                //  und  darum  muss  der  Ausdruck  ZUERST
                //  geprueft  werden.**
                if (rest.Contains("get(this", StringComparison.Ordinal))
                {
                    var op = rest.Contains("+ 1", StringComparison.Ordinal)
                        || rest.EndsWith("+ 1)", StringComparison.Ordinal)
                        ? 1
                        : rest.Contains("- 1", StringComparison.Ordinal)
                            || rest.EndsWith("- 1)", StringComparison.Ordinal)
                            ? -1
                            : 0;
                    _werte[schluessel] = alter + op;
                }
                else if (Zahl(rest, out var wert))
                {
                    _werte[schluessel] = wert;
                }
                else if (pFakten != null
                    && MzArithmetic.TryRead(
                        rest, pFakten, out _) is double ausVar)
                {
                    // **Und  das  Spiel  schreibt  auch
                    //  `set(this, 'frames', $gameVariables.value(3))`,
                    //  und  das  ist  keine  Zahl  und  kein
                    //  Ausdruck  ueber  `get(this, ...)`.**
                    _werte[schluessel] = (long)ausVar;
                }
                else
                {
                    NichtVerstanden++;
                    return false;
                }

                Verstanden++;
                return true;
            }

            case "add":
            {
                if (!Zahl(rest, out var schritt))
                {
                    NichtVerstanden++;
                    return false;
                }

                _werte[schluessel] = alter + schritt;
                Verstanden++;
                return true;
            }

            case "get":
            case "value":
                Verstanden++;
                return true;

            default:
                NichtVerstanden++;
                return false;
        }
    }

    /// <summary>
    /// And the value of one key.
    /// </summary>
    /// <summary>And it starts from a number, not from zero.</summary>
    /// <param name="pSchluessel">The name.</param>
    /// <param name="pWert">Where it stands.</param>
    /// <remarks>
    /// <para>
    /// <strong>And this is what the interpreter needs.</strong> The
    /// engine's counter is one number in one object, -- <strong>and
    /// two 355 blocks in a row are that same number.</strong> --
    /// <strong>And a block that began at zero would read the
    /// second as the first.</strong>
    /// </para>
    /// </remarks>
    public void Setze(string pSchluessel, long pWert) =>
        _werte[pSchluessel] = pWert;

    /// <summary>
    /// And the value of one key.
    /// <returns>The number, and zero when there is none.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And zero, not nothing</strong>, -- <strong>because the
    /// plugin writes <c>this._data[key] || 0</c></strong>.
    /// </para>
    /// </remarks>
    public long Lese(string pSchluessel) =>
        Lese(_werte, pSchluessel);

    private static long Lese(
        Dictionary<string, long> pWerte, string pSchluessel) =>
        pWerte.TryGetValue(pSchluessel, out var w) ? w : 0;

    private static bool Zahl(string pText, out long pWert)
    {
        pWert = 0;
        var s = pText.Trim().TrimStart(',', ' ').TrimEnd(',', ';', ')', ' ').Trim();
        return long.TryParse(s, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out pWert);
    }
}
