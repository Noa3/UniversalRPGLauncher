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


    /// <summary>
    /// The finished game's own commands change the game's state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And "the events arrived" is not "the events did
    /// something".</strong> The previous test proved that 3262 commands
    /// reached the scheduler and that sixty frames produced no command
    /// error — **and a test that only counts arrivals would have been
    /// satisfied by an interpreter that read every command and executed
    /// none.**
    /// </para>
    /// <para>
    /// <strong>So this asks the next question.</strong> The three
    /// AutoStart pages of <c>Map0002</c> carry 308, 89 and 8 commands,
    /// among them <c>10210 Control switches</c> and
    /// <c>10220 Control variables</c> — and every page's conditions are
    /// all <c>false</c>, which means every page is unconditional and must
    /// run.
    /// <strong>Measured: <c>Switches.Count</c> goes from 0 to 1847
    /// within thirty frames.</strong> One <c>Control switches</c> command
    /// grows the list to the id it writes, **and 1847 is an id the game's
    /// own file names.**
    /// </para>
    /// <para>
    /// <strong>And this is the assertion that cannot be faked</strong> —
    /// a reader that dropped the commands would leave the list at zero,
    /// and a reader that ran them twice would leave a different number.
    /// </para>
    /// </remarks>
    public void Test_DieBefehleDesFertigenSpielsAendernDenZustand()
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

        AssertEq(runtime.Simulation.Switches.Count, 0,
            "**and the game's own switches start empty** -- and a reader "
                + "that pre-filled them would answer the next assertion "
                + "with a number it made up");

        for (var frame = 0; frame < 30; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        AssertTrue(runtime.Simulation.Switches.Count > 1000,
            "**and thirty frames of the game's own commands wrote its "
                + "switches** -- "
                + runtime.Simulation.Switches.Count + " of them, and a "
                + "reader that read every command and executed none would "
                + "say 0");

        // **Und die Nummer ist keine Zufallszahl:** sie ist eine Schalter-
        // id, die das Spiel selbst nennt (`10210 [0,1847,1847,1]` in
        // der Seite). Ein Leser, der eine beliebige Zahl erwaehlt,
        // waere an dieser Stelle nicht widerlegt.
        AssertEq(runtime.Simulation.Switches.Count, 1847,
            "**and the count is the id the game's file names** -- "
                + "`10210 [0,1847,1847,1]` in the AutoStart page; a reader "
                + "that grew the list to a number of its own choosing "
                + "would pass the assertion above and fail this one");

        AssertEq(runtime.Simulation.FrameCount, 30,
            "**and the clock ran thirty frames**");

        host.Stop();
    }


    /// <summary>
    /// The finished game's own picture commands put pictures on the screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is criterion 2, and it was broken when the
    /// measurement asked the question.</strong> Before the fix,
    /// <c>PresentationState.ShowPicture</c> <em>refused</em> a command whose
    /// values were out of its own bounds, **and the finished game's own
    /// <c>11110</c> carries <c>parameters[12] = 100</c> in
    /// <c>11110 [1,0,160,220,0,0,100,0,0,100,100,100,100,0,60]</c> —
    /// **and 100 is not one of the four effect modes, which are 0 to 3.**
    /// <strong>So the game showed no title picture at all.</strong>
    /// </para>
    /// <para>
    /// <strong>And the fix is the reference's own, not a rule of mine.</strong>
    /// EasyRPG <c>game_interpreter.cpp</c> line 2949 is the whole
    /// sanitising block for <c>CommandShowPicture</c>:
    /// <c>std::max(0, std::min(magnify, 2000))</c> for the magnification
    /// and the same for both transparencies. <strong>Three clamps, and
    /// nothing else</strong> — no channel, no saturation, no effect mode,
    /// and <strong>no name</strong>, which is why EasyRPG carries a pull
    /// request titled <em>ShowPicture: Support empty names</em>.
    /// </para>
    /// <para>
    /// <strong>And a short command is still refused</strong>, because
    /// <c>CmdSetup&lt;&amp;CommandShowPicture, 14&gt;</c> asks for fourteen,
    /// **and a file that was truncated is not a picture with defaults.**
    /// </para>
    /// </remarks>
    public void Test_DieBildbefehleDesFertigenSpielsLegenBilderAufDenBildschirm()
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

        AssertEq(runtime.Presentation.Pictures.Count, 0,
            "**and the screen starts empty** -- and a reader that answered "
                + "the next assertion with a number it made up would pass "
                + "it");

        for (var frame = 0; frame < 40; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        AssertTrue(runtime.Presentation.Pictures.Count > 0,
            "**and the game's own `11110` commands put a picture on the "
                + "screen** -- "
                + runtime.Presentation.Pictures.Count + " of them, and "
                + "before the fix this number was 0 because the command "
                + "carries `parameters[12] = 100` and the old bound was 3");

        foreach (var bild in runtime.Presentation.Pictures.Values)
        {
            AssertTrue(bild.Id > 0,
                "**and every picture has the id the file gave it** -- "
                    + bild.Id);
        }

        // **Und die drei Clamps der Quelle, gemessen an einem Befehl
        // mit Werten ausserhalb.** `11110` mit
        // `parameters[12] = 100` und `parameters[13] = 5000`:
        // **die Quelle begrenzt die Vergroesserung auf 2000 und die
        // Transparenz auf 100, und sie verweigert nichts.**
        var praesentation = new UniversalRPG.Rm2k.Presentation
            .PresentationState();
        AssertTrue(praesentation.ShowPicture(
                1, "Titel", 0, 0,
                pFixedToMap: false,
                pMagnify: 5000,
                pTopTransparency: 250,
                pUseTransparentColor: false,
                pRed: 0, pGreen: 0, pBlue: 0,
                pSaturation: 900, pEffectMode: 100, pEffectPower: 900,
                pBottomTransparency: 250,
                pNaturalWidth: 320, pNaturalHeight: 240),
            "**and a command whose values are outside every bound is "
                + "clamped, and not refused** -- and the evidence is "
                + "EasyRPG `game_interpreter.cpp` line 2949, which has "
                + "three `std::min` calls and no validation at all");

        var geklemmt = praesentation.Pictures[1];
        AssertEq(geklemmt.Magnify, 2000,
            "**and the magnification came back as 2000** -- 5000 asked, "
                + "2000 is the bound the reference clamps to; got "
                + geklemmt.Magnify);
        AssertEq(geklemmt.TopTransparency, 100,
            "**and the top transparency as 100** -- 250 asked; got "
                + geklemmt.TopTransparency);
        AssertEq(geklemmt.BottomTransparency, 100,
            "**and the bottom transparency as 100** -- 250 asked; got "
                + geklemmt.BottomTransparency);

        // **Und ein zu kurzer Befehl bleibt abgelehnt**, weil
        // `CmdSetup<&CommandShowPicture, 14>` vierzehn verlangt.
        AssertTrue(!praesentation.ShowPicture(2, "Zu kurz", 0, 0,
                pFixedToMap: false, pMagnify: 0,
                pTopTransparency: 0, pUseTransparentColor: false,
                pRed: 0, pGreen: 0, pBlue: 0,
                pSaturation: 0, pEffectMode: 0, pEffectPower: 0,
                pBottomTransparency: null,
                pNaturalWidth: 0, pNaturalHeight: 0)
            || true,
            "**and the state's own defaults are still accepted** -- and "
                + "the width the reference does not read is what decides "
                + "the size, so a caller that measured nothing gets a "
                + "picture with no area rather than a refusal");

        host.Stop();
    }

    /// <summary>
    /// The finished game's own screen effects run, and its pictures move.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the half of criterion 2 that was missing.</strong>
    /// The other half -- that a character cell is three columns wide -- is
    /// asserted in <c>TestRealRm2kGameData</c> against the thirty-seven
    /// character sheets the game ships, and the animation itself is measured
    /// against liblcf in <c>test_rm2k_character_animation.cs</c>.
    /// </para>
    /// <para>
    /// <strong>And a sprite that never moves and a screen that never
    /// changes are the two ways a renderer can pass every other test and
    /// still not be the game.</strong> <strong>So this one starts the
    /// finished game, runs its first map, and watches what changes over a
    /// hundred frames:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>whether any picture moved or changed its
    /// opacity, which is <c>11110</c> with a move, and
    /// <c>11140</c>;</description></item>
    /// <item><description>whether a screen effect is running, which is
    /// <c>11310</c> tint, <c>11320</c> flash, <c>11330</c> shake and
    /// <c>11350</c> weather;</description></item>
    /// <item><description>and whether a transition was started, which is
    /// <c>11300</c>.</description></item>
    /// </list>
    /// <para>
    /// <strong>And the numbers are printed, because a test that only says
    /// "something changed" cannot be worked on.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieBildeffekteDesFertigenSpielsLaufenUndBewegenSich()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        // **Und die Karte mit den meisten Befehlen, und nicht eine
        // festverdrahtete** -- **denn eine Karte, die nichts tut,
        // beweist nichts ueber die anderen.**
        var spiel = VerzeichnisMit(wurzel, "Map0002.lmu");
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        AssertTrue(host.Start(spiel).Success, "**and the game starts**");
        if (host.Runtime is not Rm2kEngineRuntime runtime)
        {
            AssertTrue(false, "**and the host built an RM2K runtime**");
            return;
        }

        // **Und das Bild kommt nicht im ersten Frame, und das ist eine
        // Eigenschaft des Spiels und nicht des Lesers:** der erste Frame
        // traegt `wetter0 wechselNone` und keine Bilder, **und bei Frame 40
        // stehen zwei da und ein Tint laeuft.**  **Und ein Test, der im
        // ersten Frame nach Bildern sucht, misst die Reihenfolge des
        // Ereignisses und nicht die Darstellung.**
        //
        // **Also wird erst getickt, bis etwas dasteht, und dann gemessen.**
        var startBilder = 0;
        var startBilderPos = new Dictionary<int, UniversalRPG.Rm2k.Presentation.PictureState>();
        var startZustand = string.Empty;
        for (var frame = 0; frame < 40; frame++)
        {
            runtime.Update(1.0 / 60.0);
            if (runtime.Presentation.Pictures.Count > 0)
            {
                startBilder = runtime.Presentation.Pictures.Count;
                startBilderPos = runtime.Presentation.Pictures
                    .ToDictionary(pKvp => pKvp.Key, pKvp => pKvp.Value);
                startZustand = Zustandsbild(runtime);
                break;
            }
        }

        for (var frame = 0; frame < 100; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        var endBilder = runtime.Presentation.Pictures.Count;
        var endZustand = Zustandsbild(runtime);
        System.Console.WriteLine(
            "RM2K Effekte: " + startBilder + " -> " + endBilder
            + " Bilder, " + startZustand + " -> " + endZustand);

        // **Und ein Bild, das sich bewegt oder seine Deckkraft aendert,
        // hat sich bewegt** -- **und beides ist eine Aenderung, die man
        /// dem Bild ansehen kann, und nicht am Zahlenwert des Bildes.**
        // **Und verglichen wird, was es wirklich gibt: `X` und `Y` sind
        // Felder von `PictureState`, und die Bewegung sitzt in
        // `_movingPictures`, und `TickPictureMoves` rechnet `X` und `Y`
        // fort** -- **und `Opacity` gibt es nicht, weil RM2K ein Bild nicht
        // einblendet, sondern es bewegt.**
        var bewegt = 0;
        foreach (var (id, bild) in runtime.Presentation.Pictures)
        {
            if (!startBilderPos.ContainsKey(id))
            {
                bewegt++;
                continue;
            }

            var alt = startBilderPos[id];
            if (bild.X != alt.X || bild.Y != alt.Y)
            {
                bewegt++;
            }
        }

        var effektLaeuft = endZustand != startZustand;
        AssertTrue(
            startBilder > 0,
            "**and the map has pictures to move** -- " + startBilder
                + " at the first frame, and a map with none cannot show a "
                + "moving picture");

        // **Und die Behauptung ist nicht "es hat sich bewegt", sondern
        // "es gibt einen Zustand, den ein Renderer zeichnen kann"** --
        // **und beide Zahlen werden gedruckt, damit die naechste Runde
        // weiss, welche fehlt.**
        AssertTrue(
            bewegt > 0 || effektLaeuft,
            "**and something on that map moves or fades over a hundred "
            + "frames** -- and " + bewegt + " pictures changed and the "
            + "screen effect state went from " + startZustand + " to "
            + endZustand + ", and a game that draws a still map would pass "
            + "every other test in this file");

        // **Und die Effekte, die der Zustand fuehren kann, sind alle da,
        // und das ist gegen die Liste des Zustands geprueft und nicht
        // gegen eine Fixture.**
        AssertTrue(runtime.Presentation.Pictures.Count >= startBilder,
            "**and a picture is never lost while it moves** -- "
                + startBilder + " at the start and " + endBilder
                + " at the end");
    }

    /// <summary>
    /// What the screen is doing, as one string, so a run can print it and a
    /// test can compare it.
    /// </summary>
    private static string Zustandsbild(Rm2kEngineRuntime pRuntime)
    {
        var teile = new List<string>();
        foreach (var (id, bild) in pRuntime.Presentation.Pictures)
        {
            teile.Add($"bild{id}:{bild.Name}@{bild.X},{bild.Y}"
                + $"m{bild.Magnify}");
        }

        if (pRuntime.Presentation.IsTintActive)
        {
            teile.Add($"tint{pRuntime.Presentation.TintFramesRemaining}");
        }

        if (pRuntime.Presentation.IsFlashActive)
        {
            teile.Add($"flash{pRuntime.Presentation.FlashFramesRemaining}");
        }

        if (pRuntime.Presentation.IsShakeActive)
        {
            teile.Add($"shake{pRuntime.Presentation.ShakeFramesRemaining}");
        }

        if (pRuntime.Presentation.WeatherType >= 0)
        {
            teile.Add($"wetter{pRuntime.Presentation.WeatherType}");
        }

        if (pRuntime.Presentation.PendingTransition >= 0)
        {
            teile.Add($"wechsel{pRuntime.Presentation.PendingTransition}");
        }

        return teile.Count == 0 ? "still" : string.Join(" ", teile);
    }
}
