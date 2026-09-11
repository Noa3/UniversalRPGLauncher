using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public enum WebScriptCompatibility
{
    StandardBrowserApi,
    RequiresNodeShim,
    RequiresProcessExecution,
    RequiresNativeAddon,
    Truncated,
    MissingFile,
}

public sealed class WebScriptInventoryEntry
{
    public EngineScriptDescriptor Script { get; init; } = new();
    public bool Enabled { get; init; }
    public WebScriptCompatibility Compatibility { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
}

public sealed class WebScriptInventoryResult
{
    public IReadOnlyList<WebScriptInventoryEntry> Entries { get; init; } = Array.Empty<WebScriptInventoryEntry>();
    public IReadOnlyList<SdkDiagnostic> Diagnostics { get; init; } = Array.Empty<SdkDiagnostic>();
}

/// <summary>
/// Bounded, non-executing inventory for RPG Maker MV/MZ custom JavaScript
/// plugins. plugins.js is treated as data: only its JSON-shaped plugin array is
/// parsed and individual plugin files are hashed/classified from inspected data.
/// No JavaScript is evaluated.
/// </summary>
public static class WebScriptInventory
{
    public const int MaxPlugins = 2048;

    public static WebScriptInventoryResult Inspect(string pGamePath, bool pMZ)
    {
        var inspection = SafeGameInspector.Inspect(pGamePath);
        if (!inspection.Success || inspection.Value == null)
        {
            return new WebScriptInventoryResult
            {
                Diagnostics = new[]
                {
                    SdkDiagnostic.Error(
                        "scripts.inspect-failed",
                        inspection.Error?.Message ?? "The game could not be inspected safely."),
                },
            };
        }
        return Inspect(inspection.Value, pMZ);
    }

    public static WebScriptInventoryResult Inspect(GameInspectionSnapshot pSnapshot, bool pMZ)
    {
        if (pSnapshot == null) throw new ArgumentNullException(nameof(pSnapshot));

        var diagnostics = new List<SdkDiagnostic>();
        var entries = new List<WebScriptInventoryEntry>();
        var pluginFiles = pSnapshot.Files
            .Where(pFile => IsPluginFile(pFile.RelativePath))
            .OrderBy(pFile => pFile.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pFile => pFile.RelativePath, StringComparer.Ordinal)
            .ToArray();

        var configured = ReadPluginConfiguration(pSnapshot, diagnostics);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var language = pMZ ? ScriptLanguageIds.RpgMakerMzJavaScript : ScriptLanguageIds.RpgMakerMvJavaScript;
        var loadOrder = 0;

        foreach (var plugin in configured)
        {
            if (entries.Count >= MaxPlugins)
            {
                diagnostics.Add(SdkDiagnostic.Warning("scripts.plugin-limit", $"Plugin inventory stopped at {MaxPlugins} configured entries."));
                break;
            }

            var expectedName = plugin.Name + ".js";
            var file = pluginFiles.FirstOrDefault(pFile =>
                System.IO.Path.GetFileName(pFile.RelativePath).Equals(expectedName, StringComparison.OrdinalIgnoreCase));
            if (file == null)
            {
                entries.Add(new WebScriptInventoryEntry
                {
                    Enabled = plugin.Enabled,
                    Compatibility = WebScriptCompatibility.MissingFile,
                    Reasons = new[] { $"Configured plugin file '{expectedName}' was not found in js/plugins/." },
                    Script = Descriptor(plugin.Name, language, $"js/plugins/{expectedName}", "", loadOrder++, plugin.Enabled),
                });
                continue;
            }

            seenPaths.Add(GameInspectionSnapshot.NormalizeRelativePath(file.RelativePath));
            entries.Add(BuildEntry(file, plugin.Name, language, plugin.Enabled, loadOrder++));
        }

        foreach (var file in pluginFiles)
        {
            if (entries.Count >= MaxPlugins)
            {
                diagnostics.Add(SdkDiagnostic.Warning("scripts.plugin-limit", $"Plugin inventory stopped at {MaxPlugins} entries."));
                break;
            }
            var normalized = GameInspectionSnapshot.NormalizeRelativePath(file.RelativePath);
            if (seenPaths.Contains(normalized))
            {
                continue;
            }
            var name = System.IO.Path.GetFileNameWithoutExtension(file.RelativePath);
            entries.Add(BuildEntry(file, name, language, false, loadOrder++));
        }

        if (pSnapshot.IsPartial)
        {
            diagnostics.Add(SdkDiagnostic.Warning(
                "scripts.partial-inspection",
                "The game inspection hit its entry budget, so the script inventory may be incomplete."));
        }

        return new WebScriptInventoryResult
        {
            Entries = entries,
            Diagnostics = diagnostics,
        };
    }

    private static WebScriptInventoryEntry BuildEntry(
        InspectedGameFile pFile,
        string pName,
        string pLanguage,
        bool pEnabled,
        int pLoadOrder)
    {
        var reasons = new List<string>();
        var compatibility = Classify(pFile, reasons);
        var hash = pFile.IsTruncated ? "" : Convert.ToHexString(SHA256.HashData(pFile.Data)).ToLowerInvariant();
        return new WebScriptInventoryEntry
        {
            Enabled = pEnabled,
            Compatibility = compatibility,
            Reasons = reasons,
            Script = Descriptor(
                pName,
                pLanguage,
                GameInspectionSnapshot.NormalizeRelativePath(pFile.RelativePath),
                hash,
                pLoadOrder,
                pEnabled),
        };
    }

    private static EngineScriptDescriptor Descriptor(
        string pName,
        string pLanguage,
        string pRelativePath,
        string pHash,
        int pLoadOrder,
        bool pRequired)
    {
        return new EngineScriptDescriptor
        {
            Id = "plugin:" + StableId(pName),
            DisplayName = pName,
            LanguageId = pLanguage,
            RelativePath = pRelativePath,
            Sha256 = pHash,
            Origin = ScriptOrigin.Plugin,
            Required = pRequired,
            LoadOrder = pLoadOrder,
        };
    }

    private static WebScriptCompatibility Classify(InspectedGameFile pFile, List<string> pReasons)
    {
        if (pFile.IsTruncated)
        {
            pReasons.Add("Plugin source exceeds the bounded inspection size; compatibility classification is incomplete.");
            return WebScriptCompatibility.Truncated;
        }

        var text = Encoding.UTF8.GetString(pFile.Data);
        if (ContainsAny(text, ".node", "process.dlopen"))
        {
            pReasons.Add("Native Node addon loading was detected.");
            return WebScriptCompatibility.RequiresNativeAddon;
        }
        if (ContainsAny(text, "child_process", "execFile(", "exec(", "spawn("))
        {
            pReasons.Add("Host process execution API usage was detected.");
            return WebScriptCompatibility.RequiresProcessExecution;
        }
        if (ContainsAny(text, "require(", "process.", "Buffer", "require('fs')", "require(\"fs\")", "require('path')", "require(\"path\")"))
        {
            pReasons.Add("Node/NW.js API usage was detected and will require a compatibility shim or explicit permission.");
            return WebScriptCompatibility.RequiresNodeShim;
        }

        return WebScriptCompatibility.StandardBrowserApi;
    }

    private static List<(string Name, bool Enabled)> ReadPluginConfiguration(
        GameInspectionSnapshot pSnapshot,
        List<SdkDiagnostic> pDiagnostics)
    {
        var config = pSnapshot.Files.FirstOrDefault(pFile =>
            pFile.RelativePath.Equals("js/plugins.js", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.Equals("www/js/plugins.js", StringComparison.OrdinalIgnoreCase));
        if (config == null)
        {
            pDiagnostics.Add(SdkDiagnostic.Info("scripts.plugins-config-missing", "No js/plugins.js configuration was found; plugin files are inventoried as disabled/unordered."));
            return new List<(string, bool)>();
        }
        if (config.IsTruncated)
        {
            pDiagnostics.Add(SdkDiagnostic.Warning("scripts.plugins-config-truncated", "js/plugins.js is truncated; configured plugin order cannot be trusted."));
            return new List<(string, bool)>();
        }

        var text = Encoding.UTF8.GetString(config.Data);
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            pDiagnostics.Add(SdkDiagnostic.Warning("scripts.plugins-config-invalid", "js/plugins.js does not contain a bounded plugin array."));
            return new List<(string, bool)>();
        }

        try
        {
            using var document = JsonDocument.Parse(text.Substring(start, end - start + 1), new JsonDocumentOptions
            {
                MaxDepth = 64,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<(string, bool)>();
            }

            var result = new List<(string, bool)>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (result.Count >= MaxPlugins) break;
                if (item.ValueKind != JsonValueKind.Object
                    || !item.TryGetProperty("name", out var nameElement)
                    || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }
                var name = nameElement.GetString() ?? "";
                if (!SafePluginName(name))
                {
                    pDiagnostics.Add(SdkDiagnostic.Warning("scripts.invalid-plugin-name", $"Ignored unsafe plugin name '{name}'."));
                    continue;
                }
                var enabled = item.TryGetProperty("status", out var statusElement)
                    && statusElement.ValueKind == JsonValueKind.True;
                result.Add((name, enabled));
            }
            return result;
        }
        catch (JsonException)
        {
            pDiagnostics.Add(SdkDiagnostic.Warning("scripts.plugins-config-invalid", "js/plugins.js plugin array could not be parsed as bounded JSON data."));
            return new List<(string, bool)>();
        }
    }

    private static bool IsPluginFile(string pRelativePath)
    {
        var path = GameInspectionSnapshot.NormalizeRelativePath(pRelativePath);
        return (path.StartsWith("js/plugins/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("www/js/plugins/", StringComparison.OrdinalIgnoreCase))
            && path.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SafePluginName(string pName)
    {
        return !string.IsNullOrWhiteSpace(pName)
            && pName.Length <= 255
            && !pName.Contains("..", StringComparison.Ordinal)
            && !pName.Contains('/')
            && !pName.Contains('\\')
            && pName.IndexOfAny(new[] { '\0', '\r', '\n' }) < 0;
    }

    private static string StableId(string pValue)
    {
        var builder = new StringBuilder(Math.Min(pValue.Length, 160));
        foreach (var character in pValue.ToLowerInvariant())
        {
            if (builder.Length >= 150) break;
            if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character is '-' or '_' or '.')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('_');
            }
        }
        return builder.Length == 0 ? "unnamed" : builder.ToString();
    }

    private static bool ContainsAny(string pText, params string[] pNeedles)
    {
        foreach (var needle in pNeedles)
        {
            if (pText.Contains(needle, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
