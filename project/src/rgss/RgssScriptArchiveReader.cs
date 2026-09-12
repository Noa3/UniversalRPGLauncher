using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UniversalRPG.Core;
using UniversalRPG.Sdk;

namespace UniversalRPG.Rgss;

public enum RgssGeneration
{
    Rgss1,
    Rgss2,
    Rgss3,
}

public sealed class RgssScriptArchiveLimits
{
    public int MaxScripts { get; init; } = 10_000;
    public int MaxArchiveBytes { get; init; } = 64 * 1024 * 1024;
    public int MaxCompressedScriptBytes { get; init; } = 16 * 1024 * 1024;
    public int MaxDecompressedScriptBytes { get; init; } = 32 * 1024 * 1024;
    public int MaxTotalDecompressedBytes { get; init; } = 128 * 1024 * 1024;
    public int MaxMarshalDepth { get; init; } = 64;

    public bool IsValid()
    {
        return MaxScripts is > 0 and <= 100_000
            && MaxArchiveBytes is > 0 and <= 512 * 1024 * 1024
            && MaxCompressedScriptBytes is > 0 and <= 128 * 1024 * 1024
            && MaxDecompressedScriptBytes is > 0 and <= 256 * 1024 * 1024
            && MaxTotalDecompressedBytes >= MaxDecompressedScriptBytes
            && MaxTotalDecompressedBytes <= 1024 * 1024 * 1024
            && MaxMarshalDepth is >= 8 and <= 256;
    }
}

public sealed class RgssScriptEntry
{
    public int ArchiveIndex { get; init; }
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public byte[] Source { get; init; } = Array.Empty<byte>();
    public EngineScriptDescriptor Descriptor { get; init; } = new();
}

public sealed class RgssScriptArchiveResult
{
    public bool Success { get; init; }
    public string Error { get; init; } = "";
    public IReadOnlyList<RgssScriptEntry> Scripts { get; init; } = Array.Empty<RgssScriptEntry>();
}

/// <summary>
/// Reads the bounded subset of Ruby Marshal used by RPG Maker XP/VX/VX Ace
/// Scripts.rxdata/Scripts.rvdata/Scripts.rvdata2 archives. Script entries are
/// expected to be [id, name, zlib-compressed-source]. Ruby code is never
/// evaluated by this reader.
/// </summary>
public static class RgssScriptArchiveReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static RgssScriptArchiveResult Read(
        string pPath,
        RgssGeneration pGeneration,
        RgssScriptArchiveLimits? pLimits = null)
    {
        var limits = pLimits ?? new RgssScriptArchiveLimits();
        if (!limits.IsValid())
        {
            return Failure("RGSS script archive limits are outside the supported bounded range.");
        }
        if (string.IsNullOrWhiteSpace(pPath) || !File.Exists(pPath))
        {
            return Failure("RGSS script archive file was not found.");
        }

        try
        {
            var info = new FileInfo(pPath);
            if (info.Length < 2 || info.Length > limits.MaxArchiveBytes)
            {
                return Failure($"RGSS script archive size {info.Length} is outside the bounded limit.");
            }
            return Read(File.ReadAllBytes(pPath), pGeneration, limits);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failure($"RGSS script archive could not be read: {exception.Message}");
        }
    }

    public static RgssScriptArchiveResult Read(
        byte[] pArchive,
        RgssGeneration pGeneration,
        RgssScriptArchiveLimits? pLimits = null)
    {
        var limits = pLimits ?? new RgssScriptArchiveLimits();
        if (!limits.IsValid())
        {
            return Failure("RGSS script archive limits are outside the supported bounded range.");
        }
        if (pArchive == null || pArchive.Length < 2 || pArchive.Length > limits.MaxArchiveBytes)
        {
            return Failure("RGSS script archive is empty, truncated, or exceeds the bounded limit.");
        }

        try
        {
            var marshal = new RubyMarshalSubsetReader(pArchive, limits.MaxMarshalDepth);
            var root = marshal.ReadRoot();
            if (root is not List<object?> rootArray)
            {
                return Failure("RGSS Scripts archive root is not a Ruby Marshal array.");
            }
            if (rootArray.Count > limits.MaxScripts)
            {
                return Failure($"RGSS Scripts archive contains {rootArray.Count} entries, above the {limits.MaxScripts} limit.");
            }

            var scripts = new List<RgssScriptEntry>(rootArray.Count);
            var totalDecompressed = 0;
            var decoder = new LegacyTextDecoder();
            for (var index = 0; index < rootArray.Count; index++)
            {
                if (rootArray[index] is not List<object?> entry || entry.Count < 3)
                {
                    return Failure($"RGSS script entry {index} is not a three-element Marshal array.");
                }
                if (entry[0] is not int id
                    || entry[1] is not MarshalBytes nameBytes
                    || entry[2] is not MarshalBytes compressed)
                {
                    return Failure($"RGSS script entry {index} has unsupported id/name/source types.");
                }
                if (compressed.Data.Length > limits.MaxCompressedScriptBytes)
                {
                    return Failure($"RGSS script entry {index} compressed source exceeds the bounded limit.");
                }

                var source = InflateBounded(compressed.Data, limits.MaxDecompressedScriptBytes, out var inflateError);
                if (source == null)
                {
                    return Failure($"RGSS script entry {index} could not be decompressed: {inflateError}");
                }
                totalDecompressed = checked(totalDecompressed + source.Length);
                if (totalDecompressed > limits.MaxTotalDecompressedBytes)
                {
                    return Failure("Total RGSS script source exceeds the bounded decompression limit.");
                }

                var name = DecodeScriptName(nameBytes, decoder, index);
                var hash = Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
                scripts.Add(new RgssScriptEntry
                {
                    ArchiveIndex = index,
                    Id = id,
                    Name = name,
                    Source = source,
                    Descriptor = new EngineScriptDescriptor
                    {
                        Id = $"rgss-script:{index}",
                        DisplayName = name,
                        LanguageId = LanguageId(pGeneration),
                        RelativePath = ScriptArchiveName(pGeneration),
                        Sha256 = hash,
                        Origin = ScriptOrigin.Game,
                        Required = true,
                        LoadOrder = index,
                    },
                });
            }

            return new RgssScriptArchiveResult
            {
                Success = true,
                Scripts = scripts,
            };
        }
        catch (InvalidDataException exception)
        {
            return Failure(exception.Message);
        }
        catch (OverflowException)
        {
            return Failure("RGSS script archive exceeded bounded integer limits.");
        }
    }

    private static string DecodeScriptName(MarshalBytes pBytes, LegacyTextDecoder pDecoder, int pIndex)
    {
        if (pBytes.Data.Length == 0) return "";
        try
        {
            if (pBytes.EncodingName.Equals("UTF-8", StringComparison.OrdinalIgnoreCase))
            {
                return StrictUtf8.GetString(pBytes.Data);
            }
            if (pBytes.EncodingName.Equals("US-ASCII", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.ASCII.GetString(pBytes.Data);
            }
        }
        catch (DecoderFallbackException)
        {
            return $"Script {pIndex}";
        }

        var decoded = pDecoder.Decode(pBytes.Data);
        return string.IsNullOrEmpty(decoded) ? $"Script {pIndex}" : decoded;
    }

    private static string LanguageId(RgssGeneration pGeneration) => pGeneration switch
    {
        RgssGeneration.Rgss1 => ScriptLanguageIds.Rgss1Ruby,
        RgssGeneration.Rgss2 => ScriptLanguageIds.Rgss2Ruby,
        _ => ScriptLanguageIds.Rgss3Ruby,
    };

    private static string ScriptArchiveName(RgssGeneration pGeneration) => pGeneration switch
    {
        RgssGeneration.Rgss1 => "Data/Scripts.rxdata",
        RgssGeneration.Rgss2 => "Data/Scripts.rvdata",
        _ => "Data/Scripts.rvdata2",
    };

    private static byte[]? InflateBounded(byte[] pCompressed, int pLimit, out string pError)
    {
        pError = "";
        try
        {
            using var input = new MemoryStream(pCompressed, writable: false);
            using var inflater = new ZLibStream(input, CompressionMode.Decompress, leaveOpen: false);
            using var output = new MemoryStream(Math.Min(Math.Max(pCompressed.Length * 2, 256), pLimit));
            var buffer = new byte[8192];
            while (true)
            {
                var read = inflater.Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (output.Length + read > pLimit)
                {
                    pError = $"decompressed source exceeds {pLimit} bytes";
                    return null;
                }
                output.Write(buffer, 0, read);
            }
            return output.ToArray();
        }
        catch (InvalidDataException exception)
        {
            pError = exception.Message;
            return null;
        }
    }

    private static RgssScriptArchiveResult Failure(string pMessage)
        => new() { Success = false, Error = pMessage ?? "Unknown RGSS script archive error." };

    private sealed class MarshalBytes
    {
        public MarshalBytes(byte[] pData) => Data = pData;
        public byte[] Data { get; }
        public string EncodingName { get; set; } = "";
    }

    private sealed class RubyMarshalSubsetReader
    {
        private readonly byte[] _data;
        private readonly int _maxDepth;
        private readonly List<object?> _objects = new();
        private readonly List<string> _symbols = new();
        private int _offset;

        public RubyMarshalSubsetReader(byte[] pData, int pMaxDepth)
        {
            _data = pData;
            _maxDepth = pMaxDepth;
        }

        public object? ReadRoot()
        {
            if (ReadByte() != 4 || ReadByte() != 8)
            {
                throw Error("Unsupported Ruby Marshal header; expected version 4.8.");
            }
            var root = ReadObject(0);
            if (_offset != _data.Length)
            {
                throw Error("Ruby Marshal payload contains trailing bytes.");
            }
            return root;
        }

        private object? ReadObject(int pDepth)
        {
            if (pDepth > _maxDepth)
            {
                throw Error("Ruby Marshal nesting exceeds the bounded depth.");
            }

            var type = (char)ReadByte();
            return type switch
            {
                '0' => null,
                'T' => true,
                'F' => false,
                'i' => ReadFixnum(),
                '"' => ReadString(),
                '[' => ReadArray(pDepth + 1),
                ':' => ReadSymbol(),
                ';' => ReadSymbolLink(),
                '@' => ReadObjectLink(),
                'I' => ReadIvarObject(pDepth + 1),
                _ => throw Error($"Unsupported Ruby Marshal type marker 0x{(byte)type:X2} ('{type}')."),
            };
        }

        private MarshalBytes ReadString()
        {
            var length = ReadCollectionLength("string");
            var bytes = ReadBytes(length);
            var value = new MarshalBytes(bytes);
            _objects.Add(value);
            return value;
        }

        private List<object?> ReadArray(int pDepth)
        {
            var count = ReadCollectionLength("array");
            if (count > 100_000)
            {
                throw Error("Ruby Marshal array exceeds the bounded element limit.");
            }
            var result = new List<object?>(count);
            _objects.Add(result);
            for (var index = 0; index < count; index++)
            {
                result.Add(ReadObject(pDepth));
            }
            return result;
        }

        private object? ReadIvarObject(int pDepth)
        {
            var value = ReadObject(pDepth);
            var count = ReadCollectionLength("instance-variable table");
            if (count > 256)
            {
                throw Error("Ruby Marshal instance-variable table exceeds the bounded limit.");
            }
            for (var index = 0; index < count; index++)
            {
                var key = ReadObject(pDepth);
                var attribute = ReadObject(pDepth);
                if (value is MarshalBytes bytes && key is string name)
                {
                    ApplyStringIvar(bytes, name, attribute);
                }
            }
            return value;
        }

        private static void ApplyStringIvar(MarshalBytes pBytes, string pName, object? pValue)
        {
            if (pName == "E" && pValue is bool encoded)
            {
                pBytes.EncodingName = encoded ? "UTF-8" : "US-ASCII";
                return;
            }
            if (pName.Equals("encoding", StringComparison.OrdinalIgnoreCase))
            {
                if (pValue is string symbol)
                {
                    pBytes.EncodingName = symbol;
                }
                else if (pValue is MarshalBytes nameBytes && nameBytes.Data.Length <= 128)
                {
                    pBytes.EncodingName = Encoding.ASCII.GetString(nameBytes.Data);
                }
            }
        }

        private string ReadSymbol()
        {
            var length = ReadCollectionLength("symbol");
            if (length > 4096)
            {
                throw Error("Ruby Marshal symbol exceeds the bounded length.");
            }
            var symbol = Encoding.ASCII.GetString(ReadBytes(length));
            _symbols.Add(symbol);
            return symbol;
        }

        private string ReadSymbolLink()
        {
            var index = ReadFixnum();
            if (index < 0 || index >= _symbols.Count)
            {
                throw Error($"Ruby Marshal symbol link {index} is outside the symbol table.");
            }
            return _symbols[index];
        }

        private object? ReadObjectLink()
        {
            var index = ReadFixnum();
            if (index < 0 || index >= _objects.Count)
            {
                throw Error($"Ruby Marshal object link {index} is outside the object table.");
            }
            return _objects[index];
        }

        private int ReadCollectionLength(string pLabel)
        {
            var value = ReadFixnum();
            if (value < 0)
            {
                throw Error($"Ruby Marshal {pLabel} has negative length {value}.");
            }
            return value;
        }

        private int ReadFixnum()
        {
            var first = unchecked((sbyte)ReadByte());
            if (first == 0) return 0;
            if (first >= 5) return first - 5;
            if (first <= -5) return first + 5;

            if (first > 0)
            {
                long value = 0;
                for (var index = 0; index < first; index++)
                {
                    value |= (long)ReadByte() << (8 * index);
                }
                if (value > int.MaxValue) throw Error("Ruby Marshal fixnum exceeds Int32 range.");
                return (int)value;
            }

            var byteCount = -first;
            long negative = -1;
            for (var index = 0; index < byteCount; index++)
            {
                negative &= ~(0xFFL << (8 * index));
                negative |= (long)ReadByte() << (8 * index);
            }
            if (negative < int.MinValue || negative > int.MaxValue)
            {
                throw Error("Ruby Marshal negative fixnum exceeds Int32 range.");
            }
            return (int)negative;
        }

        private byte ReadByte()
        {
            if (_offset >= _data.Length)
            {
                throw Error("Ruby Marshal payload ended unexpectedly.");
            }
            return _data[_offset++];
        }

        private byte[] ReadBytes(int pCount)
        {
            if (pCount < 0 || pCount > _data.Length - _offset)
            {
                throw Error("Ruby Marshal byte string exceeds remaining payload.");
            }
            var result = new byte[pCount];
            Buffer.BlockCopy(_data, _offset, result, 0, pCount);
            _offset += pCount;
            return result;
        }

        private InvalidDataException Error(string pMessage)
            => new($"Offset 0x{_offset:X}: {pMessage}");
    }
}
