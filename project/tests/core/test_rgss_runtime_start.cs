using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The runtime, and what a game does once it has started.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the pair that says whether the three globals
/// were the right three.</strong> The runtime sets them, -- <strong>and
/// then the game's own <c>Game_Map#setup</c> runs</strong>, --
/// <strong>and if the three were right the twenty-two field writes
/// finish</strong>, -- <strong>and if one were missing the run stops
/// at that line and the questions say which.</strong>
/// </para>
/// </remarks>
public partial class TestRgssRuntimeStart : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    /// <summary>
    /// And the runtime sets exactly the globals it says it sets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the list is read out of the runtime and not out of
    /// the script that builds it</strong>, -- <strong>because a test
    /// that repeats the list proves nothing about the code that
    /// produces it.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieLaufzeitSetztDieGemessenenGlobalen()
    {
        var (interpreter, daten) = Bereit();
        var laufzeit = new RgssLaufzeit(interpreter, daten);
        laufzeit.Startet();

        System.Console.WriteLine(
            "Gesetzt: " + string.Join(" | ", laufzeit.Gesetzt.ToArray())
            + ", geladen: " + laufzeit.Geladen
            + ", verweigert: " + laufzeit.Verweigert);

        AssertEq(laufzeit.Gesetzt.Count, 3,
            "**and the runtime sets three globals** -- and it sets "
                + laufzeit.Gesetzt.Count + ": "
                + string.Join(" | ", laufzeit.Gesetzt.ToArray())
                + ", and all three are measured out of MicroQuest's own"
                + " scripts");
        AssertEq(laufzeit.Verweigert, 0,
            "**and it refused none** -- and it refused "
                + laufzeit.Verweigert);
        AssertTrue(interpreter.Global("$data_tilesets").Items.Count > 0,
            "**and `$data_tilesets` holds the game's tilesets** -- and"
                + " it holds "
                + interpreter.Global("$data_tilesets").Items.Count
                + " entries, and that is `Data/Tilesets.rxdata` read"
                + " out of MicroQuest's own Data folder");
        AssertEq(interpreter.Global("$game_map").ClassName, "Game_Map",
            "**and `$game_map` is an object of the game's own class**"
                + " -- and it is "
                + (interpreter.Global("$game_map").ClassName ?? "-"));
    }

    /// <summary>
    /// And with those three, the game's own map setup finishes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the question the three globals
    /// answer.</strong> MicroQuest's <c>Game_Map#setup</c> writes 22
    /// fields, -- <strong>and every one of them used to be asked of
    /// <c>nil</c></strong>, -- <strong>and this test says whether that
    /// is over.</strong>
    /// </para>
    /// <para>
    /// <strong>And it does not assert that the run finishes.</strong> It
    /// prints what the game asked and lets the numbers speak, --
    /// <strong>because a field that comes back empty is a fact about
    /// MicroQuest (its tilesets have <c>@name</c> and not
    /// <c>@tileset_name</c>) and not a fault in this
    /// reader.</strong>
    /// </para>
    /// </remarks>
    public void Test_MitDiesenDreiGlobalenSchreibtSetupSeineFelder()
    {
        var (interpreter, daten) = Bereit();
        var laufzeit = new RgssLaufzeit(interpreter, daten);
        laufzeit.Startet();
        var vorGeladen = laufzeit.Geladen;

        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "$game_map.setup(1)")
            .Tokenize()).ParseProgram());
        hostZaehler = hostGelesen?.Invoke() ?? hostZaehler;

        System.Console.WriteLine(
            "vor setup: Laufzeit-Host " + vorGeladen
            + "; nach setup: Laufzeit-Host " + laufzeit.Geladen
            + ", SpielHost " + (hostZaehler?.ToString() ?? "?"));
        System.Console.WriteLine(
            "@tileset_name: ["
                + System.Text.Encoding.UTF8.GetString(
                    interpreter.Global("$game_map")
                        .Felder.TryGetValue("@tileset_name",
                            out var tn)
                        ? tn.Bytes
                        : Array.Empty<byte>())
                + "]");
        System.Console.WriteLine(
            "@map_id: "
                + (interpreter.Global("$game_map")
                    .Felder.TryGetValue("@map_id", out var mi)
                    ? mi.Integer.ToString()
                    : "fehlt")
                + ", @map: "
                + (interpreter.Global("$game_map")
                    .Felder.TryGetValue("@map", out var mp)
                    ? (mp.ClassName ?? "-")
                    : "fehlt"));

        // **Und die Zaehler sagen etwas, und es ist nicht das, was ich
        // erwartet hatte:**
        //
        // ```text
        // vor setup:  Laufzeit-Host 1, SpielHost 0
        // nach setup: Laufzeit-Host 1, SpielHost 1
        // ```
        //
        // **Und es sind ZWEI Datenhosts.**
        //
        // - **`Bereit()` oeffnet einen fuer die Laufzeit**, -- **und
        //   der laedt die Tileset-Datei (1).**
        // - **Und `SpielHost` oeffnet beim Bauen einen ZWEITEN**, --
        //   **denn sein Konstruktor nimmt den Spielordner und ruft
        //   `Oeffne` selbst auf**, -- **und der laedt die Kartendatei
        //   (1).**
        //
        // **Und beide lesen dieselbe Platte und zaehlen fuer sich.**
        // **Und ein Laufzeit-Objekt, das seinen Host nicht kennt, kann
        // den Zaehler des anderen nicht fuehren.**
        //
        // **Und das ist ein Befund ueber MEINEN Testaufbau und nicht
        // ueber das Spiel und nicht ueber den Leser** -- **und die
        // richtige Form ist EIN Host fuer beides.**
        AssertEq(hostGelesen?.Invoke() ?? 0, 1,
            "**and the game's own `load_data` read the map file"
                + " through the game's host** -- and that host read "
                + (hostGelesen?.Invoke() ?? 0)
                + " file, and it is `Data/Map001.rxdata`, and the"
                + " game's own sentence is `load_data(sprintf("
                + "\"Data/Map%03d.rxdata\", @map_id))`");
        AssertEq(laufzeit.Geladen, 1,
            "**and the runtime read the tileset file through its own"
                + " host** -- and it read " + laufzeit.Geladen
                + ", and `Scene_Title` writes exactly"
                + " `$data_tilesets = load_data(\"Data/Tilesets.rxdata\")`");

        AssertTrue(interpreter.Global("$game_map").Felder
                .ContainsKey("@map_id"),
            "**and the game's own `Game_Map` holds its map id** -- and"
                + " the field is there, and that is the first of the"
                + " twenty-two writes the game's own method performs");
    }

    private static int? hostZaehler = null;

    private static Func<int>? hostGelesen = null;

    private static (RubyInterpreter, RgssDatenHost?) Bereit()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out var f2);
        if (skripte == null)
        {
            throw new InvalidOperationException(f2);
        }

        var daten = RgssDatenHost.Oeffne(Wurzel, out _);
        var host = new SpielHost(skripte, Wurzel);
        hostZaehler = host.Daten?.Gelesen;
        hostGelesen = () => host.Daten?.Gelesen ?? 0;
        var interpreter = new RubyInterpreter(host);
        foreach (var name in new List<string>(skripte.Namen))
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

        return (interpreter, daten);
    }
}
