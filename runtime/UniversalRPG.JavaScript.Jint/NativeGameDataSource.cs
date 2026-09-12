using System;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.JavaScript.Jint;

/// <summary>
/// Read-only native transport used by original MV/MZ startup/data loaders. Data
/// requests are limited to data/*.json. The only script request accepted is the
/// selected project's own js/main.js URI, needed by MZ's startup accessibility
/// check. No HTTP, arbitrary file URL or native path resolution exists here.
/// </summary>
public sealed class NativeGameDataSource
{
    public const int MaxFileBytes = 8 * 1024 * 1024;
    public const int MaxBytesPerExecution = 32 * 1024 * 1024;
    public const int MaxReadsPerExecution = 128;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly IGameContentSource _content;
    private readonly string _prefix;
    private bool _allowed;
    private int _reads;
    private long _bytes;

    public NativeGameDataSource(IGameContentSource content, string contentPrefix = "")
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        if (contentPrefix == "") _prefix = "";
        else if (LogicalGamePath.TryNormalize(contentPrefix, out var prefix)) _prefix = prefix + "/";
        else throw new ArgumentException("Invalid logical content prefix.", nameof(contentPrefix));
    }

    internal void BeginExecution(bool allowReadGameFiles)
    {
        _allowed = allowReadGameFiles; _reads = 0; _bytes = 0;
    }
    internal void EndExecution() => _allowed = false;

    internal string ReadEnvelope(string url)
    {
        if (!_allowed) return Failure("data.read-denied");
        if (_reads >= MaxReadsPerExecution) return Failure("data.request-budget");
        _reads++;
        if (!TryResolveReadablePath(url, out var path)) return Failure("data.path-denied");
        try
        {
            var result = _content.Read(_prefix + path);
            if (result == null || !result.Success) return Failure("data.read-failed");
            if (result.Data.Length > MaxFileBytes) return Failure("data.file-too-large");
            if (result.Data.Length > MaxBytesPerExecution - _bytes) return Failure("data.byte-budget");
            _bytes += result.Data.Length;
            var bytes = result.Data.Span;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) bytes = bytes[3..];
            var text = Utf8.GetString(bytes);
            return JsonSerializer.Serialize(new { success = true, text, errorCode = "" });
        }
        catch (DecoderFallbackException) { return Failure("data.invalid-utf8"); }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            return Failure("data.provider-failed");
        }
    }

    internal static string Failure(string code) => JsonSerializer.Serialize(new { success = false, text = "", errorCode = code });

    private bool TryResolveReadablePath(string url, out string path)
    {
        if (TryResolveDataPath(url, out path)) return true;
        path = "";
        const string scheme = "urpg://game/";
        if (string.IsNullOrEmpty(url) || url.Length > 4096 || !url.StartsWith(scheme, StringComparison.Ordinal)) return false;
        var raw = url[scheme.Length..];
        if (raw.IndexOfAny(new[] { '?', '#', '\\' }) >= 0 || raw.Contains('%')) return false;
        if (!LogicalGamePath.TryNormalize(raw, out var normalized) || normalized != raw) return false;
        var expected = _prefix + "js/main.js";
        if (!normalized.Equals(expected, StringComparison.OrdinalIgnoreCase)) return false;
        path = "js/main.js";
        return true;
    }

    public static bool TryResolveDataPath(string url, out string path)
    {
        path = "";
        if (string.IsNullOrEmpty(url) || url.Length > 4096 || url != url.Trim()) return false;
        foreach (var c in url) if (char.IsControl(c)) return false;
        var cut = url.IndexOfAny(new[] { '?', '#' });
        var raw = cut < 0 ? url : url[..cut];
        if (raw.StartsWith("./", StringComparison.Ordinal)) raw = raw[2..];
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] != '%') continue;
            if (i + 2 >= raw.Length || !Uri.IsHexDigit(raw[i + 1]) || !Uri.IsHexDigit(raw[i + 2])) return false;
            i += 2;
        }
        var decoded = Uri.UnescapeDataString(raw);
        if (decoded.IndexOfAny(new[] { '%', '?', '#', '\\' }) >= 0) return false;
        if (!LogicalGamePath.TryNormalize(decoded, out var normalized)) return false;
        if (!normalized.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
            || !normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return false;
        if (normalized != decoded || normalized.Length <= "data/.json".Length) return false;
        foreach (var segment in normalized.Split('/'))
        {
            if (segment.EndsWith(" ", StringComparison.Ordinal) || segment.EndsWith(".", StringComparison.Ordinal)) return false;
            var stem = segment.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" or "CLOCK$") return false;
            if (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal))
                && (stem[3] is >= '1' and <= '9' or '¹' or '²' or '³')) return false;
        }
        path = normalized;
        return true;
    }
}
