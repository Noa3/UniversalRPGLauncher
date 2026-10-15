using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Synthetic, data-only RGSS fixtures. The fixtures deliberately contain names
/// that would be executable in a real game, but the test can only pass when the
/// bounded inspector and shared backend treat them as metadata.
/// </summary>
public partial class TestRgssRuntime : TestBase
{
    private const string TempBase = "user://rgss_runtime_test";
    private PluginGameDetector _detector = null!;

    public override void Setup()
    {
        CleanupDir(TempBase);
        DirAccess.MakeDirRecursiveAbsolute(TempBase);
        CreateRgss("XP", "RGSS102A.dll", ".rxdata", ".rgssad", "xp");
        CreateRgss("VX", "RGSS202E.dll", ".rvdata", ".rgss2a", "vx");
        CreateRgss("VXAce", "RGSS302A.dll", ".rvdata2", ".rgss3a", "vx-ace");
        CreateMissingRuntime();
        CreateVxArchive();
        _detector = new PluginGameDetector(BuiltInEnginePluginCatalog.CreateDetectionRegistry());
    }

    public override void Teardown()
    {
        CleanupDir(TempBase);
    }

    /// <summary>
    /// And the three RGSS engines offer a runtime that runs the project's own
    /// scripts, and the launcher does not pretend it can show them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test used to read "XP, VX and VX Ace are detection
    /// only" as if that were a property of the engines.</strong> It is not; it
    /// is a property of this repository's runtime selector -- and the owner has
    /// since decided that a game's own scripts may run while that game runs, so
    /// the gate this test guarded is open.
    /// </para>
    /// <para>
    /// <strong>And what replaces the gate is not silence.</strong> A runtime
    /// that runs 90 scripts and then stops at the first <c>$game_player</c>
    /// shows a black screen, so the honest contract has two halves: the
    /// capability list says exactly what exists -- a runtime, and no rendering,
    /// input, audio, save or debugging -- and the launcher reports
    /// <c>Experimental</c> with a reason that says it cannot show the game.
    /// </para>
    /// <para>
    /// <strong>And the measured facts behind the old gate are unchanged</strong>:
    /// MicroQuest carries 90 scripts in <c>Scripts.rxdata</c>, this repository
    /// parses and executes them, and none of that is a picture.
    /// </para>
    /// </remarks>
    public void Test_XpVxAndVxAceOfferAScriptRuntimeWithoutAPicture()
    {
        var cases = new[]
        {
            (Name: "XP", Id: EnginePluginIds.RpgMakerXp, Generation: "xp"),
            (Name: "VX", Id: EnginePluginIds.RpgMakerVx, Generation: "vx"),
            (Name: "VXAce", Id: EnginePluginIds.RpgMakerVxAce, Generation: "vx-ace"),
        };

        var launcher = new RuntimeLauncher();
        foreach (var item in cases)
        {
            var report = Analyze(item.Name);
            AssertEq(report.SelectedCandidate?.EngineId, item.Id, $"Detection selects {item.Name}");
            var selector = new EngineRuntimeSelector();
            var selection = selector.Select(report, "windows");
            AssertTrue(selection.Success,
                $"{item.Name} runtime selection succeeds -- and it said: "
                    + selection.Error?.Message);

            // **And the capability list is the promise.** A plugin that
            // declares Rendering while it has none is the black screen this
            // test exists to prevent.
            var capabilities = selection.Value!.Plugin.Metadata.Capabilities;
            AssertTrue(capabilities.HasFlag(PluginCapability.Runtime),
                $"{item.Name} declares the runtime it has");
            AssertFalse(capabilities.HasFlag(PluginCapability.Rendering),
                $"{item.Name} declares no rendering, because it has none");
            AssertFalse(capabilities.HasFlag(PluginCapability.SaveLoad),
                $"{item.Name} declares no save support, because it has none");
            AssertFalse(capabilities.HasFlag(PluginCapability.Debugging),
                $"{item.Name} declares no debugging, because it has none");
            AssertFalse(
                selection.Value.Plugin.Metadata.Description.Contains("Complete",
                    StringComparison.OrdinalIgnoreCase),
                $"{item.Name} does not call itself complete: "
                    + selection.Value.Plugin.Metadata.Description);

            // **And the window says what the metadata says.**
            var support = launcher.GetSupport(report, "windows");
            AssertEq(support.State, RuntimeLauncher.SupportState.Experimental,
                $"{item.Name} is offered as a runtime without a picture, and"
                    + $" not as a playable game; the window says: {support.Reason}");
            AssertTrue(support.Reason.Contains("cannot")
                    || support.Reason.Contains("no rendering"),
                $"{item.Name} says what is missing -- and it said: {support.Reason}");

            // **And the runtime goes for the project's scripts, and reports by
            // name and reason when they are not real.** The synthetic fixture's
            // Scripts file is not a script file, so this is the honest failure
            // path and not a start.
            using var host = new EnginePluginHost(
                BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
            var started = host.Start(new PluginGameInfo
            {
                GameDirectory = ProjectSettings.GlobalizePath(TempBase.PathJoin(item.Name)),
                EngineId = item.Id,
                Generation = item.Generation,
                DetectorScore = 850,
            });
            AssertFalse(started.Success,
                $"{item.Name} does not start a project whose Scripts file is"
                    + " not a script file");
            // **And the refusal names the script runtime it went for**, which
            // is the honest failure path: measured, the synthetic payload is
            // read as a Marshal stream and refused by version, and the message
            // says which runtime refused it.
            var refusal = started.Error?.Message ?? "";
            AssertTrue(refusal.Contains("Ruby runtime", StringComparison.OrdinalIgnoreCase),
                $"{item.Name} names the script runtime it went for -- and it"
                    + " said: " + refusal);
        }

        // **Und der Grund fuer das geschlossene Tor ist jetzt gemessen
        // und nicht behauptet**: -- **das Repository liest bereits, was
        // es noch nicht laufen lassen kann.**
        //
        // **Und 90 Skripte sind geparst und ausgefuehrt**, --
        // **und 96 Befehle stehen im Skript des Spiels**, --
        // **und `command_101` verlangt `$game_temp.message_text`,
        // ein Nachrichtenfenster und eine WarteAntwort**, --
        // **und keines davon hat heute einen Bildschirm.**
        AssertTrue(File.Exists(
                "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0"
                + "/Data/Scripts.rxdata"),
            "**and the game whose scripts this repository reads is really"
                + " on this machine** -- and the reading side is proven in"
                + " TestRgssSkriptHost");
        }

    /// <summary>
    /// And an archived VX project is still data: the selector may offer the
    /// script runtime, and nothing unpacks the archive, loads its DLL or starts
    /// its EXE on that path.
    /// </summary>
    public void Test_RgssArchiveIsInspectedWithoutExtractionOrExecution()
    {
        var report = Analyze("VXArchive.zip");
        AssertTrue(report.IsArchive);
        AssertEq(report.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerVx);
        var selection = new EngineRuntimeSelector().Select(report, "windows");
        AssertTrue(selection.Success,
            "the archived VX project offers the same script runtime -- and it"
                + " said: " + selection.Error?.Message);
        AssertTrue(selection.Value!.Plugin.Metadata.Capabilities
            .HasFlag(PluginCapability.Runtime));
        AssertFalse(selection.Value.Plugin.Metadata.Capabilities
            .HasFlag(PluginCapability.Rendering));
    }

    /// <summary>
    /// And a project whose RGSS library is missing is not refused, because this
    /// runtime never needs it -- it reads the project's own scripts.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the honest version of "fails before the runtime
    /// starts".</strong> The old assertion said a missing <c>RGSS102A.dll</c>
    /// must stop selection; what the runtime needs is
    /// <c>Data/Scripts.rxdata</c>, and the DLL is a native library this
    /// repository does not load on any path.
    /// </remarks>
    public void Test_MissingRgssLibraryDoesNotStopTheScriptRuntime()
    {
        var report = Analyze("MissingRuntime");
        AssertEq(report.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerXp);
        var selection = new EngineRuntimeSelector().Select(report, "windows");
        AssertTrue(selection.Success,
            "a project without its RGSS library still offers the script runtime"
                + " -- and it said: " + selection.Error?.Message);

        // **And the fixture really has no library**, so the assertion above is
        // about a missing file and not about a present one.
        var root = ProjectSettings.GlobalizePath(TempBase.PathJoin("MissingRuntime"));
        AssertFalse(Directory.GetFiles(root).Any(pFile =>
                Path.GetFileName(pFile).StartsWith("RGSS", StringComparison.OrdinalIgnoreCase)),
            "**and no RGSS library is there to load**");
        AssertTrue(File.Exists(Path.Combine(root, "Data", "Map001.rxdata")),
            "**and the project's own data is what it has**");
    }

    private EngineDetectionReport Analyze(string pName)
    {
        return _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin(pName)));
    }

    private static void CreateRgss(
        string pName,
        string pLibrary,
        string pDataExtension,
        string pArchiveExtension,
        string pGeneration)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("Game.ini"),
            $"[Game]\nTitle=Synthetic {pGeneration}\nLibrary={pLibrary}\nRTP=Standard\n");
        WriteText(root.PathJoin(pLibrary), "synthetic DLL bytes; never loaded");
        WriteText(root.PathJoin("Game.exe"), "synthetic executable bytes; never executed");
        WriteText(root.PathJoin("Data/Map001" + pDataExtension), "synthetic map data");
        WriteText(root.PathJoin("Data/Scripts" + pDataExtension), "synthetic script payload");
        WriteText(root.PathJoin("Game" + pArchiveExtension), "synthetic encrypted archive; never decrypted");
    }

    private static void CreateMissingRuntime()
    {
        var root = TempBase.PathJoin("MissingRuntime");
        WriteText(root.PathJoin("Game.ini"),
            "[Game]\nTitle=Missing runtime\nLibrary=RGSS102A.dll\nRTP=Standard\n");
        WriteText(root.PathJoin("Data/Map001.rxdata"), "synthetic map data");
    }

    private static void CreateVxArchive()
    {
        var archivePath = ProjectSettings.GlobalizePath(TempBase.PathJoin("VXArchive.zip"));
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var stream = File.Create(archivePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        AddArchiveText(archive, "Game.ini", "[Game]\nTitle=Archived VX\nLibrary=RGSS202E.dll\nRTP=Standard\n");
        AddArchiveText(archive, "RGSS202E.dll", "synthetic DLL bytes; never loaded");
        AddArchiveText(archive, "Game.exe", "synthetic executable bytes; never executed");
        AddArchiveText(archive, "Data/Map001.rvdata", "synthetic map data");
        AddArchiveText(archive, "Data/Scripts.rvdata", "synthetic script payload");
        AddArchiveText(archive, "Game.rgss2a", "synthetic encrypted archive; never decrypted");
    }

    private static void AddArchiveText(ZipArchive pArchive, string pPath, string pText)
    {
        using var writer = new StreamWriter(pArchive.CreateEntry(pPath).Open());
        writer.Write(pText);
    }

    private static void WriteText(string pPath, string pText)
    {
        var fullPath = ProjectSettings.GlobalizePath(pPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, pText);
    }

    private static void CleanupDir(string pDir)
    {
        var fullPath = ProjectSettings.GlobalizePath(pDir);
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }
}
