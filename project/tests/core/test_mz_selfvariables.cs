using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Mz;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The self variables, measured against the game and against the
/// plugin that defines them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the measurement is six methods and 679 calls</strong>:
///
/// <code>
/// .get()       251     .value()      52
/// .set()       246     .setValue()   40
/// .add()        84     .addValue()    6
/// </code>
///
/// <strong>And 251 of them are <c>get(this, 'frames')</c> or one of
/// four other names</strong>, -- <strong>and the single most common
/// line is <c>let id = $gameSelfVariables.get(this, 'frames')</c>,
/// 78 times.</strong>
/// </para>
/// </remarks>
public partial class TestMzSelfVariables : TestBase
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

        var ce = Path.Combine(Wurzel, "CommonEvents.json");
        if (File.Exists(ce))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ce));
            foreach (var e in doc.RootElement.EnumerateArray())
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
    /// And the six methods, counted out of the game's scripts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And all six are plugin methods</strong>, -- <strong>and
    /// that is why the name is answered here and not read from RPG
    /// Maker.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSechsMethoden()
    {
        var skripte = Skripte();
        var zaehler = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in skripte)
        {
            foreach (Match m in Regex.Matches(s,
                @"\$gameSelfVariables\.([A-Za-z_]\w*)"))
            {
                var n = m.Groups[1].Value;
                zaehler.TryGetValue(n, out var k);
                zaehler[n] = k + 1;
            }
        }

        Console.WriteLine(string.Join("  ", zaehler.OrderByDescending(
            x => x.Value).Select(x => x.Key + " " + x.Value)));

        AssertEq(251, zaehler.GetValueOrDefault("get"),
            "**and `get` is called 251 times** -- and that is"
                + " `get(interpreter, key)`, which is a map id, an"
                + " event id and a name");
        AssertEq(246, zaehler.GetValueOrDefault("set"),
            "**and `set` is called 246 times**");
        AssertEq(84, zaehler.GetValueOrDefault("add"),
            "**and `add` is called 84 times**");
        AssertEq(52, zaehler.GetValueOrDefault("value"),
            "**and `value` is called 52 times directly**");
        AssertEq(40, zaehler.GetValueOrDefault("setValue"),
            "**and `setValue` is called 40 times** -- and the game"
                + " writes `setValue([$gameMap.mapId(), 1, 'rainyCreation'], 0)`,"
                + " and that is the array form of the key");
    }

    /// <summary>
    /// And a key that is not there gives zero, not nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole point of the class</strong>, --
    /// <strong>and the plugin writes
    /// <c>return this._data[key] || 0</c></strong>, -- <strong>and
    /// that is the opposite of <c>$gameMap.event()</c></strong>, --
    /// <strong>which answers <c>undefined</c> and makes the game
    /// write a question mark.</strong> -- <strong>And the game writes
    /// <c>get(this, 'frames')</c> 251 times with no question
    /// mark</strong>, -- <strong>and that is what the zero is
    /// for.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinFehlenderSchluesselIstNull()
    {
        var v = new MzGameSelfVariables();

        AssertEq(0, v.Get(3, 1, "frames"),
            "**and a key that was never stored answers zero** -- and"
                + " the plugin writes `this._data[key] || 0`, and the"
                + " game calls `get(this, 'frames')` 251 times"
                + " without a question mark anywhere, and that is"
                + " what the zero is for");

        v.Set(3, 1, "frames", 12);
        AssertEq(12, v.Get(3, 1, "frames"),
            "**and what was stored comes back**");

        v.Add(3, 1, "frames", -5);
        AssertEq(7, v.Get(3, 1, "frames"),
            "**and `add` reads the current value first** -- and the"
                + " game calls it 84 times, and 44 of those are"
                + " `set(this, 'frames', get(this, 'frames') - ...)`,"
                + " which is the same thing written out");

        // **Und  die  drei  Teile  sind  drei  Schritte  und  nicht
        // ein  Name.**
        AssertEq(0, v.Get(4, 1, "frames"),
            "**and the same name on another map is another key** --"
                + " and the plugin writes `[interpreter._mapId,"
                + " interpreter._eventId, key]`");
        AssertEq(0, v.Get(3, 2, "frames"),
            "**and the same name on another event is another key**");
    }

    /// <summary>
    /// And a string the game handed in is parsed, as the plugin does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the plugin parses before storing</strong>, --
    /// <strong>and the game writes
    /// <c>set(this, 'fudou', 99)</c> with a number and also hands
    /// strings to <c>setValue</c></strong>, -- <strong>and a reader
    /// that stored the raw string would answer differently.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineZahlAlsTextWirdGelesen()
    {
        var v = new MzGameSelfVariables();
        v.Set(1, 1, "n", "42");
        AssertEq(42, v.Get(1, 1, "n"),
            "**and a number the game wrote as text reads back as a"
                + " number** -- and the plugin writes `_parseInt(value)`"
                + " before it stores");

        v.Set(1, 1, "leer", "");
        AssertEq(0, v.Get(1, 1, "leer"),
            "**and text that is not a number reads as zero** -- and"
                + " that is what `parseInt` gives and what this"
                + " repository must not answer differently");
    }
}
