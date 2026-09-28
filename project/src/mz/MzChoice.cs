using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// 102, the choices under a line — and the branch they set.
/// </summary>
/// <remarks>
/// <para>
/// <c>setupChoices</c> is <b>five parameters, four of them optional, and two
/// of those four have defaults the caller cannot guess</b>:
///
/// <c>
/// const choices = params[0].clone();
/// const cancelType = params[1] &lt; choices.length ? params[1] : -2;
/// const defaultType = params.length &gt; 2 ? params[2] : 0;
/// const positionType = params.length &gt; 3 ? params[3] : 2;
/// const background = params.length &gt; 4 ? params[4] : 0;
/// $gameMessage.setChoices(choices, defaultType, cancelType);
/// …
/// $gameMessage.setChoiceCallback(n =&gt; { this._branch[this._indent] = n; });
/// </c>
///
/// <para>
/// <b>And the first line is the rule a first reading gets wrong.</b>
/// <c>cancelType = params[1] &lt; choices.length ? params[1] : -2</c> — a
/// cancel index that is <b>not below the number of choices becomes minus
/// two</b>, which is the engine's "no cancel" value. A reader that took the
/// number as written would let a player cancel a list of three by choosing a
/// fourth thing, and a number like <c>4</c> — which the editor writes for
/// "cancel on the last option" — would be out of range for a list of three.
/// <b>-2 and "the fourth of three" are the same answer.</b>
/// </para>
///
/// <para>
/// <b>The branch is set through a callback, not by the choice itself.</b>
/// <c>setChoiceCallback(n =&gt; { this._branch[this._indent] = n; })</c> — and
/// <b>at the current indent</b>, which is the indent of the 102, so a choice
/// deep in a branch writes its answer into that branch's slot and a choice
/// in a loop writes into the loop's.
/// </para>
///
/// <para>
/// <b>This game's own numbers:</b> eight 102s, and every one of them sits
/// directly after a 101's last line. **Its cancel numbers are worth
/// measuring rather than assuming** — see the tests, where eight values from
/// real files are checked against the engine's own rule.
/// </para>
/// </remarks>
public sealed class MzChoice
{
    /// <summary>The engine's "no cancel" value.</summary>
    public const int NoCancel = -2;

    /// <summary>One set of choices, as <c>setupChoices</c> left it.</summary>
    public sealed class Set
    {
        /// <summary>The options, as the game wrote them — <b>each still
        /// carrying its own escape codes</b>, because
        /// <c>params[0].clone()</c> clones and does not read.</summary>
        public IReadOnlyList<string> Options { get; init; } = System.Array.Empty<string>();

        /// <summary>The cancel index, <b>after the engine's own clamp</b>.</summary>
        public int CancelType { get; init; } = NoCancel;

        /// <summary>What the cancel index was before the clamp, which is what
        /// the game actually wrote.</summary>
        public int WrittenCancelType { get; init; }

        /// <summary>Which option is chosen, <c>defaultType</c>.</summary>
        public int DefaultType { get; init; }

        /// <summary>Where the window sits, and the default is <b>2</b> — the
        /// bottom — not 0.</summary>
        public int Position { get; init; } = 2;

        /// <summary>0 window, 1 faded, 2 none.</summary>
        public int Background { get; init; }

        /// <summary>
        /// Whether the game wrote the cancel number at all, or whether this
        /// reader supplied the engine's default.
        /// </summary>
        public bool CancelWasClamped { get; init; }

        /// <summary>One line, for an action and for a log.</summary>
        public override string ToString() =>
            $"{Options.Count} choices, default {DefaultType}, cancel"
            + (CancelType == NoCancel
                ? NoCancel == WrittenCancelType
                    ? " not allowed"
                    : $" not allowed, because {WrittenCancelType} is not"
                        + " below the number of choices"
                : $" on option {CancelType}")
            + $", position {Position}, background {Background}";
    }

    /// <summary>
    /// Reads a 102, and <b>applies the engine's clamp to the cancel
    /// number</b>.
    /// </summary>
    /// <remarks>
    /// **The clamp is the whole of this method.** A game writes a cancel
    /// number the editor chose, and the engine turns any number that is not
    /// below the length of the list into "no cancel" — a reader that took the
    /// number as written would be answering a different question from the one
    /// the game asked.
    /// </remarks>
    public static Set Read(IReadOnlyList<string> pParameters)
    {
        // **`params[0]` is an array of strings, not a separated string.**
        // A first draft wrote `params[0].split("|")` — the shape an older
        // RPG Maker used — and this game writes `["Yes", "No"]`, so the
        // draft would have read one option that reads
        // `["Yes", "No"]` with its brackets and its comma in it, and the
        // clamp below would have compared the game's cancel number against
        // the wrong length.
        //
        // **`MzCommandEntry` keeps parameters as strings**, and a nested array
        // arrives as written JSON (K-131's `MzJson.Write`), so this is read
        // back through the value tree rather than by splitting.
        var options = ReadOptions(pParameters.Count > 0 ? pParameters[0] : "");

        // **`params[1] < choices.length ? params[1] : -2`** — below the
        // length means an option, and anything else is "no cancel".
        var written = pParameters.Count > 1 ? At(pParameters[1]) : 0;
        var cancel = written < options.Count ? written : NoCancel;

        return new Set
        {
            Options = options,
            WrittenCancelType = written,
            CancelType = cancel,
            CancelWasClamped = cancel != written,
            DefaultType = pParameters.Count > 2 ? At(pParameters[2]) : 0,
            // **The default position is 2, and the default background is 0.**
            // `positionType = params.length > 3 ? params[3] : 2`.
            Position = pParameters.Count > 3 ? At(pParameters[3]) : 2,
            Background = pParameters.Count > 4 ? At(pParameters[4]) : 0,
        };
    }

    /// <summary>
    /// Reads <c>params[0]</c>, which is an array of strings the game wrote.
    /// </summary>
    /// <remarks>
    /// **And an array that is not one is named rather than taken for a
    /// single option.** A reader that wrote the whole thing as one option
    /// would show a player <c>["Yes", "No"]</c> and count one choice where
    /// there are two.
    /// </remarks>
    public static IReadOnlyList<string> ReadOptions(string pParameter)
    {
        if (string.IsNullOrEmpty(pParameter))
        {
            return System.Array.Empty<string>();
        }

        if (MzJson.TryParse(pParameter, out var wert, out _) && wert.Kind == MzKind.Array)
        {
            var options = new List<string>();
            foreach (var item in wert.Items)
            {
                options.Add(item.Text ?? "");
            }
            return options;
        }

        // **Not an array.** The engine's `params[0].clone()` would carry a
        // string through, so this reader does too — and the length is 1, which
        // makes the cancel clamp answer the engine's question about a list
        // of one.
        return new[] { pParameter };
    }

    private static int At(string pText) =>
        int.TryParse(
            pText, NumberStyles.Integer, CultureInfo.InvariantCulture,
            out var wert)
            ? wert
            : 0;
}
