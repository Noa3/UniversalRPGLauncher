using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace UniversalRPG.Rgss;

/// <summary>
/// One event command of a Ruby Maker map, in the numbers the engine uses.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the command numbers are the same across XP, VX and VX
/// Ace</strong>, -- <strong>because all three read the list the editor
/// wrote and all three hand it to <c>commandNNN</c></strong>, --
/// <strong>and that is why one table serves the three generations.</strong>
/// </para>
/// <para>
/// <strong>And the shape is three fields</strong> -- <c>[code, indent,
/// parameters]</c> -- <strong>and the parameters are a flat array of
/// whatever the command carries</strong>, -- <strong>so a reader that
/// types them all as integers breaks the first command that asks for a
/// name.</strong>
/// </para>
/// </remarks>
public sealed class RgssEventCommand
{
    /// <summary>The engine's own <c>commandNNN</c> number.</summary>
    public int Code { get; init; }

    /// <summary>How deep this command sits inside branches and loops.</summary>
    public int Indent { get; init; }

    /// <summary>
    /// What the command carries, and numbers and names both.
    /// </summary>
    /// <remarks>
    /// <strong>And keeping both is not tidiness.</strong> Measured on
    /// this project's own games: <c>101 Show Text</c> carries a face
    /// name and an integer and a face name again, -- <strong>and a
    /// parameter list of integers turns <c>People</c> into
    /// <c>0</c>.</strong>
    /// </remarks>
    public IReadOnlyList<RgssParameter> Parameters { get; init; } =
        Array.Empty<RgssParameter>();
}

/// <summary>One parameter, and which of the two kinds it is.</summary>
/// <remarks>
/// <para>
/// <strong>And a parameter is a number or a name and the reader has to
/// say which</strong>, -- <strong>because <c>At(1)</c> and
/// <c>Text(0)</c> ask different questions of the same list.</strong>
/// </para>
/// </remarks>
public readonly struct RgssParameter
{
    private RgssParameter(long pZahl, string pText, bool pIstText)
    {
        Zahl = pZahl;
        Text = pText;
        IstText = pIstText;
    }

    /// <summary>The number, when this is one.</summary>
    public long Zahl { get; }

    /// <summary>The name, when this is one.</summary>
    public string Text { get; }

    /// <summary>Whether this parameter is a name.</summary>
    public bool IstText { get; }

    /// <summary>Builds a number parameter.</summary>
    /// <param name="pWert">The number.</param>
    /// <returns>The parameter.</returns>
    public static RgssParameter Zahl_(long pWert) =>
        new(pWert, "", false);

    /// <summary>Builds a name parameter.</summary>
    /// <param name="pWert">The name.</param>
    /// <returns>The parameter.</returns>
    public static RgssParameter Text_(string pWert) =>
        new(0, pWert ?? "", true);

    /// <summary>The number, or zero for a name.</summary>
    /// <param name="pListe">The command's own parameter list.</param>
    /// <param name="pIndex">Which parameter.</param>
    /// <returns>The number, and zero when the parameter is a name.</returns>
    /// <remarks>
    /// <strong>And zero for a name is the engine's own answer</strong>,
    /// and <strong>the editor writes a name exactly where a command
    /// expects a number only when the author left the box empty</strong>,
    /// -- <strong>and a reader that has to guess which happened will
    /// guess wrong on a game's first empty face box.</strong>
    /// </remarks>
    public long At(
        IReadOnlyList<RgssParameter> pListe, int pIndex) =>
        pIndex >= 0 && pIndex < pListe.Count && !pListe[pIndex].IstText
            ? pListe[pIndex].Zahl
            : 0;

    /// <summary>The name, or an empty string for a number.</summary>
    /// <param name="pListe">The command's own parameter list.</param>
    /// <param name="pIndex">Which parameter.</param>
    /// <returns>The name, and empty when the parameter is a number.</returns>
    public string TextOf(
        IReadOnlyList<RgssParameter> pListe, int pIndex) =>
        pIndex >= 0 && pIndex < pListe.Count && pListe[pIndex].IstText
            ? pListe[pIndex].Text
            : "";

    /// <summary>Whether a parameter is a name, and that is a question.</summary>
    /// <param name="pListe">The command's own parameter list.</param>
    /// <param name="pIndex">Which parameter.</param>
    /// <returns>Yes or no, and out of range is no.</returns>
    /// <remarks>
    /// <strong>And <c>121 Switch</c> writes <c>"True"</c> and
    /// <c>"False"</c></strong>, -- <strong>which are names and not
    /// numbers</strong>, -- <strong>and a reader that parses numbers
    /// turns both into zero, and zero is off, and the wait setting of
    /// every switch in a game does nothing.</strong>
    /// </remarks>
    public bool IstName(
        IReadOnlyList<RgssParameter> pListe, int pIndex) =>
        pIndex >= 0 && pIndex < pListe.Count && pListe[pIndex].IstText;

    public override string ToString() =>
        IstText ? $"\"{Text}\"" : Zahl.ToString(CultureInfo.InvariantCulture);
}

/// <summary>One event of a Ruby Maker map, and its pages.</summary>
public sealed class RgssMapEvent
{
    /// <summary>The event's own number, and not its position.</summary>
    public int Id { get; init; }

    /// <summary>What the editor calls it.</summary>
    public string Name { get; init; } = "";

    /// <summary>Which tile the event stands on, in x.</summary>
    public int X { get; init; }

    /// <summary>Which tile the event stands on, in y.</summary>
    public int Y { get; init; }

    /// <summary>
    /// The pages, and the engine reads them from the last one back.
    /// </summary>
    /// <remarks>
    /// <strong>And this order is not a choice.</strong> Measured at
    /// <c>Game_Interpreter</c>'s own page search: the loop starts at
    /// <c>pages.length - 1</c> and walks down, -- <strong>and a reader
    /// that goes the other way picks a page the engine would never
    /// run.</strong>
    /// </remarks>
    public IReadOnlyList<RgssMapEventPage> Pages { get; init; } =
        Array.Empty<RgssMapEventPage>();

    /// <summary>The page the engine would pick, and why.</summary>
    /// <returns>
    /// The index, and -1 when no page matches, and that is the engine's
    /// own answer rather than a failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>findProperPageIndex</c>, in order:</strong>
    /// the last page whose conditions all hold, and <strong>a page whose
    /// conditions this reader cannot answer does not stop the
    /// search</strong> -- <strong>it is skipped</strong>, -- <strong>and
    /// a reader that stopped there would run a page the engine skips
    /// and never reach the one the engine runs.</strong>
    /// </para>
    /// </remarks>
    public int PassendeSeite(Func<RgssMapEventPage, bool>? pPasst = null)
    {
        for (var i = Pages.Count - 1; i >= 0; i--)
        {
            if (pPasst == null || pPasst(Pages[i]))
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>One page of one event, and its list.</summary>
public sealed class RgssMapEventPage
{
    /// <summary>The trigger, as the editor writes it.</summary>
    public int Trigger { get; init; }

    /// <summary>The condition switch, and zero when there is none.</summary>
    public int ConditionSwitchId { get; init; }

    /// <summary>What the switch has to be, as a word.</summary>
    public string ConditionSwitchValue { get; init; } = "";

    /// <summary>The condition item, and zero when there is none.</summary>
    public int ConditionItemId { get; init; }

    /// <summary>What the item has to be, as a word.</summary>
    public string ConditionItemValue { get; init; } = "";

    /// <summary>The condition actor, and zero when there is none.</summary>
    public int ConditionActorId { get; init; }

    /// <summary>The condition switch of the page itself.</summary>
    public string SelfSwitchCh { get; init; } = "";

    /// <summary>The character sheet, and empty when the event is a door.</summary>
    public string CharacterName { get; init; } = "";

    /// <summary>Which cell of that sheet.</summary>
    public int CharacterIndex { get; init; }

    /// <summary>Which way the event faces, and 2 is down.</summary>
    public int Direction { get; init; }

    /// <summary>The start frame, and this is a real field.</summary>
    public int Pattern { get; init; }

    /// <summary>The command list, and the whole of it.</summary>
    public IReadOnlyList<RgssEventCommand> Commands { get; init; } =
        Array.Empty<RgssEventCommand>();

    /// <summary>
    /// Whether the engine's own conditions all hold.
    /// </summary>
    /// <remarks>
    /// <strong>And a condition this reader cannot answer is not a
    /// condition that fails</strong>, -- <strong>it is one that is
    /// skipped</strong>, -- <strong>and the two are different
    /// answers.</strong>
    /// </remarks>
    public bool BedingungenPassen(
        Func<int, string, bool>? pSchalter = null,
        Func<int, string, bool>? pGegenstand = null,
        Func<int, bool>? pDarsteller = null)
    {
        if (ConditionSwitchId > 0
            && pSchalter != null
            && !pSchalter(ConditionSwitchId, ConditionSwitchValue))
        {
            return false;
        }

        if (ConditionItemId > 0
            && pGegenstand != null
            && !pGegenstand(ConditionItemId, ConditionItemValue))
        {
            return false;
        }

        return !(ConditionActorId > 0
            && pDarsteller != null && !pDarsteller(ConditionActorId));
    }
}

/// <summary>One map of a Ruby Maker project.</summary>
public sealed class RgssMap
{
    /// <summary>How wide the map is, in tiles.</summary>
    public int Width { get; init; }

    /// <summary>How high the map is, in tiles.</summary>
    public int Height { get; init; }

    /// <summary>How many tiles the screen shows, in x.</summary>
    public int DisplayWidth { get; init; }

    /// <summary>How many tiles the screen shows, in y.</summary>
    public int DisplayHeight { get; init; }

    /// <summary>The tile ids, row by row, and one array per row.</summary>
    public IReadOnlyList<IReadOnlyList<int>> Data { get; init; } =
        Array.Empty<IReadOnlyList<int>>();

    /// <summary>The events, and every one of them.</summary>
    public IReadOnlyList<RgssMapEvent> Events { get; init; } =
        Array.Empty<RgssMapEvent>();
}
