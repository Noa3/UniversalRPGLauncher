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
/// Which of the game's globals the engine declares, and that it is
/// eleven of thirteen.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test is the base for every other claim about
/// this game.</strong> -- <strong>Every adapter in this repository
/// stands or falls on whether a name is the engine's or a
/// plugin's</strong>, -- <strong>and until now that was carried in
/// prose.</strong>
/// </para>
/// <code>
/// $gameMap  JA rpg_managers.js      $gameNumberArray  NEIN  (Drill-Plugin)
/// $gameScreen JA rpg_managers.js    $gameStrings      NEIN  (Drill-Plugin)
/// $gameSystem JA rpg_managers.js    $gamePlayer       JA rpg_managers.js
/// $gameVariables JA rpg_managers.js $gameParty        JA rpg_managers.js
/// $gameActors JA rpg_managers.js    $gameSelfSwitches JA rpg_managers.js
/// $gameTemp  JA rpg_managers.js    $gameSwitches     JA rpg_managers.js
/// $gameMessage JA rpg_managers.js
/// </code>
/// </remarks>
public partial class TestMzGameStringsEngine : TestBase
{
    private const string Js = "D:/Itch/sister/www/js";

    private static bool EngineDefiniert(string pObjekt)
    {
        foreach (var datei in Directory.GetFiles(Js, "*.js"))
        {
            //  plugins.js  ist  eine  Liste,  keine  Definition.
            var name = Path.GetFileName(datei);
            if (name is "plugins.js" or "plugins")
            {
                continue;
            }

            if (File.ReadAllText(datei).Contains("$" + pObjekt + " ="))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// And eleven of the thirteen globals are the engine's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the two that are not are both
    /// <c>Drill_CoreOf…</c></strong>, -- <strong>and one of them this
    /// repository already answers</strong>, -- <strong>and one it now
    /// does.</strong>
    /// </para>
    /// </remarks>
    public void Test_ElfVonDreizehnSindEngine()
    {
        var engine = new[]
        {
            "gameMap", "gameScreen", "gameSystem", "gamePlayer",
            "gameVariables", "gameParty", "gameActors", "gameSelfSwitches",
            "gameTemp", "gameSwitches", "gameMessage",
        };
        var plugin = new[] { "gameNumberArray", "gameStrings" };

        foreach (var name in engine)
        {
            AssertTrue(EngineDefiniert(name),
                "**and `$" + name + "` is the engine's** -- and every"
                    + " adapter in this repository rests on that");
        }

        foreach (var name in plugin)
        {
            AssertTrue(!EngineDefiniert(name),
                "**and `$" + name + "` is not** -- and it lives in"
                    + " `Drill_CoreOfString.js` and"
                    + " `Drill_CoreOfNumberArray.js`, and a reader"
                    + " must not answer a name the engine did not"
                    + " declare");
        }

        AssertEq(11, engine.Length,
            "**and eleven globals are the engine's**");
        AssertEq(2, plugin.Length,
            "**and two are plugins', and both are Drill's**");
    }

    /// <summary>
    /// And a missing string is an empty string, not nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the third distinct answer in this
    /// repository for "a key that is not there":</strong> --
    /// <c>undefined</c> for <c>$gameMap.event()</c>, -- <c>0</c> for
    /// <c>$gameSelfVariables.value()</c>, -- <strong>and <c>""</c>
    /// here.</strong> -- <strong>And the game writes no question
    /// mark for any of the three.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieDreiAntworten()
    {
        var s = new MzGameStrings();

        AssertEq("", s.Value(7),
            "**and a string that was never written is empty** -- and"
                + " the plugin writes `this._data[stringId] || leer`");

        AssertTrue(!s.SetValue(0, "x"),
            "**and the number zero is refused** -- and the plugin"
                + " writes `if( stringId > 0 )`, and that is the"
                + " same refusal `$gameNumberArray` makes");

        AssertTrue(s.SetValue(7, 42),
            "**and a real number is stored**");
        AssertEq("42", s.Value(7),
            "**and it reads back as text** -- and the plugin writes"
                + " `String(value)`, and a reader that stored the"
                + " number would answer something else");

        AssertEq("", s.ConvertedValue(99),
            "**and a converted string that is not there is empty"
                + " too** -- and the plugin answers an empty string for a"
                + " falsy value before it does anything else");
        AssertEq(1, s.Anzahl,
            "**and one string is stored**");

        s.Clear();
        AssertEq("", s.Value(7),
            "**and `clear()` takes it away**");
    }

    /// <summary>
    /// And the 29 calls, and that they are plugin calls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>$gameStrings</c> is used 29 times in the game's
    /// scripts</strong>, -- <strong><c>value</c> 18 and
    /// <c>setValue</c> 11</strong>, -- <strong>and the engine's nine
    /// files mention it zero times.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieNeunundzwanzig()
    {
        var zeilen = new List<string>();

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

                zeilen.Add(ps[0].GetString() ?? "");
            }
        }

        foreach (var datei in Directory.GetFiles(
            "D:/Itch/sister/www/data", "Map*.json"))
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

        var ce = Path.Combine("D:/Itch/sister/www/data", "CommonEvents.json");
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

        var value = 0;
        var setValue = 0;
        foreach (var z in zeilen)
        {
            foreach (Match m in Regex.Matches(z,
                @"\$gameStrings\s*\.\s*(\w+)"))
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
        }

        Console.WriteLine($"value {value}  setValue {setValue}");
        AssertEq(18, value,
            "**and `$gameStrings.value()` is called 18 times**");
        AssertEq(11, setValue,
            "**and `setValue()` 11 times** -- and 29 in all, and"
                + " this reader answers both");
    }
}
