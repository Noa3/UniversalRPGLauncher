using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UniversalRPG.Sdk;

namespace UniversalRPG.Platform;

/// <summary>
/// App-owned persistent script storage. Keys are SHA-256 filenames and the
/// original key is verified inside a small binary envelope, so imported games
/// never choose native paths. Writes use create-new temp files and atomic-ish
/// same-directory replacement. The caller chooses an app-controlled root.
/// </summary>
public sealed class DirectoryScriptKeyValueStorage : IScriptKeyValueStorage
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("URPGKV1\0");
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private const string Extension = ".urpgkv";
    private const int HeaderBytes = 16;
    private const int MaxKeyBytes = MemoryScriptKeyValueStorage.MaxKeyCharacters * 4;
    private const int MaxValueBytes = MemoryScriptKeyValueStorage.MaxValueCharacters * 4;
    private readonly string _root;
    private bool _disposed;

    public DirectoryScriptKeyValueStorage(string pRootDirectory, string pId = "directory-script-storage")
    {
        if (string.IsNullOrWhiteSpace(pRootDirectory)) throw new ArgumentException("Storage root is required.", nameof(pRootDirectory));
        _root = Path.GetFullPath(pRootDirectory);
        Directory.CreateDirectory(_root);
        EnsureSafeRoot();
        Id = string.IsNullOrWhiteSpace(pId) ? "directory-script-storage" : pId;
    }

    public string Id { get; }

    public ScriptStorageValueResult GetItem(string pKey)
    {
        if (_disposed) return ScriptStorageValueResult.Failed("storage.disposed", "Storage is disposed.");
        if (!ValidKey(pKey)) return ScriptStorageValueResult.Failed("storage.key-invalid", "Storage key is outside the bounded limit.");
        try
        {
            EnsureSafeRoot();
            var path = PathForKey(pKey);
            if (!File.Exists(path)) return ScriptStorageValueResult.Missing();
            EnsureRegularFile(path);
            using var stream = OpenBounded(path);
            if (!TryReadEnvelope(stream, pKey, pReadValue: true, out _, out var value))
                return ScriptStorageValueResult.Failed("storage.corrupt", "Stored value is malformed or belongs to another key.");
            return ScriptStorageValueResult.FoundValue(value);
        }
        catch (DecoderFallbackException) { return ScriptStorageValueResult.Failed("storage.invalid-utf8", "Stored value is not valid UTF-8."); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        { return ScriptStorageValueResult.Failed("storage.io-failed", "Persistent storage read failed."); }
    }

    public SdkOperationResult SetItem(string pKey, string pValue)
    {
        if (_disposed) return SdkOperationResult.Failed("storage.disposed", "Storage is disposed.");
        if (!ValidKey(pKey) || pValue == null || pValue.Length > MemoryScriptKeyValueStorage.MaxValueCharacters)
            return SdkOperationResult.Failed("storage.value-invalid", "Storage key/value is outside the bounded limit.");
        try
        {
            EnsureSafeRoot();
            var keyBytes = Utf8.GetBytes(pKey);
            var valueBytes = Utf8.GetBytes(pValue);
            if (keyBytes.Length > MaxKeyBytes || valueBytes.Length > MaxValueBytes)
                return SdkOperationResult.Failed("storage.byte-limit", "Encoded storage value exceeds its byte limit.");

            var destination = PathForKey(pKey);
            if (!File.Exists(destination) && CountEntries() >= MemoryScriptKeyValueStorage.MaxEntries)
                return SdkOperationResult.Failed("storage.entry-limit", "Storage entry limit reached.");
            if (File.Exists(destination)) EnsureRegularFile(destination);

            var temporary = Path.Combine(_root, ".tmp-" + Guid.NewGuid().ToString("N") + Extension);
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None, 8192, FileOptions.WriteThrough))
                {
                    Span<byte> header = stackalloc byte[HeaderBytes];
                    Magic.CopyTo(header);
                    BinaryPrimitives.WriteInt32LittleEndian(header[8..12], keyBytes.Length);
                    BinaryPrimitives.WriteInt32LittleEndian(header[12..16], valueBytes.Length);
                    stream.Write(header);
                    stream.Write(keyBytes);
                    stream.Write(valueBytes);
                    stream.Flush(true);
                }
                File.Move(temporary, destination, true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            return SdkOperationResult.Succeeded();
        }
        catch (EncoderFallbackException) { return SdkOperationResult.Failed("storage.invalid-utf8", "Storage key/value cannot be encoded as UTF-8."); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        { return SdkOperationResult.Failed("storage.io-failed", "Persistent storage write failed."); }
    }

    public SdkOperationResult RemoveItem(string pKey)
    {
        if (_disposed) return SdkOperationResult.Failed("storage.disposed", "Storage is disposed.");
        if (!ValidKey(pKey)) return SdkOperationResult.Failed("storage.key-invalid", "Storage key is outside the bounded limit.");
        try
        {
            EnsureSafeRoot();
            var path = PathForKey(pKey);
            if (File.Exists(path)) { EnsureRegularFile(path); File.Delete(path); }
            return SdkOperationResult.Succeeded();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        { return SdkOperationResult.Failed("storage.io-failed", "Persistent storage removal failed."); }
    }

    public SdkOperationResult Clear()
    {
        if (_disposed) return SdkOperationResult.Failed("storage.disposed", "Storage is disposed.");
        try
        {
            EnsureSafeRoot();
            var files = EnumerateEntries();
            foreach (var path in files) { EnsureRegularFile(path); File.Delete(path); }
            return SdkOperationResult.Succeeded();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        { return SdkOperationResult.Failed("storage.io-failed", "Persistent storage clear failed."); }
    }

    public ScriptStorageKeysResult GetKeys()
    {
        if (_disposed) return ScriptStorageKeysResult.Failed("storage.disposed", "Storage is disposed.");
        try
        {
            EnsureSafeRoot();
            var keys = new List<string>();
            foreach (var path in EnumerateEntries())
            {
                EnsureRegularFile(path);
                using var stream = OpenBounded(path);
                if (!TryReadEnvelope(stream, null, pReadValue: false, out var key, out _))
                    return ScriptStorageKeysResult.Failed("storage.corrupt", "Stored key envelope is malformed.");
                if (!Path.GetFileNameWithoutExtension(path).Equals(HashKey(key), StringComparison.OrdinalIgnoreCase))
                    return ScriptStorageKeysResult.Failed("storage.corrupt", "Stored key hash does not match its filename.");
                keys.Add(key);
            }
            keys.Sort(StringComparer.Ordinal);
            return ScriptStorageKeysResult.Succeeded(keys);
        }
        catch (DecoderFallbackException) { return ScriptStorageKeysResult.Failed("storage.invalid-utf8", "Stored key is not valid UTF-8."); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        { return ScriptStorageKeysResult.Failed("storage.io-failed", "Persistent storage enumeration failed."); }
    }

    public void Dispose() => _disposed = true;

    private FileStream OpenBounded(string pPath)
    {
        var info = new FileInfo(pPath);
        if (info.Length < HeaderBytes || info.Length > (long)HeaderBytes + MaxKeyBytes + MaxValueBytes)
            throw new InvalidDataException("Storage envelope size is outside bounds.");
        return new FileStream(pPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan);
    }

    private static bool TryReadEnvelope(Stream pStream, string? pExpectedKey, bool pReadValue, out string pKey, out string pValue)
    {
        pKey = ""; pValue = "";
        Span<byte> header = stackalloc byte[HeaderBytes];
        if (!ReadExactly(pStream, header) || !header[..8].SequenceEqual(Magic)) return false;
        var keyLength = BinaryPrimitives.ReadInt32LittleEndian(header[8..12]);
        var valueLength = BinaryPrimitives.ReadInt32LittleEndian(header[12..16]);
        if (keyLength is < 0 or > MaxKeyBytes || valueLength is < 0 or > MaxValueBytes
            || pStream.Length != (long)HeaderBytes + keyLength + valueLength) return false;
        var keyBytes = new byte[keyLength];
        if (!ReadExactly(pStream, keyBytes)) return false;
        pKey = Utf8.GetString(keyBytes);
        if (!ValidKey(pKey) || (pExpectedKey != null && !pKey.Equals(pExpectedKey, StringComparison.Ordinal))) return false;
        if (!pReadValue) { pStream.Seek(valueLength, SeekOrigin.Current); return pStream.Position == pStream.Length; }
        var valueBytes = new byte[valueLength];
        if (!ReadExactly(pStream, valueBytes)) return false;
        pValue = Utf8.GetString(valueBytes);
        return pStream.Position == pStream.Length && pValue.Length <= MemoryScriptKeyValueStorage.MaxValueCharacters;
    }

    private static bool ReadExactly(Stream pStream, Span<byte> pBuffer)
    {
        var read = 0;
        while (read < pBuffer.Length)
        {
            var count = pStream.Read(pBuffer[read..]);
            if (count <= 0) return false;
            read += count;
        }
        return true;
    }

    private string[] EnumerateEntries()
    {
        var files = Directory.EnumerateFiles(_root, "*" + Extension, SearchOption.TopDirectoryOnly)
            .Take(MemoryScriptKeyValueStorage.MaxEntries + 1).ToArray();
        if (files.Length > MemoryScriptKeyValueStorage.MaxEntries)
            throw new InvalidDataException("Storage contains too many entries.");
        return files;
    }

    private int CountEntries() => EnumerateEntries().Length;
    private string PathForKey(string pKey) => Path.Combine(_root, HashKey(pKey) + Extension);
    private static string HashKey(string pKey) => Convert.ToHexString(SHA256.HashData(Utf8.GetBytes(pKey))).ToLowerInvariant();
    private static bool ValidKey(string pKey) => pKey != null && pKey.Length <= MemoryScriptKeyValueStorage.MaxKeyCharacters;

    private void EnsureSafeRoot()
    {
        var attributes = File.GetAttributes(_root);
        if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Storage root must be a regular directory.");
    }

    private static void EnsureRegularFile(string pPath)
    {
        var attributes = File.GetAttributes(pPath);
        if ((attributes & FileAttributes.Directory) != 0 || (attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Storage entry is not a regular file.");
    }
}
