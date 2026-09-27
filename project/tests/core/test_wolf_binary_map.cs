using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the binary .mps reader against byte sequences built from the
/// published format description, not from a real WOLF game. What is proven is
/// the framing and the field order. The meaning of any single field is not,
/// because no real game is available to check it against, and this file says so
/// rather than pretending otherwise.
/// </summary>
public partial class TestWolfBinaryMap : TestBase
{
    private const string TempBase = "user://wolf_binary_map_test";

    public override void Setup() => Cleanup();

    public override void Teardown() => Cleanup();

    public void Test_AMapBuiltFromTheVerifiedLayoutDecodesEveryField()
    {
        var bytes = BuildMap(width: 2, height: 2, pixels: [123_456, 7, 0, 1_000_000]);
        var reader = new WolfBinaryMapReader(new WolfParseLimits());

        var result = reader.Read(bytes, 7, "Map007.mps");

        AssertTrue(result.Success, result.Error?.Message ?? "the verified map layout must decode");
        var map = result.Value!;
        AssertEq(map.Width, 2, "the decoded width");
        AssertEq(map.Height, 2, "the decoded height");
        AssertEq(map.MapId, 7, "the map id comes from the caller");
        AssertEq(map.Title, "Cave", "the length prefixed title");
        AssertEq(map.TilesetId, 3, "the tileset id");
        AssertEq(map.Pixels.Count, 4, "one pixel per map cell");
        AssertEq(map.Pixels[0].Raw, 123_456, "the first pixel is read verbatim");
        AssertEq(map.Pixels[0].AutotileId, 1, "raw / 100000 is the autotile id");
        AssertTrue(map.Pixels[0].HasAutotile, "autotile id 1 means an autotile is present");
        AssertEq(map.Pixels[1].HasAutotile, false, "raw 7 has no autotile");
        AssertEq(map.Pixels[3].AutotileId, 10, "raw 1000000 is autotile 10");
    }

    public void Test_TheDocumentedAutotileDigitSplitMatchesTheSpecification()
    {
        // The specification defines top left as raw % 10000 / 1000, top right as
        // raw % 1000 / 100, bottom left as raw % 100 / 10 and bottom right as
        // raw % 10. A value with every digit distinct pins all four at once.
        const int raw = 123_456;
        var bytes = BuildMap(width: 1, height: 1, pixels: [raw]);
        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(bytes, 1, "Map001.mps");

        AssertTrue(result.Success, result.Error?.Message ?? "decode");
        var pixel = result.Value!.Pixels[0];
        AssertEq(pixel.AutotileTopLeft, raw % 10_000 / 1_000, "top left digit");
        AssertEq(pixel.AutotileTopRight, raw % 1_000 / 100, "top right digit");
        AssertEq(pixel.AutotileBottomLeft, raw % 100 / 10, "bottom left digit");
        AssertEq(pixel.AutotileBottomRight, raw % 10, "bottom right digit");
    }

    public void Test_AMapWhoseFirstPixelIsAllOnesIsReportedAsNonExisting()
    {
        // The format description states a map that does not exist has a first
        // byte of 0xFFFFFFFF. That is a documented quirk and must not be
        // reported as a decode failure.
        var bytes = BuildMap(width: 1, height: 1, pixels: [unchecked((int)0xFFFFFFFF)]);
        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(bytes, 3, "Map003.mps");

        AssertTrue(result.Success, $"a map that does not exist still decodes as a frame: {result.Error?.Message}");
        AssertEq(result.Value!.MapExists, false, "the first pixel marks the map as not existing");
        AssertEq(result.Value!.Pixels.Count, 0, "a non existing map has no pixel data");
    }

    public void Test_TheVerifiedEventFramingIsDecodedAndChecked()
    {
        var bytes = BuildMap(width: 1, height: 1, pixels: [1],
            events: [BuildEvent(id: 5, x: 3, y: 4, pageCount: 1)]);
        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(bytes, 9, "Map009.mps");

        AssertTrue(result.Success, result.Error?.Message ?? "the event framing must decode");
        var map = result.Value!;
        AssertEq(map.Events.Count, 1, "one event");
        AssertEq(map.Events[0].EventId, 5, "the event id");
        AssertEq(map.Events[0].Title, "Guard", "the event title");
        AssertEq(map.Events[0].MapX, 3, "the event map x");
        AssertEq(map.Events[0].MapY, 4, "the event map y");
        AssertEq(map.Events[0].Pages.Count, 1, "one page");
    }

    public void Test_AForeignMagicIsRefusedInsteadOfDecoded()
    {
        var bytes = BuildMap(width: 1, height: 1, pixels: [1]);
        bytes[10] = (byte)'X';

        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(bytes, 1, "Map001.mps");

        AssertTrue(!result.Success, "a file without the WOLFM header must be refused");
        AssertTrue((result.Error?.Message ?? "").Contains("WOLFM", StringComparison.Ordinal),
            $"the diagnostic names the missing header, but it said: {result.Error?.Message}");
    }

    public void Test_AWrongEventSignatureIsRefusedInsteadOfSkipped()
    {
        var bytes = BuildMap(width: 1, height: 1, pixels: [1],
            events: [BuildEvent(id: 1, x: 0, y: 0, pageCount: 0, signatureOverride: 0x1234)]);

        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(bytes, 1, "Map001.mps");

        AssertTrue(!result.Success, "a wrong event signature must be refused, not skipped");
        AssertTrue((result.Error?.Message ?? "").Contains("0x3039", StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the expected signature, but it said: {result.Error?.Message}");
    }

    public void Test_AMissingFooterIsRefusedInsteadOfAccepted()
    {
        var bytes = BuildMap(width: 1, height: 1, pixels: [1]);
        bytes[^1] = 0x00;

        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(bytes, 1, "Map001.mps");

        AssertTrue(!result.Success, "a map without its 0x66 footer must be refused");
        AssertTrue((result.Error?.Message ?? "").Contains("0x66", StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the footer, but it said: {result.Error?.Message}");
    }

    public void Test_AnOutOfRangeDimensionIsRefusedBeforeAnythingIsAllocated()
    {
        var bytes = BuildMap(width: 1, height: 1, pixels: [1]);
        var cursor = 10 + 5 + 1 + 1 + 3 + 4 + 1;
        cursor += 4;            // title length
        cursor += 4;            // tileset
        cursor += 4;            // width
        var index = 10 + 5 + 1 + 1 + 3 + 4 + 1 + 4 + 4 + 4;
        WriteUInt32(bytes, index, 100_000);

        var limits = new WolfParseLimits { MaxMapDimension = 500 };
        var result = new WolfBinaryMapReader(limits).Read(bytes, 1, "Map001.mps");

        AssertTrue(!result.Success, "a map wider than the limit must be refused");
        AssertTrue((result.Error?.Message ?? "").Contains("dimensions", StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the dimension problem, but it said: {result.Error?.Message}");
    }

    public void Test_ATruncatedFileIsRefusedWithAByteOffset()
    {
        var bytes = BuildMap(width: 4, height: 4, pixels: new int[16]);
        var truncated = new byte[bytes.Length - 20];
        Array.Copy(bytes, truncated, truncated.Length);

        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(truncated, 1, "Map001.mps");

        AssertTrue(!result.Success, "a truncated map must be refused");
        AssertTrue((result.Error?.Message ?? "").Contains("ends at byte", StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the offset, but it said: {result.Error?.Message}");
    }

    public void Test_AnUnknownVersionIsRefusedRatherThanGuessed()
    {
        var bytes = BuildMap(width: 1, height: 1, pixels: [1]);
        bytes[16] = 0x07; // the version header byte

        var result = new WolfBinaryMapReader(new WolfParseLimits())
            .Read(bytes, 1, "Map001.mps");

        AssertTrue(!result.Success, "an unknown version header must be refused");
        AssertTrue((result.Error?.Message ?? "").Contains("version header", StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the version header, but it said: {result.Error?.Message}");
    }

    // ---- fixture construction, straight from the format description ----

    private static byte[] BuildMap(int width, int height, int[] pixels, byte[][]? events = null)
    {
        var body = new List<byte>();
        body.AddRange(new byte[10]);
        body.AddRange(Encoding.ASCII.GetBytes("WOLFM"));
        body.Add(0x00);
        body.Add(0x00);       // version header, v2
        body.AddRange(new byte[3]);
        AddUInt32(body, 0x64);
        body.Add(0x65);       // version, v2
        AddString(body, "Cave");
        AddUInt32(body, 3);   // tileset
        AddUInt32(body, (uint)width);
        AddUInt32(body, (uint)height);
        AddUInt32(body, (uint)(events?.Length ?? 0));

        var first = pixels.Length > 0 ? pixels[0] : 0;
        AddUInt32(body, (uint)first);
        // The format description skips width * height * 12 - 4 bytes only when
        // the first pixel is not 0xFFFFFFFF. A map that does not exist carries
        // no body at all, so writing one here would be a fixture that cannot
        // exist in a real file.
        if (first != unchecked((int)0xFFFFFFFF))
        {
            for (var index = 1; index < pixels.Length; index++)
            {
                AddUInt32(body, (uint)pixels[index]);
            }
            // The two base tile values per pixel.
            for (var index = 0; index < pixels.Length; index++)
            {
                AddUInt32(body, 0);
                AddUInt32(body, 0);
            }
        }

        if (events != null)
        {
            foreach (var evt in events)
            {
                body.AddRange(evt);
            }
        }
        body.Add(0x66);
        return body.ToArray();
    }

    private static byte[] BuildEvent(int id, int x, int y, int pageCount, uint? signatureOverride = null)
    {
        var body = new List<byte>();
        body.Add(0x6F);
        AddUInt32(body, signatureOverride ?? 0x3039);
        AddUInt32(body, (uint)id);
        AddString(body, "Guard");
        AddUInt32(body, (uint)x);
        AddUInt32(body, (uint)y);
        AddUInt32(body, (uint)pageCount);
        AddUInt32(body, 0);   // the verified zero separator
        for (var page = 0; page < pageCount; page++)
        {
            body.AddRange(BuildPage());
        }
        body.Add(0x70);
        return body.ToArray();
    }

    private static byte[] BuildPage()
    {
        var body = new List<byte>();
        body.AddRange([0x79, 0xFF, 0xFF, 0xFF, 0xFF]);
        AddString(body, "Hero");
        body.Add(3);   // icon row byte, the reference uses (byte >> 1) - 1
        body.Add(0);   // icon column
        body.Add(4);   // icon opacity
        body.Add(0);   // icon blend
        body.Add(3);   // event trigger
        for (var condition = 0; condition < 4; condition++)
        {
            body.Add(0);
            AddUInt32(body, 0);
            AddUInt32(body, 0);
        }
        body.Add(4);   // move speed
        body.Add(3);   // move frequency
        body.Add(0);   // move route
        return body.ToArray();
    }

    private static void AddString(List<byte> pBody, string pText)
    {
        var bytes = Encoding.GetEncoding("Shift-JIS").GetBytes(pText);
        AddUInt32(pBody, (uint)bytes.Length);
        pBody.AddRange(bytes);
    }

    private static void AddUInt32(List<byte> pBody, uint pValue)
    {
        pBody.Add((byte)(pValue & 0xFF));
        pBody.Add((byte)((pValue >> 8) & 0xFF));
        pBody.Add((byte)((pValue >> 16) & 0xFF));
        pBody.Add((byte)((pValue >> 24) & 0xFF));
    }

    private static void WriteUInt32(byte[] pBytes, int pIndex, uint pValue)
    {
        pBytes[pIndex] = (byte)(pValue & 0xFF);
        pBytes[pIndex + 1] = (byte)((pValue >> 8) & 0xFF);
        pBytes[pIndex + 2] = (byte)((pValue >> 16) & 0xFF);
        pBytes[pIndex + 3] = (byte)((pValue >> 24) & 0xFF);
    }

    private static void Cleanup()
    {
        var directory = ProjectSettings.GlobalizePath(TempBase);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}
