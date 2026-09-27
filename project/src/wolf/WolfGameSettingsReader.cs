using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>The decoded contents of a WOLF <c>Game.dat</c>.</summary>
public sealed class WolfGameSettings
{
    /// <summary>The file's own version byte, the one after the magic.</summary>
    public int Version { get; init; }

    /// <summary>True when the string record is the v3 shape.</summary>
    public bool IsVersion3 { get; init; }

    /// <summary>The eight one byte settings, in the order the file stores them.</summary>
    public IReadOnlyList<int> ByteSettings { get; init; } = [];

    /// <summary>The string settings, decoded with the encoding this version uses.</summary>
    public IReadOnlyList<string> StringSettings { get; init; } = [];

    /// <summary>The sixteen bit settings, in the order the file stores them.</summary>
    public IReadOnlyList<int> WordSettings { get; init; } = [];

    /// <summary>The game title, which is the first string setting.</summary>
    public string GameTitle => StringSettings.Count > 0 ? StringSettings[0] : "";

    /// <summary>
    /// The encryption key, which is the third string setting. This reader never
    /// uses it and never decrypts anything; it is reported so a protected game
    /// can be recognised instead of misread.
    /// </summary>
    public string EncryptionKey => StringSettings.Count > 2 ? StringSettings[2] : "";

    /// <summary>Whether the file declares an encryption key, from its length.</summary>
    public bool IsProtected => EncryptionKey.Length > 0;

    /// <summary>
    /// The WOLF editor version the file was written by, at index 16 of the word
    /// record.
    /// </summary>
    /// <remarks>
    /// The record is <c>unknown</c> at 0, then twelve custom move speeds for
    /// heroes and events at 1 to 12, <c>unknown_2</c> at 13, the screen width at
    /// 14, the height at 15 and the version at 16. Later versions append
    /// loading gauge fields behind the record's own length, so anything past 16
    /// is not guaranteed to exist.
    /// </remarks>
    public int WolfRpgVersion => WordSettings.Count > 16 ? WordSettings[16] : 0;
}

/// <summary>
/// Reads the WOLF <c>Game.dat</c> binary format as data only.
/// </summary>
/// <remarks>
/// <para>
/// The file is the most version dependent of the WOLF formats, and the version
/// byte changes the layout rather than just the meaning. A v2 file has eight
/// Shift-JIS string settings followed by one UTF-8 string, while a v3 file has
/// twelve UTF-8 strings and no trailing one. Reading a v2 file as a v3 file
/// produces eleven plausible wrong strings rather than an error, so the version
/// byte has to select the record shape before anything is read.
/// </para>
/// <para>
/// Two more things have to be read in the right order. The sixteen bit settings
/// carry their own length and a later version added fields behind that length,
/// so the record is bounded by it rather than by the number of fields known
/// here. And the file stores its own size, which is used to skip the trailing
/// static random bytes; treating that as a fixed block would misread every
/// other file.
/// </para>
/// <para>
/// Nothing is decrypted. The encryption key is read because it is in the file
/// and its presence is what marks a protected game, not because this reader can
/// act on it.
/// </para>
/// <para>
/// This reader is structural only. It is validated against byte sequences built
/// from the specification, not against a real WOLF game.
/// </para>
/// </remarks>
public sealed class WolfGameSettingsReader
{
    /// <summary>The magic, <c>0 'W' 0 0 'O' 'L' 0 'F' 'M'</c>.</summary>
    private static readonly byte[] FileMagic =
        [0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C, 0x00, 0x46, 0x4D];

    /// <summary>The v2 string count, from <c>record_string_settings_v2</c>.</summary>
    private const int V2StringCount = 8;

    /// <summary>The v3 string count, from <c>record_string_settings_v3</c>.</summary>
    private const int V3StringCount = 12;

    /// <summary>The version header byte for v2.</summary>
    private const byte VersionHeaderV2 = 0x00;

    /// <summary>The version header byte for v3.</summary>
    private const byte VersionHeaderV3 = 0x55;

    private readonly WolfParseLimits _limits;

    public WolfGameSettingsReader(WolfParseLimits pLimits)
    {
        _limits = pLimits ?? throw new ArgumentNullException(nameof(pLimits));
    }

    /// <summary>True when the payload carries the verified Game.dat header.</summary>
    public static bool HasGameHeader(byte[] pBytes)
    {
        if (pBytes == null || pBytes.Length < FileMagic.Length + 1 + 4)
        {
            return false;
        }
        for (var index = 0; index < FileMagic.Length; index++)
        {
            if (pBytes[index] != FileMagic[index])
            {
                return false;
            }
        }
        var versionHeader = pBytes[FileMagic.Length];
        return versionHeader is VersionHeaderV2 or VersionHeaderV3;
    }

    /// <summary>Reads a Game.dat file.</summary>
    public PluginResult<WolfGameSettings> Read(byte[] pBytes, string pSourceName)
    {
        if (pBytes == null)
        {
            throw new ArgumentNullException(nameof(pBytes));
        }
        if (pBytes.LongLength > _limits.MaxFileBytes)
        {
            return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} is {pBytes.LongLength} bytes, over the {_limits.MaxFileBytes} byte limit");
        }
        if (!HasGameHeader(pBytes))
        {
            return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} does not carry the verified WOLF/FM header");
        }

        var offset = FileMagic.Length;
        var version = pBytes[offset];
        offset += 1;
        var isVersion3 = version == VersionHeaderV3;

        var byteCount = ReadInt32(pBytes, ref offset, pSourceName, "byte settings length");
        if (!byteCount.Success)
        {
            return Refuse<WolfGameSettings>(byteCount);
        }
        if (byteCount.Value < 0 || byteCount.Value > 64)
        {
            return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} declares {byteCount.Value} byte settings, outside 0 to 64");
        }
        var byteSettings = new List<int>(byteCount.Value);
        for (var index = 0; index < byteCount.Value; index++)
        {
            if (offset + 1 > pBytes.Length)
            {
                return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} ends inside the byte settings");
            }
            byteSettings.Add(pBytes[offset]);
            offset += 1;
        }

        // The version byte has to pick the record shape here, because a v2 file
        // read as a v3 file yields eleven plausible but wrong strings.
        var stringCount = isVersion3 ? V3StringCount : V2StringCount + 1;
        var strings = new List<string>(stringCount);
        for (var index = 0; index < stringCount; index++)
        {
            // A v2 file's first eight strings are Shift-JIS and the trailing one
            // is UTF-8, which is the one asymmetry in the record.
            var shiftJis = !isVersion3 && index < V2StringCount;
            var value = ReadString(pBytes, ref offset, pSourceName, $"string setting {index}", shiftJis);
            if (!value.Success)
            {
                return Refuse<WolfGameSettings>(value);
            }
            strings.Add(value.Value);
        }

        // The file stores its own size, which is what bounds the static random
        // bytes at the end rather than a fixed block length.
        var declaredSize = ReadInt32(pBytes, ref offset, pSourceName, "file size");
        if (!declaredSize.Success)
        {
            return Refuse<WolfGameSettings>(declaredSize);
        }
        var fileSize = declaredSize.Value;
        if (fileSize < offset || fileSize > pBytes.Length)
        {
            return Fail<WolfGameSettings>(
                $"WOLF Game.dat {pSourceName} declares size {fileSize}, outside {offset} to {pBytes.Length}");
        }
        // unknown3 sits between the size and the word settings.
        offset += 4;

        var wordLength = ReadInt32(pBytes, ref offset, pSourceName, "word settings length");
        if (!wordLength.Success)
        {
            return Refuse<WolfGameSettings>(wordLength);
        }
        // A later version added fields behind this length, so the record is
        // bounded by it rather than by the number of fields known here.
        if (wordLength.Value < 0 || wordLength.Value > 64)
        {
            return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} declares {wordLength.Value} word settings, outside 0 to 64");
        }
        var wordSettings = new List<int>(wordLength.Value);
        for (var index = 0; index < wordLength.Value; index++)
        {
            if (offset + 2 > fileSize)
            {
                return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} ends inside the word settings at {index}");
            }
            wordSettings.Add(pBytes[offset] | (pBytes[offset + 1] << 8));
            offset += 2;
        }

        // Everything up to the declared size is the static random block, which
        // this reader skips rather than decodes: it is editor data with no
        // meaning at runtime, and its length varies per file.
        var randomBytes = fileSize - offset;
        if (randomBytes < 0)
        {
            return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} has {randomBytes} random bytes, which cannot happen");
        }
        offset = fileSize;

        if (offset + 1 > pBytes.Length)
        {
            return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} ends before the version footer");
        }
        var footer = pBytes[offset];
        offset += 1;
        if (offset != pBytes.Length)
        {
            return Fail<WolfGameSettings>($"WOLF Game.dat {pSourceName} has {pBytes.Length - offset} bytes after the version footer");
        }

        return PluginResult<WolfGameSettings>.Succeeded(new WolfGameSettings
        {
            Version = version,
            IsVersion3 = isVersion3,
            ByteSettings = byteSettings,
            StringSettings = strings,
            WordSettings = wordSettings,
        });
    }

    private PluginResult<int> ReadInt32(byte[] pBytes, ref int pOffset, string pSourceName, string pWhat)
    {
        if (pOffset + 4 > pBytes.Length)
        {
            return Fail<int>($"WOLF Game.dat {pSourceName} ends before the {pWhat} value");
        }
        var value = pBytes[pOffset]
            | (pBytes[pOffset + 1] << 8)
            | (pBytes[pOffset + 2] << 16)
            | (pBytes[pOffset + 3] << 24);
        pOffset += 4;
        return PluginResult<int>.Succeeded(value);
    }

    /// <summary>
    /// Reads a length-prefixed NUL terminated string, in the encoding this
    /// record uses.
    /// </summary>
    /// <remarks>
    /// A v2 file mixes encodings inside one record: the first eight strings are
    /// Shift-JIS and the trailing one is UTF-8. Decoding both as UTF-8 turns a
    /// Japanese title into replacement characters, and decoding both as Shift-JIS
    /// turns a UTF-8 name into mojibake, so the encoding is a parameter rather
    /// than a constant of the reader.
    /// </remarks>
    private PluginResult<string> ReadString(
        byte[] pBytes, ref int pOffset, string pSourceName, string pWhat, bool pShiftJis)
    {
        var length = ReadInt32(pBytes, ref pOffset, pSourceName, $"{pWhat} length");
        if (!length.Success)
        {
            return Refuse<string>(length);
        }
        if (length.Value < 0 || length.Value > _limits.MaxStringBytes)
        {
            return Fail<string>($"WOLF Game.dat {pSourceName} {pWhat} declares {length.Value} bytes, outside 0 to {_limits.MaxStringBytes}");
        }
        if (pOffset + length.Value + 1 > pBytes.Length)
        {
            return Fail<string>($"WOLF Game.dat {pSourceName} ends inside the {pWhat}");
        }
        if (pBytes[pOffset + length.Value] != 0)
        {
            return Fail<string>($"WOLF Game.dat {pSourceName} {pWhat} is not NUL terminated at {length.Value}");
        }

        // Both encodings are decoded strictly, so bytes that are not valid in
        // the declared encoding are reported rather than silently replaced. A
        // replacement character in a game title looks like a real title.
        var encoding = pShiftJis ? Encoding.GetEncoding(932) : new UTF8Encoding(false, true);
        string text;
        try
        {
            text = encoding.GetString(pBytes, pOffset, length.Value);
        }
        catch (DecoderFallbackException)
        {
            return Fail<string>($"WOLF Game.dat {pSourceName} {pWhat} is not valid {(pShiftJis ? "Shift-JIS" : "UTF-8")}");
        }
        catch (ArgumentException)
        {
            return Fail<string>($"WOLF Game.dat {pSourceName} {pWhat} needs code page 932, which this build does not have");
        }
        pOffset += length.Value + 1;
        return PluginResult<string>.Succeeded(text);
    }

    private static PluginResult<T> Fail<T>(string pMessage)
    {
        return PluginResult<T>.Failed(PluginError.Create(
            PluginErrorCode.InvalidGame,
            pMessage,
            EnginePluginIds.WolfRpg,
            "wolf-game-settings"));
    }

    private static PluginResult<T> Refuse<T>(PluginResult<int> pFailed)
    {
        return PluginResult<T>.Failed(pFailed.Error!);
    }

    private static PluginResult<T> Refuse<T>(PluginResult<string> pFailed)
    {
        return PluginResult<T>.Failed(pFailed.Error!);
    }
}
