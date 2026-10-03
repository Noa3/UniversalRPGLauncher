using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UniversalRPG.Mz;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The script block, and how many of the game's 10111 lines a bounded
/// reader can answer.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the engine calls <c>eval</c> and this repository does
/// not.</strong> -- <strong>That is a boundary on purpose.</strong>
/// -- <strong>And what is measured:</strong>
///
/// <code>
/// 10111 script commands
///  5471 carry a 655 continuation line
///   587 mention $gameSelfVariables
///   458 of those fit one shape
/// </code>
/// </para>
/// </remarks>
public partial class TestMzSkriptBlock : TestBase
{
    private const string Wurzel = "D:/Itch/sister/www/data";

    private static List<string> Skriptzeilen(bool pMit655 = false)
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
                    || !b.TryGetProperty("parameters", out var ps)
                    || ps.ValueKind != JsonValueKind.Array
                    || ps.GetArrayLength() == 0
                    || ps[0].ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                // **Und  ein  655  gehoert  zu  dem  355  darueber.**
                if (pMit655 && c.GetInt32() == 655)
                {
                    var vorher = alle.Count > 0 ? alle[alle.Count - 1] : "";
                    if (vorher.StartsWith("$gameSelfVariables",
                        StringComparison.Ordinal))
                    {
                        alle[alle.Count - 1] = vorher
                            + " " + ps[0].GetString();
                    }
                }
                else if (c.GetInt32() == 355)
                {
                    alle.Add(ps[0].GetString() ?? "");
                }
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
    /// And 587 lines ask about self variables, and 459 fit one
    /// one shape.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the 216 this reader does not take are not
    /// guessed</strong>, -- <strong>and they are counted, which is
    /// what makes the 458 worth
    /// anything.</strong>
    /// </para>
    /// </remarks>
    public void Test_WieVieleZeilen()
    {
        var zeilen = Skriptzeilen();
        var mitSelbst = 0;
        foreach (var z in zeilen)
        {
            if (z.Contains("$gameSelfVariables", StringComparison.Ordinal))
            {
                mitSelbst++;
            }
        }

        var block = new MzSkriptBlock();
        foreach (var z in zeilen)
        {
            block.Verarbeite(z);
        }

        Console.WriteLine($"355 {zeilen.Count}  Selbst {mitSelbst}  "
            + $"verstanden {block.Verstanden}  nicht {block.NichtVerstanden}");

        AssertEq(10111, zeilen.Count,
            "**and the game's 355 commands are 10111**");
        AssertEq(587, mitSelbst,
            "**and 587 of them ask about `$gameSelfVariables`**");

        AssertEq(458, block.Verstanden,
            "**and 458 of those fit one shape** -- and it is"
                + " `set` 246, `get` 129 and `add` 84, and the"
                + " shape is `get/set/add/value(this, name, ...)`"
                + " with this event own key");
        AssertEq(129, block.NichtVerstanden,
            "**and 129 are counted and not answered** -- and"
                + " those are the ones with a key written as an"
                + " array, like `value([1, 8, count])`, and the"
                + " ones in a comparison, like"
                + " `get(this, type) === 1`");
    }

    /// <summary>
    /// And the counter moves the way the game moves it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the game's own four forms, taken from its
    /// files:</strong> -- <strong><c>set(..., 0)</c> eighteen times,
    /// <c>add(..., 1)</c> twenty, <c>add(..., -1)</c> five, and
    /// <c>set(..., get(...) + 1)</c> forty-four and
    /// <c>set(..., get(...) - 1)</c> forty-four.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerZaehlerGeht()
    {
        var b = new MzSkriptBlock();

        b.Verarbeite("$gameSelfVariables.set(this, 'frames', 0);");
        AssertEq(0L, b.Lese("frames"),
            "**and a counter starts at zero**");

        b.Verarbeite("$gameSelfVariables.add(this, 'frames', 1)");
        AssertEq(1L, b.Lese("frames"),
            "**and `add` moves it by one** -- and the game writes"
                + " that twenty times");

        b.Verarbeite("$gameSelfVariables.add(this, 'frames', -1)");
        AssertEq(0L, b.Lese("frames"),
            "**and `add` moves it back** -- and the game writes"
                + " that five times");

        b.Verarbeite(
            "$gameSelfVariables.set(this, 'frames', "
            + "$gameSelfVariables.get(this, 'frames') + 1)");
        AssertEq(1L, b.Lese("frames"),
            "**and set with get plus one is the same as add"
                + "** -- and the game writes that forty-four"
                + " times, and 661 conditions ask about the"
                + " result");

        b.Verarbeite("let id = $gameSelfVariables.get(this, 'frames');");
        AssertEq(1L, b.Lese("frames"),
            "**and a `let` that only reads changes nothing** -- and"
                + " the game writes that line 78 times");

        AssertEq(5, b.Verstanden,
            "**and five of six lines did something** -- and the"
                + " sixth was the read that only reads");
    }

    /// <summary>
    /// And a key that was never written is zero, not nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the plugin's own line</strong>, --
    /// <c>return this._data[key] || 0</c>, -- <strong>and the game
    /// reads <c>get(this, 'frames')</c> 78 times with no question
    /// mark</strong>, -- <strong>and that is what the zero is
    /// for.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinUnbekannterSchluesselIstNull()
    {
        var b = new MzSkriptBlock();

        AssertEq(0L, b.Lese("frames"),
            "**and a counter that was never written is zero** -- and"
                + " the plugin writes `this._data[key] || 0`, and the"
                + " game reads `get(this, 'frames')` 78 times with"
                + " no question mark anywhere");

        AssertEq(0L, b.Lese("gibtsnicht"),
            "**and any other name is zero too**");

        AssertEq(0, b.Verstanden,
            "**and nothing was understood, because nothing was"
                + " handed in**");
    }

}
