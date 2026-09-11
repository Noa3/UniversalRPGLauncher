using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Sdk;

namespace UniversalRPG.SdkHost;

/// <summary>
/// Adapter from the current UniversalRPG detector/plugin host to the Godot-free
/// public SDK contracts. External consumers should target UniversalRPG.Sdk;
/// this adapter is the in-application implementation while the runtime is still
/// hosted by Godot.
/// </summary>
public sealed class UniversalRpgLibraryAdapter : IUniversalRpgLibrary
{
    private readonly EnginePluginRegistry _runtimeRegistry;
    private readonly ProtectedContentRegistry _contentRegistry;
    private readonly IReadOnlyList<EngineSupportDescriptor> _engines;

    public UniversalRpgLibraryAdapter()
    {
        _runtimeRegistry = BuiltInEnginePluginCatalog.CreateRuntimeRegistry();
        _contentRegistry = ProtectedContentAnalyzer.CreateBuiltInRegistry();
        _engines = BuildEngineDescriptors();
    }

    public int ApiVersion => UniversalRpgSdkVersion.ApiVersion;
    public IReadOnlyList<EngineSupportDescriptor> Engines => _engines;

    public GameAnalysis Analyze(string pGameDirectory)
    {
        if (string.IsNullOrWhiteSpace(pGameDirectory))
        {
            return new GameAnalysis
            {
                GameDirectory = pGameDirectory ?? "",
                SupportLevel = EngineSupportLevel.Unknown,
                Diagnostics = new[] { SdkDiagnostic.Error("analysis.path-required", "A game directory is required.") },
            };
        }

        var detection = new GameDetector().Analyze(pGameDirectory);
        var selected = detection.Report.SelectedCandidate;
        var engineId = selected?.PluginId ?? "";
        var support = FindSupport(engineId);

        var diagnostics = new List<SdkDiagnostic>();
        foreach (var diagnostic in detection.Diagnostics)
        {
            diagnostics.Add(new SdkDiagnostic
            {
                Severity = diagnostic.Severity switch
                {
                    PluginDiagnosticSeverity.Error => SdkDiagnosticSeverity.Error,
                    PluginDiagnosticSeverity.Warning => SdkDiagnosticSeverity.Warning,
                    _ => SdkDiagnosticSeverity.Info,
                },
                Code = diagnostic.Code,
                Message = diagnostic.Message,
            });
        }

        var scripts = AnalyzeScripts(detection.GameDirectory, engineId, diagnostics);
        var protectedContent = ProtectedContentAnalyzer.Analyze(
            detection.GameDirectory,
            engineId,
            diagnostics,
            _contentRegistry);

        return new GameAnalysis
        {
            GameDirectory = detection.GameDirectory,
            EngineId = engineId,
            Generation = selected?.Generation ?? "",
            Title = detection.Title,
            ConfidenceScore = selected?.Score ?? 0,
            SupportLevel = support?.SupportLevel ?? EngineSupportLevel.Unknown,
            Evidence = detection.Evidence,
            Scripts = scripts,
            ProtectedContent = protectedContent,
            Diagnostics = diagnostics,
        };
    }

    public SdkSessionResult CreateSession(GameAnalysis pAnalysis)
    {
        if (pAnalysis == null)
        {
            return SdkSessionResult.Failed("session.analysis-required", "A game analysis is required.");
        }
        if (string.IsNullOrWhiteSpace(pAnalysis.GameDirectory) || string.IsNullOrWhiteSpace(pAnalysis.EngineId))
        {
            return SdkSessionResult.Failed("session.invalid-analysis", "The game analysis does not contain a usable directory and engine ID.");
        }

        var support = FindSupport(pAnalysis.EngineId);
        if (support == null || support.SupportLevel is EngineSupportLevel.Unknown or EngineSupportLevel.DetectionOnly or EngineSupportLevel.ParsingOnly)
        {
            return SdkSessionResult.Failed(
                "session.runtime-unavailable",
                $"Engine '{pAnalysis.EngineId}' is recognized but does not currently expose an executable UniversalRPG runtime.");
        }

        var unreadable = pAnalysis.ProtectedContent.FirstOrDefault(pContent => !pContent.RuntimeReadable);
        if (unreadable != null)
        {
            return SdkSessionResult.Failed(
                "session.protected-content-unavailable",
                $"The game requires protected-content scheme '{unreadable.Descriptor.SchemeId}', but no trusted runtime reader is available.",
                new[]
                {
                    SdkDiagnostic.Warning(
                        "session.protected-content-blocked",
                        unreadable.Note),
                });
        }

        var game = new PluginGameInfo
        {
            GameDirectory = pAnalysis.GameDirectory,
            EngineId = pAnalysis.EngineId,
            Generation = pAnalysis.Generation,
            DetectorScore = Math.Clamp(pAnalysis.ConfidenceScore / 250, 1, 3),
            Evidence = pAnalysis.Evidence,
        };
        return SdkSessionResult.Succeeded(new UniversalRpgSessionAdapter(_runtimeRegistry, game));
    }

    private static IReadOnlyList<EngineScriptDescriptor> AnalyzeScripts(
        string pGameDirectory,
        string pEngineId,
        List<SdkDiagnostic> pDiagnostics)
    {
        if (string.IsNullOrWhiteSpace(pGameDirectory)) return Array.Empty<EngineScriptDescriptor>();

        if (pEngineId is EnginePluginIds.RpgMakerMv or EnginePluginIds.RpgMakerMz)
        {
            var inspection = SafeGameInspector.Inspect(pGameDirectory);
            if (!inspection.Success || inspection.Value == null)
            {
                pDiagnostics.Add(SdkDiagnostic.Warning(
                    "scripts.inspect-failed",
                    inspection.Error?.Message ?? "Web plugin inventory could not inspect the game source."));
                return Array.Empty<EngineScriptDescriptor>();
            }
            var inventory = WebScriptInventory.Inspect(
                inspection.Value,
                pEngineId == EnginePluginIds.RpgMakerMz);
            pDiagnostics.AddRange(inventory.Diagnostics);
            foreach (var entry in inventory.Entries)
            {
                if (entry.Compatibility == WebScriptCompatibility.RequiresNodeShim)
                {
                    pDiagnostics.Add(SdkDiagnostic.Warning(
                        "scripts.node-shim-required",
                        $"Plugin '{entry.Script.DisplayName}' uses Node/NW.js APIs and will require a compatibility shim."));
                }
                else if (entry.Compatibility == WebScriptCompatibility.RequiresProcessExecution)
                {
                    pDiagnostics.Add(SdkDiagnostic.Warning(
                        "scripts.process-execution-required",
                        $"Plugin '{entry.Script.DisplayName}' requests host process execution, which is denied by the safe default policy."));
                }
                else if (entry.Compatibility == WebScriptCompatibility.RequiresNativeAddon)
                {
                    pDiagnostics.Add(SdkDiagnostic.Warning(
                        "scripts.native-addon-required",
                        $"Plugin '{entry.Script.DisplayName}' references a native Node addon and needs a platform-specific compatibility strategy."));
                }
            }
            return inventory.Entries.Select(pEntry => pEntry.Script).ToArray();
        }

        if (TryRgssGeneration(pEngineId, out var generation))
        {
            var archive = FindRgssScriptArchive(pGameDirectory, generation);
            if (archive == null)
            {
                pDiagnostics.Add(SdkDiagnostic.Info(
                    "scripts.rgss-archive-missing",
                    "No unencrypted Data/Scripts archive was available for bounded RGSS script inventory."));
                return Array.Empty<EngineScriptDescriptor>();
            }
            var result = RgssScriptArchiveReader.Read(archive, generation);
            if (!result.Success)
            {
                pDiagnostics.Add(SdkDiagnostic.Warning("scripts.rgss-read-failed", result.Error));
                return Array.Empty<EngineScriptDescriptor>();
            }
            return result.Scripts.Select(pScript => pScript.Descriptor).ToArray();
        }

        return Array.Empty<EngineScriptDescriptor>();
    }

    private static bool TryRgssGeneration(string pEngineId, out RgssGeneration pGeneration)
    {
        if (pEngineId == EnginePluginIds.RpgMakerXp)
        {
            pGeneration = RgssGeneration.Rgss1;
            return true;
        }
        if (pEngineId == EnginePluginIds.RpgMakerVx)
        {
            pGeneration = RgssGeneration.Rgss2;
            return true;
        }
        if (pEngineId == EnginePluginIds.RpgMakerVxAce)
        {
            pGeneration = RgssGeneration.Rgss3;
            return true;
        }
        pGeneration = default;
        return false;
    }

    private static string? FindRgssScriptArchive(string pGameDirectory, RgssGeneration pGeneration)
    {
        if (!Directory.Exists(pGameDirectory)) return null;
        try
        {
            var dataDirectory = Directory.EnumerateDirectories(pGameDirectory, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(pDirectory => Path.GetFileName(pDirectory).Equals("Data", StringComparison.OrdinalIgnoreCase));
            if (dataDirectory == null) return null;

            var expectedName = pGeneration switch
            {
                RgssGeneration.Rgss1 => "Scripts.rxdata",
                RgssGeneration.Rgss2 => "Scripts.rvdata",
                _ => "Scripts.rvdata2",
            };
            return Directory.EnumerateFiles(dataDirectory, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(pFile => Path.GetFileName(pFile).Equals(expectedName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private EngineSupportDescriptor? FindSupport(string pEngineId)
        => _engines.FirstOrDefault(pEngine => pEngine.EngineId.Equals(pEngineId, StringComparison.Ordinal));

    private static IReadOnlyList<EngineSupportDescriptor> BuildEngineDescriptors()
    {
        var result = new List<EngineSupportDescriptor>();
        foreach (var plugin in BuiltInEnginePluginCatalog.CreatePlugins())
        {
            var capabilities = plugin.Metadata.Capabilities;
            var level = (capabilities & PluginCapability.Runtime) != 0
                ? plugin.Metadata.Id == EnginePluginIds.WolfRpg
                    ? EngineSupportLevel.ExperimentalRuntime
                    : EngineSupportLevel.PartialRuntime
                : (capabilities & PluginCapability.Parsing) != 0
                    ? EngineSupportLevel.ParsingOnly
                    : EngineSupportLevel.DetectionOnly;

            result.Add(new EngineSupportDescriptor
            {
                EngineId = plugin.Metadata.Id,
                DisplayName = plugin.Metadata.DisplayName,
                Generation = plugin.Metadata.SupportedEngines.FirstOrDefault()?.Generation ?? "",
                SupportLevel = level,
                SupportsScripting = false,
                ScriptLanguageIds = Array.Empty<string>(),
                Platforms = plugin.Metadata.SupportedPlatforms,
            });
        }
        return result.OrderBy(pEngine => pEngine.EngineId, StringComparer.Ordinal).ToArray();
    }
}

public sealed class UniversalRpgSessionAdapter : IUniversalRpgSession
{
    private readonly EnginePluginHost _host;
    private readonly PluginGameInfo _game;
    private bool _disposed;

    public UniversalRpgSessionAdapter(EnginePluginRegistry pRegistry, PluginGameInfo pGame)
    {
        _host = new EnginePluginHost(pRegistry ?? throw new ArgumentNullException(nameof(pRegistry)));
        _game = pGame ?? throw new ArgumentNullException(nameof(pGame));
    }

    public string EngineId => _game.EngineId;
    public bool IsRunning => !_disposed && _host.State == PluginRuntimeState.Running;
    public IEngineScriptingRuntime? Scripting => _host.Runtime as IEngineScriptingRuntime;

    public SdkOperationResult Start()
    {
        if (_disposed) return SdkOperationResult.Failed("session.disposed", "The runtime session has been disposed.");
        return Convert(_host.Start(_game));
    }

    public SdkOperationResult Update(double pDeltaSeconds)
    {
        if (_disposed) return SdkOperationResult.Failed("session.disposed", "The runtime session has been disposed.");
        return Convert(_host.Update(pDeltaSeconds));
    }

    public SdkOperationResult Stop()
    {
        if (_disposed) return SdkOperationResult.Succeeded();
        return Convert(_host.Stop());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _host.Dispose();
    }

    private static SdkOperationResult Convert(PluginOperationResult pResult)
    {
        var diagnostics = pResult.Diagnostics.Select(pDiagnostic => new SdkDiagnostic
        {
            Severity = pDiagnostic.Severity switch
            {
                PluginDiagnosticSeverity.Error => SdkDiagnosticSeverity.Error,
                PluginDiagnosticSeverity.Warning => SdkDiagnosticSeverity.Warning,
                _ => SdkDiagnosticSeverity.Info,
            },
            Code = pDiagnostic.Code,
            Message = pDiagnostic.Message,
        }).ToArray();

        return pResult.Success
            ? SdkOperationResult.Succeeded(diagnostics)
            : SdkOperationResult.Failed(
                pResult.Error?.Code.ToString() ?? "runtime.failure",
                pResult.Error?.Message ?? "Runtime operation failed.",
                diagnostics);
    }
}
