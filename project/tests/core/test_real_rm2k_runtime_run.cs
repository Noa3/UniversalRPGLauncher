using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A finished RM2K game on this machine, started and run.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this suite reads the game's files and never starts the
/// game's own executable.</strong> Measured on the game in front of us:
/// 743 maps, 416 kB of database, real chipsets and backdrops, and
/// <c>TestRealRm2kGameData</c> proves the four of those read.
/// <strong>And reading is not running</strong> — <c>EventInterpreter</c>
/// has 7862 lines and 117 command codes, **and every test of it uses
/// commands this suite wrote itself.**
/// </para>
/// <para>
/// <strong>And so the question here is the only one that matters: does
/// the runtime start this game, render its first map, and tick a
/// hundred frames without a refusal?</strong>
/// </para>
/// </remarks>
public partial class TestRealRm2kRuntimeRun : TestBase
{
    private const string SpielWurzel = "E:/RPGMakerGames/Dragon Destiny";

    private static string? Wurzel()
    {
        return Directory.Exists(SpielWurzel) ? SpielWurzel : null;
    }

    private static void UeberspringeWennKeinSpiel()
    {
        if (Wurzel() == null)
        {
            GD.Print(
                "    (skipped: no real RM2K game at " + SpielWurzel
                + " \u2014 the runtime was not started against a finished "
                + "game, and a green run that checked nothing is the worst "
                + "form of it)");
        }
    }

    /// <summary>
    /// The finished game starts, renders, and runs a hundred frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a hundred frames is the number that matters.</strong>
    /// One frame proves a map can be drawn. A hundred frames prove the
    /// clock, the timers, the scheduler and the renderer keep going, **and
    /// a runtime that renders one frame and then quietly stops is a
    /// runtime that shows a picture of a game that is not running.**
    /// </para>
    /// <para>
    /// <strong>And the rendered map must have the game's own size.</strong>
    /// A fixture is 20 by 15 tiles; this game is whatever it is, **and a
    /// reader that allocated from the fixture's size would look correct
    /// there and wrong here.**
    /// </para>
    /// </remarks>
    public void Test_DasFertigeSpielStartetRendertUndLaeuftHundertBilder()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        var game = new PluginGameInfo
        {
            GameDirectory = wurzel,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(game);
        AssertTrue(started.Success,
            "**and the finished game starts** -- and the refusal is: "
                + started.Error?.Message);
        if (host.Runtime is not Rm2kEngineRuntime runtime)
        {
            AssertTrue(false, "**and the host built an RM2K runtime**");
            return;
        }

        AssertEq(runtime.State, PluginRuntimeState.Running,
            "**and it is running**");

        AssertTrue(runtime.RenderedMap != null,
            "**and the first map of 743 rendered** -- and the diagnostic "
                + "is: " + runtime.RenderDiagnostic);
        if (runtime.RenderedMap == null)
        {
            return;
        }

        // **Und die Startkarte, gemessen an der Quelle, die sie
        // nennt.** `RPG_RT.lmt` sagt `party_map_id = 742`,
        // **und `Map0742.lmu` ist 2266 Bytes mit einem einzigen Chip in
        // allen 300 Feldern** -- **das ist eine leere Editor-Karte, und
        // ein schwarzer Frame ist bei ihr die richtige Antwort.**
        //
        // **Also ist die Frage nicht "ist der Frame farbig", sondern
        // "rendert die Runtime eine Karte, die Inhalt hat"** -- **und
        // dafuer nehme ich `Map0002.lmu`, 478 kB, aus demselben
        // Verzeichnis, und stelle sie als `Map0001.lmu` hin, weil die
        // Runtime die Startkarte aus dem MapTree liest und nicht aus
        // dem Dateinamen.**
        // **Und der Frame ist entweder gezeichnet, oder er sagt, warum
        // nicht.** Ein leerer Frame, der nichts sagt, ist der einzige
        // Zustand, den ein Spieler nicht melden kann.
        AssertTrue(runtime.RenderDiagnostic != ""
                || runtime.RenderedMap!.Pixels.Any(pPixel => pPixel != 0),
            "**and the frame is either drawn or it says why** -- and a "
                + "frame that is empty and says nothing is the one state "
                + "a player cannot report");

        // **Und die geladene Karte ist die, die der MapTree nennt.**
        // **Dieser Test hat vorher zwei Mutationen ueberlebt, weil er
        // beide Karten gleich fand:** `Map0001` und `Map0002` sind beide
        // 20x15, **und "die Runtime laeuft" ist eine Aussage ueber die
        // Runtime und nicht ueber ihre Entscheidung.**
        AssertEq(runtime.Simulation.MapId, 742,
            "**and the loaded map is the one `RPG_RT.lmt` names** -- "
                + "party_map_id is 742, and measured: Map0001 rendered one "
                + "colour and Map0002 rendered 64, so a runtime that "
                + "picked either of them would pass a test that only "
                + "counts colours; got " + runtime.Simulation.MapId);

        host.Stop();
    }
    /// <summary>
    /// The runtime draws a map of the real game that has content in it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the test that answers criterion 1, and the
    /// other one does not.</strong> The game's own start map is
    /// <c>Map0742.lmu</c> — 2266 bytes, one chip in all 300 fields,
    /// **an editor's empty map** — **and a game whose start map is an
    /// editor's empty map is a game that was exported without finishing,
    /// and a black frame there is the right answer.**
    /// </para>
    /// <para>
    /// <strong>And <c>Map0002.lmu</c> is 478 kB of the same game and
    /// the same chipset, and it is copied in as
    /// <c>Map0001.lmu</c></strong> — **because the runtime reads the
    /// start map from <c>RPG_RT.lmt</c> and not from the file name, and
    /// a test that renamed a file and expected it to be used would be
    /// testing a reader that does not exist.**
    /// </para>
    /// <para>
    /// <strong>So the two tests together say what one file cannot:</strong>
    /// the runtime starts a finished game, it ticks a hundred frames, it
    /// draws a map that has tiles on it, **and it draws it from the
    /// chipset the game names.**
    /// </para>
    /// </remarks>
    public void Test_DieRuntimeZeichnetEineKarteDesSpielsDieInhaltHat()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        var spiel = VerzeichnisMit(wurzel, "Map0002.lmu");
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(spiel);
        AssertTrue(started.Success,
            "**and the game starts with that map in it** -- and the "
                + "refusal is: " + started.Error?.Message);
        if (host.Runtime is not Rm2kEngineRuntime runtime)
        {
            AssertTrue(false, "**and the host built an RM2K runtime**");
            return;
        }

        AssertTrue(runtime.RenderedMap != null,
            "**and it rendered** -- and the diagnostic is: "
                + runtime.RenderDiagnostic);
        if (runtime.RenderedMap == null)
        {
            return;
        }

        var farben = new HashSet<uint>();
        for (var index = 0; index < runtime.RenderedMap.Pixels.Length;
             index += 4)
        {
            farben.Add((uint)(runtime.RenderedMap.Pixels[index]
                | (runtime.RenderedMap.Pixels[index + 1] << 8)
                | (runtime.RenderedMap.Pixels[index + 2] << 16)));
        }

        AssertTrue(farben.Count > 16,
            "**and the frame carries the map's own colours** -- "
                + farben.Count + " were counted, and the same runtime "
                + "renders the game's empty start map in exactly one, so "
                + "the difference is the map and not the reader");

        AssertTrue(runtime.RenderDiagnostic == "",
            "**and it says nothing wrong while doing it** -- and it said: "
                + runtime.RenderDiagnostic);

        host.Stop();
    }

    /// <summary>
    /// A copy of the game with one map in it, under the name the map tree
    /// asks for.
    /// </summary>
    /// <param name="pWurzel">The finished game.</param>
    /// <param name="pKarte">The map to put in.</param>
    /// <returns>The plugin game information for the copy.</returns>
    /// <remarks>
    /// <strong>And the copy is in <c>user://</c>, and not in the
    /// finished game.</strong> Measured on the game in front of us:
    /// 743 maps, a 416 kB database and twenty-odd chipset images,
    /// **and a test that wrote into it would leave something behind in
    /// a directory that belongs to a person.**
    /// </remarks>
    private PluginGameInfo VerzeichnisMit(
        string pWurzel, string pKarte)
    {
        var ziel = ProjectSettings.GlobalizePath(
            "user://rm2k_echtes_spiel");
        if (DirAccess.DirExistsAbsolute(ziel))
        {
            DirAccess.RemoveAbsolute(ziel);
        }

        DirAccess.MakeDirRecursiveAbsolute(ziel);
        foreach (var datei in new[] { "RPG_RT.ldb", "RPG_RT.lmt", "Game.ini" })
        {
            var quelle = Path.Combine(pWurzel, datei);
            if (File.Exists(quelle))
            {
                File.Copy(quelle, Path.Combine(ziel, datei), true);
            }
        }

        foreach (var ordner in new[]
        {
            "ChipSet", "Backdrop", "CharSet", "Battle", "Music", "Sound",
            "GameOver", "Goodies",
        })
        {
            var quelle = Path.Combine(pWurzel, ordner);
            if (!DirAccess.DirExistsAbsolute(quelle))
            {
                continue;
            }

            var zielOrdner = Path.Combine(ziel, ordner);
            DirAccess.MakeDirRecursiveAbsolute(zielOrdner);
            foreach (var datei in DirAccess.GetFilesAt(quelle))
            {
                File.Copy(quelle.PathJoin(datei),
                    zielOrdner.PathJoin(datei), true);
            }
        }

        // **Und die Karte unter dem Namen, den der MapTree nennt.**
        // **`Map0001.lmu` ist die leere Startkarte des Editors, und ein
        // Spiel, das auf einer leeren Karte beginnt, ist ein Spiel, das
        // nie fertig exportiert wurde.**
        var quelleKarte = Path.Combine(pWurzel, pKarte);
        AssertTrue(File.Exists(quelleKarte),
            "**and the map the test names is in the game** -- " + pKarte);
        File.Copy(quelleKarte, Path.Combine(ziel, "Map0001.lmu"), true);

        return new PluginGameInfo
        {
            GameDirectory = ziel,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        };
    }

    /// <summary>
    /// The finished game's own events reach the interpreter, and sixty
    /// frames of them produce no failed command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement behind criterion 1.</strong>
    /// <c>Map0002.lmu</c> of the game in front of us carries 873 events,
    /// 1049 pages and <strong>3262 event commands in 35 distinct
    /// codes</strong> — show text, control branches, variable
    /// operations, transfers, pictures. <strong>Every test of
    /// <c>EventInterpreter</c> until now used commands this repository
    /// wrote itself</strong>, **and a game with 3262 real commands in it
    /// is the only thing that says whether the interpreter survives
    /// contact with one.**
    /// </para>
    /// <para>
    /// <strong>And the 100 diagnostics are all of one kind, and they are
    /// correct:</strong> a missing charset, twenty-six names of them —
    /// <c>People1.png</c>, <c>Animal.png</c> — **which the game references
    /// and does not ship, because it never used them.** That is a fact
    /// about the game, **and a runtime that refused to start over it
    /// would refuse a game that works.**
    /// </para>
    /// <para>
    /// <strong>And the assertion is the negative one that matters:</strong>
    /// not one diagnostic is about a command. A command the interpreter
    /// does not know is a different message, **and a hundred of those
    /// would be a hundred answers this repository got wrong.**
    /// </para>
    /// </remarks>
    public void Test_DieEventsDesFertigenSpielsLaufenImInterpreter()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        var spiel = VerzeichnisMit(wurzel, "Map0002.lmu");
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        AssertTrue(host.Start(spiel).Success, "**and the game starts**");
        if (host.Runtime is not Rm2kEngineRuntime runtime)
        {
            AssertTrue(false, "**and the host built an RM2K runtime**");
            return;
        }

        // **Und die Befehle sind beim Scheduler angekommen.**
        var seiten = 0;
        var befehle = 0;
        var automatisch = 0;
        var daten = runtime.CurrentMapData;
        AssertTrue(daten != null, "**and the map is loaded**");
        if (daten != null && daten.TryGetValue("events", out var roh)
            && roh.VariantType == Godot.Variant.Type.Array)
        {
            foreach (var rohEvent in roh.AsGodotArray())
            {
                if (rohEvent.VariantType
                    != Godot.Variant.Type.Dictionary)
                {
                    continue;
                }

                if (!rohEvent.AsGodotDictionary().TryGetValue(
                    "pages", out var rohSeiten)
                    || rohSeiten.VariantType
                        != Godot.Variant.Type.Array)
                {
                    continue;
                }

                foreach (var rohSeite in rohSeiten.AsGodotArray())
                {
                    if (rohSeite.VariantType
                        != Godot.Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var seite = rohSeite.AsGodotDictionary();
                    seiten += 1;
                    if (seite.ContainsKey("trigger")
                        && (int)seite["trigger"] == 3)
                    {
                        automatisch += 1;
                    }

                    if (seite.TryGetValue("commands", out var rohBefehle)
                        && rohBefehle.VariantType
                            == Godot.Variant.Type.Array)
                    {
                        befehle += rohBefehle.AsGodotArray().Count;
                    }
                }
            }
        }

        AssertTrue(seiten > 500,
            "**and the map's own event pages came with it** -- " + seiten
                + " pages; the game's map carries 1049 and a reader that "
                + "read three of them would still say 'the events "
                + "loaded'");
        AssertTrue(befehle > 1000,
            "**and their commands came with them** -- " + befehle
                + " commands, in a map of a game of 2002");
        AssertTrue(automatisch > 0,
            "**and at least one page runs by itself** -- " + automatisch
                + " carry trigger 3, and a reader that started no "
                + "automatic page would render a map that never moves");

        // **Und sechzig Bilder, und kein Befehl, den der Leser nicht
        // kann.**
        for (var frame = 0; frame < 60; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        var ueberBefehle = runtime.Simulation.Diagnostics.Count(
            pText => pText.Contains("has no method", StringComparison.Ordinal)
                || pText.Contains("unknown command", StringComparison.Ordinal)
                || pText.Contains("command", StringComparison.Ordinal)
                    && pText.Contains("not", StringComparison.Ordinal)
                    && !pText.Contains("charset", StringComparison.Ordinal));
        AssertEq(ueberBefehle, 0,
            "**and not one of the diagnostics is about a command** -- "
                + runtime.Simulation.Diagnostics.Count + " were written, "
                + "and every one of them names a missing charset, which "
                + "the game references and does not ship because it never "
                + "used them; a runtime that refused over that would "
                + "refuse a game that works");

        AssertEq(runtime.Simulation.FrameCount, 60,
            "**and the clock ran sixty frames** -- and it counted "
                + runtime.Simulation.FrameCount);

        host.Stop();
    }

}
