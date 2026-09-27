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

    /// <summary>Terrain table size (liblcf ChunkChipset::terrain_data, 162 shorts).</summary>
    public const int TerrainDataEntries = 162;

    /// <summary>
    /// Terrain tag used when a chipset carries no terrain table. RPG_RT omits an
    /// all-ones table and liblcf defaults every entry to 1, so an absent table
    /// and a chip index the table does not cover both mean terrain 1.
    /// </summary>
    public const int DefaultTerrainTag = 1;

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
    /// exception that lets a walking-through wall tile be entered. Block E tile
    /// ids run through the lower substitution table
    /// (<c>map_info.lower_tiles[tile_raw_id - BLOCK_E] + BLOCK_E_INDEX</c>).
    /// </summary>
    public static bool IsPassableLowerTile(int pLowerRawId, byte[] pLowerPassability, byte pBit)
    {
        return IsPassableLowerTile(pLowerRawId, pLowerPassability, pBit, null);
    }

    public static bool IsPassableLowerTile(
        int pLowerRawId, byte[] pLowerPassability, byte pBit, Rm2kTileSubstitution? pSubstitution)
    {
        if (pLowerPassability == null || pLowerPassability.Length == 0)
        {
            return false;
        }
        var tileId = 0;
        if (pLowerRawId >= BlockE)
        {
            tileId = pLowerRawId - BlockE;
            if (pSubstitution != null)
            {
                var substituted = pSubstitution.SubstituteLower(tileId);
                if (substituted < 0)
                {
                    return false;
                }
                tileId = substituted;
            }
            else
            {
                // Verified Game_Map::IsPassableLowerTile applies
                //   tile_id = map_info.lower_tiles[tile_id] + BLOCK_E_INDEX
                // and an absent table is the identity, so the offset is still
                // added. Skipping it here read entry 0 instead of entry 18 and
                // made every block E tile inherit the first autotile's
                // passability.
                tileId += BlockEIndex;
            }
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
        return IsPassableTile(pLowerRawId, pUpperRawId, pLowerPassability, pUpperPassability, pBit, null);
    }

    /// <summary>
    /// Verified Game_Map::IsPassableTile with tile substitution: the upper raw
    /// id is reduced by <c>BLOCK_F</c> and then replaced through
    /// <c>map_info.upper_tiles</c> before the flag lookup.
    /// </summary>
    public static bool IsPassableTile(
        int pLowerRawId,
        int pUpperRawId,
        byte[] pLowerPassability,
        byte[] pUpperPassability,
        byte pBit,
        Rm2kTileSubstitution? pSubstitution)
    {
        if (pUpperPassability == null || pLowerPassability == null)
        {
            return false;
        }
        var upperIndex = pUpperRawId - BlockF;
        if (upperIndex < 0)
        {
            return false;
        }
        if (pSubstitution != null)
        {
            var substituted = pSubstitution.SubstituteUpper(upperIndex);
            if (substituted < 0)
            {
                return false;
            }
            upperIndex = substituted;
        }
        if (upperIndex >= pUpperPassability.Length)
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
        return IsPassableLowerTile(pLowerRawId, pLowerPassability, pBit, pSubstitution);
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
        return BuildDirectionMasks(pLowerLayer, pUpperLayer, pLowerPassability, pUpperPassability, null);
    }

    public static byte[] BuildDirectionMasks(
        int[] pLowerLayer,
        int[] pUpperLayer,
        byte[] pLowerPassability,
        byte[] pUpperPassability,
        Rm2kTileSubstitution? pSubstitution)
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
                if (IsPassableTile(pLowerLayer[index], pUpperLayer[index], pLowerPassability, pUpperPassability, bit, pSubstitution))
                {
                    mask |= bit;
                }
            }
            masks[index] = (byte)mask;
        }
        return masks;
    }

    /// <summary>
    /// Verified Game_Map::IsCounter: the upper layer must hold a tile above
    /// <c>BLOCK_F</c>, its id runs through the upper substitution table, and the
    /// resulting entry must carry the <see cref="PassCounter"/> flag.
    /// </summary>
    public static bool IsCounterTile(
        int pUpperRawId, byte[]? pUpperPassability, Rm2kTileSubstitution? pSubstitution)
    {
        if (pUpperPassability == null || pUpperPassability.Length == 0)
        {
            return false;
        }
        var upperIndex = pUpperRawId - BlockF;
        if (upperIndex < 0)
        {
            return false;
        }
        if (pSubstitution != null)
        {
            var substituted = pSubstitution.SubstituteUpper(upperIndex);
            if (substituted < 0)
            {
                return false;
            }
            upperIndex = substituted;
        }
        if (upperIndex >= pUpperPassability.Length)
        {
            return false;
        }
        return (pUpperPassability[upperIndex] & PassCounter) != 0;
    }

    // ---- Autotile animation -------------------------------------------------
    // Verified against EasyRPG Player `src/tilemap_layer.cpp` (Draw),
    // `src/game_map.cpp` (SetChipset/GetAnimation*) and liblcf `rpg::Chipset`.

    /// <summary>liblcf <c>rpg::Chipset::AnimType</c>.</summary>
    public const int AnimTypeReciprocating = 0;

    /// <summary>liblcf <c>rpg::Chipset::AnimType</c>: three even frames.</summary>
    public const int AnimTypeCyclic = 1;

    /// <summary>Frame step per game frame for the static C block (water).</summary>
    public const int CBlockFramesPerStep = 6;

    /// <summary>Frame count of the fixed C block cycle.</summary>
    public const int CBlockFrameCount = 4;

    /// <summary>
    /// <c>Game_Map::GetAnimationSpeed()</c>: the chipset only stores a boolean
    /// "animated" flag, and the Player maps it to 12 or 24 frames per step.
    /// </summary>
    public static int AnimationSpeed(int pChipsetAnimationSpeed)
    {
        return pChipsetAnimationSpeed != 0 ? 12 : 24;
    }

    /// <summary>
    /// <c>GetCachedAutotileAB</c> step for a reciprocating chipset
    /// (<c>animation_type == 0</c>): the Player divides by 4 and replaces the
    /// fourth step with the second, which yields 0, 1, 2, 1, 0, 1, 2, 1, ...
    /// </summary>
    public static int ReciprocatingStep(int pFrameCount, int pAnimationSpeed)
    {
        var step = StepOf(pFrameCount, pAnimationSpeed) % 4;
        return step == 3 ? 1 : step;
    }

    /// <summary>
    /// <c>GetCachedAutotileAB</c> step for a cyclic chipset
    /// (<c>animation_type != 0</c>): three even frames.
    /// </summary>
    public static int CyclicStep(int pFrameCount, int pAnimationSpeed)
    {
        return StepOf(pFrameCount, pAnimationSpeed) % 3;
    }

    /// <summary>
    /// Block C animates on a fixed 6-frames-per-step cycle that ignores both
    /// the chipset animation type and the chipset animation speed.
    /// </summary>
    public static int CBlockStep(int pFrameCount)
    {
        return pFrameCount / CBlockFramesPerStep % CBlockFrameCount;
    }

    /// <summary>
    /// Animation step for a map tile id, following the Player's dispatch order:
    /// blocks A/B use the chipset animation, block C uses the fixed cycle, and
    /// blocks D-F never animate.
    /// </summary>
    public static int ChipAnimationStep(
        int pChipId, int pFrameCount, int pChipsetAnimationType, int pChipsetAnimationSpeed)
    {
        if (pChipId >= BlockC && pChipId < BlockD)
        {
            return CBlockStep(pFrameCount);
        }
        if (pChipId < BlockC)
        {
            var speed = AnimationSpeed(pChipsetAnimationSpeed);
            return pChipsetAnimationType != 0
                ? CyclicStep(pFrameCount, speed)
                : ReciprocatingStep(pFrameCount, speed);
        }
        return 0;
    }

    private static int StepOf(int pFrameCount, int pAnimationSpeed)
    {
        if (pFrameCount < 0)
        {
            return 0;
        }
        return pFrameCount / Math.Max(1, pAnimationSpeed);
    }
}

/// <summary>
/// RM2K tile substitution tables, verified against EasyRPG Player
/// <c>Game_Map::Setup</c> (<c>std::iota</c> identity), <c>IsPassableTile</c>,
/// <c>IsPassableLowerTile</c> and <c>GetChipId</c>, plus liblcf
/// <c>rpg::SaveMapInfo</c>, which stores both tables with 144 identity entries.
/// They live in the save file, not in the LMT map info, so the identity tables
/// are the verified default for a freshly loaded map.
/// </summary>
public sealed class Rm2kTileSubstitution
{
    private readonly int[] _lower;
    private readonly int[] _upper;

    public Rm2kTileSubstitution(int[]? pLower, int[]? pUpper)
    {
        _lower = Validate(pLower, Rm2kChipset.NumUpperTiles);
        _upper = Validate(pUpper, Rm2kChipset.NumUpperTiles);
    }

    private static int[] Validate(int[]? pTable, int pExpectedLength)
    {
        if (pTable == null)
        {
            return Identity(pExpectedLength);
        }
        var result = new int[pTable.Length];
        for (var index = 0; index < pTable.Length; index++)
        {
            if (pTable[index] < 0 || pTable[index] >= pExpectedLength)
            {
                // An out-of-range entry is refused instead of clamped so a
                // malformed save cannot silently remap tiles.
                return Identity(pExpectedLength);
            }
            result[index] = pTable[index];
        }
        return result;
    }

    private static int[] Identity(int pLength)
    {
        var result = new int[pLength];
        for (var index = 0; index < pLength; index++)
        {
            result[index] = index;
        }
        return result;
    }

    /// <summary>
    /// <c>map_info.lower_tiles[tile_raw_id - BLOCK_E] + BLOCK_E_INDEX</c>, or
    /// -1 when the request cannot be resolved.
    /// </summary>
    public int SubstituteLower(int pBlockEIndex)
    {
        if (pBlockEIndex < 0 || pBlockEIndex >= _lower.Length)
        {
            return -1;
        }
        return _lower[pBlockEIndex] + Rm2kChipset.BlockEIndex;
    }

    /// <summary>
    /// <c>map_info.upper_tiles[tile_raw_id - BLOCK_F]</c>, or -1 when the
    /// request cannot be resolved.
    /// </summary>
    public int SubstituteUpper(int pUpperIndex)
    {
        if (pUpperIndex < 0 || pUpperIndex >= _upper.Length)
        {
            return -1;
        }
        return _upper[pUpperIndex];
    }

    /// <summary>
    /// Verified Game_Map::GetChipId: the raw id is converted to a chip index
    /// first, and the lower table then replaces indices inside
    /// <c>[BLOCK_E_INDEX, NUM_LOWER_TILES)</c>.
    /// </summary>
    public int ResolveChipIndex(int pChipId)
    {
        var chipIndex = Rm2kChipset.ChipIdToIndex(pChipId);
        if (chipIndex >= Rm2kChipset.BlockEIndex && chipIndex < Rm2kChipset.NumLowerTiles)
        {
            var substituted = SubstituteLower(chipIndex - Rm2kChipset.BlockEIndex);
            return substituted < 0 ? chipIndex : substituted;
        }
        return chipIndex;
    }

    /// <summary>
    /// Verified Game_Map::GetTerrainTag: the lower tile decides, its chip index
    /// is resolved through the substitution, and the chipset terrain table maps
    /// that index to a terrain tag. An absent table means every tile is terrain
    /// 1 (the RPG_RT optimisation that drops an all-ones table), and an index the
    /// table does not cover falls back to that same default.
    /// </summary>
    public static int GetTerrainTag(int pLowerRawId, int[]? pTerrainData, Rm2kTileSubstitution? pSubstitution)
    {
        if (pTerrainData == null || pTerrainData.Length == 0)
        {
            return Rm2kChipset.DefaultTerrainTag;
        }
        var chipIndex = pSubstitution == null
            ? Rm2kChipset.ChipIdToIndex(pLowerRawId)
            : pSubstitution.ResolveChipIndex(pLowerRawId);
        if (chipIndex < 0 || chipIndex >= pTerrainData.Length)
        {
            return Rm2kChipset.DefaultTerrainTag;
        }
        return pTerrainData[chipIndex];
    }
}
