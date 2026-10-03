using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Where the 10 111 script commands of this game come from.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the answer to "what does this game need", and
/// it is not "a JavaScript interpreter".</strong>
/// </para>
/// <code>
///   4038  lokal    let eid = this._eventId;
///   3222  Engine   $gameMap.event(...), SceneManager.push(...)
///   1541  $game*   die vierzehn Spielobjekte
///   1284  Plugin   QJ.MPMZ.Shoot({...}), this.showMapEventDialogue(0)
///     26  sonstiges
/// </code>
/// <para>
/// <strong>And 1284 of them are plugins</strong> -- <strong>and this
/// game has 114 active plugins among 253 files</strong>, --
/// <strong>and a reader that understood every plugin would be a
/// reader for 253 third-party libraries</strong> --
/// <strong>and that is a different thing from reading RPG
/// Maker.</strong>
/// </para>
/// <para>
/// <strong>And 1328 of them read <c>this._eventId</c></strong>, --
/// <strong>and that is the interpreter's own field</strong>, --
/// <strong>and it is where the reading begins.</strong>
/// </para>
/// </remarks>
public partial class TestMzSkriptHerkunft : TestBase
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

        using var ce = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Wurzel, "CommonEvents.json")));
        foreach (var e in ce.RootElement.EnumerateArray())
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                Seite(e);
            }
        }

        return alle;
    }

    private static readonly string[] Spielobjekte =
    {
        "$gameMap", "$gameSelfVariables", "$gameScreen", "$gameSystem",
        "$gameNumberArray", "$gamePlayer", "$gameVariables", "$gameParty",
        "$gameActors", "$gameSelfSwitches", "$gameTemp", "$gameStrings",
        "$gameSwitches", "$gameMessage",
    };

    private static readonly string[] Engine =
    {
        "this", "SceneManager", "AudioManager", "Utils", "DataManager",
        "StorageManager", "WindowBase", "Input", "Graphics", "Audio",
        "Math",
    };

    /// <summary>
    /// And the four kinds, counted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asserts the four counts</strong>, --
    /// <strong>because they say what the next step is</strong>: --
    /// <strong>reading <c>$game*</c> is 1541 lines,</strong> --
    /// <strong>and the engine calls are 3222,</strong> --
    /// <strong>and the plugins are 1284 and are not RPG Maker
    /// at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_VierArten()
    {
        var skripte = Skripte();
        var lokal = 0;
        var engine = 0;
        var spiel = 0;
        var plugin = 0;
        var evid = 0;

        foreach (var s in skripte)
        {
            var m = Regex.Match(s.Trim(),
                @"^(?:let|var|const)?\s*([A-Za-z_$][\w$]*)");
            if (!m.Success)
            {
                continue;
            }

            var name = m.Groups[1].Value;
            if (Array.IndexOf(Spielobjekte, name) >= 0)
            {
                spiel++;
            }
            else if (Array.IndexOf(Engine, name) >= 0)
            {
                engine++;
            }
            else if (name.Contains('.') || char.IsUpper(name[0]))
            {
                plugin++;
            }
            else
            {
                lokal++;
            }

            if (s.Contains("this._eventId"))
            {
                evid++;
            }
        }

        Console.WriteLine($"lokal {lokal}  Engine {engine}  "
            + $"$game* {spiel}  Plugin {plugin}");
        Console.WriteLine($"mit this._eventId: {evid}");

        AssertEq(10111, skripte.Count,
            "**and this game has 10111 script commands** -- and that"
                + " is the number a reader has to answer, and not the"
                + " 4952 that stood in earlier reports");

        AssertEq(1541, spiel,
            "**and 1541 of them begin with one of the fourteen game"
                + " objects** -- and those are the lines a reader can"
                + " answer without evaluating JavaScript");

        AssertEq(3222, engine,
            "**and 3222 begin with an engine name** -- and those are"
                + " calls into the game's own engine files");

        AssertEq(1284, plugin,
            "**and 1284 are plugin calls** -- and this game has 114"
                + " active plugins among 253 files, and a reader that"
                + " understood all of them would be a reader for 253"
                + " third-party libraries, and that is not the same"
                + " thing as reading RPG Maker");

        AssertEq(1328, evid,
            "**and 1328 read `this._eventId`** -- and that is the"
                + " interpreters own field, and it is where reading"
                + " begins");
    }
}
