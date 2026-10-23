using System;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

public partial class TestMzRgbaPixels : TestBase
{
    public void Test_RgbExpansionPreservesPixelsAcrossAllFilters() => CheckPixels(3);
    public void Test_RgbaControlPreservesColorAndAlphaAcrossAllFilters() => CheckPixels(4);

    private void CheckPixels(int channels)
    {
        var cases = 0;
        foreach (var size in new[] { (1, 1), (2, 2), (3, 2), (4, 3) })
        foreach (var filter in new[] { 0, 1, 2, 3, 4 })
        {
            var (width, height) = size;
            var raw = new byte[width * height * channels];
            var expected = new byte[width * height * 4];
            for (var i = 0; i < width * height; i++)
            {
                for (var channel = 0; channel < 3; channel++)
                {
                    var value = unchecked((byte)(11 + i * 37 + channel * 19));
                    raw[i * channels + channel] = value;
                    expected[i * 4 + channel] = value;
                }
                var alpha = channels == 3 ? (byte)255 : new byte[] { 0, 1, 127, 255 }[i % 4];
                if (channels == 4) raw[i * channels + 3] = alpha;
                expected[i * 4 + 3] = alpha;
            }
            var stride = width * channels;
            var rows = new byte[height * (stride + 1)];
            for (var y = 0; y < height; y++)
            {
                rows[y * (stride + 1)] = (byte)filter;
                for (var x = 0; x < stride; x++)
                {
                    var index = y * stride + x;
                    int left = x >= channels ? raw[index - channels] : 0;
                    int up = y > 0 ? raw[index - stride] : 0;
                    int diagonal = y > 0 && x >= channels ? raw[index - stride - channels] : 0;
                    var prediction = filter switch
                    {
                        0 => 0, 1 => left, 2 => up, 3 => (left + up) / 2,
                        _ => PredictPaeth(left, up, diagonal),
                    };
                    rows[y * (stride + 1) + 1 + x] = unchecked((byte)(raw[index] - prediction));
                }
            }
            var png = TestRgssBitmapFiles.Png(width, height, channels == 3 ? 2 : 6, 8, rows);
            var context = $"channels={channels}, size={width}x{height}, filter={filter}";
            using var native = new Image();
            AssertEq(native.LoadPngFromBuffer(png), Error.Ok, context);
            native.Convert(Image.Format.Rgba8);
            var oracle = native.GetData();
            AssertTrue(oracle.SequenceEqual(expected), "Fixture/native agreement: " + context);
            AssertTrue(MzRgbaImage.TryParse(png, out var decoded, out var error), context + ": " + error);
            AssertTrue(decoded != null, context);
            if (decoded == null) continue;
            AssertEq(decoded.Width, width);
            AssertEq(decoded.Height, height);
            AssertTrue(decoded.Pixels.SequenceEqual(oracle), "Decoder pixels must match the native oracle: " + context);
            AssertTrue(decoded.TryGetPixel(width - 1, height - 1, out var last));
            AssertTrue(last.SequenceEqual(expected.AsSpan(expected.Length - 4).ToArray()), "Last pixel: " + context);

            // Exercise the real caller, not only a disconnected decoder helper.
            AssertTrue(MzCharacterSheet.Read(png, out var sheet, out error), context + ": " + error);
            AssertTrue(sheet != null && !sheet.WasPalette, "True-color path: " + context);
            if (sheet != null) AssertTrue(sheet.Pixels.SequenceEqual(expected), "Character-sheet pixels: " + context);
            cases++;
        }
        GD.Print($"MZ_PNG_PIXEL_CASES: channels={channels}, cases={cases}");
    }

    private static int PredictPaeth(int left, int up, int diagonal)
    {
        var prediction = left + up - diagonal;
        var distances = new[] { Math.Abs(prediction - left), Math.Abs(prediction - up), Math.Abs(prediction - diagonal) };
        var closest = distances.Min();
        return distances[0] == closest ? left : distances[1] == closest ? up : diagonal;
    }
}
