using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Web;

/// <summary>
/// A line of text, as a game wrote it and as a reader has to read it.
/// </summary>
/// <remarks>
/// <para>
/// This is 401's whole content, and there are 938 of them in this game, and
/// **each carries exactly one parameter**: a line of text with escape codes
/// in it. The reading is <c>Window_Base</c>'s, and it happens in
/// <b>two passes that follow different rules</b> — and a reader that does
/// them in one gets a different line than the game shows.
/// </para>
///
/// <para>
/// <b>Pass one, <c>convertEscapeCharacters</c>, rewrites in three steps.</b>
///
/// <list type="number">
/// <item><c>text.replace(/\\/g, "\x1b")</c> — <b>every</b> backslash becomes
/// the escape character. This is the step a reader skips, and skipping it
/// means a line keeps a backslash and its next letter is read as text.</item>
/// <item><c>text.replace(/\x1b\x1b/g, "\\")</c> — <b>two escape characters put
/// one backslash back.</b> A literal backslash is written <c>\\</c> and the
/// game shows one. A reader that did only the first step would show the
/// player a string of control characters where a path was meant to be.</item>
/// <item><c>while (text.match(/\x1bV\[(\d+)\]/gi)) …</c> — <b>variables, actor
/// names, party names and the currency unit are replaced here, in a
/// loop.</b> A line with two <c>\V[3]</c> gets both filled in.</item>
/// </list>
///
/// <para>
/// <b>Pass two, the drawing loop, reads what is left.</b>
/// <c>processCharacter</c> treats <b>every character below 0x20 as a
/// control character</b> and never puts it in the buffer, and
/// <c>obtainEscapeCode</c> matches
/// <c>/^[$.|^!><{}\\]|^[A-Z]+/i</c> — a single punctuation mark, or a run of
/// letters.
/// </para>
///
/// <para>
/// <b>Which codes change the text and which change the pen.</b> A reader
/// with no renderer can answer the first and must name the second:
///
/// <list type="bullet">
/// <item><b>In the text</b> after pass one: <c>\V[n]</c> a variable's value,
/// <c>\N[n]</c> an actor's name, <c>\P[n]</c> a party member's name,
/// <c>\G</c> the currency unit.</item>
/// <item><b>Not in the text, and never shown</b>: <c>\|</c> waits for the
/// player, <c>^</c> waits for a choice, <c>!</c> waits for a decision,
/// <c>&gt;\n</c> and <c>&lt;\n</c> move the pen <b>one page forward and one
/// page back</b>, <c>$</c> ends the line. <b>None of them is a character in
/// the output</b>, and a reader that emitted them would put <c>|</c> in the
/// middle of a sentence.</item>
/// <item><b>Neither text nor pen, and this reader names them</b>:
/// <c>\C[n]</c> a colour, <c>\I[n]</c> an icon, <c>\PX[n]</c> and
/// <c>\PY[n]</c> a position, <c>\FS[n]</c> a font size, <c>\{</c> and
/// <c>\}</c> bigger and smaller. <b>A reader with no renderer cannot draw
/// them, and it says so rather than dropping them in silence.</b></item>
/// </list>
///
/// <para>
/// <b>And one this game actually uses.</b> Its 938 lines carry
/// <c>\|</c> once and <c>\|.\|.\|.</c> in the same line — a line that waits
/// three times for the player. The letters <c>C</c>, <c>N</c>, <c>V</c> and
/// <c>P</c> that appear so often in a scan of the text are <b>ordinary
/// letters in ordinary words</b>: "SEND HELP", "*Nom*", "nutrients", "OUT".
/// <b>A first draft counted them as escape codes and was wrong by a
/// factor of fifty.</b>
/// </para>
/// </remarks>
public sealed class MzMessage
{
    /// <summary>
    /// The escape character MZ puts in front of every code, as
    /// <c>text.replace(/\\/g, "\x1b")</c> does.
    /// </summary>
    public const char Escape = '';

    /// <summary>What one line said, and what it asked of the player.</summary>
    public sealed class Line
    {
        /// <summary>The words, with every code already resolved or removed.</summary>
        public string Text { get; init; } = "";

        /// <summary>
        /// How many times the line waits for the player, which is the count of
        /// <c>\|</c> in it.
        /// </summary>
        /// <remarks>
        /// **This is the only thing about a line a reader without a window can
        /// act on**, and it is not a rendering detail: a 401 is followed by a
        /// 402 to carry on, and a line with three <c>\|</c> needs three
        /// decisions. A reader that dropped <c>\|</c> would show a
        /// conversation that asks nothing.
        /// </remarks>
        public int Waits { get; init; }

        /// <summary>How many times the line puts up a choice, from
        /// <c>^</c>.</summary>
        public int Choices { get; init; }

        /// <summary>How many times the line asks the player to decide, from
        /// <c>!</c>.</summary>
        public int Decisions { get; init; }

        /// <summary>Whether the line ends early, from <c>$</c>.</summary>
        public bool EndsEarly { get; init; }

        /// <summary>
        /// What the line asked of something this reader cannot draw, named
        /// rather than dropped: colours, icons, positions, font sizes.
        /// </summary>
        public IReadOnlyList<string> Undrawable { get; init; } =
            Array.Empty<string>();

        /// <summary>The variable, actor, party and currency codes that were
        /// replaced, in the order they were found.</summary>
        public IReadOnlyList<string> Substituted { get; init; } =
            Array.Empty<string>();

        /// <summary>One line, for an action and for a log.</summary>
        public override string ToString() =>
            $"\"{Text}\"" + (Waits > 0 ? $", waiting {Waits} times" : "")
            + (Choices > 0 ? $", with {Choices} choices" : "")
            + (Decisions > 0 ? $", {Decisions} decisions" : "")
            + (EndsEarly ? ", ending early" : "")
            + (Undrawable.Count > 0
                ? $", and {Undrawable.Count} things this reader cannot draw"
                : "");
    }

    /// <summary>
    /// The two names pass one needs, which come from a database this reader
    /// has not opened.
    /// </summary>
    public interface INameSource
    {
        /// <summary><c>actorName(n)</c>, empty for an actor there is not.</summary>
        public string ActorName(int pIndex);

        /// <summary><c>partyMemberName(n)</c>, empty for a member there is
        /// not.</summary>
        public string PartyMemberName(int pIndex);

        /// <summary><c>TextManager.currencyUnit</c>.</summary>
        public string CurrencyUnit { get; }
    }

    /// <summary>
    /// A source that knows three names and no others, which is what a reader
    /// without a database can honestly offer.
    /// </summary>
    /// <remarks>
    /// **This exists so a caller can be explicit.** A reader with no actors at
    /// all should say so by handing this in and watching the names come out
    /// empty — not by leaving the source null and hoping the difference does
    /// not matter. <b>Both are honest; only one of them is visible.</b>
    /// </remarks>
    public sealed class ThreeNames : INameSource
    {
        /// <summary>Names by index, one-based as the engine's are.</summary>
        public Dictionary<int, string> Actors { get; init; } = new();

        /// <summary>Names by index, one-based as the engine's are.</summary>
        public Dictionary<int, string> Members { get; init; } = new();

        /// <summary><c>TextManager.currencyUnit</c>.</summary>
        public string CurrencyUnit { get; init; } = "";

        /// <summary>
        /// The engine's <c>actorName</c> is
        /// <c>const actor = n &gt;= 1 ? $gameActors.actor(n) : null; return
        /// actor ? actor.name() : "";</c> — <b>empty for an actor that is not
        /// there, and for an index below one.</b>
        /// </summary>
        public string ActorName(int pIndex) =>
            pIndex >= 1 && Actors.TryGetValue(pIndex, out var name) ? name : "";

        /// <summary>The engine's <c>partyMemberName</c> the same way.</summary>
        public string PartyMemberName(int pIndex) =>
            pIndex >= 1 && Members.TryGetValue(pIndex, out var name) ? name : "";
    }

    /// <summary>Reads a line, as <c>drawTextEx</c> reads it.</summary>
    public static Line Read(
        string pText,
        Func<int, string>? pVariable = null,
        INameSource? pNames = null)
    {
        // **Step one: every backslash becomes the escape character.** A line
        // with `\!` does not carry a backslash and a bang, it carries an
        // escape code — and one that is never shown.
        var text = (pText ?? "").Replace('\\', Escape);

        // **Step two: two escape characters are one backslash.** A literal
        // backslash is written `\\` and shown as one, and without this a path
        // in a line of dialogue would appear as a run of control characters.
        text = text.Replace($"{Escape}{Escape}", "\\");

        // **Step three: the substitutions, in the engine's own order, and the
        // variable one is a loop because a line can name the same variable
        // twice and each occurrence is filled in.**
        var substituted = new List<string>();
        text = Substitute(text, pVariable, pNames, substituted);

        // **Now the drawing loop, where every character below 0x20 is a
        // control character and never reaches the output.**
        var undrawable = new List<string>();
        var waits = 0;
        var choices = 0;
        var decisions = 0;
        var endsEarly = false;
        var outp = new StringBuilder();

        for (var i = 0; i < text.Length;)
        {
            var c = text[i];
            if (c != Escape)
            {
                outp.Append(c);
                i++;
                continue;
            }

            // **`obtainEscapeCode`'s pattern: one punctuation mark, or a run
            // of letters.** `{` and `}` are single marks, `PX` is a run, and
            // **a backslash at the end of the string is a mark on its own.**
            //
            // **The code is read before the index moves.** A first draft set
            // `i` first and sliced afterwards, and every code came back as an
            // empty string — so `\|` read as nothing at all and a line that
            // waited three times looked like a line that did not wait.
            var j = i + 1;
            var ende = j;
            if (j < text.Length && (".$|!><{}\\^".Contains(text[j])))
            {
                ende = j + 1;
            }
            else
            {
                while (ende < text.Length && char.IsLetter(text[ende]))
                {
                    ende++;
                }
                if (ende == j && j < text.Length && text[j] == '\\')
                {
                    // **`^[$.|^!><{}\\]`** — the pattern's own last term is a
                    // backslash, and a line that ends in one is not a line
                    // with a missing code.
                    ende = j + 1;
                }
            }

            var code = text[(i + 1)..ende];
            i = ende;

            // **A code with a number after it in brackets takes it**, and the
            // number is the parameter: `\V[3]`, `\C[2]`, `\PX[96]`.
            var parameter = ReadParameter(text, ref i);

            switch (code)
            {
                case "|":
                    // **Waits for the player, and is never shown.** Not a
                    // character in the output: a reader that emitted it would
                    // put a `|` in the middle of a sentence.
                    waits++;
                    break;

                case "^":
                    choices++;
                    break;

                case "!":
                    decisions++;
                    break;

                case ">":
                    // **One page forward.** The engine draws a new page, so a
                    // reader with no pages records that it was asked and says
                    // so.
                    undrawable.Add("a page forward");
                    break;

                case "<":
                    undrawable.Add("a page back");
                    break;

                case "$":
                    endsEarly = true;
                    break;

                case "{":
                    undrawable.Add("bigger text");
                    break;

                case "}":
                    undrawable.Add("smaller text");
                    break;

                case "C":
                    undrawable.Add($"colour {parameter}");
                    break;

                case "I":
                    undrawable.Add($"icon {parameter}");
                    break;

                case "PX":
                    undrawable.Add($"position x {parameter}");
                    break;

                case "PY":
                    undrawable.Add($"position y {parameter}");
                    break;

                case "FS":
                    undrawable.Add($"font size {parameter}");
                    break;

                default:
                    // **A code MZ has and this reader has no name for is
                    // named.** The engine's list is closed — C, I, PX, PY,
                    // FS, { and } — so an unknown one is a data fault, and it
                    // is worth saying which.
                    undrawable.Add(
                        $"escape code {(code.Length == 0 ? "?" : code)}");
                    break;
            }
        }

        return new Line
        {
            Text = outp.ToString(),
            Waits = waits,
            Choices = choices,
            Decisions = decisions,
            EndsEarly = endsEarly,
            Undrawable = undrawable,
            Substituted = substituted,
        };
    }

    /// <summary>
    /// Reads <c>[n]</c> if it is there, as <c>obtainEscapeParam</c> does.
    /// </summary>
    private static int ReadParameter(string pText, ref int pIndex)
    {
        if (pIndex < pText.Length && pText[pIndex] == '[')
        {
            var close = pText.IndexOf(']', pIndex);
            if (close > pIndex
                && int.TryParse(
                    pText[(pIndex + 1)..close],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var wert))
            {
                pIndex = close + 1;
                return wert;
            }
        }
        return 0;
    }

    /// <summary>
    /// Pass one's three substitutions, in the engine's order, with the
    /// variable one in a loop.
    /// </summary>
    private static string Substitute(
        string pText,
        Func<int, string>? pVariable,
        INameSource? pNames,
        List<string> pSubstituted)
    {
        var text = pText;

        // **The variable one is a `while`, not an `if`:** a line that names
        // the same variable twice has both filled in, and a reader that
        // replaced one occurrence would leave the second as `\V[3]`.
        if (pVariable != null)
        {
            while (true)
            {
                var at = FindCode(text, "V", out var parameter);
                if (at < 0)
                {
                    break;
                }
                pSubstituted.Add($"variable {parameter}");
                text = text.Remove(at, ParameterLength(text, at))
                    .Insert(at, pVariable(parameter) ?? "0");
            }
        }

        if (pNames != null)
        {
            while (true)
            {
                var at = FindCode(text, "N", out var parameter);
                if (at < 0)
                {
                    break;
                }
                pSubstituted.Add($"actor {parameter}");
                text = text.Remove(at, ParameterLength(text, at))
                    .Insert(at, pNames.ActorName(parameter));
            }
            while (true)
            {
                var at = FindCode(text, "P", out var parameter);
                if (at < 0)
                {
                    break;
                }
                pSubstituted.Add($"party member {parameter}");
                text = text.Remove(at, ParameterLength(text, at))
                    .Insert(at, pNames.PartyMemberName(parameter));
            }
        }

        // **`\G` is the currency unit**, and it takes no number.
        var g = text.IndexOf($"{Escape}G", StringComparison.Ordinal);
        if (g >= 0)
        {
            pSubstituted.Add("the currency unit");
            text = text.Remove(g, 2)
                .Insert(g, pNames?.CurrencyUnit ?? "");
        }

        return text;
    }

    private static int FindCode(string pText, string pCode, out int pParameter)
    {
        pParameter = 0;
        var at = 0;
        while (true)
        {
            at = pText.IndexOf(
                $"{Escape}{pCode}[", at, StringComparison.Ordinal);
            if (at < 0)
            {
                return -1;
            }
            var close = pText.IndexOf(']', at);
            if (close > at
                && int.TryParse(
                    pText[(at + 3)..close],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var wert))
            {
                pParameter = wert;
                return at;
            }
            at += 2;
        }
    }

    /// <summary>
    /// How long the code starting at <c>pAt</c> is, so it can be taken out
    /// and something put in its place.
    /// </summary>
    /// <remarks>
    /// **A code with no number in brackets is two characters** — the escape
    /// and the letter — and a code whose brackets are not there is not a
    /// longer one. A first draft returned <c>close - pAt + 1</c> with
    /// <c>close</c> at minus one when there was no closing bracket, and
    /// <c>Remove</c> was then asked to take away minus two characters and
    /// threw. **An unterminated code in a game's text is bad data, and the
    /// reader has to survive it to be able to say so.**
    /// </remarks>
    private static int ParameterLength(string pText, int pAt)
    {
        if (pAt + 1 >= pText.Length)
        {
            return pText.Length - pAt;
        }
        var close = pText.IndexOf(']', pAt);
        return close > pAt ? close - pAt + 1 : 2;
    }
}
