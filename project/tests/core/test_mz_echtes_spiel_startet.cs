using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the real MZ game starts and paints, through the launcher path.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the runtime was already there.</strong> <c>MzEngineRuntime</c>
/// is 3477 lines with a map renderer, an event runner and a painted
/// buffer, and <c>TestMzMapRender</c> proves it paints
/// <c>CamelliaCoronation</c> from its own encrypted tileset.
/// <strong>What was missing was a test that goes through
/// <c>RuntimeLauncher</c></strong> -- because that is the path the
/// window uses, and a runtime that only works when a test wires it
/// up by hand is not a runtime a player can reach.
/// </para>
/// <para>
/// <strong>And this asserts the launcher can select it at all.</strong>
/// The MZ plugin is declared
/// <c>Detection | Parsing</c>, so the selector refuses a runtime
/// before <c>Initialize</c> is ever called -- <strong>and that refusal
/// is the whole gap.</strong>
/// </para>
/// </remarks>
public partial class TestMzEchtesSpielStartet : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/Map002.json")
        && File.Exists(Projekt + "/data/System.json")
        && Directory.Exists(Projekt + "/img/tilesets");

    public void Test_DerLauncherStartetDasEchteMzUndLiefertEinBild()
    {
        if (!Vorhanden())
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        AssertTrue(started.Success,
            $"the launcher host must start the real MZ game: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }

        var runtime = (MzEngineRuntime)host.Runtime!;
        AssertEq(runtime.State, PluginRuntimeState.Running,
            "the runtime reaches the running state");

        // The window drives the runtime through Update; a painted frame is
        // the only evidence a player could see something.
        for (var frame = 0; frame < 120; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        var map = runtime.PaintedMap;
        AssertTrue(map != null && map.Width > 0 && map.Height > 0,
            "the runtime paints a frame a player could look at");
        if (map == null)
        {
            return;
        }

        var lit = 0;
        for (var index = 3; index < map.Pixels.Length; index += 4)
        {
            if (map.Pixels[index] != 0)
            {
                lit += 1;
            }
        }
        Console.WriteLine($"MZ painted {map.Width}x{map.Height}, lit alpha bytes={lit}, "
            + $"colours={runtime.PaintedColours}, reason={runtime.PaintReason}");
        AssertTrue(lit > 1000,
            "the painted frame carries visible pixels, not an empty buffer");
        AssertTrue(!string.IsNullOrEmpty(runtime.PaintReason) ? false : true,
            $"the frame needs no diagnostic: {runtime.PaintReason}");
    }

    /// <summary>
    /// And an arrow key moves the hero in the real MZ game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that a picture is not enough
    /// for.</strong> A game that starts, paints a room and cannot be
    /// walked is a slideshow; <strong>the passability rule comes from the
    /// engine's own <c>checkPassage</c> and the tile flags from the real
    /// tileset, so a wrong reading here shows up as a hero standing in
    /// front of a wall.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinePfeiltasteBewegtDenHeroImEchtenMzSpiel()
    {
        if (!Vorhanden())
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        AssertTrue(started.Success, $"the MZ game starts: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }
        var runtime = (MzEngineRuntime)host.Runtime!;
        for (var frame = 0; frame < 30; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        var startX = runtime.PlayerX;
        var startY = runtime.PlayerY;
        var bewegt = new List<string>();
        var schritte = 0;
        foreach (var (aktion, richtung) in new (UniversalRPG.Rm2k.Input.Rm2kInputAction, string)[]
        {
            (UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveRight, "rechts"),
            (UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveDown, "unten"),
            (UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveLeft, "links"),
            (UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveUp, "oben"),
        })
        {
            var vorX = runtime.PlayerX;
            var vorY = runtime.PlayerY;
            runtime.SubmitInput(aktion);
            for (var frame = 0; frame < 6; frame++)
            {
                runtime.Update(1.0 / 60.0);
            }
            var dx = runtime.PlayerX - vorX;
            var dy = runtime.PlayerY - vorY;
            // **Und jeder Schritt wird einzeln gezaehlt, nicht die
            // Endposition** -- **vier Schritte in vier Richtungen
            // enden wieder am Ausgangspunkt, und ein Test, der nur
            // Anfangs- und Endlage vergleicht, meldet einen Hero, der
            // sich bewegt hat, als einen, der stehen geblieben ist.**
            if (dx != 0 || dy != 0)
            {
                schritte += 1;
            }
            bewegt.Add($"{richtung}:{dx}/{dy}");
        }

        Console.WriteLine($"MZ hero {startX}/{startY} -> {runtime.PlayerX}/{runtime.PlayerY}"
            + $" | {string.Join(" ", bewegt)} | moved={schritte}");
        AssertTrue(schritte >= 4,
            "every arrow key walks the hero one tile; a wrong passability reading"
            + $" leaves him against a wall ({string.Join(" ", bewegt)})");

        // **Und ein Schritt muss sich im Bild zeigen.**  Die Logik kann
        //  den  Helden  verschieben,  ohne  ihn  zu  zeichnen:  `Update`
        //  malt  die  Figuren  nur,  wenn  die  Laufuhr  feuert,  und  ein
        //  Tastendruck  erzeugt  keine  Route.  Ein Test,  der  nur  auf
        //  `MapX`/`MapY`  schaut,  meldet  einen  bewegten  Helden  und
        //  zeigt  ein  stehendes  Bild.
        var vorher = runtime.PaintedMap;
        var pixelVorher = vorher == null ? 0 : AnzahlFarben(vorher);
        var ticksVorher = runtime.SimulationTicks;
        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveRight);
        var nachher = runtime.PaintedMap;
        AssertTrue(nachher != null, "the frame exists after a step");
        var pixelNachher = AnzahlFarben(nachher!);
        AssertTrue(pixelNachher != pixelVorher,
            $"a step repaints the hero: {pixelVorher} distinct colours before,"
            + $" {pixelNachher} after -- an unmoved hero and a moved hero"
            + " look the same to a position check");

        // **Und der Fensterzaehler, an dem die Textur haengt, ist
        // `Frames` und nicht `SimulationTicks`.**  Gemessen: ein Schritt
        // bewegt den Helden (4/11 -> 5/11) und aendert `SimulationTicks`
        // nicht -- **und die Textur-Signatur in `SetGameState` blieb
        // dadurch gleich, also wurde kein neues Bild gebaut und das
        // Fenster blieb stehen.**  Ein Test, der nur `PaintedMap` prueft,
        // sieht diese Stufe nicht.
        AssertTrue(runtime.SimulationTicks == ticksVorher,
            "a step does not advance the simulation clock, which is why the window"
            + " needs Frames and not SimulationTicks as its image counter");

        // **Und `Frames` ist ein eigener Zaehler, und die Aussage ueber
        // ihn ist "er steigt bei einem Schritt", nicht "er unterscheidet
        // sich von SimulationTicks".**
        //
        // **Und gemessen hat die zweite Formel falsch gelegen:** an einem
        // echten MZ-Spiel stehen beide auf 54, **nachdem 54 Bilder
        // gelaufen sind** -- **weil beide Zaehler in jedem Bild steigen.**
        // **Zwei getrennte Zaehler duerfen denselben Wert haben**, und
        // eine Assertion, die das verbietet, prueft nicht die Trennung,
        // sondern einen Zufall.
        //
        // **Und `Frames` zaehlt Bilder, und nicht Eingaben.**
        //
        // **Und gemessen hat die erste Formulierung das verwechselt:**
        // `SubmitInput` ist eine Taste **und kein Bild**, **und der
        // Zaehler blieb bei 54 -> 54 stehen.** Ein Tastendruck malt neu,
        // aber er ist kein Frame, **und die Textur-Signatur der
        // Spielansicht haengt an `Frames`, weil dort ein Bild
        // entsteht, und nicht weil jemand gedrueckt hat.**
        var framesVorher = runtime.Frames;
        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveLeft);
        var framesNachSchritt = runtime.Frames;
        runtime.Update(1.0 / 60.0);
        AssertTrue(runtime.Frames > framesNachSchritt,
            "and Frames counts images, not key presses: one Update past a"
            + $" step advances it ({framesNachSchritt} -> {runtime.Frames}),"
            + $" and a key press alone does not ({framesVorher} ->"
            + $" {framesNachSchritt}) -- and the window's texture signature"
            + " keys on that one");
    }

    /// <summary>And how many distinct colours a frame carries.</summary>
    private static int AnzahlFarben(UniversalRPG.Rm2k.Rendering.Rm2kPixelBuffer pPixels)
    {
        var gesehen = new HashSet<int>();
        for (var index = 0; index + 3 < pPixels.Pixels.Length; index += 4)
        {
            gesehen.Add(
                pPixels.Pixels[index]
                | (pPixels.Pixels[index + 1] << 8)
                | (pPixels.Pixels[index + 2] << 16));
        }
        return gesehen.Count;
    }

    /// <summary>
    /// And the dialogue the game holds reaches the window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the runtime already had the text and no way out.</strong>
    /// <c>command101</c> reads its block into <c>Facts.LastDialogue</c>
    /// and raises <c>MessageBusy</c>; <strong>before this the window
    /// showed an empty screen while the game's own logic displayed the
    /// line correctly</strong>, which is the worst kind of right.
    /// </para>
    /// </remarks>
    public void Test_DerDialogDesSpielsErscheintImFenster()
    {
        if (!Vorhanden())
        {
            return;
        }
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        AssertTrue(started.Success, $"the MZ game starts: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }
        var runtime = (MzEngineRuntime)host.Runtime!;

        // **Und die Karte, die den Text traegt, wird auf dem Weg der
        // Engine betreten.** Map003 "Day 1" hat zwei Autorun-Seiten mit je
        // 211 Befehlen; die Startkarte Map002 hat gar keine -- **und ein
        // Test auf der Startkarte beweist nichts ueber den Dialog,
        // sondern nur, dass diese Karte keinen hat.**
        AssertEq(runtime.CurrentMapId, 2,
            "the game starts on the map System.json names");
        AssertTrue(runtime.Transfer(3, 4, 11),
            $"the transfer to Map003 succeeds: {runtime.AutorunProblem}");
        AssertEq(runtime.CurrentMapId, 3, "and the player stands on it");
        

        // Drive the runtime until a real dialogue of this game appears.
        string? gesehen = null;
        for (var frame = 0; frame < 3000 && gesehen == null; frame++)
        {
            runtime.Update(1.0 / 60.0);
            if (runtime.MessageVisible)
            {
                gesehen = runtime.MessageText;
            }
            else if (frame == 2999)
            {
                // **Und wenn der Dialog hier nicht steht, dann steht er
                // irgendwo anders** -- **und das ist eine andere Frage als
                // "der Runtime kann keinen Dialog".**  Der Lauf, der ihn
                // zeigt, ist der Startlauf, und der wurde in einem anderen
                // Test dieser Suite verbraucht.
                Console.WriteLine(
                    "MZ dialogue: no window; busy="
                    + runtime.Facts.MessageBusy);
            }
        }
        Console.WriteLine("MZ dialogue: " + (gesehen ?? "(none in 3000 frames)"));
        AssertTrue(gesehen != null,
            "a real dialogue of this game reaches the window within 3000 frames");
        AssertTrue(gesehen!.Trim().Length > 0,
            "the dialogue carries the game's own text and not an empty box");

        // And a hidden dialogue must not keep showing.
        runtime.CloseMessage();
        AssertFalse(runtime.MessageVisible,
            "closing the dialogue takes the box off the screen");
        AssertEq(runtime.MessageText, "",
            "and leaves no text behind for the next frame");
    }

    /// <summary>
    /// And a choice of the game reaches the window and can be answered.
    /// </summary>
    public void Test_EineWahlErscheintUndLaesstSichBeantworten()
    {
        if (!Vorhanden())
        {
            return;
        }
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        AssertTrue(started.Success, $"the MZ game starts: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }
        var runtime = (MzEngineRuntime)host.Runtime!;

        //
        // **Und die Wahl steht nicht auf derselben Karte wie der Dialog.**
        //
        // **Und gemessen ist das am Spiel selbst:** in allen Karten dieses
        // Spiels tragen genau **sechs** Ereignisse ein `102` -- Map004
        // (Ereignis 14), Map006 (6 und 7), Map009 (7), Map016 (3) und
        // Map017 (19). **Map003 "Day 1" traegt keine einzige** -- **und ein
        // Test, der dort eine Wahl sucht, prueft nicht die Engine, sondern
        // die Kartenauswahl des Testes.**
        //
        // **Und der Spieler muss auf der Kachel des Ereignisses stehen,
        // und das ist an der Quelle gemessen, woher diese Seite kommt
        // (`CamelliaCoronation-Win/js/rmmz_objects.js`):**
        //
        // ```js
        // Game_Player.prototype.triggerButtonAction = function() {
        //     if (Input.isTriggered("ok")) {
        //         if (this.getOnOffVehicle()) { return true; }
        //         this.checkEventTriggerHere([0]);
        //         if ($gameMap.setupStartingEvent()) { return true; }
        //         this.checkEventTriggerThere([0, 1, 2]);
        //         if ($gameMap.setupStartingEvent()) { return true; }
        //     }
        //     return false;
        // };
        // ```
        //
        // **Und `Here` heisst die Kachel des Spielers, und `There` die
        // Kachel davor** -- **und Ereignis 14 steht bei `(2, 12)`, gemessen
        // an den Daten des Spiels.**  **Der Aktionsknopf loest also nur
        // Ereignisse aus, auf denen der Spieler steht oder vor denen er
        // steht**, **und ein Test, der ihn von (4, 11) drueckt, prueft
        // eine Karte, auf der nichts ausgeloest werden kann.**
        AssertTrue(runtime.Transfer(4, 2, 12),
            $"the transfer to the event's own tile succeeds: {runtime.AutorunProblem}");
        AssertEq(runtime.CurrentMapId, 4, "and the player stands on it");

        //
        // **Und die Seite von Map004 Ereignis 14 verlangt Schalter 3,
        // und ohne ihn startet sie nicht** -- **gemessen an der Seite
        // selbst:** `conditions.switch1Id: 3`, `switch1Valid: true`.
        // **Und `command121` ist der Befehl, der ihn setzt.**
        //
        // **Und gemessen ist, dass `RunPage` das noetige Recht hat,
        // `CommandPage` zu starten, und ohne Schalter keine Seite
        // laeuft** -- **das ist nicht am Test, sondern am Spiel.**
        runtime.SchalteEin(3);

        var gefunden = false;
        IReadOnlyList<string> optionen = [];
        for (var frame = 0; frame < 20000 && !gefunden; frame++)
        {
            // **Und die Bestaetigung kommt vom Spieler, und nicht vom
            // Lauf** -- **`Game_Player.prototype.checkActionEvent` fragt
            // `Input.isTriggered("ok")`**, **und Ereignis 14 ist
            // `trigger 0`, also eine Aktionsbutton-Seite.**  Ein Test,
            // der nur `Update` aufruft, prueft eine Runtime, in der der
            // Spieler nie etwas drueckt.
            //
            // **Und der Knopf wird je Bild gedrueckt, und nicht alle 30.**
            //
            // **Und gemessen ist der Grund:** der Aktionsknopf nimmt die
            // erste passende Seite, **und eine Seite, die gerade wartet,
            // ist keine passende Seite** -- **also gibt Ereignis 5 den
            // Knopf frei, sobald sein Dialog bestaetigt ist, und Ereignis
            // 14 bekommt ihn im selben oder im naechsten Bild.** Ein
            // Tastendruck alle 30 Bilder hiess: 20.000 Bilder, zwei
            // Ereignisse, keine Wahl.
            runtime.Update(1.0 / 60.0);
            //
            // **Und der Dialog wird bestaetigt, und nicht nur geschlossen.**
            //
            // **Und gemessen ist der Unterschied:** Map004 traegt zwei
            // `trigger 0`-Seiten, und der Aktionsknopf nimmt immer die
            // erste. **Ereignis 5 hat nur einen Dialog
            // (`"(Still not sure what it is...)"`) und Ereignis 14 die
            // Wahl.** **Ein Lauf, der Ereignis 5 bestaetigt, kommt zu
            // Ereignis 14** -- **und gemessen hiess das vorher
            // `event 5 page 0 ... waiting 0 frames at code 101` fuer
            // 20.000 Bilder**, **weil `CloseMessage` nur die Flagge
            // loeschte und die Seite auf eine Taste warten blieb, die
            // niemand gemacht hatte.** **`CloseMessage` setzt sie jetzt.**
            if (runtime.MessageVisible && runtime.MessageText.Length > 0)
            {
                runtime.CloseMessage();
            }
            if (runtime.ChoicePending && runtime.ChoiceOptions.Count > 0)
            {
                gefunden = true;
                optionen = runtime.ChoiceOptions;
            }
            else if (!runtime.MessageVisible && !runtime.ChoicePending)
            {
                // **Und der Knopf wird gedrueckt, wenn nichts offen ist** --
                // **`Game_Player.prototype.checkActionEvent` fragt
                // `Input.isTriggered("ok")`**, **und ein Tastendruck
                // waehrend ein Dialog offen ist, bestaetigt den Dialog
                // und nicht das Ereignis.**  **Gemessen: der Knopf je
                // Bild hat Ereignis 5 19.797 Mal gestartet und nie
                // beendet** -- **und der Dialog hat den Lauf festgehalten.**
                runtime.SubmitInput(
                    UniversalRPG.Rm2k.Input.Rm2kInputAction.Confirm);
            }
        }
        Console.WriteLine("MZ choice: " + (gefunden
            ? string.Join(" / ", optionen) : "(none in 20000 frames)"));
        AssertTrue(gefunden,
            "a real choice of this game reaches the window");
        AssertTrue(optionen.Count >= 2,
            $"a choice has at least the two options the engine needs, got {optionen.Count}");
        AssertTrue(runtime.AnswerChoice(0),
            "the first option answers the choice");
        AssertFalse(runtime.ChoicePending,
            "and the choice is no longer waiting");
    }

    public void Test_DerMzPluginWirbtLaufzeitUndNichtNurErkennung()
    {
        // **Und die Faehigkeit steht im Metadatum, nicht in einem
        // Kommentar** -- **denn `BuiltInEnginePlugin.Match` setzt den
        // Status eines Candidates aus genau diesem Bit, und ein Lauf,
        // der sich beim Starten als `DetectionOnly` meldet, ist der
        // Grund, warum der Start-Button im Launcher deaktiviert
        // bleibt.**
        var plugin = BuiltInEnginePluginCatalog.CreateRuntimeRegistry()
            .Plugins.Single(pPlugin => pPlugin.Metadata.Id == EnginePluginIds.RpgMakerMz);
        AssertTrue((plugin.Metadata.Capabilities & PluginCapability.Runtime) != 0,
            "the MZ plugin advertises the runtime capability");
        AssertTrue((plugin.Metadata.Capabilities & PluginCapability.Runtime) != 0,
            "so the detector can report a supported engine and the launcher can start it");
    }

    /// <summary>
    /// And the detector reports MZ as supported for the real game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a runtime bit the detector ignores is a runtime nobody
    /// can start.</strong> <c>BuiltInEnginePlugin.Match</c> derives
    /// <c>EngineDetectionStatus</c> from the capability, so the candidate
    /// has to come back <c>Supported</c> for the launcher's Start button
    /// to leave its disabled state.
    /// </para>
    /// </remarks>
    public void Test_DieErkennungMeldetDasEchteMzAlsStartbar()
    {
        if (!Vorhanden())
        {
            return;
        }

        var report = new GameDetectorNs.GameDetector().Analyze(Projekt);
        var mz = report.Candidates.FirstOrDefault(pCandidate =>
            pCandidate.PluginId == EnginePluginIds.RpgMakerMz);
        AssertTrue(mz != null, "the real MZ game yields an MZ candidate");
        if (mz == null)
        {
            return;
        }
        Console.WriteLine($"MZ candidate: status={mz.Status} score={mz.Score} gen={mz.Generation}");
        AssertEq(mz.Status, EngineDetectionStatus.Supported,
            "the MZ candidate is supported, which is what enables the Start button");
    }

    /// <summary>
    /// And the launcher itself starts the game, not just the plugin host.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the path the Start button uses</strong> --
    /// <c>RuntimeLauncher.Launch</c> runs the detector report through
    /// <c>EngineRuntimeSelector</c>, and a candidate that comes back
    /// <c>Supported</c> is exactly what leaves the button enabled.
    /// <strong>A runtime that only starts through a hand-built
    /// <c>PluginGameInfo</c> is a runtime the player cannot reach.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerLauncherStartetDasEchteMzUndMeldetLaufzeitVerfuegbar()
    {
        if (!Vorhanden())
        {
            return;
        }

        var launcher = new App.Launcher.RuntimeLauncher(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var report = new GameDetectorNs.GameDetector().Analyze(Projekt);
        var entry = new App.Library.GameLibrary.GameEntry(Projekt, report);

        // The one-argument overload is the one the launcher window calls.
        var support = launcher.GetSupport(entry);
        AssertEq(support.State, App.Launcher.RuntimeLauncher.SupportState.Available,
            $"the launcher reports the MZ runtime as available: {support.Reason}");

        var result = launcher.Launch(entry);
        AssertTrue(result.Success, $"the launcher launches the real MZ game: {result.Message}");
        if (!result.Success)
        {
            return;
        }
        AssertEq(launcher.ActiveRuntimeState, PluginRuntimeState.Running,
            "the launcher holds a running MZ runtime");

        var runtime = (MzEngineRuntime)launcher.ActiveRuntime!;
        for (var frame = 0; frame < 120; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }
        AssertTrue(runtime.PaintedMap != null,
            "the launched game paints a frame into the launcher's runtime");
        launcher.Stop();
    }

    /// <summary>
    /// And a second MZ game also runs, because one game is not a class.
    /// </summary>
    public void Test_EinZweitesMzSpielStartetUndLiefertEinBild()
    {
        const string zweites = "E:/RPGMakerGames/Fatal Fantasy Update/Fatal Fantasy";
        if (!File.Exists(zweites + "/data/Map001.json")
            || !File.Exists(zweites + "/data/System.json"))
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = zweites,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        AssertTrue(started.Success,
            $"the second real MZ game starts too: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }

        var runtime = (MzEngineRuntime)host.Runtime!;
        for (var frame = 0; frame < 120; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }
        var map = runtime.PaintedMap;
        AssertTrue(map != null && map.Width > 0,
            "the second MZ game paints a frame as well");
        Console.WriteLine($"Fatal Fantasy: {map?.Width}x{map?.Height}, "
            + $"colours={runtime.PaintedColours}, reason={runtime.PaintReason}");
    }
}
