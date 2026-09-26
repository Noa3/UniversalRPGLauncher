using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Pins the verified RM2K draw order from EasyRPG Player
/// <c>src/tilemap_layer.cpp</c> (<c>CreateTileCacheAt</c> and the two
/// <c>TilemapSubLayer</c> constructions) and renders the real pinned map.
/// </summary>
public partial class TestRm2kMapFrame : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";
    private const string ChipsetFixture = "rm2000/ChipSet/World.png";

    public void Test_LowerLayerSubLayerFollowsWallAndAboveFlags()
    {
        // CreateTileCacheAt: passable[chip_index] & (Wall | Above) puts a lower
        // layer tile into the above sublayer.
        var passability = new byte[162];
        Array.Fill(passability, Rm2kChipset.AllDirections);
        passability[Rm2kChipset.BlockDIndex] = Rm2kChipset.PassWall;
        passability[Rm2kChipset.BlockDIndex + 1] = Rm2kChipset.PassAbove;
        passability[Rm2kChipset.BlockDIndex + 2] =
            (byte)(Rm2kChipset.PassWall | Rm2kChipset.PassAbove);

        AssertEq(Rm2kTileZOrder.LowerLayerSubLayer(Rm2kChipset.BlockD, passability, null),
            Rm2kTileZOrder.SubLayerAbove, "a wall tile is drawn above");
        AssertEq(Rm2kTileZOrder.LowerLayerSubLayer(Rm2kChipset.BlockD + 50, passability, null),
            Rm2kTileZOrder.SubLayerAbove, "an above tile is drawn above");
        AssertEq(Rm2kTileZOrder.LowerLayerSubLayer(Rm2kChipset.BlockD + 100, passability, null),
            Rm2kTileZOrder.SubLayerAbove, "wall and above together are drawn above");
        AssertEq(Rm2kTileZOrder.LowerLayerSubLayer(Rm2kChipset.BlockD + 150, passability, null),
            Rm2kTileZOrder.SubLayerBelow, "a plain autotile is drawn below");
        AssertEq(Rm2kTileZOrder.LowerLayerSubLayer(Rm2kChipset.BlockC, passability, null),
            Rm2kTileZOrder.SubLayerBelow, "block C defaults to below");
    }

    public void Test_UpperLayerSubLayerFollowsTheAboveFlag()
    {
        var passability = new byte[144];
        Array.Fill(passability, Rm2kChipset.AllDirections);
        passability[0] = (byte)(Rm2kChipset.PassAbove | Rm2kChipset.AllDirections);
        AssertEq(Rm2kTileZOrder.UpperLayerSubLayer(Rm2kChipset.BlockF, passability, null),
            Rm2kTileZOrder.SubLayerAbove, "an above upper tile is drawn above");
        AssertEq(Rm2kTileZOrder.UpperLayerSubLayer(Rm2kChipset.BlockF + 1, passability, null),
            Rm2kTileZOrder.SubLayerBelow, "a plain upper tile is drawn below");
        AssertEq(Rm2kTileZOrder.UpperLayerSubLayer(Rm2kChipset.BlockF - 1, passability, null),
            Rm2kTileZOrder.SubLayerBelow, "a tile below block F has no upper entry");
    }

    public void Test_SubLayerRulesFailClosedWithoutPassability()
    {
        // The Player keeps the default TileBelow when there is no passability.
        AssertEq(Rm2kTileZOrder.LowerLayerSubLayer(Rm2kChipset.BlockD, null, null), Rm2kTileZOrder.SubLayerBelow);
        AssertEq(Rm2kTileZOrder.LowerLayerSubLayer(Rm2kChipset.BlockD, [], null), Rm2kTileZOrder.SubLayerBelow);
        AssertEq(Rm2kTileZOrder.UpperLayerSubLayer(Rm2kChipset.BlockF, null, null), Rm2kTileZOrder.SubLayerBelow);
        AssertEq(Rm2kTileZOrder.UpperLayerSubLayer(Rm2kChipset.BlockF + 500, new byte[144], null),
            Rm2kTileZOrder.SubLayerBelow, "an id outside the table is drawn below");
    }

    public void Test_ChipIndexResolutionMatchesTheTileCacheRule()
    {
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(0, null), 0, "block A start");
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockA + 1999, null), 1, "block A second tile");
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockB + 999, null), 2, "block B");
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockC, null), 3, "block C start");
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockC + 100, null), 5);
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockD, null), 6, "block D start");
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockE, null), 18, "block E start");
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockE + 143, null), 161, "block E end");

        // The substitution moves a block E chip index, matching IsPassableLowerTile.
        var lowerTable = new int[144];
        for (var index = 0; index < lowerTable.Length; index++)
        {
            lowerTable[index] = index;
        }
        lowerTable[0] = 7;
        var substitution = new Rm2kTileSubstitution(lowerTable, null);
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockE, substitution),
            Rm2kChipset.BlockEIndex + 7, "block E follows the lower substitution");
        AssertEq(Rm2kTileZOrder.ResolveChipIndex(Rm2kChipset.BlockD, substitution),
            Rm2kChipset.BlockDIndex, "block D is not substituted");
    }

    public void Test_RealMapRendersLowerAndUpperLayers()
    {
        var parser = new Rm2kParser();
        var database = parser.ParseDatabase(ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.ldb")));
        AssertTrue(database.IsSuccess(), "the pinned LDB parses");
        if (!database.IsSuccess())
        {
            return;
        }
        var map = parser.ParseMap(ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/Map0001.lmu")));
        AssertTrue(map.IsSuccess(), "the pinned LMU parses");
        if (!map.IsSuccess())
        {
            return;
        }
        var chipsetPath = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        AssertTrue(File.Exists(chipsetPath), $"the pinned chipset fixture exists: {ChipsetFixture}");
        if (!File.Exists(chipsetPath))
        {
            return;
        }
        var decoded = Rm2kChipsetBitmap.TryLoad(chipsetPath, out var chipset, out var chipsetError);
        AssertTrue(decoded, $"the pinned chipset decodes: {chipsetError}");
        if (!decoded)
        {
            return;
        }

        var width = (int)map.GetData()["width"];
        var height = (int)map.GetData()["height"];
        var lower = (int[])map.GetData()["lower_layer"];
        var upper = (int[])map.GetData()["upper_layer"];
        var tables = new Rm2kChipsetTables
        {
            Lower = LoadPassability(database.GetData(), "passable_data_lower"),
            Upper = LoadPassability(database.GetData(), "passable_data_upper"),
        };

        var layers = new Rm2kMapLayers(width, height, lower, upper);
        var renderer = new Rm2kMapFrameRenderer(chipset);
        var frame = new Rm2kPixelBuffer(width * Rm2kChipsetBitmap.TileSize, height * Rm2kChipsetBitmap.TileSize);
        renderer.RenderLower(frame, layers, tables, 0);
        var lowerPainted = CountOpaque(frame);
        AssertTrue(lowerPainted > 0, "the real lower layer painted pixels");

        // Transparency is verified on a single tile instead, because the pinned
        // testgame room is covered by floor and wall tiles that fill every pixel
        // and therefore cannot show the panorama rule.

        // The pinned testgame map documents its own shape: 20x15, a lower layer of
        // block D and E tiles only, and an upper layer of block F tiles only. It
        // therefore contains no animated autotile, and its upper tiles are fully
        // transparent in the real chipset, so drawing them must add nothing.
        AssertEq(width, 20, "the real map is 20 tiles wide");
        AssertEq(height, 15, "the real map is 15 tiles high");
        AssertEq(DescribeBlocks(lower), "D,E", "the real lower layer blocks");
        AssertEq(DescribeBlocks(upper), "F", "the real upper layer blocks");
        renderer.RenderUpper(frame, layers, tables, 0);
        AssertEq(CountOpaque(frame), lowerPainted,
            "the real upper tiles are transparent, so the frame does not change");
    }

    /// <summary>Sorted, distinct block letters of a raw layer.</summary>
    private static string DescribeBlocks(int[] pLayer)
    {
        var blocks = new HashSet<string>();
        foreach (var chipId in pLayer)
        {
            blocks.Add(DescribeBlock(chipId));
        }
        return string.Join(",", blocks.OrderBy(pName => pName, StringComparer.Ordinal));
    }

    public void Test_VisibleUpperTileIsDrawnAboveTheLowerLayer()
    {
        // The pinned map has transparent upper tiles, so the upper layer draw path
        // is verified with a synthetic map that uses a visible chipset tile.
        var chipsetPath = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        AssertTrue(File.Exists(chipsetPath), $"the pinned chipset fixture exists: {ChipsetFixture}");
        if (!File.Exists(chipsetPath))
        {
            return;
        }
        var decoded = Rm2kChipsetBitmap.TryLoad(chipsetPath, out var chipset, out var chipsetError);
        AssertTrue(decoded, $"the pinned chipset decodes: {chipsetError}");
        if (!decoded)
        {
            return;
        }
        var visibleUpper = FindVisibleUpperChip(chipset);
        AssertTrue(visibleUpper >= 0, "the real chipset has an upper tile with opaque pixels");
        if (visibleUpper < 0)
        {
            return;
        }

        var lower = new[] { Rm2kChipset.BlockD };
        var upper = new[] { visibleUpper };
        var layers = new Rm2kMapLayers(1, 1, lower, upper);
        var renderer = new Rm2kMapFrameRenderer(chipset);
        var lowerOnly = new Rm2kPixelBuffer(16, 16);
        renderer.RenderLower(lowerOnly, layers, new Rm2kChipsetTables(), 0);
        AssertTrue(CountOpaque(lowerOnly) > 0, "the lower tile painted");

        // The upper tile covers the same tile area, so the opaque pixel count can
        // stay the same; what must change is the image content.
        var both = new Rm2kPixelBuffer(16, 16);
        renderer.RenderLower(both, layers, new Rm2kChipsetTables(), 0);
        renderer.RenderUpper(both, layers, new Rm2kChipsetTables(), 0);
        AssertFalse(both.Pixels.SequenceEqual(lowerOnly.Pixels),
            "a visible upper tile changes the rendered tile area");
        AssertTrue(CountOpaque(both) > 0, "the combined frame still has painted pixels");

        // A transparent chipset tile must leave the target untouched, which is
        // what lets a panorama or an earlier layer show through.
        var transparentChip = FindTransparentUpperChip(chipset);
        AssertTrue(transparentChip >= 0, "the real chipset has a fully transparent upper tile");
        if (transparentChip < 0)
        {
            return;
        }
        var empty = new Rm2kPixelBuffer(16, 16);
        renderer.RenderUpper(
            empty,
            new Rm2kMapLayers(1, 1, [Rm2kChipset.BlockD], [transparentChip]),
            new Rm2kChipsetTables(),
            0);
        AssertEq(CountOpaque(empty), 0, "a transparent upper tile paints nothing");
    }

    /// <summary>Finds an upper layer chipset tile that is fully transparent.</summary>
    private static int FindTransparentUpperChip(Rm2kChipsetBitmap pChipset)
    {
        for (var offset = 0; offset < 144; offset++)
        {
            var column = offset < 48 ? 18 + offset % 6 : 24 + (offset - 48) % 6;
            var row = offset < 48 ? 8 + offset / 6 : (offset - 48) / 6;
            var hasOpaque = false;
            for (var y = 0; y < Rm2kChipsetBitmap.TileSize && !hasOpaque; y++)
            {
                for (var x = 0; x < Rm2kChipsetBitmap.TileSize; x++)
                {
                    if (pChipset.IndexAt(column * Rm2kChipsetBitmap.TileSize + x, row * Rm2kChipsetBitmap.TileSize + y) != 0)
                    {
                        hasOpaque = true;
                        break;
                    }
                }
            }
            if (!hasOpaque)
            {
                return Rm2kChipset.BlockF + offset;
            }
        }
        return -1;
    }

    public void Test_AnimatedBlocksChangeWithTheFrameCount()
    {
        // The pinned map uses only block D and E, so animation is verified with a
        // synthetic map over the A, B and C blocks, which do animate.
        var chipsetPath = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        AssertTrue(File.Exists(chipsetPath), $"the pinned chipset fixture exists: {ChipsetFixture}");
        if (!File.Exists(chipsetPath))
        {
            return;
        }
        var decoded = Rm2kChipsetBitmap.TryLoad(chipsetPath, out var chipset, out var chipsetError);
        AssertTrue(decoded, $"the pinned chipset decodes: {chipsetError}");
        if (!decoded)
        {
            return;
        }
        var renderer = new Rm2kMapFrameRenderer(chipset);
        var animatedChips = new List<int>();
        foreach (var chipId in new[]
        {
            Rm2kChipset.BlockA, Rm2kChipset.BlockA + 1, Rm2kChipset.BlockB, Rm2kChipset.BlockC,
        })
        {
            // The pinned chipset may leave a block empty, so only chips that paint
            // something at all can be compared.
            var probe = new Rm2kPixelBuffer(32, 32);
            renderer.RenderLower(probe, new Rm2kMapLayers(1, 1, [chipId], null), new Rm2kChipsetTables(), 0);
            if (CountOpaque(probe) == 0)
            {
                continue;
            }
            animatedChips.Add(chipId);
            var atZero = new Rm2kPixelBuffer(32, 32);
            var atTwentyFour = new Rm2kPixelBuffer(32, 32);
            renderer.RenderLower(atZero, new Rm2kMapLayers(1, 1, [chipId], null), new Rm2kChipsetTables(), 0);
            renderer.RenderLower(atTwentyFour, new Rm2kMapLayers(1, 1, [chipId], null), new Rm2kChipsetTables(), 24);
            AssertTrue(CountOpaque(atTwentyFour) > 0, $"chip {chipId} paints at frame 24");
            AssertFalse(atZero.Pixels.SequenceEqual(atTwentyFour.Pixels),
                $"chip {chipId} animates between frame 0 and 24");
        }
        AssertTrue(animatedChips.Count > 0, "the real chipset has at least one animated block that paints");

        // A block D tile is not animated, so it must not change.
        var blockDProbe = new Rm2kPixelBuffer(16, 16);
        renderer.RenderLower(blockDProbe, new Rm2kMapLayers(1, 1, [Rm2kChipset.BlockD], null), new Rm2kChipsetTables(), 0);
        if (CountOpaque(blockDProbe) > 0)
        {
            var dZero = new Rm2kPixelBuffer(16, 16);
            var dTwentyFour = new Rm2kPixelBuffer(16, 16);
            renderer.RenderLower(dZero, new Rm2kMapLayers(1, 1, [Rm2kChipset.BlockD], null), new Rm2kChipsetTables(), 0);
            renderer.RenderLower(dTwentyFour, new Rm2kMapLayers(1, 1, [Rm2kChipset.BlockD], null), new Rm2kChipsetTables(), 24);
            AssertTrue(dZero.Pixels.SequenceEqual(dTwentyFour.Pixels), "block D does not animate");
        }
    }

    /// <summary>Finds an upper layer chipset tile that has at least one opaque pixel.</summary>
    private static int FindVisibleUpperChip(Rm2kChipsetBitmap pChipset)
    {
        for (var offset = 0; offset < 144; offset++)
        {
            var column = offset < 48 ? 18 + offset % 6 : 24 + (offset - 48) % 6;
            var row = offset < 48 ? 8 + offset / 6 : (offset - 48) / 6;
            for (var y = 0; y < Rm2kChipsetBitmap.TileSize; y++)
            {
                for (var x = 0; x < Rm2kChipsetBitmap.TileSize; x++)
                {
                    if (pChipset.IndexAt(column * Rm2kChipsetBitmap.TileSize + x, row * Rm2kChipsetBitmap.TileSize + y) != 0)
                    {
                        return Rm2kChipset.BlockF + offset;
                    }
                }
            }
        }
        return -1;
    }

    private static string DescribeBlock(int pChipId)
    {
        if (pChipId < Rm2kChipset.BlockB) { return "A"; }
        if (pChipId < Rm2kChipset.BlockC) { return "B"; }
        if (pChipId < Rm2kChipset.BlockD) { return "C"; }
        if (pChipId < Rm2kChipset.BlockE) { return "D"; }
        if (pChipId < Rm2kChipset.BlockF) { return "E"; }
        return "F";
    }

    public void Test_RenderRefusesIncompleteLayers()
    {
        var chipsetPath = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        if (!File.Exists(chipsetPath) || !Rm2kChipsetBitmap.TryLoad(chipsetPath, out var chipset, out _))
        {
            return;
        }
        var renderer = new Rm2kMapFrameRenderer(chipset);
        var target = new Rm2kPixelBuffer(16, 16);
        var layers = new Rm2kMapLayers(2, 1, [0, 1], null);
        renderer.RenderUpper(target, layers, new Rm2kChipsetTables(), 0);
        AssertEq(CountOpaque(target), 0, "a missing upper layer paints nothing");

        // A 1x1 map fits a 16x16 target exactly, so it does paint.
        var single = new Rm2kMapLayers(1, 1, [0], null);
        renderer.RenderLower(target, single, new Rm2kChipsetTables(), 0);
        AssertEq(CountOpaque(target), 16 * 16, "a single tile fills the target");

        // A map larger than the target is clipped by the blit, not by an overflow.
        var large = new Rm2kMapLayers(4, 4, Enumerable.Repeat(0, 16).ToArray(), null);
        var small = new Rm2kPixelBuffer(16, 16);
        renderer.RenderLower(small, large, new Rm2kChipsetTables(), 0);
        AssertEq(CountOpaque(small), 16 * 16, "only the part that fits is painted");
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

    /// <summary>Reads a verified passability table from the pinned LDB chipset section.</summary>
    private static byte[]? LoadPassability(Godot.Collections.Dictionary pDatabase, string pKey)
    {
        if (!pDatabase.TryGetValue("sections", out var rawSections)
            || rawSections.VariantType != Godot.Variant.Type.Dictionary)
        {
            return null;
        }
        var sections = rawSections.AsGodotDictionary();
        if (!sections.TryGetValue("chipsets", out var rawChipset)
            || rawChipset.VariantType != Godot.Variant.Type.Dictionary)
        {
            return null;
        }
        var chipset = rawChipset.AsGodotDictionary();
        if (!chipset.TryGetValue(pKey, out var raw)
            || raw.VariantType != Godot.Variant.Type.PackedInt32Array)
        {
            return null;
        }
        var values = raw.AsInt32Array();
        var result = new byte[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            result[index] = (byte)values[index];
        }
        return result;
    }
}
