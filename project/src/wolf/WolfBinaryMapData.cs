using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// The verified binary WolfRPG MapData layout. Every constant and field order
/// here comes from the published .mps format description; nothing is guessed.
///
/// Reference facts used by <see cref="WolfBinaryMapReader"/>:
/// <list type="bullet">
///   <item>Header: ten zero bytes, <c>WOLFM</c>, a zero byte, a version header
///   byte (0x00 for v2, 0x55 for v3), three zero bytes, 0x64, then a version
///   byte that must be 0x65 (v2) or 0x66 (v3).</item>
///   <item>Then a length prefixed title, tileset id, width, height and the
///   event count, all little endian u4.</item>
///   <item>The map body is a first pixel u4. A value of 0xFFFFFFFF means the
///   map does not exist. Otherwise the body is width * height * 12 bytes read
///   as width * height mappixels of three u4 values each.</item>
///   <item>An event starts with 0x6F, a u4 that must be 0x3039, its id, a
///   length prefixed title, map x, map y, the page count, a zero u4, the pages
///   and a 0x70 footer.</item>
///   <item>A page starts with the five byte signature 79 FF FF FF FF and ends
///   with 0x7A.</item>
/// </list>
/// </summary>
public sealed class WolfBinaryMapData
{
    public int MapId { get; init; }
    public string Title { get; init; } = "";
    public int TilesetId { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool MapExists { get; init; } = true;
    public int FormatVersionByte { get; init; }
    public IReadOnlyList<WolfBinaryMapPixel> Pixels { get; init; } = Array.Empty<WolfBinaryMapPixel>();
    public IReadOnlyList<WolfBinaryEvent> Events { get; init; } = Array.Empty<WolfBinaryEvent>();
}

/// <summary>
/// One map cell, decoded from its three little endian u4 values exactly as the
/// format description specifies: the digits of the first value carry the
/// autotile id and its four corner modes, and the second and third values
/// carry the base tile.
/// </summary>
public sealed class WolfBinaryMapPixel
{
    public int Raw { get; init; }
    public int AutotileId { get; init; }
    public bool HasAutotile { get; init; }
    public int AutotileTopLeft { get; init; }
    public int AutotileTopRight { get; init; }
    public int AutotileBottomLeft { get; init; }
    public int AutotileBottomRight { get; init; }
    public int BaseTileLower { get; init; }
    public int BaseTileUpper { get; init; }
}

public sealed class WolfBinaryEvent
{
    public int EventId { get; init; }
    public string Title { get; init; } = "";
    public int MapX { get; init; }
    public int MapY { get; init; }
    public IReadOnlyList<WolfBinaryEventPage> Pages { get; init; } = Array.Empty<WolfBinaryEventPage>();
}

/// <summary>
/// The part of an event page this card decodes. The page body after the
/// signature carries the graphic, the trigger and the move settings; a command
/// list is a separate card because its encoding is the largest part of the
/// format and a wrong command count would desynchronise every following event.
/// </summary>
public sealed class WolfBinaryEventPage
{
    public string Icon { get; init; } = "";
    public int IconRow { get; init; }
    public int IconColumn { get; init; }
    public int IconOpacity { get; init; }
    public int IconBlend { get; init; }
    public int EventTrigger { get; init; }
    public int MoveSpeed { get; init; }
    public int MoveFrequency { get; init; }
    public int MoveRoute { get; init; }
}
