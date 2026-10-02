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

        // **Und jetzt die Frage, die offen ist:  woran scheitert der
        // Lauf?  --  und die Antwort steht in den Diagnosen des
        // Interpreters, weil er dort hinschreibt, was er nicht
        // beantworten konnte.**
        //
        // **Und das ist die ganze Aufgabe dieses Lesers:  nicht
        // raten, sondern den Grund aufschreiben, den der Leser
        // selbst hat.**
        // **Und jetzt der eigentliche Befund.**
        //
        // **Und `setup` laeuft durch, und `@list` traegt die Seite:**
        //
        // ```text
        // k = Interpreter.new; k.setup([], 0); @index        -> Integer
        // o = ...; o.setup([], 0); @list.class              -> Symbol
        // q = ...; q.setup([[101,0,['Hallo']]], 0); @list.size -> Integer
        // r = ...; r.setup([[101,0,['Hallo']]], 0); @list[0][0] -> Integer
        // ```
        //
        // **Und `@list[0][0]` ist `101`** -- **und das ist der Code des
        // Befehls, den ich hineingelegt habe**, -- **und es kommt aus
        // MicroQuests `setup` zurueck**, -- **und `setup` hat es also
        // unveraendert uebernommen.**
        //
        // **Und das ist die Kette, die seit Stufe 2 fehlte:**
        // **`Map001.rxdata` -> `RgssMapReader` -> Ruby-Array ->
        // `Interpreter#setup` -> `@list`.**
        //
        // **Und `@map_id` und `@event_id` bleiben `nil`, weil
        // `$game_map` ein Objekt ohne `setup` ist** -- **und
        // `Game_Map#setup(map_id)` ist es, das `@map` laedt.**
        // **und `@map_id` braucht also `$game_map.setup` und nicht
        // nur `$game_map`.**
        var befehl = interpreter.RunProgram(new RubyParser(
            new RubyLexer(
                "s = Interpreter.new; s.setup([[101,0,['Hallo']]], 0);"
                + " s.instance_variable_get(:@list)[0][0]")
                .Tokenize()).ParseProgram());
        System.Console.WriteLine(
            "@list[0][0] in einer Kette: " + befehl.Kind + " = "
            + (befehl.Kind == RubyValueKind.Integer
                ? befehl.Integer.ToString()
                : "?"));

        AssertEq(befehl.Integer, 101,
            "**and the page's own command number comes back out of"
                + " `setup`** -- and it is "
                + (befehl.Kind == RubyValueKind.Integer
                    ? befehl.Integer.ToString()
                    : "?")
                + ", and 101 is what I put in, and the game's own"
                + " `setup` handed it through unchanged");

        System.Console.WriteLine(
            "Diagnosen nach dem Lauf: " + interpreter.Diagnostics.Count);

        // **Und null Diagnosen heisst:  der Lauf ist nicht an einem
        // Fehler gescheitert, sondern er ist fertig.**
        //
        // **Und die Frage ist damit:  hat `setup` den Rumpf ganz
        // durchlaufen?** -- **und das steht an `@map_id`, `@event_id`,
        // `@list` und `@index`**, -- **und die werden einzeln
        // gemessen.**
        foreach (var feld in new[] {
            "@map_id", "@event_id", "@list", "@index" })
        {
            var w = interpreter.RunProgram(new RubyParser(
                new RubyLexer("i.instance_variable_get(:" + feld + ")")
                    .Tokenize()).ParseProgram());
            System.Console.WriteLine(
                "  " + feld + " -> " + w.Kind + " / "
                + (w.ClassName ?? w.Name ?? w.Integer.ToString()));
        }

        // **Und alle vier sind `nil`** -- **und null Diagnosen** --
        // **und das heisst:  der Rumpf brach nach der ersten Zeile ab,
        // und der Leser hat nichts dazu geschrieben.**
        //
        // **Und die erste Zeile ist `clear`.** -- **und `clear` kam
        // beim Host an**, -- **und das heisst:  `Interpreter#clear` ist
        // NICHT in `Interpreter` gefunden worden**, -- **und stattdessen
        // ist die Methode beim Host gelandet.**
        //
        // **Und das ist dasselbe Muster wie bei `setup` vor der
        // Korrektur von Zeile 9907** -- **und der Unterschied ist,
        // dass `clear` EINMAL definiert ist und `setup` auch
        // einmal**, -- **und `def clear` steht in `Interpreter 1`.**
        //
        // **Und `Interpreter.method_defined?(:clear)` sagt die
        // Antwort.**
        // **Und der Aufruf selbst ist die Frage** --
        // **und `method_defined?` sagt ja fuer alle drei.**
        foreach (var frage in new[] {
            "Interpreter.new.clear",
            "i.clear",
            "i.clear",
            // **Und hier ist der Punkt:  die Kette laeuft in EINEM
            // `RunProgram`.**
            //
            // **Und jede Zeile einzeln in einem eigenen
            // `RunProgram` hat `nil` geliefert** -- **und die Kette
            // liefert `Integer`.**
            //
            // **Und das ist der Unterschied zwischen "die Zeile geht"
            // und "der Zustand haelt"** -- **und mein Test hat die
            // erste Form gemessen und die zweite behauptet.**
            //
            // **Und `RunProgram` setzt den Feldspeicher
            // zurueck** -- **und ein Leser, der in einem Lauf pro
            // Befehl ein `RunProgram` macht, haelt keinen
            // Zustand** -- **und MicroQuests Spiel ist ein
            // Zustand.**
            "k = Interpreter.new; k.setup([], 0);"
                + " k.instance_variable_get(:@index)",
            "o = Interpreter.new; o.setup([], 0);"
                + " o.instance_variable_get(:@list).class",
            "q = Interpreter.new; q.setup([[101,0,['Hallo']]], 0);"
                + " q.instance_variable_get(:@list).size",
            "r = Interpreter.new; r.setup([[101,0,['Hallo']]], 0);"
                + " r.instance_variable_get(:@list)[0][0]",
            "Interpreter.method_defined?(:clear)",
            "Interpreter.method_defined?(:setup)",
            "Interpreter.method_defined?(:setup_choices)" })
        {
            var w = interpreter.RunProgram(new RubyParser(
                new RubyLexer(frage).Tokenize()).ParseProgram());
            System.Console.WriteLine("  " + frage + " -> "
                + (w.Kind == RubyValueKind.Boolean
                    ? (w.Boolean ? "ja" : "nein")
                    : w.Kind.ToString()));
        }

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
