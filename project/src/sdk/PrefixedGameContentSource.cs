using System;

namespace UniversalRPG.Sdk;

/// <summary>
/// Presents a logical subdirectory of another content source as its root. This
/// is useful for deployed MV/MZ games whose web root lives below `www/` while
/// keeping the underlying case-insensitive/archive/protected source unchanged.
/// </summary>
public sealed class PrefixedGameContentSource : IGameContentSource
{
    private readonly IGameContentSource _inner;
    private readonly string _prefix;
    private readonly bool _disposeInner;
    private bool _disposed;

    public PrefixedGameContentSource(
        IGameContentSource pInner,
        string pPrefix,
        string pSourceId = "prefixed-content",
        bool pDisposeInner = false)
    {
        _inner = pInner ?? throw new ArgumentNullException(nameof(pInner));
        _prefix = NormalizePrefix(pPrefix);
        SourceId = string.IsNullOrWhiteSpace(pSourceId) ? "prefixed-content" : pSourceId;
        _disposeInner = pDisposeInner;
    }

    public string SourceId { get; }
    public GameContentProtectionKind Protection => _inner.Protection;

    public bool Exists(string pLogicalPath)
    {
        ThrowIfDisposed();
        return TryCombine(pLogicalPath, out var combined) && _inner.Exists(combined);
    }

    public ContentReadResult Read(string pLogicalPath)
    {
        ThrowIfDisposed();
        if (!TryCombine(pLogicalPath, out var combined))
        {
            return ContentReadResult.Failed("content.path-invalid", "Logical content path is unsafe.");
        }
        return _inner.Read(combined);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_disposeInner) _inner.Dispose();
    }

    private bool TryCombine(string pLogicalPath, out string pCombined)
    {
        pCombined = "";
        if (string.IsNullOrWhiteSpace(pLogicalPath) || pLogicalPath.IndexOf('\0') >= 0) return false;
        var path = pLogicalPath.Replace('\\', '/').TrimStart('/');
        if (path.Length == 0 || path == "." || path == ".." || path.StartsWith("../", StringComparison.Ordinal)
            || path.Contains("/../", StringComparison.Ordinal))
        {
            return false;
        }
        pCombined = _prefix + path;
        return true;
    }

    private static string NormalizePrefix(string pPrefix)
    {
        if (string.IsNullOrWhiteSpace(pPrefix)) return "";
        var prefix = pPrefix.Replace('\\', '/').Trim('/');
        if (prefix.Length == 0) return "";
        var parts = prefix.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part is "." or "..") throw new ArgumentException("Content prefix cannot contain traversal segments.", nameof(pPrefix));
        }
        return string.Join('/', parts) + "/";
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PrefixedGameContentSource));
    }
}
