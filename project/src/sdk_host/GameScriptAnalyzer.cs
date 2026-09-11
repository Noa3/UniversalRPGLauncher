using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Sdk;

namespace UniversalRPG.SdkHost;

/// <summary>
/// Non-executing game-authored script/plugin inventory shared by the public SDK
/// and future launcher diagnostics. Runtime execution remains a separate layer.
/// </summary>
public static class GameScriptAnalyzer
{
    public static IReadOnlyList<EngineScriptDescriptor> Analyze(
        string pGameSource,
        string pEngineId,
        List<SdkDiagnostic> pDiagnostics)
    {
        if (pDiagnostics == null) throw new ArgumentNullException(nameof(pDiagnostics));
        if (string.IsNullOrWhiteSpace(pGameSource)) return Array.Empty<EngineScriptDescriptor>();

        if (pEngineId is EnginePluginIds.RpgMakerMv or EnginePluginIds.RpgMakerMz)
        {
            return AnalyzeWeb(pGameSource, pEngineId, pDiagnostics);
        }
        if (TryRgssGeneration(pEngineId, out var generation))
        {
            return AnalyzeRgss(pGameSource, generation, pDiagnostics);
        }
        return Array.Empty<EngineScriptDescriptor>();
    }

    private static IReadOnlyList<EngineScriptDescriptor> AnalyzeWeb(
        string pGameSource,
        string pEngineId,
        List<SdkDiagnostic> pDiagnostics)
    {
        var inspection = SafeGameInspector.Inspect(pGameSource);
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
            switch (entry.Compatibility)
            {
                case WebScriptCompatibility.RequiresNodeShim:
                    pDiagnostics.Add(SdkDiagnostic.Warning(
                        "scripts.node-shim-required",
                        $"Plugin '{entry.Script.DisplayName}' uses Node/NW.js APIs and will require a compatibility shim."));
                    break;
                case WebScriptCompatibility.RequiresProcessExecution:
                    pDiagnostics.Add(SdkDiagnostic.Warning(
                        "scripts.process-execution-required",
                        $"Plugin '{entry.Script.DisplayName}' requests host process execution, which is denied by the safe default policy."));
                    break;
                case WebScriptCompatibility.RequiresNativeAddon:
                    pDiagnostics.Add(SdkDiagnostic.Warning(
                        "scripts.native-addon-required",
                        $"Plugin '{entry.Script.DisplayName}' references a native Node addon and needs a platform-specific compatibility strategy."));
                    break;
            }
        }
        return inventory.Entries.Select(pEntry => pEntry.Script).ToArray();
    }

    private static IReadOnlyList<EngineScriptDescriptor> AnalyzeRgss(
        string pGameSource,
        RgssGeneration pGeneration,
        List<SdkDiagnostic> pDiagnostics)
    {
        if (!Directory.Exists(pGameSource))
        {
            pDiagnostics.Add(SdkDiagnostic.Info(
                "scripts.rgss-directory-required",
                "RGSS script inventory currently requires a mounted game directory; protected archive content needs an approved provider first."));
            return Array.Empty<EngineScriptDescriptor>();
        }

        try
        {
            using var content = new DirectoryGameContentSource(pGameSource, "rgss-script-analysis");
            var config = RgssProjectConfigurationReader.Read(content, pGeneration);
            if (!config.Success || config.Value == null)
            {
                pDiagnostics.Add(SdkDiagnostic.Warning(
                    "scripts.rgss-config-invalid",
                    config.Result.ErrorMessage));
                return Array.Empty<EngineScriptDescriptor>();
            }

            var logicalPath = config.Value.ScriptArchivePath;
            if (!content.Exists(logicalPath))
            {
                pDiagnostics.Add(SdkDiagnostic.Info(
                    "scripts.rgss-archive-missing",
                    $"Configured RGSS script archive '{logicalPath}' is not available in the mounted game content."));
                return Array.Empty<EngineScriptDescriptor>();
            }

            var read = content.Read(logicalPath);
            if (!read.Success)
            {
                pDiagnostics.Add(SdkDiagnostic.Warning(
                    "scripts.rgss-read-failed",
                    $"[{read.ErrorCode}] {read.ErrorMessage}"));
                return Array.Empty<EngineScriptDescriptor>();
            }

            var result = RgssScriptArchiveReader.Read(read.Data.ToArray(), pGeneration);
            if (!result.Success)
            {
                pDiagnostics.Add(SdkDiagnostic.Warning("scripts.rgss-read-failed", result.Error));
                return Array.Empty<EngineScriptDescriptor>();
            }

            if (config.Value.UsesConfiguredScriptPath)
            {
                pDiagnostics.Add(SdkDiagnostic.Info(
                    "scripts.rgss-custom-archive",
                    $"Using Game.ini configured script archive '{logicalPath}'."));
            }
            return result.Scripts
                .Select(pScript => CloneWithPath(pScript.Descriptor, logicalPath))
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            pDiagnostics.Add(SdkDiagnostic.Warning(
                "scripts.rgss-analysis-failed",
                $"RGSS script inventory failed safely: {exception.Message}"));
            return Array.Empty<EngineScriptDescriptor>();
        }
    }

    private static EngineScriptDescriptor CloneWithPath(EngineScriptDescriptor pDescriptor, string pLogicalPath)
    {
        return new EngineScriptDescriptor
        {
            Id = pDescriptor.Id,
            DisplayName = pDescriptor.DisplayName,
            LanguageId = pDescriptor.LanguageId,
            RelativePath = pLogicalPath,
            Sha256 = pDescriptor.Sha256,
            Origin = pDescriptor.Origin,
            Required = pDescriptor.Required,
            LoadOrder = pDescriptor.LoadOrder,
            Dependencies = pDescriptor.Dependencies,
        };
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
}
