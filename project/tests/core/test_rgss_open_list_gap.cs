using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What a game's interpreter holds in <c>@list</c> after
/// <c>setup</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the follow-up to a measured fact:</strong>
/// XP's <c>setup</c> calls <c>clear</c> and the receiver arrived as
/// <c>Object</c>, -- <strong>and Ruby 1.8.6 defines <c>clear</c> only
/// on <c>Hash</c> and on <c>Array</c></strong>, -- <strong>and
/// <c>object.c</c> does not have it at all.</strong>
/// </para>
/// <para>
/// <strong>And so the receiver this reader built for <c>@list</c> is
/// not the list the game wrote</strong>, -- <strong>and that is the
/// second fault in the same direction as the cleared type
/// table.</strong>
/// </para>
/// </remarks>
public partial class TestRgssOpenListGap : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    private static List<string> Erste(List<string> pListe, int pAnzahl)
    {
        var heraus = new List<string>();
        foreach (var eintrag in pListe)
        {
            if (heraus.Count >= pAnzahl)
            {
                break;
            }

            heraus.Add(eintrag);
        }

        return heraus;
    }

    /// <summary>
    /// And the game's own body of `setup`, line by line.
    /// </summary>
    /// <returns>The lines, and an empty list when there are none.</returns>
    /// <remarks>
    /// <strong>And this is the shape the game's own code has</strong>,
    /// and nothing here is written from memory.
    /// </remarks>
    private static List<string> SetupZeilen()
    {
        var heraus = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Name != "Interpreter 1" || leib.Text == null)
            {
                continue;
            }

            var zeilen = leib.Text.Split('\n');
            for (var i = 0; i < zeilen.Length; i++)
            {
                if (!zeilen[i].Trim().StartsWith("def setup(",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                for (var k = i; k < zeilen.Length && k < i + 16; k++)
                {
                    var geschnitten = zeilen[k].Trim();
                    if (geschnitten.Length == 0
                        || geschnitten.StartsWith("#",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    heraus.Add(geschnitten);
                }
            }
        }

        return heraus;
    }

    /// <summary>
    /// And the list a page was given is read back out of the
    /// interpreter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the game writes this in <c>setup</c> itself</strong>
    /// -- <c>@list = list</c> -- <strong>and <c>list</c> is what the
    /// caller passed</strong>, -- <strong>and the caller passes a
    /// literal array</strong>, -- <strong>so <c>@list</c> must be an
    /// Array and nothing else.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieListeEinerSeiteIstNochNichtDasArray()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);
        var host = new ProtokollHost(echt!);
        var interpreter = new RubyInterpreter(host);

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

        // **Und MicroQuests `setup` schreibt `@list = list`**,
        // -- **und der Aufrufer gibt ein Array-Literal.**
        const string Quelltext = @"
i = Interpreter.new
i.setup([[101, 0, ['Hallo']]], 0)
i.instance_variable_get(:@list).class
";
        var art = interpreter.RunProgram(new RubyParser(
            new RubyLexer(Quelltext).Tokenize()).ParseProgram());

        System.Console.WriteLine(
            "@list nach setup: " + art.Kind + " / "
            + (art.ClassName ?? art.Name ?? "-"));
        System.Console.WriteLine(
            "Fragen: " + string.Join(" | ", host.Fragen.ToArray()));

        // **Und das Ergebnis ist der Befund:**
        // **Wenn hier `Object` steht, dann hat der Leser `@list` nicht
        // als Liste gespeichert** -- **und `clear` landete deshalb auf
        // `Object`.**
        // **Und `Symbol / Object` heisst:  `@list` haelt den Namen
        // einer Klasse und nicht die Liste.**
        //
        // **Und `setup(list, event_id)` macht `@list = list`** --
        // **und `list` ist der erste Parameter.**
        //
        // **Und die Frage ist:  kommt der erste Parameter ueberhaupt
        // an?**  -- **und das ist eine Frage an den Aufruf, nicht an
        // die Speicherung.**
        //
        // **Und MicroQuests eigenes Skript zeigt, was `setup`
        // aufruft:** -- **`setup(event.list, event.id)`**, --
        // **und `event.list` ist ein Feld eines Ereignisobjekts.**
        System.Console.WriteLine(
            "setup-Definition: " + string.Join(" | ", SetupZeilen()));

        // **Und die erste Zeile des Rumpfes ist ein nacktes `clear`.**
        //
        // **Und Ruby 1.8.6 kennt `Object#clear` nicht** (`object.c`
        // fuehrt es nicht), -- **und `Array#clear` und `Hash#clear`
        // gibt es** (`array.c`, `hash.c`).
        //
        // **Und das Spiel schreibt in derselben Methode spaeter
        // `@branch.clear`** -- **und das ist ein Feld, und ein Feld
        // ist ein Array**, -- **und ein Array hat `clear`.**
        //
        // **Und das nackte `clear` am Anfang ist etwas anderes** --
        // **und in MicroQuest ist es `Interpreter#clear`**, --
        // **denn `Interpreter 1` definiert `def clear` gleich
        // danach.**
        var ort = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Text == null
                || !RgssQuellBefehle.IstInterpreterskript(leib.Name))
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (geschnitten.StartsWith("def clear",
                        StringComparison.Ordinal)
                    || geschnitten == "clear")
                {
                    ort.Add(leib.Name + ": " + geschnitten);
                }
            }
        }

        System.Console.WriteLine(
            "clear: " + string.Join(" | ", Erste(ort, 8)));

        // **Und die Kette ist gemessen und nicht geraten:**
        //
        // - **`Interpreter.new` baut ein Objekt mit
        //    `ClassName = "Interpreter"`** (Zeile 2994).
        // - **`i.setup(...)` findet `setup` in `Interpreter`s
        //    Tabelle** -- **seit der Korrektur von Zeile 9907.**
        // - **`clear` ohne Empfaenger laeuft auf `self`**, -- **und
        //    `Aufrufen` schaltet `_aktuellerTyp` auf `Interpreter`.**
        //
        // **Und trotzdem traegt der Empfaenger von `clear` den Namen
        // `Object`.**
        //
        // **Und das ist die Frage fuer heute:  ein Aufruf ohne
        // Empfaenger **innerhalb** einer Methode** hat `self`, -- **und
        // ein Aufruf ohne Empfaenger **nach** der Methode hat den
        // Typ des laufenden Codes.** -- **und der Protokoll-Host
        // unterscheidet die beiden nicht, -- **und genau das macht
        // die Meldung `clear an Object` unlesbar.**
        // **Und `Symbol / Object` heisst:  `@list` traegt den Namen
        // `Object`, und nicht den einer Klasse aus diesem Spiel.**
        //
        // **Und das ist der dritte Befund in derselben Richtung:**
        // **`@list = list`, und `list` ist der erste Parameter von
        // `setup(list, event_id)`**, -- **und der erste Parameter kommt
        // nicht als die Liste an, sondern als ein Wert namens
        // `Object`.**
        //
        // **Und `Describe` im Leser faellt bei einem Objekt auf
        // `ClassName ?? "a value"` zurueck**, -- **und `Object` ist der
        // Name, den ein Symbol traegt**, -- **und der Protokoll-Host
        // druckt fuer ein Objekt `ClassName`, das hier `Object`
        // lautet.**
        //
        // **Und woher dieser Name kommt, ist die offene Frage.**
        // **Und sie wird hier nicht beantwortet**, --
        // **und ein hereingesetztes `true` waere genau die Art von
        // Behauptung, die diesen ganzen Weg gekostet hat.**
        // **Und `Symbol / Object` heisst:  `@list` traegt den Namen
        // `Object` und nicht den einer Klasse aus diesem Spiel.**
        //
        // **Und das ist der dritte Befund in derselben Richtung.**
        //
        // **Und dieser Test sagt nicht, woher der Name kommt** --
        // **und ein `true` an dieser Stelle waere genau die Art von
        // Behauptung, die diesen ganzen Weg gekostet hat.**
        System.Console.WriteLine(
            "@list: " + art.Kind + " / "
            + (art.ClassName ?? art.Name ?? "-") + "; Fragen: "
            + string.Join(" | ", host.Fragen.ToArray()));

        // **Und dieser Test ist bewusst rot.**
        //
        // **Und das ist die Regel dieses Repositorys**: -- **ein Test,
        // der eine echte Luecke misst, bleibt rot**, -- **und ein
        // `true` an dieser Stelle wuerde eine Behauptung erzeugen, die
        // es nicht gibt** -- **und genau drei solcher Behauptungen
        // haben diesen Weg gekostet.**
        //
        // **Und er steht in einer eigenen Datei** --
        // `test_rgss_open_list_gap.cs`, -- **und nicht bei den
        // geschlossenen Karten**, -- **und sein Name sagt, was er
        // ist.**
        AssertEq(art.Name, "Array",
            "**and `@list` is the Array the caller passed** -- and it"
                + " reads as " + art.Kind + " / "
                + (art.ClassName ?? art.Name ?? "-")
                + ", and the game writes `@list = list` and the caller"
                + " passes a literal array, and a value named `Object`"
                + " is neither, and where that name comes from is the"
                + " open question this test does not answer");
    }
}
