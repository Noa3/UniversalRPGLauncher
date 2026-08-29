using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Core;
using UniversalRPG.Wolf;

namespace UniversalRPG.Plugins;

/// <summary>
/// Built-in, compiled detection plugins. They inspect the bounded snapshot and
/// return metadata-only results. None of these plugins loads an imported DLL,
/// executable, Ruby script, JavaScript file, or native plugin.
/// </summary>
public static class BuiltInEnginePluginCatalog
{
    public static EngineDetectionRegistry CreateDetectionRegistry()
    {
        var registry = new EngineDetectionRegistry();
        foreach (var plugin in CreatePlugins())
        {
            var result = registry.Register(plugin);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Error?.Message ?? "Built-in detection registration failed.");
            }
        }
        return registry;
    }

    public static EnginePluginRegistry CreateRuntimeRegistry()
    {
        var registry = new EnginePluginRegistry();
        foreach (var plugin in CreatePlugins())
        {
            var result = registry.Register(plugin);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Error?.Message ?? "Built-in plugin registration failed.");
            }
        }
        return registry;
    }

    public static IReadOnlyList<BuiltInEnginePlugin> CreatePlugins() => new BuiltInEnginePlugin[]
    {
        new RpgMaker95Plugin(),
        new Dante98Plugin(),
        new RpgMaker2000Plugin(),
        new RpgMaker2003Plugin(),
        new RpgMakerXpPlugin(),
        new RpgMakerVxPlugin(),
        new RpgMakerVxAcePlugin(),
        new RpgMakerMvPlugin(),
        new RpgMakerMzPlugin(),
        new WolfRpgPlugin(),
        new RpgMakerUnitePlugin(),
    };
}

public abstract class BuiltInEnginePlugin : IEnginePlugin, IEngineDetectionPlugin
{
    protected BuiltInEnginePlugin(
        string pId,
        string pDisplayName,
        string pDescription,
        string pGeneration,
        int pPriority,
        PluginCapability pCapabilities)
    {
        Metadata = new EnginePluginMetadata
        {
            Id = pId,
            DisplayName = pDisplayName,
            Description = pDescription,
            Priority = pPriority,
            Capabilities = pCapabilities,
            SupportedEngines = new[]
            {
                new PluginEngineRange
                {
                    EngineId = pId,
                    Generation = pGeneration,
                },
            },
        };
        Generation = pGeneration;
    }

    public EnginePluginMetadata Metadata { get; }
    protected string Generation { get; }

    public abstract EngineDetectionProbe Detect(EngineInspectionContext pContext);

    public PluginProbeResult Probe(EnginePluginProbeContext pContext)
    {
        if (!pContext.Game.EngineId.Equals(Metadata.Id, StringComparison.Ordinal))
        {
            return PluginProbeResult.NoMatch("The detected engine ID does not match this plugin.");
        }
        return PluginProbeResult.Match(
            Math.Clamp(pContext.Game.DetectorScore * 250, 1, 1000),
            "The detected engine ID matches this plugin.");
    }

    public virtual PluginResult<IEngineRuntime> CreateRuntime(EnginePluginRuntimeContext pContext)
    {
        if ((Metadata.Capabilities & PluginCapability.Runtime) != 0)
        {
            return PluginResult<IEngineRuntime>.Succeeded(new EngineBootstrapRuntime(Metadata.Id, pContext.Game));
        }
        return PluginResult<IEngineRuntime>.Failed(PluginError.Create(
            PluginErrorCode.UnsupportedEngine,
            $"'{Metadata.DisplayName}' is detection-only; no runtime backend is registered.",
            Metadata.Id,
            "create"));
    }

    protected EngineDetectionProbe Match(
        GameInspectionSnapshot pSnapshot,
        int pScore,
        string pReason,
        IEnumerable<string> pEvidence,
        string pTitle = "",
        Version? pVersion = null,
        string pRtp = "",
        IEnumerable<PluginDiagnostic>? pDiagnostics = null)
    {
        var evidence = pEvidence.Distinct(StringComparer.Ordinal).OrderBy(pItem => pItem, StringComparer.Ordinal).ToArray();
        var status = (Metadata.Capabilities & PluginCapability.Runtime) != 0
            ? EngineDetectionStatus.Supported
            : EngineDetectionStatus.DetectionOnly;
        var candidate = new EngineDetectionCandidate
        {
            PluginId = Metadata.Id,
            EngineId = Metadata.Id,
            DisplayName = Metadata.DisplayName,
            Generation = Generation,
            EngineVersion = pVersion,
            Score = Math.Clamp(pScore, 0, 1000),
            Status = pSnapshot.IsMalformed ? EngineDetectionStatus.Malformed : status,
            Reason = pReason,
            Title = pTitle,
            RtpDependency = pRtp,
            Evidence = evidence,
            Diagnostics = pDiagnostics == null ? Array.Empty<PluginDiagnostic>() : pDiagnostics.ToArray(),
        };
        return EngineDetectionProbe.Match(candidate, candidate.Diagnostics);
    }

    protected static bool Has(GameInspectionSnapshot pSnapshot, string pPath)
        => pSnapshot.Contains(pPath);

    protected static InspectedGameFile? Find(GameInspectionSnapshot pSnapshot, string pFileName)
        => pSnapshot.Files.FirstOrDefault(pFile =>
            System.IO.Path.GetFileName(pFile.RelativePath).Equals(pFileName, StringComparison.OrdinalIgnoreCase));

    protected static bool HasExtension(GameInspectionSnapshot pSnapshot, string pExtension)
        => pSnapshot.Files.Any(pFile => pFile.RelativePath.EndsWith(pExtension, StringComparison.OrdinalIgnoreCase));

    protected static InspectedGameFile? FindByName(GameInspectionSnapshot pSnapshot, Func<string, bool> pPredicate)
        => pSnapshot.Files.FirstOrDefault(pFile => pPredicate(System.IO.Path.GetFileName(pFile.RelativePath)));

    protected static string Text(GameInspectionSnapshot pSnapshot, string pPath)
        => pSnapshot.ReadText(pPath);

    protected static string IniValue(GameInspectionSnapshot pSnapshot, string pName)
    {
        var file = Find(pSnapshot, "Game.ini") ?? Find(pSnapshot, "RPG_RT.ini");
        if (file == null || file.IsTruncated)
        {
            return "";
        }
        foreach (var line in new LegacyTextDecoder().Decode(file.Data).Split('\n'))
        {
            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }
            if (line[..separator].Trim().Equals(pName, StringComparison.OrdinalIgnoreCase))
            {
                return line[(separator + 1)..].Trim();
            }
        }
        return "";
    }

    protected static string IniText(GameInspectionSnapshot pSnapshot)
    {
        var file = Find(pSnapshot, "Game.ini") ?? Find(pSnapshot, "RPG_RT.ini");
        return file == null || file.IsTruncated ? "" : new LegacyTextDecoder().Decode(file.Data).ToLowerInvariant();
    }

    protected static string JsonTitle(GameInspectionSnapshot pSnapshot)
    {
        var file = Find(pSnapshot, "System.json");
        if (file == null || file.IsTruncated)
        {
            return "";
        }
        var text = System.Text.Encoding.UTF8.GetString(file.Data);
        // Parse as JSON (bounded: the file is already size-capped) so a nested
        // "gameTitle" key in an object cannot shadow the top-level property.
        try
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                MaxDepth = 64,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("gameTitle", out var title)
                && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString() ?? "";
            }
        }
        catch (JsonException)
        {
            // Bounded metadata only: malformed JSON yields an empty title.
        }
        return "";
    }

    protected static Version? RuntimeVersion(string pLibrary)
    {
        var match = Regex.Match(pLibrary, "rgss(?<major>[123])(?<minor>\\d{2})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success || !int.TryParse(match.Groups["major"].Value, out var major)
            || !int.TryParse(match.Groups["minor"].Value, out var minor))
        {
            return null;
        }
        return new Version(major, minor / 10, minor % 10);
    }

    protected static string Relative(GameInspectionSnapshot pSnapshot, string pName)
        => Find(pSnapshot, pName)?.RelativePath ?? pName;
}

public sealed class RpgMaker95Plugin : BuiltInEnginePlugin
{
    public RpgMaker95Plugin()
        : base(EnginePluginIds.RpgMaker95, "RPG Maker 95", "Detection-only RPG Maker 95 research boundary.", "rm95", 20, PluginCapability.Detection)
    {
    }

    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var snapshot = pContext.Snapshot;
        var rootFiles = snapshot.Files.Where(pFile => !pFile.RelativePath.Contains('/', StringComparison.Ordinal)).ToArray();
        var descriptor = rootFiles.Any(pFile => pFile.RelativePath.EndsWith(".rpg", StringComparison.OrdinalIgnoreCase));
        var companion = rootFiles.Any(pFile =>
        {
            var name = System.IO.Path.GetFileName(pFile.RelativePath);
            return name.EndsWith(".atr", StringComparison.OrdinalIgnoreCase)
                || (name.StartsWith("evt", StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                || name.Equals("strings.dat", StringComparison.OrdinalIgnoreCase)
                || name.Equals("swname.dat", StringComparison.OrdinalIgnoreCase);
        });
        if (!descriptor || !companion)
        {
            return EngineDetectionProbe.NoMatch("RPG Maker 95 requires a root .RPG descriptor and a documented companion data file.");
        }
        var evidence = rootFiles
            .Where(pFile => pFile.RelativePath.EndsWith(".rpg", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.EndsWith(".atr", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.StartsWith("evt", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.Equals("strings.dat", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.Equals("swname.dat", StringComparison.OrdinalIgnoreCase))
            .Select(pFile => $"signature: {pFile.RelativePath}")
            .OrderBy(pPath => pPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Match(snapshot, 850, "RPG Maker 95 descriptor and companion layout matched.", evidence);
    }
}

public sealed class Dante98Plugin : BuiltInEnginePlugin
{
    public Dante98Plugin()
        : base(EnginePluginIds.Dante98, "RPG Tsukūru Dante 98", "Detection-only PC-98 research boundary; no runtime or format aliasing.", "dante98", 21, PluginCapability.Detection)
    {
    }

    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var marker = pContext.Snapshot.Files.FirstOrDefault(pFile =>
            pFile.RelativePath.Equals("DANTE98.MRK", StringComparison.OrdinalIgnoreCase));
        if (marker == null)
        {
            return EngineDetectionProbe.NoMatch("No explicit Dante 98 project marker was found; PC-98 media formats remain unclassified.");
        }
        return Match(pContext.Snapshot, 900,
            "Explicit Dante 98 research marker matched; project remains detection-only.",
            new[] { "DANTE98.MRK (marker only; content is not executed)" });
    }
}

public abstract class LcfPlugin : BuiltInEnginePlugin
{
    private readonly string _engineToken;

    protected LcfPlugin(
        string pId,
        string pName,
        string pGeneration,
        string pToken,
        int pPriority,
        PluginCapability pCapabilities = PluginCapability.Detection | PluginCapability.Parsing)
        : base(pId, pName, $"Metadata-only {pName} detector.", pGeneration, pPriority, pCapabilities)
    {
        _engineToken = pToken;
    }

    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var snapshot = pContext.Snapshot;
        if (!Has(snapshot, "RPG_RT.ldb") || !Has(snapshot, "RPG_RT.lmt"))
        {
            return EngineDetectionProbe.NoMatch("The RPG_RT database and map-tree pair is incomplete.");
        }
        var ini = IniText(snapshot);
        var explicitEngine = ini.Contains("engineid=" + _engineToken, StringComparison.Ordinal)
            || ini.Contains("engine=" + _engineToken, StringComparison.Ordinal);
        var otherEngine = _engineToken == "rm2000"
            ? ini.Contains("engineid=rm2003", StringComparison.Ordinal) || ini.Contains("engine=rm2003", StringComparison.Ordinal)
            : ini.Contains("engineid=rm2000", StringComparison.Ordinal) || ini.Contains("engine=rm2000", StringComparison.Ordinal);
        if (otherEngine)
        {
            return EngineDetectionProbe.NoMatch("The project metadata identifies the other LCF generation.");
        }
        var evidence = new List<string> { Relative(snapshot, "RPG_RT.ldb"), Relative(snapshot, "RPG_RT.lmt") };
        var score = 600;
        if (snapshot.Files.Any(pFile => pFile.RelativePath.EndsWith(".lmu", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Map*.lmu");
            score += 120;
        }
        if (FindByName(snapshot, pName => pName.Equals("RPG_RT.exe", StringComparison.OrdinalIgnoreCase)) != null)
        {
            evidence.Add("RPG_RT.exe (inspected as data only)");
            score += 30;
        }
        if (explicitEngine)
        {
            evidence.Add($"Game metadata engine ID: {_engineToken}");
            score += 250;
        }
        else if (ini.Contains("[rpg_rt]", StringComparison.Ordinal))
        {
            evidence.Add("RPG_RT.ini [RPG_RT] section");
            score += 40;
        }
        return Match(snapshot, score, "LCF database/map-tree signatures matched.", evidence, IniValue(snapshot, "GameTitle"));
    }
}

public sealed class RpgMaker2000Plugin : LcfPlugin
{
    public RpgMaker2000Plugin()
        : base(EnginePluginIds.RpgMaker2000, "RPG Maker 2000", "rm2k", "rm2000", 50,
            PluginCapability.Detection | PluginCapability.Parsing | PluginCapability.Runtime | PluginCapability.SaveLoad | PluginCapability.Debugging) { }

    public override PluginResult<IEngineRuntime> CreateRuntime(EnginePluginRuntimeContext pContext)
        => PluginResult<IEngineRuntime>.Succeeded(new Rm2kEngineRuntime(Metadata.Id, pContext.Game));
}

public sealed class RpgMaker2003Plugin : LcfPlugin
{
    public RpgMaker2003Plugin()
        : base(EnginePluginIds.RpgMaker2003, "RPG Maker 2003", "rm2k3", "rm2003", 50,
            PluginCapability.Detection | PluginCapability.Parsing | PluginCapability.Runtime | PluginCapability.SaveLoad | PluginCapability.Debugging) { }

    public override PluginResult<IEngineRuntime> CreateRuntime(EnginePluginRuntimeContext pContext)
        => PluginResult<IEngineRuntime>.Succeeded(new Rm2kEngineRuntime(Metadata.Id, pContext.Game));
}

public abstract class RgssPlugin : BuiltInEnginePlugin
{
    private readonly string _runtimePrefix;
    private readonly string _dataExtension;
    private readonly string _archiveExtension;

    protected RgssPlugin(string pId, string pName, string pGeneration, string pPrefix, string pDataExtension, string pArchiveExtension, int pPriority)
        : base(pId, pName, $"Detection-only {pName} boundary until a bounded Ruby runtime is available.", pGeneration, pPriority, PluginCapability.Detection | PluginCapability.Parsing)
    {
        _runtimePrefix = pPrefix;
        _dataExtension = pDataExtension;
        _archiveExtension = pArchiveExtension;
    }


    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var snapshot = pContext.Snapshot;
        var ini = IniText(snapshot);
        var library = IniValue(snapshot, "Library");
        var runtime = library.StartsWith(_runtimePrefix, StringComparison.OrdinalIgnoreCase)
            || snapshot.Files.Any(pFile => System.IO.Path.GetFileName(pFile.RelativePath).StartsWith(_runtimePrefix, StringComparison.OrdinalIgnoreCase));
        var data = HasExtension(snapshot, _dataExtension);
        var archive = HasExtension(snapshot, _archiveExtension);
        if (!runtime && !data && !archive && !ini.Contains(_runtimePrefix, StringComparison.Ordinal))
        {
            return EngineDetectionProbe.NoMatch("No matching RGSS runtime, data, or archive signature was found.");
        }
        var evidence = new List<string>();
        var score = 250;
        if (runtime)
        {
            evidence.Add($"{_runtimePrefix} runtime library (inspected as data only)");
            score += 400;
        }
        if (data)
        {
            evidence.Add($"Data/*{_dataExtension}");
            score += 220;
        }
        if (archive)
        {
            evidence.Add($"*{_archiveExtension} archive (not decrypted or executed)");
            score += 150;
        }
        var version = RuntimeVersion(library);
        return Match(snapshot, score, "RGSS runtime and data signatures matched.", evidence, IniValue(snapshot, "Title"), version, IniValue(snapshot, "RTP") == "" ? IniValue(snapshot, "RTP1") : IniValue(snapshot, "RTP"));
    }
}

public sealed class RpgMakerXpPlugin : RgssPlugin
{
    public RpgMakerXpPlugin() : base(EnginePluginIds.RpgMakerXp, "RPG Maker XP", "xp", "rgss1", ".rxdata", ".rgssad", 40) { }
}

public sealed class RpgMakerVxPlugin : RgssPlugin
{
    public RpgMakerVxPlugin() : base(EnginePluginIds.RpgMakerVx, "RPG Maker VX", "vx", "rgss2", ".rvdata", ".rgss2a", 40) { }
}

public sealed class RpgMakerVxAcePlugin : RgssPlugin
{
    public RpgMakerVxAcePlugin() : base(EnginePluginIds.RpgMakerVxAce, "RPG Maker VX Ace", "vx-ace", "rgss3", ".rvdata2", ".rgss3a", 40) { }
}

public abstract class WebRpgPlugin : BuiltInEnginePlugin
{
    protected readonly string _runtimeFile;
    protected readonly string _runtimeLabel;

    protected WebRpgPlugin(string pId, string pName, string pGeneration, string pRuntimeFile, string pRuntimeLabel, int pPriority)
        : base(pId, pName, $"Detection-only {pName} boundary until an embedded JavaScript runtime is available.", pGeneration, pPriority, PluginCapability.Detection | PluginCapability.Parsing)
    {
        _runtimeFile = pRuntimeFile;
        _runtimeLabel = pRuntimeLabel;
    }

    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var snapshot = pContext.Snapshot;
        var hasIndex = snapshot.Files.Any(pFile => IsWebRootPath(pFile.RelativePath)
            && System.IO.Path.GetFileName(pFile.RelativePath).Equals("index.html", StringComparison.OrdinalIgnoreCase));
        var hasData = snapshot.Files.Any(pFile =>
            IsWebRootPath(pFile.RelativePath)
            && (pFile.RelativePath.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.Contains("/data/", StringComparison.OrdinalIgnoreCase)));
        var runtime = snapshot.Files.Any(pFile => IsWebRootPath(pFile.RelativePath)
            && System.IO.Path.GetFileName(pFile.RelativePath).Equals(_runtimeFile, StringComparison.OrdinalIgnoreCase));
        var nestedRuntime = snapshot.Files.Any(pFile => pFile.RelativePath.StartsWith("www/", StringComparison.OrdinalIgnoreCase)
            && pFile.RelativePath.EndsWith("/" + _runtimeFile, StringComparison.OrdinalIgnoreCase));
        if (!hasIndex || !hasData || !runtime)
        {
            return EngineDetectionProbe.NoMatch("The web runtime, index, and data signatures are incomplete.");
        }
        if (!ValidateMetadata(snapshot, out var metadataFailure))
        {
            return EngineDetectionProbe.NoMatch(metadataFailure);
        }
        var evidence = new List<string> { "index.html", "data/", $"JavaScript runtime: {_runtimeLabel}" };
        var score = 850;
        if (nestedRuntime)
        {
            evidence.Add("www/ web-game root");
            score += 30;
        }
        var title = JsonTitle(snapshot);
        var package = Find(snapshot, "package.json");
        var version = package == null || package.IsTruncated
            ? null
            : ExtractVersion(System.Text.Encoding.UTF8.GetString(package.Data));
        version ??= ExtractRuntimeVersion(snapshot, _runtimeFile);
        return Match(snapshot, score, $"{_runtimeLabel} and web-game layout matched.", evidence, title, version);
    }

    protected virtual bool ValidateMetadata(GameInspectionSnapshot pSnapshot, out string pFailure)
    {
        pFailure = "";
        return true;
    }

    protected static bool IsWebRootPath(string pPath)
    {
        return !pPath.Contains('/', StringComparison.Ordinal)
            || pPath.StartsWith("js/", StringComparison.OrdinalIgnoreCase)
            || pPath.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
            || pPath.StartsWith("www/", StringComparison.OrdinalIgnoreCase);
    }

    protected static Version? ExtractRuntimeVersion(GameInspectionSnapshot pSnapshot, string pRuntimeFile)
    {
        var file = pSnapshot.Files.FirstOrDefault(pItem =>
            Path.GetFileName(pItem.RelativePath).Equals(pRuntimeFile, StringComparison.OrdinalIgnoreCase));
        if (file == null || file.Data.Length == 0)
        {
            return null;
        }
        var text = System.Text.Encoding.UTF8.GetString(file.Data);
        var match = Regex.Match(text, $"{Regex.Escape(pRuntimeFile)}\\s+v(?<version>\\d+(?:\\.\\d+){{1,3}})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && Version.TryParse(match.Groups["version"].Value, out var version) ? version : null;
    }

    protected static Version? ExtractVersion(string pText)
    {
        var match = Regex.Match(pText, "\\\"version\\\"\\s*:\\s*\\\"(?<version>\\d+(?:\\.\\d+){1,3})", RegexOptions.CultureInvariant);
        return match.Success && Version.TryParse(match.Groups["version"].Value, out var version) ? version : null;
    }
}

public sealed class RpgMakerMvPlugin : WebRpgPlugin
{
    public RpgMakerMvPlugin() : base(EnginePluginIds.RpgMakerMv, "RPG Maker MV", "mv", "rpg_core.js", "js/rpg_core.js", 30) { }

    /// <summary>
    /// Extracts the small, useful subset of MV System.json metadata without
    /// loading the browser runtime or evaluating plugin JavaScript.
    /// </summary>
    public static MvMetadataResult? ExtractMetadata(GameInspectionSnapshot pSnapshot)
    {
        var system = pSnapshot.Files.FirstOrDefault(pFile =>
            pFile.RelativePath.Equals("data/System.json", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.Equals("www/data/System.json", StringComparison.OrdinalIgnoreCase));
        if (system == null || system.IsTruncated || system.Data.Length == 0 || system.Data.Length > 512 * 1024)
        {
            return null;
        }

        var text = System.Text.Encoding.UTF8.GetString(system.Data);
        if (!text.TrimStart().StartsWith("{", StringComparison.Ordinal)
            || !text.TrimEnd().EndsWith("}", StringComparison.Ordinal))
        {
            return null;
        }

        // Parse as JSON (bounded by the 512 KiB cap above) so a nested
        // "gameTitle" key inside an object cannot shadow the top-level field.
        var title = "";
        try
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                MaxDepth = 64,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("gameTitle", out var gameTitle)
                && gameTitle.ValueKind == JsonValueKind.String)
            {
                title = gameTitle.GetString() ?? "";
            }
        }
        catch (JsonException)
        {
            // Bounded metadata only: malformed JSON yields an empty title.
        }

        var encrypted = pSnapshot.Files.Any(pFile =>
            pFile.RelativePath.EndsWith(".rpgmvp", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.EndsWith(".rpgmvo", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.EndsWith(".rpgmvm", StringComparison.OrdinalIgnoreCase));
        return new MvMetadataResult
        {
            GameTitle = title,
            HasEncryptedFiles = encrypted,
            Diagnostics = encrypted
                ? new[] { "Encrypted MV assets detected; files remain metadata-only." }
                : Array.Empty<string>(),
        };
    }
}

public sealed class MvMetadataResult
{
    public string GameTitle { get; init; } = "";
    public bool HasEncryptedFiles { get; init; }
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Bounded metadata extracted from MZ's data/System.json without executing any JavaScript.
/// Fields are read with explicit size limits and validated against known MZ structure.
/// </summary>
public sealed class MzMetadataResult
{
    public string GameTitle { get; init; } = "";
    public string SystemVersion { get; init; } = "";
    public IReadOnlyList<string> AudioBrowsers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MainCommands { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ContextCommands { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WindowSkinTypes { get; init; } = Array.Empty<string>();
    public bool HasEncryptedFiles { get; init; }
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
}

public sealed class RpgMakerMzPlugin : WebRpgPlugin
{
    public RpgMakerMzPlugin() : base(EnginePluginIds.RpgMakerMz, "RPG Maker MZ", "mz", "rmmz_core.js", "js/rmmz_core.js", 30) { }

    private const int MaxSystemJsonBytes = 512 * 1024; // 512 KiB cap

    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var snapshot = pContext.Snapshot;
        var hasIndex = snapshot.Files.Any(pFile => IsWebRootPath(pFile.RelativePath)
            && System.IO.Path.GetFileName(pFile.RelativePath).Equals("index.html", StringComparison.OrdinalIgnoreCase));
        var hasData = snapshot.Files.Any(pFile =>
            IsWebRootPath(pFile.RelativePath)
            && (pFile.RelativePath.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
                || pFile.RelativePath.Contains("/data/", StringComparison.OrdinalIgnoreCase)));
        var runtime = snapshot.Files.Any(pFile => IsWebRootPath(pFile.RelativePath)
            && System.IO.Path.GetFileName(pFile.RelativePath).Equals(_runtimeFile, StringComparison.OrdinalIgnoreCase));
        var nestedRuntime = snapshot.Files.Any(pFile => pFile.RelativePath.StartsWith("www/", StringComparison.OrdinalIgnoreCase)
            && pFile.RelativePath.EndsWith("/" + _runtimeFile, StringComparison.OrdinalIgnoreCase));
        if (!hasIndex || !hasData || !runtime)
        {
            return EngineDetectionProbe.NoMatch("The web runtime, index, and data signatures are incomplete.");
        }
        if (!ValidateMetadata(snapshot, out var metadataFailure))
        {
            return EngineDetectionProbe.NoMatch(metadataFailure);
        }

        var evidence = new List<string> { "index.html", "data/", $"JavaScript runtime: {_runtimeLabel}" };
        var score = 850;
        if (nestedRuntime)
        {
            evidence.Add("www/ web-game root");
            score += 30;
        }

        var title = JsonTitle(snapshot);
        var package = Find(snapshot, "package.json");
        var version = package == null || package.IsTruncated
            ? null
            : ExtractVersion(System.Text.Encoding.UTF8.GetString(package.Data));
        version ??= ExtractRuntimeVersion(snapshot, _runtimeFile);
        return Match(snapshot, score, $"{_runtimeLabel} and web-game layout matched.", evidence, title, version);
    }

    protected override bool ValidateMetadata(GameInspectionSnapshot pSnapshot, out string pFailure)
    {
        pFailure = "";
        var managers = pSnapshot.Files.Any(pFile =>
            pFile.RelativePath.Equals("js/rmmz_managers.js", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.Equals("www/js/rmmz_managers.js", StringComparison.OrdinalIgnoreCase));
        if (!managers)
        {
            pFailure = "The MZ manager runtime signature is missing.";
            return false;
        }

        var systemJson = Find(pSnapshot, "System.json");
        if (systemJson == null)
        {
            return true; // optional metadata; detection can still succeed without it
        }
        if (systemJson.IsTruncated)
        {
            pFailure = "data/System.json is truncated beyond the bounded inspection limit.";
            return false;
        }
        if (systemJson.Data.Length > MaxSystemJsonBytes)
        {
            pFailure = "data/System.json exceeds the bounded inspection limit.";
            return false;
        }
        var text = System.Text.Encoding.UTF8.GetString(systemJson.Data);
        if (!text.StartsWith("{", StringComparison.Ordinal) || !text.EndsWith("}", StringComparison.Ordinal))
        {
            pFailure = "data/System.json has invalid JSON boundaries.";
            return false;
        }
        if (text.Length > MaxSystemJsonBytes)
        {
            pFailure = "data/System.json exceeds the bounded inspection limit.";
            return false;
        }
        return true;
    }

    /// <summary>
    /// Extract bounded typed metadata from a System.json snapshot. Never executes JavaScript.
    /// Returns null if the snapshot is missing or truncated beyond safe reading.
    /// </summary>
    public static MzMetadataResult? ExtractMetadata(GameInspectionSnapshot pSnapshot)
    {
        var systemJson = Find(pSnapshot, "System.json");
        if (systemJson == null || systemJson.IsTruncated)
        {
            return null;
        }
        var data = systemJson.Data;
        if (data.Length == 0 || data.Length > MaxSystemJsonBytes)
        {
            return null;
        }
        var text = System.Text.Encoding.UTF8.GetString(data);
        if (text.Length == 0 || !text.StartsWith("{", StringComparison.Ordinal) || !text.EndsWith("}", StringComparison.Ordinal))
        {
            return null;
        }

        var title = ExtractJsonString(text, "gameTitle");
        var version = ExtractJsonString(text, "systemVersion");
        var audioBrowsers = ExtractJsonStringArray(text, "audioBrowsers");

        var diagnostics = new List<string>();
        var hasEncrypted = pSnapshot.Files.Any(pFile =>
            pFile.RelativePath.EndsWith(".encrypted", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.Contains("/encrypted/", StringComparison.OrdinalIgnoreCase));

        if (hasEncrypted)
        {
            diagnostics.Add("Encrypted game files detected; metadata is from unencrypted System.json only.");
        }

        // Check for known MZ structure indicators
        var hasJsDir = pSnapshot.Files.Any(pFile => pFile.RelativePath.StartsWith("js/", StringComparison.OrdinalIgnoreCase));
        var hasDataDir = pSnapshot.Files.Any(pFile => pFile.RelativePath.StartsWith("data/", StringComparison.OrdinalIgnoreCase));
        var hasHtml = pSnapshot.Files.Any(pFile =>
            pFile.RelativePath.Equals("index.html", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase));

        if (!hasJsDir) diagnostics.Add("No js/ directory found in snapshot.");
        if (!hasDataDir) diagnostics.Add("No data/ directory found in snapshot.");
        if (!hasHtml) diagnostics.Add("No index.html found in snapshot.");

        return new MzMetadataResult
        {
            GameTitle = title ?? "",
            SystemVersion = version ?? "",
            AudioBrowsers = audioBrowsers,
            Diagnostics = diagnostics,
            HasEncryptedFiles = hasEncrypted,
        };
    }

    private static string? ExtractJsonString(string pText, string pKey)
    {
        var pattern = $"\"{pKey}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";
        var match = System.Text.RegularExpressions.Regex.Match(pText, pattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static IReadOnlyList<string> ExtractJsonStringArray(string pText, string pKey)
    {
        var pattern = $"\"{pKey}\"\\s*:\\s*\\[([^\\]]*)\\]";
        var match = System.Text.RegularExpressions.Regex.Match(pText, pattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return Array.Empty<string>();
        }
        var arrText = match.Groups[1].Value;
        var result = new List<string>();
        var inner = System.Text.RegularExpressions.Regex.Matches(arrText, "\"((?:[^\"\\\\]|\\\\.)*)\"");
        foreach (System.Text.RegularExpressions.Match m in inner)
        {
            result.Add(m.Groups[1].Value);
        }
        return result;
    }
}

public class WolfRpgPlugin : BuiltInEnginePlugin
{
    public WolfRpgPlugin() : base(EnginePluginIds.WolfRpg, "WOLF RPG Editor", "Bounded WOLF plain-data runtime slice.", "wolf", 25, PluginCapability.Detection | PluginCapability.Parsing | PluginCapability.Runtime) { }

    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var snapshot = pContext.Snapshot;
        var gameData = FindByName(snapshot, pName => pName.Equals("Game.dat", StringComparison.OrdinalIgnoreCase));
        var basicData = snapshot.Files.Any(pFile => pFile.RelativePath.Contains("BasicData", StringComparison.OrdinalIgnoreCase));
        var mapData = snapshot.Files.Any(pFile => pFile.RelativePath.Contains("MapData", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.EndsWith(".mps", StringComparison.OrdinalIgnoreCase));
        if (gameData == null || !basicData)
        {
            return EngineDetectionProbe.NoMatch("The WOLF Game.dat and BasicData signatures are incomplete.");
        }
        var evidence = new List<string> { gameData.RelativePath, "BasicData" };
        var score = 760;
        if (mapData)
        {
            evidence.Add("MapData");
            score += 100;
        }
        return Match(snapshot, score, "WOLF RPG Editor data signatures matched; only explicit unencrypted plain data is loadable.", evidence,
            pDiagnostics: new[] { PluginDiagnostic.Warning("wolf.plain-data-only", "Protected or proprietary WOLF data is not decrypted or bypassed.", Metadata.Id) });
    }

    public override PluginResult<IEngineRuntime> CreateRuntime(EnginePluginRuntimeContext pContext)
        => PluginResult<IEngineRuntime>.Succeeded(new WolfEngineRuntime(Metadata.Id, pContext.Game));
}

public sealed class RpgMakerUnitePlugin : BuiltInEnginePlugin
{
    public RpgMakerUnitePlugin() : base(EnginePluginIds.RpgMakerUnite, "RPG Maker Unite / Unity", "Metadata-only Unity/RPG Maker Unite detector.", "unite", 10, PluginCapability.Detection) { }

    public override EngineDetectionProbe Detect(EngineInspectionContext pContext)
    {
        var snapshot = pContext.Snapshot;
        var unityPlayer = snapshot.Files.Any(pFile => System.IO.Path.GetFileName(pFile.RelativePath).Equals("UnityPlayer.dll", StringComparison.OrdinalIgnoreCase));
        var gameAssembly = snapshot.Files.Any(pFile => System.IO.Path.GetFileName(pFile.RelativePath).Equals("GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
        var globalManagers = snapshot.Files.Any(pFile => pFile.RelativePath.EndsWith("/globalgamemanagers", StringComparison.OrdinalIgnoreCase));
        var dataDirectory = snapshot.Files.Any(pFile => pFile.RelativePath.Contains("_Data/", StringComparison.OrdinalIgnoreCase));
        if ((!unityPlayer && !gameAssembly) || !dataDirectory)
        {
            return EngineDetectionProbe.NoMatch("No Unity player and data-directory pair was found.");
        }
        var evidence = new List<string>();
        if (unityPlayer) evidence.Add("UnityPlayer.dll (inspected as data only)");
        if (gameAssembly) evidence.Add("GameAssembly.dll (inspected as data only)");
        if (globalManagers) evidence.Add("globalgamemanagers");
        evidence.Add("<game>_Data/");
        return Match(snapshot, 500, "Unity export signatures found; the export cannot prove that the source project was RPG Maker Unite.", evidence,
            pDiagnostics: new[] { PluginDiagnostic.Warning("unite.not-provable", "Arbitrary Unity exports are not treated as playable RPG Maker Unite projects.", Metadata.Id) });
    }
}

/// <summary>
/// Bounded metadata from an MZ game's data/ directory (Actors.json, MapInfos.json),
/// decoded without executing any JavaScript. Entry counts and names only.
/// </summary>
public sealed class MzDataDirectoryResult
{
    public const int MaxDataJsonBytes = 2048 * 1024;
    public const int MaxActorEntries = 9999;
    public const int MaxMapEntries = 9999;
    public const int MaxListedNames = 32;
    public const int MaxNameLength = 64;
    public const int MaxMapFiles = 9999;
    private static readonly string[] EncryptedExtensions =
    {
        ".rpgmvp", ".rpgmvo", ".rpgmvm", ".png_", ".ogg_", ".m4a_", ".encrypted",
    };
    private static readonly string[] OptionalDatabaseSections =
    {
        "Classes", "Skills", "Items", "Weapons", "Armors", "Enemies", "Troops",
    };

    public int ActorCount { get; init; }
    public IReadOnlyList<string> ActorNames { get; init; } = Array.Empty<string>();
    public int MapCount { get; init; }
    public IReadOnlyList<string> MapNames { get; init; } = Array.Empty<string>();

    /// <summary>Entry counts for present optional database JSON sections.</summary>
    public IReadOnlyDictionary<string, int> SectionCounts { get; init; } =
        new Dictionary<string, int>();

    /// <summary>Entries in System.json "switches" name array (0 when absent).</summary>
    public int SwitchNameCount { get; init; }

    /// <summary>Entries in System.json "variables" name array (0 when absent).</summary>
    public int VariableNameCount { get; init; }

    /// <summary>Number of physical data/Map###.json files in the snapshot.</summary>
    public int MapFileCount { get; init; }

    public bool HasEncryptedAssets { get; init; }
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Extract bounded data-directory metadata from a validated MZ snapshot.
    /// Returns null when the snapshot lacks the MZ runtime signature or the
    /// requested files are truncated/oversized; partial results keep diagnostics.
    /// </summary>
    public static MzDataDirectoryResult? Extract(GameInspectionSnapshot pSnapshot)
    {
        var runtime = pSnapshot.Files.FirstOrDefault(pFile =>
            pFile.RelativePath.EndsWith("/rmmz_core.js", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.Equals("rmmz_core.js", StringComparison.OrdinalIgnoreCase)
            || pFile.RelativePath.EndsWith("/rmmz_managers.js", StringComparison.OrdinalIgnoreCase));
        if (runtime == null)
        {
            return null;
        }

        var diagnostics = new List<string>();
        var actors = ReadNamedEntries(pSnapshot, "data/Actors.json", MaxActorEntries, MaxDataJsonBytes, "Actors", diagnostics);
        var maps = ReadNamedEntries(pSnapshot, "data/MapInfos.json", MaxMapEntries, MaxDataJsonBytes, "MapInfos", diagnostics);
        var sectionCounts = CountOptionalSections(pSnapshot, diagnostics);
        var systemCounts = ReadSystemNameCounts(pSnapshot, diagnostics);
        var mapFileCount = CountMapFiles(pSnapshot, diagnostics);
        var encrypted = pSnapshot.Files.Any(pFile => EncryptedExtensions.Any(pExtension =>
            pFile.RelativePath.EndsWith(pExtension, StringComparison.OrdinalIgnoreCase)));
        if (encrypted)
        {
            diagnostics.Add("Encrypted assets detected (.rpgmv*); names come from unencrypted JSON only.");
        }
        if (!pSnapshot.Contains("data/Actors.json"))
        {
            diagnostics.Add("data/Actors.json not found in snapshot.");
        }
        if (!pSnapshot.Contains("data/MapInfos.json"))
        {
            diagnostics.Add("data/MapInfos.json not found in snapshot.");
        }

        return new MzDataDirectoryResult
        {
            ActorCount = actors.Count,
            ActorNames = actors.Names,
            MapCount = maps.Count,
            MapNames = maps.Names,
            SectionCounts = sectionCounts,
            SwitchNameCount = systemCounts.switches,
            VariableNameCount = systemCounts.variables,
            MapFileCount = mapFileCount,
            HasEncryptedAssets = encrypted,
            Diagnostics = diagnostics,
        };
    }

    private static IReadOnlyDictionary<string, int> CountOptionalSections(
        GameInspectionSnapshot pSnapshot, List<string> pDiagnostics)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var section in OptionalDatabaseSections)
        {
            var path = $"data/{section}.json";
            if (!pSnapshot.TryGet(path, out var file))
            {
                continue; // absent sections are normal for trimmed games
            }
            if (file.IsTruncated)
            {
                pDiagnostics.Add($"{path} is truncated beyond the bounded inspection limit.");
                continue;
            }
            if (file.Data.Length > MaxDataJsonBytes)
            {
                pDiagnostics.Add($"{path} exceeds the bounded inspection limit ({MaxDataJsonBytes} bytes).");
                continue;
            }
            var count = CountJsonArrayEntries(System.Text.Encoding.UTF8.GetString(file.Data));
            if (count < 0)
            {
                pDiagnostics.Add($"{path} contains malformed JSON; skipped.");
                continue;
            }
            if (count > MaxActorEntries)
            {
                pDiagnostics.Add($"{section}: entry count {count} exceeds the bounded limit ({MaxActorEntries}); count reported as the limit.");
                count = MaxActorEntries;
            }
            counts[section] = count;
        }
        return counts;
    }

    private static (int switches, int variables) ReadSystemNameCounts(
        GameInspectionSnapshot pSnapshot, List<string> pDiagnostics)
    {
        if (!pSnapshot.TryGet("data/System.json", out var file) || file.IsTruncated
            || file.Data.Length == 0 || file.Data.Length > 512 * 1024)
        {
            return (0, 0);
        }
        var text = System.Text.Encoding.UTF8.GetString(file.Data);
        var parsed = Godot.Json.ParseString(text);
        if (parsed.VariantType != Godot.Variant.Type.Dictionary)
        {
            return (0, 0); // detection already validated boundaries; stay quiet here
        }
        var dictionary = parsed.AsGodotDictionary();
        return (CountNameArray(dictionary, "switches", pDiagnostics),
            CountNameArray(dictionary, "variables", pDiagnostics));
    }

    private static int CountNameArray(Godot.Collections.Dictionary pSystem, string pKey, List<string> pDiagnostics)
    {
        if (!pSystem.TryGetValue(pKey, out var arrayVariant)
            || arrayVariant.VariantType != Godot.Variant.Type.Array)
        {
            return 0;
        }
        var count = arrayVariant.AsGodotArray().Count;
        if (count > MaxActorEntries)
        {
            pDiagnostics.Add($"System.json {pKey}: entry count {count} exceeds the bounded limit ({MaxActorEntries}); count reported as the limit.");
            return MaxActorEntries;
        }
        return count;
    }

    private static int CountMapFiles(GameInspectionSnapshot pSnapshot, List<string> pDiagnostics)
    {
        var count = 0;
        foreach (var file in pSnapshot.Files)
        {
            var name = System.IO.Path.GetFileName(file.RelativePath);
            var inData = file.RelativePath.EndsWith(name, StringComparison.OrdinalIgnoreCase)
                && (file.RelativePath.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
                    || file.RelativePath.Contains("/data/", StringComparison.OrdinalIgnoreCase));
            if (!inData || !name.StartsWith("Map", StringComparison.OrdinalIgnoreCase)
                || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var stem = name[3..^5];
            if (stem.Length is < 3 or > 4 || !int.TryParse(stem, out _))
            {
                continue;
            }
            count++;
        }
        if (count > MaxMapFiles)
        {
            pDiagnostics.Add($"Map file count {count} exceeds the bounded limit ({MaxMapFiles}).");
            count = MaxMapFiles;
        }
        return count;
    }

    /// <summary>Counts entries of a JSON array text; -1 when malformed or not an array.</summary>
    private static int CountJsonArrayEntries(string pText)
    {
        if (!pText.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            return -1;
        }
        var parsed = Godot.Json.ParseString(pText);
        if (parsed.VariantType != Godot.Variant.Type.Array)
        {
            return -1;
        }
        return parsed.AsGodotArray().Count;
    }

    private static (int Count, IReadOnlyList<string> Names) ReadNamedEntries(
        GameInspectionSnapshot pSnapshot, string pPath, int pMaxEntries, int pMaxBytes,
        string pLabel, List<string> pDiagnostics)
    {
        if (!pSnapshot.TryGet(pPath, out var file))
        {
            return (0, Array.Empty<string>());
        }
        if (file.IsTruncated)
        {
            pDiagnostics.Add($"{pPath} is truncated beyond the bounded inspection limit.");
            return (0, Array.Empty<string>());
        }
        if (file.Data.Length > pMaxBytes)
        {
            pDiagnostics.Add($"{pPath} exceeds the bounded inspection limit ({pMaxBytes} bytes).");
            return (0, Array.Empty<string>());
        }

        var text = System.Text.Encoding.UTF8.GetString(file.Data);
        var trimmed = text.TrimStart();
        if (!trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            pDiagnostics.Add($"{pPath} is not a bounded JSON array.");
            return (0, Array.Empty<string>());
        }

        var parsed = Godot.Json.ParseString(text);
        if (parsed.VariantType != Godot.Variant.Type.Array)
        {
            pDiagnostics.Add($"{pPath} contains malformed JSON; skipped.");
            return (0, Array.Empty<string>());
        }

        var array = parsed.AsGodotArray();
        if (array.Count > pMaxEntries)
        {
            pDiagnostics.Add($"{pLabel}: entry count {array.Count} exceeds the bounded limit ({pMaxEntries}); count reported as the limit.");
        }

        var count = Math.Min(array.Count, pMaxEntries);
        var names = new List<string>();
        foreach (var element in array)
        {
            if (names.Count >= MaxListedNames)
            {
                break;
            }
            if (element.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }
            var dictionary = element.AsGodotDictionary();
            if (!dictionary.TryGetValue("name", out var nameVariant))
            {
                continue;
            }
            var name = nameVariant.AsString();
            if (name.Length > MaxNameLength)
            {
                name = name[..MaxNameLength];
            }
            names.Add(name);
        }
        return (count, names);
    }
}
