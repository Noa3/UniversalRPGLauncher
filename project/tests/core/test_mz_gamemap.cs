using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
using UniversalRPG.Mz;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The map object, measured against the game's own scripts.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the measurement is the point</strong>, -- <strong>and
/// it was taken from the game, not from a doc:</strong>
///
/// <code>
/// $gameMap.event()   870
/// $gameMap.mapId()   130
/// das sind 1000 von 1187
/// </code>
///
/// <strong>And the other 187 are plugin methods</strong> --
/// <c>chahuiMapTemp</c>, <c>getGroupBulletListQJ</c>,
/// <c>drill_COET_getEventsByTag_direct</c> -- <strong>and this
/// repository has no business answering those.</strong>
/// </para>
/// </remarks>
public partial class TestMzGameMap : TestBase
{
    private const string Wurzel = "D:/Itch/sister/www/data";

    private static List<string> Skripte()
    {
        var alle = new List<string>();

        void Seite(JsonElement pSeite)
        {
            if (!pSeite.TryGetProperty("list", out var liste)
                || liste.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var b in liste.EnumerateArray())
            {
                if (b.ValueKind != JsonValueKind.Object
                    || !b.TryGetProperty("code", out var c)
                    || c.GetInt32() != 355
                    || !b.TryGetProperty("parameters", out var ps)
                    || ps.ValueKind != JsonValueKind.Array
                    || ps.GetArrayLength() == 0
                    || ps[0].ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                alle.Add(ps[0].GetString() ?? "");
            }
        }

        foreach (var datei in Directory.GetFiles(Wurzel, "Map*.json"))
        {
            if (Path.GetFileName(datei)
                .Equals("MapInfos.json", StringComparison.OrdinalIgnoreCase)
                || new FileInfo(datei).Length > 4_000_000)
            {
                continue;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(datei));
            }
            catch (JsonException)
            {
                continue;
            }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("events", out var ev)
                    || ev.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var e in ev.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object
                        || !e.TryGetProperty("pages", out var seiten)
                        || seiten.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var s in seiten.EnumerateArray())
                    {
                        if (s.ValueKind == JsonValueKind.Object)
                        {
                            Seite(s);
                        }
                    }
                }
            }
        }

        // **Und `CommonEvents.json` gehoert dazu**, --
        // **und mein erster Test las nur die Maps** --
        // **und zaehlte 808 statt 870**, --
        // **und die Differenz waren 62 Aufrufe in den
        // CommonEvents.**
        var ce = Path.Combine(Wurzel, "CommonEvents.json");
        if (File.Exists(ce))
        {
            using var ceDoc = JsonDocument.Parse(File.ReadAllText(ce));
            foreach (var e in ceDoc.RootElement.EnumerateArray())
            {
                if (e.ValueKind == JsonValueKind.Object)
                {
                    Seite(e);
                }
            }
        }

        return alle;
    }

    /// <summary>
    /// And the two methods that carry 1000 calls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this counts them out of the game's own scripts</strong>
    /// -- <strong>and asserts that the reader answers exactly
    /// these</strong>, -- <strong>because a reader that answered
    /// everything would be a reader that answered nothing
    /// correctly.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieHundertAufrufe()
    {
        var skripte = Skripte();
        var event_ = 0;
        var mapId = 0;
        var plugin = 0;

        foreach (var s in skripte)
        {
            foreach (Match m in Regex.Matches(s,
                @"\$gameMap\.([A-Za-z_]\w*)"))
            {
                var name = m.Groups[1].Value;
                if (name == "event")
                {
                    event_++;
                }
                else if (name == "mapId")
                {
                    mapId++;
                }
                else
                {
                    plugin++;
                }
            }
        }

        Console.WriteLine($"event() {event_}  mapId() {mapId}  "
            + $"Plugin {plugin}");

        AssertEq(870, event_,
            "**and `$gameMap.event()` appears 870 times** -- and the"
                + " engine writes it as one line, `return"
                + " this._events[eventId]`, and this reader answers"
                + " it");

        AssertEq(130, mapId,
            "**and `$gameMap.mapId()` appears 130 times** -- and the"
                + " engine writes it as `return this._mapId`");

        AssertEq(187, plugin,
            "**and 187 calls are plugin methods** -- and this"
                + " repository does not answer them, and a list of"
                + " what it does not answer is worth more than a"
                + " number that pretends to");
    }

    /// <summary>
    /// And an id with no event gives nothing, not a crash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the case the game itself writes with a
    /// question mark</strong>, -- <c>$gameMap.event(18)?.start()</c>
    /// appears 12 times, -- <strong>and the engine answers
    /// <c>undefined</c> there and throws on
    /// null.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineLeereIdGibtNichts()
    {
        var karte = new MzGameMap { MapId = 3, Width = 20, Height = 15 };
        karte.Setze(1, new MzGameEvent { Id = 1, Name = "Tor" });

        AssertTrue(karte.Event(1) != null,
            "**and an event that exists comes back**");
        AssertTrue(karte.Event(18) == null,
            "**and an event that does not exist gives nothing** -- and"
                + " the game writes `$gameMap.event(18)?.start()` 12"
                + " times, and that question mark is safe"
                + " navigation against exactly this");
        AssertEq(1, karte.Events().Count,
            "**and `events()` drops what is not there** -- and the"
                + " engine writes `.filter(event => !!event)`, and"
                + " that is why the count is smaller than the keys");
        AssertEq(3, karte.MapId,
            "**and `mapId()` is the number `setup` wrote**");
    }

    /// <summary>
    /// And the reader names what it answers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And `Game_Map` has 110 methods in the game's own
    /// engine file</strong>, -- <strong>and this reader answers
    /// six</strong>, -- <strong>and the six are the six the scripts
    /// actually use most.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasBekannte()
    {
        var karte = new MzGameMap();
        Console.WriteLine("bekannt: "
            + string.Join(", ", karte.BekannteMethoden()));
        AssertTrue(karte.BekannteMethoden().Contains("event")
                && karte.BekannteMethoden().Contains("mapId"),
            "**and the reader says which methods it answers** -- and"
                + " `Game_Map` has 110 in the game's own file, and"
                + " six of them carry the scripts this repository"
                + " reads");
    }
}
