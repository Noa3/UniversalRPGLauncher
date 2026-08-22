using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.App.Library;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestGameLibraryIntegration : TestBase
{
    private const string TempBase = "user://game_library_integration";
    private const string SettingsPath = "user://game_library_integration.cfg";
    private string _rm2kPath = "";
    private string _ambiguousPath = "";
    private string _detectionOnlyPath = "";

    public override void Setup()
    {
        Cleanup(TempBase);
        RemoveFile(SettingsPath);
        DirAccess.MakeDirRecursiveAbsolute(TempBase);
        _rm2kPath = TempBase.PathJoin("RM2K");
        _ambiguousPath = TempBase.PathJoin("Ambiguous");
        _detectionOnlyPath = TempBase.PathJoin(".unsupported/Unite");
        CreateLcf(_rm2kPath, "RM2000");
        CreateLcf(_ambiguousPath, "");
        WriteText(_detectionOnlyPath.PathJoin("UnityPlayer.dll"), "metadata only");
        WriteText(_detectionOnlyPath.PathJoin("GameAssembly.dll"), "metadata only");
        WriteText(_detectionOnlyPath.PathJoin("Demo_Data/globalgamemanagers"), "metadata only");
        WriteText(_detectionOnlyPath.PathJoin("Demo_Data/Managed/marker.txt"), "metadata only");
    }

    public override void Teardown()
    {
        Cleanup(TempBase);
        RemoveFile(SettingsPath);
    }

    public void Test_ImportPersistsCandidatesSelectionConfidenceEvidenceAndStatus()
    {
        var library = NewLibrary();
        library.SetRootPath(ProjectSettings.GlobalizePath(TempBase));

        var entry = library.Import(ProjectSettings.GlobalizePath(_rm2kPath));

        AssertTrue(entry != null);
        AssertEq(entry?.SelectedPluginId, EnginePluginIds.RpgMaker2000);
        AssertEq(entry?.CompatibilityStatus, GameLibrary.GameCompatibilityStatus.Supported);
        AssertTrue((entry?.Candidates.Count ?? 0) > 0);
        AssertTrue((entry?.Detection.Evidence.Count ?? 0) > 0);
        AssertTrue((entry?.DetectionConfidence ?? 0) > 0);

        var config = new ConfigFile();
        AssertEq(config.Load(SettingsPath), Error.Ok);
        var persisted = config.GetValue("games", entry!.Id, "").AsString();
        AssertTrue(persisted.Contains("selectedPluginId", StringComparison.Ordinal));
        AssertTrue(persisted.Contains("candidates", StringComparison.Ordinal));
        AssertTrue(persisted.Contains("compatibilityStatus", StringComparison.Ordinal));
        AssertTrue(persisted.Contains("evidence", StringComparison.Ordinal));
    }

    public void Test_RelaunchLoadsPersistedImportAndRevalidatesCurrentDetection()
    {
        var first = NewLibrary();
        first.SetRootPath(ProjectSettings.GlobalizePath(TempBase));
        var imported = first.Import(ProjectSettings.GlobalizePath(_rm2kPath));
        AssertTrue(imported != null);

        var relaunched = NewLibrary();
        relaunched.LoadSettings();
        var entries = relaunched.Scan();

        AssertEq(entries.Count, 2);
        var importedEntry = imported!;
        var restored = entries.Find(pEntry => pEntry.Path.Equals(importedEntry.Path, StringComparison.OrdinalIgnoreCase));
        AssertTrue(restored != null);
        AssertTrue(restored!.LoadedFromPersistence);
        AssertEq(restored.SelectedPluginId, importedEntry.SelectedPluginId);
        AssertEq(restored.CompatibilityStatus, importedEntry.CompatibilityStatus);
        AssertEq(restored.Candidates.Count, importedEntry.Candidates.Count);
        AssertTrue(restored.Detection.Evidence.Count > 0);
    }

    public void Test_AmbiguousDetectionIsPersistedWithoutUnsafeSelection()
    {
        var library = NewLibrary();
        var entry = library.Import(ProjectSettings.GlobalizePath(_ambiguousPath));

        AssertTrue(entry != null);
        AssertEq(entry?.CompatibilityStatus, GameLibrary.GameCompatibilityStatus.Ambiguous);
        AssertEq(entry?.SelectedPluginId, "");
        AssertTrue(entry != null && entry.Candidates.Count >= 2);
        AssertTrue(entry != null && entry.Detection.Diagnostics.Count > 0);
    }

    public void Test_UnsupportedDetectionOnlyEngineProducesStructuredLaunchError()
    {
        var library = NewLibrary();
        var entry = library.Import(ProjectSettings.GlobalizePath(_detectionOnlyPath));
        var launcher = new RuntimeLauncher(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());

        var support = launcher.GetSupport(entry!);
        var launch = launcher.Launch(entry!);

        AssertEq(support.State, RuntimeLauncher.SupportState.Unavailable);
        AssertEq(support.ErrorCode, PluginErrorCode.UnsupportedEngine);
        AssertEq(launch.ErrorCode, PluginErrorCode.UnsupportedEngine);
        AssertEq(launch.Phase, "select");
        AssertTrue(launch.Message.Contains(EnginePluginIds.RpgMakerUnite, StringComparison.Ordinal));
    }

    public void Test_MissingRuntimeRegistryProducesActionableFailure()
    {
        var report = new EngineDetectionReport
        {
            SourcePath = "fixture://supported-but-unregistered",
            SelectedCandidate = new EngineDetectionCandidate
            {
                PluginId = "supported-but-unregistered",
                EngineId = "supported-but-unregistered",
                DisplayName = "Supported but unregistered",
                Generation = "fixture",
                Score = 900,
                Status = EngineDetectionStatus.Supported,
                Reason = "Synthetic integration candidate.",
                Evidence = new[] { "synthetic fixture" },
            },
            Candidates = Array.Empty<EngineDetectionCandidate>(),
        };
        var detection = new GameDetector.DetectionResult
        {
            Engine = GameDetector.EngineType.Unknown,
            GameDirectory = report.SourcePath,
            Report = report,
        };
        var entry = new GameLibrary.GameEntry(report.SourcePath, detection);
        var launcher = new RuntimeLauncher(new EnginePluginRegistry());

        var support = launcher.GetSupport(entry, "windows");
        var launch = launcher.Launch(entry);

        AssertEq(support.ErrorCode, PluginErrorCode.NoMatchingPlugin);
        AssertEq(support.PluginId, "supported-but-unregistered");
        AssertEq(launch.ErrorCode, PluginErrorCode.NoMatchingPlugin);
        AssertEq(launch.PluginId, "supported-but-unregistered");
        AssertTrue(launch.Message.Contains("not registered", StringComparison.OrdinalIgnoreCase));
    }

    private GameLibrary NewLibrary()
    {
        return new GameLibrary(pSettingsPath: SettingsPath);
    }

    private static void CreateLcf(string pPath, string pEngineId)
    {
        WriteText(pPath.PathJoin("RPG_RT.ldb"), "LcfDataBase");
        WriteText(pPath.PathJoin("RPG_RT.lmt"), "LcfMapTree");
        WriteText(pPath.PathJoin("Map0001.lmu"), "map");
        var metadata = string.IsNullOrEmpty(pEngineId)
            ? "[RPG_RT]\nGameTitle=Integration Test\n"
            : $"[RPG_RT]\nEngineID={pEngineId}\nGameTitle=Integration Test\n";
        WriteText(pPath.PathJoin("RPG_RT.ini"), metadata);
    }

    private static void WriteText(string pPath, string pText)
    {
        var fullPath = ProjectSettings.GlobalizePath(pPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, pText);
    }

    private static void RemoveFile(string pPath)
    {
        var fullPath = ProjectSettings.GlobalizePath(pPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static void Cleanup(string pPath)
    {
        var fullPath = ProjectSettings.GlobalizePath(pPath);
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }
}
