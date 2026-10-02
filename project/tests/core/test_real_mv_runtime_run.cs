using System;
using System.IO;
using System.Linq;

using Godot;

using UniversalRPG.Plugins;
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
        var begruendung = lauf.RunPage(
            MzEngineRuntime.StartMode.ActionButton);
        System.Console.WriteLine("MV RunPage: " + begruendung);
        AssertTrue(begruendung.Length > 0,
            "**and asking for a page gives an answer** -- and an answer is a "
                + "sentence saying why nothing ran, which is what the engine "
                + "does and what silence does not");

        for (var i = 0; i < 120 && lauf.State == PluginRuntimeState.Running; i++)
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

        System.Console.WriteLine(
            "MV Lauf: " + lauf.SimulationTicks + " Frames, "
            + lauf.Actions.Count + " Aktionen, "
            + lauf.MapCount + " Karten, Zustand " + lauf.State);
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