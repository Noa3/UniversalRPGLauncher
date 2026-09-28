using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// The other two things a dialogue can ask for: a number to enter and an item
/// to choose.
/// </summary>
/// <remarks>
/// <para>
/// <b>A 103 and a 104 are not choices, and reading them as choices is the
/// mistake a first draft of K-133 made.</b> <c>setupNumInput</c> is
/// <c>$gameMessage.setNumberInput(params[0], params[1])</c> and
/// <c>setupItemChoice</c> is
/// <c>$gameMessage.setItemChoice(params[0], params[1] || 2)</c> — a digit
/// count and an item id, and neither is a list of options. A reader that put
/// both through the choice reader would have counted <c>4</c> — a digit count
/// — as four options, and would have compared a game's cancel number against
/// the length of a number.
/// </para>
///
/// <para>
/// <b>And the one default worth writing down</b> is
/// <c>params[1] || 2</c>: a missing second parameter is <b>2</b>, the whole
/// party, and <b>not zero</b>. The number 0 is falsy in JavaScript, so
/// <c>0 || 2</c> is 2 as well — <b>a game that wrote 0 and a game that wrote
/// nothing get the same answer</b>, and both get the whole party.
/// </para>
///
/// <para>
/// <b>This game has no 103 and no 104 in nineteen maps</b>, so everything
/// here is built from the engine and not from data, and the tests say so.
/// </para>
/// </remarks>
public abstract class MzPrompt
{
    /// <summary>
    /// A number to enter, from a 103.
    /// </summary>
    /// <remarks>
    /// **`setNumberInput(digitCount, type)`**, and both are the engine's own
    /// — not read further, because the window that would show them is not
    /// this repository's.
    /// </remarks>
    public sealed class Number : MzPrompt
    {
        /// <summary>How many digits, `params[0]`.</summary>
        public int Digits { get; init; }

        /// <summary>What it is for, `params[1]`: 0 digits, 1 a number of
        /// items, 2 equipment of a slot, 3 a choice from other actors.</summary>
        public int Type { get; init; }

        public override string ToString() =>
            $"a number of {Digits} digit{(Digits == 1 ? "" : "s")},"
            + $" type {Type}";
    }

    /// <summary>
    /// An item to choose, from a 104.
    /// </summary>
    /// <remarks>
    /// **`setItemChoice(itemId, category)`**, and the category's default is
    /// **2** — `params[1] || 2` — which is the whole party.
    /// </remarks>
    public sealed class Item : MzPrompt
    {
        /// <summary>Which item, `params[0]`.</summary>
        public int ItemId { get; init; }

        /// <summary>Which of the party's things to choose from, and the
        /// engine's default is <b>2</b> — the whole party.</summary>
        public int Category { get; init; }

        /// <summary>Whether the game wrote the category at all.</summary>
        public bool CategoryWasWritten { get; init; }

        public override string ToString() =>
            $"item {ItemId} from category {Category}"
            + (CategoryWasWritten ? string.Empty : ", which the game did not write");
    }

    /// <summary>
    /// Reads a 103 or a 104 the way the engine's two setup methods do.
    /// </summary>
    public static MzPrompt Read(int pCode, IReadOnlyList<string> pParameters)
        => pCode == 103
            ? new Number { Digits = At(pParameters, 0), Type = At(pParameters, 1) }
            : new Item
            {
                ItemId = At(pParameters, 0),
                // **`params[1] || 2`** — a missing parameter, a zero and an
                // empty string all give 2, because all three are falsy.
                Category = pParameters.Count > 1 && At(pParameters, 1) != 0
                    ? At(pParameters, 1)
                    : 2,
                CategoryWasWritten = pParameters.Count > 1,
            };

    private static int At(IReadOnlyList<string> pParameters, int pIndex) =>
        pIndex < pParameters.Count
        && int.TryParse(
            pParameters[pIndex],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var wert)
            ? wert
            : 0;
}
