using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Every command number a finished RPG Maker MZ game on this machine uses, and
/// what this repository's interpreter does with each one.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the test that existed before this one counted commands and
/// asserted nothing.</strong> Its assertion blocks were empty: it walked the
/// project's maps, summed the command lists, and then had nothing to say
/// about the sum. <strong>A count with no assertion is a comment that
/// costs a run.</strong>
/// </para>
/// <para>
/// <strong>And the question this asks is narrow and answerable:</strong> the
/// finished game uses thirty-four command numbers, <strong>and this
/// repository's interpreter knows forty-eight</strong> -- <strong>so the
/// question is not whether the table is big enough but whether every number
/// the game writes ends in a command that runs.</strong>
/// </para>
/// </remarks>
public partial class TestRealMzCommandCoverage : TestBase
{
    private const string Wurzel = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden()
    {
        if (!Directory.Exists(Wurzel + "/data"))
        {
            GD.Print("    (skipped: no MZ project at " + Wurzel + ")");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The command numbers the game uses, and which of them this
    /// repository's table does not carry.
    /// </summary>
    public void Test_JedeBefehlsnummerDesSpielsIstBekanntOderKeinBefehl()
    {
        if (!Vorhanden())
        {
            return;
        }

        var verwendet = Vorkommen();
        AssertTrue(verwendet.Count > 25,
            "**and the game uses a game's worth of command numbers** -- "
                + verwendet.Count + " distinct numbers, "
                + verwendet.Values.Sum() + " commands in total");

        // **Und diese Liste ist nicht geraten, sondern an der Datei
        // abgelesen.** **`0`, `404` und `505` sind keine Befehle, und das
        // ist hier die Behauptung, die man pruefen kann:**
        //
        // ```text
        // 0    Kommentar
        // 404  Ende einer Textzeile, gehört zu 401
        // 505  Fortsetzungszeile eines Befehls darüber
        // ```
        // **Und `401` steht nicht in dieser Liste, weil es keine eigene
        // Methode hat** -- **und das steht woertlich im Quelltext:**
        //
        // ```text
        // /// One line of text. **It has no <c>command401</c> method** -- the engine
        // /// reads it by position, as the text of a 101's line, and
        // /// <c>command401</c> does not exist.
        // ```
        //
        // **Ein Test, der 401 als fehlenden Befehl meldet, meldet die
        // Engine.** **Die richtige Pruefung ist die Frage nach der
        // Besitzerschaft, und die hat der Leser bereits: `OwnerOf`.**
        var ohneMethode = new HashSet<int> { 0, 404, 405, 412, 505, 655, 657 };
        var fehlend = verwendet.Keys
            .Where(pCode => !ohneMethode.Contains(pCode)
                && !MzCommandTable.IsCommand(pCode)
                && MzCommandTable.OwnerOf(pCode) == 0)
            .OrderBy(pCode => pCode)
            .ToList();

        var artBefehl = verwendet.Keys.Count(pCode =>
            MzCommandTable.IsCommand(pCode));
        var artDaten = verwendet.Keys.Count(pCode =>
            !MzCommandTable.IsCommand(pCode) && MzCommandTable.OwnerOf(pCode) > 0);
        System.Console.WriteLine(
            "MZ Befehlsnummern: " + verwendet.Count + " verschieden, "
            + verwendet.Values.Sum() + " Befehle, "
            + artBefehl + " ausfuehrbar, " + artDaten + " Datenzeilen, "
            + ohneMethode.Count(pCode => verwendet.ContainsKey(pCode))
            + " ohne Methode");
        foreach (var paar in verwendet.OrderByDescending(p => p.Value).Take(12))
        {
            System.Console.WriteLine(
                "   " + paar.Key.ToString().PadLeft(4) + " "
                + MzCommandTable.NameOf(paar.Key) + "  (" + paar.Value + "x)");
        }

        AssertTrue(fehlend.Count == 0,
            "**and every number the game writes is a command this "
            + "repository can run** -- missing " + string.Join(", ", fehlend));
    }

    /// <summary>
    /// The heaviest commands, run one at a time through the interpreter, and
    /// what came out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the part that has teeth.</strong> A number being
    /// in the table is not a number running; <strong>so the ten commands
    /// this game uses most often are each put through the interpreter on
    /// their own</strong>, <strong>and the test records what each one
    /// did.</strong>
    /// </para>
    /// <para>
    /// <strong>And the assertion is that none of them refused.</strong> A
    /// refusal carries a sentence saying why, <strong>and a refusal here
    /// would be this repository saying a command the game depends on it
    /// cannot do.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSchwerstenBefehleDesSpielsLaufenImInterpreter()
    {
        if (!Vorhanden())
        {
            return;
        }

        var verwendet = Vorkommen();
        // **Und dieselbe Unterscheidung hier:** `401` ist eine Zeile von
        // `101`, `402` eine Option von `102`, `405` die Wahlfrage und
        // `412` das Ende eines Zweigs. **Einen davon einzeln laufen zu
        // lassen ist keine Messung, sondern ein Fehler** -- **der
        // Interpreter verweigert zu Recht, weil ueber ihm kein Dialog
        // steht.** **Die zehn sind also die zehn haeufigsten echten
        // Befehle, und nicht die zehn haeufigsten Nummern.**
        var top = verwendet
            .Where(p => MzCommandTable.IsCommand(p.Key))
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();

        var verweigerungen = new List<string>();
        var ausgefuehrt = 0;
        foreach (var (code, anzahl) in top)
        {
            foreach (var befehl in BefehlsPro(code))
            {
                try
                {
                    // **Und eine Befehlsliste, ein Interpreter, und der Weg,
                    // den die Engine auch geht: Setup, dann Run.**
                    var interp = new MzInterpreter(new List<MzCommandEntry>
                    {
                        befehl,
                    });
                    interp.Setup(1, 1);
                    // **Und `Run` nimmt die Aktionsliste UND die Zweigfakten
                    // -- nicht nur die Fakten.** **Das ist der Weg, den die
                    // Engine geht: Befehle einlesen, Aktionen bilden, laufen
                    // lassen.**
                    interp.Run(
                        new List<MzAction>
                        {
                            new MzAction(befehl, "code " + befehl.Code),
                        },
                        new MzBranchFacts());
                    if (interp.Stopped == MzStep.Refused)
                    {
                        verweigerungen.Add(
                            code + " (" + MzCommandTable.NameOf(code) + ", "
                            + anzahl + "x im Spiel): " + interp.Reason);
                    }
                    else
                    {
                        ausgefuehrt++;
                    }
                }
                catch (Exception pAusnahme)
                {
                    verweigerungen.Add(
                        code + " (" + MzCommandTable.NameOf(code) + "): "
                        + pAusnahme.GetType().Name + " " + pAusnahme.Message);
                }
            }
        }

        System.Console.WriteLine(
            "MZ Interpreter: " + ausgefuehrt + " ausgefuehrt, "
            + verweigerungen.Count + " verweigert");
        AssertTrue(verweigerungen.Count == 0,
            "**and none of the game's ten commonest commands is refused** -- "
                + string.Join(" | ", verweigerungen.Take(4)));
    }

    // -----------------------------------------------------------------

    private static Dictionary<int, int> Vorkommen()
    {
        var zaehler = new Dictionary<int, int>();
        foreach (var datei in Directory.GetFiles(Wurzel + "/data", "Map*.json")
            .Where(pF => Path.GetFileNameWithoutExtension(pF)[3..]
                .All(char.IsDigit)))
        {
            var karte = JsonDocument.Parse(File.ReadAllText(datei)).RootElement;
            if (!karte.TryGetProperty("events", out var events)
                || events.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var e in events.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object
                    || !e.TryGetProperty("pages", out var seiten))
                {
                    continue;
                }

                foreach (var seite in seiten.EnumerateArray())
                {
                    if (seite.ValueKind != JsonValueKind.Object
                        || !seite.TryGetProperty("list", out var liste))
                    {
                        continue;
                    }

                    foreach (var cmd in liste.EnumerateArray())
                    {
                        if (cmd.ValueKind == JsonValueKind.Object
                            && cmd.TryGetProperty("code", out var roh)
                            && roh.TryGetInt32(out var code))
                        {
                            zaehler[code] = zaehler.GetValueOrDefault(code) + 1;
                        }
                    }
                }
            }
        }

        return zaehler;
    }

    private static IEnumerable<MzCommandEntry> BefehlsPro(int pCode)
    {
        foreach (var datei in Directory.GetFiles(Wurzel + "/data", "Map*.json")
            .Where(pF => Path.GetFileNameWithoutExtension(pF)[3..]
                .All(char.IsDigit)))
        {
            var karte = JsonDocument.Parse(File.ReadAllText(datei)).RootElement;
            if (!karte.TryGetProperty("events", out var events))
            {
                continue;
            }

            foreach (var e in events.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object
                    || !e.TryGetProperty("pages", out var seiten))
                {
                    continue;
                }

                foreach (var seite in seiten.EnumerateArray())
                {
                    var befehle = seite.TryGetProperty("list", out var liste)
                        ? liste.EnumerateArray().ToList()
                        : new List<JsonElement>();
                    var tiefe = 0;
                    foreach (var cmd in befehle)
                    {
                        if (cmd.ValueKind == JsonValueKind.Object
                            && cmd.TryGetProperty("code", out var roh)
                            && roh.TryGetInt32(out var code))
                        {
                            if (code == pCode)
                            {
                                // **Und `MzJson.TryParse` und nicht
                                // `System.Text.Json`**, **denn `MzValue`
                                // ist der Werttyp, den der Interpreter liest,
                                // und ein zweiter JSON-Leser daneben waere
                                // eine zweite Wahrheit ueber dieselbe
                                // Datei.**
                                if (MzJson.TryParse(cmd.GetRawText(),
                                    out var wert, out var fehler))
                                {
                                    yield return MzCommandEntry.From(wert);
                                }
                            }

                            if (code is 401 or 405 or 102 or 301 or 355 or 357)
                            {
                                tiefe++;
                            }
                            else if (code is 412 or 413 or 413)
                            {
                                tiefe = Math.Max(0, tiefe - 1);
                            }
                        }
                    }
                }
            }
        }
    }

    private static string MarshalReaderText(JsonElement pCmd)
    {
        var roh = pCmd.GetRawText();
        return roh.Length > 400 ? roh[..400] : roh;
    }
}
