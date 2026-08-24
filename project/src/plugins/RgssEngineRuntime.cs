using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;

namespace UniversalRPG.Plugins;

/// <summary>
/// Bounded metadata exposed by the shared RGSS backend. This is intentionally
/// not a Ruby VM state: scripts, Game.exe, RGSS DLLs, and external runtimes are
/// never loaded or executed by this implementation.
/// </summary>
public sealed class RgssRuntimeInfo
{
    public string PluginId { get; init; } = "";
    public string Generation { get; init; } = "";
    public string RuntimeLibraryPath { get; init; } = "";
    public Version? RuntimeVersion { get; init; }
    public string DataExtension { get; init; } = "";
    public string ArchiveExtension { get; init; } = "";
    public IReadOnlyList<string> DataFilePaths { get; init; } = Array.Empty<string>();
    public string ArchivePath { get; init; } = "";
    public string Title { get; init; } = "";
    public string RtpDependency { get; init; } = "";
    public int InspectedFileCount { get; init; }
    public bool IsArchiveSource { get; init; }
    public bool HasScriptPayload { get; init; }
    public string ExpectedSystemDataPath { get; init; } = "";
    public bool HasSystemData { get; init; }

    public int DataFileCount => DataFilePaths.Count;
}

/// <summary>
/// Safe common backend for RPG Maker XP (RGSS1), VX (RGSS2), and VX Ace
/// (RGSS3). It inspects a bounded folder/ZIP snapshot and advances the shared
/// deterministic clock, but does not interpret Ruby or load native engine code.
/// </summary>
public sealed class RgssEngineRuntime : IEngineRuntime
{
    private readonly string _pluginId;
    private readonly string _generation;
    private readonly string _runtimePrefix;
    private readonly string _dataExtension;
    private readonly string _archiveExtension;
    private readonly string _systemDataPath;
    private readonly PluginGameInfo _game;
    private readonly VirtualClock _clock = new();

    public RgssEngineRuntime(
        string pPluginId,
        string pGeneration,
        string pRuntimePrefix,
        string pDataExtension,
        string pArchiveExtension,
        PluginGameInfo pGame)
    {
        _pluginId = pPluginId ?? "";
        _generation = pGeneration ?? "";
        _runtimePrefix = pRuntimePrefix ?? "";
        _dataExtension = pDataExtension ?? "";
        _archiveExtension = pArchiveExtension ?? "";
        _systemDataPath = _generation switch
        {
            "xp" => "Data/System.rxdata",
            "vx" => "Data/System.rvdata",
            "vx-ace" => "Data/System.rvdata2",
            _ => ""
        };
        _game = pGame ?? throw new ArgumentNullException(nameof(pGame));
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public RgssRuntimeInfo? RuntimeInfo { get; private set; }
    public int SimulationTicks => _clock.GetSimulationTicks();
    public int InspectedFileCount => RuntimeInfo?.InspectedFileCount ?? 0;

    public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
    {
        if (State != PluginRuntimeState.Created)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "RGSS runtime was already initialized.", "initialize");
        }
        if (pContext == null || pContext.Game == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "RGSS runtime context is missing the detected game.", "initialize");
        }
        if (!pContext.Game.EngineId.Equals(_pluginId, StringComparison.Ordinal)
            || !pContext.Selection.Plugin.Metadata.Id.Equals(_pluginId, StringComparison.Ordinal))
        {
            return Fail(PluginErrorCode.UnsupportedEngine, "The RGSS runtime context does not match this plugin.", "initialize");
        }

        var inspection = SafeGameInspector.Inspect(_game.GameDirectory);
        if (!inspection.Success || inspection.Value == null)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                inspection.Error?.Message ?? "The detected RGSS game could not be inspected safely.",
                "initialize");
        }
        var snapshot = inspection.Value;
        if (snapshot.IsMalformed)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                "The detected RGSS game contains malformed or over-budget input.",
                "initialize");
        }

        var files = snapshot.Files;
        var runtimeFile = files.FirstOrDefault(pFile =>
        {
            var name = Path.GetFileName(pFile.RelativePath);
            return name.StartsWith(_runtimePrefix, StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        });
        if (runtimeFile == null)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                $"The RGSS {_generation} runtime library '{_runtimePrefix}*.dll' was not found in the bounded inspection.",
                "initialize");
        }

        var dataFiles = files
            .Where(pFile => IsDataFile(pFile.RelativePath))
            .Select(pFile => pFile.RelativePath)
            .OrderBy(pPath => pPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pPath => pPath, StringComparer.Ordinal)
            .ToArray();
        var archivePath = files
            .Where(pFile => pFile.RelativePath.EndsWith(_archiveExtension, StringComparison.OrdinalIgnoreCase))
            .Select(pFile => pFile.RelativePath)
            .OrderBy(pPath => pPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pPath => pPath, StringComparer.Ordinal)
            .FirstOrDefault() ?? "";
        var scriptPayload = files.Any(pFile =>
        {
            var name = Path.GetFileName(pFile.RelativePath);
            return name.StartsWith("Scripts.", StringComparison.OrdinalIgnoreCase);
        });
        var hasSystemData = files.Any(pFile =>
            GameInspectionSnapshot.NormalizeRelativePath(pFile.RelativePath)
                .Equals(_systemDataPath, StringComparison.OrdinalIgnoreCase));
        var libraryName = ReadIniValue(snapshot, "Library");
        var runtimeVersion = ParseRuntimeVersion(Path.GetFileName(runtimeFile.RelativePath));
        if (!string.IsNullOrEmpty(libraryName)
            && !Path.GetFileName(libraryName).StartsWith(_runtimePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                $"Game.ini points to '{libraryName}', which is not an {_runtimePrefix} library.",
                "initialize");
        }

        RuntimeInfo = new RgssRuntimeInfo
        {
            PluginId = _pluginId,
            Generation = _generation,
            RuntimeLibraryPath = runtimeFile.RelativePath,
            RuntimeVersion = runtimeVersion,
            DataExtension = _dataExtension,
            ArchiveExtension = _archiveExtension,
            DataFilePaths = dataFiles,
            ArchivePath = archivePath,
            Title = ReadIniValue(snapshot, "Title"),
            RtpDependency = ReadIniValue(snapshot, "RTP") is { Length: > 0 } rtp
                ? rtp
                : ReadIniValue(snapshot, "RTP1"),
            InspectedFileCount = files.Count,
            IsArchiveSource = snapshot.IsArchive,
            HasScriptPayload = scriptPayload,
            ExpectedSystemDataPath = _systemDataPath,
            HasSystemData = hasSystemData,
        };
        State = PluginRuntimeState.Initialized;

        var diagnostics = new List<PluginDiagnostic>
        {
            PluginDiagnostic.Info(
                "rgss.runtime-initialized",
                $"Initialized bounded {_generation} RGSS backend with {files.Count} inspected files; Ruby, Game.exe, RGSS DLLs, and external runtimes were not executed.",
                _pluginId),
        };
        if (snapshot.IsPartial)
        {
            diagnostics.Add(PluginDiagnostic.Warning(
                "rgss.partial-scan",
                $"The project exceeded the bounded inspection entry budget ({files.Count} files scanned); metadata is advisory for files outside the covered set.",
                _pluginId));
        }
        if (scriptPayload)
        {
            diagnostics.Add(PluginDiagnostic.Info(
                "rgss.scripts-inspected-only",
                "RGSS script payload metadata was detected but was not loaded or executed.",
                _pluginId));
        }
        if (!hasSystemData)
        {
            diagnostics.Add(PluginDiagnostic.Warning(
                "rgss.system-data-missing",
                $"Expected {_systemDataPath} for {_generation}, but it was not present in the bounded project snapshot; runtime initialization remains metadata-only.",
                _pluginId));
        }
        if (!string.IsNullOrEmpty(archivePath))
        {
            diagnostics.Add(PluginDiagnostic.Info(
                "rgss.archive-inspected-only",
                $"Detected {archivePath}; encrypted RGSS archive contents remain data-only in this runtime slice.",
                _pluginId));
        }
        return PluginOperationResult.Succeeded(diagnostics);
    }

    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"RGSS runtime cannot start from state {State}.", "start");
        }
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"RGSS runtime cannot update from state {State}.", "update");
        }
        if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "Delta time must be finite and non-negative.", "update");
        }
        _clock.ProcessFrame(pDeltaSeconds);
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"RGSS runtime cannot stop from state {State}.", "stop");
        }
        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    public void Dispose()
    {
        State = PluginRuntimeState.Disposed;
        RuntimeInfo = null;
    }

    private bool IsDataFile(string pRelativePath)
    {
        var normalized = GameInspectionSnapshot.NormalizeRelativePath(pRelativePath);
        return normalized.StartsWith("Data/", StringComparison.OrdinalIgnoreCase)
            && normalized.EndsWith(_dataExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadIniValue(GameInspectionSnapshot pSnapshot, string pName)
    {
        var file = pSnapshot.Files.FirstOrDefault(pFile =>
            Path.GetFileName(pFile.RelativePath).Equals("Game.ini", StringComparison.OrdinalIgnoreCase));
        if (file == null || file.IsTruncated)
        {
            return "";
        }
        var text = new LegacyTextDecoder().Decode(file.Data);
        foreach (var line in text.Split('\n'))
        {
            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }
            if (line[..separator].Trim().Equals(pName, StringComparison.OrdinalIgnoreCase))
            {
                return line[(separator + 1)..].Trim().TrimEnd('\r');
            }
        }
        return "";
    }

    private Version? ParseRuntimeVersion(string pFileName)
    {
        var lowerName = pFileName.ToLowerInvariant();
        var prefix = _runtimePrefix.ToLowerInvariant();
        if (!lowerName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }
        var digits = new string(lowerName[prefix.Length..].TakeWhile(char.IsDigit).ToArray());
        if (digits.Length < 2 || !int.TryParse(digits[..2], out var minorCode))
        {
            return null;
        }
        var major = prefix.Length > 0 && char.IsDigit(prefix[^1])
            ? prefix[^1] - '0'
            : 0;
        return major > 0 ? new Version(major, minorCode / 10, minorCode % 10) : null;
    }

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}
