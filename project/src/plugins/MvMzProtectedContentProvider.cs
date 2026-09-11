using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public sealed class MvMzProtectedContentProvider : IProtectedContentProvider
{
    public const string MvScheme = "rpg-maker-mv-assets";
    public const string MzScheme = "rpg-maker-mz-assets";

    private static readonly string[] Schemes = { MvScheme, MzScheme };

    public string Id => "builtin-rpg-maker-mv-mz-assets";
    public IReadOnlyList<string> SchemeIds => Schemes;

    public bool CanOpen(ProtectedContentDescriptor pDescriptor)
    {
        if (pDescriptor == null) return false;
        if (pDescriptor.Protection != GameContentProtectionKind.EngineManagedEncryption) return false;
        return pDescriptor.SchemeId switch
        {
            MvScheme => string.IsNullOrEmpty(pDescriptor.EngineId)
                || pDescriptor.EngineId.Equals(EnginePluginIds.RpgMakerMv, StringComparison.Ordinal),
            MzScheme => string.IsNullOrEmpty(pDescriptor.EngineId)
                || pDescriptor.EngineId.Equals(EnginePluginIds.RpgMakerMz, StringComparison.Ordinal),
            _ => false,
        };
    }

    public ContentSourceResult Open(ProtectedContentDescriptor pDescriptor)
    {
        if (!CanOpen(pDescriptor))
        {
            return ContentSourceResult.Failed(
                "mv-mz.provider-mismatch",
                "The MV/MZ content provider does not accept this descriptor.");
        }
        try
        {
            return ContentSourceResult.Succeeded(
                new MvMzGameContentSource(pDescriptor.SourcePath),
                new[]
                {
                    SdkDiagnostic.Info(
                        "mv-mz.transparent-decryption",
                        "Built-in RPG Maker asset encryption is mounted read-only and decrypted in memory on demand."),
                });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return ContentSourceResult.Failed(
                "mv-mz.provider-open-failed",
                $"Could not mount RPG Maker encrypted assets: {exception.Message}");
        }
    }
}
