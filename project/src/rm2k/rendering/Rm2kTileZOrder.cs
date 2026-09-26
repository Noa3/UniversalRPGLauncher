using System;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// The RM2K tile draw order, verified in EasyRPG Player
/// <c>src/tilemap_layer.cpp</c> and <c>src/drawable.h</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>CreateTileCacheAt</c> assigns each tile a sublayer. An upper layer tile
/// with the <c>Above</c> flag goes into the upper sublayer, otherwise into the
/// lower sublayer. A lower layer tile goes into the upper sublayer when its
/// resolved chip index carries <c>Wall</c> or <c>Above</c>, otherwise into the
/// lower sublayer.
/// </para>
/// <para>
/// The two sublayers are drawn as two drawables:
/// <c>lower_layer(this, Priority_TilesetBelow + TileBelow + layer)</c> and
/// <c>upper_layer(this, Priority_TilesetAbove + TileAbove + layer)</c>, with
/// <c>TileBelow = 0</c>, <c>TileAbove = 100</c>,
/// <c>Priority_TilesetBelow = 20</c>, <c>Priority_TilesetAbove = 50</c> and
/// <c>Priority_Player = 40</c>. Because drawables are sorted ascending, the
/// effective order is: lower layer tiles, then the hero, then upper layer tiles.
/// That is why a wall tile covers the hero.
/// </para>
/// </remarks>
public static class Rm2kTileZOrder
{
    /// <summary>Chipset sublayer for tiles drawn below everything else.</summary>
    public const int SubLayerBelow = 0;

    /// <summary>Chipset sublayer for tiles drawn above the hero.</summary>
    public const int SubLayerAbove = 1;

    /// <summary>Lower layer tiles never carry the above sublayer flag on their own.</summary>
    public const int LowerLayerIndex = 0;

    /// <summary>Upper layer tile ids start here.</summary>
    public const int UpperLayerThreshold = 10000;

    /// <summary>
    /// Verified chip index resolution used by <c>CreateTileCacheAt</c>: block E
    /// goes through the lower substitution table plus <c>BLOCK_E_INDEX</c>,
    /// block D divides by the block stride, block C likewise, and everything
    /// below block C is the block number itself.
    /// </summary>
    public static int ResolveChipIndex(int pChipId, Rm2kTileSubstitution? pSubstitution)
    {
        if (pChipId >= Rm2kChipset.BlockE)
        {
            return pSubstitution == null
                ? pChipId - Rm2kChipset.BlockE + Rm2kChipset.BlockEIndex
                : pSubstitution.ResolveChipIndex(pChipId);
        }
        if (pChipId >= Rm2kChipset.BlockD)
        {
            return (pChipId - Rm2kChipset.BlockD) / Rm2kChipset.BlockDStride + Rm2kChipset.BlockDIndex;
        }
        if (pChipId >= Rm2kChipset.BlockC)
        {
            return (pChipId - Rm2kChipset.BlockC) / Rm2kChipset.BlockCStride + Rm2kChipset.BlockCIndex;
        }
        return pChipId / Rm2kChipset.BlockBStride;
    }

    /// <summary>
    /// Sublayer of a lower layer tile, following
    /// <c>TileData.z = tile.ID &gt;= BLOCK_F ? ... : (passable[chip_index] &amp; (Wall | Above)) ? TileAbove : TileBelow</c>.
    /// Without passability data the Player keeps the tile below.
    /// </summary>
    public static int LowerLayerSubLayer(int pChipId, byte[]? pLowerPassability, Rm2kTileSubstitution? pSubstitution)
    {
        if (pLowerPassability == null || pLowerPassability.Length == 0)
        {
            return SubLayerBelow;
        }
        var chipIndex = ResolveChipIndex(pChipId, pSubstitution);
        if (chipIndex < 0 || chipIndex >= pLowerPassability.Length)
        {
            return SubLayerBelow;
        }
        var wallOrAbove = (byte)(Rm2kChipset.PassWall | Rm2kChipset.PassAbove);
        return (pLowerPassability[chipIndex] & wallOrAbove) != 0 ? SubLayerAbove : SubLayerBelow;
    }

    /// <summary>
    /// Sublayer of an upper layer tile, following
    /// <c>passable[substitutions[tile.ID - BLOCK_F]] &amp; Above</c>. A tile
    /// below <c>BLOCK_F</c> has no upper entry and stays below.
    /// </summary>
    public static int UpperLayerSubLayer(int pChipId, byte[]? pUpperPassability, Rm2kTileSubstitution? pSubstitution)
    {
        if (pUpperPassability == null || pUpperPassability.Length == 0)
        {
            return SubLayerBelow;
        }
        var index = pChipId - Rm2kChipset.BlockF;
        if (index < 0)
        {
            return SubLayerBelow;
        }
        if (pSubstitution != null)
        {
            var substituted = pSubstitution.SubstituteUpper(index);
            if (substituted < 0)
            {
                return SubLayerBelow;
            }
            index = substituted;
        }
        if (index >= pUpperPassability.Length)
        {
            return SubLayerBelow;
        }
        return (pUpperPassability[index] & Rm2kChipset.PassAbove) != 0 ? SubLayerAbove : SubLayerBelow;
    }
}
