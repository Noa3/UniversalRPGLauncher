using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Bounded MV data-directory inventory. MV shares the JSON layout with MZ, so
/// the same reader is used; only the runtime signature differs and the two
/// engines must never be read as each other.
/// </summary>
public partial class TestMvDataDirectory : TestBase
{
    private const string TempBase = "user://mv_data_directory_test";
    private PluginGameDetector _detector = null!;

    public override void Setup()
    {
        DirAccess.MakeDirRecursiveAbsolute(TempBase);
        _detector = new PluginGameDetector(BuiltInEnginePluginCatalog.CreateDetectionRegistry());
        CreateMvGame("MVFull",
            "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]",
            "[{\"id\":1,\"name\":\"Field\",\"parentId\":0},{\"id\":2,\"name\":\"Cave\",\"parentId\":1}]");
        WriteText(TempBase.PathJoin("MVFull/data/System.json"),
            "{\"gameTitle\":\"MVFull\",\"switches\":[null,\"Flag A\"],\"variables\":[null,\"Gold\",\"HP\"]}");
        WriteText(TempBase.PathJoin("MVFull/data/Classes.json"), "[{\"id\":1,\"name\":\"Mage\"}]");
        WriteText(TempBase.PathJoin("MVFull/data/Enemies.json"),
            "[{\"id\":1,\"name\":\"Slime\"},{\"id\":2,\"name\":\"Bat\"}]");
        WriteText(TempBase.PathJoin("MVFull/data/Map0001.json"), "{\"displayName\":\"Field\"}");
        WriteText(TempBase.PathJoin("MVFull/data/Map0002.json"), "{}");
        CreateMvGame("MVNoDataFiles");
        CreateMvGame("MVMalformed",
            "[{\"id\":1,\"name\"",
            "[]");
        CreateMvGame("MVNonArray",
            "{\"id\":1}",
            "[]");
        CreateMvGame("MVEncrypted");
        WriteText(TempBase.PathJoin("MVEncrypted/img/Actor1.rpgmvp"), new string('x', 32));
        CreateMvGame("MVBadOptionalSection");
        WriteText(TempBase.PathJoin("MVBadOptionalSection/data/Items.json"), "[{\"broken\":");
        CreateMzGame("MZOnlyForRefusal");
    }

    public override void Teardown()
    {
        CleanupDir(TempBase);
    }

    public void Test_ExtractsBoundedActorAndMapMetadata()
    {
        var result = Extract("MVFull");

        AssertTrue(result != null, "result extracted from MV snapshot");
        AssertEq(result!.ActorCount, 2, "actor entry count");
        AssertEq(result.ActorNames[0], "Alice", "first actor name");
        AssertEq(result.MapCount, 2, "map entry count");
        AssertEq(result.MapNames[1], "Cave", "second map name");
        AssertFalse(result.HasEncryptedAssets, "no encrypted assets present");
        AssertEq(result.Diagnostics.Count, 0, "no diagnostics on clean data");
    }

    public void Test_DatabaseInventoryCounts()
    {
        var result = Extract("MVFull");

        AssertTrue(result != null, "result extracted");
        AssertEq(result!.SectionCounts["Classes"], 1, "classes count");
        AssertEq(result.SectionCounts["Enemies"], 2, "enemies count");
        AssertFalse(result.SectionCounts.ContainsKey("Items"), "absent sections are omitted");
        AssertEq(result.SwitchNameCount, 2, "system switch array length");
        AssertEq(result.VariableNameCount, 3, "system variable array length");
        AssertEq(result.MapFileCount, 2, "physical map files counted");
    }

    public void Test_MissingDataFilesProduceDiagnostics()
    {
        var result = Extract("MVNoDataFiles");

        AssertTrue(result != null, "result still returned without data files");
        AssertEq(result!.ActorCount, 0, "no actors counted");
        AssertTrue(HasDiagnostic(result.Diagnostics, "Actors.json not found"), "missing actors diagnostic");
        AssertTrue(HasDiagnostic(result.Diagnostics, "MapInfos.json not found"), "missing map infos diagnostic");
    }

    public void Test_MalformedAndNonArrayJsonAreSkippedSafely()
    {
        var malformed = Extract("MVMalformed");

        AssertTrue(malformed != null, "malformed actors still returns a result object");
        AssertTrue(HasDiagnostic(malformed!.Diagnostics, "malformed JSON"), "malformed JSON diagnostic");
        AssertEq(malformed.ActorCount, 0, "no actors from malformed file");

        var nonArray = Extract("MVNonArray");

        AssertTrue(nonArray != null, "non-array actors still returns a result object");
        AssertTrue(HasDiagnostic(nonArray!.Diagnostics, "not a bounded JSON array"), "non-array diagnostic");
    }

    public void Test_MalformedOptionalSectionDiagnosedButSiblingsKept()
    {
        var result = Extract("MVBadOptionalSection");

        AssertTrue(result != null, "result returned despite malformed optional section");
        AssertFalse(result!.SectionCounts.ContainsKey("Items"), "malformed section omitted");
        AssertTrue(HasDiagnostic(result.Diagnostics, "data/Items.json contains malformed JSON"), "per-file diagnostic");
    }

    public void Test_EncryptedAssetsDetected()
    {
        var result = Extract("MVEncrypted");

        AssertTrue(result != null, "encrypted snapshot returns result");
        AssertTrue(result!.HasEncryptedAssets, ".rpgmvp asset detected");
        AssertTrue(HasDiagnostic(result.Diagnostics, "Encrypted assets detected"), "encryption diagnostic");
    }

    public void Test_MzSnapshotIsRefusedByMvReader()
    {
        var analysis = _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("MZOnlyForRefusal")));

        AssertTrue(analysis.Inspection != null, "MZ folder inspects to a snapshot");
        AssertTrue(MvDataDirectoryResult.Extract(analysis.Inspection!) == null,
            "snapshot without rpg_core.js is refused by the MV reader");
        AssertTrue(MzDataDirectoryResult.Extract(analysis.Inspection!) != null,
            "the same snapshot is still read by the MZ reader");
    }

    public void Test_MvSnapshotIsRefusedByMzReader()
    {
        var analysis = _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("MVFull")));

        AssertTrue(analysis.Inspection != null, "MV folder inspects to a snapshot");
        AssertTrue(MzDataDirectoryResult.Extract(analysis.Inspection!) == null,
            "snapshot without rmmz runtime signature is refused by the MZ reader");
        AssertTrue(MvDataDirectoryResult.Extract(analysis.Inspection!) != null,
            "the same snapshot is still read by the MV reader");
    }

    public void Test_SystemMetadataReadsVerifiedMvKeys()
    {
        WriteText(TempBase.PathJoin("MVFull/data/System.json"),
            "{\"gameTitle\":\"MVFull\",\"versionId\":7,\"locale\":\"ja_JP\",\"currencyUnit\":\"G\","
            + "\"startMapId\":12,\"startX\":7,\"startY\":19,\"partyMembers\":[0,1,2,3,4,5],"
            + "\"nested\":{\"gameTitle\":\"Trap\",\"versionId\":99}}");
        var analysis = _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("MVFull")));
        var metadata = RpgMakerMvPlugin.ExtractMetadata(analysis.Inspection!);

        AssertTrue(metadata != null, "MV System.json metadata is extracted");
        AssertEq(metadata!.GameTitle, "MVFull", "top-level title");
        AssertEq(metadata.VersionId, 7, "MV versionId is read, nested keys cannot shadow it");
        AssertEq(metadata.Locale, "ja_JP", "locale read");
        AssertEq(metadata.CurrencyUnit, "G", "currency unit read");
        AssertEq(metadata.StartMapId, 12, "start map read");
        AssertEq(metadata.StartX, 7, "start x read");
        AssertEq(metadata.StartY, 19, "start y read");
        AssertEq(metadata.PartyMemberIds.Count, 4, "actor id 0 is dropped and the party is capped at four");
        AssertEq(metadata.PartyMemberIds[0], 1, "first party member");
        AssertEq(metadata.PartyMemberIds[3], 4, "fifth entry is dropped by the cap");
    }

    public void Test_MvDataInventoryStillReadsAfterMetadataExtraction()
    {
        WriteText(TempBase.PathJoin("MVFull/data/System.json"),
            "{\"gameTitle\":\"MVFull\",\"versionId\":7,\"partyMembers\":[1,2]}");
        var result = Extract("MVFull");

        AssertTrue(result != null, "inventory still extracted");
        AssertEq(result!.ActorCount, 2, "actor count unchanged by metadata extraction");
        AssertEq(result.SwitchNameCount, 0, "missing switches array counts zero");
    }

    private MvDataDirectoryResult? Extract(string pGame)
    {
        var analysis = _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin(pGame)));
        return MvDataDirectoryResult.Extract(analysis.Inspection!);
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

    private static void CreateMvGame(string pName, string pActors = "", string pMapInfos = "")
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
        WriteText(root.PathJoin("js/rpg_core.js"), "runtime metadata");
        WriteText(root.PathJoin("js/rpg_managers.js"), "runtime metadata");
    }

    private static void CreateMzGame(string pName)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("index.html"), "<!doctype html>");
        WriteText(root.PathJoin("data/System.json"), $"{{\"gameTitle\":\"{pName}\"}}");
        WriteText(root.PathJoin("js/rmmz_core.js"), "runtime metadata");
        WriteText(root.PathJoin("js/rmmz_managers.js"), "runtime metadata");
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
