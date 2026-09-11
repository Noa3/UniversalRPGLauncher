using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;

namespace UniversalRPG.SdkHost;

public static class ProtectedContentAnalyzer
{
    public static IReadOnlyList<ProtectedContentStatus> Analyze(
        string pGameDirectory,
        string pEngineId,
        List<SdkDiagnostic>? pDiagnostics = null,
        ProtectedContentRegistry? pRegistry = null)
    {
        var diagnostics = pDiagnostics ?? new List<SdkDiagnostic>();
        var registry = pRegistry ?? CreateBuiltInRegistry();
        if (string.IsNullOrWhiteSpace(pGameDirectory) || !Directory.Exists(pGameDirectory))
        {
            return Array.Empty<ProtectedContentStatus>();
        }

        var result = new List<ProtectedContentStatus>();
        if (pEngineId is EnginePluginIds.RpgMakerMv or EnginePluginIds.RpgMakerMz)
        {
            AnalyzeMvMz(pGameDirectory, pEngineId, registry, diagnostics, result);
        }
        else if (pEngineId is EnginePluginIds.RpgMakerXp or EnginePluginIds.RpgMakerVx or EnginePluginIds.RpgMakerVxAce)
        {
            AnalyzeRgssArchives(pGameDirectory, pEngineId, registry, result);
        }
        else if (pEngineId == EnginePluginIds.WolfRpg)
        {
            AnalyzeWolfArchives(pGameDirectory, pEngineId, registry, result);
        }
        return result;
    }

    public static ProtectedContentRegistry CreateBuiltInRegistry()
    {
        var registry = new ProtectedContentRegistry();
        var registered = registry.Register(new MvMzProtectedContentProvider());
        if (!registered.Success)
        {
            throw new InvalidOperationException(registered.ErrorMessage);
        }
        return registry;
    }

    private static void AnalyzeMvMz(
        string pGameDirectory,
        string pEngineId,
        ProtectedContentRegistry pRegistry,
        List<SdkDiagnostic> pDiagnostics,
        List<ProtectedContentStatus> pResult)
    {
        var systemPath = FindSystemJson(pGameDirectory);
        if (systemPath == null) return;

        var metadata = MvMzEncryptedAssetCodec.ReadMetadata(systemPath);
        if (!metadata.Success || metadata.Value == null)
        {
            pDiagnostics.Add(SdkDiagnostic.Warning(
                "protected-content.mv-mz-metadata",
                metadata.Result.ErrorMessage));
            return;
        }

        if (!metadata.Value.HasEncryptedImages && !metadata.Value.HasEncryptedAudio)
        {
            return;
        }

        var descriptor = new ProtectedContentDescriptor
        {
            SchemeId = pEngineId == EnginePluginIds.RpgMakerMz
                ? ProtectedContentSchemes.RpgMakerMzAssets
                : ProtectedContentSchemes.RpgMakerMvAssets,
            SourcePath = pGameDirectory,
            EngineId = pEngineId,
            Protection = GameContentProtectionKind.EngineManagedEncryption,
            Metadata = new Dictionary<string, string>
            {
                ["encryptedImages"] = metadata.Value.HasEncryptedImages ? "true" : "false",
                ["encryptedAudio"] = metadata.Value.HasEncryptedAudio ? "true" : "false",
                ["keyPresent"] = metadata.Value.HasUsableKey ? "true" : "false",
            },
        };
        var providers = pRegistry.MatchingProviderIds(descriptor);
        var readable = metadata.Value.HasUsableKey && providers.Count == 1;
        pResult.Add(new ProtectedContentStatus
        {
            Descriptor = descriptor,
            RuntimeReadable = readable,
            ProviderId = readable ? providers[0] : "",
            Note = readable
                ? "Built-in MV/MZ encryption can be read transparently in memory."
                : "Encrypted MV/MZ assets were detected but no usable key/provider combination is available.",
        });
        if (!readable)
        {
            pDiagnostics.Add(SdkDiagnostic.Warning(
                "protected-content.mv-mz-unreadable",
                "Encrypted MV/MZ assets are present but cannot currently be mounted transparently."));
        }
    }

    private static void AnalyzeRgssArchives(
        string pGameDirectory,
        string pEngineId,
        ProtectedContentRegistry pRegistry,
        List<ProtectedContentStatus> pResult)
    {
        foreach (var path in EnumerateTopLevelFiles(pGameDirectory))
        {
            var extension = Path.GetExtension(path);
            var scheme = extension.ToLowerInvariant() switch
            {
                ".rgssad" => ProtectedContentSchemes.Rgss1Archive,
                ".rgss2a" => ProtectedContentSchemes.Rgss2Archive,
                ".rgss3a" => ProtectedContentSchemes.Rgss3Archive,
                _ => "",
            };
            if (string.IsNullOrEmpty(scheme)) continue;

            var descriptor = new ProtectedContentDescriptor
            {
                SchemeId = scheme,
                SourcePath = path,
                EngineId = pEngineId,
                Protection = GameContentProtectionKind.ProtectedArchive,
            };
            var providers = pRegistry.MatchingProviderIds(descriptor);
            pResult.Add(new ProtectedContentStatus
            {
                Descriptor = descriptor,
                RuntimeReadable = providers.Count == 1,
                ProviderId = providers.Count == 1 ? providers[0] : "",
                Note = providers.Count == 1
                    ? "A trusted RGSS archive provider is available."
                    : "RGSS encrypted archive recognized; no built-in archive provider is enabled yet.",
            });
        }
    }

    private static void AnalyzeWolfArchives(
        string pGameDirectory,
        string pEngineId,
        ProtectedContentRegistry pRegistry,
        List<ProtectedContentStatus> pResult)
    {
        foreach (var path in EnumerateTopLevelFiles(pGameDirectory))
        {
            if (!Path.GetExtension(path).Equals(".wolf", StringComparison.OrdinalIgnoreCase)) continue;
            var descriptor = new ProtectedContentDescriptor
            {
                SchemeId = ProtectedContentSchemes.WolfProtectedArchive,
                SourcePath = path,
                EngineId = pEngineId,
                Protection = GameContentProtectionKind.ProtectedArchive,
            };
            var providers = pRegistry.MatchingProviderIds(descriptor);
            pResult.Add(new ProtectedContentStatus
            {
                Descriptor = descriptor,
                RuntimeReadable = providers.Count == 1,
                ProviderId = providers.Count == 1 ? providers[0] : "",
                Note = providers.Count == 1
                    ? "A trusted WOLF protected-content provider is available."
                    : "Protected WOLF archive recognized; protection bypass is not part of the built-in runtime.",
            });
        }
    }

    private static string? FindSystemJson(string pGameDirectory)
    {
        var www = Path.Combine(pGameDirectory, "www", "data", "System.json");
        if (File.Exists(www)) return www;
        var root = Path.Combine(pGameDirectory, "data", "System.json");
        return File.Exists(root) ? root : null;
    }

    private static IEnumerable<string> EnumerateTopLevelFiles(string pDirectory)
    {
        try
        {
            return Directory.EnumerateFiles(pDirectory, "*", SearchOption.TopDirectoryOnly).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }
}
