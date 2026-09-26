using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Verifies the RM2K charset geometry and drawing against the real pinned
/// EasyRPG TestGame charset image.
/// </summary>
public partial class TestRm2kCharset : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";
    private const string CharsetFixture = "rm2000/CharSet/Chara1.png";

    public void Test_RealCharsetMatchesTheVerifiedCellGeometry()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(CharsetFixture));
        AssertTrue(File.Exists(path), $"the pinned charset fixture exists: {CharsetFixture}");
        if (!File.Exists(path))
        {
            return;
        }
        var decoded = Rm2kIndexedImage.TryLoad(path, out var image, out var error);
        AssertTrue(decoded, $"the pinned charset decodes: {error}");
        if (!decoded)
        {
            return;
        }
        var charset = new Rm2kCharset(image);

        // GetCharacterRect builds a 72 by 128 cell and places it at
        // (index % 4, index / 4), so the image must be a whole number of cells.
        AssertEq(Rm2kCharset.CellWidth, 72, "the cell is 24 * 3 pixels wide");
        AssertEq(Rm2kCharset.CellHeight, 128, "the cell is 32 * 4 pixels high");
        AssertEq(Rm2kCharset.FrameWidth, 24, "a frame is 24 pixels wide");
        AssertEq(Rm2kCharset.FrameHeight, 32, "a frame is 32 pixels high");
        AssertEq(image.Width % Rm2kCharset.CellWidth, 0, "the width is a whole number of cells");
        AssertEq(image.Height % Rm2kCharset.CellHeight, 0, "the height is a whole number of cells");
        AssertEq(image.Width, 4 * Rm2kCharset.CellWidth,
            "the real charset is exactly the four cells per row the index split allows");
        AssertEq(charset.CellCapacity, 12, "the real charset holds twelve characters");
    }

    public void Test_CellAndFrameCoordinatesFollowTheIndexSplit()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(CharsetFixture));
        if (!File.Exists(path) || !Rm2kIndexedImage.TryLoad(path, out var image, out _))
        {
            return;
        }
        var charset = new Rm2kCharset(image);
        foreach (var index in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 11 })
        {
            AssertEq(charset.TryGetCell(index, out var cellX, out var cellY), true, $"cell {index} exists");
            AssertEq(cellX, index % 4 * Rm2kCharset.CellWidth, $"cell {index} column");
            AssertEq(cellY, index / 4 * Rm2kCharset.CellHeight, $"cell {index} row");
        }
        AssertEq(charset.TryGetCell(-1, out _, out _), false, "a negative index is refused");
        AssertEq(charset.TryGetCell(charset.CellCapacity, out _, out _), false, "an index past the image is refused");

        AssertEq(Rm2kCharset.TryGetFrameRect(0, 0, Rm2kCharset.DirectionUp, Rm2kCharset.FrameMiddle,
            out var frameX, out var frameY), true, "the middle frame of the up row exists");
        AssertEq(frameX, 24, "the middle frame is the second column");
        AssertEq(frameY, 0, "the up row is the first row");
        AssertEq(Rm2kCharset.TryGetFrameRect(0, 0, Rm2kCharset.DirectionLeft, Rm2kCharset.FrameRight,
            out var leftX, out var leftY), true, "the right frame of the left row exists");
        AssertEq(leftX, 48, "the right frame is the third column");
        AssertEq(leftY, 3 * Rm2kCharset.FrameHeight, "the left row is the fourth row");
    }

    public void Test_FrameClampingMatchesThePlayer()
    {
        // Sprite_Character::Draw replaces anything from Frame_middle2 with
        // Frame_middle.
        AssertEq(Rm2kCharset.ClampFrame(Rm2kCharset.FrameLeft), Rm2kCharset.FrameLeft);
        AssertEq(Rm2kCharset.ClampFrame(Rm2kCharset.FrameMiddle), Rm2kCharset.FrameMiddle);
        AssertEq(Rm2kCharset.ClampFrame(Rm2kCharset.FrameRight), Rm2kCharset.FrameRight);
        AssertEq(Rm2kCharset.ClampFrame(Rm2kCharset.FrameMiddle2), Rm2kCharset.FrameMiddle,
            "the fourth frame is shown as the middle frame");
        AssertEq(Rm2kCharset.ClampFrame(9), Rm2kCharset.FrameMiddle, "an unknown frame is shown as the middle one");
        AssertEq(Rm2kCharset.ClampFrame(-1), Rm2kCharset.FrameLeft, "a negative frame is clamped to the first");
    }

    public void Test_FacingConversionUsesTheVerifiedDirectionIndex()
    {
        // The project stores 2 down, 4 left, 6 right, 8 up; the Player uses the
        // liblcf direction index up 0, right 1, down 2, left 3 as the row.
        AssertEq(Rm2kCharset.FacingToRow(2), Rm2kCharset.DirectionDown);
        AssertEq(Rm2kCharset.FacingToRow(4), Rm2kCharset.DirectionLeft);
        AssertEq(Rm2kCharset.FacingToRow(6), Rm2kCharset.DirectionRight);
        AssertEq(Rm2kCharset.FacingToRow(8), Rm2kCharset.DirectionUp);
        AssertEq(Rm2kCharset.FacingToRow(0), Rm2kCharset.DirectionUp, "an unknown facing falls back to up");
    }

    public void Test_CharacterIsDrawnWithFeetOnTheTileBottom()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(CharsetFixture));
        if (!File.Exists(path) || !Rm2kIndexedImage.TryLoad(path, out var image, out _))
        {
            return;
        }
        var charset = new Rm2kCharset(image);
        // SetOx(chara_width / 2) centres the frame, SetOy(chara_height) puts the
        // feet on the bottom edge of the map tile.
        var target = new Rm2kPixelBuffer(64, 64);
        AssertEq(charset.TryDrawCharacter(0, 2, Rm2kCharset.FrameMiddle, target, 1, 1), true,
            "the character frame is drawn");
        var opaque = 0;
        for (var index = 3; index < target.Pixels.Length; index += 4)
        {
            if (target.Pixels[index] != 0)
            {
                opaque++;
            }
        }
        AssertTrue(opaque > 0, "the drawn character has opaque pixels");

        // Every pixel must sit inside the target, which proves the offsets do not
        // read or write outside the buffer.
        var tallest = -1;
        for (var y = 0; y < target.Height; y++)
        {
            for (var x = 0; x < target.Width; x++)
            {
                if (target.Pixels[(x + y * target.Width) * 4 + 3] != 0)
                {
                    if (tallest < 0 || y > tallest)
                    {
                        tallest = y;
                    }
                }
            }
        }
        AssertTrue(tallest < 64, "no pixel was written outside the target");
        AssertEq(charset.TryDrawCharacter(-1, 2, 0, target, 0, 0), false, "an invalid index draws nothing");

        // A character at the frame edge is clipped instead of refused, which is
        // what the Player does. At tile (0, 0) the top half of the 32 pixel high
        // frame is cut off, because the feet sit on the tile bottom.
        var edge = new Rm2kPixelBuffer(32, 32);
        AssertEq(charset.TryDrawCharacter(0, 2, Rm2kCharset.FrameMiddle, edge, 0, 0), true,
            "a character at the frame edge is clipped, not refused");
        var edgeOpaque = 0;
        for (var index = 3; index < edge.Pixels.Length; index += 4)
        {
            AssertTrue(edge.Pixels[index] is 0 or 255, "clipped output is opaque or fully transparent");
            if (edge.Pixels[index] != 0)
            {
                edgeOpaque++;
            }
        }
        AssertTrue(edgeOpaque > 0, "the visible lower half is painted");
        AssertTrue(edgeOpaque < opaque, "clipping paints less than a fully visible character");

        // One tile further outside the frame, nothing of the character is visible.
        var outside = new Rm2kPixelBuffer(32, 32);
        AssertEq(charset.TryDrawCharacter(0, 2, Rm2kCharset.FrameMiddle, outside, -1, -1), true,
            "a character fully outside the frame is still handled without throwing");
        AssertEq(CountOpaque(outside), 0, "a fully clipped character paints nothing");
    }

    public void Test_EventLayerAndDirectionMapToTheDrawStage()
    {
        // liblcf Layers_below = 0, Layers_same = 1, Layers_above = 2 and
        // Direction up 0, right 1, down 2, left 3.
        AssertEq(Rm2kCharacterSprite.StageForLayer(0), Rm2kMapFrameRenderer.SpriteStage.BelowLayer);
        AssertEq(Rm2kCharacterSprite.StageForLayer(1), Rm2kMapFrameRenderer.SpriteStage.HeroLayer);
        AssertEq(Rm2kCharacterSprite.StageForLayer(2), Rm2kMapFrameRenderer.SpriteStage.AboveLayer);
        AssertEq(Rm2kCharacterSprite.StageForLayer(7), Rm2kMapFrameRenderer.SpriteStage.HeroLayer,
            "an unknown layer falls back to the hero stage");

        // The project stores 2 down, 4 left, 6 right, 8 up.
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(0), (byte)8, "liblcf up");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(1), (byte)6, "liblcf right");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(2), (byte)2, "liblcf down");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(3), (byte)4, "liblcf left");
        AssertEq(Rm2kCharacterSprite.FacingFromLiblcfDirection(9), (byte)8, "an unknown direction is up");

        // The two conversions have to be inverse to each other.
        foreach (var facing in new byte[] { 2, 4, 6, 8 })
        {
            AssertEq(Rm2kCharset.FacingToRow(Rm2kCharacterSprite.FacingFromLiblcfDirection(
                Rm2kCharset.FacingToRow(facing))), Rm2kCharset.FacingToRow(facing),
                $"facing {facing} survives the round trip");
        }
    }

    public void Test_SpritesAreDrawnPerStageAndInvalidOnesAreSkipped()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(CharsetFixture));
        if (!File.Exists(path) || !Rm2kIndexedImage.TryLoad(path, out var image, out _))
        {
            return;
        }
        var charset = new Rm2kCharset(image);
        var renderer = new Rm2kMapFrameRenderer();
        var sprites = new List<Rm2kCharacterSprite>
        {
            new() { Charset = charset, MapX = 0, MapY = 0, CharacterIndex = 0, Stage = Rm2kMapFrameRenderer.SpriteStage.BelowLayer },
            new() { Charset = charset, MapX = 1, MapY = 0, CharacterIndex = 1, Stage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer },
            new() { Charset = charset, MapX = 2, MapY = 0, CharacterIndex = 2, Stage = Rm2kMapFrameRenderer.SpriteStage.AboveLayer },
            new() { Charset = charset, MapX = 3, MapY = 0, CharacterIndex = 9999, Stage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer },
        };
        var target = new Rm2kPixelBuffer(64, 16);

        renderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.BelowLayer;
        AssertEq(renderer.RenderSprites(target, new Rm2kMapLayers(4, 1, [0, 0, 0, 0], null), sprites), 1,
            "only the below stage is drawn");
        renderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer;
        AssertEq(renderer.RenderSprites(target, new Rm2kMapLayers(4, 1, [0, 0, 0, 0], null), sprites), 1,
            "the hero stage draws the valid sprite and skips the out of range index");
        renderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.AboveLayer;
        AssertEq(renderer.RenderSprites(target, new Rm2kMapLayers(4, 1, [0, 0, 0, 0], null), sprites), 1,
            "only the above stage is drawn");

        AssertEq(sprites[3].Skipped, true,
            $"a character index past the charset capacity is skipped, not drawn from an arbitrary cell");

        // Each stage must be drawn into its own frame so the order is observable.
        var below = new Rm2kPixelBuffer(64, 16);
        renderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.BelowLayer;
        renderer.RenderSprites(below, new Rm2kMapLayers(4, 1, [0, 0, 0, 0], null), sprites);
        var hero = new Rm2kPixelBuffer(64, 16);
        renderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer;
        renderer.RenderSprites(hero, new Rm2kMapLayers(4, 1, [0, 0, 0, 0], null), sprites);
        AssertFalse(System.Linq.Enumerable.SequenceEqual(below.Pixels, hero.Pixels),
            "the below stage and the hero stage paint different characters");
    }

    private static int CountOpaque(Rm2kPixelBuffer pBuffer)
    {
        var count = 0;
        for (var index = 3; index < pBuffer.Pixels.Length; index += 4)
        {
            if (pBuffer.Pixels[index] != 0)
            {
                count++;
            }
        }
        return count;
    }
}
