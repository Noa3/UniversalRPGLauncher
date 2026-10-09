using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kEventPagePoseParser : TestBase
{
    public void Test_PagePoseFieldsUseThePinnedLmuIdsAndDefaults()
    {
        // liblcf6854310c: 0x17 direction, 0x18 pattern, 0x19 translucent,
        // 0x24 animation type. The translucent value must not become a pose.
        var explicitPage = TestRm2kParser.Struct(
            TestRm2kParser.Chunk(0x1F, TestRm2kParser.Ber(6)),
            TestRm2kParser.Chunk(0x20, TestRm2kParser.Ber(8)),
            TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(3)),
            TestRm2kParser.Chunk(0x18, TestRm2kParser.Ber(2)),
            TestRm2kParser.Chunk(0x19, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x24, TestRm2kParser.Ber(4)),
            TestRm2kParser.Chunk(0x25, TestRm2kParser.Ber(6)));
        var defaultPage = TestRm2kParser.Struct();
        var path = ProjectSettings.GlobalizePath("user://page-pose-" + Guid.NewGuid().ToString("N") + ".lmu");
        try
        {
            File.WriteAllBytes(path, MapWithPages(explicitPage, defaultPage));
            var parsed = new Rm2kParser().ParseMap(path);
            AssertTrue(parsed.IsSuccess(), $"The spec-derived LMU page fixture parses: {parsed.Error?.Message}");
            if (!parsed.IsSuccess()) return;
            var events = parsed.Data["events"].AsGodotArray();
            var pages = events[0].AsGodotDictionary()["pages"].AsGodotArray();
            AssertEq(pages.Count, 2);
            var authored = pages[0].AsGodotDictionary();
            AssertTrue(authored.ContainsKey("move_type"), "Page movement mode must survive parsing");
            if (authored.ContainsKey("move_type")) AssertEq(authored["move_type"].AsInt32(), 6);
            AssertEq(authored["move_frequency"].AsInt32(), 8);
            AssertEq(authored["character_direction"].AsInt32(), 3);
            AssertTrue(authored.ContainsKey("character_pattern"), "Page pattern must survive parsing");
            AssertTrue(authored.ContainsKey("animation_type"), "Page animation type must survive parsing");
            AssertTrue(authored.ContainsKey("move_speed"), "LMU0x25 move speed must survive parsing for animation timing");
            if (authored.ContainsKey("move_speed")) AssertEq(authored["move_speed"].AsInt32(), 6);
            if (authored.ContainsKey("character_pattern")) AssertEq(authored["character_pattern"].AsInt32(), 2,
                "0x18 pattern is not the 0x19 translucent flag");
            if (authored.ContainsKey("animation_type")) AssertEq(authored["animation_type"].AsInt32(), 4);
            var defaults = pages[1].AsGodotDictionary();
            AssertTrue(defaults.ContainsKey("move_type"), "Missing mode still publishes the format default");
            if (defaults.ContainsKey("move_type")) AssertEq(defaults["move_type"].AsInt32(), 1,
                "Pinned liblcf defaults to random, not stationary");
            AssertEq(defaults["move_frequency"].AsInt32(), 3, "Absent frequency defaults to3, not0");
            AssertEq(defaults["character_direction"].AsInt32(), 2, "Absent direction uses liblcf down, not up");
            if (defaults.ContainsKey("character_pattern")) AssertEq(defaults["character_pattern"].AsInt32(), 1);
            if (defaults.ContainsKey("animation_type")) AssertEq(defaults["animation_type"].AsInt32(), 0);
            if (defaults.ContainsKey("move_speed")) AssertEq(defaults["move_speed"].AsInt32(), 3);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    internal static byte[] MapWithPages(params byte[][] pages)
    {
        var mapEvent = TestRm2kParser.Struct(
            TestRm2kParser.Chunk(0x02, TestRm2kParser.Ber(8)),
            TestRm2kParser.Chunk(0x03, TestRm2kParser.Ber(8)),
            TestRm2kParser.Chunk(0x05, TestRm2kParser.StructArray(pages)));
        var bytes = new List<byte>();
        var header = Encoding.ASCII.GetBytes("LcfMapUnit");
        bytes.AddRange(TestRm2kParser.Ber(header.Length));
        bytes.AddRange(header);
        bytes.AddRange(TestRm2kParser.Chunk(0x02, TestRm2kParser.Ber(20)));
        bytes.AddRange(TestRm2kParser.Chunk(0x03, TestRm2kParser.Ber(15)));
        var lower = new byte[20 * 15 * 2];
        var upper = new byte[lower.Length];
        for (var index = 0; index < lower.Length; index += 2)
        {
            lower[index] = (byte)(5000 & 255); lower[index + 1] = (byte)(5000 >> 8);
            upper[index] = (byte)(10000 & 255); upper[index + 1] = (byte)(10000 >> 8);
        }
        bytes.AddRange(TestRm2kParser.Chunk(0x47, lower));
        bytes.AddRange(TestRm2kParser.Chunk(0x48, upper));
        bytes.AddRange(TestRm2kParser.Chunk(0x51, TestRm2kParser.StructArray(mapEvent)));
        bytes.Add(0);
        return bytes.ToArray();
    }
}
