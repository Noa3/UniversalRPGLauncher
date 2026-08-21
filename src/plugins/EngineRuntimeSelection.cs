using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Plugins;

/// <summary>
/// Resolves a runtime only for a single unambiguous detection candidate. It
/// intentionally does not ask the registry to choose a different engine when
/// the candidate is uncertain, detection-only, malformed, or unsupported.
/// </summary>
public sealed class EngineRuntimeSelector
{
    private readonly EnginePluginRegistry _registry;

    public EngineRuntimeSelector()
        : this(BuiltInEnginePluginCatalog.CreateRuntimeRegistry())
    {
    }

    public EngineRuntimeSelector(EnginePluginRegistry pRegistry)
    {
        _registry = pRegistry ?? throw new ArgumentNullException(nameof(pRegistry));
    }

    public PluginResult<EnginePluginSelection> Select(
        EngineDetectionReport pReport,
        string pPlatform,
        PluginCapability pRequiredCapabilities = PluginCapability.Runtime)
    {
        if (pReport == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "A detection report is required before runtime selection.", "select");
        }
        if (pReport.IsAmbiguous)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                "Multiple engines have equally strong evidence; choose an engine explicitly before launching.",
                "select",
                DiagnosticsForReport(pReport, PluginDiagnostic.Warning(
                    "runtime.selection-ambiguous", "Runtime selection refused an ambiguous detection report.")));
        }
        if (pReport.IsUnknown || pReport.SelectedCandidate == null)
        {
            return Fail(
                PluginErrorCode.NoMatchingPlugin,
                "The engine is unknown or below the confidence threshold; runtime selection requires an explicit diagnostic path.",
                "select",
                DiagnosticsForReport(pReport, PluginDiagnostic.Warning(
                    "runtime.selection-unknown", "No safe runtime fallback was attempted.")));
        }
        if (pReport.IsMalformed || pReport.SelectedCandidate.Status == EngineDetectionStatus.Malformed)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                "The imported project contains malformed or bounded-out metadata; inspect the diagnostics before launching.",
                "select",
                DiagnosticsForReport(pReport, PluginDiagnostic.Warning(
                    "runtime.selection-malformed", "Runtime selection refused malformed input.")));
        }

        var candidate = pReport.SelectedCandidate;
        if (candidate.Status != EngineDetectionStatus.Supported)
        {
            return Fail(
                PluginErrorCode.UnsupportedEngine,
                $"'{candidate.DisplayName}' ({candidate.PluginId}) is detected but is not backed by a playable runtime.",
                "select",
                new[] { PluginDiagnostic.Warning("runtime.detection-only", "Detection-only metadata cannot be launched.", candidate.PluginId) },
                candidate.PluginId);
        }

        var plugin = _registry.Plugins.FirstOrDefault(pItem =>
            pItem.Metadata.Id.Equals(candidate.PluginId, StringComparison.Ordinal));
        if (plugin == null)
        {
            return Fail(
                PluginErrorCode.NoMatchingPlugin,
                $"Detected plugin '{candidate.PluginId}' is not registered; no incompatible fallback was attempted.",
                "select",
                new[] { PluginDiagnostic.Warning("runtime.plugin-missing", "The detected plugin is not registered; no incompatible fallback was attempted.", candidate.PluginId) },
                candidate.PluginId);
        }

        if ((plugin.Metadata.Capabilities & pRequiredCapabilities) != pRequiredCapabilities)
        {
            return Fail(
                PluginErrorCode.UnsupportedEngine,
                $"Plugin '{plugin.Metadata.Id}' does not provide the required runtime capabilities ({pRequiredCapabilities}).",
                "select",
                new[] { PluginDiagnostic.Warning("runtime.capability-missing", "The selected plugin does not advertise the required capabilities.", plugin.Metadata.Id) },
                plugin.Metadata.Id);
        }
        if (!plugin.Metadata.SupportsPlatform(pPlatform))
        {
            return Fail(
                PluginErrorCode.UnsupportedEngine,
                $"Plugin '{plugin.Metadata.Id}' is not compatible with platform '{pPlatform}'.",
                "select",
                new[] { PluginDiagnostic.Warning("runtime.platform-mismatch", "The selected plugin does not support the requested platform.", plugin.Metadata.Id) },
                plugin.Metadata.Id);
        }

        var game = candidate.ToPluginGameInfo(pReport.SourcePath);
        if (!plugin.Metadata.Supports(game))
        {
            return Fail(
                PluginErrorCode.UnsupportedEngine,
                $"Plugin '{plugin.Metadata.Id}' does not support the detected engine/version combination.",
                "select",
                new[] { PluginDiagnostic.Warning("runtime.engine-mismatch", "The selected plugin rejected the detected engine range.", plugin.Metadata.Id) },
                plugin.Metadata.Id);
        }

        PluginProbeResult probe;
        try
        {
            probe = plugin.Probe(new EnginePluginProbeContext(game));
        }
        catch (Exception exception)
        {
            return PluginResult<EnginePluginSelection>.Failed(PluginError.Create(
                PluginErrorCode.ProbeFailed,
                $"Runtime plugin '{plugin.Metadata.Id}' failed its compatibility probe.",
                plugin.Metadata.Id,
                "probe",
                exception));
        }
        if (probe == null || !probe.IsMatch)
        {
            return Fail(
                PluginErrorCode.NoMatchingPlugin,
                $"Runtime plugin '{plugin.Metadata.Id}' rejected the detected game during its compatibility probe.",
                "probe",
                probe?.Diagnostics,
                plugin.Metadata.Id);
        }
        var probeValidation = probe.Validate(plugin.Metadata.Id);
        if (!probeValidation.Success)
        {
            return PluginResult<EnginePluginSelection>.Failed(probeValidation.Error!, probeValidation.Diagnostics);
        }

        var report = new PluginProbeReport
        {
            PluginId = plugin.Metadata.Id,
            Supported = true,
            Matched = true,
            Score = probe.Score,
            Reason = probe.Reason,
            Diagnostics = probe.Diagnostics,
        };
        var selection = new EnginePluginSelection(plugin, game, probe, new[] { report });
        return PluginResult<EnginePluginSelection>.Succeeded(selection, new[]
        {
            PluginDiagnostic.Info("runtime.selected", $"Selected compatible runtime plugin '{plugin.Metadata.Id}'.", plugin.Metadata.Id),
        });
    }

    public PluginResult<IEngineRuntime> CreateRuntime(EnginePluginSelection pSelection)
    {
        if (pSelection == null)
        {
            return PluginResult<IEngineRuntime>.Failed(PluginError.Create(
                PluginErrorCode.RuntimeCreationFailed,
                "A validated runtime selection is required before runtime creation.",
                pPhase: "create"));
        }
        return _registry.CreateRuntime(pSelection);
    }

    public PluginResult<IEngineRuntime> SelectAndCreateRuntime(
        EngineDetectionReport pReport,
        string pPlatform,
        PluginCapability pRequiredCapabilities = PluginCapability.Runtime)
    {
        var selection = Select(pReport, pPlatform, pRequiredCapabilities);
        return selection.Success && selection.Value != null
            ? CreateRuntime(selection.Value)
            : PluginResult<IEngineRuntime>.Failed(
                selection.Error ?? PluginError.Create(
                    PluginErrorCode.RuntimeCreationFailed,
                    "Runtime selection did not produce a runtime plugin.",
                    pPhase: "create"),
                selection.Diagnostics);
    }

    private static PluginResult<EnginePluginSelection> Fail(
        PluginErrorCode pCode,
        string pMessage,
        string pPhase,
        IEnumerable<PluginDiagnostic>? pDiagnostics = null,
        string pPluginId = "")
    {
        return PluginResult<EnginePluginSelection>.Failed(
            PluginError.Create(pCode, pMessage, pPluginId, pPhase),
            pDiagnostics);
    }

    private static IReadOnlyList<PluginDiagnostic> DiagnosticsForReport(
        EngineDetectionReport pReport,
        PluginDiagnostic pPrimary)
    {
        var diagnostics = new List<PluginDiagnostic> { pPrimary };
        diagnostics.AddRange(pReport.Diagnostics);
        diagnostics.AddRange(pReport.InspectionDiagnostics.Select(pDiagnostic => PluginDiagnostic.Warning(
            pDiagnostic.Code,
            pDiagnostic.Message)));
        return diagnostics;
    }
}

/// <summary>Descriptive alias for callers that model selection as a service.</summary>
public sealed class RuntimeSelectionService
{
    private readonly EngineRuntimeSelector _selector;

    public RuntimeSelectionService(EnginePluginRegistry pRegistry)
    {
        _selector = new EngineRuntimeSelector(pRegistry);
    }

    public PluginResult<EnginePluginSelection> Select(EngineDetectionReport pReport, string pPlatform)
        => _selector.Select(pReport, pPlatform);

    public PluginResult<IEngineRuntime> SelectAndCreateRuntime(EngineDetectionReport pReport, string pPlatform)
        => _selector.SelectAndCreateRuntime(pReport, pPlatform);
}
