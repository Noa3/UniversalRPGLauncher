using System;
using System.Collections.Generic;

namespace UniversalRPG.Sdk;

/// <summary>
/// Canonical logical-path rules for imported Windows-authored games. Validation
/// is intentionally platform-independent: a Windows drive/UNC path must remain
/// unsafe even when UniversalRPG is running on Linux, Android, or macOS.
/// </summary>
public static class LogicalGamePath
{
    public const int DefaultMaxLength = 4096;

    public static bool TryNormalize(
        string pPath,
        out string pNormalized,
        int pMaxLength = DefaultMaxLength)
    {
        pNormalized = "";
        if (string.IsNullOrWhiteSpace(pPath) || pMaxLength <= 0 || pPath.Length > pMaxLength) return false;
        if (pPath.IndexOf('\0') >= 0) return false;
        foreach (var character in pPath)
        {
            if (character < 0x20) return false;
        }

        var normalized = pPath.Replace('\\', '/').Trim();
        if (normalized.Length == 0 || normalized.StartsWith('/', StringComparison.Ordinal)) return false;
        if (normalized.StartsWith("//", StringComparison.Ordinal)) return false;
        if (IsWindowsDriveQualified(normalized)) return false;

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        var safeParts = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (part is "." or "..") return false;
            // Colons are not portable Windows game-path characters and can
            // address NTFS alternate data streams on Windows.
            if (part.Contains(':')) return false;
            safeParts.Add(part);
        }
        pNormalized = string.Join('/', safeParts);
        return pNormalized.Length > 0;
    }

    public static string Combine(string pPrefix, string pLogicalPath)
    {
        if (!TryNormalize(pLogicalPath, out var path))
        {
            throw new ArgumentException("Logical game path is unsafe.", nameof(pLogicalPath));
        }
        if (string.IsNullOrWhiteSpace(pPrefix)) return path;
        if (!TryNormalize(pPrefix, out var prefix))
        {
            throw new ArgumentException("Logical game-path prefix is unsafe.", nameof(pPrefix));
        }
        return prefix + "/" + path;
    }

    private static bool IsWindowsDriveQualified(string pPath)
        => pPath.Length >= 2
            && ((pPath[0] >= 'A' && pPath[0] <= 'Z') || (pPath[0] >= 'a' && pPath[0] <= 'z'))
            && pPath[1] == ':';
}
