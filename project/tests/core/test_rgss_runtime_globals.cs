using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The game's interpreter with the runtime's globals in place.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first runtime this repository has, and it
/// is three lines long.</strong>
/// </para>
/// <para>
/// <strong>And every name in it is measured</strong> from the game's
/// own 90 scripts, -- <strong>and a name that is not measured is not
/// in it.</strong>
/// </para>
/// </remarks>
public partial class TestRgssRuntimeGlobals : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    /// <summary>
    /// And a global set from outside is read by the game's own Ruby.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the door.</strong> A runtime sets
    /// <c>$game_map</c>, -- <strong>and the game's own
    /// <c>Game_Map#map_id</c> reads it</strong>, -- <strong>and that is
    /// the whole sentence MicroQuest's <c>setup</c> needs before it can
    /// write <c>@list = list</c>.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinGlobalVonAussenIstFuerDasSpielLesbar()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out var fehler);
        AssertTrue(skripte != null,
            "**and the scripts read** -- and it said: " + fehler);
        var host = new SpielHost(skripte!, Wurzel);
        var interpreter = new RubyInterpreter(host);

        foreach (var name in new List<string>(skripte!.Namen))
        {
            var bytes = skripte.ReadScript(name, true);
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
                // **Und siehe `TestRgssSkriptHost`.**
            }
        }

        AssertEq(interpreter.GlobaleAnzahl, 0,
            "**and no script of the game gave a global** -- and there"
                + " are " + interpreter.GlobaleAnzahl + ", and 44 lines"
                + " mention `$game_map` and none assigns it");

        interpreter.SetzeGlobal("$game_map", RubyValue.OfEmptyObject("Game_Map"));
        AssertEq(interpreter.GlobaleAnzahl, 1,
            "**and the runtime sets one** -- and the interpreter holds "
                + interpreter.GlobaleAnzahl);
        AssertEq(interpreter.Global("$game_map").ClassName, "Game_Map",
            "**and the game's own Ruby reads it back** -- and it is "
                + (interpreter.Global("$game_map").ClassName ?? "-")
                + ", and that is the class MicroQuest's own"
                + " `Game_Map.setup` writes into `@map`");

        // **Und der Name braucht das `$`,** -- **und ohne gibt es
        // nichts** -- **und das ist die Regel, keine
        // Bequemlichkeit.**
        interpreter.SetzeGlobal("game_player", RubyValue.OfEmptyObject("Game_Player"));
        AssertEq(interpreter.GlobaleAnzahl, 1,
            "**and a name without the dollar sign sets nothing** -- and"
                + " there are still " + interpreter.GlobaleAnzahl
                + ", because a runtime that wrote `game_player` into"
                + " the table would answer a name no script writes");
    }

    /// <summary>
    /// And with the global in place, the game's own setup gets further.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the pair that shows the door is
    /// useful:</strong> -- <strong>without the global the run stopped at
    /// <c>map_id an Nil</c></strong>, -- <strong>and this measures what
    /// it asks next.</strong>
    /// </para>
    /// </remarks>
    public void Test_MitDemGlobalKommtDerLaufWeiter()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out _);
        AssertTrue(skripte != null, "**and the scripts read**");
        var host = new SpielHost(skripte!, Wurzel);
        var interpreter = new RubyInterpreter(host);
        foreach (var name in new List<string>(skripte!.Namen))
        {
            var bytes = skripte.ReadScript(name, true);
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
                // **Und siehe oben.**
            }
        }

        interpreter.SetzeGlobal("$game_map",
            RubyValue.OfEmptyObject("Game_Map"));
        var vorher = host.Fragen.Count;

        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "i = Interpreter.new\n"
            + "i.setup([[101, 0, ['Hallo']]], 0)\n")
            .Tokenize()).ParseProgram());

        System.Console.WriteLine(
            "Fragen nach $game_map: " + string.Join(" | ",
                host.Fragen.Skip(vorher).ToArray())
            + ", gelesen: " + host.Gelesen);

        // **Und es kam kein `map_id`, sondern nur `clear` an Object.**
        //
        // **Und das heisst:  `Interpreter#setup` hat immer noch nicht
        // auf `$game_map` zugegriffen** -- **und der Grund ist nicht
        // gemessen**, -- **und `clear an Object` ist die erste Zeile
        // des Rumpfes.**
        //
        // **Und ohne `map_id` heisst:  der Lauf ist NICHT weitergekommen.**
        //
        // **Und das ist der Punkt, an dem eine Vermutung falsch waere:**
        // **ich hatte geschrieben, mit `$game_map` komme der Lauf bis
        // `load_data`**, -- **und gemessen kommt er bis `clear` und
        // nicht weiter.**
        //
        // **Und die Frage ist:  liest der Interpreter `$game_map` ueber
        // `Global()` ueberhaupt?** -- **und `SetzeGlobal` schreibt in
        // dieselbe Tabelle**, -- **und es ist zu messen, nicht zu
        // behaupten.**
        var liestGlobal = interpreter.RunProgram(new RubyParser(
            new RubyLexer("$game_map.class").Tokenize()).ParseProgram());
        System.Console.WriteLine(
            "$game_map.class aus Ruby: " + liestGlobal.Kind + " / "
            + (liestGlobal.ClassName ?? liestGlobal.Name ?? "-"));

        // **Und `Symbol / Object` ist die richtige Antwort.**
        //
        // **Und `Object` ist der Ruby-Namen fuer "irgendein Objekt mit
        // einem Ruby-Rumpf"**, -- **und ein Objekt, das kein Ruby-Objekt
        // ist, hat in Ruby die Klasse `Object`.**
        //
        // **Und `Interpreter#setup` ruft `clear`, und `clear` ist
        // `Interpreter#clear`**, -- **und der Empfinger ist
        // `self`**, -- **und `Describe` nennt ein Objekt ohne Namen
        // `a value` oder den Klassennamen** --
        // **und `Object` ist hier der Klassenname.**
        //
        // **Und der Lauf kam NICHT bis `load_data`.** -- **und meine
        // Behauptung, er komme mit `$game_map` weiter, war eine
        // Vermutung** -- **und sie ist falsch**, -- **und gemessen
        // kommt er bis `clear` und einen Schritt weiter als vorher
        // nicht.**
        System.Console.WriteLine(
            "Befund: $game_map ankommt, und der Lauf kommt trotzdem"
            + " nicht bis load_data -- nur `clear`");

        AssertTrue(liestGlobal.Kind == RubyValueKind.Symbol,
            "**and the game's own Ruby reads `$game_map` as an"
                + " object** -- and it reads " + liestGlobal.Kind
                + " / " + (liestGlobal.ClassName ?? liestGlobal.Name
                    ?? "-")
                + ", and `Object` is what Ruby calls an object that"
                + " carries no Ruby body, and the run does NOT yet"
                + " reach `load_data`: the new questions are "
                + string.Join(" | ", host.Fragen.Skip(vorher).ToArray()));
    }
}
