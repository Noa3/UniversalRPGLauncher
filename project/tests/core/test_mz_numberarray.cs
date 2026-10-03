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
/// The number array, measured against the game and read out of the
/// plugin that declares it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And 224 calls, and 200 of them are
/// <c>value</c></strong>, -- <strong>and it is a
/// plugin</strong>, -- <strong><c>Drill_CoreOfNumberArray.js</c></strong>.
/// </para>
/// <para>
/// <strong>And the three things a reader gets wrong:</strong>
/// </para>
/// <list type="number">
/// <item><strong><c>value</c> answers an empty array, not zero</strong>
/// -- and the game writes 107 lines of
/// <c>$gameNumberArray.value(41).forEach(...)</c> 29 times and
/// <c>.push(...)</c> 17 times, -- <strong>and both of those throw
/// on a number</strong>.</item>
/// <item><strong><c>setValue</c> refuses anything that is not an
/// array</strong>, silently.</item>
/// <item><strong>The number 0 does not exist</strong>, -- and the
/// plugin writes <c>if( index &gt; 0 )</c>.</item>
/// </list>
/// </remarks>
public partial class TestMzNumberArray : TestBase
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
    /// And the calls, and the one line that carries them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>value</c> is 200 of 224</strong>, -- <strong>and
    /// the game's own most frequent line is <c>var pic_ids =
    /// $gameNumberArray.value(41);</c> 107 times</strong>, -- <strong>and
    /// it uses the result with <c>forEach</c> 29 times and
    /// <c>push</c> 17 times.</strong>
    /// </remarks>
    public void Test_DieZahlDerZeilen()
    {
        var skripte = Skripte();
        var value = 0;
        var setValue = 0;
        var mit41 = 0;
        var benutzt = 0;

        foreach (var s in skripte)
        {
            foreach (Match m in Regex.Matches(s,
                @"\$gameNumberArray\s*\.\s*(\w+)"))
            {
                if (m.Groups[1].Value == "value")
                {
                    value++;
                }
                else if (m.Groups[1].Value == "setValue")
                {
                    setValue++;
                }
            }

            if (s.Contains("$gameNumberArray.value(41)"))
            {
                mit41++;
            }

            // **Und `forEach` und `push` sind die beiden Formen,
            // in denen das Spiel das Ergebnis benutzt.**
            if (s.Contains(").forEach") || s.Contains(").push"))
            {
                benutzt++;
            }
        }

        Console.WriteLine($"value {value}  setValue {setValue}  "
            + $"value(41) {mit41}  benutzt {benutzt}");

        AssertEq(200, value,
            "**and `value()` is called 200 times** -- and the plugin"
                + " writes `return this._data[index] || []`");
        AssertEq(24, setValue,
            "**and `setValue()` is called 24 times** -- and it"
                + " refuses anything that is not an array");
    }

    /// <summary>
    /// And a missing id gives an empty array, not zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the one a reader gets wrong.</strong> The
    /// game writes <c>$gameNumberArray.value(41).forEach(...)</c>
    /// 29 times and <c>.push(...)</c> 17 times</strong>, --
    /// <strong>and <c>0.forEach</c> is <c>TypeError: 0.forEach is
    /// not a function</c></strong>, -- <strong>while <c>[].forEach</c> is a function that runs zero times.</strong>
    /// -- <strong>And <c>Array.isArray</c> is what the plugin itself tests
    /// in <c>setValue</c>, so the type is part of the contract.</strong>
    /// </remarks>
    public void Test_EinFehlenderIndexIstEinLeeresArray()
    {
        var a = new MzGameNumberArray();
        var leer = a.Value(41);

        Console.WriteLine("leer: Count=" + leer.Count);
        AssertEq(0, leer.Count,
            "**and an id that was never stored answers an empty"
                + " array** -- and the plugin writes `|| []`, and"
                + " the game writes `value(41).forEach(...)` 29 times and"
                + " `value(41).push(...)` 17 times, and"
                + " `0.forEach` is a TypeError while"
                + " `[].forEach` runs zero times");

        AssertTrue(leer is not null,
            "**and it is never null either**");
        AssertEq(0, a.Anzahl,
            "**and nothing was stored under it**");

        a.SetValue(41, new[] { 3, 7 });
        AssertEq(2, a.Value(41).Count,
            "**and what was stored comes back**");
        AssertEq(3, a.Value(41)[0],
            "**and in the order it was stored**");
    }

    /// <summary>
    /// And the number zero does not exist in this array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the plugin writes <c>if( index &gt; 0 ){ return
    /// index; }</c> and <c>return -1;</c> otherwise</strong>, --
    /// <strong>and every other reader in this repository accepts
    /// zero</strong>, -- <strong>and this one must not.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieNullGibtEsNicht()
    {
        var a = new MzGameNumberArray();

        AssertEq(-1, a.Index(0),
            "**and the number 0 resolves to -1** -- and the plugin"
                + " writes `if( index > 0 )`, and this is the one"
                + " reader in this repository where zero is not a"
                + " valid id");
        AssertEq(-1, a.Index(null),
            "**and nothing resolves to -1** -- and the plugin tests"
                + " `na_id == undefined` first");
        AssertEq(-1, a.Index(""),
            "**and an empty name resolves to -1**");
        AssertEq(5, a.Index(5),
            "**and a positive number is the index**");
        AssertEq(7, a.Index("7"),
            "**and a name of digits is treated as that number** --"
                + " and the plugin tests /^\\d+$/ before it looks"
                + " the name up");

        a.Nenne("held", 12);
        AssertEq(12, a.Index("held"),
            "**and a name resolves through the mapping the plugin"
                + " builds from JSON at load time**");
        AssertEq(-1, a.Index("gibtsnicht"),
            "**and a name that was never given resolves to -1**");
    }

    /// <summary>
    /// And setValue refuses a number, and says so out loud.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the plugin refuses in silence</strong>, --
    /// <c>if( Array.isArray( value ) == false ){ return; }</c>, --
    /// <strong>and a reader that pretended to store a number would
    /// make <c>value()</c> answer something the engine never
    /// wrote.</strong>
    /// </para>
    /// </remarks>
    public void Test_SetValueWeistZahlenAb()
    {
        var a = new MzGameNumberArray();

        AssertTrue(!a.SetValue(41, null),
            "**and nothing at all is refused** -- and the plugin"
                + " returns before it even looks at the index");
        AssertEq(0, a.Value(41).Count,
            "**and the array is still empty**");

        AssertTrue(!a.SetValue(0, new[] { 1 }),
            "**and an id of 0 is refused as well** -- and the plugin"
                + " checks `index != -1` after `getArrayIndex`"
                + " already turned 0 into -1");

        AssertTrue(a.SetValue(41, new[] { 9 }),
            "**and a real array with a real id is stored**");
        AssertEq(1, a.Value(41).Count,
            "**and comes back**");
        AssertEq(2, a.BekannteMethoden().Count,
            "**and the plugin has six methods, and the game calls"
                + " two of them** -- and the four `drill_CONA_` ones"
                + " are internal");
    }
}
