using System;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Draws a parsed RM2K map into a pixel buffer using the verified chipset
/// resolution of K-095 to K-098 and the verified draw order of K-099.
/// </summary>
/// <remarks>
/// The Player draws the lower layer first, then the hero, then the upper layer,
/// because the two chipset sublayers are drawables with the z values
/// <c>Priority_TilesetBelow + TileBelow + layer</c> and
/// <c>Priority_TilesetAbove + TileAbove + layer</c>. Within a layer, a tile in
/// the above sublayer is drawn after a tile in the below sublayer, because the
/// sublayer value is added to the z.
/// </remarks>
public sealed class Rm2kMapFrameRenderer
{
    private readonly Rm2kChipsetBitmap _chipset;

    public Rm2kMapFrameRenderer(Rm2kChipsetBitmap pChipset)
    {
        _chipset = pChipset ?? throw new ArgumentNullException(nameof(pChipset));
    }

    /// <summary>
    /// Renders the whole map. The lower layer is drawn completely, both
    /// sublayers of the upper layer afterwards, and the hero is deliberately not
    /// drawn here: it belongs between the two layers, which is why a wall tile
    /// of the lower layer is exposed by a caller that draws the hero between the
    /// two calls below.
    /// </summary>
    public void RenderLower(Rm2kPixelBuffer pTarget, Rm2kMapLayers pMap, Rm2kChipsetTables pTables, int pFrameCount)
    {
        RenderLayer(pTarget, pMap, pTables, pFrameCount, pLowerLayer: true);
    }

    /// <summary>Draws the upper layer, which the Player draws above the hero.</summary>
    public void RenderUpper(Rm2kPixelBuffer pTarget, Rm2kMapLayers pMap, Rm2kChipsetTables pTables, int pFrameCount)
    {
        RenderLayer(pTarget, pMap, pTables, pFrameCount, pLowerLayer: false);
    }

    private void RenderLayer(
        Rm2kPixelBuffer pTarget, Rm2kMapLayers pMap, Rm2kChipsetTables pTables,
        int pFrameCount, bool pLowerLayer)
    {
        ArgumentNullException.ThrowIfNull(pTarget);
        ArgumentNullException.ThrowIfNull(pMap);
        ArgumentNullException.ThrowIfNull(pTables);
        for (var subLayer = Rm2kTileZOrder.SubLayerBelow; subLayer <= Rm2kTileZOrder.SubLayerAbove; subLayer++)
        {
            for (var y = 0; y < pMap.Height; y++)
            {
                for (var x = 0; x < pMap.Width; x++)
                {
                    var chipId = pMap.ChipIdAt(x, y, pLowerLayer);
                    if (chipId < 0)
                    {
                        continue;
                    }
                    var actual = pLowerLayer
                        ? Rm2kTileZOrder.LowerLayerSubLayer(chipId, pTables.Lower, pTables.Substitution)
                        : Rm2kTileZOrder.UpperLayerSubLayer(chipId, pTables.Upper, pTables.Substitution);
                    if (actual != subLayer)
                    {
                        continue;
                    }
                    BlitChip(pTarget, chipId, x, y, pTables, pFrameCount);
                }
            }
        }
    }

    private void BlitChip(
        Rm2kPixelBuffer pTarget, int pChipId, int pX, int pY,
        Rm2kChipsetTables pTables, int pFrameCount)
    {
        var targetX = pX * Rm2kChipsetBitmap.TileSize;
        var targetY = pY * Rm2kChipsetBitmap.TileSize;
        if (pChipId < Rm2kChipset.BlockD)
        {
            if (!Rm2kAutotileQuarters.TryResolveBlockAB(pChipId, AnimationStep(pChipId, pFrameCount, pTables), out var quarters))
            {
                return;
            }
            foreach (var quarter in quarters)
            {
                _chipset.TryBlitTile(quarter.Column, quarter.Row, pTarget, targetX, targetY);
            }
            return;
        }
        if (pChipId < Rm2kChipset.BlockE)
        {
            if (!Rm2kAutotileQuarters.TryResolveBlockD(pChipId, out var blockDQuarters))
            {
                return;
            }
            foreach (var quarter in blockDQuarters)
            {
                _chipset.TryBlitTile(quarter.Column, quarter.Row, pTarget, targetX, targetY);
            }
            return;
        }
        if (Rm2kChipsetSource.TryResolve(pChipId, pFrameCount, pTables.Substitution, out var rect))
        {
            _chipset.TryBlitTile(rect.Column, rect.Row, pTarget, targetX, targetY);
        }
    }

    /// <summary>
    /// The A and B autotiles animate on the chipset settings, while block C
    /// animates on its own fixed cycle, so the step depends on the block.
    /// </summary>
    private static int AnimationStep(int pChipId, int pFrameCount, Rm2kChipsetTables pTables)
    {
        return Rm2kChipset.ChipAnimationStep(
            pChipId, pFrameCount, pTables.AnimationType, pTables.AnimationSpeed);
    }
}

/// <summary>Map layers in raw tile ids.</summary>
public sealed class Rm2kMapLayers
{
    public Rm2kMapLayers(int pWidth, int pHeight, int[] pLowerLayer, int[]? pUpperLayer)
    {
        if (pWidth <= 0 || pHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pWidth), "Map dimensions must be positive.");
        }
        ArgumentNullException.ThrowIfNull(pLowerLayer);
        if (pLowerLayer.Length != checked(pWidth * pHeight))
        {
            throw new ArgumentException("The lower layer does not cover the map.", nameof(pLowerLayer));
        }
        Width = pWidth;
        Height = pHeight;
        LowerLayer = pLowerLayer;
        UpperLayer = pUpperLayer;
    }

    public int Width { get; }
    public int Height { get; }
    public int[] LowerLayer { get; }
    public int[]? UpperLayer { get; }

    /// <summary>Tile id of a coordinate, or -1 when the layer does not cover it.</summary>
    public int ChipIdAt(int pX, int pY, bool pLowerLayer)
    {
        if (pX < 0 || pY < 0 || pX >= Width || pY >= Height)
        {
            return -1;
        }
        var index = pX + pY * Width;
        var layer = pLowerLayer ? LowerLayer : UpperLayer;
        if (layer == null || index >= layer.Length)
        {
            return -1;
        }
        return layer[index];
    }
}

/// <summary>Chipset data the renderer needs, as decoded by the runtime.</summary>
public sealed class Rm2kChipsetTables
{
    public byte[]? Lower { get; init; }
    public byte[]? Upper { get; init; }
    public Rm2kTileSubstitution? Substitution { get; init; }
    public int AnimationType { get; init; } = Rm2kChipset.AnimTypeReciprocating;
    public int AnimationSpeed { get; init; }
}
