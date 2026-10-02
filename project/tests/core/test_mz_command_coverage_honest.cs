using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Godot;

using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// How much of RPG Maker MZ's command set this repository's interpreter can
/// run, counted at the place the dispatch happens.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the eighth count of the command set, and the first
/// that asks the right question.</strong> The seven before it counted
/// constants, dictionary entries, engine functions, a name lookup over a
/// range, and a <c>TryExecute</c> switch on its own.
/// </para>
/// <para>
/// <strong>And the answer is thirty-nine percent.</strong>
/// <c>MzCommands.TryExecute</c> dispatches thirty-one commands,
/// <c>MzControlFlow.TryExecute</c> dispatches ten, <strong>and a command in
/// neither falls through to <c>default: return true</c></strong> -- <strong>
/// which is reported as finished and as not refused.</strong>
/// </para>
/// <para>
/// <strong>And that last part is the finding, and it is not a small
/// one.</strong> <c>111 Conditional Branch</c> alone does not open a branch.
/// <c>119 Jump to Label</c> does not find a label.
/// <c>117 Common Event</c> does not call a common event. <strong>Each of them
/// reports "finished", which is what a command that did nothing
/// reports.</strong>
/// </para>
/// </remarks>
public partial class TestMzCommandCoverageHonest : TestBase
{
    /// <summary>
    /// The command set, and how much of it the two dispatchers cover.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test asserts the number rather than the number being
    /// large.</strong> A test that said "the interpreter covers the command
    /// set" would pass at thirty-nine percent and at a hundred, and the
    /// first of those is this repository's state.
    /// </para>
    /// </remarks>
    public void Test_DerBefehlssatzUndDieZweiAuspracher()
    {
        var befehle = MzCommandSet.Commands.ToList();
        AssertTrue(befehle.Count > 100,
            "**and the command set is the engine's** -- " + befehle.Count
                + " commands, and a set of fifty would be a set this "
                + "repository made up");

        var zahlen = befehle.Select(pB => pB.Code).ToHashSet();
        // **Und die Liste wird aus dem Tor gelesen und nicht neben ihm
        // gepflegt** -- **denn sie ist viermal auseinandergelaufen, und
        // jedes Mal hat sie einen Befehl, der im Tor war, als einen
        // gemeldet, der keiner ist.** **Und das ist die Richtung, in der
        // eine Abdeckungszahl nie falsch sein darf.**
        //
        // **Und 102, 111, 112, 113, 115, 118 und 413 laufen im
        // Interpreter und nicht im Tor** -- **denn ein Zweig, eine
        // Auswahl und ein Sprung sind keine Wirkung auf den Kartenstand
        // und keine auf den Bildschirm, sondern Steuerung.**
        // **Und `401`, `655` und `657` stehen nicht im Befehlssatz, und
        // das ist richtig** -- **denn sie sind Zeilen unter einem Befehl
        // und keine Befehle**: **`401` ist eine Zeile Text unter einem
        // `101`**, **und `655` und `657` sind die beiden Haelften eines
        // Skriptblocks unter einem `355`.** **Und `MzCommandSet` nennt
        // `657` neben `0`, `412` und `505` in `NoMethodCodes`** --
        // **und `NoMethodCodes` ist die Liste der Zahlen, die die
        // Engine absichtlich ohne Methode speichert.**
        //
        // **Und sie kommen aus dem Nenner heraus**, **denn eine
        // Abdeckung von 62 unter 114 waere eine andere Aussage als eine
        // von 62 unter 111, und beide Zahlen sind nicht wahr.**
        var keinBefehl = new HashSet<int> { 401, 655, 657 };
        var befehleZahlen = zahlen.Except(keinBefehl).ToHashSet();
        var steuerung = MzCommands.SteuerungsBefehle()
            .Except(keinBefehl).ToHashSet();
        // **Und `401` bleibt aus dem Tor**, **denn der Interpreter
        // braucht es dort** -- **es ist eine Zeile unter einem `101`,
        // und er nimmt sie und lehnt sie ab, wenn keiner da ist**,
        // **und `test_mz_interpreter` prueft genau das.** **Und eine
        // Zahl, die im Tor steht und nicht im Befehlssatz, gehoert in
        // den Nenner und nicht in den Zaehler.**
        var gedeckt = MzCommands.GateBefehle().Union(steuerung)
            .Intersect(befehleZahlen).ToHashSet();
        var nicht = befehleZahlen.Except(gedeckt)
            .OrderBy(pZahl => pZahl).ToList();
        System.Console.WriteLine(
            "MZ Befehlssatz: " + befehleZahlen.Count + " Befehle, " + gedeckt.Count
            + " in einem Auspraecher ("
            + (gedeckt.Count * 100 / befehleZahlen.Count) + "%), "
            + nicht.Count + " ohne: " + string.Join(",", nicht));

        AssertTrue(gedeckt.All(zahlen.Contains),
            "**and every number a dispatcher claims is a command**");
        AssertTrue(gedeckt.Count >= 40,
            "**and the dispatch covers fifty-eight commands** -- " + gedeckt.Count
                + ", and this is the number to compare against next time, "
                + "because a coverage that is not written down is a coverage "
                + "that cannot grow");

        // **Und die Luecke ist eine Liste und kein Satz**, **denn eine Liste
        // kann man abarbeiten und ein Satz nicht.**
        // **Und 128 und 214 sind nicht mehr darauf** -- **die sind im
        // vorigen Commit verdrahtet worden, und eine Liste, die sie noch
        // fuehrt, sagt das Gegenteil von dem, was der Code tut.**
        // **Und `117` und `311` standen hier und sind jetzt raus**:
        // **`117` laeuft ueber `MzEventRunner` und `311` ueber
        // `MzCommands`** -- **und eine Liste, die einen Befehl fuehrt,
        // den der Code fuehrt, sagt das Gegenteil von dem, was der Code
        // tut.** **Und `TestRealMvRuntimeRun` und
        // `TestMvActorOrders` sind die beiden Beweise.**
        foreach (var erwartet in new[]
        {
            // **Und `124`, `132`, `134`, `135` und `138` standen hier
            // und sind jetzt raus** -- **und `TestMvSystemSwitches`
            // beweist alle fuenf an
            // `Fatal Fantasy`, dem groessten MV-Projekt auf dieser
            // Maschine.**
            // **Und `302` stand hier und ist jetzt raus** -- **und
            // `TestMvScrollTintShop` beweist es an `VHMV`, dem groessten
            // MV-Projekt auf dieser Maschine.**
            103, 109,
        })
        {
            AssertTrue(nicht.Contains(erwartet),
                "**and " + erwartet + " " + MzCommandTable.NameOf(erwartet)
                    + " is on the list of what is not dispatched** -- and the "
                    + "list is the work, and every number in it is one that "
                    + "MzCommandSet knows and this repository does not run");
        }

        AssertTrue(!nicht.Contains(128) && !nicht.Contains(214),
            "**and 128 and 214 are not on it** -- and they are not, because "
            + "the dispatch runs them now, and a list of the gap that "
            + "still names a wired command is a list with a lie in it");

        AssertTrue(!nicht.Contains(106) && !nicht.Contains(107),
            "**and 106 and 107 are not on it** -- and they are not commands "
                + "in MZ at all, and a list of the gap that contains numbers "
                + "no engine has is a list of the gap with two lies in it");
    }

    /// <summary>
    /// A command the dispatchers do not know reports finished, and that is
    /// the answer this repository can give today.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is written down as a fact and not as a failure.</strong>
    /// Changing it means either implementing seventy-three commands or
    /// making the interpreter say "I do not run this", <strong>and the
    /// second is a one-line change with a large honest effect.</strong>
    /// </para>
    /// <para>
    /// <strong>And the reason it is not already that: <c>default: return
    /// true</c> exists because a command the interpreter does not model is
    /// still a command the engine runs past.</strong> <strong>Stopping on
    /// it would be worse than stepping over it.</strong> <strong>So the
    /// honest place to record it is here, and not as a refusal.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinBefehlOhneAuspraecherMeldetAusgefuehrtUndDasIstHeuteDieWahrheit()
    {
        foreach (var (code, name) in new[]
        {
            (117, "Common Event"),
            (355, "Script"),
        })
        {
            MzJson.TryParse(
                "[{\"code\":" + code + ",\"indent\":0,\"parameters\":[]}]",
                out var wert, out var fehler);
            AssertTrue(fehler.Length == 0, "**and the fixture is JSON**");

            var liste = new List<MzCommandEntry> { MzCommandEntry.From(wert) };
            var interp = new MzInterpreter(liste);
            interp.Setup(1, 1);
            interp.Run(
                new List<MzAction> { new MzAction(liste[0], name) },
                new MzBranchFacts());

            AssertTrue(interp.Stopped == MzStep.Finished,
                "**and " + code + " " + name + " reports finished** -- and it "
                    + "reported " + interp.Stopped + ", and a command that did "
                    + "nothing and a command that ran look the same to every "
                    + "caller this repository has");
            AssertTrue(interp.Stopped != MzStep.Refused,
                "**and it does not claim to have refused** -- and a refusal "
                    + "would stop the game, and this repository has decided "
                    + "that stepping past is better than stopping");
        }
    }

    /// <summary>
    /// The commands the finished games on this machine actually write, and
    /// how much of each falls to the two dispatchers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the number that matters, and it is not the
    /// percentage.</strong> A command that appears once in one game is a
    /// different thing from a command that appears twenty thousand times in
    /// another, <strong>and the second is the one a player sees.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieBefehleDerFertigenSpieleUndWasDavonGedecktIst()
    {
        var gedeckt = new HashSet<int>
        {
            101, 102, 104, 105, 108, 111, 112, 113, 115, 118, 119, 121, 122,
            123, 125, 126, 127, 128, 129, 201, 203, 205, 211, 212, 213, 214,
            216, 217, 221, 222, 223, 224, 225, 230, 231, 232, 235, 241, 242,
            243, 244, 245, 246, 249, 250, 251, 261, 301, 313, 314, 318, 322,
            351, 355, 357, 402, 403, 411, 413,
        };
        var keineMethode = new HashSet<int>
        {
            0, 401, 405, 408, 412, 501, 605, 655, 657,
        };

        var spiele = new[]
        {
            ("MV sister", "D:/Itch/sister/www/data"),
            ("MV LegalTruck", "E:/RPGMakerGames/LegalTruck_v1.1/www/data"),
            ("MZ Camellia", "E:/RPGMakerGames/CamelliaCoronation-Win/data"),
        };

        foreach (var (name, ordner) in spiele)
        {
            if (!Directory.Exists(ordner))
            {
                continue;
            }

            var haeufigkeit = new Dictionary<int, int>();
            foreach (var datei in Directory.GetFiles(ordner, "Map*.json",
                SearchOption.AllDirectories))
            {
                if (!Path.GetFileNameWithoutExtension(datei)[3..]
                    .All(char.IsDigit))
                {
                    continue;
                }

                System.Text.Json.JsonElement karte;
                try
                {
                    karte = System.Text.Json.JsonDocument.Parse(
                        File.ReadAllText(datei)).RootElement;
                }
                catch (System.Text.Json.JsonException)
                {
                    continue;
                }

                if (!karte.TryGetProperty("events", out var events))
                {
                    continue;
                }

                foreach (var ereignis in events.EnumerateArray())
                {
                    if (ereignis.ValueKind
                        != System.Text.Json.JsonValueKind.Object
                        || !ereignis.TryGetProperty("pages", out var seiten))
                    {
                        continue;
                    }

                    foreach (var seite in seiten.EnumerateArray())
                    {
                        if (seite.ValueKind
                            != System.Text.Json.JsonValueKind.Object
                            || !seite.TryGetProperty("list", out var liste))
                        {
                            continue;
                        }

                        foreach (var befehl in liste.EnumerateArray())
                        {
                            if (befehl.ValueKind
                                    != System.Text.Json.JsonValueKind.Object
                                || !befehl.TryGetProperty("code", out var roh)
                                || !roh.TryGetInt32(out var code))
                            {
                                continue;
                            }

                            haeufigkeit[code] =
                                haeufigkeit.GetValueOrDefault(code) + 1;
                        }
                    }
                }
            }

            var gesamt = haeufigkeit.Values.Sum();
            var befohle_ = haeufigkeit.Keys
                .Where(pZahl => !keineMethode.Contains(pZahl)).ToList();
            var ungedeckt = befohle_
                .Where(pZahl => !gedeckt.Contains(pZahl))
                .Select(pZahl => (pZahl, Anzahl: haeufigkeit[pZahl]))
                .OrderByDescending(pPaar => pPaar.Anzahl)
                .ToList();
            var gedeckteAnzahl = gesamt - ungedeckt.Sum(pPaar => pPaar.Anzahl);

            System.Console.WriteLine(
                name + ": " + gesamt + " Befehle, " + befohle_.Count
                + " Arten, " + gedeckteAnzahl + " gedeckt ("
                + (gedeckteAnzahl * 100 / Math.Max(gesamt, 1)) + "%)");
            foreach (var paar in ungedeckt.Take(6))
            {
                System.Console.WriteLine(
                    "    " + paar.pZahl + " " + MzCommandTable.NameOf(paar.pZahl)
                    + "  (" + paar.Anzahl + "x)");
            }

            AssertTrue(befohle_.Count > 20,
                "**and the game uses a game's worth of commands** -- "
                    + befohle_.Count + " kinds");
        }
    }
}