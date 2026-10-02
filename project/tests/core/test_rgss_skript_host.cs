using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The Ruby interpreter against a real XP game's own 90 scripts.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this repository already has a Ruby lexer, parser and
/// interpreter</strong>, -- <c>RubyInterpreter</c> is 12 736 lines with
/// 45 test files behind it, -- <strong>and XP and VX need exactly
/// that</strong>, -- <strong>because their event commands are methods
/// on a Ruby class.</strong>
/// </para>
/// <para>
/// <strong>And what was missing was not the language but the
/// door.</strong> A Ruby Maker project has no script folder: its Ruby
/// sits inside <c>Data/Scripts.rxdata</c> as zlib bodies, -- <strong>and
/// <c>IRubyHost.ReadScript</c> is the only thing between the
/// interpreter and those 90 scripts.</strong>
/// </para>
/// <para>
/// <strong>And this test answers the only question that matters:</strong>
/// how much of a real game's own Ruby does this repository actually
/// parse and run. <strong>It does not assert a target. It measures.</strong>
/// </para>
/// </remarks>
public partial class TestRgssSkriptHost : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And the host holds the game's 90 scripts.
    /// </summary>
    /// <remarks>
    /// <strong>And 90 is measured</strong> from the file, -- <strong>and
    /// not the number of scripts RPG Maker XP shipped with.</strong>
    /// </remarks>
    public void Test_DerHostHaeltDieNeunzigSkripteDesSpiels()
    {
        var host = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(host != null,
            "**and the host reads the script file** -- and it said: "
                + fehler);
        AssertEq(host!.Anzahl, 90,
            "**and it holds all 90 scripts** -- and it holds "
                + host.Anzahl);
        AssertTrue(host.ReadScript("Game_Player", true) != null,
            "**and it can hand out Game_Player by name**");
        AssertTrue(host.ReadScript("Interpreter 4", true) != null,
            "**and it can hand out `Interpreter 4` by name** -- and the"
                + " name has a space in it, as the file spells it");
    }

    /// <summary>
    /// And a name the file does not carry is refused, not invented.
    /// </summary>
    /// <remarks>
    /// <strong>And an empty string instead of a refusal would run and
    /// define nothing</strong>, -- <strong>and a game whose
    /// <c>require</c> silently did nothing would fail far from the
    /// line that was missing.</strong>
    /// </remarks>
    public void Test_EinNameDenDasSpielNichtFuehrtIstEineAblehnung()
    {
        var host = RgssSkriptHost.Lese(XpSkripte, out _);
        AssertTrue(host != null, "**and the host reads**");
        AssertTrue(host!.ReadScript("Game_PlayerX", true) == null,
            "**and a name one letter off is refused**");
        AssertTrue(host.ReadScript("game_player", true) == null,
            "**and the same name in another case is refused too** -- and"
                + " Ruby constants are case sensitive and so is this");

        // **Und `.rb` wird nicht angehaengt**, -- **denn die Namen
        // sind Editornamen und keine Dateinamen.**
        AssertTrue(host.ReadScript("Game_Player.rb", true) == null,
            "**and no extension is appended** -- and a Ruby Maker"
                + " project has no script files at all");
    }

    /// <summary>
    /// And the interpreter parses the game's own Ruby.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement, and it is deliberately not
    /// an assertion with a target.</strong> A test that says "95 %"
    /// would be a number I invented, -- <strong>and this one prints
    /// what came out and fails only if nothing at all parses.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerInterpreterLiestDasRubyDesSpiels()
    {
        var host = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(host != null,
            "**and the host reads** -- and it said: " + fehler);

        var interpreter = new RubyInterpreter(host!);
        var namen = new List<string>(host!.Namen);
        var geparst = 0;
        var fehlgeschlagen = new List<string>();
        foreach (var name in namen)
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                var knoten = new RubyParser(
                    new RubyLexer(text).Tokenize()).ParseProgram();
                interpreter.RunProgram(knoten);
                geparst++;
            }
            catch (RubyParseException ausnahme)
            {
                fehlgeschlagen.Add(name + ": " + ausnahme.Message);
            }
        }

        System.Console.WriteLine(
            "XP: " + namen.Count + " Skripte, " + geparst
                + " geparst und ausgefuehrt, " + fehlgeschlagen.Count
                + " nicht");
        foreach (var grund in FehlgeschlagenKurz(fehlgeschlagen))
        {
            System.Console.WriteLine("    " + grund);
        }

        AssertTrue(geparst > 0,
            "**and at least one script of a real game parses and runs**"
                + " -- and " + geparst + " do");
        AssertTrue(geparst + fehlgeschlagen.Count == namen.Count,
            "**and every script is either parsed or named as a failure**"
                + " -- and " + geparst + " and "
                + fehlgeschlagen.Count + " of " + namen.Count);
    }

    /// <summary>
    /// And the game's own classes come out defined.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what "running a game's scripts" means in
    /// practice</strong>: -- <strong>after the scripts have run, the
    /// classes they define exist</strong>, -- <strong>and a class that
    /// is missing is a script that did not run.</strong>
    /// </para>
    /// </remarks>
    public void Test_NachDenSkriptenIstDerSpieltypDefiniert()
    {
        var host = RgssSkriptHost.Lese(XpSkripte, out _);
        var interpreter = new RubyInterpreter(host!);
        foreach (var name in new List<string>(host!.Namen))
        {
            var bytes = host.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                var knoten = new RubyParser(
                    new RubyLexer(System.Text.Encoding.UTF8
                        .GetString(bytes)).Tokenize())
                    .ParseProgram();
                interpreter.RunProgram(knoten);
            }
            catch (RubyParseException)
            {
                // **Und ein Skript, das nicht laeuft, wird hier
                // uebersprungen** -- **und das sagt der letzte Test.**
            }
        }

        // **Und `Game_Character` ist der Typ, auf den jede XP-Karte
        // ihre Ereignisse aufbaut** -- **und er steht in
        // `Game_Character 1` bis `Game_Character 3`.**
        System.Console.WriteLine(
            "Konstanten nach dem Lauf: "
            + string.Join(" ",
                Bekannte(interpreter, "Game_", 8)));

        AssertTrue(Bekannte(interpreter, "Game_", 8).Count > 3,
            "**and the game's own classes exist afterwards** -- and at"
                + " least a few of them do");
    }

    private static List<string> FehlgeschlagenKurz(
        List<string> pListe)
    {
        var heraus = new List<string>();
        foreach (var eintrag in pListe)
        {
            if (heraus.Count >= 6)
            {
                break;
            }

            var kuerzel = eintrag.Length > 110
                ? eintrag.Substring(0, 110)
                : eintrag;
            heraus.Add(kuerzel);
        }

        return heraus;
    }

    private static List<string> Bekannte(
        RubyInterpreter pInterpreter, string pPraefix, int pHoechstens)
    {
        var heraus = new List<string>();
        foreach (var name in pInterpreter.DefinedTypes)
        {
            if (!name.StartsWith(pPraefix, StringComparison.Ordinal))
            {
                continue;
            }

            heraus.Add(name);
            if (heraus.Count >= pHoechstens)
            {
                break;
            }
        }

        heraus.Sort(StringComparer.Ordinal);
        return heraus;
    }
}
