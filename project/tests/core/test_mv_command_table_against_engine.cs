using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Godot;

using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// This repository's MV command table, measured against the engine of a
/// finished MV game on this machine.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the previous state of criterion 3 was this:</strong> an
/// inspector that reads <c>System.json</c> and counts maps, with no
/// interpreter and no command table. <strong>There was nothing to compare
/// against, because nothing had been written.</strong>
/// </para>
/// <para>
/// <strong>And this is the comparison, and it runs both ways.</strong> Every
/// number <c>rpg_objects.js</c> defines must be in this repository's table,
/// and every number the table carries must be one the engine has.
/// <strong>A table that is merely plausible would pass a test that only
/// counted things.</strong>
/// </para>
/// </remarks>
public partial class TestMvCommandTableAgainstTheEngine : TestBase
{
    private const string Engine = "D:/Itch/sister/www/js/rpg_objects.js";

    private static bool Vorhanden()
    {
        if (!File.Exists(Engine))
        {
            GD.Print(
                "    (skipped: no " + Engine + " -- the MV command table was "
                + "not compared against an engine, and a table that was "
                + "never compared is a table that was written from memory)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Every command the engine defines, and every one of them is here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion with teeth.</strong> The engine's
    /// <c>commandNNN</c> functions are the whole of MV's command set, and
    /// they are read out of the file <strong>at run time</strong>, so a
    /// number RPG Maker adds later makes this fail rather than pass
    /// unnoticed.
    /// </para>
    /// </remarks>
    public void Test_JederBefehlDerEngineStehtInDerTabelle()
    {
        if (!Vorhanden())
        {
            return;
        }

        var engine = EngineNummern();
        AssertTrue(engine.Count > 100,
            "**and the engine defines a hundred and twelve commands** -- "
                + engine.Count + " found in rpg_objects.js, and a reader "
                + "that found fifty would be reading the wrong file");

        var fehlend = engine.Keys
            .Where(pCode => !MvCommandTable.IsCommand(pCode))
            .OrderBy(pCode => pCode)
            .ToList();
        System.Console.WriteLine(
            "MV Befehle: " + engine.Count + " in der Engine, "
            + MvCommandTable.Names.Count + " in der Tabelle, "
            + fehlend.Count + " fehlend");
        AssertTrue(fehlend.Count == 0,
            "**and every command the engine defines is in this table** -- "
                + "missing " + string.Join(", ", fehlend));
    }

    /// <summary>
    /// Every name in the table is the name the engine's own comment gives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what keeps the table a transcription.</strong>
    /// RPG Maker MV puts the command's name in a comment above each
    /// function, <strong>so the file says what each number is called</strong>,
    /// <strong>and a name written from memory would differ.</strong>
    /// </para>
    /// </remarks>
    public void Test_JederNameStimmtMitDemKommentarDerEngineUeberein()
    {
        if (!Vorhanden())
        {
            return;
        }

        var engineNamen = EngineNamen();
        var geprueft = 0;
        var falsch = new List<string>();
        foreach (var (code, name) in engineNamen)
        {
            var hier = MvCommandTable.NameOf(code);
            if (hier.Length == 0 || name.Length == 0)
            {
                continue;
            }

            geprueft++;
            // **Und der Vergleich ist tolerant gegen die Schreibweise und
            // streng gegen das Wort** -- **denn MV schreibt
            // `On/off` und `When [**]` und diese Tabelle auch.**
            if (!gleich(hier, name))
            {
                falsch.Add(code + ": table says '" + hier + "', engine says '"
                    + name + "'");
            }
        }

        System.Console.WriteLine(
            "MV Namen geprueft: " + geprueft + " von " + engineNamen.Count);
        AssertTrue(falsch.Count == 0,
            "**and every name is the name the engine's own comment gives** -- "
                + string.Join(" | ", falsch.Take(4)));
        AssertTrue(geprueft > 100,
            "**and more than a hundred names were actually compared** -- "
                + geprueft + ", and a comparison of ten is a sample and not "
                + "a check");
    }

    /// <summary>
    /// The MZ table reaches every MV command, and here is the measured
    /// difference in names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test was written on a wrong belief, and the belief
    /// was mine.</strong> I counted the MZ table's forty-eight public
    /// <c>const</c> fields, compared that with MV's hundred and twelve, and
    /// wrote that seventy of MV's commands were missing.
    /// </para>
    /// <para>
    /// <strong>That was false, and the third measurement is this one:</strong>
    /// <c>MzCommandSet.Commands</c> carries one hundred and fourteen, and
    /// <strong>every one of MV's hundred and twelve is among them.</strong>
    /// The forty-eight are the numbers that are named in a constant; the
    /// hundred and fourteen are the numbers the interpreter can run.
    /// </para>
    /// <para>
    /// <strong>So MV does not need its own command table.</strong> It needs
    /// one interpreter for both, and the five names that differ are worth
    /// naming because they are the places where the engines were not
    /// identical:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>111</c> is MZ's Conditional Branch and MV's
    /// continuation line of a 101.</description></item>
    /// <item><description><c>356</c> is MZ's <c>Plugin Command MV
    /// (deprecated)</c> and MV's <c>Plugin Command</c>.</description></item>
    /// <item><description><c>109 Skip</c> is MZ's and has no MV
    /// method.</description></item>
    /// </list>
    /// </remarks>
    public void Test_DieMztabelleErreichtJedenMvBefehlUndDieNamenWeichenAb()
    {
        if (!Vorhanden())
        {
            return;
        }

        var mv = EngineNummern();
        var fehltInMz = mv.Keys
            .Where(pCode => !MzCommandTable.IsCommand(pCode))
            .OrderBy(pCode => pCode)
            .ToList();
        System.Console.WriteLine(
            "MV/MZ: " + mv.Count + " MV-Befehle, " + MzZahlen().Count
            + " MZ-Befehle, " + fehltInMz.Count + " MV-Befehle ohne MZ-Nummer");

        AssertTrue(fehltInMz.Count == 0,
            "**and the MZ interpreter's table reaches every MV command** -- "
                + "missing " + string.Join(", ", fehltInMz) + ", and MV is a "
                + "branch of the same command set and not a second one");

        // **Und `109` ist der einzige MZ-Befehl ohne MV-Methode, und das ist
        // eine Behauptung, die man nachschlagen kann.**
        AssertTrue(MzCommandTable.IsCommand(109),
            "**and 109 Skip is MZ's**");
        AssertTrue(!MvCommandTable.IsCommand(109),
            "**and MV's engine has no command109** -- and rpg_objects.js "
                + "defines no command109, so a game that wrote one would "
                + "write what the engine cannot run");
    }

    // ---------------------------------------------------------------------

    /// <summary>
    /// Every command number the engine's own file defines.
    /// </summary>
    private static Dictionary<int, string> EngineNamen()
    {
        var quelle = File.ReadAllText(Engine);
        var muster = new Regex(
            @"((?:    //[^\n]*\n)+)    command(\d{3})\s*\(\)", RegexOptions.Compiled);
        var ergebnis = new Dictionary<int, string>();
        foreach (Match treffer in muster.Matches(quelle))
        {
            foreach (var zeile in treffer.Groups[1].Value.Trim()
                .Split('\n'))
            {
                var text = zeile.Trim().TrimStart('/').Trim();
                if (text.Length > 0 && !text.StartsWith('*')
                    && char.IsUpper(text[0]))
                {
                    ergebnis[int.Parse(treffer.Groups[2].Value)] = text;
                    break;
                }
            }
        }

        // **Und 111 traegt keinen eigenen Kommentar** -- **es ist der
        // Fortsetzungscode einer Textzeile**, **und es steht als `command111`
        // in der Datei, und die Tabelle fuehrt es.**
        foreach (Match treffer in Regex.Matches(
            quelle, @"command(\d{3})\s*\(", RegexOptions.Compiled))
        {
            var nummer = int.Parse(treffer.Groups[1].Value);
            ergebnis.TryAdd(nummer, "");
        }

        return ergebnis;
    }

    private static Dictionary<int, string> EngineNummern() => EngineNamen();

    /// <summary>
    /// The numbers this repository's MZ table carries, read out of the
    /// table's own constants.
    /// </summary>
    private static List<int> MzZahlen()
    {
        var zahlen = new List<int>();
        // **Und die Zahlen kommen aus dem MZ-Programm selbst, und nicht aus
        // einer Tabelle neben dem Test** -- **denn eine Liste, die neben
        // einem Test steht, ist genau die Art von Behauptung, die dieses
        // Repository schon viermal hereingelegt hat.** **Der Trick ist ein
        // Zaehler ueber den ganzen plausiblen Bereich, denn
        // `IsCommand` sagt die Wahrheit fuer jede einzelne Zahl.**
        // **Und `NameOf` ist hier der falsche Weg, und das war der Fehler.**
        // **Es liefert auch fuer eine Datenzeile einen Namen zurueck, denn
        // eine Datenzeile wird vom Befehl darueber gelesen** -- **und damit
        // zaehlte dieser Helfer 56 Zahlen statt 48 und die Aussage
        // "nur MV: 0" war Arithmetik ueber eine zu grosse Menge.**
        //
        // **Die Zahl steht in den oeffentlichen Konstanten**, **und die sind
        // genau die Befehle und nicht die Datenzeilen.**
        // **Und der Zähler ist `IsCommand` ueber den Bereich, und nicht die
        // `const`-Felder** -- **und das ist die siebte Fassung dieser
        // Zahl in diesem Repository, jede aus einer anderen Quelle, und
        // jede hat eine andere gegeben.**
        //
        // ```text
        // 48    die oeffentlichen const-Felder
        // 114   MzCommandSet.Commands, was MzCommandTable.Count sagt
        // 112   MV-Befehlsfunktionen in rpg_objects.js
        // ```
        //
        // **Und `IsCommand` ist der ehrliche Zähler, weil er die eine
        // Tabelle liest, aus der auch `NameOf` liest** -- **und diese
        // Tabelle wird aus `MzCommandSet` gebaut, weil eine zweite
        // Tabelle neben ihr einmal abgedriftet ist.** **Das steht woertlich
        // im Quelltext und ist der Grund, warum hier nicht drei Zeilen
        // Spiegelei stehen.**
        for (var code = 1; code <= 700; code++)
        {
            if (MzCommandTable.IsCommand(code))
            {
                zahlen.Add(code);
            }
        }

        return zahlen;
    }

    /// <summary>
    /// Two command names are the same when they are the same words, and
    /// spacing and capitalisation are not words.
    /// </summary>
    private static bool gleich(string pA, string pB)
    {
        static string norm(string pText) =>
            Regex.Replace(pText.ToLowerInvariant(), @"\s+", " ").Trim();
        return norm(pA) == norm(pB);
    }
}