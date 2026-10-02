using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// One arithmetic comparison, read out of a game's own event list.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not a JavaScript interpreter and it is not an expression
/// engine.</b> It reads a grammar of eleven operators and one function
/// call, <b>and it refuses everything it does not know</b> -- because a
/// reader that answered an unknown expression would be claiming a result
/// it did not compute.
/// </para>
/// <para>
/// <b>And the grammar is not invented, and this is the measurement.</b>
/// At <c>D:/Itch/sister/www</c>, of the 449 conditions that read only
/// <c>$gameVariables.value</c> and numbers, <b>364 are pure arithmetic</b>
/// and they use no other operator than these:
///
/// <code>
///   309x &lt;      308x &lt;=     211x &gt;      210x &gt;=
///   205x &amp;&amp;      35x ||       143x +       138x *
///   136x **       136x -        1x ==        1x !=
///    1x ===       1x !==
///
/// -- and 44 different expressions in all.
/// </code>
///
/// <b>And the commonest of the 364 is a circle</b>, <b>and it appears
/// twelve to sixteen times each</b>:
///
/// <code>
/// ($gameVariables.value(4) - 1107) ** 2 + ($gameVariables.value(5) - 612) ** 2
///     &lt;= 113 ** 2
/// </code>
///
/// <b>That is a radius around a point on the map</b>, <b>and it is
/// arithmetic and not a computation over anything this reader does not
/// have.</b>
/// </para>
/// <para>
/// <b>And <c>Variables</c> is a plain dictionary of numbers</b>, <b>and a
/// variable the game has not written is zero in JavaScript and zero
/// here</b> -- <strong>which is the engine's own
/// <c>$gameVariables.prototype.value</c>: <c>return this._data[index] ||
/// 0;</c></strong>
/// </para>
/// <para>
/// <b>And what it will not read, and says so:</b> a call that is not
/// <c>$gameVariables.value</c>, a name, a string, a member access, an
/// assignment, a function definition, a ternary, a template.
/// <strong>Every one of those is refused with the text that caused
/// it</strong>, <b>because a refusal nobody can act on is a refusal
/// nobody will act on.</strong>
/// </para>
/// </remarks>
public sealed class MzArithmetic
{
    private readonly MzBranchFacts _fakten;

    private MzArithmetic(MzBranchFacts pFakten)
    {
        _fakten = pFakten;
    }

    /// <summary>
    /// Reads one expression, and says where it stopped when it cannot.
    /// </summary>
    /// <param name="pText">The author's expression.</param>
    /// <param name="pFakten">Where the numbers come from.</param>
    /// <param name="pMissing">Why not, and empty when it worked.</param>
    /// <returns>
    /// What the expression came to, or <c>null</c>.
    /// </returns>
    public static double? TryRead(
        string pText, MzBranchFacts pFakten, out string pMissing)
    {
        pMissing = "";
        var text = pText ?? "";
        if (text.Trim().Length == 0)
        {
            pMissing = "an empty expression";
            return null;
        }

        var leser = new MzArithmetic(pFakten);
        leser._text = text;
        leser._stelle = 0;
        try
        {
            var wert = leser.LeseUnd();
            leser.LueckeRest();
            if (leser._fehler.Length > 0)
            {
                pMissing = leser._fehler;
                return null;
            }

            return wert;
        }
        catch (FormatException ausnahme)
        {
            pMissing = ausnahme.Message;
            return null;
        }
    }

    /// <summary>
    /// The operators of two characters, and the list is in this order.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>&amp;&amp;</c> and <c>||</c> are not here</strong>,
    /// <strong>because they are the only place where the reading stops and
    /// starts an arm of a condition</strong>, <strong>and they are longer
    /// than two characters.</strong>
    /// </remarks>
    private static readonly string[] ZweiZeichen =
    {
        "**", "===", "!==", "<=", ">=", "==", "!=", "&&", "||",
    };

    private string _text = "";
    private int _stelle;
    private string _fehler = "";

    /// <summary>The comparison at the top, and it is the only place where
    /// <c>&amp;&amp;</c> and <c>||</c> live.</summary>
    private double LeseUnd()
    {
        var links = LeseOder();
        while (true)
        {
            var op = NaechsteOperator();
            if (op != "&&")
            {
                // **Und ein `||` ist hier kein Fehler**, **denn `||`
                // eine Ebene tiefer steht und dieser Aufruf sie
                // zurueckgegeben hat.** **Und `LeseOder` nimmt ihn, und
                // wenn er nicht `||` ist, ist er nichts, was diese Ebene
                // verstehen kann.**
                if (op.Length > 0 && _fehler.Length == 0)
                {
                    _fehler = "a '" + op + "' where && belongs, at \""
                        + BisHier() + "\"";
                }

                return links;
            }

            _stelle += op.Length;

            // **Und JavaScripts `&&` gibt den rechten Wert zurueck und nicht
            // nur ein Wahrheitswert** -- **und die erste Zahl hier ist, was
            // `&&` zurueckgibt, und es ist in diesem Spiel immer eine Zahl.**
            var rechts = LeseOder();
            links = IstWahr(links) ? rechts : links;
        }
    }

    /// <summary><c>||</c>, and it binds tighter than <c>&amp;&amp;</c>.</summary>
    private double LeseOder()
    {
        var links = LeseVergleich();
        while (true)
        {
            var op = NaechsteOperator();
            if (op != "||")
            {
                // **Und ein `&&` ist hier kein Fehler**: **es gehoert einer
                // Ebene hoeher, es wurde nicht gegessen, und `LeseUnd`
                // sieht es gleich.**
                //
                // **Und die erste Fassung hat es als Fehler gemeldet**, **und
                // damit 205 von 364 Ausdruecken dieses Spiels abgelehnt** --
                // **und zwar alle, in denen `&&` oder `||` vorkommt.**
                if (op.Length > 0 && op != "&&" && _fehler.Length == 0)
                {
                    _fehler = "a '" + op + "' where || belongs, at \""
                        + BisHier() + "\"";
                }

                return links;
            }

            _stelle += op.Length;
            var rechts = LeseVergleich();
            links = IstWahr(links) ? links : rechts;
        }
    }

    /// <summary>
    /// One comparison, and a comparison is two sides and an operator.
    /// </summary>
    /// <remarks>
    /// <strong>And a bare number is a truth value and not an error</strong>,
    /// <strong>because <c>if (x)</c> is legal JavaScript and this game
    /// could have written it.</strong>
    /// </remarks>
    private double LeseVergleich()
    {
        var links = LeseSumme();
        var op = NaechsteOperator();
        if (op.Length == 0)
        {
            return links;
        }

        if (!IstVergleich(op))
        {
            // **Und ein `+` ist an dieser Stelle kein Fehler**, **denn
            // `1 + 2` ist eine Rechnung und kein Vergleich**, **und die
            // Ebenen darunter haben ihn nicht genommen.**
            if (_fehler.Length == 0 && !GehoertEinerHoeheren(op))
            {
                _fehler = "a '" + op + "' where || belongs, at \""
                    + BisHier() + "\"";
            }

            return links;
        }

        _stelle += op.Length;
        var rechts = LeseSumme();
        return Vergleich(op, links, rechts);
    }

    /// <summary><c>+</c> and <c>-</c>.</summary>
    private double LeseSumme()
    {
        var wert = LeseProdukt();
        while (true)
        {
            var op = NaechsteOperator();
            if (op != "+" && op != "-")
            {
                if (op.Length > 0 && !IstVergleich(op) && op != "&&"
                    && op != "||" && !GehoertEinerHoeheren(op)
                    && _fehler.Length == 0)
                {
                    _fehler = "a '" + op + "' where + belongs, at \""
                        + BisHier() + "\"";
                }

                return wert;
            }

            _stelle += op.Length;
            var rechts = LeseProdukt();
            wert = op == "+" ? wert + rechts : wert - rechts;
        }
    }

    /// <summary><c>*</c>, <c>/</c> and <c>%</c>.</summary>
    private double LeseProdukt()
    {
        var wert = LesePotenz();
        while (true)
        {
            var op = NaechsteOperator();
            if (op != "*" && op != "/" && op != "%")
            {
                if (op.Length > 0 && !IstVergleich(op) && op != "&&"
                    && op != "||" && !GehoertEinerHoeheren(op)
                    && _fehler.Length == 0)
                {
                    _fehler = "a '" + op + "' where * belongs, at \""
                        + BisHier() + "\"";
                }

                return wert;
            }

            _stelle += op.Length;
            var rechts = LesePotenz();
            // **Und eine Division durch null ist in JavaScript nicht ein
            // Fehler**, **und sie ergibt `Infinity` oder `NaN`**, **und ein
            // Leser, der eine Ausnahme warf, wuerde eine Bedingung ablehnen,
            // die die Engine annimmt.**
            wert = op switch
            {
                "*" => wert * rechts,
                "/" => rechts == 0
                    ? (wert == 0 ? double.NaN : wert / rechts)
                    : wert / rechts,
                _ => rechts == 0 ? double.NaN : wert % rechts,
            };
        }
    }

    /// <summary>
    /// <c>**</c>, and it binds tighter than <c>*</c> in JavaScript.
    /// </summary>
    /// <remarks>
    /// <strong>And it was measured 136 times in this game</strong> --
    /// <strong>always with a small number on the right, and always as a
    /// square.</strong>
    /// </remarks>
    private double LesePotenz()
    {
        var basis = LeseFaktor();
        var op = NaechsteOperator();
        if (op != "**")
        {
            return basis;
        }

        _stelle += op.Length;
        var exponent = LesePotenz();
        // **Und eine negative Basis mit gebrochenem Exponent ist in
        // JavaScript `NaN`**, **und `Math.pow` sagt das auch.**
        return Math.Pow(basis, exponent);
    }

    /// <summary>A number, a bracket, or the one call this reader knows.</summary>
    private double LeseFaktor()
    {
        Luecke();
        if (_stelle >= _text.Length)
        {
            _fehler = "the expression ends where a value belongs";
            return 0;
        }

        var c = _text[_stelle];
        if (c == '-')
        {
            _stelle++;
            return -LeseFaktor();
        }

        if (c == '+')
        {
            _stelle++;
            return LeseFaktor();
        }

        if (c == '(')
        {
            _stelle++;
            var wert = LeseUnd();
            Luecke();
            if (_stelle < _text.Length && _text[_stelle] == ')')
            {
                _stelle++;
            }
            else
            {
                _fehler = "a bracket that is never closed, at \""
                    + BisHier() + "\"";
            }

            return wert;
        }

        if (c == '!' && _stelle + 1 < _text.Length
            && _text[_stelle + 1] != '=')
        {
            _stelle++;
            return IstWahr(LeseFaktor()) ? 0 : 1;
        }

        // **Und `$gameVariables.value(N)` ist der einzige Aufruf, den
        // dieser Leser kennt** -- **und gemessen an `command122` ist es
        // auch der, den das Spiel schreibt.**
        if (_text[_stelle] == '$')
        {
            return LeseAufruf();
        }

        if (char.IsDigit(c) || c == '.')
        {
            var anfang = _stelle;
            while (_stelle < _text.Length
                && (char.IsDigit(_text[_stelle]) || _text[_stelle] == '.'))
            {
                _stelle++;
            }

            // **Und eine Flusszahl mit einem `e`** -- **denn JSON schreibt
            // sie so und `MzCommandEntry` laesst sie durch.**
            if (_stelle < _text.Length
                && (_text[_stelle] == 'e' || _text[_stelle] == 'E'))
            {
                var merke = _stelle;
                _stelle++;
                if (_stelle < _text.Length
                    && (_text[_stelle] == '+' || _text[_stelle] == '-'))
                {
                    _stelle++;
                }

                if (_stelle < _text.Length && char.IsDigit(_text[_stelle]))
                {
                    while (_stelle < _text.Length
                        && char.IsDigit(_text[_stelle]))
                    {
                        _stelle++;
                    }
                }
                else
                {
                    _stelle = merke;
                }
            }

            var text = _text.Substring(
                anfang, _stelle - anfang);
            if (double.TryParse(
                text, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var zahl))
            {
                return zahl;
            }

            _fehler = "'" + text + "' is not a number";
            return 0;
        }

        // **Und alles andere ist ein Name und wird abgelehnt**, **denn ein
        // Name ist eine Sache, die dieser Leser nicht hat, und nicht
        // etwa ein Name, den man raten koennte.**
        var name = NaechsterName();
        _fehler = "'" + name + "', which is a name or a call this reader "
            + "does not know, and this reader reads numbers, "
            + "$gameVariables.value(n), brackets and eleven operators";
        return 0;
    }

    private double LeseAufruf()
    {
        const string aufruf = "$gameVariables.value(";
        if (!_text[_stelle..].StartsWith(aufruf, StringComparison.Ordinal))
        {
            var fremd = NaechsterName();
            _fehler = "'" + fremd + "', and this reader reads only "
                + "$gameVariables.value(n) of the game's own objects";
            return 0;
        }

        _stelle += aufruf.Length;
        Luecke();
        var anfang = _stelle;
        while (_stelle < _text.Length
            && char.IsDigit(_text[_stelle]))
        {
            _stelle++;
        }

        var nummer = _text.Substring(anfang, _stelle - anfang);
        Luecke();
        if (_stelle < _text.Length && _text[_stelle] == ')')
        {
            _stelle++;
        }
        else
        {
            _fehler = "a bracket that is never closed, at \""
                + BisHier() + "\"";
        }

        if (nummer.Length == 0
            || !int.TryParse(
                nummer, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var id))
        {
            _fehler = "'" + nummer + "' is not a variable number";
            return 0;
        }

        // **Und `Game_Variables.prototype.value` ist
        // `return this._data[index] || 0;`** -- **und das heisst: eine
        // Variable, die das Spiel nie geschrieben hat, ist null, und null
        // ist in JavaScript falsy, und der Ausdruck ergibt null.**
        return _fakten != null && _fakten.Variables.TryGetValue(id, out var wert)
            ? wert
            : 0;
    }

    /// <summary>
    /// Whether this operator belongs to a level above this one, and is
    /// therefore not this level's mistake.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the rule that makes
    /// <c>$gameVariables.value(15) &gt;= 6 ||
    /// $gameVariables.value(18) &gt;= 3</c> readable</strong>:
    /// <strong>the <c>||</c> belongs to the level above the comparison, and
    /// a comparison that sees one must leave it alone.</strong>
    /// </para>
    /// <para>
    /// <strong>And the first version reported it as a mistake</strong>, <strong>
    /// and that rejected every expression that is not a bare
    /// comparison</strong> -- <strong>which is 33 of the 44 forms in this
    /// game.</strong>
    /// </remarks>
    private static bool GehoertEinerHoeheren(string pOp) =>
        pOp is "+" or "-" or "*" or "/" or "%" or "**" or "&&" or "||";

    private static bool IstVergleich(string pOp) =>
        pOp is "<" or "<=" or ">" or ">=" or "==" or "!=" or "===" or "!==";

    /// <summary>
    /// Every variable the expression names, and whether the game has written
    /// each one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the rule that keeps an unwritten variable from
    /// answering a question about itself.</strong>
    /// </para>
    /// <para>
    /// <c>Game_Variables.prototype.value</c> is <c>return this._data[index]
    /// || 0;</c>, <strong>so an unwritten variable reads as zero</strong> --
    /// <strong>and that is a fact about a variable the game never
    /// touched</strong>, <strong>and the same zero as one the game wrote on
    /// purpose.</strong> <strong>And those two are different facts</strong>,
    /// <strong>and a condition that asks <c>$gameVariables.value(3280) &gt;
    /// 5</c> is asking about a thing that does not exist.</strong>
    /// </para>
    /// <para>
    /// <strong>And this is measured.</strong> Measured at
    /// <c>D:/Itch/sister/www</c>: <strong>the game writes 1623 variables
    /// and reads 3189, and the reads name numbers up to 3491</strong> --
    /// <strong>which is more than it writes, because it reads the ones a
    /// plugin made.</strong>
    /// </para>
    /// </remarks>
    public static bool KenntAlleVariablen(
        string pText, MzBranchFacts pFakten) =>
        FehlendeVariablen(pText, pFakten).Count == 0;

    /// <summary>
    /// The variables the expression names that the game has not written.
    /// </summary>
    public static List<int> FehlendeVariablen(
        string pText, MzBranchFacts pFakten)
    {
        var fehlend = new List<int>();
        var rest = pText ?? "";
        const string aufruf = "$gameVariables.value(";
        var stelle = 0;
        while ((stelle = rest.IndexOf(aufruf, stelle,
            StringComparison.Ordinal)) >= 0)
        {
            var anfang = stelle + aufruf.Length;
            var ende = rest.IndexOf(')', anfang);
            if (ende < 0)
            {
                break;
            }

            var nummer = rest.Substring(anfang, ende - anfang).Trim();
            if (int.TryParse(
                nummer, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var id)
                && (pFakten == null
                    || !pFakten.Variables.ContainsKey(id))
                && !fehlend.Contains(id))
            {
                fehlend.Add(id);
            }

            stelle = ende + 1;
        }

        return fehlend;
    }

    /// <summary>
    /// The first variable the expression names that the game has not
    /// written, and zero when there is none.
    /// </summary>
    public static int WelcheVariableFehlt(
        string pText, MzBranchFacts pFakten)
    {
        var fehlend = FehlendeVariablen(pText, pFakten);
        return fehlend.Count > 0 ? fehlend[0] : 0;
    }

    /// <summary>
    /// Whether a number counts as true, and JavaScript's own rule.
    /// </summary>
    /// <remarks>
    /// <strong>And it is <c>!= 0</c> and not <c>&gt; 0</c></strong>:
    /// <strong>a negative number is true in JavaScript</strong>, <strong>and
    /// <c>NaN</c> is false because <c>NaN</c> is the one number that is not
    /// equal to itself.</strong>
    /// </remarks>
    public static bool IstWahr(double pWert) =>
        pWert != 0 && !double.IsNaN(pWert);

    private static double Vergleich(
        string pOp, double pLinks, double pRechts) =>
        pOp switch
        {
            "<" => pLinks < pRechts ? 1 : 0,
            "<=" => pLinks <= pRechts ? 1 : 0,
            ">" => pLinks > pRechts ? 1 : 0,
            ">=" => pLinks >= pRechts ? 1 : 0,
            "==" or "===" => pLinks == pRechts ? 1 : 0,
            "!=" or "!==" => pLinks != pRechts ? 1 : 0,
            _ => double.NaN,
        };

    /// <summary>
    /// The next operator, or empty, and it never eats a name.
    /// </summary>
    /// <summary>
    /// The operator at this place, and it does not eat it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And "does not eat it" is the whole rule</strong>, <strong>and
    /// the first version did and that was the last bug in this
    /// reader.</strong> <strong>Every level of the grammar asks "is there an
    /// operator here?"</strong>, <strong>and a level that ate the answer
    /// would take an operator that belongs to a level above it</strong> --
    /// <strong>and <c>1 + 2</c> came out as "1" with "+ 2" left
    /// over.</strong>
    /// </para>
    /// <para>
    /// <strong>And the two-character list is in this order</strong>:
    /// <c>"=="</c> is a start of <c>"==="</c> and <c>"!="</c> is a start of
    /// <c>"!=="</c>, <strong>and a list that tries the shorter first would
    /// return <c>"=="</c> for a <c>"==="</c> and leave the third
    /// <c>=</c> standing</strong>. <strong>And <c>"**"</c> is one hundred
    /// and thirty-six times in this game and <c>"==="</c> once.</strong>
    /// </para>
    /// </remarks>
    private string NaechsteOperator()
    {
        Luecke();
        if (_stelle >= _text.Length)
        {
            return "";
        }

        foreach (var zwoi in ZweiZeichen)
        {
            if (_text.AsSpan(_stelle).StartsWith(zwoi, StringComparison.Ordinal))
            {
                return zwoi;
            }
        }

        // **Und eine Klammer ist kein Operator**, **und sie wird hier
        // nicht zurueckgegeben**, **denn `LeseFaktor` ist die einzige
        // Stelle, die sie liest** -- **und 136 der 364 Ausdruecke in
        // diesem Spiel sind geklammert.**
        var c = _text[_stelle];
        return "+-*/%<>&|=".IndexOf(c) >= 0 ? c.ToString() : "";
    }

    /// <summary>
    /// Take the operator that is at this place, and step past it.
    /// </summary>
    private string NaechstenOperatorNehmen()
    {
        var op = NaechsteOperator();
        if (op.Length > 0)
        {
            _stelle += op.Length;
        }

        return op;
    }


    private void Luecke()
    {
        while (_stelle < _text.Length && char.IsWhiteSpace(_text[_stelle]))
        {
            _stelle++;
        }
    }

    /// <summary>
    /// Skips what is left over, and complains about it.
    /// </summary>
    /// <remarks>
    /// <strong>And "what is left over" is a refusal and not a
    /// shrug</strong>: <b>an expression that ended in the middle is an
    /// expression this reader did not finish</b>, <b>and saying so is the
    /// difference between a reader that is careful and a reader that
    /// guesses.</b>
    /// </remarks>
    private void LueckeRest()
    {
        Luecke();
        if (_stelle < _text.Length && _fehler.Length == 0)
        {
            var rest = _text[_stelle..].Trim();
            _fehler = "'" + (rest.Length > 24 ? rest[..24] + "..." : rest)
                + "' is left over, and this reader does not know what to do "
                + "with the rest of an expression";
        }
    }

    private string NaechsterName()
    {
        Luecke();
        var anfang = _stelle;
        while (_stelle < _text.Length
            && (char.IsLetterOrDigit(_text[_stelle])
                || _text[_stelle] == '_' || _text[_stelle] == '$'
                || _text[_stelle] == '.'))
        {
            _stelle++;
        }

        return _text.Substring(anfang, _stelle - anfang);
    }

    private string BisHier()
    {
        var ende = Math.Min(_stelle + 12, _text.Length);
        return _text.Substring(
            Math.Max(0, ende - 24), ende - Math.Max(0, ende - 24));
    }
}