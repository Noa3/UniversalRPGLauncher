using System;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// RM2K tile-id blocks, passability flags and chip-id resolution.
/// Every constant and rule is verified against EasyRPG Player
/// <c>src/map_data.h</c> and the <c>Game_Map</c> passability helpers.
/// </summary>
public static class Rm2kChipset
{
    // Tile id blocks (map_data.h).
    public const int BlockA = 0;
    public const int BlockAEnd = BlockA + 2 * 1000;
    public const int BlockAStride = 1000;
    public const int BlockAIndex = 0;

    public const int BlockB = 2000;
    public const int BlockBEnd = BlockB + 1 * 1000;
    public const int BlockBStride = 1000;
    public const int BlockBIndex = 2;

    public const int BlockC = 3000;
    public const int BlockCEnd = BlockC + 3 * 50;
    public const int BlockCStride = 50;
    public const int BlockCIndex = 3;

    public const int BlockD = 4000;
    public const int BlockDEnd = BlockD + 12 * 50;
    public const int BlockDStride = 50;
    public const int BlockDIndex = 6;

    public const int BlockE = 5000;
    public const int BlockEEnd = BlockE + 144;
    public const int BlockEStride = 1;
    public const int BlockEIndex = 18;

    public const int BlockF = 10000;
    public const int BlockFEnd = BlockF + 144;
    public const int BlockFStride = 1;
    public const int BlockFIndex = 162;

    public const int NumLowerTiles = BlockFIndex;   // 162
    public const int NumUpperTiles = BlockFTiles;   // 144
    public const int BlockFTiles = 144;

    /// <summary>Lower passability table size (liblcf ChunkChipset::passable_data_lower).</summary>
    public const int PassabilityLowerEntries = 162;

    /// <summary>Upper passability table size (liblcf ChunkChipset::passable_data_upper).</summary>
    public const int PassabilityUpperEntries = 144;

    // Passability flags (map_data.h namespace Passable).
    public const byte PassDown = 0x01;
    public const byte PassLeft = 0x02;
    public const byte PassRight = 0x04;
    public const byte PassUp = 0x08;
    public const byte PassAbove = 0x10;
    public const byte PassWall = 0x20;
    public const byte PassCounter = 0x40;

    /// <summary>All four direction bits, as used by liblcf's default lower entry (15).</summary>
    public const byte AllDirections = PassDown | PassLeft | PassRight | PassUp;

    /// <summary>Verified map_data.h ChipIdToIndex.</summary>
    public static int ChipIdToIndex(int pChipId)
    {
        if (pChipId is >= BlockA and < BlockAEnd) return BlockAIndex + (pChipId - BlockA) / BlockAStride;
        if (pChipId is >= BlockB and < BlockBEnd) return BlockBIndex + (pChipId - BlockB) / BlockBStride;
        if (pChipId is >= BlockC and < BlockCEnd) return BlockCIndex + (pChipId - BlockC) / BlockCStride;
        if (pChipId is >= BlockD and < BlockDEnd) return BlockDIndex + (pChipId - BlockD) / BlockDStride;
        if (pChipId is >= BlockE and < BlockEEnd) return BlockEIndex + (pChipId - BlockE) / BlockEStride;
        if (pChipId is >= BlockF and < BlockFEnd) return BlockFIndex + (pChipId - BlockF) / BlockFStride;
        return 0;
    }

    /// <summary>Verified map_data.h IndexToChipId.</summary>
    public static int IndexToChipId(int pIndex)
    {
        if (pIndex is >= BlockAIndex and < BlockBIndex) return BlockA + (pIndex - BlockAIndex) * BlockAStride;
        if (pIndex is >= BlockBIndex and < BlockCIndex) return BlockB + (pIndex - BlockBIndex) * BlockBStride;
        if (pIndex is >= BlockCIndex and < BlockDIndex) return BlockC + (pIndex - BlockCIndex) * BlockCStride;
        if (pIndex is >= BlockDIndex and < BlockEIndex) return BlockD + (pIndex - BlockDIndex) * BlockDStride;
        if (pIndex is >= BlockEIndex and < BlockFIndex) return BlockE + (pIndex - BlockEIndex) * BlockEStride;
        if (pIndex >= BlockFIndex) return BlockF + (pIndex - BlockFIndex) * BlockFStride;
        return 0;
    }

    /// <summary>
    /// Verified GetPassableMask: a single cardinal step contributes exactly one
    /// direction bit. Returns 0 for anything that is not one cardinal step.
    /// </summary>
    public static byte DirectionBit(int pDeltaX, int pDeltaY)
    {
        if (pDeltaX == 0 && pDeltaY == 0) return 0;
        if (pDeltaX > 0 && pDeltaY == 0) return PassRight;
        if (pDeltaX < 0 && pDeltaY == 0) return PassLeft;
        if (pDeltaX == 0 && pDeltaY > 0) return PassDown;
        if (pDeltaX == 0 && pDeltaY < 0) return PassUp;
        return 0;
    }

    /// <summary>
    /// Verified Game_Map::IsPassableLowerTile, including the wall/autotile
    /// exception that lets a walking-through wall tile be entered.
    /// </summary>
    public static bool IsPassableLowerTile(int pLowerRawId, byte[] pLowerPassability, byte pBit)
    {
        if (pLowerPassability == null || pLowerPassability.Length == 0)
        {
            return false;
        }
        var tileId = 0;
        if (pLowerRawId >= BlockE)
        {
            tileId = pLowerRawId - BlockE;
        }
        else if (pLowerRawId >= BlockD)
        {
            tileId = (pLowerRawId - BlockD) / BlockDStride + BlockDIndex;
            var autotileId = (pLowerRawId - BlockD) % BlockDStride;
            if (autotileId is >= 20 and <= 23 or >= 33 and <= 37 or 42 or 43 or 45 or 46)
            {
                if (tileId < pLowerPassability.Length && (pLowerPassability[tileId] & PassWall) != 0)
                {
                    return true;
                }
            }
        }
        else if (pLowerRawId >= BlockC)
        {
            tileId = (pLowerRawId - BlockC) / BlockCStride + BlockCIndex;
        }
        else if (pLowerRawId < BlockC)
        {
            tileId = pLowerRawId / BlockBStride;
        }

        return tileId < pLowerPassability.Length && (pLowerPassability[tileId] & pBit) != 0;
    }

    /// <summary>
    /// Verified Game_Map::IsPassableTile for a plain walking character: the
    /// upper layer decides, and a tile that is not "above" the character does
    /// not fall through to the lower layer.
    /// </summary>
    public static bool IsPassableTile(
        int pLowerRawId,
        int pUpperRawId,
        byte[] pLowerPassability,
        byte[] pUpperPassability,
        byte pBit)
    {
        if (pUpperPassability == null || pLowerPassability == null)
        {
            return false;
        }
        var upperIndex = pUpperRawId - BlockF;
        if (upperIndex < 0 || upperIndex >= pUpperPassability.Length)
        {
            return false;
        }
        var upperFlags = pUpperPassability[upperIndex];
        if ((upperFlags & pBit) == 0)
        {
            return false;
        }
        if ((upperFlags & PassAbove) == 0)
        {
            return true;
        }
        return IsPassableLowerTile(pLowerRawId, pLowerPassability, pBit);
    }

    /// <summary>
    /// Builds the per-tile direction mask used by the simulation. Unknown tile
    /// ids resolve to "not passable" so malformed data fails closed.
    /// </summary>
    public static byte[] BuildDirectionMasks(
        int[] pLowerLayer,
        int[] pUpperLayer,
        byte[] pLowerPassability,
        byte[] pUpperPassability)
    {
        if (pLowerLayer == null || pUpperLayer == null)
        {
            return [];
        }
        var tileCount = Math.Min(pLowerLayer.Length, pUpperLayer.Length);
        var masks = new byte[tileCount];
        for (var index = 0; index < tileCount; index++)
        {
            var mask = 0;
            foreach (var bit in new[] { PassDown, PassLeft, PassRight, PassUp })
            {
                if (IsPassableTile(pLowerLayer[index], pUpperLayer[index], pLowerPassability, pUpperPassability, bit))
                {
                    mask |= bit;
                }
            }
            masks[index] = (byte)mask;
        }
        return masks;
    }
}
