using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestMzDataDirectory : TestBase
{
    private const string TempBase = "user://mz_data_directory_test";
    private PluginGameDetector _detector = null!;

    public override void Setup()
    {
        DirAccess.MakeDirRecursiveAbsolute(TempBase);
        _detector = new PluginGameDetector(BuiltInEnginePluginCatalog.CreateDetectionRegistry());
        CreateMzGame("MZFull",
            "[{\"id\":1,\"name\":\"Harold\"},{\"id\":2,\"name\":\"Gloria\"}]",
            "[{\"id\":1,\"name\":\"World\",\"parentIndex\":0},{\"id\":2,\"name\":\"Town\",\"parentIndex\":1}]");
        CreateMzGame("MZNoDataFiles");
        CreateMzGame("MZMalformedActors",
             "[{\"id\":1,\"name\"",
            "[]");
        CreateMzGame("MZNonArrayActors",
             "{\"id\":1}",
            "[]");
        CreateMzGame("MZEncrypted");
        WriteText(TempBase.PathJoin("MZEncrypted/img/Actor1.rpgmvp"), new string('x', 32));
        CreateWebOnly("MVFolder", "rpg_core.js");
    }

    public override void Teardown()
    {
        CleanupDir(TempBase);
    }

    public void Test_ExtractsBoundedActorAndMapMetadata()
    {
        var result = Extract("MZFull");

        AssertTrue(result != null, "result extracted from MZ snapshot");
        AssertEq(result!.ActorCount, 2, "actor entry count");
        AssertEq(result.ActorNames.Count, 2, "listed actor names");
        AssertEq(result.ActorNames[0], "Harold", "first actor name");
        AssertEq(result.MapCount, 2, "map entry count");
        AssertEq(result.MapNames[1], "Town", "second map name");
        AssertFalse(result.HasEncryptedAssets, "no encrypted assets present");
        AssertEq(result.Diagnostics.Count, 0, "no diagnostics on clean data");
    }

    public void Test_MissingDataFilesProduceDiagnostics()
    {
        var result = Extract("MZNoDataFiles");

        AssertTrue(result != null, "result still returned without data files");
        AssertEq(result!.ActorCount, 0, "no actors counted");
        AssertEq(result.MapCount, 0, "no maps counted");
        AssertTrue(HasDiagnostic(result.Diagnostics, "Actors.json not found"), "missing actors diagnostic");
        AssertTrue(HasDiagnostic(result.Diagnostics, "MapInfos.json not found"), "missing map infos diagnostic");
    }

    public void Test_MalformedAndNonArrayJsonAreSkippedSafely()
    {
        var malformed = Extract("MZMalformedActors");

        AssertTrue(malformed != null, "malformed actors still returns a result object");
        AssertTrue(HasDiagnostic(malformed!.Diagnostics, "malformed JSON"), "malformed JSON diagnostic");
        AssertEq(malformed.ActorCount, 0, "no actors from malformed file");
        AssertEq(malformed.MapCount, 0, "empty map array counts zero maps");

        var nonArray = Extract("MZNonArrayActors");
        AssertTrue(nonArray != null, "non-array actors still returns a result object");
        AssertTrue(HasDiagnostic(nonArray!.Diagnostics, "not a bounded JSON array"), "non-array diagnostic");
    }

    public void Test_EncryptedAssetsDetected()
    {
        var result = Extract("MZEncrypted");

        AssertTrue(result != null, "encrypted snapshot returns result");
        AssertTrue(result!.HasEncryptedAssets, ".rpgmvp asset detected");
        AssertTrue(HasDiagnostic(result.Diagnostics, "Encrypted assets detected"), "encryption diagnostic");
    }

    public void Test_NonMzSnapshotIsRefused()
    {
        var analysis = _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("MVFolder")));
        var snapshot = analysis.Inspection;

        AssertTrue(snapshot != null, "MV folder inspects to a snapshot");
        var result = MzDataDirectoryResult.Extract(snapshot!);

        AssertTrue(result == null, "snapshot without rmmz runtime signature is refused");
    }

    private MzDataDirectoryResult? Extract(string pGame)
    {
        var analysis = _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin(pGame)));
        return MzDataDirectoryResult.Extract(analysis.Inspection!);
    }

    private static bool HasDiagnostic(IReadOnlyList<string> pDiagnostics, string pFragment)
    {
        foreach (var diagnostic in pDiagnostics)
        {
            if (diagnostic.Contains(pFragment, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static void CreateMzGame(string pName, string pActors = "", string pMapInfos = "")
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("index.html"), "<!doctype html>");
        WriteText(root.PathJoin("data/System.json"), $"{{\"gameTitle\":\"{pName}\"}}");
        if (pActors.Length > 0)
        {
            WriteText(root.PathJoin("data/Actors.json"), pActors);
        }
        if (pMapInfos.Length > 0)
        {
            WriteText(root.PathJoin("data/MapInfos.json"), pMapInfos);
        }
        WriteText(root.PathJoin("js/rmmz_core.js"), "runtime metadata");
        WriteText(root.PathJoin("js/rmmz_managers.js"), "runtime metadata");
    }

    private static void CreateWebOnly(string pName, string pRuntime)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("index.html"), "<!doctype html>");
        WriteText(root.PathJoin("data/System.json"), $"{{\"gameTitle\":\"{pName}\"}}");
        WriteText(root.PathJoin("js/" + pRuntime), "runtime metadata");
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
