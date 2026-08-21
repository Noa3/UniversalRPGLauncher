using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace UniversalRPG.Plugins;

/// <summary>
/// Hard limits applied before any engine plugin sees imported content. Detection
/// reads metadata and bounded prefixes only; it never extracts or executes files.
/// </summary>
public sealed class GameInspectionLimits
{
    public int MaxDepth { get; init; } = 4;
    public int MaxEntries { get; init; } = 4096;
    public long MaxFileBytes { get; init; } = 1024 * 1024;
    public long MaxArchiveBytes { get; init; } = 64 * 1024 * 1024;
    public int MaxArchiveEntryBytes { get; init; } = 1024 * 1024;
    public int MaxPrefixBytes { get; init; } = 4096;

    public PluginOperationResult Validate()
    {
        if (MaxDepth < 0 || MaxDepth > 16 || MaxEntries <= 0 || MaxEntries > 100_000
            || MaxFileBytes <= 0 || MaxArchiveBytes <= 0 || MaxArchiveEntryBytes <= 0
            || MaxPrefixBytes <= 0)
        {
            return PluginOperationResult.Failed(PluginError.Create(
                PluginErrorCode.InvalidMetadata,
                "Game inspection limits are outside the safe supported range.",
                pPhase: "inspect"));
        }
        return PluginOperationResult.Succeeded();
    }
}

public sealed class EngineInspectionDiagnostic
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public bool IsError { get; init; }

    public static EngineInspectionDiagnostic Info(string pCode, string pMessage) => new()
    {
        Code = pCode,
        Message = pMessage,
    };

    public static EngineInspectionDiagnostic Error(string pCode, string pMessage) => new()
    {
        Code = pCode,
        Message = pMessage,
        IsError = true,
    };
}

public sealed class InspectedGameFile
{
    internal InspectedGameFile(string pRelativePath, long pLength, byte[] pData, bool pTruncated, bool pArchiveEntry)
    {
        RelativePath = pRelativePath;
        Length = pLength;
        Data = pData;
        IsTruncated = pTruncated;
        IsArchiveEntry = pArchiveEntry;
    }

    public string RelativePath { get; }
    public long Length { get; }
    public byte[] Data { get; }
    public bool IsTruncated { get; }
    public bool IsArchiveEntry { get; }
}

public sealed class GameInspectionSnapshot
{
    private readonly Dictionary<string, InspectedGameFile> _files;

    internal GameInspectionSnapshot(
        string pSourcePath,
        bool pArchive,
        bool pMalformed,
        Dictionary<string, InspectedGameFile> pFiles,
        List<EngineInspectionDiagnostic> pDiagnostics)
    {
        SourcePath = pSourcePath;
        IsArchive = pArchive;
        IsMalformed = pMalformed;
        _files = pFiles;
        Diagnostics = pDiagnostics;
    }

    public string SourcePath { get; }
    public bool IsArchive { get; }
    public bool IsMalformed { get; }
    public IReadOnlyList<InspectedGameFile> Files => _files.Values
        .OrderBy(pFile => pFile.RelativePath, StringComparer.OrdinalIgnoreCase)
        .ThenBy(pFile => pFile.RelativePath, StringComparer.Ordinal)
        .ToArray();
    public IReadOnlyList<EngineInspectionDiagnostic> Diagnostics { get; }

    public bool Contains(string pRelativePath)
    {
        return TryGet(pRelativePath, out _);
    }

    public bool TryGet(string pRelativePath, out InspectedGameFile pFile)
    {
        var key = NormalizeRelativePath(pRelativePath);
        return _files.TryGetValue(key, out pFile!);
    }

    public IEnumerable<InspectedGameFile> Where(Func<InspectedGameFile, bool> pPredicate)
    {
        return Files.Where(pPredicate);
    }

    public string ReadText(string pRelativePath)
    {
        if (!TryGet(pRelativePath, out var file) || file.IsTruncated)
        {
            return "";
        }
        try
        {
            return DecodeText(file.Data);
        }
        catch
        {
            return "";
        }
    }

    public string FindPath(string pFileName)
    {
        return Files.FirstOrDefault(pFile =>
            Path.GetFileName(pFile.RelativePath).Equals(pFileName, StringComparison.OrdinalIgnoreCase))?.RelativePath ?? "";
    }

    public static string NormalizeRelativePath(string pPath)
    {
        return pPath.Replace('\\', '/').TrimStart('/');
    }

    private static string DecodeText(byte[] pData)
    {
        if (pData.Length >= 3 && pData[0] == 0xef && pData[1] == 0xbb && pData[2] == 0xbf)
        {
            return System.Text.Encoding.UTF8.GetString(pData, 3, pData.Length - 3);
        }
        return System.Text.Encoding.UTF8.GetString(pData);
    }
}

public static class SafeGameInspector
{
    public static PluginResult<GameInspectionSnapshot> Inspect(
        string pSourcePath,
        GameInspectionLimits? pLimits = null)
    {
        var limits = pLimits ?? new GameInspectionLimits();
        var limitResult = limits.Validate();
        if (!limitResult.Success)
        {
            return PluginResult<GameInspectionSnapshot>.Failed(limitResult.Error!);
        }
        if (string.IsNullOrWhiteSpace(pSourcePath))
        {
            return PluginResult<GameInspectionSnapshot>.Failed(PluginError.Create(
                PluginErrorCode.InvalidGame,
                "An imported folder or supported archive is required.",
                pPhase: "inspect"));
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(pSourcePath);
        }
        catch (Exception exception)
        {
            return PluginResult<GameInspectionSnapshot>.Failed(PluginError.Create(
                PluginErrorCode.InvalidGame,
                "The imported path is not valid.",
                pPhase: "inspect",
                pException: exception));
        }

        try
        {
            if (Directory.Exists(fullPath))
            {
                return InspectDirectory(fullPath, limits);
            }
            if (File.Exists(fullPath))
            {
                return InspectArchive(fullPath, limits);
            }
        }
        catch (Exception exception)
        {
            return PluginResult<GameInspectionSnapshot>.Failed(PluginError.Create(
                PluginErrorCode.InvalidGame,
                "The imported source could not be inspected safely.",
                pPhase: "inspect",
                pException: exception));
        }

        return PluginResult<GameInspectionSnapshot>.Failed(PluginError.Create(
            PluginErrorCode.InvalidGame,
            "The imported folder or archive does not exist.",
            pPhase: "inspect"));
    }

    private static PluginResult<GameInspectionSnapshot> InspectDirectory(string pRoot, GameInspectionLimits pLimits)
    {
        var files = new Dictionary<string, InspectedGameFile>(StringComparer.OrdinalIgnoreCase);
        var diagnostics = new List<EngineInspectionDiagnostic>();
        var malformed = false;
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((pRoot, 0));

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            DirectoryInfo directory;
            try
            {
                directory = new DirectoryInfo(current.Path);
                if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    diagnostics.Add(EngineInspectionDiagnostic.Info(
                        "inspect.reparse-skipped", $"Skipped reparse directory '{current.Path}'."));
                    continue;
                }

                var children = directory.EnumerateFileSystemInfos()
                    .OrderBy(pChild => pChild.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(pChild => pChild.Name, StringComparer.Ordinal)
                    .ToArray();
                foreach (var child in children)
                {
                    if (files.Count >= pLimits.MaxEntries)
                    {
                        malformed = true;
                        diagnostics.Add(EngineInspectionDiagnostic.Error(
                            "inspect.entry-limit", "The imported folder exceeded the inspection entry limit."));
                        return PluginResult<GameInspectionSnapshot>.Succeeded(
                            CreateSnapshot(pRoot, false, malformed, files, diagnostics));
                    }
                    if (child.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        diagnostics.Add(EngineInspectionDiagnostic.Info(
                            "inspect.reparse-skipped", $"Skipped reparse entry '{child.Name}'."));
                        continue;
                    }
                    if (child.Name.StartsWith(".", StringComparison.Ordinal)
                        || child.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        diagnostics.Add(EngineInspectionDiagnostic.Info(
                            "inspect.hidden-skipped", $"Skipped hidden entry '{child.Name}'."));
                        continue;
                    }
                    if (child is DirectoryInfo childDirectory)
                    {
                        if (current.Depth < pLimits.MaxDepth)
                        {
                            pending.Push((childDirectory.FullName, current.Depth + 1));
                        }
                        else
                        {
                            malformed = true;
                            diagnostics.Add(EngineInspectionDiagnostic.Error(
                                "inspect.depth-limit", $"Inspection depth limit reached at '{childDirectory.Name}'."));
                        }
                        continue;
                    }
                    if (child is FileInfo childFile)
                    {
                        var relative = Path.GetRelativePath(pRoot, childFile.FullName).Replace('\\', '/');
                        AddFile(files, relative, childFile.Length, childFile.FullName, false, pLimits, diagnostics);
                    }
                }
            }
            catch (Exception exception)
            {
                malformed = true;
                diagnostics.Add(EngineInspectionDiagnostic.Error(
                    "inspect.read-failed", $"Could not inspect '{current.Path}': {exception.GetType().Name}."));
            }
        }

        return PluginResult<GameInspectionSnapshot>.Succeeded(
            CreateSnapshot(pRoot, false, malformed, files, diagnostics));
    }

    private static PluginResult<GameInspectionSnapshot> InspectArchive(string pArchivePath, GameInspectionLimits pLimits)
    {
        var diagnostics = new List<EngineInspectionDiagnostic>();
        var files = new Dictionary<string, InspectedGameFile>(StringComparer.OrdinalIgnoreCase);
        if (!Path.GetExtension(pArchivePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(EngineInspectionDiagnostic.Error(
                "inspect.archive-unsupported", "Only ZIP archives are supported for bounded inspection."));
            return PluginResult<GameInspectionSnapshot>.Succeeded(
                CreateSnapshot(pArchivePath, true, true, files, diagnostics));
        }

        var malformed = false;
        long totalBytes = 0;
        try
        {
            using var stream = File.OpenRead(pArchivePath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            foreach (var entry in archive.Entries
                .OrderBy(pEntry => pEntry.FullName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pEntry => pEntry.FullName, StringComparer.Ordinal))
            {
                if (files.Count >= pLimits.MaxEntries)
                {
                    malformed = true;
                    diagnostics.Add(EngineInspectionDiagnostic.Error(
                        "inspect.archive-entry-limit", "The archive exceeded the inspection entry limit."));
                    break;
                }
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }
                if (!TryNormalizeArchivePath(entry.FullName, out var relative))
                {
                    malformed = true;
                    diagnostics.Add(EngineInspectionDiagnostic.Error(
                        "inspect.archive-path", $"Skipped unsafe archive path '{entry.FullName}'."));
                    continue;
                }
                if (files.ContainsKey(relative))
                {
                    malformed = true;
                    diagnostics.Add(EngineInspectionDiagnostic.Error(
                        "inspect.archive-duplicate", $"Duplicate archive path '{relative}' was ignored."));
                    continue;
                }
                if (entry.Length < 0 || entry.Length > pLimits.MaxArchiveEntryBytes
                    || totalBytes > pLimits.MaxArchiveBytes - Math.Max(0, entry.Length))
                {
                    malformed = true;
                    diagnostics.Add(EngineInspectionDiagnostic.Error(
                        "inspect.archive-size", $"Archive entry '{relative}' exceeds the bounded inspection budget."));
                    continue;
                }
                var data = ReadStream(entry.Open(), (int)Math.Min(entry.Length, pLimits.MaxFileBytes));
                totalBytes += entry.Length;
                var truncated = entry.Length > pLimits.MaxFileBytes;
                files.Add(relative, new InspectedGameFile(relative, entry.Length, data, truncated, true));
            }
        }
        catch (InvalidDataException exception)
        {
            malformed = true;
            diagnostics.Add(EngineInspectionDiagnostic.Error(
                "inspect.archive-malformed", $"The ZIP archive is malformed: {exception.Message}"));
        }
        catch (Exception exception)
        {
            malformed = true;
            diagnostics.Add(EngineInspectionDiagnostic.Error(
                "inspect.archive-read-failed", $"The archive could not be read: {exception.GetType().Name}."));
        }

        return PluginResult<GameInspectionSnapshot>.Succeeded(
            CreateSnapshot(pArchivePath, true, malformed, files, diagnostics));
    }

    private static void AddFile(
        Dictionary<string, InspectedGameFile> pFiles,
        string pRelativePath,
        long pLength,
        string pFullPath,
        bool pArchiveEntry,
        GameInspectionLimits pLimits,
        List<EngineInspectionDiagnostic> pDiagnostics)
    {
        var relative = GameInspectionSnapshot.NormalizeRelativePath(pRelativePath);
        if (string.IsNullOrEmpty(relative) || pFiles.ContainsKey(relative))
        {
            return;
        }
        try
        {
            var amount = (int)Math.Min(Math.Max(0, pLength), pLimits.MaxFileBytes);
            using var stream = File.OpenRead(pFullPath);
            var data = ReadStream(stream, amount);
            pFiles.Add(relative, new InspectedGameFile(relative, pLength, data, pLength > pLimits.MaxFileBytes, pArchiveEntry));
            if (pLength > pLimits.MaxFileBytes)
            {
                pDiagnostics.Add(EngineInspectionDiagnostic.Info(
                    "inspect.file-truncated", $"Metadata file '{relative}' was read only up to the file-size limit."));
            }
        }
        catch (Exception exception)
        {
            pDiagnostics.Add(EngineInspectionDiagnostic.Error(
                "inspect.file-read-failed", $"Could not read '{relative}': {exception.GetType().Name}."));
        }
    }

    private static byte[] ReadStream(Stream pStream, int pMaximum)
    {
        using (pStream)
        using (var memory = new MemoryStream(Math.Max(0, pMaximum)))
        {
            var buffer = new byte[Math.Min(64 * 1024, Math.Max(1, pMaximum))];
            while (memory.Length < pMaximum)
            {
                var toRead = (int)Math.Min(buffer.Length, pMaximum - memory.Length);
                var read = pStream.Read(buffer, 0, toRead);
                if (read <= 0)
                {
                    break;
                }
                memory.Write(buffer, 0, read);
            }
            return memory.ToArray();
        }
    }

    private static GameInspectionSnapshot CreateSnapshot(
        string pSourcePath,
        bool pArchive,
        bool pMalformed,
        Dictionary<string, InspectedGameFile> pFiles,
        List<EngineInspectionDiagnostic> pDiagnostics)
    {
        return new GameInspectionSnapshot(pSourcePath, pArchive, pMalformed, pFiles, pDiagnostics);
    }

    private static bool TryNormalizeArchivePath(string pPath, out string pRelativePath)
    {
        pRelativePath = GameInspectionSnapshot.NormalizeRelativePath(pPath);
        if (string.IsNullOrEmpty(pRelativePath) || pRelativePath.StartsWith("/", StringComparison.Ordinal)
            || Path.IsPathRooted(pRelativePath))
        {
            pRelativePath = "";
            return false;
        }
        var parts = pRelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(pPart => pPart == "." || pPart == ".." || pPart.Contains('\0')))
        {
            pRelativePath = "";
            return false;
        }
        pRelativePath = string.Join('/', parts);
        return true;
    }
}

public sealed class EngineInspectionContext
{
    public EngineInspectionContext(GameInspectionSnapshot pSnapshot)
    {
        Snapshot = pSnapshot ?? throw new ArgumentNullException(nameof(pSnapshot));
    }

    public GameInspectionSnapshot Snapshot { get; }
}

public enum EngineDetectionStatus
{
    Supported,
    DetectionOnly,
    Malformed,
    Unknown,
}

public sealed class EngineDetectionCandidate
{
    public string PluginId { get; init; } = "";
    public string EngineId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Generation { get; init; } = "";
    public Version? EngineVersion { get; init; }
    public int Score { get; init; }
    public double Confidence => Math.Clamp(Score / 1000.0, 0.0, 1.0);
    public EngineDetectionStatus Status { get; init; } = EngineDetectionStatus.DetectionOnly;
    public string Reason { get; init; } = "";
    public string Title { get; init; } = "";
    public string RtpDependency { get; init; } = "";
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PluginDiagnostic> Diagnostics { get; init; } = Array.Empty<PluginDiagnostic>();

    public PluginGameInfo ToPluginGameInfo(string pSourcePath)
    {
        return new PluginGameInfo
        {
            GameDirectory = pSourcePath,
            EngineId = EngineId,
            Generation = Generation,
            EngineVersion = EngineVersion,
            DetectorScore = Score,
            Evidence = Evidence,
        };
    }
}

public sealed class EngineDetectionProbe
{
    private EngineDetectionProbe(bool pMatch, EngineDetectionCandidate? pCandidate, IReadOnlyList<PluginDiagnostic> pDiagnostics)
    {
        IsMatch = pMatch;
        Candidate = pCandidate;
        Diagnostics = pDiagnostics;
    }

    public bool IsMatch { get; }
    public EngineDetectionCandidate? Candidate { get; }
    public IReadOnlyList<PluginDiagnostic> Diagnostics { get; }

    public static EngineDetectionProbe Match(EngineDetectionCandidate pCandidate, IEnumerable<PluginDiagnostic>? pDiagnostics = null)
    {
        return new EngineDetectionProbe(true, pCandidate, CopyDiagnostics(pDiagnostics));
    }

    public static EngineDetectionProbe NoMatch(string pReason, IEnumerable<PluginDiagnostic>? pDiagnostics = null)
    {
        return new EngineDetectionProbe(false, null, CopyDiagnostics(pDiagnostics).Append(
            PluginDiagnostic.Info("detection.no-match", pReason)).ToArray());
    }

    private static IReadOnlyList<PluginDiagnostic> CopyDiagnostics(IEnumerable<PluginDiagnostic>? pDiagnostics)
    {
        return pDiagnostics == null ? Array.Empty<PluginDiagnostic>() : new List<PluginDiagnostic>(pDiagnostics);
    }
}

public interface IEngineDetectionPlugin
{
    EnginePluginMetadata Metadata { get; }
    EngineDetectionProbe Detect(EngineInspectionContext pContext);
}

public sealed class EngineDetectionRegistry
{
    private readonly Dictionary<string, IEngineDetectionPlugin> _plugins = new(StringComparer.Ordinal);

    public IReadOnlyList<IEngineDetectionPlugin> Plugins => _plugins.Values
        .OrderBy(pPlugin => pPlugin.Metadata.Id, StringComparer.Ordinal)
        .ToArray();

    public PluginOperationResult Register(IEngineDetectionPlugin pPlugin)
    {
        if (pPlugin == null || pPlugin.Metadata == null)
        {
            return PluginOperationResult.Failed(PluginError.Create(
                PluginErrorCode.InvalidMetadata,
                "A non-null detection plugin with metadata is required.",
                pPhase: "register"));
        }
        var validation = pPlugin.Metadata.Validate();
        if (!validation.Success)
        {
            return validation;
        }
        if ((pPlugin.Metadata.Capabilities & PluginCapability.Detection) == 0)
        {
            return PluginOperationResult.Failed(PluginError.Create(
                PluginErrorCode.InvalidMetadata,
                "A detection plugin must advertise the Detection capability.",
                pPlugin.Metadata.Id,
                "register"));
        }
        if (_plugins.ContainsKey(pPlugin.Metadata.Id))
        {
            return PluginOperationResult.Failed(PluginError.Create(
                PluginErrorCode.DuplicatePluginId,
                $"Detection plugin ID '{pPlugin.Metadata.Id}' is already registered.",
                pPlugin.Metadata.Id,
                "register"));
        }
        _plugins.Add(pPlugin.Metadata.Id, pPlugin);
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info("detection.plugin-registered", $"Registered detection plugin '{pPlugin.Metadata.Id}'.", pPlugin.Metadata.Id),
        });
    }

    public bool Unregister(string pPluginId) => !string.IsNullOrEmpty(pPluginId) && _plugins.Remove(pPluginId);
}

public sealed class EngineDetectionReport
{
    public string SourcePath { get; init; } = "";
    public bool IsArchive { get; init; }
    public bool IsMalformed { get; init; }
    public bool IsUnknown { get; init; }
    public bool IsAmbiguous { get; init; }
    public EngineDetectionCandidate? SelectedCandidate { get; init; }
    public IReadOnlyList<EngineDetectionCandidate> Candidates { get; init; } = Array.Empty<EngineDetectionCandidate>();
    public IReadOnlyList<EngineInspectionDiagnostic> InspectionDiagnostics { get; init; } = Array.Empty<EngineInspectionDiagnostic>();
    public IReadOnlyList<PluginDiagnostic> Diagnostics { get; init; } = Array.Empty<PluginDiagnostic>();
    public GameInspectionSnapshot? Inspection { get; init; }

    public static EngineDetectionReport Unknown(string pSourcePath, string pReason)
    {
        return new EngineDetectionReport
        {
            SourcePath = pSourcePath,
            IsUnknown = true,
            Diagnostics = new[] { PluginDiagnostic.Warning("detection.unknown", pReason) },
        };
    }
}

public sealed class PluginGameDetector
{
    private readonly EngineDetectionRegistry _registry;
    private readonly GameInspectionLimits _limits;

    public PluginGameDetector(EngineDetectionRegistry pRegistry, GameInspectionLimits? pLimits = null)
    {
        _registry = pRegistry ?? throw new ArgumentNullException(nameof(pRegistry));
        _limits = pLimits ?? new GameInspectionLimits();
    }

    public EngineDetectionReport Analyze(string pSourcePath)
    {
        var inspection = SafeGameInspector.Inspect(pSourcePath, _limits);
        if (!inspection.Success || inspection.Value == null)
        {
            return EngineDetectionReport.Unknown(pSourcePath, inspection.Error?.Message ?? "The source could not be inspected.");
        }

        var snapshot = inspection.Value;
        var candidates = new List<EngineDetectionCandidate>();
        var diagnostics = new List<PluginDiagnostic>();
        foreach (var plugin in _registry.Plugins)
        {
            try
            {
                var probe = plugin.Detect(new EngineInspectionContext(snapshot));
                if (probe == null)
                {
                    diagnostics.Add(PluginDiagnostic.Warning(
                        "detection.invalid-probe", $"Plugin '{plugin.Metadata.Id}' returned no detection result.", plugin.Metadata.Id));
                    continue;
                }
                diagnostics.AddRange(probe.Diagnostics);
                if (!probe.IsMatch || probe.Candidate == null)
                {
                    continue;
                }
                var candidate = probe.Candidate;
                if (!candidate.PluginId.Equals(plugin.Metadata.Id, StringComparison.Ordinal)
                    || !plugin.Metadata.SupportedEngines.Any(pRange => pRange.Matches(new PluginGameInfo
                    {
                        GameDirectory = snapshot.SourcePath,
                        EngineId = candidate.EngineId,
                        Generation = candidate.Generation,
                        EngineVersion = candidate.EngineVersion,
                    })))
                {
                    diagnostics.Add(PluginDiagnostic.Warning(
                        "detection.invalid-candidate", $"Plugin '{plugin.Metadata.Id}' returned an incompatible candidate.", plugin.Metadata.Id));
                    continue;
                }
                candidates.Add(candidate);
            }
            catch (Exception exception)
            {
                diagnostics.Add(PluginDiagnostic.Warning(
                    "detection.plugin-failed", $"Detection plugin '{plugin.Metadata.Id}' failed: {exception.GetType().Name}.", plugin.Metadata.Id));
            }
        }

        candidates.Sort((pLeft, pRight) =>
        {
            var score = pRight.Score.CompareTo(pLeft.Score);
            if (score != 0) return score;
            var leftPlugin = _registry.Plugins.FirstOrDefault(p => p.Metadata.Id == pLeft.PluginId);
            var rightPlugin = _registry.Plugins.FirstOrDefault(p => p.Metadata.Id == pRight.PluginId);
            var priority = (rightPlugin?.Metadata.Priority ?? 0).CompareTo(leftPlugin?.Metadata.Priority ?? 0);
            return priority != 0 ? priority : string.CompareOrdinal(pLeft.PluginId, pRight.PluginId);
        });

        var top = candidates.FirstOrDefault();
        var topEngines = candidates.Where(p => top != null && p.Score == top.Score)
            .Select(p => p.EngineId).Distinct(StringComparer.Ordinal).ToArray();
        var ambiguous = top != null && topEngines.Length > 1;
        var unknown = top == null || top!.Score < 200;
        if (ambiguous)
        {
            diagnostics.Add(PluginDiagnostic.Warning(
                "detection.ambiguous", "Multiple engines have the same strongest evidence; runtime selection requires user disambiguation."));
        }
        if (unknown)
        {
            diagnostics.Add(PluginDiagnostic.Warning(
                "detection.unknown", "No registered engine reached the minimum confidence threshold."));
        }
        if (snapshot.IsMalformed)
        {
            diagnostics.Add(PluginDiagnostic.Warning(
                "detection.malformed-input", "Inspection found malformed or bounded-out input; results are advisory only."));
        }

        return new EngineDetectionReport
        {
            SourcePath = snapshot.SourcePath,
            IsArchive = snapshot.IsArchive,
            IsMalformed = snapshot.IsMalformed,
            IsUnknown = unknown,
            IsAmbiguous = ambiguous,
            SelectedCandidate = unknown || ambiguous ? null : top,
            Candidates = candidates,
            InspectionDiagnostics = snapshot.Diagnostics,
            Diagnostics = diagnostics,
            Inspection = snapshot,
        };
    }
}
