using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Godot;

using UniversalRPG.Plugins;
using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A finished RPG Maker MV game on this machine, started through the host,
/// the way the application starts it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And before this test MV had no runtime at all.</strong> Its
/// plugin detected the engine, read <c>System.json</c> and counted maps,
/// <strong>and stopped there</strong> -- <strong>while MZ, on the very same
/// command set, had one.</strong>
/// </para>
/// <para>
/// <strong>And the reason MV was left out was a belief that turned out to be
/// wrong.</strong> The two engines were treated as two runtimes.
/// <strong>MV's hundred and twelve commands are all among MZ's hundred and
/// fourteen</strong>, <strong>measured against the engine's own
/// <c>rpg_objects.js</c> and not against a table written beside either of
/// them</strong> -- <strong>and so the one runtime serves both.</strong>
/// </para>
/// <para>
/// <strong>And the other difference is the layout.</strong> MZ keeps its data
/// in <c>data/</c> and MV in <c>www/data/</c>, <strong>and a runtime that
/// only knew the first would read an MV project and find nothing in
/// it.</strong>
/// </para>
/// </remarks>
public partial class TestRealMvRuntimeRun : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    private static bool Vorhanden()
    {
        var ok = Directory.Exists(Projekt + "/www/data")
            || Directory.Exists(Projekt + "/data");
        if (!ok)
        {
            GD.Print(
                "    (skipped: no MV project at " + Projekt + " -- MV had no "
                + "runtime and this test is what says whether it now has "
                + "one, so a run that checked nothing would prove nothing)");
        }

        return ok;
    }

    /// <summary>
    /// The MV project starts through the host.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And through the host, and not by building the runtime by
    /// hand.</strong> <strong>A test that constructs the runtime itself proves
    /// that a class works and not that the program can reach it</strong> --
    /// and for MV the reachability is the whole question, because the plugin
    /// had no <c>CreateRuntime</c> at all until this test's commit.
    /// </para>
    /// </remarks>
    public void Test_DasMvProjektStartetDurchDenHost()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        AssertTrue(gestartet.Success,
            "**and an MV project starts** -- " + gestartet.Error?.Message
                + ", and before this the plugin had no CreateRuntime at all, "
                + "so there was nothing to start");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built a runtime for it** -- and "
                + "it built " + (host.Runtime?.GetType().Name ?? "nothing"));
            return;
        }

        AssertTrue(lauf.MapCount > 0,
            "**and it has maps** -- " + lauf.MapCount + " read, and MV keeps "
                + "its data under www/data where a runtime that only knew "
                + "data/ would find nothing");
        AssertEq(lauf.SkippedMaps.Count, 0,
            "**and none of them was skipped** -- " + lauf.SkippedMaps.Count
                + " skipped, and a reader that quietly skipped a map leaves "
                + "a game with a hole in it and no word about it");

        // **Und ein genauer Wert und nicht "mehr als null"** -- **denn
        // "81 Karten" war gemessen, und die Zahl war falsch.**
        //
        // **Und `data/` enthaelt neben seinen 62 Dateien vier
        // `GameLanguage`-Pakete mit je fuenf Karten**: **das sind 20
        // Kopien echter Karten mit uebersetzten Texten und ohne Feld
        // `id`**, **und `MapIdOf` faellt auf den Dateinamen zurueck**,
        // **wenn `id` fehlt** -- **und `GameLanguage0/Map003.json` gibt
        // ihm 003 und damit genau die Karte, die `data/Map003.json`
        // auch beansprucht.** **Und alle 20 Paare haben verschiedene
        // `events`.**
        //
        // **Und `IsMap` sah nur auf den Dateinamen**, **also hat
        // `GameLanguage2/Map003.json` die Karte 3 ueberschrieben**, **und
        // `MapCount` zaehlte beide.**
        //
        // **Und das ist derselbe Fehler wie bei `data/VN/`**, **und er
        // ist die zweite Fassung derselben Sache:** **eine Karte, die
        // nicht unter `data/` liegt, ist keine Karte dieses Spiels.**
        AssertEq(lauf.MapCount, 61,
            "**and sixty-one maps, and not eighty-one** -- "
            + lauf.MapCount + " read, and the twenty extra ones were the "
            + "language packs' copies of maps 3, 7, 18, 19 and 20, each "
            + "of which overwrote the real map of that number");
        AssertTrue(lauf.CurrentMapId > 0, "**and it is on a map**");
        AssertEq(lauf.CurrentMapId, StartMapAusSystem(),
            "**and it is the one the project's own System.json names** -- "
                + "and MV writes startMapId there, and a runtime that took "
                + "the first map it found would start the game in a room the "
                + "game never made");
    }

    /// <summary>
    /// Frames run, and the project's own commands are executed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the part that says whether the runtime does
    /// anything.</strong> A runtime that starts, draws and executes nothing
    /// renders a screenshot, <strong>and a screenshot is not a game.</strong>
    /// </para>
    /// <para>
    /// <strong>And the assertion is on executed actions, not on frames.</strong>
    /// Frames pass on their own; <strong>actions only exist when the
    /// project's own command lists were read and run.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerLaufFuehrtDieEigenenBefehleDesProjektsAus()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        AssertTrue(gestartet.Success, "**and the project starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built a runtime**");
            return;
        }

        AssertEq(lauf.SimulationTicks, 0,
            "**and no frame has passed before the first one**");
        var erster = lauf.Update(1.0 / 60.0);
        AssertTrue(erster.Success,
            "**and the first frame runs** -- " + erster.Error?.Message);
        AssertEq(lauf.SimulationTicks, 1,
            "**and it counted exactly one frame**");

        // **Und hier steht der Unterschied zwischen den beiden Spielen, und
        // er ist nicht die Engine, sondern das Spiel:**
        //
        // ```text
        // CamelliaCoronation  Startkarte hat eine Autorun-Seite
        // sister/www         Map002, 10x10, zwei Events, 179 Befehle,
        //                    und KEINER einziger Autorun
        // ```
        //
        // **Und 179 Befehle, die auf eine Beruehrung warten, sind kein
        // Fehler des Lesers.** **Der Spieler muss auf ein Event treten
        // oder es mit dem Aktionenknoopf ausloesen** -- **und genau das ist
        // der Weg, den der Lauf hier geht, ueber `StartMode.Touched`.**
        //
        // **Und das ist der Unterschied zwischen "das Spiel startet" und
        // "das Spiel tut etwas":** **MZ startet eine Seite von allein, MV
        // nicht, und ein Test, der beide gleich behandelt, misst bei einem
        // von beiden das Falsche.**
        // **Und gemessen an der Startkarte dieses Spiels:**
        //
        // ```text
        // Event 1 Seite 1: trigger=3,  49 Befehle
        // Event 2 Seite 1: trigger=0, 117 Befehle
        // Event 2 Seite 2: trigger=0,  13 Befehle, selfSwitchCh A gueltig
        // ```
        //
        // **Und `Passt` sagt: `2` ist Autorun, `0` ist der Aktionenknoopf,
        // `1` ist Beruehrung, und `3` startet nie ueber `RunPage`.** **Also
        // ist der Weg, den dieses Spiel beim Start nimmt, der
        // Aktionenknoopf** -- **und nicht die Beruehrung, und nicht der
        // Autorun.**
        //
        // **Und die Seitenschleife laeuft von hinten nach vorn**, **also
        // zuerst Seite 2, und die verlangt den Selbstschalter A** -- **und
        // der ist nicht gesetzt**, **und die Bedingung wird mit einem
        // `return` beantwortet, das den ganzen Lauf beendet.**
        //
        // **Das ist die eigentliche Beobachtung dieses Tests: nicht
        // "MV laeuft nicht", sondern "MV verlangt, dass der Spieler zuerst
        // handelt".**
        // **Und der Weg, den dieses Spiel beim Start nimmt, ist der
        // Aktionenknoopf** -- **und nicht die Beruehrung und nicht der
        // Autorun.** Gemessen an `Map002`:
        //
        // ```text
        // Event 2 Seite 1: trigger=0, selfSwitchValid=false, 117 Befehle
        // Event 2 Seite 2: trigger=0, selfSwitchValid=true,   13 Befehle
        // ```
        //
        // **Und Seite 2 wird zuerst gesehen und uebersprungen, weil ihr
        // Selbstschalter A am Anfang nicht gesetzt ist, und Seite 1 hat
        // keine Bedingung.** **Das ist die Regel der Engine:**
        //
        // ```text
        // findProperPageIndex() {
        //     for (let i = pages.length - 1; i >= 0; i--) {
        //         if (this.meetsConditions(page)) { return i; }
        //     }
        //     return -1;
        // }
        // ```
        //
        // **Und `Update` ist der Weg, den die Anwendung nimmt**, **und er
        // laeuft ueber `PageOf`, das jede Seite der Karte gibt** -- **und
        // nicht ueber `RunPage`, das eine Seite einmal startet.** **Das
        // ist der Unterschied zwischen "ein Ereignis wird ausgeloest" und
        // "das Spiel laeuft", und der Test misst den zweiten Fall.**
        for (var i = 0; i < 600 && lauf.State == PluginRuntimeState.Running; i++)
        {
            lauf.Update(1.0 / 60.0);
        }

        AssertTrue(lauf.Actions.Count > 0,
            "**and the run executed the project's own commands** -- "
                + lauf.Actions.Count + " actions, and a runtime that executes "
                + "nothing renders a screenshot and calls it a game");
        for (var k = 0; k < 3 && k < lauf.Actions.Count; k++)
        {
            AssertTrue(lauf.Actions[k].Code > 0,
                "**and every action names the command it ran** -- "
                    + lauf.Actions[k].What);
        }

        // **Und der Lauf endet hier an einer Grenze, die keine ist, sondern
        // eine Grenze des Projekts** -- **und sie ist gemessen und nicht
        // geraten:**
        //
        // ```text
        // 111 Conditional Branch
        //   parameters[0] = 12        <- "Script" als Bedingungstyp
        //   parameters[1] = "!Utils.isMobileDevice()"
        // ```
        //
        // **Bedingungstyp 12 ist in MZ und MV gleich: die Bedingung ist ein
        // Ausdruck im JavaScript des Projekts.** **Und `AGENTS.md` sagt, dass
        // kein JavaScript eines importierten Spiels ausgefuehrt wird** --
        // **also wird die Bedingung nicht geraten und nicht ausgefuehrt,
        // sondern der Lauf sagt, woran er steht.**
        //
        // **Und das ist der Unterschied zwischen "das Spiel laeuft nicht"
        // und "das Spiel verlangt, dass sein eigenes JavaScript laeuft":
        // vor diesem Befehl hat der Lauf ausgefuehrt, was ausfuehrbar war.**
        // **Und die Grenze, die hier stand, ist keine mehr** -- **und sie
        // stand nicht dort, wo ich sie hingeschrieben hatte.**
        //
        // ```text
        // vorher:  1 Frames, 2 Aktionen
        //          Grund=a branch on the author's own script
        //          !Utils.isMobileDevice()          <- beantwortbar, war aber nicht der Stopp
        //
        // nachher: 1 Frames, 13 Aktionen
        //          Grund=the author's own script, and this repository runs no JavaScript
        // ```
        //
        // **Und der Stopp war `!localStorage.getItem("hasShownSteamLink")`
        // in der parallelen Seite von Ereignis 1** -- **und das ist eine
        // Speicherfrage des Browsers und keine Spielmechanik.** **Und
        // dahinter steht `$gameMap.event(2).start()`, also laeuft der
        // Lauf jetzt in die Seite mit den 117 Befehlen hinein.**
        // **Und der Stopp hat sich dreimal geaendert, und keiner der
        // drei war ein Umbau:**
        //
        // ```text
        // 1 Frames,  2 Aktionen  a branch on the author's own script
        //                   !Utils.isMobileDevice()          <- war
        //                       beantwortbar und war nicht der Stopp
        //
        // 1 Frames, 13 Aktionen  the author's own script
        //                   !localStorage.getItem(...)        <- Browser-
        //                       speicher, keine Spielmechanik
        //
        // 2 Frames, 27 Aktionen  a dialogue is not shown
        //                   !ConfigManager.isJapanesePlatform <- Plattform
        //
        // 601 Frames, 8414 Aktionen  Finished
        //                   der Lauf laeuft durch
        // ```
        //
        // **Und der vierte Stopp war der schlimmste von allen**, **weil
        // er die Engine falsch gelesen hat** -- **und `command101` gibt
        // ohne Ausnahme `false` zurueck:**
        //
        // ```js
        // command101() {
        //     if (!$gameMessage.isBusy()) {
        //         ... this.setWaitMode('message');
        //     }
        //     return false;
        // }
        // ```
        //
        // **Und `executeCommand` sagt `if (!this[methodName]()) { return
        // false; }`** -- **und das ist die einzige Stelle der Engine,
        // die "warte" bedeutet** -- **und mein Leser hat daraus eine
        // Ablehnung gemacht**, **und damit jedes Spiel an seinem ersten
        // Dialog angehalten, der auf einen anderen folgt.** **Und der
        // Grund stand in der Nachricht, die er selbst schrieb.**
        AssertEq(lauf.Stopped, MzStep.Finished,
            "**and the run finishes** -- and it stopped at "
            + lauf.Stopped + " after " + lauf.Actions.Count
            + " actions and " + lauf.SimulationTicks + " frames");
        var zaehlung = new System.Collections.Generic.Dictionary<int, int>();
        foreach (var a in lauf.Actions)
        {
            zaehlung[a.Code] = zaehlung.TryGetValue(a.Code, out var n) ? n + 1 : 1;
        }

        var verteilung = new System.Text.StringBuilder();
        foreach (var kv in System.Linq.Enumerable.OrderBy(zaehlung, x => x.Key))
        {
            verteilung.Append(kv.Key).Append('x').Append(kv.Value).Append(' ');
        }

        Console.WriteLine($"MV state: stopped={lauf.Stopped} "
            + $"reason='{lauf.StopReason}' pages={lauf.PagesRun} "
            + $"last={lauf.LastPage} frames={lauf.SimulationTicks}");
        Console.WriteLine("MV first actions: "
            + string.Join(" | ", System.Linq.Enumerable.Take(
                System.Linq.Enumerable.Select(lauf.Actions, a => a.Code + ": " + a.What), 4)));
        Console.WriteLine($"MV actions: {lauf.Actions.Count} over "
            + $"{lauf.SimulationTicks} frames: {verteilung}");

        // **Und die Seite, die hier laeuft, ist die eigene Autorun-Seite
        // dieser Startkarte, und das ist gemessen.**
        //
        // **Denn Map002 dieses Spiels traegt genau eine Seite mit
        // `trigger 3`** -- Ereignis 1 auf `(0,9)`, 49 Befehle, mit 4x`355`
        // und 9x`356` als Plugin-Befehlen -- **und `pages=1 last=1`.
        // Vorher lief hier eine andere Seite**, **weil die Nummern im
        // Leser um eins verschoben waren und `trigger 2` als Autorun
        // galten** -- **und die Zahl dieses Tests, 8414 Aktionen, war die
        // Zahl jener Seite.**
        AssertEq(lauf.PagesRun, 1,
            "**and exactly one page ran** -- the start map's own"
            + " autorun page, and it is event 1");

        // **Und sie laeuft bis zu einer Bedingung, die dieses Spiel
        // selbst schreibt, und die der Lauf nicht raet.**
        //
        // **Gemessen: der erste Befehl ist
        // `111 branch ScriptNotRun: needs ConfigManager.isImouto &&
        // !ConfigManager.isJapanesePlatform, which this game's plugins
        // also write from game state, so it is not a fact about the
        // machine`.**
        //
        // **Und `ConfigManager` ist ein Plugin dieses Spiels**, und die
        // beiden Werte darin schreibt das Spiel selbst -- **also sind sie
        // kein Fakt ueber den Rechner, und `AGENTS.md` verbietet, das
        // JavaScript eines importierten Spiels auszufuehren.** **Der Lauf
        // sagt darum, woran er steht, statt zu raten** -- **und das ist
        // der Unterschied zwischen "das Spiel laeuft nicht" und "das
        // Spiel verlangt, dass sein eigenes JavaScript laeuft".**
        AssertTrue(lauf.Actions.Count > 0,
            "**and the page ran commands of its own** -- "
            + lauf.Actions.Count + " of them, over "
            + lauf.SimulationTicks + " frames: " + verteilung);
        AssertTrue(lauf.Actions[0].Code == 111
                && lauf.Actions[0].What.Contains("ScriptNotRun"),
            "**and it stops where the game's own script condition is** "
            + "-- and that is measured, not guessed: the condition is "
            + "ConfigManager.isImouto && "
            + "!ConfigManager.isJapanesePlatform, and ConfigManager is a "
            + "plugin of this game, so the value is game state and not a "
            + "fact about the machine. It said: "
            + lauf.Actions[0].What);
        AssertTrue(lauf.StopReason.Length == 0,
            "**and there is nothing left to report** -- and it is '"
            + lauf.StopReason + "', and a refusal is one thing and"
            + " a wait the other, and this reader was making"
            + " the first out of the second");

        System.Console.WriteLine(
            "MV Lauf: " + lauf.SimulationTicks + " Frames, "
            + lauf.Actions.Count + " Aktionen, "
            + lauf.MapCount + " Karten, Zustand " + lauf.State
            + ", Stopped=" + lauf.Stopped
            + ", Grund=" + lauf.StopReason
            + ", Seiten=" + lauf.PagesRun
            + ", Ereignis=" + lauf.LastPage);
        // **Und jeder Stopp wird einzeln genannt** -- **denn ein
        // Grund, der abgeschnitten wird, ist kein Grund.**
        System.Console.WriteLine("MV Stops: " + lauf.Stops.Count);
        foreach (var stop in lauf.Stops)
        {
            System.Console.WriteLine("   Stop: " + stop);
        }

        // **Und die Seiten, die gelaufen sind.**
        foreach (var aktion in lauf.Actions)
        {
            System.Console.WriteLine(
                "   " + aktion.Code + "|" + aktion.What);
        }

        foreach (var aktion in lauf.Actions)
        {
            System.Console.WriteLine("   Aktion: " + aktion.What);
        }
    }

    /// <summary>
    /// This game's own common events are read, and one of them runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the second most used command in this game's
    /// 133484 map commands: 1848 calls to 49 different common
    /// events.</strong> <strong>And every one of the 1848 names a slot
    /// the project's own <c>CommonEvents.json</c> has content in</strong>
    /// -- <strong>500 of its 501 slots do, and none of the 49 is
    /// empty.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>command117</c> is four lines</strong>:
    /// <c>const commonEvent = $dataCommonEvents[this._params[0]];</c>,
    /// then <c>this.setupChild(commonEvent.list, this.isOnCurrentMap() ?
    /// this._eventId : 0)</c>, then <c>return true</c>. <strong>And
    /// <c>MzEventRunner.Run</c> is that, plus the depth cap and a
    /// refusal when the slot is empty.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGemeinsamenEreignisseDiesesSpielsSindGelesenUndEinesLaeuft()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        AssertTrue(gestartet.Success, "**and the project starts** -- "
            + gestartet.Error?.Message);
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and it is an MZ-shaped runtime**");
            return;
        }

        AssertEq(lauf.CommonEventProblem, "",
            "**and the common events were read and not skipped** -- and the "
            + "runtime says '" + lauf.CommonEventProblem + "', and 117 "
            + "without them would refuse at every one of the 1848 calls");
        AssertTrue(lauf.CommonEventCount >= 500,
            "**and it has five hundred of them** -- " + lauf.CommonEventCount
            + " read, and the file has 501 slots of which 500 have content");

        var ziel = ErstesGemeinsamesEreignis();
        AssertTrue(ziel > 0,
            "**and this game's maps call a common event** -- they do, 1848 "
            + "times, and a test that picked its own target would prove "
            + "nothing about this game");
        if (ziel <= 0)
        {
            return;
        }

        var liste = GemeinsamesEreignis(ziel);
        AssertTrue(liste.Count > 0,
            "**and slot " + ziel + " has commands in it** -- " + liste.Count
            + ", and $dataCommonEvents[" + ziel + "] returning nothing is "
            + "the one case the engine steps over and this repository "
            + "refuses");
        AssertTrue(Keine117(liste, ziel),
            "**and it does not call itself** -- and none of this game's 49 "
            + "does, and one that did would need the depth cap");

        // **Und jetzt laeuft es** -- **durch `MzEventRunner`, denn der
        // Interpreter allein kennt keine gemeinsamen Ereignisse.**
        var tabelle = new Dictionary<int, List<MzCommandEntry>>
        {
            [ziel] = liste,
        };
        // **Und der Aufruf ist selbst eine Befehlsliste**, **denn so
        // schreibt es die Engine** -- **`{"code":117,"indent":0,
        // "parameters":[85]}` gefolgt von `{"code":0,...}`**.
        MzJson.TryParse(
            "[{\"code\":117,\"indent\":0,\"parameters\":[" + ziel + "]},"
            + "{\"code\":0,\"indent\":0,\"parameters\":[]}]",
            out var aufrufWert, out var aufrufFehler);
        AssertEq(aufrufFehler, "",
            "**and the call is a command list the way the engine writes "
            + "it**");
        var aufruf = new List<MzCommandEntry>();
        foreach (var zeile in aufrufWert.Items)
        {
            aufruf.Add(MzCommandEntry.From(zeile));
        }
        var runner = new MzEventRunner(tabelle);
        var erg = runner.Run(aufruf, new MzBranchFacts());
        AssertTrue(erg.Actions.Count > 1,
            "**and the call runs it** -- and it came to "
            + erg.Actions.Count + " actions, and a call that "
            + "returns true and runs nothing is what this repository had "
            + "for 117");
        AssertTrue(erg.Actions.Count > 0,
            "**and the child says what it did** -- " + erg.Actions.Count
            + " actions, and a step that does work and reports nothing "
            + "leaves a caller unable to tell a call from a pass");
        AssertTrue(erg.Stopped != MzStep.Refused,
            "**and it did not refuse** -- and it came to " + erg.Stopped
            + ", and the reason is '" + erg.Reason + "'");
    }

    /// <summary>
    /// This project's map files, wherever this project keeps its data.
    /// </summary>
    /// <remarks>
    /// <strong>And both paths, because MV writes <c>www/data/</c> when the
    /// game folder is the project root and <c>data/</c> when the game folder
    /// already is <c>www</c>.</strong> <strong>And a test that only knew the
    /// first would have read zero maps here and called it "this game calls
    /// no common event".</strong>
    /// </remarks>
    private static IEnumerable<string> KartenDateien()
    {
        foreach (var ordner in new[]
        {
            Projekt + "/www/data",
            Projekt + "/data",
        })
        {
            if (!Directory.Exists(ordner))
            {
                continue;
            }

            foreach (var pfad in Directory.GetFiles(ordner, "Map*.json"))
            {
                yield return pfad;
            }
        }
    }

    /// <summary>
    /// The commands of one slot of this project's own CommonEvents.json.
    /// </summary>
    private static List<MzCommandEntry> GemeinsamesEreignis(int pIndex)
    {
        var liste = new List<MzCommandEntry>();
        foreach (var pfad in new[]
        {
            Projekt + "/www/data/CommonEvents.json",
            Projekt + "/data/CommonEvents.json",
        })
        {
            if (!File.Exists(pfad))
            {
                continue;
            }

            MzJson.TryParse(
                File.ReadAllText(pfad, System.Text.Encoding.UTF8),
                out var dokument, out var fehler);
            if (fehler.Length > 0
                || dokument.Kind != MzKind.Array
                || pIndex < 0
                || pIndex >= dokument.Items.Count)
            {
                continue;
            }

            var befehle = dokument.Items[pIndex].Member("list");
            if (befehle == null)
            {
                return liste;
            }

            foreach (var zeile in befehle.Items ?? new List<MzValue>())
            {
                liste.Add(MzCommandEntry.From(zeile));
            }

            return liste;
        }

        return liste;
    }

    /// <summary>
    /// Whether a common event calls itself, which would need the depth cap.
    /// </summary>
    private static bool Keine117(List<MzCommandEntry> pListe, int pIndex)
    {
        foreach (var zeile in pListe)
        {
            if (zeile.Code == MzCommandTable.CommonEvent
                && MzCommands.At(zeile, 0) == pIndex)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The first common event a map on this project's disk calls.
    /// </summary>
    private static int ErstesGemeinsamesEreignis()
    {
        // **Und die Karten liegen unter `data/`, nicht `www/data/`** --
        // **und dieser Unterschied hat schon einmal eine ganze Messung
        // leer laufen lassen.** **Und die Datei, aus der der Host das
        // Projekt liest, nennt beides.**
        foreach (var pfad in KartenDateien())
        {
            // **Und eine Karte ist ein Objekt mit `events`, und kein
            // Array** -- **und meine erste Fassung dieses Hilfs hat ein
            // Array erwartet**, **und deshalb null Karten gelesen und
            // gemeldet, dieses Spiel rufe nie ein gemeinsames Ereignis.**
            MzJson.TryParse(
                File.ReadAllText(pfad, System.Text.Encoding.UTF8),
                out var karte, out var fehler);
            if (fehler.Length > 0 || karte.Kind != MzKind.Object)
            {
                continue;
            }

            foreach (var zeile in (karte.Member("events")?.Items
                ?? new List<MzValue>()))
            {
                foreach (var seiten in (zeile.Member("pages")?.Items
                    ?? new List<MzValue>()))
                {
                    foreach (var befehl in (seiten.Member("list")?.Items
                        ?? new List<MzValue>()))
                    {
                        if (befehl.Member("code")?.IntOr(-1)
                            != MzCommandTable.CommonEvent)
                        {
                            continue;
                        }

                        var ziel = befehl.Member("parameters")?.Items;
                        if (ziel != null && ziel.Count > 0
                            && ziel[0].IntOr(0) > 0)
                        {
                            return ziel[0].IntOr(0);
                        }
                    }
                }
            }
        }

        return 0;
    }

    // ---------------------------------------------------------------------

    private static (EnginePluginHost Host, PluginOperationResult Started)
        Starten()
    {
        var game = new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMv,
            Generation = "mv",
            DetectorScore = 3,
        };
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        return (host, host.Start(game));
    }

    /// <summary>
    /// The start map the project's own file names.
    /// </summary>
    /// <remarks>
    /// <strong>And this reads <c>www/data/System.json</c>, which is MV's
    /// path</strong> -- <strong>and MZ's path is <c>data/</c>, and a test that
    /// only knew MZ's would report zero here and read it as "the project
    /// names no start map".</strong>
    /// </summary>
    private static int StartMapAusSystem()
    {
        foreach (var pfad in new[]
        {
            Projekt + "/www/data/System.json",
            Projekt + "/data/System.json",
        })
        {
            if (!File.Exists(pfad))
            {
                continue;
            }

            using var dokument = System.Text.Json.JsonDocument.Parse(
                File.ReadAllText(pfad));
            if (dokument.RootElement.TryGetProperty("startMapId", out var id)
                && id.TryGetInt32(out var wert))
            {
                return wert;
            }
        }

        return 0;
    }
}