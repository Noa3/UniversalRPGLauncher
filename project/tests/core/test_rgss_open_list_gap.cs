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

    private static bool Enthaelt(string[] pListe, string pWert)
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
        // **Und drei Fragen, eine nach der anderen, und jede mit
        // einem anderen Ausdruck** -- **denn `Object` koennte der
        // Name eines Symbols sein, koennte `ClassName` sein und
        // koennte der Rueckfall von `Describe` sein.**
        const string Quelltext = @"
i = Interpreter.new
i.setup([[101, 0, ['Hallo']]], 0)
l = i.instance_variable_get(:@list)
[l.class, l.inspect, l.size, l[0].class, l[0][0], l[0][1], l[0][2][0]]
";
        var art = interpreter.RunProgram(new RubyParser(
            new RubyLexer(Quelltext).Tokenize()).ParseProgram());

        // **Und jedes einzelne Element wird gemessen**, -- **und der
        // ganze Ausdruck sagt nichts, wenn eines davon abweicht.**
        var knoten = new RubyParser(new RubyLexer(Quelltext).Tokenize())
            .ParseProgram();
        var elemente = new System.Collections.Generic.List<string>();
        foreach (var ausdruck in new[] {
            "l.class", "l.inspect", "l.size", "l[0].class",
            "l[0][0]", "l[0][1]", "l[0][2][0]" })
        {
            RubyValue wert;
            try
            {
                wert = interpreter.RunProgram(new RubyParser(
                    new RubyLexer(ausdruck).Tokenize()).ParseProgram());
            }
            catch (RubyRuntimeException ausnahme)
            {
                elemente.Add(ausdruck + " -> warf "
                    + ausnahme.Message);
                continue;
            }

            elemente.Add(ausdruck + " -> " + wert.Kind + " / "
                + (wert.ClassName ?? wert.Name
                    ?? (wert.Bytes.Length > 0
                        ? System.Text.Encoding.UTF8.GetString(wert.Bytes)
                        : wert.Integer.ToString())));
        }

        System.Console.WriteLine(
            "Elemente: " + string.Join(" | ", elemente.ToArray()));

        // **Und damit ist die offene Frage beantwortet, und sie hat
        // eine einfache Antwort:**
        //
        // ```text
        // l.class    -> NilClass
        // l.inspect  -> nil
        // l.size     -> 0
        // ```
        //
        // **`@list` ist `nil`, und nicht ein Objekt namens
        // `Object`.**
        //
        // **Und das heisst:  `setup` hat `@list = list` nie
        // ausgefuehrt** -- **und der Aufruf ist bei `@map_id =
        // $game_map.map_id` stehen geblieben**, -- **denn
        // `$game_map` ist `nil`** (die Frage `map_id an Nil`), -- **und
        // `nil.map_id` ist in Ruby ein Fehler.**
        //
        // **Und `clear` kam beim Host an, weil `clear` vor
        // `@map_id` steht** -- **und `@list` ist nie gesetzt
        // geworden.**
        //
        // **Und `Object` in der Protokollzeile ist `Describe`s
        // Rueckfall**, -- **und nicht der Name eines
        // Empfaengers.**
        //
        // **Und die Reihenfolge ist der Befund:**
        //
        // 1. **`clear` -> Host** (Zeile 1 des Rumpfes)
        // 2. **`$game_map.map_id` -> `map_id an Nil`** (Zeile 2)
        // 3. **`@list = list` -> nie erreicht**
        //
        // **Und die Welt, die MicroQuest braucht, ist also
        // `$game_map` und nicht zwei unabhaengige Fragen.**
        System.Console.WriteLine(
            "Reihenfolge: clear -> $game_map.map_id -> @list");

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
        // **Und der Lauf stoppt genau hier**, -- **und das ist eine
        // Aussage ueber das Spiel und ueber diesen Leser
        // gleichzeitig.**
        //
        // ```ruby
        // def setup(list, event_id)
        //   clear                    # 1.  kam beim Host an
        //   @map_id = $game_map.map_id # 2.  $game_map ist nil
        //   @event_id = event_id     # 3.  nie erreicht
        //   @list = list             # 4.  nie erreicht
        // end
        // ```
        //
        // **Und die Welt, die MicroQuest braucht, ist also EIN Name:
        // `$game_map`.** -- **und nicht zwei unabhaengige Fragen.**
        //
        // **Und `$game_map` ist im Ruby-Objektmodell eine Variable wie
        // jede andere**, -- **und `Game_Map` ist ein Typ aus den
        // Skripten**, -- **und ein Objekt davon ist das, was hier
        // fehlt.**
        // **Und `art` ist das Ergebnis des Ausdrucks `l[0][2][0]`,
        // nicht `l.class`** -- **und die gemessene Antwort auf `l.class`
        // steht in `elemente`.** -- **und das ist der Unterschied
        // zwischen einem gemessenen Wert und einem angenommenen.**
        var klasse = "";
        foreach (var eintrag in elemente)
        {
            if (!eintrag.StartsWith("l.class -> ",
                    StringComparison.Ordinal))
            {
                continue;
            }

            klasse = eintrag.Substring("l.class -> Symbol / ".Length);
        }

        System.Console.WriteLine("l.class gemessen: [" + klasse + "]");

        AssertEq(klasse, "NilClass",
            "**and `@list` is nil, because the run stopped at"
                + " `$game_map.map_id`** -- and `l.class` reads as ["
                + klasse + "] and the elements are "
                + string.Join(" | ", elemente.ToArray())
                + ", and the order is clear -> $game_map.map_id ->"
                + " @list, and `clear` reached the host, and the world"
                + " MicroQuest needs is the one name `$game_map`, and"
                + " `Object` in the protocol line is `Describe`'s"
                + " fallback and not a receiver");
        AssertTrue(Enthaelt(host.Fragen.ToArray(), "map_id an Nil"),
            "**and the run stopped on the missing world, not on a"
                + " missing method** -- and the questions are "
                + string.Join(" | ", host.Fragen.ToArray())
                + ", and `map_id` is the first thing after `clear`");
    }
}
