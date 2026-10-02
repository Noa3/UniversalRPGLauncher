using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A finished RPG Maker MZ project on this machine, measured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the number criterion 7 needs, and it was
/// written down from a fixture.</strong> Measured on
/// <c>CamelliaCoronation-Win</c>: twenty maps, a real tileset, and
/// <strong>2436 event commands in 34 distinct codes</strong>.
/// </para>
/// <para>
/// <strong>And 1556 of them run, and 880 do not.</strong> Among the 880:
/// <c>241 Play BGM</c> four times, <c>250 Play SE</c> eighteen times,
/// <c>221 Fadeout Screen</c> sixteen, <c>301 Battle Processing</c> seven.
/// <strong>A game whose music never starts and whose battles never
/// begin is a game that runs and is not the game.</strong>
/// </para>
/// <para>
/// <strong>And the 114 named commands are a fact about the table, and
/// not about a game.</strong> <c>MzCommandSet.All</c> names 114,
/// <c>MzCommands.TryExecute</c> has 13 cases and
/// <c>MzControlFlow.TryExecute</c> 8 — <strong>so 21 execute and 93 do
/// not.</strong>
/// </para>
/// <para>
/// <strong>And a number that is worse than assumed is a number to write
/// down, and not a number to round.</strong>
/// </para>
/// </remarks>
public partial class TestRealMzGameData : TestBase
{
    private const string SpielWurzel =
        "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static string? Wurzel()
    {
        return Directory.Exists(SpielWurzel) ? SpielWurzel : null;
    }

    private static void UeberspringeWennKeinSpiel()
    {
        if (Wurzel() == null)
        {
            GD.Print(
                "    (skipped: no real MZ project at " + SpielWurzel
                + " — the command table was not measured against a game "
                + "of 2436 commands, and a green run that checked nothing "
                + "is the worst form of it)");
        }
    }

    /// <summary>
    /// The finished project's own commands are counted, and the count is
    /// compared with what this reader can execute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the counting walks the JSON structure, and not a
    /// regular expression.</strong> A first pass counted 3509 and found
    /// codes 0, 1, 2, 3, 29 and 505 among them — **and those are not
    /// commands at all**: they sit inside
    /// <c>205 Set Movement Route</c>'s parameter, which is a movement list
    /// of its own. **A regex cannot tell a command from a parameter, and a
    /// number that counts parameters is a number about nothing.**
    /// </para>
    /// <para>
    /// <strong>And the 880 that do not run are named, so the next session
    /// does not have to rediscover them.</strong> <c>241 Play BGM</c>,
    /// <c>250 Play SE</c>, <c>221 Fadeout Screen</c>,
    /// <c>222 Fadein Screen</c>, <c>225 Shake Screen</c>,
    /// <c>301 Battle Processing</c>, <c>105 Show Scrolling Text</c>,
    /// <c>123 Control Self Switch</c>, <c>129 Change Party Member</c>,
    /// <c>203 Set Event Location</c>, <c>213 Show Balloon Icon</c>,
    /// <c>314 Recover All</c>, <c>322 Change Actor Images</c>.
    /// </para>
    /// </remarks>
    public void Test_DieBefehleDesFertigenMzSpielsSindGezaehlt()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        var vorkommen = new Dictionary<int, int>();

        void Zaehle(List<JsonElement>? pListe)
        {
            if (pListe == null)
            {
                return;
            }

            foreach (var element in pListe)
            {
                if (element.ValueKind != JsonValueKind.Object
                    || !element.TryGetProperty("code", out var rohCode))
                {
                    continue;
                }

                if (!rohCode.TryGetInt32(out var code))
                {
                    continue;
                }

                vorkommen[code] = vorkommen.GetValueOrDefault(code) + 1;
            }
        }

        var karten = Directory.GetFiles(
            Path.Combine(wurzel, "data"), "Map*.json");
        AssertTrue(karten.Length > 15,
            "**and the project has a game's worth of maps** -- "
                + karten.Length + " were found, and a project with "
                + "two is a fixture with a game's name on it");

        foreach (var karte in karten)
        {
            var daten = JsonDocument.Parse(File.ReadAllText(karte)).RootElement;
            if (daten.ValueKind != JsonValueKind.Object
                || !daten.TryGetProperty("events", out var events)
                || events.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var rohEvent in events.EnumerateArray())
            {
                if (rohEvent.ValueKind != JsonValueKind.Object
                    || !rohEvent.TryGetProperty("pages", out var seiten))
                {
                    continue;
                }

                foreach (var rohSeite in seiten.EnumerateArray())
                {
                    if (rohSeite.ValueKind == JsonValueKind.Object
                        && rohSeite.TryGetProperty("list", out var liste))
                    {
                        Zaehle(liste.EnumerateArray().ToList());
                    }
                }
            }
        }

        var gemeinsam = Path.Combine(wurzel, "data", "CommonEvents.json");
        if (File.Exists(gemeinsam))
        {
            foreach (var rohEvent in JsonDocument.Parse(
                File.ReadAllText(gemeinsam)).RootElement.EnumerateArray())
            {
                if (rohEvent.ValueKind == JsonValueKind.Object
                    && rohEvent.TryGetProperty("list", out var liste))
                {
                    Zaehle(liste.EnumerateArray().ToList());
                }
            }
        }

        var gesamt = vorkommen.Values.Sum();
        AssertTrue(gesamt > 1000,
            "**and the project carries a game's worth of commands** -- "
                + gesamt + " in " + vorkommen.Count
                + " codes; the measured number is 2436 in 34, and a "
                + "number that dropped means the walk stopped early");

        // **Und was davon laeuft, gemessen an den 21 Faellen, die der
        // Interpreter wirklich hat.**
        // **Und die 22 Codes, die `MzCommands` und `MzControlFlow`
        // wirklich anfassen — gemessen, nicht behauptet.** Die Liste
        // steht hier und nicht im Interpreter, **denn der Test soll
        // fragen, was der Interpreter kann, und nicht mit ihm
        // dasselbe sagen.**
        // **Und diese Liste ist handgepflegt, und das war schon einmal
        // ein Fehler** -- **denn `105`, `225` und `314` standen
        // hier nicht, obwohl sie laufen.** **Und jetzt steht hier
        // genau das, was `UniversalRPG.Web.MzCommands.GateBefehle()` behauptet**, **und
        // `test_mz_command_coverage_honest` vergleicht beides**, **und
        // eine Liste, die auseinanderlaufen kann, wird nicht mehr
        // gepflegt, sondern gelesen.**
        var ausfuehrbar = new HashSet<int>(
            UniversalRPG.Web.MzCommands.GateBefehle()
                .Concat(UniversalRPG.Web.MzCommands.SteuerungsBefehle()));


        var nicht = vorkommen
            .Where(pKvp => !ausfuehrbar.Contains(pKvp.Key))
            .ToList();

        System.Console.WriteLine(
            "MZ  gemessen: " + gesamt + " Befehle, "
            + (gesamt - nicht.Sum(pKvp => pKvp.Value)) + " ausfuehrbar, "
            + nicht.Sum(pKvp => pKvp.Value) + " nicht");
        System.Console.WriteLine(
            "MZ  nicht: " + string.Join(", ", nicht
                .OrderByDescending(pKvp => pKvp.Value)
                .Select(pKvp => pKvp.Key + "(" + pKvp.Value + ")")));

        // **Und die fuenf, die bleiben, haben keine Methode in der Engine
        // und sind damit keine Befehle** -- **und das ist an
        // `rmmz_objects.js` gemessen, wo keines der fuenf vorkommt:**
        //
        // ```text
        // Game_Interpreter.prototype.command505   not found
        // Game_Interpreter.prototype.command405   not found
        // Game_Interpreter.prototype.command404   not found
        // Game_Interpreter.prototype.command412   not found
        // ```
        //
        // **Und die drei mit Methoden sind `105`, `225` und `314`, und
        // `105` traegt seine Zeilen in `405`-Eintraegen, und `505` ist ein
        // Routenpunkt im Parameter von `205`** -- **und ein
        // handgepflegter Zaehler, der das nicht weiss, meldet eine
        // Ausfuehrung, die es gibt, als fehlend**, **und das war 4
        // Scrolltexte und 2 Erschuetterungen und 1 Genesung.**
        var ohneMethode = new HashSet<int> { 0, 404, 405, 412, 505 };
        var echteLuecke = nicht
            .Where(pKvp => !ohneMethode.Contains(pKvp.Key))
            .ToList();
        System.Console.WriteLine(
            "MZ  echte Luecke: " + (echteLuecke.Count == 0
                ? "keine -- alle fuenf sind Codes ohne Engine-Methode"
                : string.Join(", ", echteLuecke
                    .Select(pKvp => pKvp.Key + "(" + pKvp.Value + ")"))));

        AssertTrue(echteLuecke.Count == 0,
            "**and every code the game carries that has a method in the"
            + " engine runs here** -- and these do not: "
            + (echteLuecke.Count == 0
                ? "none"
                : string.Join(", ", echteLuecke
                    .Select(pKvp => pKvp.Key + "(" + pKvp.Value + ")"))));
        AssertTrue(nicht.Count > 0,
            "**and this reader still cannot run every command the game "
                + "uses** -- "
                + nicht.Sum(pKvp => pKvp.Value) + " of " + gesamt
                + " in " + nicht.Count
                + " codes, and a green run that said 'all commands "
                + "run' would have been false");
    }
}
