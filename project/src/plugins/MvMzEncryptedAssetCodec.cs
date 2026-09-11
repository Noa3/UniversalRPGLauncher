using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Transparent reader for RPG Maker MV/MZ's built-in encrypted image/audio
/// deployment format. The engine stores a 16-byte key in System.json and the
/// deployed asset contains a 16-byte RPG Maker header followed by a payload
/// whose first 16 bytes are XORed with that key. Decryption stays in memory;
/// this component does not expose an extraction workflow.
/// </summary>
public static class MvMzEncryptedAssetCodec
{
    public const int HeaderLength = 16;
    public const int KeyLength = 16;
    public const long DefaultMaxAssetBytes = 256L * 1024 * 1024;

    private static readonly byte[] Header =
    {
        0x52, 0x50, 0x47, 0x4d, 0x56, 0x00, 0x00, 0x00,
        0x00, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
    };

    public sealed class EncryptionMetadata
    {
        public byte[] Key { get; init; } = Array.Empty<byte>();
        public bool HasEncryptedImages { get; init; }
        public bool HasEncryptedAudio { get; init; }
        public string SystemJsonPath { get; init; } = "";
        public bool HasUsableKey => Key.Length == KeyLength;
    }

    public static SdkValueResult<EncryptionMetadata> ReadMetadata(string pSystemJsonPath)
    {
        if (string.IsNullOrWhiteSpace(pSystemJsonPath) || !File.Exists(pSystemJsonPath))
        {
            return SdkValueResult<EncryptionMetadata>.Failed(
                "mv-mz.system-json-missing",
                "RPG Maker MV/MZ data/System.json is required for encrypted asset metadata.");
        }

        try
        {
            var info = new FileInfo(pSystemJsonPath);
            if (info.Length <= 0 || info.Length > 4 * 1024 * 1024)
            {
                return SdkValueResult<EncryptionMetadata>.Failed(
                    "mv-mz.system-json-size",
                    "System.json is empty or exceeds the bounded metadata limit.");
            }

            using var stream = File.OpenRead(pSystemJsonPath);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                MaxDepth = 64,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return SdkValueResult<EncryptionMetadata>.Failed(
                    "mv-mz.system-json-invalid",
                    "System.json root is not an object.");
            }

            var root = document.RootElement;
            var keyText = ReadString(root, "encryptionKey");
            var key = Array.Empty<byte>();
            if (!string.IsNullOrWhiteSpace(keyText))
            {
                if (!TryParseHexKey(keyText, out key))
                {
                    return SdkValueResult<EncryptionMetadata>.Failed(
                        "mv-mz.encryption-key-invalid",
                        "System.json encryptionKey must be exactly 16 bytes encoded as 32 hexadecimal characters.");
                }
            }

            return SdkValueResult<EncryptionMetadata>.Succeeded(new EncryptionMetadata
            {
                Key = key,
                HasEncryptedImages = ReadBool(root, "hasEncryptedImages"),
                HasEncryptedAudio = ReadBool(root, "hasEncryptedAudio"),
                SystemJsonPath = pSystemJsonPath,
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return SdkValueResult<EncryptionMetadata>.Failed(
                "mv-mz.system-json-read-failed",
                $"Could not read encrypted asset metadata: {exception.Message}");
        }
    }

    public static ContentReadResult Decrypt(ReadOnlySpan<byte> pEncrypted, ReadOnlySpan<byte> pKey)
    {
        if (pKey.Length != KeyLength)
        {
            return ContentReadResult.Failed(
                "mv-mz.key-length",
                "RPG Maker MV/MZ encrypted assets require a 16-byte key.");
        }
        if (pEncrypted.Length < HeaderLength)
        {
            return ContentReadResult.Failed(
                "mv-mz.asset-too-small",
                "Encrypted RPG Maker asset is smaller than the required 16-byte header.");
        }
        for (var index = 0; index < HeaderLength; index++)
        {
            if (pEncrypted[index] != Header[index])
            {
                return ContentReadResult.Failed(
                    "mv-mz.header-invalid",
                    "Encrypted RPG Maker asset header is not recognized.");
            }
        }

        var result = pEncrypted[HeaderLength..].ToArray();
        var xorLength = Math.Min(KeyLength, result.Length);
        for (var index = 0; index < xorLength; index++)
        {
            result[index] ^= pKey[index];
        }
        return ContentReadResult.Succeeded(result);
    }

    public static bool TryParseHexKey(string pValue, out byte[] pKey)
    {
        pKey = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(pValue)) return false;
        var value = pValue.Trim();
        if (value.Length != KeyLength * 2) return false;

        var result = new byte[KeyLength];
        for (var index = 0; index < result.Length; index++)
        {
            var high = Hex(value[index * 2]);
            var low = Hex(value[index * 2 + 1]);
            if (high < 0 || low < 0) return false;
            result[index] = (byte)((high << 4) | low);
        }
        pKey = result;
        return true;
    }

    public static bool IsEncryptedAssetExtension(string pExtension)
    {
        return pExtension.Equals(".rpgmvp", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".rpgmvo", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".rpgmvm", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".png_", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".ogg_", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".m4a_", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadString(JsonElement pRoot, string pName)
    {
        return pRoot.TryGetProperty(pName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }

    private static bool ReadBool(JsonElement pRoot, string pName)
    {
        return pRoot.TryGetProperty(pName, out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            && value.GetBoolean();
    }

    private static int Hex(char pValue)
    {
        if (pValue is >= '0' and <= '9') return pValue - '0';
        if (pValue is >= 'a' and <= 'f') return pValue - 'a' + 10;
        if (pValue is >= 'A' and <= 'F') return pValue - 'A' + 10;
        return -1;
    }
}

/// <summary>
/// Read-only logical MV/MZ content source. Plain files win when present;
/// otherwise well-known encrypted asset variants are resolved and decrypted in
/// memory using the project's own System.json key.
/// </summary>
public sealed class MvMzGameContentSource : IGameContentSource
{
    private readonly string _root;
    private readonly string _rootPrefix;
    private readonly StringComparison _pathComparison;
    private readonly MvMzEncryptedAssetCodec.EncryptionMetadata _metadata;
    private readonly long _maxAssetBytes;

    public MvMzGameContentSource(string pGameDirectory, long pMaxAssetBytes = MvMzEncryptedAssetCodec.DefaultMaxAssetBytes)
    {
        if (string.IsNullOrWhiteSpace(pGameDirectory)) throw new ArgumentException("Game directory is required.", nameof(pGameDirectory));
        if (pMaxAssetBytes <= 0) throw new ArgumentOutOfRangeException(nameof(pMaxAssetBytes));

        var gameRoot = Path.GetFullPath(pGameDirectory);
        var wwwSystem = Path.Combine(gameRoot, "www", "data", "System.json");
        var rootSystem = Path.Combine(gameRoot, "data", "System.json");
        var systemPath = File.Exists(wwwSystem) ? wwwSystem : rootSystem;
        _root = File.Exists(wwwSystem) ? Path.Combine(gameRoot, "www") : gameRoot;
        _rootPrefix = EnsureTrailingSeparator(Path.GetFullPath(_root));
        _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        _maxAssetBytes = pMaxAssetBytes;

        var metadata = MvMzEncryptedAssetCodec.ReadMetadata(systemPath);
        if (!metadata.Success || metadata.Value == null)
        {
            throw new InvalidDataException(metadata.Result.ErrorMessage);
        }
        _metadata = metadata.Value;
    }

    public string SourceId => "rpg-maker-mv-mz-directory";
    public GameContentProtectionKind Protection =>
        _metadata.HasEncryptedImages || _metadata.HasEncryptedAudio
            ? GameContentProtectionKind.EngineManagedEncryption
            : GameContentProtectionKind.None;

    public bool Exists(string pLogicalPath)
    {
        if (!TryResolveLogical(pLogicalPath, out var plainPath)) return false;
        if (File.Exists(plainPath)) return true;
        return EncryptedCandidates(plainPath).Any(File.Exists);
    }

    public ContentReadResult Read(string pLogicalPath)
    {
        if (!TryResolveLogical(pLogicalPath, out var plainPath))
        {
            return ContentReadResult.Failed("content.path-invalid", "Logical content path is unsafe or outside the game root.");
        }

        if (File.Exists(plainPath))
        {
            return ReadBounded(plainPath);
        }

        var encrypted = EncryptedCandidates(plainPath).FirstOrDefault(File.Exists);
        if (encrypted == null)
        {
            return ContentReadResult.Failed("content.not-found", $"Game content '{pLogicalPath}' was not found.");
        }
        if (!_metadata.HasUsableKey)
        {
            return ContentReadResult.Failed(
                "mv-mz.encryption-key-missing",
                $"Encrypted asset '{pLogicalPath}' exists but System.json does not provide a usable 16-byte key.");
        }

        var raw = ReadBounded(encrypted);
        if (!raw.Success) return raw;
        return MvMzEncryptedAssetCodec.Decrypt(raw.Data.Span, _metadata.Key);
    }

    public void Dispose()
    {
        // No persistent streams or decrypted buffers are retained.
    }

    private ContentReadResult ReadBounded(string pPath)
    {
        try
        {
            var info = new FileInfo(pPath);
            if (info.Length < 0 || info.Length > _maxAssetBytes)
            {
                return ContentReadResult.Failed(
                    "content.size-limit",
                    $"Game content '{Path.GetFileName(pPath)}' exceeds the configured {_maxAssetBytes}-byte read limit.");
            }
            return ContentReadResult.Succeeded(File.ReadAllBytes(pPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ContentReadResult.Failed("content.read-failed", exception.Message);
        }
    }

    private bool TryResolveLogical(string pLogicalPath, out string pFullPath)
    {
        pFullPath = "";
        if (string.IsNullOrWhiteSpace(pLogicalPath) || pLogicalPath.IndexOf('\0') >= 0) return false;
        var normalized = pLogicalPath.Replace('\\', '/');
        if (normalized.StartsWith('/', StringComparison.Ordinal) || Path.IsPathRooted(normalized)) return false;
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(pPart => pPart == "..")) return false;

        try
        {
            var full = Path.GetFullPath(Path.Combine(_root, string.Join(Path.DirectorySeparatorChar, parts)));
            if (!full.StartsWith(_rootPrefix, _pathComparison)) return false;
            pFullPath = full;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> EncryptedCandidates(string pPlainPath)
    {
        var extension = Path.GetExtension(pPlainPath);
        var stem = extension.Length == 0 ? pPlainPath : pPlainPath[..^extension.Length];
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            yield return stem + ".rpgmvp";
            yield return stem + ".png_";
        }
        else if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            yield return stem + ".rpgmvo";
            yield return stem + ".ogg_";
        }
        else if (extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase))
        {
            yield return stem + ".rpgmvm";
            yield return stem + ".m4a_";
        }
    }

    private static string EnsureTrailingSeparator(string pPath)
    {
        return pPath.EndsWith(Path.DirectorySeparatorChar)
            ? pPath
            : pPath + Path.DirectorySeparatorChar;
    }
}
