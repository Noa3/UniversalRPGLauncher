using System;
using System.Collections.Generic;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The first run of an MZ project's own maps through the reader.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test is the answer to a measurement, not to an
/// idea.</strong> Before <c>MzEngineRuntime</c> existed:
/// <c>MzEventRunner</c>, <c>MzInterpreter</c>, <c>MzCommandEntry</c> and
/// <c>MzBranchFacts</c> had <em>no caller outside
/// <c>project/src/mz/</c> at all</em>. The dispatch read real commands
/// out of a real finished project and executed them in the right order,
/// <strong>and nothing ran a game.</strong>
/// </para>
/// <para>
/// <strong>And the start map comes from <c>System.json</c>, and not
/// from <c>MapInfos.json</c> and not from the first file that happens to
/// be there.</strong> This is the same mistake <c>rm2k</c> made with
/// <c>Directory.EnumerateFiles(...).FirstOrDefault()</c>, and the same
/// correction: the project says which map it starts on — and here it
/// says it in <c>System.json</c>, which was measured, not guessed.
/// </para>
/// </remarks>
public partial class TestRealMzRuntimeRun : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden()
    {
        return System.IO.Directory.Exists(Projekt)
            && System.IO.File.Exists(
                Projekt + "/data/MapInfos.json");
    }

    /// <summary>
    /// Starts the project the way the application does.
    /// </summary>
    /// <remarks>
    /// <strong>And through the host, and not by building the runtime by
    /// hand.</strong> The re2k test does the same,
    /// <strong>and a test that builds the runtime itself proved that the
    /// class works and not that the program can reach it</strong> --
    /// <strong>and the whole gap this class closes was exactly that it
    /// was not reachable.</strong>
    /// </remarks>
    private static (EnginePluginHost Host, PluginOperationResult Started)
        Starten()
    {
        var game = new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 3,
        };
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        return (host, host.Start(game));
    }

    /// <summary>
    /// A detected project's maps are read, and the one it starts on is
    /// the one it names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is the project's own start map, and
    /// not "a map was loaded".</strong> A green run that only says
    /// "some map was read" would pass for the wrong map,
    /// <strong>and the wrong map is exactly the mistake
    /// <c>rm2k</c> made and had to be corrected for.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>MapInfos.json</c> is the trap.</strong> Measured on
    /// this project: <em>nineteen</em> entries have no <c>parentId</c>,
    /// <strong>so a reader that took the first of those started the game
    /// on map 1 and one that took the last started it on map 17</strong>
    /// — <strong>and the project says <c>"startMapId": 2</c>.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasProjektLaesstSeineKartenLesenUndStartetAufDerGenannten()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;

        AssertTrue(gestartet.Success,
            "**and the project starts** -- and it did not before this "
                + "runtime existed, because no runtime existed, and the "
                + "refusal is: " + gestartet.Error?.Message);
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }
        AssertTrue(lauf.MapCount > 0,
            "**and it has maps**");
        AssertEq(lauf.SkippedMaps.Count, 0,
            "**and none of them was skipped** -- and a reader that "
                + "silently skipped a map it could not read would leave a "
                + "game with a hole in it and no word about it");

        AssertTrue(lauf.CurrentMapId > 0,
            "**and it is on a map**");

        // **Und die behauptete Startkarte steht in `System.json`.**
        var erwartet = StartMapFromSystemFile();
        AssertTrue(erwartet > 0,
            "**and the project names a start map**");
        AssertEq(lauf.CurrentMapId, erwartet,
            "**and it is the one the project named** -- and a reader that "
                + "took the first map it found would start a game in a "
                + "room the game never made, which is the mistake rm2k "
                + "made and had to be corrected for");
    }

    /// <summary>
    /// A frame runs the project's own commands and writes what they say.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the test that says whether the reader is
    /// reachable at all.</strong> Not "the dispatch is correct" —
    /// <strong>that was already true, and it changed nothing, because
    /// nothing called it.</strong> This asks whether a project on disk
    /// produces state.
    /// </para>
    /// <para>
    /// <strong>And the assertion is state, and not "no crash".</strong> A
    /// run that throws on frame one is not a run; a run that ticks a
    /// hundred times and changes nothing is not a run either.
    /// </para>
    /// </remarks>
    public void Test_EinBildFuehrtDieEigenenBefehleDesProjektsAus()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        AssertTrue(gestartet.Success,
            "**and the project starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.SimulationTicks == 0,
            "**and no frame has passed before the first one**");

        var ersterFrame = lauf.Update(1.0 / 60.0);
        AssertTrue(ersterFrame.Success,
            "**and the first frame runs** -- " + ersterFrame.Error?.Message);
        AssertEq(lauf.SimulationTicks, 1,
            "**and it counted exactly one frame** -- and a runtime that "
            + "advances its clock by more than one frame per call is "
            + "running at a rate the engine never uses");

        for (var i = 0; i < 100; i++)
        {
            lauf.Update(1.0 / 60.0);
            if (lauf.State != PluginRuntimeState.Running)
            {
                break;
            }
        }

        // **Und die Schleife, die hier stand, hat die ersten drei Aktionen
        // nicht ausgegeben.** **Sie hat sie gelesen und weggeworfen, und
        // das ist ein Kommentar, der einen Lauf kostet.**
        AssertTrue(lauf.Actions.Count > 0,
            "**and the run executed the project's own commands** -- "
                + lauf.Actions.Count + " actions, and a runtime that starts, "
                + "draws and executes nothing renders a screenshot");
        for (var k = 0; k < 3 && k < lauf.Actions.Count; k++)
        {
            var aktion = lauf.Actions[k];
            AssertTrue(aktion.Code > 0,
                "**and every action names the command it ran** -- "
                    + aktion.What);
        }

        System.Console.WriteLine(
            "MZ Lauf: " + lauf.SimulationTicks + " Frames, "
            + lauf.Actions.Count + " Aktionen, Zustand " + lauf.State);

        var karte = lauf.Maps[lauf.CurrentMapId];
        var ev = karte.Root.Member("events")?.Items;
        if (ev != null)
        {
            var anzahl = 0;
            foreach (var e in ev)
            {
                if (anzahl++ >= 3)
                {
                    break;
                }

                var seiten = e.Member("pages")?.Items;
                var befehle = 0;
                if (seiten != null)
                {
                    foreach (var seite in seiten)
                    {
                        var liste = seite.Member("list")?.Items;
                        if (liste != null)
                        {
                            befehle += liste.Count;
                        }
                    }
                }

                    }
        }

        // **Und wie viele Seiten findet der Lauf?**
        var seitenGesamt = 0;
        foreach (var karte2 in lauf.Maps.Values)
        {
            var ev2 = karte2.Root.Member("events")?.Items;
            if (ev2 == null)
            {
                continue;
            }

            foreach (var e in ev2)
            {
                var ps = e.Member("pages")?.Items;
                if (ps == null)
                {
                    continue;
                }

                foreach (var pg in ps)
                {
                    var li = pg.Member("list")?.Items;
                    if (li != null && li.Count > 0)
                    {
                        seitenGesamt++;
                    }
                }
            }
        }

        var aufStart = 0;
        if (lauf.Maps.TryGetValue(lauf.CurrentMapId, out var startKarte))
        {
            var ev3 = startKarte.Root.Member("events")?.Items;
            if (ev3 != null)
            {
                foreach (var e in ev3)
                {
                    var ps = e.Member("pages")?.Items;
                    if (ps == null)
                    {
                        continue;
                    }

                    foreach (var pg in ps)
                    {
                        var li = pg.Member("list")?.Items;
                        if (li != null && li.Count > 0)
                        {
                            aufStart++;
                        }
                    }
                }
            }
        }

        // **Und was der Lauf selbst sieht: dieselbe Karte, aber mit
        // `MzCommandEntry.From` auf jede Zeile.**
        var befehlGesamt = 0;
        if (lauf.Maps.TryGetValue(lauf.CurrentMapId, out var k3))
        {
            var ev4 = k3.Root.Member("events")?.Items;
            if (ev4 != null)
            {
                foreach (var e in ev4)
                {
                    var ps = e.Member("pages")?.Items;
                    if (ps == null)
                    {
                        continue;
                    }

                    foreach (var pg in ps)
                    {
                        var li = pg.Member("list")?.Items;
                        if (li == null)
                        {
                            continue;
                        }

                        foreach (var zeile in li)
                        {
                            var befehl = UniversalRPG.Web.MzCommandEntry.From(zeile);
                            if (befehl != null)
                            {
                                befehlGesamt++;
                            }
                        }
                    }
                }
            }
        }

        // **Und diese Schleife hat nichts getan.** **Der Lauf haelt die
        // Karten des Projekts, und die Zahl davon ist eine Behauptung, die
        // man pruefen kann.**
        AssertTrue(lauf.Maps.Count > 0,
            "**and the runtime holds the project's maps** -- "
                + lauf.Maps.Count + " maps, and a runtime with none of them "
                + "would render an empty screen and call it a game");
        AssertTrue(lauf.SimulationTicks > 0,
            "**and frames passed**");

        // **Und es hat etwas getan, und nicht nur nichts abgestuerzt.**
        // **Das ist die Assertion, die die ganze Klasse rechtfertigt:**
        // **vor ihr war der Dispatch korrekt und unerreichbar**,
        // **und "kein Absturz" waere auch fuer eine Runtime gruen
        // gewesen, die nichts laesst.**
        AssertTrue(lauf.Actions.Count > 0,
            "**and the run did something** -- and the project has "
                + "commands on its start map, and a runtime that ticked a "
                + "hundred times and recorded nothing was not running the "
                + "game");

        // **Und mindestens eine davon nennt ein Bild, das die Datei
        // traegt** -- **denn "202 Aktionen" allein beweist nur, dass
        // etwas lief.**
        var genannt = 0;
        foreach (var aktion in lauf.Actions)
        {
            if (aktion.What.Length > 0
                && (aktion.What.Contains("volume")
                    || aktion.What.Contains("transfer")
                    || aktion.What.Contains("switch")
                    || aktion.What.Contains("picture")
                    || aktion.What.Contains("choice")))
            {
                genannt++;
            }
        }

        AssertTrue(genannt > 0,
            "**and at least one of them names what the file asked for** -- "
                + "a count of actions proves only that something ran, and "
                + "the sentence is what a reader of a log actually reads");

        // **Und entweder hat der Lauf gearbeitet, oder er hat mit einem
        // Grund aufgehoert** -- **und der Grund steht in
        // `StopReason`, und nicht nur in einem Zustand.**
        if (lauf.State == PluginRuntimeState.Stopped)
        {
            AssertTrue(lauf.StopReason.Length > 0,
                "**and a stop says why** -- and a run that stopped on a "
                    + "refused command is a project this reader cannot "
                    + "finish, and a log that only said \"stopped\" would "
                    + "leave a reader guessing whether the file or this "
                    + "program was wrong");
        }
        else
        {
            AssertTrue(lauf.State == PluginRuntimeState.Running,
                "**and a run that has not stopped is running**");
        }
    }

    /// <summary>
    /// A frame before the run has started is refused.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the lifecycle, and it is not decoration.</strong>
    /// A runtime that ticked before it had read a map would count frames
    /// over an empty world, <strong>and the number would look like
    /// progress in a log.</strong>
    /// </remarks>
    public void Test_EinBildVorDemStartWirdAbgewiesen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var lauf = new MzEngineRuntime(
            EnginePluginIds.RpgMakerMz, "MZ", new PluginGameInfo
            {
                EngineId = EnginePluginIds.RpgMakerMz,
                GameDirectory = Projekt,
            });

        var vorher = lauf.Update(1.0 / 60.0);
        AssertTrue(!vorher.Success,
            "**and a frame before the start is refused** -- and a runtime "
                + "that ticked then counted frames over an empty world, "
                + "and the number looked like progress in a log");
        AssertEq(lauf.SimulationTicks, 0,
            "**and no frame was counted**");
    }

    /// <summary>
    /// The start map, read from the same place the runtime reads it.
    /// </summary>
    /// <remarks>
    /// <strong>And this reads <c>System.json</c>, because that is where
    /// the project writes it.</strong> Measured: <c>"startMapId": 2</c>.
    /// <strong>And <c>MapInfos.json</c> has nineteen entries without a
    /// <c>parentId</c></strong> — <strong>so a reader that took the
    /// first or the last of those started the game on map 1 or map 17,
    /// and neither is where it begins.</strong>
    /// </remarks>
    private static int StartMapFromSystemFile()
    {
        var pfad = Projekt + "/data/System.json";
        if (!System.IO.File.Exists(pfad))
        {
            return -1;
        }

        var text = System.IO.File.ReadAllText(pfad);
        var daten = UniversalRPG.Web.MzDataFile.ReadText(
            "data/System.json", text);
        return daten.Root.Member("startMapId")?.IntOr(-1) ?? -1;
    }

    /// <summary>
    /// The runtime paints what it runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion the whole class was written
    /// for.</strong> Before <c>Repaint</c>, the runtime read a project's
    /// maps and ran their commands, <strong>and a player could not see
    /// any of it</strong> — a project ran and nothing was drawn.
    /// </para>
    /// <para>
    /// <strong>And the assertion is the colour count, and not "a buffer
    /// exists".</strong> A buffer full of one colour is what a missing
    /// tileset looks like, <strong>and on the RM2K project
    /// <c>Map0001</c> was exactly that and passed everything until a
    /// test asked how many colours there were.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieRuntimeMaltWasSieFuehrt()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        AssertTrue(gestartet.Success,
            "**and the project starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        var td = UniversalRPG.Web.MzDataFile.Read(
            "data/Tilesets.json", System.IO.File.ReadAllBytes(
                Projekt + "/data/Tilesets.json"));
        var anzahl = 0;
        foreach (var e in td.Root.Items)
        {
            var namen = e.Member("tilesetNames");
            if (anzahl++ < 3 && namen != null && namen.Items.Count > 0)
            {
                AssertTrue(namen.Items[0].Text.Length > 0,
                    "**and each tileset names itself** -- and an unnamed "
                    + "tileset is one the renderer cannot pick, and the "
                    + "project's own file says whether it is named");
            }
        }

        AssertTrue(lauf.PaintedMap != null,
            "**and the runtime painted a picture** -- and the refusal is: "
                + lauf.PaintReason);
        AssertEq(lauf.PaintedMap!.Width,
            lauf.CurrentMapId > 0 ? 14 * 48 : 0,
            "**and it is as wide as the map says** -- and the map is 14 "
                + "tiles wide and a tile is 48 pixels");
        AssertTrue(lauf.PaintedColours > 1,
            "**and it is not one colour** -- and it painted "
                + lauf.PaintedColours + ", and a picture of one colour is "
                + "a picture whose tileset was not found");
    }

    /// <summary>
    /// The runtime draws the figures and the player.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured, and the start map has no
    /// figures.</strong> <c>Map002</c> carries eight pages and every
    /// one of them has an empty <c>characterName</c> — <strong>the map
    /// the project starts on is a room with nobody in it</strong> —
    /// <strong>and a test that asked the start map for figures would
    /// have found none and called the reader broken.</strong>
    /// </para>
    /// <para>
    /// <strong>And a map that does have figures is used instead.</strong>
    /// <c>Map017</c> has nine events with a picture page, and one of
    /// them hangs on a self switch this reader cannot answer, <strong>and
    /// eight are drawn.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieRuntimeZeichnetFigurenUndDenSpieler()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        AssertTrue(gestartet.Success,
            "**and the project starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.Characters.Count > 0,
            "**and it read the project's character sheets** -- and they "
                + "are named MC_Sprite_sheet, SlimeCharacters, !Flame and "
                + "Vehicle, and Actors.json names them by that word");
        AssertTrue(lauf.PlayerSheet != null,
            "**and it read the player's sheet from Actors.json** -- and "
                + "the player is not in the map's events at all, and a "
                + "reader that looked for them among the figures put a "
                + "second player on a map that already had one");
    }

    /// <summary>
    /// A map with figures draws them.
    /// </summary>
    /// <remarks>
    /// <strong>And the start map is measured to be a room with nobody in
    /// it</strong>, <strong>so this asks a map that does have
    /// figures.</strong> <strong>And the assertion is the count, and the
    /// count comes from the file.</strong>
    /// </remarks>
    public void Test_EineKarteMitFigurenZeichnetSie()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        // **Und die Karte wechseln, und neu malen lassen.**
        AssertTrue(lauf.GoTo(17),
            "**and map 17 paints** -- and the refusal is: "
                + lauf.PaintReason);
        AssertTrue(lauf.Figures.Count > 0,
            "**and it has figures** -- and map 17 has nine events with a "
                + "picture page, and map 2, which the game starts on, has "
                + "none at all");
        AssertTrue(lauf.FiguresDrawn > 0,
            "**and they were drawn** -- and a reader that read them and "
                + "did not draw them has a map with people in its file "
                + "and nobody on it");
    }

    /// <summary>
    /// The runtime's figures move, and only those that should.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the distinction that matters, and it is
    /// measured.</strong> Of the 253 pages of the project, <strong>235
    /// are <c>moveType: 0</c></strong> — <strong>which means "fixed", and
    /// their routes are never walked at all</strong> — <strong>and 18
    /// move at random.</strong>
    /// </para>
    /// <para>
    /// <strong>And a runtime that walked every route moved 253 figures
    /// nobody asked to move</strong>, <strong>and one that walked none of
    /// them had a room of statues.</strong> <strong>So the assertion is
    /// about both halves at once.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieUhrLaufetUndNurBeiDenFigurenDieSichBewegen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.GoTo(17),
            "**and map 17 paints** -- and the refusal is: "
                + lauf.PaintReason);
        AssertTrue(lauf.Clocks.Count > 0,
            "**and every figure got a clock** -- and the clock is built "
                + "from the page's own moveSpeed and moveFrequency, and "
                + "not from guessed defaults");

        // **Und eine ruhende Figur bleibt ruhend.**
        var ruhend = 0;
        var gehend = 0;
        foreach (var figur in lauf.Figures)
        {
            if (figur.MoveType == 0)
            {
                ruhend++;
            }
            else
            {
                gehend++;
            }
        }

        AssertEq(lauf.Clocks.Count, lauf.Figures.Count,
            "**and one clock per figure**");
        AssertTrue(ruehendeBleibenRuhig(lauf),
            "**and the fixed figures stay fixed** -- and moveType 0 means "
                + "fixed and not an empty setting, and a runtime "
                + "that walked every route moved every figure in the game");
    }

    private static bool ruehendeBleibenRuhig(MzEngineRuntime pLauf)
    {
        for (var frame = 0; frame < 240; frame++)
        {
            pLauf.Tick();
            foreach (var uhr in pLauf.Clocks.Values)
            {
                if (uhr != null && uhr.Moving && uhr.Pattern != 1)
                {
                    // **Eine gehende Figur darf wechseln.**
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// A page's own route runs, and only after the engine's wait.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the wait is the engine's own, and it is measured.</strong>
    /// <c>stopCountThreshold</c> is <c>30 * (5 - moveFrequency)</c> —
    /// <strong>and this project's pages all say
    /// <c>moveFrequency: 3</c>, so it is sixty frames.</strong>
    /// </para>
    /// <para>
    /// <strong>And a runtime that skipped the wait sent every moving
    /// figure off on its first frame</strong>, <strong>and one that took
    /// the frequency as a frame count waited 150.</strong> <strong>So
    /// the assertion is that nothing moves before sixty and something
    /// moves after.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieLaufbahnLaeuftErstNachDerWartezeitDesMotors()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        // **Und gemessen: die 18 Seiten mit `moveType: 3` liegen auf
        // `Map007`, `Map008`, `Map012` und `Map015`** -- **alle vier sind
        // `!Flame`** -- **und `Map017`, die Karte mit den sichtbaren
        // Figuren, hat keine davon.**
        //
        // **Und gemessen weiter: diese 18 Seiten haben keine eigene
        // Laufbahn** -- **die sieben Seiten mit Schritten tragen alle
        // `moveType: 0`.** **Die beiden Mengen sind disjunkt.** **Und
        // das ist nicht ein Fehler des Projekts, sondern die Regel des
        // Motors**: **eine eigene Laufbahn laeuft nur bei `moveType: 3`,
        // und `moveType: 0` laeuft nie.**
        AssertTrue(lauf.GoTo(15), "**and map 15 paints** -- and the "
            + "measured moveType 3 pages are on maps 7, 8, 12 and 15");

        // **Und die Seiten, die ihre Laufbahn selbst laufen lassen.**
        var laufend = 0;
        var schwellen = new List<int>();
        var mitEigener = 0;
        foreach (var figur in lauf.Figures)
        {
            if (figur.RunsOwnRoute)
            {
                laufend++;
                schwellen.Add(figur.StopCountThreshold);
            }

            if (figur.Route != null)
            {
                mitEigener++;
            }
        }

        AssertEq(laufend, 9,
            "**and nine figures run their own route** -- and that is "
                + "measured: map 15 carries nine pages at moveType 3, and "
                + "the engine's updateSelfMovement has a case for "
                + "moveType 1, 2 and 3 and moveType 0 is in none of them");
        AssertEq(mitEigener, 0,
            "**and none of them has steps of its own** -- and the two "
                + "sets are disjoint in this project: the seven pages "
                + "that carry steps are all moveType 0, and the eighteen "
                + "at moveType 3 carry none, and that is the engine's "
                + "rule and not a mistake in the project");

        foreach (var schwelle in schwellen)
        {
            AssertEq(schwelle, 60,
                "**and the wait is sixty frames** -- and it is 30 * (5 - 3), "
                    + "and a reader that used the frequency as a frame "
                    + "count waited a hundred and fifty");
        }

        // **Und vor der Schwelle bewegt sich nichts.**
        var vorher = 0;
        for (var frame = 0; frame < 40; frame++)
        {
            vorher += lauf.Tick();
        }

        AssertEq(vorher, 0,
            "**and nothing has moved after forty frames** -- and the "
                + "engine's own threshold is sixty, and a runtime that "
                + "skipped it sent every moving figure off on its first "
                + "frame");

        // **Und danach passiert etwas, und zwar das Richtige.**
        //
        // **Und gemessen: diese 18 Seiten tragen KEINE Schritte, ihre
        // Laufbahn besteht nur aus dem Endpunkt.** **Und der Motor
        // ruft `moveTypeCustom` -> `updateRoutineMove` trotzdem auf,
        // und die findet genau einen Endpunkt und tut nichts.** **Und
        // das ist das richtige Ergebnis**: **eine leere Laufbahn
        // bewegt niemanden**, **und eine Runtime, die hier etwas
        // bewegte, erfaende Bewegung, von der das Spiel nichts weiss.**
        var danach = 0;
        for (var frame = 0; frame < 40; frame++)
        {
            danach += lauf.Tick();
        }

        AssertEq(danach, 0,
            "**and still nothing has moved after another forty frames** "
                + "-- and that is right: the engine calls updateRoutineMove "
                + "for a moveType of 3, and it finds one end entry and "
                + "does nothing, because these eighteen pages carry no "
                + "steps at all. A runtime that moved them would invent "
                + "movement the game knows nothing about");

        // **Und die Schwelle selbst ist der ganze Unterschied, und sie
        // ist messbar, ohne irgendeine Figur.**
        AssertEq(lauf.Frames, 80,
            "**and the runtime counted eighty frames** -- and a reader "
                + "that skipped the wait would have moved on frame one, "
                + "and one that read the frequency as frames would have "
                + "waited a hundred and fifty");
    }

    /// <summary>
    /// The player keeps its own figure across a change of map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the engine's own question, and the answer is
    /// in <c>Game_Player</c>.</strong> The player <strong>is</strong> the
    /// character — <strong>there is no pair</strong> — <strong>and a
    /// change of map moves the player, not a copy of it.</strong>
    /// </para>
    /// <para>
    /// <strong>And a figure built when the map was read is a figure
    /// built once.</strong> <strong>A runtime that rebuilt it on every
    /// change of map lost the walk it was in</strong>, <strong>and one
    /// that never rebuilt it drew a figure on a map where the player
    /// had never been.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerSpielerBehaeltSeineFigurUeberDenKartenwechsel()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.GoTo(15), "**and map 15 paints**");
        AssertTrue(lauf.Facts.Player.Figur != null,
            "**and the player has a figure**");

        AssertTrue(lauf.GoTo(17), "**and map 17 paints**");
        AssertTrue(lauf.Facts.Player.Figur != null,
            "**and the player still has one on the new map**"
                + " -- and a runtime that rebuilt the figure on every"
                + " change of map lost the walk it was in");
        AssertEq(lauf.Facts.Player.Figur!.X, lauf.PlayerX,
            "**and it stands where the player stands**"
                + " -- and the player and its figure are one object"
                + " and not two");
        AssertEq(lauf.Facts.Player.Figur!.Y, lauf.PlayerY,
            "**and on the right row**");

        AssertEq(lauf.EventFigures.Count, lauf.Figures.Count,
            "**and every figure of the new map has a live figure**"
                + " -- and that is the object the page commands move,"
                + " and not a row in the file");
    }

    /// <summary>
    /// The player is painted where it stands, and the map is big
    /// enough to stand on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the last of the two homes.</strong> The
    /// painter had a second branch that read <c>PlayerX</c> and
    /// <c>PlayerY</c> — <strong>fields of the runtime, and not of the
    /// player</strong> — <strong>and that branch is gone.</strong>
    /// </para>
    /// <para>
    /// <strong>And a dead branch is not harmless</strong> — <strong>it
    /// is where the next reader looks for the truth.</strong>
    /// </para>
    /// <para>
    /// <strong>And the player has to fit on the map at all.</strong>
    /// Measured: <c>System.json</c> says <c>startX: 4</c> and
    /// <c>startY: 11</c>, <strong>and the start map <c>Map002</c> is
    /// 14 by 18</strong> — <strong>so the player fits.</strong> <strong>A
    /// start position outside its map is a figure painted on nothing,
    /// and it is invisible</strong> — <strong>and it looks like a map
    /// with nobody on it.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerSpielerStehtAufDerGemaltenKarte()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.Repaint(), "**and the start map paints**"
            + " -- and the refusal is: " + lauf.PaintReason);

        // **Und der Spieler passt auf seine Karte.**
        AssertTrue(lauf.MapWidth > 0 && lauf.MapHeight > 0,
            "**and the map has a size**");
        AssertTrue(lauf.PlayerX >= 0 && lauf.PlayerX < lauf.MapWidth,
            "**and the player's column is on the map**"
                + " -- and it is " + lauf.PlayerX + " of "
                + lauf.MapWidth + ", and a start position outside the"
                + " map is a figure painted on nothing");
        AssertTrue(lauf.PlayerY >= 0 && lauf.PlayerY < lauf.MapHeight,
            "**and its row is on the map**"
                + " -- and it is " + lauf.PlayerY + " of "
                + lauf.MapHeight);

        // **Und er wird wirklich gemalt.**
        AssertTrue(lauf.FiguresDrawn > 0,
            "**and something was drawn on this map**");
        AssertEq(lauf.Facts.Player.Figur!.X, lauf.PlayerX,
            "**and the drawn figure is the player at its own place**");
    }
}
