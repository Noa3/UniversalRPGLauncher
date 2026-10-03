using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// How far MicroQuest's own `setup` gets when the globals are set.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this measures the XP blocker's real shape.</strong>
/// <c>Interpreter.setup</c> stops at <c>@map_id = $game_map.map_id</c>,
/// because <c>$game_map</c> is <c>nil</c> -- <strong>and the host that
/// recorded that answers every question with <c>null</c>.</strong>
/// </para>
/// <para>
/// <strong>And <c>RubyInterpreter.SetzeGlobal</c> exists</strong>,
/// -- <strong>and this asks what happens when <c>$game_map</c> and its
/// neighbours are set before the run</strong>, -- <strong>because
/// "the scripts run" and "the interpreter reaches line two of
/// <c>setup</c>" are different claims and only one of them is
/// measured here.</strong>
/// </para>
/// </remarks>
public partial class TestRgssSetupWelt : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    /// <summary>
    /// And the step it reaches, with and without the globals.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the two runs are printed one after the other</strong>,
    /// -- <strong>because the difference between them is the whole
    /// question and a single number would hide it.</strong>
    /// </para>
    /// </remarks>
    public void Test_WieWeitSetupKommt()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out var grund);
        if (skripte == null)
        {
            throw new InvalidOperationException(grund);
        }

        Console.WriteLine("--- ohne Globals");
        var a = new ProtokollHost(skripte);
        var i1 = new RubyInterpreter(a);
        Schreibe(i1, false);
        var stell1 = Lauf(i1, a);
        Console.WriteLine("  erreicht: " + stell1);
        Console.WriteLine("  ohne Antwort: " + a.OhneAntwort);

        Console.WriteLine("--- mit $game_map");
        var b = new ProtokollHost(skripte);
        var i2 = new RubyInterpreter(b);
        Schreibe(i2, true);
        var stell2 = Lauf(i2, b);
        Console.WriteLine("  erreicht: " + stell2);
        Console.WriteLine("  ohne Antwort: " + b.OhneAntwort);

        Console.WriteLine("--- die Fragen der zweiten Runde");
        var gezeigt = 0;
        foreach (var f in b.Fragen)
        {
            if (gezeigt++ >= 20)
            {
                Console.WriteLine("  ... und " + (b.Fragen.Count - 20)
                    + " weitere");
                break;
            }

            Console.WriteLine("  " + f);
        }

        AssertTrue(true,
            "**and both runs are printed above** -- and how far"
                + " `setup` gets with and without the globals is the"
                + " measurement, and this test asserts only that it"
                + " was taken");
    }

    /// <summary>And the globals a page needs, set or not.</summary>
    /// <param name="pInterpreter">The interpreter to set them on.</param>
    /// <param name="pSetzen">Whether to set them at all.</param>
    private static void Schreibe(RubyInterpreter pInterpreter, bool pSetzen)
    {
        if (!pSetzen)
        {
            return;
        }

        // **Und `$game_map` ist der erste, an dem `setup` scheitert**,
        // **und `$game_player` der naechste**, -- **und beide sind
        // Objekte, die der Host beantworten soll.**
        foreach (var name in new[]
        {
            "$game_map", "$game_player", "$game_party", "$game_variables",
            "$game_switches", "$game_self_switches", "$game_temp",
            "$game_system", "$game_message", "$game_actors", "$game_enemies",
            "$game_time", "$game_debug", "$game_managers",
        })
        {
            // **Und `OfEmptyObject` und nicht `OfObject`**, --
            // **und das ist gemessen an der Signatur**, --
            // **und ein Objekt ohne Membern ist hier richtig**,
            // **denn die Methoden beantwortet der Host.**
            pInterpreter.SetzeGlobal(name,
                RubyValue.OfEmptyObject("Game_" + name.TrimStart('$')));
    
        }
    }

    /// <summary>And where the run stopped.</summary>
    /// <param name="pInterpreter">The interpreter that ran.</param>
    /// <param name="pHost">The host that answered.</param>
    /// <returns>A line of text naming the last question.</returns>
    private static string Lauf(RubyInterpreter pInterpreter, ProtokollHost pHost)
    {
        // **Und der Aufruf geht durch einen kleinen Quelltext und
        // nicht durch alle 90 Skripte.**
        //
        // **Und das ist nicht Kosmetik:**  **beim blossen Laden der
        // Skripte wird `setup` nie aufgerufen** --
        // **und mein erster Versuch hat genau das getan und
        // darum beide Laeufe gleich gemessen.**
        //
        // **Und MicroQuests `setup` traegt `def setup(list, event_id)`,
        // und Befehl 101 ist einer, den das Spiel kennt.**
        const string kQuelltext = @"
i = Interpreter.new
i.setup([[101, 0, ['Hallo']]], 0)
";

        // **Und erst alle 90 Skripte laden,  und dann aufrufen.**
        //
        // **Und das ist gemessen,  und es ist die ganze
        // Unterscheidung:**
        //
        // ```text
        // ohne das Laden:  Konstante Interpreter / new an Nil
        // mit  dem Laden:  clear an Object / map_id an Nil
        // ```
        //
        // **Und mein erster Versatz hat die Skripte nicht geladen,
        //  und darum kam bei beiden Laeufen `new an Nil` heraus**
        // -- **und beide Laeufe sahen gleich aus,  und der Test hat
        // nichts gemessen.**
        foreach (var name in new List<string>(pHost.SkriptNamen()))
        {
            var bytes = pHost.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                pInterpreter.RunProgram(new RubyParser(new RubyLexer(
                    System.Text.Encoding.UTF8.GetString(bytes))
                    .Tokenize()).ParseProgram());
            }
            catch (RubyParseException)
            {
                // **Und siehe `TestRgssSkriptHost`** --
                // **und ein Skript,  das nicht parst,  ist hier nicht
                // der Gegenstand.**
            }
        }

        try
        {
            pInterpreter.RunProgram(new RubyParser(
                new RubyLexer(kQuelltext).Tokenize()).ParseProgram());
        }
        catch (RubyRuntimeException ausnahme)
        {
            return ausnahme.Message;
        }
        catch (RubyParseException ausnahme)
        {
            return ausnahme.Message;
        }

        return "(kein Fehler, alle Skripte gelesen)";
    }
}
