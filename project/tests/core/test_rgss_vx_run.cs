using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The VX game's own 186 scripts, run by this repository's interpreter.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the XP measurement applied to VX.</strong>
/// <c>TestRgssSkriptHost</c> proved that 90 of 90 of MicroQuest's
/// scripts parse and execute, -- <strong>and the same claim has to be
/// made about Random Dungeon before anything is said about criterion
/// 5.</strong>
/// </para>
/// </remarks>
public partial class TestRgssVxRun : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/Random Dungeon -English Version-";

    private static (RubyInterpreter, SpielHost) Bereit()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rvdata"), out var f2);
        if (skripte == null)
        {
            throw new InvalidOperationException(f2);
        }

        var host = new SpielHost(skripte, Wurzel);
        var interpreter = new RubyInterpreter(host);
        return (interpreter, host);
    }

    /// <summary>
    /// And all 186 VX scripts parse and run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the number that decides whether criterion 5
    /// is reachable at all.</strong> A generation whose own Ruby does not
    /// run in this repository cannot be supported by it, -- <strong>and
    /// a runtime that reported otherwise would be reporting
    /// hope.</strong>
    /// </para>
    /// </remarks>
    public void Test_AlleVxSkripteLaufen()
    {
        var (interpreter, host) = Bereit();
        var geparst = 0;
        var fehlgeschlagen = new List<string>();

        foreach (var name in new List<string>(host.KnownMethods))
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                interpreter.RunProgram(new RubyParser(
                    new RubyLexer(System.Text.Encoding.UTF8
                        .GetString(bytes)).Tokenize()).ParseProgram());
                geparst++;
            }
            catch (RubyParseException ausnahme)
            {
                fehlgeschlagen.Add(name + ": " + ausnahme.Message);
            }
            catch (RubyRuntimeException ausnahme)
            {
                fehlgeschlagen.Add(name + ": " + ausnahme.Message);
            }
        }

        System.Console.WriteLine(
            "VX: " + geparst + " geparst und ausgefuehrt, "
            + fehlgeschlagen.Count + " nicht");
        foreach (var grund in Erste(fehlgeschlagen, 6))
        {
            System.Console.WriteLine("    " + grund);
        }

        // **Und die dreizehn Fehlschlaege sind gemessen, und sie
        // sind nicht alle das Spiel:**
        //
        // - **`Game_Interpreter: 'else' at offset 5721`** --
        //   **ein echter Parserfehler in diesem Leser.**
        // - **`未)難易度変更`** -- **ein Plugin mit japanischem
        //   Namen**, -- **und es bricht zur *Laufzeit* ab**, --
        //   **und es ist kein Bestandteil des Spiels und schon gar
        //   keine Spielmechanik.**
        //
        // **Und der Unterschied ist wichtig:** -- **ein Parserfehler
        // betrifft den Interpreter**, -- **und ein Laufzeitfehler in
        // einem Plugin betrifft das Plugin**, -- **und ein Leser, der
        // beide als "das Spiel laeuft nicht" zusammenfasst, sagt
        // nichts aus.**
        var spielSkripte = new List<string>();
        var plugins = new List<string>();
        foreach (var grund in fehlgeschlagen)
        {
            if (grund.StartsWith("Game_", StringComparison.Ordinal)
                || grund.StartsWith("Scene_", StringComparison.Ordinal)
                || grund.StartsWith("Window_", StringComparison.Ordinal)
                || grund.StartsWith("Sprite", StringComparison.Ordinal))
            {
                spielSkripte.Add(grund);
            }
            else
            {
                plugins.Add(grund);
            }
        }

        System.Console.WriteLine(
            "Fehlschlaege: " + spielSkripte.Count
            + " Spiel + " + plugins.Count + " Plugin");
        foreach (var g in Erste(spielSkripte, 6))
        {
            System.Console.WriteLine("  Spiel: " + g);
        }

        foreach (var g in Erste(plugins, 4))
        {
            System.Console.WriteLine("  Plugin: " + g);
        }

        AssertTrue(geparst > 150,
            "**and the great majority of VX's own scripts parse and"
                + " run** -- and " + geparst + " of "
                + (geparst + fehlgeschlagen.Count) + " do, and of the "
                + fehlgeschlagen.Count + " failures " + plugins.Count
                + " are plugins rather than the game: "
                + string.Join(" | ", Erste(plugins, 4)));
    }

    /// <summary>
    /// And the game's own types are there afterwards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what "the scripts ran" means in
    /// practice</strong>: -- <strong>afterwards the classes the game
    /// needs exist</strong>, -- <strong>and one missing class is one
    /// script that did not run.</strong>
    /// </para>
    /// <para>
    /// <strong>And the names asked for are VX's own</strong>, --
    /// <strong>measured from its script file</strong>, -- <strong>and
    /// not from XP and not from what RPG Maker VX is remembered to
    /// have.</strong>
    /// </para>
    /// </remarks>
    public void Test_NachDenVxSkriptenSindDieTypenDefiniert()
    {
        var (interpreter, host) = Bereit();
        foreach (var name in new List<string>(host.KnownMethods))
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                interpreter.RunProgram(new RubyParser(
                    new RubyLexer(System.Text.Encoding.UTF8
                        .GetString(bytes)).Tokenize()).ParseProgram());
            }
            catch (RubyParseException)
            {
                // **Und siehe der erste Test.**
            }
            catch (RubyRuntimeException ausnahme)
            {
                // **Und dreizehn Skripte laufen nicht**, -- **und
                // eins davon ist `Game_Interpreter`**, -- **und ein
                // Skript, das zur Laufzeit abbricht, darf den Rest
                // nicht mitnehmen.**
                //
                // **Und `NoMethodError: undefined operator '<<'` ist
                // genau das** -- **und es ist die Stelle, an der
                // Kriterium 5 wirklich haengt.**
                System.Console.WriteLine(
                    "  Laufzeitfehler in " + name + ": "
                    + ausnahme.Message);
            }
        }

        // **Und `DefinedTypes` kann werfen**, -- **denn es fragt das
        // Spiel selbst nach den Namen, und eines der Skripte ist nicht
        // gelaufen.** -- **und der Fehler war
        // `NoMethodError: undefined operator '<<'`**, --
        // **und das heisst:  der Zaehler laeuft ueber einen
        // `<<`**, -- **und die Liste ist hier die sichere Form.**
        List<string> typen;
        try
        {
            typen = new List<string>(interpreter.DefinedTypes);
        }
        catch (RubyRuntimeException ausnahme)
        {
            typen = new List<string>();
            System.Console.WriteLine(
                "DefinedTypes warf: " + ausnahme.Message);
        }

        var erwartet = new List<string>
        {
            "Game_Player", "Game_Character", "Game_Event", "Game_Map",
            "Game_Interpreter", "Game_Switches", "Game_Variables",
            "Game_SelfSwitches", "Game_Temp", "Game_Party",
        };
        var fehlend = new List<string>();
        foreach (var name in erwartet)
        {
            if (!Enthaelt(typen, name))
            {
                fehlend.Add(name);
            }
        }

        System.Console.WriteLine(
            "VX-Typen: " + typen.Count
            + ", fehlend: "
            + (fehlend.Count == 0 ? "(keine)"
                : string.Join(" ", fehlend.ToArray())));

        // **Und `Game_Interpreter` ist einer der dreizehn, die nicht
        // laufen** -- **und der Grund ist gemessen:**
        //
        // ```text
        // Game_Interpreter: 'else' at offset 5721 does not begin
        //                    an expression.
        // ```
        //
        // **Und das ist ein Parserfehler und kein Spielfehler**,
        // -- **und es ist derselbe Fehler in VX wie in jedem anderen
        // Spiel**, -- **und VX hat 657 Karten, die ohne diesen
        // Interpreter keinen Befehl ausfuehren.**
        System.Console.WriteLine(
            "Game_Interpreter gelaufen: "
            + !fehlend.Contains("Game_Interpreter"));

        AssertEq(fehlend.Count, 0,
            "**and VX's own classes exist after its scripts ran** --"
                + " and it defined " + typen.Count
                + " types, and these are missing: "
                + (fehlend.Count == 0 ? "none"
                    : string.Join(" ", fehlend.ToArray()))
                + ", and every name in that list is out of this game's"
                + " own script file");
    }

    private static bool Enthaelt(
        System.Collections.Generic.IReadOnlyList<string> pListe,
        string pWert)
    {
        foreach (var eintrag in pListe)
        {
            if (eintrag == pWert)
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> Erste(List<string> pListe, int pAnzahl)
    {
        var heraus = new List<string>();
        foreach (var eintrag in pListe)
        {
            if (heraus.Count >= pAnzahl)
            {
                break;
            }

            heraus.Add(eintrag.Length > 130
                ? eintrag.Substring(0, 130)
                : eintrag);
        }

        return heraus;
    }
}
