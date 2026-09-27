using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>
/// Reads the verified binary WolfRPG MapData (.mps) format as data only.
///
/// Every field order, constant and digit split in this reader is taken from the
/// published .mps format description. Nothing is inferred from a directory
/// listing and no archive is decrypted. A file that does not match the framing
/// is reported as such instead of being partially decoded, because a silent
/// partial map is worse than a refusal: it looks like a real game with a hole
/// in it.
///
/// This reader is structural only. It is validated against byte sequences built
/// from the specification, not against a real WOLF game, so the framing and the
/// field order are proven but the interpretation of any single field is not.
/// </summary>
public sealed class WolfBinaryMapReader
{
    private static readonly byte[] MapMagic = [0x57, 0x4F, 0x4C, 0x46, 0x4D]; // WOLFM

    private readonly WolfParseLimits _limits;

    public WolfBinaryMapReader(WolfParseLimits pLimits)
    {
        _limits = pLimits ?? throw new ArgumentNullException(nameof(pLimits));
    }

    /// <summary>True when the payload carries the verified WOLFM header.</summary>
    public static bool HasMapHeader(byte[] pBytes)
    {
        if (pBytes.Length < 17)
        {
            return false;
        }
        for (var index = 0; index < 10; index++)
        {
            if (pBytes[index] != 0)
            {
                return false;
            }
        }
        for (var index = 0; index < MapMagic.Length; index++)
        {
            if (pBytes[10 + index] != MapMagic[index])
            {
                return false;
            }
        }
        return pBytes[15] == 0;
    }

    public PluginResult<WolfBinaryMapData> Read(byte[] pBytes, int pMapId, string pSourceName)
    {
        ArgumentNullException.ThrowIfNull(pBytes);
        if (!_limits.IsValid())
        {
            return Failed<WolfBinaryMapData>("The WOLF parse limits are not valid.", pSourceName);
        }
        if (pBytes.Length > _limits.MaxFileBytes)
        {
            return Failed<WolfBinaryMapData>("The WOLF map file exceeds the bounded file-size limit.", pSourceName);
        }
        if (!HasMapHeader(pBytes))
        {
            return Failed<WolfBinaryMapData>(
                "The WOLF map file does not carry the verified WOLFM header.", pSourceName);
        }

        var cursor = new WolfByteCursor(pBytes, pSourceName);
        try
        {
            cursor.Skip(10);
            cursor.Skip(MapMagic.Length);
            cursor.Skip(1);

            // version_header: 0x00 is v2, 0x55 is v3.
            var versionHeader = cursor.ReadByte();
            if (versionHeader is not (0x00 or 0x55))
            {
                return Failed<WolfBinaryMapData>(
                    $"Unknown WOLF version header 0x{versionHeader:X2}.", pSourceName);
            }

            cursor.Skip(3);
            var marker = cursor.ReadUInt32();
            if (marker != 0x64u)
            {
                return Failed<WolfBinaryMapData>(
                    $"Unexpected WOLF map marker 0x{marker:X}, expected 0x64.", pSourceName);
            }

            var versionByte = cursor.ReadByte();
            if (versionByte is not (0x65 or 0x66))
            {
                return Failed<WolfBinaryMapData>(
                    $"Unknown WOLF map version 0x{versionByte:X2}.", pSourceName);
            }

            var title = cursor.ReadLengthPrefixedString(_limits.MaxStringBytes);
            var tileset = (int)cursor.ReadUInt32();
            var width = (int)cursor.ReadUInt32();
            var height = (int)cursor.ReadUInt32();
            var eventCount = (int)cursor.ReadUInt32();

            if (width <= 0 || height <= 0
                || width > _limits.MaxMapDimension || height > _limits.MaxMapDimension)
            {
                return Failed<WolfBinaryMapData>(
                    $"WOLF map '{pSourceName}' has invalid dimensions {width}x{height}.", pSourceName);
            }
            var expectedPixels = checked((int)(width * height));
            if (expectedPixels > _limits.MaxMapTiles)
            {
                return Failed<WolfBinaryMapData>(
                    $"WOLF map '{pSourceName}' exceeds the tile limit.", pSourceName);
            }
            if (eventCount > _limits.MaxEventsPerMap)
            {
                return Failed<WolfBinaryMapData>(
                    $"WOLF map '{pSourceName}' exceeds the event limit.", pSourceName);
            }

            // A first pixel of 0xFFFFFFFF means the map does not exist. That is
            // a documented quirk of the format, not a decode failure.
            var firstPixel = cursor.ReadUInt32();
            var pixels = new List<WolfBinaryMapPixel>(expectedPixels);
            var mapExists = firstPixel != 0xFFFFFFFFu;
            if (mapExists)
            {
                pixels.Add(DecodePixel((int)firstPixel));
                var remaining = expectedPixels - 1;
                for (var index = 0; index < remaining; index++)
                {
                    pixels.Add(DecodePixel((int)cursor.ReadUInt32()));
                }
                // Each pixel carries two further u4 values the format uses for
                // the base tile. They are consumed so the event block starts at
                // the right offset; their meaning is not interpreted here.
                for (var index = 0; index < expectedPixels; index++)
                {
                    cursor.ReadUInt32();
                    cursor.ReadUInt32();
                }
            }

            var events = new List<WolfBinaryEvent>((int)eventCount);
            for (var index = 0; index < eventCount; index++)
            {
                var result = ReadEvent(cursor, _limits);
                if (!result.Success)
                {
                    return Failed<WolfBinaryMapData>(result.Error!.Message, pSourceName);
                }
                events.Add(result.Value!);
            }

            var footer = cursor.ReadByte();
            if (footer != 0x66)
            {
                return Failed<WolfBinaryMapData>(
                    $"WOLF map '{pSourceName}' is missing its 0x66 footer, so the framing is wrong.", pSourceName);
            }

            return PluginResult<WolfBinaryMapData>.Succeeded(new WolfBinaryMapData
            {
                MapId = pMapId,
                Title = title,
                TilesetId = tileset,
                Width = (int)width,
                Height = (int)height,
                MapExists = mapExists,
                FormatVersionByte = versionByte,
                Pixels = pixels,
                Events = events,
            });
        }
        catch (WolfFormatException exception)
        {
            return Failed<WolfBinaryMapData>(exception.Message, pSourceName);
        }
    }

    /// <summary>
    /// Decodes one mappixel from its first u4. The format description defines
    /// the autotile id as raw / 100000 and the four corner modes as the
    /// remaining decimal digits, so the split is arithmetic, not a table.
    /// </summary>
    private static WolfBinaryMapPixel DecodePixel(int pRaw)
    {
        if (pRaw < 0)
        {
            return new WolfBinaryMapPixel { Raw = pRaw };
        }
        var autotileId = pRaw / 100_000;
        return new WolfBinaryMapPixel
        {
            Raw = pRaw,
            AutotileId = autotileId,
            HasAutotile = autotileId > 0,
            AutotileTopLeft = pRaw % 10_000 / 1_000,
            AutotileTopRight = pRaw % 1_000 / 100,
            AutotileBottomLeft = pRaw % 100 / 10,
            AutotileBottomRight = pRaw % 10,
        };
    }

    private static PluginResult<WolfBinaryEvent> ReadEvent(WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        var header = pCursor.ReadByte();
        if (header != 0x6F)
        {
            return PluginResult<WolfBinaryEvent>.Failed(PluginError.Create(
                PluginErrorCode.InvalidGame,
                $"WOLF event header 0x{header:X2} is not the expected 0x6F.",
                EnginePluginIds.WolfRpg, "wolf-event"), Array.Empty<PluginDiagnostic>());
        }
        var signature = pCursor.ReadUInt32();
        if (signature != 0x3039u)
        {
            return PluginResult<WolfBinaryEvent>.Failed(PluginError.Create(
                PluginErrorCode.InvalidGame,
                $"WOLF event signature 0x{signature:X} is not the expected 0x3039.",
                EnginePluginIds.WolfRpg, "wolf-event"), Array.Empty<PluginDiagnostic>());
        }

        var id = (int)pCursor.ReadUInt32();
        var title = pCursor.ReadLengthPrefixedString(pLimits.MaxStringBytes);
        var mapX = (int)pCursor.ReadUInt32();
        var mapY = (int)pCursor.ReadUInt32();
        var pageCount = (int)pCursor.ReadUInt32();
        pCursor.ReadUInt32(); // a verified zero separator

        var pages = new List<WolfBinaryEventPage>((int)pageCount);
        for (var index = 0; index < pageCount; index++)
        {
            var page = ReadPage(pCursor, pLimits);
            if (!page.Success)
            {
                return PluginResult<WolfBinaryEvent>.Failed(page.Error!, page.Diagnostics);
            }
            pages.Add(page.Value!);
        }
        var footer = pCursor.ReadByte();
        if (footer != 0x70)
        {
            return PluginResult<WolfBinaryEvent>.Failed(PluginError.Create(
                PluginErrorCode.InvalidGame,
                $"WOLF event is missing its 0x70 footer.",
                EnginePluginIds.WolfRpg, "wolf-event"), Array.Empty<PluginDiagnostic>());
        }

        return PluginResult<WolfBinaryEvent>.Succeeded(new WolfBinaryEvent
        {
            EventId = (int)id,
            Title = title,
            MapX = (int)mapX,
            MapY = (int)mapY,
            Pages = pages,
        });
    }

    private static PluginResult<WolfBinaryEventPage> ReadPage(WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        // The five byte page signature 79 FF FF FF FF.
        for (var index = 0; index < 5; index++)
        {
            var b = pCursor.ReadByte();
            var expected = index == 0 ? 0x79 : 0xFF;
            if (b != expected)
            {
                return PluginResult<WolfBinaryEventPage>.Failed(PluginError.Create(
                    PluginErrorCode.InvalidGame,
                    $"WOLF event page signature byte {index} is 0x{b:X2}, expected 0x{expected:X2}.",
                    EnginePluginIds.WolfRpg, "wolf-page"), Array.Empty<PluginDiagnostic>());
            }
        }

        var icon = pCursor.ReadLengthPrefixedString(pLimits.MaxStringBytes);
        var rowByte = pCursor.ReadByte();
        var column = pCursor.ReadByte();
        var opacity = pCursor.ReadByte();
        var blend = pCursor.ReadByte();
        var trigger = pCursor.ReadByte();

        // Four conditions, nine bytes each: a flag b4 and two u4 values.
        for (var index = 0; index < 4; index++)
        {
            pCursor.Skip(1);
            pCursor.ReadUInt32();
            pCursor.ReadUInt32();
        }

        var moveSpeed = pCursor.ReadByte();
        var moveFrequency = pCursor.ReadByte();
        var moveRoute = pCursor.ReadByte();

        return PluginResult<WolfBinaryEventPage>.Succeeded(new WolfBinaryEventPage
        {
            Icon = icon,
            // The reference implementation derives the row as (byte >> 1) - 1.
            IconRow = (rowByte >> 1) - 1,
            IconColumn = column,
            IconOpacity = opacity,
            IconBlend = blend,
            EventTrigger = trigger,
            MoveSpeed = moveSpeed,
            MoveFrequency = moveFrequency,
            MoveRoute = moveRoute,
        });
    }

    private static PluginResult<T> Failed<T>(string pMessage, string pSource)
    {
        return PluginResult<T>.Failed(PluginError.Create(
            PluginErrorCode.InvalidGame, pMessage, EnginePluginIds.WolfRpg, "wolf-binary"),
            [PluginDiagnostic.Warning("wolf.binary-parse", pMessage, EnginePluginIds.WolfRpg)]);
    }
}

/// <summary>Raised when the buffer ends or a required field cannot be read.</summary>
public sealed class WolfFormatException : Exception
{
    public WolfFormatException(string pMessage) : base(pMessage) { }
}

/// <summary>
/// A bounded little endian cursor. Every read is checked, so a truncated or
/// hostile file produces a diagnostic instead of an out of range access.
/// </summary>
public sealed class WolfByteCursor
{
    private readonly byte[] _bytes;
    private readonly string _source;
    private int _position;

    public WolfByteCursor(byte[] pBytes, string pSource)
    {
        _bytes = pBytes;
        _source = pSource;
    }

    public int Position => _position;

    private void Require(int pCount)
    {
        if (_position + pCount > _bytes.Length)
        {
            throw new WolfFormatException(
                $"WOLF data file '{_source}' ends at byte {_position} but {_position + pCount} were needed.");
        }
    }

    public byte ReadByte()
    {
        Require(1);
        return _bytes[_position++];
    }

    /// <summary>
    /// Reads a big-endian u4. WolfRPG stores the event command signature this
    /// way even though every other field in the file is little endian, so it
    /// must not go through the shared little endian read.
    /// </summary>
    public uint ReadUInt32BigEndian()
    {
        Require(4);
        var value = (uint)((_bytes[_position] << 24)
            | (_bytes[_position + 1] << 16)
            | (_bytes[_position + 2] << 8)
            | _bytes[_position + 3]);
        _position += 4;
        return value;
    }

    public uint ReadUInt32()
    {
        Require(4);
        var value = (uint)(_bytes[_position]
            | (_bytes[_position + 1] << 8)
            | (_bytes[_position + 2] << 16)
            | (_bytes[_position + 3] << 24));
        _position += 4;
        return value;
    }

    public void Skip(int pCount)
    {
        if (pCount < 0)
        {
            throw new WolfFormatException("A negative skip was requested on a WOLF cursor.");
        }
        Require(pCount);
        _position += pCount;
    }

    /// <summary>
    /// Reads the format's length prefixed string: a u4 byte count, then that
    /// many bytes of Shift-JIS compatible text. The count is bounded before the
    /// buffer is touched so a hostile length cannot allocate.
    /// </summary>
    public string ReadLengthPrefixedString(int pMaxBytes)
    {
        var length = ReadUInt32();
        if (length > (uint)pMaxBytes)
        {
            throw new WolfFormatException(
                $"WOLF data file '{_source}' declares a string of {length} bytes, above the limit.");
        }
        Require((int)length);
        var text = Encoding.GetEncoding("Shift-JIS").GetString(_bytes, _position, (int)length);
        _position += (int)length;
        return text.TrimEnd('\0');
    }
}
