using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestPluginDetection : TestBase
{
    private const string TempBase = "user://plugin_detection_test";
    private PluginGameDetector _detector = null!;

    public override void Setup()
    {
        DirAccess.MakeDirRecursiveAbsolute(TempBase);
        CreateRm95("RM95");
        WriteText(TempBase.PathJoin("Dante98/DANTE98.MRK"), "DANTE98 research fixture");
        WriteText(TempBase.PathJoin("RM95Weak/RPG95.exe"), "metadata only");
        CreateLcf("RM2K", "RM2000");
        CreateLcf("RM2K3", "RM2003");
        CreateRgss("RMXP", "RGSS102A.dll", ".rxdata", "");
        CreateRgss("RMVX", "RGSS202E.dll", ".rvdata", "");
        CreateRgss("RMVXA", "RGSS302A.dll", ".rvdata2", "");
        CreateWeb("RMMV", "rpg_core.js");
        CreateWeb("RMMZ", "rmmz_core.js", true);
        CreateWeb("MZWeak", "rmmz_core.js");
        CreateWeb("MZMalformed", "rmmz_core.js", true);
        WriteText(TempBase.PathJoin("MZMalformed/data/System.json"), "{not valid json");
        CreateWeb("MZOversized", "rmmz_core.js", true);
        WriteText(TempBase.PathJoin("MZOversized/data/System.json"), "{\"gameTitle\":\"" + new string('x', 1_100_000) + "\"}");
        WriteText(TempBase.PathJoin("WOLF/Data/Game.dat"), "wolf data");
        WriteText(TempBase.PathJoin("WOLF/Data/BasicData/System.db"), "wolf database");
        WriteText(TempBase.PathJoin("WOLF/Data/MapData/Map001.mps"), "wolf map");
        WriteText(TempBase.PathJoin("Unite/UnityPlayer.dll"), "MZ metadata only");
        WriteText(TempBase.PathJoin("Unite/GameAssembly.dll"), "MZ metadata only");
        WriteText(TempBase.PathJoin("Unite/MyGame_Data/globalgamemanagers"), "Unity metadata only");
        CreateLcf("Ambiguous", "");
        WriteText(TempBase.PathJoin("Unknown/readme.txt"), "not a game");
        WriteText(TempBase.PathJoin("Malformed.zip"), "not a zip archive");
        CreateMvArchive();
        _detector = new PluginGameDetector(BuiltInEnginePluginCatalog.CreateDetectionRegistry());
    }

    public override void Teardown()
    {
        CleanupDir(TempBase);
    }

    public void Test_DetectRepresentativeLegacyAndModernEngines()
    {
        AssertEq(Analyze("RM95").SelectedCandidate?.EngineId, EnginePluginIds.RpgMaker95);
        AssertEq(Analyze("Dante98").SelectedCandidate?.EngineId, EnginePluginIds.Dante98);
        AssertEq(Analyze("RM2K").SelectedCandidate?.EngineId, EnginePluginIds.RpgMaker2000);
        AssertEq(Analyze("RM2K3").SelectedCandidate?.EngineId, EnginePluginIds.RpgMaker2003);
        AssertEq(Analyze("RMXP").SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerXp);
        AssertEq(Analyze("RMVX").SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerVx);
        AssertEq(Analyze("RMVXA").SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerVxAce);
        AssertEq(Analyze("RMMV").SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerMv);
        AssertEq(Analyze("RMMZ").SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerMz);
        AssertEq(Analyze("WOLF").SelectedCandidate?.EngineId, EnginePluginIds.WolfRpg);
        AssertEq(Analyze("Unite").SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerUnite);
    }

    public void Test_MzRequiresManagersAndValidBoundedSystemMetadata()
    {
        var positive = Analyze("RMMZ");
        AssertEq(positive.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerMz);

        var weak = Analyze("MZWeak");
        AssertTrue(weak.SelectedCandidate == null || weak.SelectedCandidate.EngineId != EnginePluginIds.RpgMakerMz);

        var malformed = Analyze("MZMalformed");
        AssertTrue(malformed.SelectedCandidate == null || malformed.SelectedCandidate.EngineId != EnginePluginIds.RpgMakerMz);

        var oversized = Analyze("MZOversized");
        AssertTrue(oversized.SelectedCandidate == null || oversized.SelectedCandidate.EngineId != EnginePluginIds.RpgMakerMz);
    }

    public void Test_MvMetadataIsBoundedAndReportsEncryptedAssets()
    {
        var system = new InspectedGameFile("data/System.json", 27,
            System.Text.Encoding.UTF8.GetBytes("{\"gameTitle\":\"MV Fixture\"}"), false, false);
        var encrypted = new InspectedGameFile("img/system/Window.png.rpgmvp", 0,
            Array.Empty<byte>(), false, false);
        var snapshot = new GameInspectionSnapshot("mv-fixture", false, false,
            new System.Collections.Generic.Dictionary<string, InspectedGameFile>
            {
                [system.RelativePath] = system,
                [encrypted.RelativePath] = encrypted,
            }, new System.Collections.Generic.List<EngineInspectionDiagnostic>());
        var metadata = RpgMakerMvPlugin.ExtractMetadata(snapshot);
        AssertTrue(metadata != null);
        AssertEq(metadata!.GameTitle, "MV Fixture");
        AssertTrue(metadata.HasEncryptedFiles);
        AssertTrue(metadata.Diagnostics.Count == 1);
    }

    public void Test_AmbiguousAndUnknownResultsAreSafe()
    {
        var ambiguous = Analyze("Ambiguous");
        AssertTrue(ambiguous.IsAmbiguous);
        AssertTrue(ambiguous.SelectedCandidate == null);
        AssertTrue(ambiguous.Diagnostics.Count > 0);

        var unknown = Analyze("Unknown");
        AssertTrue(unknown.IsUnknown);
        AssertTrue(unknown.SelectedCandidate == null);

        var malformed = Analyze("Malformed.zip");
        AssertTrue(malformed.IsMalformed);
        AssertTrue(malformed.IsUnknown);
        AssertTrue(malformed.InspectionDiagnostics.Count > 0);
    }

    public void Test_ZipArchiveIsInspectedBoundedly()
    {
        var report = Analyze("RMMV.zip");
        AssertTrue(report.IsArchive);
        AssertEq(report.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerMv);
        AssertTrue(report.InspectionDiagnostics.Count == 0);
    }

    public void Test_RegistrationExtendsDetectionWithoutChangingFacade()
    {
        var registry = new EngineDetectionRegistry();
        var plugin = new SyntheticDetectionPlugin();
        AssertTrue(registry.Register(plugin).Success);
        var report = new PluginGameDetector(registry).Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("Unknown")));
        AssertEq(report.SelectedCandidate?.EngineId, "synthetic-detection");
        AssertEq(report.SelectedCandidate?.Score, 900);
    }

    public void Test_RuntimeSelectionRejectsDetectionOnlyAndAmbiguousReports()
    {
        var selector = new EngineRuntimeSelector();
        var detectionOnly = Analyze("Unite");
        var unsupported = selector.Select(detectionOnly, "windows");
        AssertFalse(unsupported.Success);
        AssertEq(unsupported.Error?.Code, PluginErrorCode.UnsupportedEngine);

        var ambiguous = selector.Select(Analyze("Ambiguous"), "windows");
        AssertFalse(ambiguous.Success);
        AssertEq(ambiguous.Error?.Code, PluginErrorCode.InvalidGame);
    }

    public void Test_RuntimeSelectionHonorsPlatformAndDoesNotFallback()
    {
        var detectionRegistry = new EngineDetectionRegistry();
        var plugin = new SyntheticRuntimePlugin();
        AssertTrue(detectionRegistry.Register(plugin).Success);
        var report = new PluginGameDetector(detectionRegistry).Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("Unknown")));

        var runtimeRegistry = new EnginePluginRegistry();
        AssertTrue(runtimeRegistry.Register(plugin).Success);
        var selector = new EngineRuntimeSelector(runtimeRegistry);
        var wrongPlatform = selector.Select(report, "linux");
        AssertFalse(wrongPlatform.Success);
        AssertEq(wrongPlatform.Error?.Code, PluginErrorCode.UnsupportedEngine);
        var matchingPlatform = selector.Select(report, "windows");
        AssertTrue(matchingPlatform.Success);
    }

    public void Test_Rm2kRuntimeLoadsValidatedFixtureAndAdvancesClock()
    {
        var fixture = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/rm2000");
        var game = new PluginGameInfo
        {
            GameDirectory = fixture,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());

        var started = host.Start(game);
        AssertTrue(started.Success, started.Error?.Message ?? "RM2K runtime start failed");
        AssertEq(host.State, PluginRuntimeState.Running);
        var updated = host.Update(1.0 / 30.0);
        AssertTrue(updated.Success, updated.Error?.Message ?? "RM2K runtime update failed");
        AssertTrue(host.Runtime is Rm2kEngineRuntime runtime && runtime.SimulationTicks >= 2);
        var stopped = host.Stop();
        AssertTrue(stopped.Success);
    }

    public void Test_Rm2kRuntimeToolsRequireExplicitDebugOptIn()
    {
        var runtime = new Rm2kEngineRuntime(EnginePluginIds.RpgMaker2000, new PluginGameInfo
        {
            GameDirectory = ProjectSettings.GlobalizePath(TempBase),
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
        });

        AssertTrue(runtime is IRuntimeSaveTools);
        AssertTrue(runtime is IRuntimeDebugTools);
        AssertFalse(((IRuntimeDebugTools)runtime).TrySetGold(999).Success);
        AssertTrue(((IRuntimeDebugTools)runtime).SetDebugToolsEnabled(true).Success);
        runtime.Simulation.ConfigureMap(1, 1, 1, new[] { true });
        AssertTrue(((IRuntimeDebugTools)runtime).TrySetGold(999).Success);
        AssertEq(runtime.Simulation.Gold, 999);
        var snapshot = ((IRuntimeSaveTools)runtime).ExportSaveSnapshot();
        AssertTrue(snapshot.Success);
        runtime.Simulation.Gold = 1;
        AssertTrue(((IRuntimeSaveTools)runtime).ImportSaveSnapshot(snapshot.Value ?? "").Success);
        AssertEq(runtime.Simulation.Gold, 999);
    }

    public void Test_BuiltInDetectionOnlyRuntimeRefusesLaunch()
    {
        var report = Analyze("RMMV");
        var selector = new EngineRuntimeSelector();
        var selection = selector.Select(report, "windows");
        AssertFalse(selection.Success);
        AssertEq(selection.Error?.Code, PluginErrorCode.UnsupportedEngine);

        var rm95Selection = selector.Select(Analyze("RM95"), "windows");
        AssertFalse(rm95Selection.Success);
        AssertEq(rm95Selection.Error?.Code, PluginErrorCode.UnsupportedEngine);
    }

    public void Test_Rm95FilenameAloneDoesNotCreateCandidate()
    {
        var report = Analyze("RM95Weak");
        AssertTrue(report.IsUnknown);
        AssertTrue(report.SelectedCandidate == null);
    }

    public void Test_Dante98DoesNotAliasGenericPc98Files()
    {
        WriteText(TempBase.PathJoin("GenericPc98/DANTE2"), "pc98 data");
        var report = Analyze("GenericPc98");
        AssertTrue(report.SelectedCandidate == null || report.SelectedCandidate.EngineId != EnginePluginIds.Dante98);
    }

    public void Test_Dante98FacadeEngineResolution()
    {
        WriteText(TempBase.PathJoin("DanteFacade/DANTE98.MRK"), "facade fixture");
        var result = new GameDetector().Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin("DanteFacade")));
        AssertEq(result.Engine, GameDetector.EngineType.Dante98);
        AssertEq(result.GetEngineName(), "RPG Tsukūru Dante 98");
    }

    public void Test_PartialEntryBudgetDoesNotRefuseDetection()
    {
        var root = TempBase.PathJoin("PartialBudget");
        WriteText(root.PathJoin("index.html"), "<!doctype html>");
        WriteText(root.PathJoin("js/rpg_core.js"), "runtime metadata");
        WriteText(root.PathJoin("data/System.json"), "{\"gameTitle\":\"Partial Budget\"}");
        for (var index = 0; index < 50; index++)
        {
            WriteText(root.PathJoin($"audio/Bgm/track{index:D3}.ogg"), "pad");
        }

        var limitedDetector = new PluginGameDetector(
            BuiltInEnginePluginCatalog.CreateDetectionRegistry(),
            new GameInspectionLimits { MaxEntries = 40 });
        var report = limitedDetector.Analyze(ProjectSettings.GlobalizePath(root));
        AssertTrue(report.Inspection?.IsPartial ?? false, "entry budget marks the snapshot partial");
        AssertFalse(report.IsMalformed, "partial scan is not malformed");
        AssertEq(report.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerMv);
        AssertTrue(report.Diagnostics.Any(pDiagnostic => pDiagnostic.Code == "detection.partial-scan"),
            "partial scan is reported diagnostically");

        var selector = new EngineRuntimeSelector();
        var selection = selector.Select(report, "windows");
        AssertEq(selection.Error?.Code, PluginErrorCode.UnsupportedEngine,
            "selection still refuses MV (detection-only) but not for being malformed");
    }

    public void Test_MvMetadataTitleIgnoresNestedGameTitleKeys()
    {
        // The nested "gameTitle" appears earlier in the document than the real
        // top-level one, so a naive first-match regex would pick the trap value.
        var system = new InspectedGameFile("data/System.json", 0,
            System.Text.Encoding.UTF8.GetBytes(
                "{\"sounds\":{\"gameTitle\":\"Nested Trap\"},\"gameTitle\":\"Top Level Title\",\"title\":\"Other\"}"), false, false);
        var snapshot = new GameInspectionSnapshot("mv-nested-fixture", false, false,
            new System.Collections.Generic.Dictionary<string, InspectedGameFile>
            {
                [system.RelativePath] = system,
            }, new System.Collections.Generic.List<EngineInspectionDiagnostic>());
        var metadata = RpgMakerMvPlugin.ExtractMetadata(snapshot);
        AssertTrue(metadata != null, "MV metadata parses the nested-key fixture");
        AssertEq(metadata!.GameTitle, "Top Level Title", "top-level gameTitle wins over a nested one");

        // The shared detection title helper (used by WebRpgPlugin.Detect for both
        // MV and MZ candidate titles) must resolve the top-level key too. Verify it
        // end-to-end through a real on-disk MV fixture so JsonTitle is exercised, not
        // just the standalone metadata API above.
        var webRoot = TempBase.PathJoin("MvNestedWeb");
        WriteText(webRoot.PathJoin("index.html"), "<!doctype html>");
        WriteText(webRoot.PathJoin("js/rpg_core.js"), "runtime metadata");
        WriteText(webRoot.PathJoin("data/System.json"),
            "{\"sounds\":{\"gameTitle\":\"Nested Trap\"},\"gameTitle\":\"Top Level Title\",\"title\":\"Other\"}");
        var webReport = Analyze("MvNestedWeb");
        AssertEq(webReport.SelectedCandidate?.EngineId, EnginePluginIds.RpgMakerMv);
        AssertEq(webReport.SelectedCandidate?.Title, "Top Level Title",
            "detection candidate title comes from the top-level gameTitle");
    }

    public void Test_BuiltInMetadataUsesStableIndependentEngineRanges()
    {
        var plugins = BuiltInEnginePluginCatalog.CreatePlugins();
        AssertEq(plugins.Count, 11);
        foreach (var plugin in plugins)
        {
            AssertTrue(plugin.Metadata.Validate().Success, $"Metadata validates for {plugin.Metadata.Id}");
            AssertEq(plugin.Metadata.SupportedEngines[0].EngineId, plugin.Metadata.Id);
            AssertTrue(!string.IsNullOrWhiteSpace(plugin.Metadata.Description));
            var hasRuntimeBootstrap = plugin.Metadata.Id is
                            EnginePluginIds.RpgMaker2000 or
                            EnginePluginIds.RpgMaker2003 or
                            EnginePluginIds.RpgMakerXp or
                            EnginePluginIds.RpgMakerVx or
                            EnginePluginIds.RpgMakerVxAce or
                EnginePluginIds.WolfRpg;
            AssertTrue(hasRuntimeBootstrap
                == ((plugin.Metadata.Capabilities & PluginCapability.Runtime) != 0),
                $"Runtime capability matches the built-in bootstrap boundary: {plugin.Metadata.Id}");
        }
    }

    public void Test_UnsafeZipPathsRemainDataOnlyAndAreReported()
    {
        var archivePath = ProjectSettings.GlobalizePath(TempBase.PathJoin("UnsafePaths.zip"));
        using (var stream = File.Create(archivePath))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            AddArchiveText(archive, "../outside.txt", "must not escape");
            AddArchiveText(archive, "index.html", "<!doctype html>");
        }

        var report = _detector.Analyze(archivePath);
        AssertTrue(report.IsArchive);
        AssertTrue(report.IsMalformed);
        AssertTrue(report.InspectionDiagnostics.Count > 0);
        AssertTrue(!File.Exists(Path.Combine(Path.GetDirectoryName(archivePath)!, "outside.txt")));
    }

    private EngineDetectionReport Analyze(string pRelativePath)
    {
        return _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin(pRelativePath)));
    }

    private static void CreateRm95(string pName)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("GAME.RPG"), "synthetic RM95 descriptor");
        WriteText(root.PathJoin("MAP0001.ATR"), "synthetic RM95 map metadata");
        WriteText(root.PathJoin("EVT00001.DAT"), "synthetic RM95 event metadata");
        WriteText(root.PathJoin("STRINGS.DAT"), "synthetic RM95 strings");
    }

    private static void CreateLcf(string pName, string pEngineId)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("RPG_RT.ldb"), "LcfDataBase");
        WriteText(root.PathJoin("RPG_RT.lmt"), "LcfMapTree");
        WriteText(root.PathJoin("Map0001.lmu"), "map");
        WriteText(root.PathJoin("RPG_RT.ini"), string.IsNullOrEmpty(pEngineId)
            ? "[RPG_RT]\nGameTitle=LCF Test\n"
            : $"[RPG_RT]\nEngineID={pEngineId}\nGameTitle=LCF Test\n");
    }

    private static void CreateRgss(string pName, string pLibrary, string pDataExtension, string pArchiveExtension)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("Game.ini"), $"[Game]\nTitle={pName}\nLibrary={pLibrary}\nRTP=Standard\n");
        WriteText(root.PathJoin(pLibrary), "library metadata");
        WriteText(root.PathJoin("Data/Map001" + pDataExtension), "map metadata");
        if (!string.IsNullOrEmpty(pArchiveExtension))
        {
            WriteText(root.PathJoin("Game" + pArchiveExtension), "archive metadata");
        }
    }

    private static void CreateWeb(string pName, string pRuntime, bool pIncludeMzManagers = false)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("index.html"), "<!doctype html>");
        WriteText(root.PathJoin("data/System.json"), $"{{\"gameTitle\":\"{pName}\"}}");
        WriteText(root.PathJoin("js/" + pRuntime), "runtime metadata");
        if (pIncludeMzManagers)
        {
            WriteText(root.PathJoin("js/rmmz_managers.js"), "runtime metadata");
        }
        WriteText(root.PathJoin("package.json"), "{\"version\":\"1.6.0\"}");
    }

    private static void CreateMvArchive()
    {
        var archivePath = ProjectSettings.GlobalizePath(TempBase.PathJoin("RMMV.zip"));
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var stream = File.Create(archivePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        AddArchiveText(archive, "index.html", "<!doctype html>");
        AddArchiveText(archive, "data/System.json", "{\"gameTitle\":\"Archive MV\"}");
        AddArchiveText(archive, "js/rpg_core.js", "runtime metadata");
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

    private sealed class SyntheticDetectionPlugin : BuiltInEnginePlugin
    {
        public SyntheticDetectionPlugin()
            : base("synthetic-detection", "Synthetic detector", "Synthetic test plugin.", "synthetic", 1, PluginCapability.Detection)
        {
        }

        public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
        {
            return Match(pContext.Snapshot, 900, "Synthetic plugin matched the fixture.", new[] { "synthetic fixture" });
        }
    }

    private sealed class SyntheticRuntimePlugin : IEnginePlugin, IEngineDetectionPlugin
    {
        public SyntheticRuntimePlugin()
        {
            Metadata = new EnginePluginMetadata
            {
                Id = "synthetic-runtime",
                DisplayName = "Synthetic runtime",
                Description = "Synthetic runtime test plugin.",
                Priority = 1,
                Capabilities = PluginCapability.Detection | PluginCapability.Runtime,
                SupportedPlatforms = new[] { "windows" },
                SupportedEngines = new[] { new PluginEngineRange { EngineId = "synthetic-runtime", Generation = "synthetic" } },
            };
        }

        public EnginePluginMetadata Metadata { get; }

        public EngineDetectionProbe Detect(EngineInspectionContext pContext)
        {
            var candidate = new EngineDetectionCandidate
            {
                PluginId = Metadata.Id,
                EngineId = Metadata.Id,
                DisplayName = Metadata.DisplayName,
                Generation = "synthetic",
                Score = 900,
                Status = EngineDetectionStatus.Supported,
                Reason = "Synthetic runtime matched the fixture.",
                Evidence = new[] { "synthetic runtime fixture" },
            };
            return EngineDetectionProbe.Match(candidate);
        }

        public PluginProbeResult Probe(EnginePluginProbeContext pContext)
            => PluginProbeResult.Match(900, "Synthetic runtime compatibility matched.");

        public PluginResult<IEngineRuntime> CreateRuntime(EnginePluginRuntimeContext pContext)
            => PluginResult<IEngineRuntime>.Succeeded(new SyntheticRuntime());
    }

    private sealed class SyntheticRuntime : IEngineRuntime
    {
        public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
        public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext) { State = PluginRuntimeState.Initialized; return PluginOperationResult.Succeeded(); }
        public PluginOperationResult Start() { State = PluginRuntimeState.Running; return PluginOperationResult.Succeeded(); }
        public PluginOperationResult Update(double pDeltaSeconds) => PluginOperationResult.Succeeded();
        public PluginOperationResult Stop() { State = PluginRuntimeState.Stopped; return PluginOperationResult.Succeeded(); }
        public void Dispose() { State = PluginRuntimeState.Disposed; }
    }
}
