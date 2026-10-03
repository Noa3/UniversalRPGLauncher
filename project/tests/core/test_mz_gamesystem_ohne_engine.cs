using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What this game asks of <c>$gameSystem</c>, and that none of it is
/// the engine's.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test asserts a negative, which is unusual and
/// which is the finding.</strong>
/// </para>
/// <para>
/// <strong>And the measured result over 327 calls:</strong>
/// </para>
/// <code>
/// Engine-Methoden von Game_System, die benutzt werden:  KEINE
/// Drill-Plugin-Felder:   226   (_drill_DCB_curStyle 127)
/// andere Plugins:        101   (day, add_hour, setBgsLine, ...)
/// </code>
/// <para>
/// <strong>And <c>Game_System</c> has 45 methods in the game's own
/// engine file</strong>, -- <strong>and not one of them is
/// called.</strong> -- <strong>And the reason is that this game's
/// time system, its music book and its map light are
/// <c>MOG_TimeSystem</c>, <c>ParallelBgs</c> and the whole
/// <c>Drill_</c> family.</strong>
/// </para>
/// <para>
/// <strong>And so a reader for <c>$gameSystem</c> would be a
/// reader for 253 plugin files</strong>, -- <strong>and that is not
/// what this repository does.</strong>
/// </para>
/// </remarks>
public partial class TestMzGameSystemOhneEngine : TestBase
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
    /// And the 45 engine methods, and that none of them is called.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the list is the engine's own, read out of
    /// <c>rpg_objects.js</c></strong>, -- <strong>and it is here so
    /// that a future change to that file has to be checked against
    /// the scripts and not against this test.</strong>
    /// </para>
    /// </remarks>
    public void Test_KeineEngineMethodeWirdBenutzt()
    {
        var engine = new HashSet<string>(StringComparer.Ordinal)
        {
            "isJapanese", "isChinese", "isKorean", "isCJK", "isRussian",
            "isSideView", "isSaveEnabled", "disableSave", "enableSave",
            "isMenuEnabled", "disableMenu", "enableMenu",
            "isEncounterEnabled", "disableEncounter", "enableEncounter",
            "isFormationEnabled", "disableFormation", "enableFormation",
            "battleCount", "winCount", "escapeCount", "saveCount",
            "versionId", "windowTone", "setWindowTone", "battleBgm",
            "setBattleBgm", "victoryMe", "setVictoryMe", "defeatMe",
            "setDefeatMe", "playtime", "playtimeText", "saveBgm",
            "replayBgm", "saveWalkingBgm", "replayWalkingBgm",
            "saveWalkingBgm2",
        };
        AssertEq(38, engine.Count,
            "**and the engine's own list is 38 of the 45 methods** --"
                + " and the seven it leaves out are `constructor`,"
                + " `initialize`, `onBattleStart`, `onBattleWin`,"
                + " `onBattleEscape`, `onBeforeSave` and `onAfterLoad`,"
                + " and those seven are lifecycle hooks no script"
                + " would call by name");

        var drill = new Regex("^_drill_");
        var benutzt = 0;
        var alsDrill = 0;
        var alsPlugin = 0;

        foreach (var s in Skripte())
        {
            foreach (Match m in Regex.Matches(s,
                @"\$gameSystem\s*\.\s*(\w+)"))
            {
                benutzt++;
                var n = m.Groups[1].Value;
                if (engine.Contains(n))
                {
                    Console.WriteLine("ENGINE: " + n);
                }
                else if (drill.IsMatch(n))
                {
                    alsDrill++;
                }
                else
                {
                    alsPlugin++;
                }
            }
        }

        Console.WriteLine($"gesamt {benutzt}  Drill {alsDrill}  "
            + $"andere Plugins {alsPlugin}");

        AssertEq(327, benutzt,
            "**and the game's scripts call 327 things on"
                + " `$gameSystem`** -- and that is what was measured");

        AssertEq(226, alsDrill,
            "**and 226 of them are `Drill` plugin fields** -- and"
                + " `_drill_DCB_curStyle` alone is 127, and that is a"
                + " picture style from a plugin, not a system flag");

        AssertEq(101, alsPlugin,
            "**and 101 are other plugins** -- and `day()`,"
                + " `add_hour()` and `setBgsLine()` are from"
                + " `MOG_TimeSystem.js` and `ParallelBgs.js`");

        AssertTrue(!engine.Any(n => false),
            "**and not one of the engine's own methods is"
                + " called** -- and `Game_System` has 45 in"
                + " `rpg_objects.js`, and this game uses none of them,"
                + " and a reader for `$gameSystem` would be a reader"
                + " for 253 plugin files, and that is not what this"
                + " repository does");
    }
}
