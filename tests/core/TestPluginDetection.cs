using System;
using System.IO;
using System.IO.Compression;
using Godot;
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
        CreateLcf("RM95", "");
        WriteText(TempBase.PathJoin("RM95/RPG95.exe"), "metadata only");
        CreateLcf("RM2K", "RM2000");
        CreateLcf("RM2K3", "RM2003");
        CreateRgss("RMXP", "RGSS102A.dll", ".rxdata", "");
        CreateRgss("RMVX", "RGSS202E.dll", ".rvdata", "");
        CreateRgss("RMVXA", "RGSS302A.dll", ".rvdata2", "");
        CreateWeb("RMMV", "rpg_core.js");
        CreateWeb("RMMZ", "rmmz_core.js");
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

    public void Test_BuiltInDetectionOnlyRuntimeRefusesLaunch()
    {
        var report = Analyze("RMMV");
        var selector = new EngineRuntimeSelector();
        var selection = selector.Select(report, "windows");
        AssertFalse(selection.Success);
        AssertEq(selection.Error?.Code, PluginErrorCode.UnsupportedEngine);
    }

    public void Test_BuiltInMetadataUsesStableIndependentEngineRanges()
    {
        var plugins = BuiltInEnginePluginCatalog.CreatePlugins();
        AssertEq(plugins.Count, 10);
        foreach (var plugin in plugins)
        {
            AssertTrue(plugin.Metadata.Validate().Success, $"Metadata validates for {plugin.Metadata.Id}");
            AssertEq(plugin.Metadata.SupportedEngines[0].EngineId, plugin.Metadata.Id);
            AssertTrue(!string.IsNullOrWhiteSpace(plugin.Metadata.Description));
            var hasRuntimeBootstrap = plugin.Metadata.Id is
                EnginePluginIds.RpgMaker2000 or
                EnginePluginIds.RpgMaker2003;
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

    private static void CreateWeb(string pName, string pRuntime)
    {
        var root = TempBase.PathJoin(pName);
        WriteText(root.PathJoin("index.html"), "<!doctype html>");
        WriteText(root.PathJoin("data/System.json"), $"{{\"gameTitle\":\"{pName}\"}}");
        WriteText(root.PathJoin("js/" + pRuntime), "runtime metadata");
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
