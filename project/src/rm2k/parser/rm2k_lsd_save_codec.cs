using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Read-only, bounded model of the original RM2K/RM2K3 LSD container.
/// Payloads remain data; no event command or plugin content is executed.
/// </summary>
public sealed class Rm2kLsdChunk
{
    public int Id { get; init; }
    public int Length { get; init; }
    public int Offset { get; init; }
    public int PayloadOffset { get; init; }
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public bool IsTerminator { get; init; }
}

public sealed class Rm2kLsdSaveModel
{
    internal Rm2kLsdSaveModel(IReadOnlyList<Rm2kLsdChunk> pChunks)
    {
        Chunks = pChunks;
        var unknown = new List<Rm2kLsdChunk>();
        foreach (var chunk in pChunks)
        {
            if (!Rm2kLsdSaveCodec.KnownTopLevelChunkIds.Contains(chunk.Id))
            {
                unknown.Add(chunk);
            }
        }
        UnknownChunks = unknown;
    }

    public IReadOnlyList<Rm2kLsdChunk> Chunks { get; }
    public IReadOnlyList<Rm2kLsdChunk> UnknownChunks { get; }
    public int UnknownChunkCount => UnknownChunks.Count;
}

public static class Rm2kLsdSaveCodec
{
    public const int MaxFileBytes = 4 * 1024 * 1024;
    public const int MaxChunkBytes = 1024 * 1024;
    public const int MaxChunks = 10_000;
    public const string Header = Rm2kParser.LsdHeader;

    // These IDs are only classification metadata. Unknown IDs remain raw.
    internal static readonly HashSet<int> KnownTopLevelChunkIds = new()
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10,
    };

    public static bool TryReadFile(
        string pSaveDirectory,
        string pSlot,
        out Rm2kLsdSaveModel? pModel,
        out string pError)
    {
        pModel = null;
        pError = "";
        if (!TryResolveSlot(pSaveDirectory, pSlot, out var path, out pError))
        {
            return false;
        }
        try
        {
            if (!File.Exists(path))
            {
                pError = "LSD save slot does not exist.";
                return false;
            }
            var info = new FileInfo(path);
            if (info.Length > MaxFileBytes)
            {
                pError = $"LSD save exceeds the {MaxFileBytes}-byte limit.";
                return false;
            }
            var bytes = File.ReadAllBytes(path);
            return TryReadBytes(bytes, out pModel, out pError);
        }
        catch (IOException exception)
        {
            pError = $"Could not read LSD save: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            pError = $"Could not read LSD save: {exception.Message}";
            return false;
        }
    }

    public static bool TryReadBytes(
        byte[] pBytes,
        out Rm2kLsdSaveModel? pModel,
        out string pError)
    {
        pModel = null;
        pError = "";
        if (pBytes == null || pBytes.Length > MaxFileBytes)
        {
            pError = $"LSD save is empty or exceeds the {MaxFileBytes}-byte limit.";
            return false;
        }

        var reader = new LcfBinaryReader(pBytes);
        reader.ReadHeader(Header);
        if (reader.HasError())
        {
            pError = reader.ErrorMessage;
            return false;
        }

        var chunks = new List<Rm2kLsdChunk>();
        var terminated = false;
        while (!reader.IsEof())
        {
            if (chunks.Count >= MaxChunks)
            {
                pError = $"LSD chunk count exceeds the {MaxChunks}-chunk limit.";
                return false;
            }
            var chunk = reader.ReadChunk();
            if (reader.HasError())
            {
                pError = reader.ErrorMessage;
                return false;
            }
            if ((bool)chunk["terminator"])
            {
                if (!reader.IsEof())
                {
                    pError = "Trailing data after LSD terminator.";
                    return false;
                }
                terminated = true;
                break;
            }
            var length = (int)chunk["length"];
            if (length > MaxChunkBytes)
            {
                pError = $"LSD chunk exceeds the {MaxChunkBytes}-byte limit.";
                return false;
            }
            chunks.Add(new Rm2kLsdChunk
            {
                Id = (int)chunk["id"],
                Length = length,
                Offset = (int)chunk["offset"],
                PayloadOffset = (int)chunk["payload_offset"],
                Data = (byte[])chunk["data"],
            });
        }
        if (!terminated)
        {
            pError = "LSD save is missing its terminator.";
            return false;
        }
        pModel = new Rm2kLsdSaveModel(chunks);
        return true;
    }

    private static bool TryResolveSlot(
        string pSaveDirectory,
        string pSlot,
        out string pPath,
        out string pError)
    {
        pPath = "";
        pError = "";
        if (string.IsNullOrWhiteSpace(pSaveDirectory)
            || string.IsNullOrWhiteSpace(pSlot)
            || pSlot.IndexOf('\0') >= 0
            || pSlot is "." or ".."
            || pSlot.IndexOfAny(new[] { '/', '\\', ':' }) >= 0
            || Path.IsPathFullyQualified(pSlot))
        {
            pError = "LSD save directory or slot name is invalid.";
            return false;
        }
        try
        {
            var root = Path.GetFullPath(pSaveDirectory);
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(root, pSlot));
            if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                pError = "LSD save slot escapes the save directory.";
                return false;
            }
            pPath = candidate;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            pError = "LSD save directory or slot name is invalid.";
            return false;
        }
    }
}
