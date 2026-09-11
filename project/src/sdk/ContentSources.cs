using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UniversalRPG.Sdk;

/// <summary>
/// Read-only directory-backed game content with Windows-style case-insensitive
/// lookup on every platform. Resolution rejects traversal, ambiguous
/// case-collisions, and reparse/symlink components.
/// </summary>
public sealed class DirectoryGameContentSource : IGameContentSource
{
    private readonly string _root;
    private readonly long _maxFileBytes;

    public DirectoryGameContentSource(
        string pRoot,
        string pSourceId = "directory",
        long pMaxFileBytes = 256L * 1024 * 1024)
    {
        if (string.IsNullOrWhiteSpace(pRoot)) throw new ArgumentException("Content root is required.", nameof(pRoot));
        if (pMaxFileBytes <= 0) throw new ArgumentOutOfRangeException(nameof(pMaxFileBytes));
        _root = Path.GetFullPath(pRoot);
        if (!Directory.Exists(_root)) throw new DirectoryNotFoundException(_root);
        SourceId = string.IsNullOrWhiteSpace(pSourceId) ? "directory" : pSourceId;
        _maxFileBytes = pMaxFileBytes;
    }

    public string SourceId { get; }
    public GameContentProtectionKind Protection => GameContentProtectionKind.None;

    public bool Exists(string pLogicalPath)
        => TryResolve(pLogicalPath, out var resolved) && File.Exists(resolved);

    public ContentReadResult Read(string pLogicalPath)
    {
        if (!TryResolve(pLogicalPath, out var resolved))
        {
            return ContentReadResult.Failed(
                "content.path-unresolved",
                $"Logical content path '{pLogicalPath}' is unsafe, ambiguous, or unavailable.");
        }
        if (!File.Exists(resolved))
        {
            return ContentReadResult.Failed("content.not-found", $"Content '{pLogicalPath}' was not found.");
        }

        try
        {
            var info = new FileInfo(resolved);
            if (info.Length < 0 || info.Length > _maxFileBytes)
            {
                return ContentReadResult.Failed(
                    "content.size-limit",
                    $"Content '{pLogicalPath}' exceeds the configured {_maxFileBytes}-byte limit.");
            }
            return ContentReadResult.Succeeded(File.ReadAllBytes(resolved));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ContentReadResult.Failed("content.read-failed", exception.Message);
        }
    }

    public void Dispose()
    {
        // Directory-backed source keeps no open handles.
    }

    private bool TryResolve(string pLogicalPath, out string pResolved)
    {
        pResolved = "";
        if (string.IsNullOrWhiteSpace(pLogicalPath) || pLogicalPath.IndexOf('\0') >= 0) return false;
        var normalized = pLogicalPath.Replace('\\', '/');
        if (normalized.StartsWith('/', StringComparison.Ordinal) || Path.IsPathRooted(normalized)) return false;
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(pPart => pPart is "." or "..")) return false;

        var current = _root;
        if (IsReparsePoint(current)) return false;
        for (var index = 0; index < parts.Length; index++)
        {
            if (!Directory.Exists(current)) return false;
            string[] matches;
            try
            {
                matches = Directory.EnumerateFileSystemEntries(current)
                    .Where(pEntry => Path.GetFileName(pEntry).Equals(parts[index], StringComparison.OrdinalIgnoreCase))
                    .OrderBy(pEntry => Path.GetFileName(pEntry).Equals(parts[index], StringComparison.Ordinal) ? 0 : 1)
                    .ThenBy(pEntry => Path.GetFileName(pEntry), StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
            if (matches.Length == 0) return false;

            var exact = matches.Where(pEntry => Path.GetFileName(pEntry).Equals(parts[index], StringComparison.Ordinal)).ToArray();
            if (exact.Length == 1)
            {
                current = exact[0];
            }
            else if (matches.Length == 1)
            {
                current = matches[0];
            }
            else
            {
                return false;
            }

            if (IsReparsePoint(current)) return false;
            if (index < parts.Length - 1 && !Directory.Exists(current)) return false;
        }

        pResolved = current;
        return true;
    }

    private static bool IsReparsePoint(string pPath)
    {
        try
        {
            return (File.GetAttributes(pPath) & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return true;
        }
    }
}

public sealed class LayeredGameContentSource : IGameContentSource
{
    public sealed class Layer
    {
        public required string Id { get; init; }
        public required IGameContentSource Source { get; init; }
    }

    private readonly Layer[] _layers;
    private readonly bool _disposeLayers;
    private bool _disposed;

    public LayeredGameContentSource(IEnumerable<Layer> pLayers, bool pDisposeLayers = true)
    {
        if (pLayers == null) throw new ArgumentNullException(nameof(pLayers));
        _layers = pLayers.ToArray();
        if (_layers.Length == 0) throw new ArgumentException("At least one content layer is required.", nameof(pLayers));
        if (_layers.Any(pLayer => pLayer == null || pLayer.Source == null || string.IsNullOrWhiteSpace(pLayer.Id)))
        {
            throw new ArgumentException("Every content layer requires an ID and source.", nameof(pLayers));
        }
        if (_layers.Select(pLayer => pLayer.Id).Distinct(StringComparer.Ordinal).Count() != _layers.Length)
        {
            throw new ArgumentException("Content layer IDs must be unique.", nameof(pLayers));
        }
        _disposeLayers = pDisposeLayers;
    }

    public string SourceId => "layered-content";
    public GameContentProtectionKind Protection => AggregateProtection(_layers);
    public IReadOnlyList<string> LayerIds => _layers.Select(pLayer => pLayer.Id).ToArray();

    public bool Exists(string pLogicalPath)
    {
        ThrowIfDisposed();
        foreach (var layer in _layers)
        {
            if (layer.Source.Exists(pLogicalPath)) return true;
        }
        return false;
    }

    public ContentReadResult Read(string pLogicalPath)
    {
        ThrowIfDisposed();
        foreach (var layer in _layers)
        {
            if (!layer.Source.Exists(pLogicalPath)) continue;
            var result = layer.Source.Read(pLogicalPath);
            if (result.Success) return result;
            return result;
        }
        return ContentReadResult.Failed("content.not-found", $"Content '{pLogicalPath}' was not found in any layer.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_disposeLayers) return;
        foreach (var layer in _layers)
        {
            layer.Source.Dispose();
        }
    }

    private static GameContentProtectionKind AggregateProtection(IEnumerable<Layer> pLayers)
    {
        var kinds = pLayers
            .Select(pLayer => pLayer.Source.Protection)
            .Where(pKind => pKind != GameContentProtectionKind.None)
            .Distinct()
            .ToArray();
        return kinds.Length switch
        {
            0 => GameContentProtectionKind.None,
            1 => kinds[0],
            _ => GameContentProtectionKind.Unknown,
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LayeredGameContentSource));
    }
}
