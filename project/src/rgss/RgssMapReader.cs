using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rgss;

/// <summary>
/// Turns a marshalled map of a Ruby Maker project into the numbers the
/// engine's own <c>commandNNN</c> functions take.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every field is read by the engine's own name</strong>, --
/// <strong>and none by position</strong>. <c>RPG::Map</c> carries
/// <c>@width</c>, <c>@height</c>, <c>@data</c> and <c>@events</c> among
/// others, and XP has eleven of those fields and VX has eighteen, --
/// <strong>so an index that is right for one is wrong for the
/// other.</strong>
/// </para>
/// <para>
/// <strong>And this reader's index handling came from a wrong
/// measurement once.</strong> A first draft assumed a map was a flat
/// array, and a probe against a real file answered with
/// <c>Klasse=RPG::Map</c> and eleven named fields, -- <strong>and that
/// is the difference between a reader that works and one that is
/// confidently wrong.</strong>
/// </para>
/// </remarks>
public static class RgssMapReader
{
    /// <summary>
    /// Reads one map file, and says what stopped it.
    /// </summary>
    /// <param name="pPath">A <c>.rxdata</c>, <c>.rvdata</c> or <c>.rvdata2</c>.</param>
    /// <param name="pMap">The map, when it could be read.</param>
    /// <param name="pError">What stopped it, and empty when nothing did.</param>
    /// <returns>Whether the file could be read at all.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a file it cannot read is said so rather than
    /// guessed at</strong>, -- <strong>and a project is untrusted
    /// input</strong>, -- <strong>and nothing outside the given path is
    /// opened.</strong>
    /// </para>
    /// </remarks>
    public static bool TryRead(
        string pPath, out RgssMap? pMap, out string pError)
    {
        pMap = null;
        pError = "";
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(pPath);
        }
        catch (IOException ausnahme)
        {
            pError = ausnahme.Message;
            return false;
        }
        catch (UnauthorizedAccessException ausnahme)
        {
            pError = ausnahme.Message;
            return false;
        }

        try
        {
            pMap = Von(new MarshalReader(bytes).Read(), out pError);
            return pMap != null;
        }
        catch (MarshalFormatException ausnahme)
        {
            pError = ausnahme.Message;
            return false;
        }
    }

    /// <summary>
    /// Builds a map from a marshalled value.
    /// </summary>
    /// <param name="pWert">The value tree.</param>
    /// <param name="pError">What stopped it.</param>
    /// <returns>The map, and nothing when the value is not one.</returns>
    private static RgssMap? Von(MarshalValue pWert, out string pError)
    {
        pError = "";
        if (pWert.ClassName != "RPG::Map")
        {
            pError = "a map is an RPG::Map, and this value is "
                + (pWert.ClassName ?? pWert.Kind);
            return null;
        }

        var breite = (int)Zahl(pWert, "@width");
        var hoehe = (int)Zahl(pWert, "@height");
        if (breite <= 0 || hoehe <= 0)
        {
            pError = $"the map says {breite}x{hoehe} tiles, and a map with"
                + " no tiles is not a map this reader can place";
            return null;
        }

        return new RgssMap
        {
            Width = breite,
            Height = hoehe,
            DisplayWidth = breite,
            DisplayHeight = hoehe,
            Data = KachelDaten(Wert(pWert, "@data"), breite, hoehe),
            Events = Ereignisse(Wert(pWert, "@events")),
        };
    }

    /// <summary>
    /// Reads one field of an object by the engine's own name.
    /// </summary>
    /// <param name="pWert">The object.</param>
    /// <param name="pFeld">The field, with its <c>@</c>.</param>
    /// <returns>The value, and nothing when the field is not there.</returns>
    /// <remarks>
    /// <strong>And the names are read from the file and not from a
    /// table here</strong>, -- <strong>because XP and VX disagree on
    /// which fields a page carries</strong> and <strong>agree on what
    /// they are called.</strong>
    /// </remarks>
    private static MarshalValue? Wert(MarshalValue pWert, string pFeld)
    {
        for (var i = 0; i < pWert.Keys.Count && i < pWert.Items.Count; i++)
        {
            if (pWert.Keys[i] == pFeld)
            {
                return pWert.Items[i];
            }
        }

        return null;
    }

    /// <summary>
    /// A field's number, and zero when it is not there or not a number.
    /// </summary>
    /// <param name="pWert">The object.</param>
    /// <param name="pFeld">The field.</param>
    /// <returns>The number, and zero is the engine's own empty answer.</returns>
    private static long Zahl(MarshalValue pWert, string pFeld) =>
        Wert(pWert, pFeld)?.Integer ?? 0;

    /// <summary>
    /// The tile rows, and it says so when they are not readable.
    /// </summary>
    /// <param name="pDaten">The <c>@data</c> field.</param>
    /// <param name="pBreite">How many tiles a row holds.</param>
    /// <param name="pHoehe">How many rows there are.</param>
    /// <returns>The rows, and none when the field is not readable.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>@data</c> comes back as <c>user defined</c></strong>,
    /// -- <strong>and that is not a failure of this reader but a
    /// property of the format</strong>: the tile array is written with a
    /// packing byte that the specification calls <c>user defined</c>.
    /// </para>
    /// <para>
    /// <strong>And a map whose tile field is packed still has its
    /// events</strong>, -- <strong>and dropping the whole map over a
    /// missing tile array would throw away the part a game spends its
    /// time on.</strong>
    /// </para>
    /// </remarks>
    private static IReadOnlyList<IReadOnlyList<int>> KachelDaten(
        MarshalValue? pDaten, int pBreite, int pHoehe)
    {
        if (pDaten == null || pDaten.Kind != "array")
        {
            return Array.Empty<IReadOnlyList<int>>();
        }

        if (pDaten.Items.Count != pHoehe)
        {
            return Array.Empty<IReadOnlyList<int>>();
        }

        var zeilen = new List<IReadOnlyList<int>>(pHoehe);
        foreach (var zeile in pDaten.Items)
        {
            if (zeile.Kind != "array" || zeile.Items.Count != pBreite)
            {
                return Array.Empty<IReadOnlyList<int>>();
            }

            var reihe = new List<int>(pBreite);
            foreach (var kachel in zeile.Items)
            {
                reihe.Add((int)(kachel.Integer ?? 0));
            }

            zeilen.Add(reihe);
        }

        return zeilen;
    }

    /// <summary>
    /// The events, and it reads the hash the way the format writes it.
    /// </summary>
    /// <param name="pEvents">The <c>@events</c> hash.</param>
    /// <returns>The events, and none when there is no such field.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a marshalled hash interleaves its keys and its
    /// values in <c>Items</c></strong>, -- <c>Items[2i]</c> is the key
    /// and <c>Items[2i+1]</c> the value, -- <strong>and <c>Keys[i]</c>
    /// is only the pair's description.</strong>
    /// </para>
    /// <para>
    /// <strong>And a first draft of this probe read <c>Items[i]</c>
    /// </strong>, -- <strong>which is the key</strong>, -- <strong>and
    /// the log answered with <c>Art integer</c></strong> for an event
    /// that is an <c>RPG::Event</c>.</strong>
    /// </para>
    /// </remarks>
    private static IReadOnlyList<RgssMapEvent> Ereignisse(
        MarshalValue? pEvents)
    {
        if (pEvents == null || pEvents.Kind != "hash")
        {
            return Array.Empty<RgssMapEvent>();
        }

        var alle = new List<RgssMapEvent>();
        for (var i = 0; i < pEvents.Keys.Count; i++)
        {
            var wertIndex = (i * 2) + 1;
            if (wertIndex >= pEvents.Items.Count)
            {
                break;
            }

            var ereignis = Ereignis(pEvents.Items[wertIndex]);
            if (ereignis != null)
            {
                alle.Add(ereignis);
            }
        }

        return alle;
    }

    /// <summary>
    /// Builds one event from its own fields.
    /// </summary>
    /// <param name="pWert">The event's value.</param>
    /// <returns>The event, and nothing when it is not an event.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>RPG::Event</c> carries five fields</strong>, --
    /// <c>@id</c>, <c>@name</c>, <c>@x</c>, <c>@y</c> and
    /// <c>@pages</c>, -- <strong>and the rest is a page.</strong>
    /// </para>
    /// </remarks>
    private static RgssMapEvent? Ereignis(MarshalValue pWert)
    {
        if (pWert.ClassName != "RPG::Event")
        {
            return null;
        }

        return new RgssMapEvent
        {
            Id = (int)Zahl(pWert, "@id"),
            Name = Wert(pWert, "@name")?.Text ?? "",
            X = (int)Zahl(pWert, "@x"),
            Y = (int)Zahl(pWert, "@y"),
            Pages = Seiten(Wert(pWert, "@pages")),
        };
    }

    /// <summary>
    /// The pages of one event.
    /// </summary>
    /// <param name="pWert">The <c>@pages</c> field.</param>
    /// <returns>The pages, and none when there are none.</returns>
    private static IReadOnlyList<RgssMapEventPage> Seiten(MarshalValue? pWert)
    {
        if (pWert == null || pWert.Kind != "array" || pWert.Items.Count == 0)
        {
            return Array.Empty<RgssMapEventPage>();
        }

        var seiten = new List<RgssMapEventPage>();
        foreach (var element in pWert.Items)
        {
            seiten.Add(Seite(element));
        }

        return seiten;
    }

    /// <summary>
    /// Builds one page, and every field is read by name.
    /// </summary>
    /// <param name="pWert">The page's value.</param>
    /// <returns>The page.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a page's command list is <c>@list</c></strong>, --
    /// <strong>and it is the only place a game's instructions
    /// live</strong>, -- <strong>and it is the last thing this reader
    /// needs before a Ruby Maker game can be read rather than
    /// only listed.</strong>
    /// </para>
    /// </remarks>
    private static RgssMapEventPage Seite(MarshalValue pWert)
    {
        var bedingungen = Wert(pWert, "@condition");
        var grafik = Wert(pWert, "@graphic");
        return new RgssMapEventPage
        {
            Trigger = (int)Zahl(pWert, "@trigger"),
            ConditionSwitchId = (int)Zahl(bedingungen ?? pWert, "@switch1_id"),
            ConditionSwitchValue = BedingungsWert(
                bedingungen, "@switch1_val"),
            ConditionItemId = (int)Zahl(bedingungen ?? pWert, "@item_id"),
            ConditionItemValue = BedingungsWert(
                bedingungen, "@item_valid")
                ?? BedingungsWert(bedingungen, "@item_id"),
            ConditionActorId = (int)Zahl(bedingungen ?? pWert, "@actor_id"),
            SelfSwitchCh = BedingungsWert(bedingungen, "@self_switch_ch") ?? "",
            CharacterName = Wert(grafik ?? pWert, "@character_name")?.Text ?? "",
            CharacterIndex = (int)Zahl(grafik ?? pWert, "@character_index"),
            Direction = (int)Zahl(grafik ?? pWert, "@character_direction"),
            Pattern = (int)Zahl(grafik ?? pWert, "@character_pattern"),
            Commands = Befehle(Wert(pWert, "@list")),
        };
    }

    /// <summary>
    /// Reads a condition field and says whether it carries a word.
    /// </summary>
    /// <param name="pBedingungen">The condition hash.</param>
    /// <param name="pFeld">The field.</param>
    /// <returns>The text, or nothing when the field is not there.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a condition's switch is a word and not a
    /// number</strong>, -- <c>switch1_val</c> is <c>"True"</c> or
    /// <c>"False"</c>, -- <strong>and a reader that asks for an integer
    /// gets zero for both</strong>, -- <strong>and zero is off</strong>,
    /// -- <strong>and the condition of every switched event in a game
    /// would be silently false.</strong>
    /// </para>
    /// </remarks>
    private static string? BedingungsWert(
        MarshalValue? pBedingungen, string pFeld)
    {
        var wert = Wert(pBedingungen ?? NEIN, pFeld);
        return wert?.Text;
    }

    private static readonly MarshalValue NEIN = new()
    {
        Kind = "nil",
    };

    /// <summary>
    /// Reads a command list, and keeps numbers and names apart.
    /// </summary>
    /// <param name="pWert">The <c>@list</c> field.</param>
    /// <returns>The commands, and none when there is no list.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And every command is three fields</strong>, --
    /// <c>code</c>, <c>indent</c> and <c>parameters</c>, -- <strong>and
    /// the third holds numbers and names together.</strong>
    /// </para>
    /// <para>
    /// <strong>And this is the reader that lets a Ruby Maker game be
    /// read rather than only listed.</strong>
    /// </para>
    /// </remarks>
    private static IReadOnlyList<RgssEventCommand> Befehle(
        MarshalValue? pWert)
    {
        if (pWert == null || pWert.Kind != "array" || pWert.Items.Count == 0)
        {
            return Array.Empty<RgssEventCommand>();
        }

        var alle = new List<RgssEventCommand>();
        foreach (var element in pWert.Items)
        {
            // **Und ein Befehl ist kein flaches Array, sondern ein
            // `RPG::EventCommand`-Objekt mit `@code`, `@indent` und
            // `@parameters`.**
            //
            // **Und das ist gemessen**, -- **an MicroQuest, wo Befehl 0
            // den Code 135 traegt und Befehl 1 den Code 134**,
            // -- **und ein Leser, der drei Positionen statt drei Namen
            // liest, haette hier `RPG::EventCommand` als Integer
            // gelesen und eine leere Liste geliefert.**
            if (element.ClassName != "RPG::EventCommand")
            {
                continue;
            }

            alle.Add(new RgssEventCommand
            {
                Code = (int)Zahl(element, "@code"),
                Indent = (int)Zahl(element, "@indent"),
                Parameters = Parameter(Wert(element, "@parameters")),
            });
        }

        return alle;
    }

    /// <summary>
    /// Turns one parameter list into values that say which they are.
    /// </summary>
    /// <param name="pWert">The parameter array.</param>
    /// <returns>The parameters, and none when it is not an array.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is where a Ruby Maker game is won or
    /// lost.</strong> <c>101 Show Text</c> carries a face name and an
    /// integer and a face name again, -- <strong>and
    /// <c>121 Switch</c> carries <c>"True"</c></strong>, -- <strong>and
    /// a reader that types every parameter as an integer turns
    /// <c>"True"</c> into zero</strong>, -- <strong>and zero is
    /// off</strong>, -- <strong>and every switch in the game is
    /// off.</strong>
    /// </para>
    /// </remarks>
    private static IReadOnlyList<RgssParameter> Parameter(
        MarshalValue? pWert)
    {
        if (pWert == null || pWert.Kind != "array")
        {
            return Array.Empty<RgssParameter>();
        }

        var alle = new List<RgssParameter>();
        foreach (var element in pWert.Items)
        {
            if (element.Text != null)
            {
                alle.Add(RgssParameter.Text_(element.Text));
                continue;
            }

            alle.Add(RgssParameter.Zahl_(element.Integer ?? 0));
        }

        return alle;
    }
}
