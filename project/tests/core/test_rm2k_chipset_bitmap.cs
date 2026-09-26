using System;
using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Verifies the chipset PNG decoder against the real pinned
/// <c>EasyRPG/TestGame</c> chipset image, and the blit against the verified
/// transparency rule from EasyRPG Player <c>src/image_png.cpp</c>.
/// </summary>
public partial class TestRm2kChipsetBitmap : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";
    private const string ChipsetFixture = "rm2000/ChipSet/World.png";

    public void Test_RealChipsetImageDecodesWithTheVerifiedShape()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        AssertTrue(File.Exists(path), $"the pinned chipset fixture exists: {ChipsetFixture}");
        if (!File.Exists(path))
        {
            return;
        }

        AssertEq(Rm2kChipsetBitmap.TryLoad(path, out var bitmap, out var error), true, $"decoding succeeds: {error}");
        AssertEq(bitmap.Width, 480, "the Player chipset spec expects 480 pixels wide");
        AssertEq(bitmap.Height, 256, "the Player chipset spec expects 256 pixels high");
        AssertEq(bitmap.HasExpectedSize, true, "the real chipset has the expected size");
        AssertEq(bitmap.Indices.Length, 480 * 256, "one palette index per pixel");
        AssertTrue(bitmap.Palette.Length is > 0 and <= Rm2kChipsetBitmap.MaxPaletteEntries,
            $"the palette has a usable size but was {bitmap.Palette.Length}");

        // Columns and rows derived from the K-095 formulas must match the image.
        AssertEq(Rm2kChipsetSource.Columns * Rm2kChipsetBitmap.TileSize, bitmap.Width,
            "the derived column count fits the real image width");
        AssertEq(Rm2kChipsetSource.Rows * Rm2kChipsetBitmap.TileSize, bitmap.Height,
            "the derived row count fits the real image height");
    }

    public void Test_RealChipsetContainsTheTransparentIndexAndOpaquePixels()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        if (!File.Exists(path) || !Rm2kChipsetBitmap.TryLoad(path, out var bitmap, out _))
        {
            return;
        }
        var indexCounts = new Dictionary<int, int>();
        foreach (var index in bitmap.Indices)
        {
            indexCounts.TryGetValue(index, out var count);
            indexCounts[index] = count + 1;
        }
        AssertTrue(indexCounts.ContainsKey(Rm2kChipsetBitmap.TransparentIndex),
            "the real chipset uses index 0, which the Player treats as transparent");
        var opaque = bitmap.Indices.Count(pIndex => pIndex != Rm2kChipsetBitmap.TransparentIndex);
        AssertTrue(opaque > 0, "the real chipset also has opaque pixels");
        AssertTrue(indexCounts.Keys.All(pIndex => pIndex < bitmap.Palette.Length),
            "every used index is covered by the palette");
    }

    public void Test_BlitHonoursTheVerifiedTransparencyRule()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        if (!File.Exists(path) || !Rm2kChipsetBitmap.TryLoad(path, out var bitmap, out _))
        {
            return;
        }
        var target = new Rm2kPixelBuffer(16, 16);
        AssertEq(bitmap.TryBlitTile(0, 0, target, 0, 0), true, "the first chipset tile exists");
        var painted = 0;
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                var alpha = target.Pixels[(x + y * 16) * 4 + 3];
                if (alpha != 0)
                {
                    painted++;
                    AssertEq(alpha, 255, "an opaque pixel is fully opaque");
                    var index = bitmap.IndexAt(x, y);
                    AssertEq(index != Rm2kChipsetBitmap.TransparentIndex, true,
                        "only non zero indices are painted");
                }
                else
                {
                    AssertEq(bitmap.IndexAt(x, y), Rm2kChipsetBitmap.TransparentIndex,
                        "a transparent pixel has palette index 0");
                }
            }
        }
        AssertTrue(painted > 0, "the first tile of the real chipset has opaque pixels");
    }

    public void Test_BlitRefusesRectanglesOutsideTheImage()
    {
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        if (!File.Exists(path) || !Rm2kChipsetBitmap.TryLoad(path, out var bitmap, out _))
        {
            return;
        }
        var target = new Rm2kPixelBuffer(32, 32);
        AssertEq(bitmap.TryBlitTile(30, 0, target, 0, 0), false, "column 30 is outside the 30 column chipset");
        AssertEq(bitmap.TryBlitTile(0, 16, target, 0, 0), false, "row 16 is outside the 16 row chipset");
        AssertEq(bitmap.TryBlitTile(-1, 0, target, 0, 0), false, "a negative column is refused");
        AssertEq(bitmap.TryBlitRectangle(470, 250, 16, 16, target, 0, 0), false,
            "a rectangle that leaves the image is refused");
        AssertEq(bitmap.TryBlitTile(0, 0, target, 16, 0), true, "a tile that leaves the target still reports the copy");
    }

    public void Test_EveryRealChipsetTileIsBlittableForAllResolvedLowerBlocks()
    {
        // The strongest available check: every chipset rectangle that K-095 to
        // K-097 can produce for the real chipset must lie inside the real image.
        var path = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        if (!File.Exists(path) || !Rm2kChipsetBitmap.TryLoad(path, out var bitmap, out _))
        {
            return;
        }
        var target = new Rm2kPixelBuffer(32, 32);
        var checkedRects = 0;
        var substitution = new Rm2kTileSubstitution(null, null);

        for (var chipId = 0; chipId < Rm2kChipset.BlockD; chipId++)
        {
            for (var step = 0; step < 4; step++)
            {
                if (!Rm2kAutotileQuarters.TryResolveBlockAB(chipId, step, out var quarters))
                {
                    continue;
                }
                foreach (var quarter in quarters)
                {
                    AssertEq(bitmap.TryBlitTile(quarter.Column, quarter.Row, target, 0, 0), true,
                        $"chip {chipId} step {step} quarter column {quarter.Column} row {quarter.Row} exists");
                    checkedRects++;
                }
            }
        }
        for (var chipId = Rm2kChipset.BlockD; chipId < Rm2kChipset.BlockDEnd; chipId++)
        {
            if (!Rm2kAutotileQuarters.TryResolveBlockD(chipId, out var quarters))
            {
                continue;
            }
            foreach (var quarter in quarters)
            {
                AssertEq(bitmap.TryBlitTile(quarter.Column, quarter.Row, target, 0, 0), true,
                    $"chip {chipId} quarter column {quarter.Column} row {quarter.Row} exists");
                checkedRects++;
            }
        }
        foreach (var chipId in new[] { Rm2kChipset.BlockC, Rm2kChipset.BlockC + 149, Rm2kChipset.BlockE, Rm2kChipset.BlockE + 143, Rm2kChipset.BlockF, Rm2kChipset.BlockF + 143 })
        {
            AssertEq(Rm2kChipsetSource.TryResolve(chipId, 0, substitution, out var rect), true,
                $"chip {chipId} resolves from the chipset");
            AssertEq(bitmap.TryBlitTile(rect.Column, rect.Row, target, 0, 0), true,
                $"chip {chipId} rectangle column {rect.Column} row {rect.Row} exists");
            checkedRects++;
        }
        AssertTrue(checkedRects > 1000, $"a large number of rectangles was verified but was {checkedRects}");
    }

    public void Test_MalformedChipsetDataIsRefused()
    {
        AssertEq(Rm2kChipsetBitmap.TryParse(new byte[] { }, out _, out _), false, "empty data is refused");
        AssertEq(Rm2kChipsetBitmap.TryParse(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, out _, out _), false, "a bad signature is refused");
        var header = new byte[16];
        System.Array.Copy(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, header, 8);
        header[8] = 0; header[9] = 0; header[10] = 0; header[11] = 13;
        AssertEq(Rm2kChipsetBitmap.TryParse(header, out _, out _), false, "a truncated header is refused");

        // A paletted header with no palette and no image data must be refused.
        // Width 1, height 256, bit depth 8, colour type 3, no interlace.
        var ihdr = BuildChunk("IHDR", new byte[] { 0, 0, 0, 1, 0, 0, 1, 0, 8, 3, 0, 0, 0 });
        var onlyHeader = new byte[8 + ihdr.Length];
        System.Array.Copy(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, onlyHeader, 8);
        System.Array.Copy(ihdr, 0, onlyHeader, 8, ihdr.Length);
        AssertEq(Rm2kChipsetBitmap.TryParse(onlyHeader, out _, out _), false, "a header without data is refused");

        // A non paletted chipset is refused instead of being reinterpreted.
        var rgba = BuildChunk("IHDR", new byte[] { 0, 0, 0, 1, 0, 0, 1, 0, 8, 6, 0, 0, 0 });
        var rgbaImage = new byte[8 + rgba.Length];
        System.Array.Copy(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, rgbaImage, 8);
        System.Array.Copy(rgba, 0, rgbaImage, 8, rgba.Length);
        AssertEq(Rm2kChipsetBitmap.TryParse(rgbaImage, out _, out _), false, "a truecolour chipset is refused");
    }

    /// <summary>Builds one PNG chunk with its big endian length and CRC fields.</summary>
    private static byte[] BuildChunk(string pType, byte[] pPayload)
    {
        var result = new byte[12 + pPayload.Length];
        result[0] = (byte)(pPayload.Length >> 24);
        result[1] = (byte)(pPayload.Length >> 16);
        result[2] = (byte)(pPayload.Length >> 8);
        result[3] = (byte)pPayload.Length;
        for (var index = 0; index < 4; index++)
        {
            result[4 + index] = (byte)pType[index];
        }
        Array.Copy(pPayload, 0, result, 8, pPayload.Length);
        // The CRC is not validated by the decoder, so a zero field is enough here.
        return result;
    }
}
