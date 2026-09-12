using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;

namespace UniversalRPG.SdkHost;

/// <summary>
/// Builds the conservative logical game-content root exposed to the public SDK.
/// It never extracts archives and never loads executable provider code from the
/// imported game. Engine-specific provider composition is added only after its
/// precedence semantics are verified.
/// </summary>
public static class GameContentMountFactory
{
    public static ContentSourceResult Open(
        GameAnalysis pAnalysis,
        ProtectedContentRegistry pRegistry)
    {
        if (pAnalysis == null)
        {
            return ContentSourceResult.Failed("content.analysis-required", "A game analysis is required.");
        }
        if (pRegistry == null) throw new ArgumentNullException(nameof(pRegistry));
        if (string.IsNullOrWhiteSpace(pAnalysis.GameDirectory))
        {
            return ContentSourceResult.Failed("content.path-required", "The game analysis does not contain a source path.");
        }

        var sourcePath = pAnalysis.GameDirectory;
        if (File.Exists(sourcePath))
        {
            if (!Path.GetExtension(sourcePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return ContentSourceResult.Failed(
                    "content.archive-unsupported",
                    $"Imported archive '{Path.GetExtension(sourcePath)}' is not supported as a generic game-content mount.");
            }
            try
            {
                return ContentSourceResult.Succeeded(new ZipGameContentSource(sourcePath, "game-zip"));
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
            {
                return ContentSourceResult.Failed("content.zip-open-failed", exception.Message);
            }
        }

        if (!Directory.Exists(sourcePath))
        {
            return ContentSourceResult.Failed("content.source-missing", "The analyzed game source no longer exists.");
        }

        // MV/MZ engine-managed encryption is intentionally special-cased because
        // its provider already presents both loose plaintext files and encrypted
        // fallback assets under the same logical paths.
        var webProtected = pAnalysis.ProtectedContent.FirstOrDefault(pStatus =>
            pStatus.RuntimeReadable
            && pStatus.Descriptor.Protection == GameContentProtectionKind.EngineManagedEncryption
            && pStatus.Descriptor.SchemeId is ProtectedContentSchemes.RpgMakerMvAssets or ProtectedContentSchemes.RpgMakerMzAssets);
        if (webProtected != null)
        {
            return pRegistry.Open(webProtected.Descriptor);
        }

        var unreadable = pAnalysis.ProtectedContent.FirstOrDefault(pStatus => !pStatus.RuntimeReadable);
        var diagnostics = unreadable == null
            ? Array.Empty<SdkDiagnostic>()
            : new[]
            {
                SdkDiagnostic.Warning(
                    "content.protected-layer-unavailable",
                    $"Protected content '{unreadable.Descriptor.SchemeId}' is present but is not mounted by the generic content root."),
            };

        try
        {
            return ContentSourceResult.Succeeded(
                new DirectoryGameContentSource(sourcePath, "game-directory"),
                diagnostics);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return ContentSourceResult.Failed("content.directory-open-failed", exception.Message, diagnostics);
        }
    }
}
