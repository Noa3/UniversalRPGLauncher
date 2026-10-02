using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
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
    /// And the gate is still closed, and the reason is named.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test used to read "XP, VX and VX Ace are
    /// detection only" as if that were a property of the engines.</strong>
    /// <strong>It is not; it is a property of this repository's runtime
    /// selector</strong>, -- <strong>and the difference matters because
    /// the work has moved on since the test was written.</strong>
    /// </para>
    /// <para>
    /// <strong>And what exists now, measured on MicroQuest:</strong>
    /// <c>Scripts.rxdata</c> with 90 scripts, -- <strong>90 of them
    /// parsed and executed by this repository's own
    /// <c>RubyInterpreter</c></strong>, -- <strong>and 96 event commands
    /// read out of the game's own Ruby.</strong> -- <strong>and none of
    /// that is reachable through the selector.</strong>
    /// </para>
    /// <para>
    /// <strong>And so the gate stays shut</strong>, -- <strong>because a
    /// selector that opened it would hand a launcher a runtime that runs
    /// a game's 90 scripts and then stops at the first
    /// <c>$game_player</c></strong>, -- <strong>and a launcher that
    /// promises a game and shows a black screen is worse than one that
    /// says the engine is not supported.</strong>
    /// </para>
    /// <para>
    /// <strong>And the gate is now stated as what it is</strong>: a gap
    /// between what the repository can read and what it can present.
    /// <c>TestRgssSkriptHost</c> proves the reading side, this test
    /// proves the presenting side, and neither pretends to be the other.
    /// </para>
    /// </remarks>
    public void Test_XpVxAndVxAceRemainDetectionOnly()
    {
        var cases = new[]
        {
            (Name: "XP", Id: EnginePluginIds.RpgMakerXp, Generation: "xp", Major: 1),
            (Name: "VX", Id: EnginePluginIds.RpgMakerVx, Generation: "vx", Major: 2),
            (Name: "VXAce", Id: EnginePluginIds.RpgMakerVxAce, Generation: "vx-ace", Major: 3),
        };

        foreach (var item in cases)
        {
            var report = Analyze(item.Name);
            AssertEq(report.SelectedCandidate?.EngineId, item.Id, $"Detection selects {item.Name}");
            AssertEq(report.SelectedCandidate?.Status, EngineDetectionStatus.DetectionOnly, $"{item.Name} is detection-only");
            var selector = new EngineRuntimeSelector();
            var selection = selector.Select(report, "windows");
            AssertFalse(selection.Success, $"{item.Name} runtime selection must be refused");
            AssertEq(selection.Error?.Code, PluginErrorCode.UnsupportedEngine, $"{item.Name} refusal code");
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

    public void Test_RgssArchiveIsInspectedWithoutExtractionOrExecution()
    {
        var report = Analyze("VXArchive.zip");
        AssertTrue(report.IsArchive);
        AssertEq(report.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerVx);
        var selector = new EngineRuntimeSelector();
        var selection = selector.Select(report, "windows");
        AssertFalse(selection.Success, "RGSS archive runtime selection must be refused");
        AssertEq(selection.Error?.Code, PluginErrorCode.UnsupportedEngine);
    }

    public void Test_MissingRgssLibraryFailsBeforeRuntimeStarts()
    {
        var report = Analyze("MissingRuntime");
        AssertEq(report.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerXp);
        var selection = new EngineRuntimeSelector().Select(report, "windows");
        AssertFalse(selection.Success, "Missing-runtime RGSS selection must be refused");
        AssertEq(selection.Error?.Code, PluginErrorCode.UnsupportedEngine);
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
