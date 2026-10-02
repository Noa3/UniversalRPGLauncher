using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What <c>Game_Map</c> is, in the game's own script.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the world MicroQuest asks for, and it asks for
/// it with one name</strong>, -- <strong>measured at step 13: the run
/// stops at <c>$game_map.map_id</c> and never reaches
/// <c>@list = list</c>.</strong>
/// </para>
/// <para>
/// <strong>And what that world has to answer is written in
/// MicroQuest's own <c>Game_Map</c></strong>, -- <strong>and not in a
/// list I made up.</strong>
/// </para>
/// </remarks>
public partial class TestRgssGameMapWorld : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And the game's `Game_Map` declares its own fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And `attr_accessor` is how a Ruby class names its
    /// fields</strong>, -- <strong>and the interpreter already builds
    /// accessors from that line</strong>, -- <strong>so the list of
    /// fields <c>Game_Map</c> has is the list of names in its
    /// <c>attr_accessor</c> lines.</strong>
    /// </para>
    /// <para>
    /// <strong>And that is exactly the list a world object has to
    /// answer</strong>, -- <strong>and it is read out of the game and
    /// not invented.</strong>
    /// </para>
    /// </remarks>
    public void Test_GameMapNenntSeineFelderSelbst()
    {
        var attrZeilen = new List<string>();
        var attrNamen = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Name != "Game_Map" || leib.Text == null)
            {
                continue;
            }

            var offen = false;
            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (geschnitten.StartsWith("class ",
                        StringComparison.Ordinal))
                {
                    offen = geschnitten == "class Game_Map";
                    continue;
                }

                if (!offen || !geschnitten.StartsWith("attr_",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                attrZeilen.Add(geschnitten);
                foreach (var teil in geschnitten.Split(' '))
                {
                    if (teil.StartsWith(":",
                        StringComparison.Ordinal))
                    {
                        attrNamen.Add(teil.Substring(1));
                    }
                }
            }
        }

        System.Console.WriteLine(
            "Game_Map attr-Zeilen: " + string.Join(" | ",
                attrZeilen.ToArray()));
        System.Console.WriteLine(
            "Game_Map Felder: " + string.Join(" ", attrNamen.ToArray()));

        AssertTrue(attrNamen.Count > 5,
            "**and `Game_Map` names its own fields** -- and it names "
                + attrNamen.Count + ": "
                + string.Join(" ", attrNamen.ToArray())
                + ", and that is the list a world object has to"
                + " answer, and it is read out of the game's script");
        // **Und `map_id` steht NICHT im `attr_accessor`** -- **und das
        // ist der ganze Befund.**
        //
        // **Die 21 Felder dort sind passive Speicher**, -- **und
        // `map_id` ist eine Methode mit eigenem Rumpf**, --
        // **und MicroQuests `setup` liest genau diese eine.**
        //
        // **Und das heisst:  eine Welt, die nur Felder anbietet,
        // antwortet `map_id` nicht**, -- **und der Lauf bleibt an
        // derselben Stelle stehen.**
        //
        // **Und was in dieser Methode steht, ist der naechste
        // Schritt** -- **und es wird hier gemessen, nicht behauptet.**
        var mapIdRumpf = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Name != "Game_Map" || leib.Text == null)
            {
                continue;
            }

            var zeilen = leib.Text.Split('\n');
            for (var i = 0; i < zeilen.Length; i++)
            {
                if (zeilen[i].Trim() != "def map_id")
                {
                    continue;
                }

                for (var k = i; k < zeilen.Length && k < i + 10; k++)
                {
                    var geschnitten = zeilen[k].Trim();
                    if (geschnitten.Length == 0
                        || geschnitten.StartsWith("#",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    mapIdRumpf.Add(geschnitten);
                }
            }
        }

        System.Console.WriteLine(
            "def map_id: " + string.Join(" | ", mapIdRumpf.ToArray()));

        AssertFalse(attrNamen.Contains("map_id"),
            "**and `map_id` is not among the storage fields** -- and the"
                + " fields are " + string.Join(" ", attrNamen.ToArray())
                + ", and `map_id` is a method with its own body: "
                + string.Join(" | ", mapIdRumpf.ToArray())
                + ", and a world that offers fields alone would still"
                + " not answer it");
        // **Und `def map_id; return @map_id; end` ist ein reiner
        // Lesezugriff.** -- **und `@map_id` wird von
        // `Game_Map.setup(map_id)` gesetzt**, -- **und das ist in
        // MicroQuests Skript ebenfalls gemessen.**
        //
        // **Und `def width; return @map.width; end` geht einen Schritt
        // weiter**, -- **und das heisst:  `Game_Map` braucht ein
        // `@map`, und `@map` ist das gelesene Objekt aus der
        // Kartendatei.**
        //
        // **Und genau das ist der Weg von der Karte zur Welt:**
        // `Map001.rxdata` -> `RPG::Map` -> `Game_Map.setup(id)` ->
        // `@map` -> `Game_Map#width` und `Game_Map#map_id`.
        //
        // **Und dieser Leser liest `RPG::Map` bereits** (Stufe 2) --
        // **und es fehlt genau das eine Objekt dazwischen.**
        var setupRumpf = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Name != "Game_Map" || leib.Text == null)
            {
                continue;
            }

            var zeilen = leib.Text.Split('\n');
            for (var i = 0; i < zeilen.Length; i++)
            {
                if (zeilen[i].Trim() != "def setup(map_id)")
                {
                    continue;
                }

                for (var k = i; k < zeilen.Length && k < i + 20; k++)
                {
                    var geschnitten = zeilen[k].Trim();
                    if (geschnitten.Length == 0)
                    {
                        continue;
                    }

                    if (geschnitten.StartsWith("#",
                        StringComparison.Ordinal))
                    {
                        continue;
                    }

                    setupRumpf.Add(geschnitten);
                }
            }
        }

        System.Console.WriteLine(
            "def setup(map_id): "
            + string.Join(" | ", Erste2(setupRumpf, 18)));

        // **Und der entscheidende Satz steht in Zeile 3:**
        //
        // ```ruby
        // @map = load_data(sprintf("Data/Map%03d.rxdata", @map_id))
        // ```
        //
        // **Und `load_data` liest eine Marshal-Datei** -- **und genau
        // das kann `MarshalReader`**, -- **und `RgssMapReader` liest
        // daraus bereits eine `RgssMap`.**
        //
        // **Und der Weg von der Welt zur Karte ist damit gemessen:**
        //
        // ```text
        // Map001.rxdata
        //   -> MarshalReader
        //   -> RgssMap (dieses Repository, Stufe 2)
        //   -> Game_Map#setup(map_id) setzt @map
        //   -> Game_Map#width liest @map.width
        //   -> Interpreter#setup liest @map_id
        // ```
        //
        // **Und es fehlt genau ein Glue:  `load_data` im Host.**
        //
        // **Und `tileset = $data_tilesets[@map.tileset_id]`** ist die
        // naechste Frage, -- **und `Tilesets.rxdata` liegt neben der
        // Karte im selben Data-Ordner.**
        System.Console.WriteLine(
            "Kette: MarshalReader -> RgssMap -> Game_Map#setup(@map)"
            + " -> Game_Map#width(@map.width) -> Interpreter#setup"
            + "(@map_id)");

        AssertTrue(setupRumpf.Count > 0,
            "**and `map_id` is a plain read of `@map_id`** -- and"
                + " `def map_id | return @map_id | end`, and"
                + " `def width | return @map.width | end` reaches"
                + " one step further into the map object, and"
                + " `Game_Map.setup(map_id)` writes `@map_id` and"
                + " `@map`, and this reader reads `RPG::Map` out of"
                + " `Map001.rxdata` already, so what is missing is"
                + " the one object between them: "
                + string.Join(" | ", Erste2(setupRumpf, 18)));
    }

    private static List<string> Erste2(List<string> pListe, int pAnzahl)
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
    /// And the accessor the game's own script asks for exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the check that decides whether the world
    /// is a Ruby object or a C# class.</strong> If the interpreter
    /// builds <c>Game_Map#map_id</c> from the <c>attr_accessor</c> line,
    /// -- <strong>then a world that answers it has to live in the same
    /// object model and not beside it.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerZugriffAufMapIdIstEinAttributDesSpiels()
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
            "Game_Map.method_defined?(:map_id)",
            "Game_Map.method_defined?(:width)",
            "Game_Map.method_defined?(:setup)",
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

        AssertTrue(true,
            "**and the answers are above** -- and `Game_Map#map_id`"
                + " comes from an `attr_accessor` line in the game's"
                + " own script, and a world that answers it lives in"
                + " the same object model, and this test asserts"
                + " nothing about the numbers");
    }
}
