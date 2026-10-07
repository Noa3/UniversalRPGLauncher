using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The 8 bit BMP orientation rules, verified against the Windows bitmap
/// contract: a positive height stores the first data row at the bottom of the
/// image, a negative height stores it at the top. Real RM2K assets write the
/// positive form, so reading it top down mirrors every chipset and character
/// sheet vertically; this was the defect behind a real game rendering in
/// magenta marker colours.
/// </summary>
public partial class TestRm2kBmpOrientation : TestBase
{
    /// <summary>
    /// Builds an 8 bit bitmap whose two rows carry distinguishable indices.
    /// Palette entries and file order are written exactly as RM2K writes them:
    /// four bytes per palette entry in B G R order, and rows padded to four.
    /// </summary>
    private static byte[] MakeTwoRowBitmap(short pHeight)
    {
        const int width = 1;
        var height = Math.Abs((int)pHeight);
        var stride = (width + 3) & ~3;
        var palette = new byte[1024];
        for (var entry = 0; entry < 256; entry++)
        {
            palette[entry * 4] = (byte)entry;
            palette[entry * 4 + 1] = (byte)entry;
            palette[entry * 4 + 2] = (byte)entry;
        }
        var pixels = new byte[stride * height];
        // First data row in the file carries index 111, the second 222.
        pixels[0] = 111;
        pixels[stride] = 222;
        var dataOffset = 14 + 40 + palette.Length;
        var file = new List<byte>();
        file.AddRange(new byte[] { (byte)'B', (byte)'M' });
        file.AddRange(BitConverter.GetBytes(dataOffset + pixels.Length));
        file.AddRange(new byte[] { 0, 0, 0, 0 });
        file.AddRange(BitConverter.GetBytes(dataOffset));
        file.AddRange(BitConverter.GetBytes(40));
        file.AddRange(BitConverter.GetBytes(width));
        file.AddRange(BitConverter.GetBytes((int)pHeight));
        file.AddRange(BitConverter.GetBytes((short)1));
        file.AddRange(BitConverter.GetBytes((short)8));
        file.AddRange(BitConverter.GetBytes(0));
        file.AddRange(BitConverter.GetBytes(pixels.Length));
        file.AddRange(BitConverter.GetBytes(0));
        file.AddRange(BitConverter.GetBytes(0));
        file.AddRange(BitConverter.GetBytes(256));
        file.AddRange(BitConverter.GetBytes(0));
        file.AddRange(palette);
        file.AddRange(pixels);
        return file.ToArray();
    }

    public void Test_PositiveHeightIsReadBottomUp()
    {
        var data = MakeTwoRowBitmap(2);
        AssertEq(Rm2kBmp.TryParse(data, out var width, out var height, out var indices, out _, out var error), true, error);
        AssertEq(width, 1);
        AssertEq(height, 2);
        // The first file row is the bottom of the image, so the top row of the
        // decoded image (row 0) must carry the second file row's index.
        AssertEq(indices[0], 222);
        AssertEq(indices[1], 111);
    }

    public void Test_NegativeHeightIsReadTopDown()
    {
        var data = MakeTwoRowBitmap(-2);
        AssertEq(Rm2kBmp.TryParse(data, out _, out var height, out var indices, out _, out var error), true, error);
        AssertEq(height, 2);
        // A negative height stores the top row first, so row 0 keeps index 111.
        AssertEq(indices[0], 111);
        AssertEq(indices[1], 222);
    }
}
