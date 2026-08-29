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
        WriteText(TempBase.PathJoin("MZFull/data/System.json"),
            "{\"gameTitle\":\"MZFull\",\"switches\":[null,\"Switch A\",\"Switch B\"],\"variables\":[null,\"Gold\"]}");
        WriteText(TempBase.PathJoin("MZFull/data/Classes.json"), "[{\"id\":1,\"name\":\"Hero\"}]");
        WriteText(TempBase.PathJoin("MZFull/data/Skills.json"), "[]");
        WriteText(TempBase.PathJoin("MZFull/data/Items.json"), "[{\"id\":1,\"name\":\"Potion\"},{\"id\":2,\"name\":\"Ether\"}]");
        WriteText(TempBase.PathJoin("MZFull/data/Map0001.json"), "{\"displayName\":\"World\"}");
        WriteText(TempBase.PathJoin("MZFull/data/Map002.json"), "{}");
        WriteText(TempBase.PathJoin("MZFull/data/Map0033.json"), "{}");
        WriteText(TempBase.PathJoin("MZFull/audio/bgm_theme.ogg"), "audio metadata");
        CreateMzGame("MZNoDataFiles");
        CreateMzGame("MZMalformedActors",
            "[{\"id\":1,\"name\"",
            "[]");
        CreateMzGame("MZBadOptionalSection");
        WriteText(TempBase.PathJoin("MZBadOptionalSection/data/Items.json"), "[{\"broken\":");
        WriteText(TempBase.PathJoin("MZBadOptionalSection/data/Skills.json"), "[{\"id\":1}]");
        CreateMzGame("MZNonArrayActors",
            "{\"id\":1}",
            "[]");
        CreateMzGame("MZEncrypted");
        WriteText(TempBase.PathJoin("MZEncrypted/img/Actor1.rpgmvp"), new string('x', 32));
        CreateMzGame("MZNestedSystem");
        WriteText(TempBase.PathJoin("MZNestedSystem/data/System.json"),
            "{\"nested\":{\"gameTitle\":\"Nested Trap\",\"systemVersion\":\"0.0.0\",\"audioBrowsers\":[\"trap\"]},\"gameTitle\":\"Top Level Title\",\"systemVersion\":\"1.9.0\",\"audioBrowsers\":[\"ogg\",\"m4a\"]}");
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

    public void Test_SystemMetadataReadsTopLevelJsonProperties()
    {
        var analysis = _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("MZNestedSystem")));
        var metadata = RpgMakerMzPlugin.ExtractMetadata(analysis.Inspection!);

        AssertTrue(metadata != null, "MZ System.json metadata is extracted");
        AssertEq(metadata!.GameTitle, "Top Level Title", "nested gameTitle cannot shadow top-level title");
        AssertEq(metadata.SystemVersion, "1.9.0", "nested systemVersion cannot shadow top-level version");
        AssertEq(metadata.AudioBrowsers.Count, 2, "top-level audioBrowsers are extracted");
        AssertEq(metadata.AudioBrowsers[0], "ogg", "first top-level audio browser");
        AssertEq(metadata.AudioBrowsers[1], "m4a", "second top-level audio browser");
    }

    public void Test_DatabaseInventoryCounts()
    {
        var result = Extract("MZFull");

        AssertTrue(result != null, "result extracted");
        AssertEq(result!.SectionCounts["Classes"], 1, "classes count");
        AssertEq(result.SectionCounts["Skills"], 0, "empty skills array counts zero");
        AssertEq(result.SectionCounts["Items"], 2, "items count");
        AssertFalse(result.SectionCounts.ContainsKey("Weapons"), "absent sections are omitted");
        AssertEq(result.SwitchNameCount, 3, "system switch name count");
        AssertEq(result.VariableNameCount, 2, "system variable name count");
        AssertEq(result.MapFileCount, 3, "physical map files counted (Map0001/002/0033)");
        AssertEq(result.Diagnostics.Count, 0, "inventory stays quiet on clean data");
    }

    public void Test_MalformedOptionalSectionDiagnosedButOthersKept()
    {
        var result = Extract("MZBadOptionalSection");

        AssertTrue(result != null, "result returned despite malformed optional section");
        AssertFalse(result!.SectionCounts.ContainsKey("Items"), "malformed section omitted");
        AssertEq(result.SectionCounts["Skills"], 1, "valid sibling section still counted");
        AssertTrue(HasDiagnostic(result.Diagnostics, "data/Items.json contains malformed JSON"), "per-file diagnostic");
        AssertTrue(HasDiagnostic(result.Diagnostics, "Actors.json not found"), "absent required sections still diagnosed");
        AssertEq(result.Diagnostics.Count, 3, "items + actors + mapinfos diagnostics");
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
