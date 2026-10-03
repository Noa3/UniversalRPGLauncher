using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What the game asks of <c>$gamePlayer</c>, and that finding it took
/// three classes of inheritance.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the first measurement was wrong.</strong> I counted
/// <c>Game_Player</c> alone and got <b>7</b> engine calls out of 102.
/// -- <strong>And <c>Game_Player</c> extends <c>Game_Character</c>
/// extends <c>Game_CharacterBase</c></strong>, -- <strong>and
/// <c>direction()</c>, <c>screenX()</c> and <c>opacity()</c> are
/// getters on the base class</strong>, -- <strong>not on
/// <c>Game_Player</c>.</strong>
/// </para>
/// <code>
/// Game_Player          72 Methoden
/// Game_Character       34
/// Game_CharacterBase  100
/// Kette gesamt:       187
/// </code>
/// <para>
/// <strong>And through the chain the answer is 22 methods
/// (36 calls with the fields)</strong>, -- <strong>and 80 calls
/// belong to plugins.</strong>
/// </para>
/// </remarks>
public partial class TestMzGamePlayerKette : TestBase
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
    /// And the three classes, and that the chain is longer than one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the reason the first count was 7 and not
    /// 22</strong>, -- <strong>and the assertion is written so that a
    /// future MZ version that flattens the chain has to change this
    /// test rather than silently change the count.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieDreiKlassen()
    {
        var js = File.ReadAllText("D:/Itch/sister/www/js/rpg_objects.js");

        AssertTrue(js.Contains(
            "var Game_Player = class extends Game_Character {"),
            "**and `Game_Player` extends `Game_Character`** -- and"
                + " that is why counting `Game_Player` alone found"
                + " only 7 engine calls");
        AssertTrue(js.Contains(
            "var Game_Character = class extends Game_CharacterBase {"),
            "**and `Game_Character` extends `Game_CharacterBase`** --"
                + " and `direction()`, `screenX()` and `opacity()`"
                + " are getters on that class and not on"
                + " `Game_Player`");

        var anzahl = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (name, anfang) in new[]
        {
            ("Game_Player", "var Game_Player = class"),
            ("Game_Character", "var Game_Character = class"),
            ("Game_CharacterBase", "var Game_CharacterBase = class"),
        })
        {
            var i = js.IndexOf(anfang, StringComparison.Ordinal);
            AssertTrue(i > 0, "**and " + name + " is in the file**");
            var j = js.IndexOf("\nvar ", i + 10, StringComparison.Ordinal);
            var seg = js.Substring(i, (j > 0 ? j : i + 30000) - i);
            var n = Regex.Matches(seg, @"^    (\w+)\([^)]*\)\s*\{",
                RegexOptions.Multiline).Count;
            anzahl[name] = n;
        }

        Console.WriteLine(string.Join("  ", anzahl.Select(
            x => x.Key + " " + x.Value)));

        AssertEq(72, anzahl["Game_Player"],
            "**and `Game_Player` has 72 methods**");
        AssertEq(100, anzahl["Game_CharacterBase"],
            "**and `Game_CharacterBase` has 100** -- and 63 of those"
                + " are getters without parameters, and the game's"
                + " six most used player names are among them");
        // **Und  die  drei  Klassen  teilen  sich  Namen.**
        // **Und  die  drei  Klassen  teilen  sich  19  Namen.**
        var alle = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anfang in new[]
        {
            "var Game_Player = class",
            "var Game_Character = class",
            "var Game_CharacterBase = class",
        })
        {
            var i = js.IndexOf(anfang, StringComparison.Ordinal);
            var j = js.IndexOf("\nvar ", i + 10,
                StringComparison.Ordinal);
            var seg = js.Substring(i, (j > 0 ? j : i + 30000) - i);
            foreach (Match m in Regex.Matches(seg,
                @"^    (\w+)\([^)]*\)\s*\{",
                RegexOptions.Multiline))
            {
                alle.Add(m.Groups[1].Value);
            }
        }

        Console.WriteLine("eindeutig " + alle.Count);
        AssertEq(187, alle.Count,
            "**and the chain holds 187 distinct names** -- and"
                + " 72 + 34 + 100 is 206, so the three share 19,"
                + " and a reader that counted the sum would"
                + " overstate the surface by 19");
    }

    /// <summary>
    /// And what the engine answers, and what it does not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And 22 calls are engine methods and 19 more are engine
    /// fields</strong>, -- <strong>and 61 are plugins.</strong>
    /// </para>
    /// </remarks>
    public void Test_WasDieEngineBeantwortet()
    {
        var js = File.ReadAllText("D:/Itch/sister/www/js/rpg_objects.js");
        var engine = new HashSet<string>(StringComparer.Ordinal);
        var felder = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (anfang, istBasis) in new[]
        {
            ("var Game_Player = class", false),
            ("var Game_Character = class", false),
            ("var Game_CharacterBase = class", true),
        })
        {
            var i = js.IndexOf(anfang, StringComparison.Ordinal);
            var j = js.IndexOf("\nvar ", i + 10, StringComparison.Ordinal);
            var seg = js.Substring(i, (j > 0 ? j : i + 30000) - i);
            foreach (Match m in Regex.Matches(seg,
                @"^    (\w+)\([^)]*\)\s*\{", RegexOptions.Multiline))
            {
                engine.Add(m.Groups[1].Value);
            }

            if (istBasis)
            {
                foreach (Match m in Regex.Matches(seg,
                    @"this\.(_\w+)\s*=", RegexOptions.Multiline))
                {
                    felder.Add(m.Groups[1].Value);
                }
            }
        }

        AssertEq(30, felder.Count,
            "**and the base class has 30 fields** -- and `_x`,"
                + " `_realX` and `_through` are among them, and the"
                + " game reads those 14 times");

        var methoden = 0;
        var feldAufrufe = 0;
        var plugin = 0;
        var treffer = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var s in Skripte())
        {
            foreach (Match m in Regex.Matches(s,
                @"\$gamePlayer\s*\.\s*(\w+)"))
            {
                var n = m.Groups[1].Value;
                if (engine.Contains(n))
                {
                    methoden++;
                    treffer.TryGetValue(n, out var k);
                    treffer[n] = k + 1;
                }
                else if (felder.Contains(n))
                {
                    feldAufrufe++;
                }
                else
                {
                    plugin++;
                }
            }
        }

        Console.WriteLine("methoden " + methoden + "  felder " + feldAufrufe
            + "  plugin " + plugin);
        Console.WriteLine(string.Join("  ", treffer.OrderByDescending(
            x => x.Value).Take(6).Select(x => x.Key + " " + x.Value)));

        AssertEq(22, methoden,
            "**and 22 calls are engine methods** -- and `direction`"
                + " is six of them, and it is a getter on"
                + " `Game_CharacterBase`, and the first measurement"
                + " that only looked at `Game_Player` found 7");
        AssertEq(19, feldAufrufe,
            "**and 19 calls are engine fields** -- and they are"
                + " `_realX` nine, `_x` five, `_realY` three and"
                + " `_through` two, and all four are among the 30"
                + " fields `Game_CharacterBase` sets in"
                + " `this._x = ...`");
        AssertEq(61, plugin,
            "**and 61 calls belong to plugins** -- and"
                + " `startOpacity` is 14 of those, and it is not one"
                + " of the 187 method names on the chain, and 22 + 19"
                + " + 61 is the 102 that were there");
    }
}
