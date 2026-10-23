using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What a real XP game needs before it can draw anything.
/// </summary>
/// <remarks>
/// <strong>And this is a measurement, not a rendering test.</strong> The RGSS layer has the Ruby
/// runtime, the data host, the map reader and the script host -- and no graphics at all: there is
/// no <c>Bitmap</c>, no <c>Sprite</c>, no <c>Tilemap</c>, no <c>Viewport</c> and no <c>Plane</c>
/// anywhere in <c>project/src/rgss</c>. The XP runtime declares <c>Runtime</c> and not
/// <c>Rendering</c>, which is why the launcher says Experimental and leaves Start disabled.
///
/// <strong>So the first step is to ask the game what it wants</strong>, rather than guessing an
/// API surface and implementing it: this test starts a real game and records what its scripts
/// report as missing, in their own words and by name. The numbers it prints are the plan for the
/// rendering work, and they are the progress measure for it -- a problem that disappears here is
/// a step towards a first frame.
/// </remarks>
public partial class TestRealXpRuntimeRun : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    private static bool Vorhanden() => Directory.Exists(Spiel);

    public void Test_RealXpSceneResumesThroughOrdinaryUpdates()
    {
        if (!Vorhanden()) return;
        using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Spiel,
            EngineId = EnginePluginIds.RpgMakerXp,
            Generation = "xp",
            DetectorScore = 900,
        });
        AssertTrue(started.Success, started.Error?.Message ?? "Real XP game starts");
        var runtime = host.Runtime as XpEngineRuntime;
        AssertTrue(runtime != null);
        if (runtime == null) return;
        AssertEq(runtime.Frames, 0, "Project Main must not run during synchronous initialization.");
        AssertEq(runtime.FrameBudget, 0);
        for (var frame = 1; frame <= 10; frame++)
        {
            var result = runtime.Update(1.0 / 60.0);
            AssertTrue(result.Success, result.Error?.Message ?? "Ordinary update reaches a frame");
            AssertEq(runtime.State, PluginRuntimeState.Running);
            AssertEq(runtime.Frames, frame);
            var progress = runtime.ScriptProgress;
            AssertEq(progress.State, RgssScriptRunState.Running);
            AssertEq(progress.CurrentScript, "Main", "The real project entry must execute, not be skipped.");
            AssertEq(progress.Attempted, progress.Total);
            AssertEq(progress.Completed, progress.Total - 1, "Main remains on its live stack between frames.");
            AssertEq(progress.Failed, 0);
        }
        AssertEq(runtime.ScriptProblems.Count, 0, string.Join("; ", runtime.ScriptProblems));
        Console.WriteLine("XP: resumable host frame boundaries = " + runtime.Frames);
        Console.WriteLine("XP: resumable script problems = " + runtime.ScriptProblems.Count);
        AssertTrue(runtime.Stop().Success);
        AssertEq(runtime.State, PluginRuntimeState.Stopped);
        AssertFalse(runtime.Update(1.0 / 60.0).Success);
        AssertEq(runtime.Frames, 10);
    }

    public void Test_WasEinEchtesXpSpielBraucht()
    {
        if (!Vorhanden())
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var gestartet = host.Start(new PluginGameInfo
        {
            GameDirectory = Spiel,
            EngineId = EnginePluginIds.RpgMakerXp,
            Generation = "xp",
            DetectorScore = 900,
        });

        AssertTrue(gestartet.Success,
            "**the real game starts** -- and it said: " + gestartet.Error?.Message);

        var lauf = host.Runtime as XpEngineRuntime;

        // **And the game's own loop runs here, bounded.** XP's scenes are
        // `loop do ... end` and `Graphics.update` presents one frame and returns,
        // so a measured run sets a budget and starts the loop -- and the scene's
        // setup, where the windows and the sprites are built, finally runs.
        // **Measured before this: it never did**, and nothing was drawn while
        // every report said zero problems.
        if (lauf != null)
        {
            lauf.FrameBudget = 5;
            lauf.SpielSchleifeStarten();
            Console.WriteLine("XP: host frame boundaries = " + lauf.Frames);
            AssertEq(lauf.Frames, 5, "The game's own main must reach the host frame budget.");
            AssertEq(lauf.State, PluginRuntimeState.Stopped,
                "A measured loop is stopped after unwinding, not resumable.");
        }
        AssertTrue(lauf != null, "**and the host built an XP runtime**");
        if (lauf == null)
        {
            return;
        }

        // **And what the runtime reports about itself is part of the measurement**: the library
        // it runs against, the data it read, and the scripts it could not answer.
        Console.WriteLine("XP: info = " + (lauf.RuntimeInfo == null
            ? "none"
            : $"library {lauf.RuntimeInfo.RuntimeVersion}, title '{lauf.RuntimeInfo.Title}', "
                + $"data {lauf.RuntimeInfo.DataFilePaths.Count}, "
                + $"inspected {lauf.RuntimeInfo.InspectedFileCount}"));
        Console.WriteLine("XP: script problems = " + lauf.ScriptProblems.Count);
        Console.WriteLine("XP: setup problems = " + lauf.SetupProblems.Count);
        foreach (var problem in lauf.SetupProblems.Take(20))
        {
            Console.WriteLine("XP:   SETUP " + problem);
        }
        foreach (var reading in lauf.SetupState)
        {
            Console.WriteLine("XP:   STATE " + reading);
        }
        foreach (var problem in lauf.ScriptProblems.Take(40))
        {
            Console.WriteLine("XP:   " + problem);
        }

        // **Und die Namen, die das Spiel verlangt hat und die es nicht gibt.**
        // Der Leser antwortet mit nil und einer Diagnose statt mit einer
        // Ausnahme, also sind die Luecken still -- **und die Liste der Namen ist
        // der Bedarf.** Sie wird hier dedupliziert gedruckt, weil ein Name, den
        // ein Spiel jeden Frame fragt, sonst jede Zeile fuellt.
        var namen = lauf.Diagnostics
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Console.WriteLine("XP: diagnostics = " + lauf.Diagnostics.Count
            + ", distinct = " + namen.Length);
        foreach (var name in namen.Take(40))
        {
            Console.WriteLine("XP:   FEHLT " + name);
        }

        AssertEq(lauf.ScriptProblems.Count, 0,
            "The measured scene must not report script failures.");

        // A bounded run unwound its stack; ordinary updates must not restart it.
        var vorher = lauf.ScriptProblems.Count;
        var completedFrames = lauf.Frames;
        for (var frame = 0; frame < 5; frame++)
        {
            AssertFalse(lauf.Update(1.0 / 60.0).Success,
                "Updates after a measured scene has stopped must be rejected.");
        }
        AssertEq(lauf.Frames, completedFrames);
        AssertEq(lauf.ScriptProblems.Count, vorher);
        Console.WriteLine("XP: script problems after 5 frames = " + lauf.ScriptProblems.Count);
        foreach (var problem in lauf.ScriptProblems.Skip(vorher))
        {
            Console.WriteLine("XP:   NEW " + problem);
        }
    }
}
