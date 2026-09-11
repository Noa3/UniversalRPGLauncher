using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Parser;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// Directional map-geometry passability derived from RPG Maker 2000/2003
/// chipset passage flags. This models chipset/map geometry only; event
/// collision, vehicles, looping maps and runtime tile substitution remain
/// separate runtime layers.
///
/// Field IDs, defaults and tile/passability constants are verified against
/// liblcf and EasyRPG Player. Runtime passability consumes only canonical typed
/// chipset vectors. Legacy parser output is normalized on a local chipset copy,
/// so constructing runtime geometry never mutates the parsed database.
/// </summary>
public sealed class Rm2kPassabilityMap
{
    public const byte Down = 0x01;
    public const byte Left = 0x02;
    public const byte Right = 0x04;
    public const byte Up = 0x08;
    public const byte Above = 0x10;
    public const byte Wall = 0x20;
    public const byte Counter = 0x40;

    public const int LowerPassageCount = Rm2kChipsetDataNormalizer.LowerPassageCount;
    public const int UpperPassageCount = Rm2kChipsetDataNormalizer.UpperPassageCount;

    private const int MaxMapDimension = 500;
    private const int MaxMapTiles = 250_000;
    private const int BlockC = 3000;
    private const int BlockCStride = 50;
    private const int BlockCIndex = 3;
    private const int BlockD = 4000;
    private const int BlockDStride = 50;
    private const int BlockDIndex = 6;
    private const int BlockE = 5000;
    private const int BlockEIndex = 18;
    private const int BlockF = 10000;

    private const string LowerPassageField = "passable_data_lower";
    private const string UpperPassageField = "passable_data_upper";

    private readonly byte[] _directionMasks;
    private readonly bool[] _counterTiles;

    private Rm2kPassabilityMap(
        int pWidth,
        int pHeight,
        byte[] pDirectionMasks,
        bool[] pCounterTiles)
    {
        Width = pWidth;
        Height = pHeight;
        _directionMasks = pDirectionMasks;
        _counterTiles = pCounterTiles;
    }

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<byte> DirectionMasks => _directionMasks;

    public bool AllowsDirection(int pX, int pY, byte pDirection)
    {
        if (!IsCardinalDirection(pDirection) || !IsInside(pX, pY))
        {
            return false;
        }
        return (_directionMasks[pY * Width + pX] & pDirection) != 0;
    }

    /// <summary>
    /// Returns whether the upper-layer tile at this coordinate carries the
    /// RPG_RT counter flag. Action-key interaction may traverse up to three
    /// consecutive counter tiles before looking for an event beyond them.
    /// </summary>
    public bool IsCounter(int pX, int pY)
    {
        return IsInside(pX, pY) && _counterTiles[pY * Width + pX];
    }

    public bool CanMove(int pFromX, int pFromY, int pToX, int pToY)
    {
        var deltaX = pToX - pFromX;
        var deltaY = pToY - pFromY;
        if (Math.Abs(deltaX) + Math.Abs(deltaY) != 1
            || !IsInside(pFromX, pFromY)
            || !IsInside(pToX, pToY))
        {
            return false;
        }

        var outgoing = DirectionForDelta(deltaX, deltaY);
        var incoming = Opposite(outgoing);
        return AllowsDirection(pFromX, pFromY, outgoing)
            && AllowsDirection(pToX, pToY, incoming);
    }

    public static bool TryCreate(
        Godot.Collections.Dictionary pDatabase,
        Godot.Collections.Dictionary pMap,
        out Rm2kPassabilityMap? pPassability,
        out string pError)
    {
        pPassability = null;
        pError = "";

        if (!TryReadInt(pMap, "width", out var width)
            || !TryReadInt(pMap, "height", out var height)
            || !TryReadInt(pMap, "chipset_id", out var chipsetId)
            || width <= 0 || width > MaxMapDimension
            || height <= 0 || height > MaxMapDimension
            || chipsetId <= 0)
        {
            pError = "Map dimensions or chipset ID are missing/invalid.";
            return false;
        }

        var tileCountLong = (long)width * height;
        if (tileCountLong <= 0 || tileCountLong > MaxMapTiles)
        {
            pError = "Map tile count is outside the bounded passability limit.";
            return false;
        }
        var expectedTiles = (int)tileCountLong;

        int[] lowerLayer;
        int[] upperLayer;
        try
        {
            lowerLayer = (int[])pMap["lower_layer"];
            upperLayer = (int[])pMap["upper_layer"];
        }
        catch (Exception)
        {
            pError = "Map tile layers are missing or not packed Int32 arrays.";
            return false;
        }

        if (lowerLayer.Length != expectedTiles || upperLayer.Length != expectedTiles)
        {
            pError = "Map tile layers do not match the declared dimensions.";
            return false;
        }

        if (!TryFindChipset(pDatabase, chipsetId, out var parsedChipset))
        {
            pError = $"Chipset {chipsetId} is not present in the parsed database.";
            return false;
        }

        var chipset = ShallowCopy(parsedChipset);
        if (!Rm2kChipsetDataNormalizer.TryNormalizeChipset(chipset, out var normalizeError))
        {
            pError = $"Chipset vector normalization failed: {normalizeError}";
            return false;
        }

        if (!TryReadPassages(chipset, LowerPassageField, LowerPassageCount,
                out var lowerPassages, out pError)
            || !TryReadPassages(chipset, UpperPassageField, UpperPassageCount,
                out var upperPassages, out pError))
        {
            return false;
        }

        var masks = new byte[expectedTiles];
        var counters = new bool[expectedTiles];
        for (var index = 0; index < expectedTiles; index += 1)
        {
            byte mask = 0;
            foreach (var direction in new[] { Down, Left, Right, Up })
            {
                if (IsPassableTile(lowerLayer[index], upperLayer[index], direction, lowerPassages, upperPassages))
                {
                    mask |= direction;
                }
            }
            masks[index] = mask;
            counters[index] = IsCounterTile(upperLayer[index], upperPassages);
        }

        pPassability = new Rm2kPassabilityMap(width, height, masks, counters);
        return true;
    }

    private static Godot.Collections.Dictionary ShallowCopy(Godot.Collections.Dictionary pSource)
    {
        var copy = new Godot.Collections.Dictionary();
        foreach (var key in pSource.Keys)
        {
            copy[key] = pSource[key];
        }
        return copy;
    }

    private static bool TryFindChipset(
        Godot.Collections.Dictionary pDatabase,
        int pChipsetId,
        out Godot.Collections.Dictionary pChipset)
    {
        pChipset = new Godot.Collections.Dictionary();
        if (!pDatabase.TryGetValue("chipsets", out var rawChipsets)
            || rawChipsets.VariantType != Variant.Type.Array)
        {
            return false;
        }

        foreach (var rawEntry in rawChipsets.AsGodotArray())
        {
            if (rawEntry.VariantType != Variant.Type.Dictionary)
            {
                continue;
            }
            var entry = rawEntry.AsGodotDictionary();
            if (TryReadInt(entry, "id", out var id) && id == pChipsetId)
            {
                pChipset = entry;
                return true;
            }
        }
        return false;
    }

    private static bool TryReadPassages(
        Godot.Collections.Dictionary pChipset,
        string pTypedField,
        int pExpectedLength,
        out byte[] pPassages,
        out string pError)
    {
        pError = "";
        pPassages = Array.Empty<byte>();
        if (!TryReadByteVector(pChipset, pTypedField, out var data))
        {
            pError = $"Normalized chipset field '{pTypedField}' is missing or invalid.";
            return false;
        }
        if (data.Length != pExpectedLength)
        {
            pError = $"Normalized chipset field '{pTypedField}' has {data.Length} bytes, expected exactly {pExpectedLength}.";
            return false;
        }
        pPassages = data;
        return true;
    }

    private static bool TryReadByteVector(
        Godot.Collections.Dictionary pData,
        string pKey,
        out byte[] pBytes)
    {
        pBytes = Array.Empty<byte>();
        if (!pData.TryGetValue(pKey, out var rawValue))
        {
            return false;
        }
        if (rawValue.VariantType == Variant.Type.PackedByteArray)
        {
            pBytes = rawValue.AsByteArray();
            return true;
        }
        if (rawValue.VariantType != Variant.Type.Array)
        {
            return false;
        }

        var source = rawValue.AsGodotArray();
        var result = new byte[source.Count];
        for (var index = 0; index < source.Count; index += 1)
        {
            if (source[index].VariantType != Variant.Type.Int)
            {
                return false;
            }
            var value = source[index].AsInt32();
            if (value < byte.MinValue || value > byte.MaxValue)
            {
                return false;
            }
            result[index] = (byte)value;
        }
        pBytes = result;
        return true;
    }

    private static bool IsPassableTile(
        int pLowerRawId,
        int pUpperRawId,
        byte pDirection,
        byte[] pLowerPassages,
        byte[] pUpperPassages)
    {
        var upperIndex = pUpperRawId - BlockF;
        if (upperIndex < 0 || upperIndex >= pUpperPassages.Length)
        {
            return false;
        }

        var upperFlags = pUpperPassages[upperIndex];
        if ((upperFlags & pDirection) == 0)
        {
            return false;
        }
        if ((upperFlags & Above) == 0)
        {
            return true;
        }

        return IsPassableLowerTile(pLowerRawId, pDirection, pLowerPassages);
    }

    private static bool IsCounterTile(int pUpperRawId, byte[] pUpperPassages)
    {
        var upperIndex = pUpperRawId - BlockF;
        return upperIndex >= 0
            && upperIndex < pUpperPassages.Length
            && (pUpperPassages[upperIndex] & Counter) != 0;
    }

    private static bool IsPassableLowerTile(int pRawId, byte pDirection, byte[] pPassages)
    {
        var index = LowerTileIndex(pRawId);
        if (index < 0 || index >= pPassages.Length)
        {
            return false;
        }

        if (pRawId >= BlockD && pRawId < BlockE)
        {
            var autotileId = (pRawId - BlockD) % BlockDStride;
            if ((pPassages[index] & Wall) != 0 && IsWallAutotilePassageException(autotileId))
            {
                return true;
            }
        }

        return (pPassages[index] & pDirection) != 0;
    }

    private static int LowerTileIndex(int pRawId)
    {
        if (pRawId >= BlockE && pRawId < BlockE + 144)
        {
            return pRawId - BlockE + BlockEIndex;
        }
        if (pRawId >= BlockD && pRawId < BlockD + 12 * BlockDStride)
        {
            return (pRawId - BlockD) / BlockDStride + BlockDIndex;
        }
        if (pRawId >= BlockC && pRawId < BlockC + 3 * BlockCStride)
        {
            return (pRawId - BlockC) / BlockCStride + BlockCIndex;
        }
        if (pRawId >= 0 && pRawId < BlockC)
        {
            return pRawId / 1000;
        }
        return -1;
    }

    private static bool IsWallAutotilePassageException(int pAutotileId)
    {
        return (pAutotileId >= 20 && pAutotileId <= 23)
            || (pAutotileId >= 33 && pAutotileId <= 37)
            || pAutotileId is 42 or 43 or 45 or 46;
    }

    private bool IsInside(int pX, int pY)
    {
        return pX >= 0 && pX < Width && pY >= 0 && pY < Height;
    }

    private static bool IsCardinalDirection(byte pDirection)
    {
        return pDirection is Down or Left or Right or Up;
    }

    private static byte DirectionForDelta(int pDeltaX, int pDeltaY)
    {
        if (pDeltaX > 0) return Right;
        if (pDeltaX < 0) return Left;
        if (pDeltaY > 0) return Down;
        return Up;
    }

    private static byte Opposite(byte pDirection)
    {
        return pDirection switch
        {
            Down => Up,
            Up => Down,
            Left => Right,
            Right => Left,
            _ => 0,
        };
    }

    private static bool TryReadInt(Godot.Collections.Dictionary pData, string pKey, out int pValue)
    {
        pValue = 0;
        if (!pData.TryGetValue("" + pKey, out var rawValue))
        {
            return false;
        }
        try
        {
            pValue = rawValue.AsInt32();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
