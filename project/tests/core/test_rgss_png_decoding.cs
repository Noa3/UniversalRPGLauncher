using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Godot;
using UniversalRPG.Rgss;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Tests.Core;

public partial class TestRgssBitmapFiles
{
    public void Test_RgbAndRgbaFiltersMatchNativePngDecoding()
    {
        foreach (var channels in new[] { 3, 4 })
        foreach (var filter in new[] { 0, 1, 2, 3, 4 })
        {
            var raw = Enumerable.Range(0, 3 * 2 * channels).Select(i => (byte)(i * 19 + 7)).ToArray();
            var rows = new byte[2 * (3 * channels + 1)];
            for (var y = 0; y < 2; y++)
            {
                rows[y * (3 * channels + 1)] = (byte)filter;
                for (var x = 0; x < 3 * channels; x++)
                {
                    var index = y * 3 * channels + x;
                    int a = x >= channels ? raw[index - channels] : 0;
                    int b = y > 0 ? raw[index - 3 * channels] : 0;
                    int c = y > 0 && x >= channels ? raw[index - 4 * channels] : 0;
                    var prediction = filter switch
                    {
                        0 => 0, 1 => a, 2 => b, 3 => (a + b) / 2,
                        _ => PngPaeth(a, b, c),
                    };
                    rows[y * (3 * channels + 1) + 1 + x] = unchecked((byte)(raw[index] - prediction));
                }
            }
            var png = Png(3, 2, channels == 4 ? 6 : 2, 8, rows);
            var parsed = new RgssPngImage(png, CancellationToken.None);
            var pixels = new Rm2kPixelBuffer(3, 2);
            parsed.DecodeInto(pixels, CancellationToken.None);
            using var native = new Image();
            AssertEq(native.LoadPngFromBuffer(png), Error.Ok);
            native.Convert(Image.Format.Rgba8);
            AssertTrue(pixels.Pixels.SequenceEqual(native.GetData()), $"Native PNG oracle: channels={channels}, filter={filter}");
            for (var i = 0; i < 6; i++)
                AssertEq(pixels.Pixels[i * 4], raw[i * channels], "RGB expansion must not overwrite later source pixels.");
        }
    }

    public void Test_IndexedDepthsAndTransparencyMatchNativePixelsWithoutRm2kColorKey()
    {
        foreach (var depth in new[] { 1, 2, 4, 8 })
        foreach (var transparent in new[] { false, true })
        {
            byte packed = depth switch { 1 => 64, 2 => 16, 4 => 1, _ => 0 };
            var rows = depth == 8 ? new byte[] { 0, 0, 1 } : new byte[] { 0, packed };
            var png = Png(2, 1, 3, depth, rows, new byte[] { 19, 23, 29, 31, 37, 41 },
                transparent ? new byte[] { 255, 87 } : null);
            var pixels = new Rm2kPixelBuffer(2, 1);
            new RgssPngImage(png, CancellationToken.None).DecodeInto(pixels, CancellationToken.None);
            AssertEq(pixels.Pixels[3], (byte)255, "Palette index zero is opaque unless tRNS says otherwise.");
            AssertEq(pixels.Pixels[7], transparent ? (byte)87 : (byte)255);
            using var native = new Image();
            AssertEq(native.LoadPngFromBuffer(png), Error.Ok);
            native.Convert(Image.Format.Rgba8);
            AssertTrue(pixels.Pixels.SequenceEqual(native.GetData()));
        }
        var keyed = Png(2, 1, 2, 8, new byte[] { 0, 19, 23, 29, 31, 37, 41 },
            null, new byte[] { 0, 19, 0, 23, 0, 29 });
        var target = new Rm2kPixelBuffer(2, 1);
        new RgssPngImage(keyed, CancellationToken.None).DecodeInto(target, CancellationToken.None);
        AssertEq(target.Pixels[3], (byte)0);
        AssertEq(target.Pixels[7], (byte)255);
    }

    public void Test_RejectedPngsNeverRetainPartiallyDecodedSurfaces()
    {
        var valid = Png(1, 1, 6, 8, new byte[] { 0, 1, 2, 3, 4 });
        var crc = (byte[])valid.Clone(); crc[29] ^= 1;
        var ending = valid[..^12];
        var trailing = valid.Concat(new byte[] { 0 }).ToArray();
        var cases = new[]
        {
            crc, ending, trailing,
            Png(4097, 1, 6, 8, new byte[] { 0, 1, 2, 3, 4 }),
            Png(1, 1, 6, 8, new byte[] { 5, 1, 2, 3, 4 }),
            Png(1, 1, 6, 8, new byte[] { 0, 1 }),
            Png(1, 1, 6, 8, new byte[4096]),
            Png(1, 1, 3, 8, new byte[] { 0, 2 }, new byte[] { 1, 2, 3 }),
            Png(1, 1, 0, 8, new byte[] { 0, 1 }),
            Png(1, 1, 6, 16, new byte[9]),
            Png(1, 1, 6, 8, new byte[] { 0, 1, 2, 3, 4 }, interlace: 1),
            Png(1, 1, 6, 8, new byte[] { 0, 1, 2, 3, 4 }, truncateZlib: true),
        };
        var graphics = new RgssGrafikHost { Wurzel = _root };
        for (var index = 0; index < cases.Length; index++)
        {
            var name = "broken-" + index + ".png";
            File.WriteAllBytes(Path.Combine(_root, name), cases[index]);
            RubyRuntimeException? failure = null;
            try { Load(graphics, name); }
            catch (RubyRuntimeException error) { failure = error; }
            AssertTrue(failure != null && failure.Message.Contains(name), name + " must fail by name.");
            AssertEq(graphics.Bitmaps.Count, 0);
            AssertEq(graphics.Bitmaps.AllocatedBytes, 0L);
        }
    }

    public void Test_LoaderRejectsUnsafePathsAndHonorsCancellationAndMemoryBounds()
    {
        var graphics = new RgssGrafikHost { Wurzel = _root };
        foreach (var path in new[] { "../known", "/known", "C:/known", "Graphics/../known", "Graphics/Pictures/known.png:stream", "known\0" })
        {
            RubyRuntimeException? failure = null;
            try { Load(graphics, path); }
            catch (RubyRuntimeException error) { failure = error; }
            AssertTrue(failure != null, "Unsafe path must be refused: " + path.Replace('\0', '?'));
            AssertEq(graphics.Bitmaps.Count, 0);
        }
        using (var oversized = File.Create(Path.Combine(_root, "oversized.png")))
            oversized.SetLength((long)RgssPngImage.MaximumFileBytes + 1);
        RubyRuntimeException? tooLarge = null;
        try { Load(graphics, "oversized.png"); }
        catch (RubyRuntimeException error) { tooLarge = error; }
        AssertTrue(tooLarge != null);
        graphics.Bitmaps.Create(4096, 4096);
        RubyRuntimeException? budget = null;
        try { Load(graphics, "Graphics/Pictures/known"); }
        catch (RubyRuntimeException error) { budget = error; }
        AssertTrue(budget != null && budget.Class == "RuntimeError");
        AssertEq(graphics.Bitmaps.Count, 1);
        AssertEq(graphics.Bitmaps.AllocatedBytes, RgssBitmapStore.MaximumBytes);
        graphics.Bitmaps.ReleaseAll();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        graphics.ExecutionCancellation = cancellation.Token;
        var cancelled = false;
        try { Load(graphics, "Graphics/Pictures/known"); }
        catch (OperationCanceledException) { cancelled = true; }
        AssertTrue(cancelled);
        AssertEq(graphics.Bitmaps.Count, 0);
    }

    public void Test_ExplicitExtensionBackslashesAndCaseInsensitiveLookup()
    {
        var graphics = new RgssGrafikHost { Wurzel = _root };
        var first = Load(graphics, "Graphics\\Pictures\\known.png");
        var second = Load(graphics, "graphics/pictures/KNOWN.PNG");
        AssertEq(first.Items[1].Integer, 2);
        AssertEq(second.Items[2].Integer, 2);
        AssertEq(graphics.Bitmaps.GetPixel(first.Items[0].Integer, 1, 0).Items[3].Integer, 128);
        graphics.Bitmaps.ReleaseAll();
    }

    private static RubyValue Load(RgssGrafikHost graphics, string path) => graphics.CallMethod(RubyValue.Nil,
        "rgss_bitmap", new[] { RubyValue.OfSymbol("load"), RubyValue.OfBytes(Encoding.UTF8.GetBytes(path)) })!;

    private static int PngPaeth(int a, int b, int c)
    {
        var prediction = a + b - c;
        var distances = new[] { Math.Abs(prediction - a), Math.Abs(prediction - b), Math.Abs(prediction - c) };
        var minimum = distances.Min();
        return distances[0] == minimum ? a : distances[1] == minimum ? b : c;
    }

    internal static byte[] Png(int width, int height, int type, int depth, byte[] rows,
        byte[]? palette = null, byte[]? transparency = null, byte interlace = 0, bool truncateZlib = false)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        void Chunk(string name, byte[] body)
        {
            var kind = Encoding.ASCII.GetBytes(name);
            Span<byte> value = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(value, body.Length); output.Write(value);
            output.Write(kind); output.Write(body);
            uint crc = uint.MaxValue;
            foreach (var item in kind.Concat(body))
            {
                crc ^= item;
                for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320u : crc >> 1;
            }
            BinaryPrimitives.WriteUInt32BigEndian(value, ~crc); output.Write(value);
        }
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = (byte)depth; header[9] = (byte)type; header[12] = interlace;
        Chunk("IHDR", header);
        if (palette != null) Chunk("PLTE", palette);
        if (transparency != null) Chunk("tRNS", transparency);
        using var compressed = new MemoryStream();
        using (var deflate = new ZLibStream(compressed, CompressionLevel.SmallestSize, true)) deflate.Write(rows);
        var data = compressed.ToArray();
        if (truncateZlib) data = data[..^4];
        // Legal consecutive IDAT chunks exercise stream joining independently of row boundaries.
        Chunk("IDAT", data[..(data.Length / 2)]); Chunk("IDAT", data[(data.Length / 2)..]);
        Chunk("IEND", Array.Empty<byte>());
        return output.ToArray();
    }
}
