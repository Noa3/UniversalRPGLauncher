using System;
using System.Collections.Generic;
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
    private readonly Rm2kChipsetBitmap? _chipset;

    public Rm2kMapFrameRenderer(Rm2kChipsetBitmap pChipset)
    {
        _chipset = pChipset ?? throw new ArgumentNullException(nameof(pChipset));
    }

    /// <summary>
    /// Creates a renderer for sprite only passes, where no chipset tiles are drawn
    /// and the caller supplies the charset of every character itself.
    /// </summary>
    public Rm2kMapFrameRenderer()
    {
        _chipset = null;
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

    /// <summary>
    /// Where a sprite sits relative to the two map layers, following the Player's
    /// drawable priorities: below-layer events between the layers, the hero and
    /// "same as hero" events after the lower layer, and above-layer events after
    /// the upper layer.
    /// </summary>
    public enum SpriteStage
    {
        /// <summary>Below the hero: drawn between the two map layers.</summary>
        BelowLayer,

        /// <summary>Same layer as the hero: drawn after the lower layer.</summary>
        HeroLayer,

        /// <summary>Above the hero: drawn after the upper layer.</summary>
        AboveLayer,
    }

    /// <summary>
    /// Draws the characters of one stage. The Player splits events by their
    /// page layer: liblcf <c>Layers_below = 0</c> goes to the below stage,
    /// <c>Layers_same = 1</c> shares the hero priority, and <c>Layers_above = 2</c>
    /// is drawn after the upper layer. The hero is always in the hero stage.
    /// </summary>
    public int RenderSprites(
        Rm2kPixelBuffer pTarget, Rm2kMapLayers pMap, IEnumerable<Rm2kCharacterSprite> pSprites)
    {
        ArgumentNullException.ThrowIfNull(pTarget);
        ArgumentNullException.ThrowIfNull(pMap);
        if (pSprites == null)
        {
            return 0;
        }
        var drawn = 0;
        foreach (var sprite in pSprites)
        {
            if (sprite.Stage != CurrentStage)
            {
                continue;
            }
            if (!sprite.Charset.TryDrawCharacter(
                sprite.CharacterIndex, sprite.FacingDirection, sprite.Frame, pTarget, sprite.MapX, sprite.MapY))
            {
                sprite.Skipped = true;
                continue;
            }
            drawn++;
        }
        return drawn;
    }

    /// <summary>Which stage <see cref="RenderSprites"/> currently draws.</summary>
    public SpriteStage CurrentStage { get; set; } = SpriteStage.HeroLayer;

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
        if (_chipset == null)
        {
            return;
        }
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

/// <summary>
/// One character to draw into the map frame: where it stands, which charset cell
/// it uses and which stage the Player draws it in.
/// </summary>
public sealed class Rm2kCharacterSprite
{
    public required Rm2kCharset Charset { get; init; }
    public required int MapX { get; init; }
    public required int MapY { get; init; }

    /// <summary>Charset cell index from the character index field.</summary>
    public int CharacterIndex { get; init; }

    /// <summary>Facing as this project stores it: 2 down, 4 left, 6 right, 8 up.</summary>
    public byte FacingDirection { get; init; } = 2;

    /// <summary>liblcf frame; anything from middle2 is clamped like the Player does.</summary>
    public int Frame { get; init; } = Rm2kCharset.FrameMiddle;

    /// <summary>Stage the Player draws this character in.</summary>
    public Rm2kMapFrameRenderer.SpriteStage Stage { get; init; } = Rm2kMapFrameRenderer.SpriteStage.HeroLayer;

    /// <summary>Set when the sprite could not be drawn, so a caller can report it.</summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// Maps a liblcf event page layer to the draw stage, following
    /// <c>Layers_below = 0</c>, <c>Layers_same = 1</c> and <c>Layers_above = 2</c>.
    /// The hero itself is never below.
    /// </summary>
    public static Rm2kMapFrameRenderer.SpriteStage StageForLayer(int pLayer)
    {
        return pLayer switch
        {
            0 => Rm2kMapFrameRenderer.SpriteStage.BelowLayer,
            2 => Rm2kMapFrameRenderer.SpriteStage.AboveLayer,
            _ => Rm2kMapFrameRenderer.SpriteStage.HeroLayer,
        };
    }

    /// <summary>
    /// Converts a liblcf event page direction into the facing this project stores.
    /// liblcf uses up 0, right 1, down 2, left 3.
    /// </summary>
    public static byte FacingFromLiblcfDirection(int pDirection)
    {
        return pDirection switch
        {
            1 => 6,
            3 => 4,
            2 => 2,
            _ => 8,
        };
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
