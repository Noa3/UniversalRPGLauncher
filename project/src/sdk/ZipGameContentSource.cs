using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace UniversalRPG.Sdk;

public sealed class ZipGameContentLimits
{
    public int MaxEntries { get; init; } = 100_000;
    public long MaxEntryBytes { get; init; } = 512L * 1024 * 1024;
    public long MaxTotalUncompressedBytes { get; init; } = 4L * 1024 * 1024 * 1024;
    public int MaxPathLength { get; init; } = 1024;
    public double MaxExpansionRatio { get; init; } = 1000.0;

    public SdkOperationResult Validate()
    {
        if (MaxEntries <= 0 || MaxEntries > 1_000_000
            || MaxEntryBytes <= 0
            || MaxTotalUncompressedBytes <= 0
            || MaxPathLength <= 0 || MaxPathLength > 16_384
            || double.IsNaN(MaxExpansionRatio) || double.IsInfinity(MaxExpansionRatio)
            || MaxExpansionRatio < 1.0 || MaxExpansionRatio > 100_000.0)
        {
            return SdkOperationResult.Failed("zip.invalid-limits", "ZIP content limits are outside the supported safe range.");
        }
        return SdkOperationResult.Succeeded();
    }
}

/// <summary>
/// Read-only ZIP-backed logical content source. It never extracts the archive;
/// entry names are normalized and indexed once, then individual files are
/// decompressed into bounded memory buffers on demand.
/// </summary>
public sealed class ZipGameContentSource : IGameContentSource
{
    private readonly FileStream _stream;
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entries;
    private readonly ZipGameContentLimits _limits;
    private bool _disposed;

    public ZipGameContentSource(
        string pArchivePath,
        string pSourceId = "zip",
        ZipGameContentLimits? pLimits = null)
    {
        if (string.IsNullOrWhiteSpace(pArchivePath)) throw new ArgumentException("ZIP archive path is required.", nameof(pArchivePath));
        _limits = pLimits ?? new ZipGameContentLimits();
        var validation = _limits.Validate();
        if (!validation.Success) throw new ArgumentOutOfRangeException(nameof(pLimits), validation.ErrorMessage);

        var fullPath = Path.GetFullPath(pArchivePath);
        _stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            _archive = new ZipArchive(_stream, ZipArchiveMode.Read, leaveOpen: true);
            _entries = BuildIndex(_archive, _limits);
        }
        catch
        {
            _stream.Dispose();
            throw;
        }
        SourceId = string.IsNullOrWhiteSpace(pSourceId) ? "zip" : pSourceId;
    }

    public string SourceId { get; }
    public GameContentProtectionKind Protection => GameContentProtectionKind.None;
    public int EntryCount => _entries.Count;

    public bool Exists(string pLogicalPath)
    {
        ThrowIfDisposed();
        return TryNormalizePath(pLogicalPath, _limits.MaxPathLength, out var key) && _entries.ContainsKey(key);
    }

    public ContentReadResult Read(string pLogicalPath)
    {
        ThrowIfDisposed();
        if (!TryNormalizePath(pLogicalPath, _limits.MaxPathLength, out var key))
        {
            return ContentReadResult.Failed("zip.path-invalid", "ZIP logical path is unsafe or invalid.");
        }
        if (!_entries.TryGetValue(key, out var entry))
        {
            return ContentReadResult.Failed("content.not-found", $"ZIP content '{pLogicalPath}' was not found.");
        }
        if (entry.Length < 0 || entry.Length > _limits.MaxEntryBytes)
        {
            return ContentReadResult.Failed("zip.entry-size-limit", $"ZIP entry '{pLogicalPath}' exceeds the configured read limit.");
        }
        if (!ExpansionRatioAllowed(entry, _limits.MaxExpansionRatio))
        {
            return ContentReadResult.Failed("zip.expansion-ratio", $"ZIP entry '{pLogicalPath}' exceeds the configured expansion-ratio limit.");
        }

        try
        {
            using var input = entry.Open();
            using var output = new MemoryStream(entry.Length > 0 && entry.Length <= int.MaxValue ? (int)entry.Length : 0);
            var buffer = new byte[64 * 1024];
            long total = 0;
            while (true)
            {
                var read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0) break;
                total += read;
                if (total > _limits.MaxEntryBytes || total > entry.Length)
                {
                    return ContentReadResult.Failed("zip.entry-size-limit", $"ZIP entry '{pLogicalPath}' exceeded its declared/bounded size while reading.");
                }
                output.Write(buffer, 0, read);
            }
            if (total != entry.Length)
            {
                return ContentReadResult.Failed("zip.entry-length-mismatch", $"ZIP entry '{pLogicalPath}' did not match its declared size.");
            }
            return ContentReadResult.Succeeded(output.ToArray());
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            return ContentReadResult.Failed("zip.read-failed", exception.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _archive.Dispose();
        _stream.Dispose();
    }

    private static Dictionary<string, ZipArchiveEntry> BuildIndex(ZipArchive pArchive, ZipGameContentLimits pLimits)
    {
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        long totalUncompressed = 0;
        var inspected = 0;
        foreach (var entry in pArchive.Entries)
        {
            inspected++;
            if (inspected > pLimits.MaxEntries)
            {
                throw new InvalidDataException($"ZIP contains more than {pLimits.MaxEntries} entries.");
            }
            if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry
            if (!TryNormalizePath(entry.FullName, pLimits.MaxPathLength, out var key))
            {
                throw new InvalidDataException($"ZIP contains unsafe entry path '{entry.FullName}'.");
            }
            if (entry.Length < 0 || entry.Length > pLimits.MaxEntryBytes)
            {
                throw new InvalidDataException($"ZIP entry '{entry.FullName}' exceeds the configured size limit.");
            }
            if (!ExpansionRatioAllowed(entry, pLimits.MaxExpansionRatio))
            {
                throw new InvalidDataException($"ZIP entry '{entry.FullName}' exceeds the configured expansion-ratio limit.");
            }
            checked { totalUncompressed += entry.Length; }
            if (totalUncompressed > pLimits.MaxTotalUncompressedBytes)
            {
                throw new InvalidDataException("ZIP total uncompressed size exceeds the configured limit.");
            }
            if (entries.ContainsKey(key))
            {
                throw new InvalidDataException($"ZIP contains duplicate or case-colliding logical path '{entry.FullName}'.");
            }
            entries.Add(key, entry);
        }
        return entries;
    }

    private static bool ExpansionRatioAllowed(ZipArchiveEntry pEntry, double pMaximum)
    {
        if (pEntry.Length == 0) return true;
        if (pEntry.CompressedLength <= 0) return false;
        return (double)pEntry.Length / pEntry.CompressedLength <= pMaximum;
    }

    private static bool TryNormalizePath(string pPath, int pMaxLength, out string pNormalized)
    {
        pNormalized = "";
        if (string.IsNullOrWhiteSpace(pPath) || pPath.Length > pMaxLength || pPath.IndexOf('\0') >= 0) return false;
        var normalized = pPath.Replace('\\', '/');
        if (normalized.StartsWith('/', StringComparison.Ordinal) || Path.IsPathRooted(normalized)) return false;
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(pPart => pPart is "." or "..")) return false;
        pNormalized = string.Join('/', parts);
        return true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ZipGameContentSource));
    }
}
