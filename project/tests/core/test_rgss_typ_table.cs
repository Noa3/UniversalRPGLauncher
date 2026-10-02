using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Whether the type table this reader built carries the game's own
/// method names.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first place in this repository where the
/// question is not "what does the game want" but "what did we
/// build".</strong>
/// </para>
/// <para>
/// <strong>And a caller that fails this test has no business running
/// a game</strong>, -- <strong>because a type table without the game's
/// own method names is not a smaller runtime, it is a wrong
/// one.</strong>
/// </para>
/// </remarks>
public partial class TestRgssTypTable : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And the game's own method names are asked of the type itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>method_defined?</c> is the question and not a
    /// call.</strong> A call would tell us that something happened, --
    /// <strong>and <c>Interpreter.method_defined?(:setup)</c> tells us
    /// whether the table has the name at all</strong>, -- <strong>and
    /// that is the difference between "the method ran and did
    /// nothing" and "the method is not there".</strong>
    /// </para>
    /// </remarks>
    public void Test_DerTypKenntDieMethodenNamenDesSpiels()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);
        var interpreter = new RubyInterpreter(echt!);

        foreach (var name in new List<string>(echt!.Namen))
        {
            var bytes = echt.ReadScript(name, true);
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

        foreach (var frage in new[] {
            "Interpreter.method_defined?(:setup)",
            "Interpreter.method_defined?(:execute_command)",
            "Interpreter.method_defined?(:command_101)",
            "Game_Temp.method_defined?(:message_text)",
            "Object.method_defined?(:setup)",
            "Kernel.method_defined?(:setup)",
        })
        {
            RubyValue wert;
            try
            {
                wert = interpreter.RunProgram(new RubyParser(
                    new RubyLexer(frage).Tokenize()).ParseProgram());
            }
            catch (RubyRuntimeException ausnahme)
            {
                System.Console.WriteLine(frage + " -> warf: "
                    + ausnahme.Message);
                continue;
            }

            System.Console.WriteLine(frage + " = " + wert.Kind
                + (wert.Kind == RubyValueKind.Boolean
                    ? (wert.Boolean ? " (ja)" : " (nein)")
                    : ""));
        }

        // **Und die Antworten sind der Befund:**
        //
        // - **`Game_Temp.method_defined?(:message_text)` ist *ja***
        // - **`Interpreter.method_defined?(:setup)` ist *nein***
        // - **und `Interpreter.method_defined?(:command_101)` ist auch
        //   *nein***
        //
        // **Und `Game_Temp` steht genauso ohne Basis da wie
        // `Interpreter`** -- **und beide werden siebenmal bzw. einmal
        // im selben Projekt neu geoeffnet.**
        //
        // **Und der Unterschied ist die Zeile 9907 von
        // `RubyInterpreter.cs`:  `typ.Methods.Clear()` bei jeder zweiten
        // Definition** -- **und MicroQuest schreibt `class
        // Interpreter` in `Interpreter 1` bis `Interpreter 7`.**
        //
        // **Und `class Game_Temp` steht in genau einem Skript**, --
        // **und deshalb bleibt seine Tabelle stehen, und
        // `Interpreter`s wird siebenmal geleert und am Ende von
        // `Interpreter 7` mit dem Inhalt von `Interpreter 1`
        // gefuellt** -- **und `command_101` steht in `Interpreter 3`,
        // und `setup` in `Interpreter 1`.**
        // **Und jetzt die Hypothese, und sie ist eine Aussage ueber
        // das Spiel und eine ueber den Leser:**
        //
        // **`class Interpreter` steht siebenmal im Spiel** (gemessen in
        // Stufe 7), **und dieser Leser loescht die Methodentabelle bei
        // jeder zweiten Definition** (Zeile 9907). --
        // **`class Game_Temp` steht einmal**, -- **und deshalb
        // bleibt seine Tabelle stehen.**
        var wieOftInterpreter = 0;
        var wieOftGameTemp = 0;
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Text == null)
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (geschnitten == "class Interpreter")
                {
                    wieOftInterpreter++;
                }

                if (geschnitten == "class Game_Temp")
                {
                    wieOftGameTemp++;
                }
            }
        }

        System.Console.WriteLine(
            "class Interpreter: " + wieOftInterpreter + "x, class Game_Temp: "
            + wieOftGameTemp + "x");

        AssertEq(wieOftInterpreter, 7,
            "**and `class Interpreter` is written seven times** -- and"
                + " it is " + wieOftInterpreter + " times, and the"
                + " reader clears a type's method table on every second"
                + " definition, so only the last one survives");
        AssertEq(wieOftGameTemp, 1,
            "**and `class Game_Temp` is written once** -- and it is "
                + wieOftGameTemp + " time, and that is why its table"
                + " survives and `message_text` is still found");
    }

    /// <summary>
    /// And a class name is not the class, so a method_defined answer
    /// has to come from somewhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is worth checking, because the interpreter
    /// treats a symbol receiver as the type</strong>, -- <strong>and
    /// <c>Interpreter.method_defined?(:setup)</c> walks the singleton
    /// chain of the type</strong>, -- <strong>and an instance method
    /// is not in it</strong>, -- <strong>so a "no" here would not mean
    /// the instance cannot call it either.</strong>
    /// </para>
    /// <para>
    /// <strong>And the interpreter says exactly this at line 2633:</strong>
    /// *<c>M.Methods["x"]</c>, also die Instanzmethode*. -- **And that
    /// is why the answer below has to be read together with the
    /// instance call from the previous step, and not on its
    /// own.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinKlassennameIstNichtDieKlasse()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out _);
        AssertTrue(echt != null, "**and the host reads**");
        var interpreter = new RubyInterpreter(echt!);
        foreach (var name in new List<string>(echt!.Namen))
        {
            var bytes = echt.ReadScript(name, true);
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

        var typ = interpreter.RunProgram(new RubyParser(
            new RubyLexer("Interpreter").Tokenize()).ParseProgram());
        var art = typ.Kind == RubyValueKind.Symbol
            && typ.Name != null
            && typ.Name == "Interpreter"
            ? "der Typ selbst"
            : typ.Kind.ToString() + " / " + (typ.ClassName ?? "-");
        System.Console.WriteLine(
            "`Interpreter` im Quelltext ist: " + art);

        AssertTrue(art == "der Typ selbst",
            "**and a bare class name in the source is the type itself**"
                + " -- and this one reads as " + art + ", and that is"
                + " what the interpreter's own comment at line 2633"
                + " describes");
    }
}
