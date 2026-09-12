using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Regression fixtures enter through actual LMU file bytes and ParseMap,
/// rather than bypassing the loader with hand-built runtime event objects.
/// All fixture data is synthetic and contains no third-party game assets.
/// </summary>
public sealed class TestRm2kLmuEventPages : TestBase
{
    public void Test_ActualLmuRetainsPagesInsteadOfOnlyTheirCount()
    {
        var command = Command(10110, 2, "Hello", 7, 128);
        var result = Parse(Map((1, Struct(Chunk(0x34, Bytes(command, EndCommands))))));
        AssertTrue(result.Success, result.Error?.Describe() ?? "");
        var pages = Pages(result);
        AssertEq(pages.Count, 1);
        var page = pages[0].AsGodotDictionary();
        AssertEq((int)page["id"], 1);
        var commands = page["commands"].AsGodotArray();
        AssertEq(commands.Count, 1);
        var decoded = commands[0].AsGodotDictionary();
        AssertEq((int)decoded["code"], 10110);
        AssertEq((int)decoded["indent"], 2);
        AssertEq(decoded["text"].AsString(), "Hello");
        AssertEq(string.Join(",", decoded["parameters"].AsInt32Array()), "7,128");
    }

    public void Test_PageIdentityAndStoredOrderSurviveParsing()
    {
        var result = Parse(Map((3, Struct(Chunk(0x21, Ber(0)))), (8, Struct(Chunk(0x21, Ber(4))))));
        AssertTrue(result.Success, result.Error?.Describe() ?? "");
        var pages = Pages(result);
        AssertEq(pages.Count, 2);
        AssertEq((int)pages[0].AsGodotDictionary()["id"], 3);
        AssertEq((int)pages[1].AsGodotDictionary()["id"], 8);
        AssertEq((int)pages[1].AsGodotDictionary()["trigger"], 4);
    }

    public void Test_NestedSwitchConditionsAreDecodedFromChunkBytes()
    {
        var nested = Struct(Chunk(0x01, new byte[] { 3 }), Chunk(0x02, Ber(7)), Chunk(0x03, Ber(9)));
        var page = FirstPage(Parse(Map((1, Struct(Chunk(0x02, nested))))));
        var condition = page["conditions"].AsGodotDictionary();
        AssertTrue(condition["switch_a_enabled"].AsBool());
        AssertTrue(condition["switch_b_enabled"].AsBool());
        AssertFalse(condition["variable_enabled"].AsBool());
        AssertEq((int)condition["switch_a_id"], 7);
        AssertEq((int)condition["switch_b_id"], 9);
    }

    public void Test_NegativeVariableThresholdIsSignedInt32()
    {
        var nested = Struct(Chunk(0x01, new byte[] { 4 }), Chunk(0x04, Ber(2)), Chunk(0x05, Ber(-123)));
        var page = FirstPage(Parse(Map((1, Struct(Chunk(0x02, nested))))));
        var condition = page["conditions"].AsGodotDictionary();
        AssertTrue(condition["variable_enabled"].AsBool());
        AssertEq((int)condition["variable_value"], -123);
    }

    public void Test_ExplicitEmptyConditionIntegerMeansZeroNotAbsentDefault()
    {
        var nested = Struct(Chunk(0x02, Array.Empty<byte>()));
        var page = FirstPage(Parse(Map((1, Struct(Chunk(0x02, nested))))));
        var condition = page["conditions"].AsGodotDictionary();
        AssertEq((int)condition["switch_a_id"], 0);
        AssertEq((int)condition["switch_b_id"], 1);
    }

    public void Test_AbsentPageFieldsUseVerifiedLcfDefaults()
    {
        var page = FirstPage(Parse(Map((1, Struct()))));
        AssertEq((int)page["trigger"], 0);
        AssertEq((int)page["priority"], 0);
        AssertEq((int)page["move_type"], 1);
        AssertEq((int)page["move_frequency"], 3);
        AssertEq((int)page["move_speed"], 3);
        AssertEq((int)page["character_direction"], 2);
        AssertEq((int)page["character_pattern"], 1);
        AssertEq(page["character_name"].AsString(), "");
        AssertFalse(page["has_command_list"].AsBool());
        AssertFalse(page["has_move_list"].AsBool());
        AssertEq(page["commands"].AsGodotArray().Count, 0);
        var route = page["move_route"].AsGodotDictionary();
        AssertTrue(route["repeat"].AsBool());
        AssertFalse(route["skippable"].AsBool());
    }

    public void Test_EmptyConditionStructureAndPayloadAreBothSupported()
    {
        foreach (var data in new[] { Array.Empty<byte>(), new byte[] { 0 } })
        {
            var page = FirstPage(Parse(Map((1, Struct(Chunk(0x02, data))))));
            var condition = page["conditions"].AsGodotDictionary();
            AssertFalse(condition["switch_a_enabled"].AsBool());
            AssertEq((int)condition["switch_a_id"], 1);
        }
    }

    public void Test_GraphicAndMovementMetadataAreRetained()
    {
        var page = FirstPage(Parse(Map((1, Struct(
            Chunk(0x15, Encoding.ASCII.GetBytes("Guard")), Chunk(0x16, Ber(4)),
            Chunk(0x17, Ber(3)), Chunk(0x18, Ber(0)), Chunk(0x19, Ber(1)),
            Chunk(0x1F, Ber(6)), Chunk(0x20, Ber(8)), Chunk(0x22, Ber(1)),
            Chunk(0x24, Ber(3)), Chunk(0x25, Ber(5)))))));
        AssertEq(page["character_name"].AsString(), "Guard");
        AssertEq((int)page["character_index"], 4);
        AssertEq((int)page["character_direction"], 3, "raw LMU direction is not a numpad direction");
        AssertEq((int)page["translucent"], 1);
        AssertEq((int)page["move_type"], 6);
        AssertEq((int)page["move_frequency"], 8);
        AssertEq((int)page["move_speed"], 5);
    }

    public void Test_MoveRouteSizeFieldIsBytesNotInstructionCount()
    {
        // One SwitchOn instruction occupies three bytes: opcode 32, BER 128.
        var stream = Bytes(Ber(32), Ber(128));
        var route = Struct(Chunk(0x0B, Ber(stream.Length)), Chunk(0x0C, stream),
            Chunk(0x15, Ber(0)), Chunk(0x16, Ber(1)));
        var page = FirstPage(Parse(Map((1, Struct(Chunk(0x29, route))))));
        AssertTrue(page["has_move_list"].AsBool());
        var decoded = page["move_route"].AsGodotDictionary();
        AssertFalse(decoded["repeat"].AsBool());
        AssertTrue(decoded["skippable"].AsBool());
        var commands = decoded["commands"].AsGodotArray();
        AssertEq(commands.Count, 1);
        AssertEq((int)commands[0].AsGodotDictionary()["code"], 32);
        AssertEq((int)commands[0].AsGodotDictionary()["parameter_a"], 128);
    }

    public void Test_AdvisorySizesNeverDriveAllocationOrIteration()
    {
        var route = Struct(Chunk(0x0B, Ber(int.MaxValue)), Chunk(0x0C, Ber(1)));
        var page = FirstPage(Parse(Map((1, Struct(
            Chunk(0x29, route), Chunk(0x33, Ber(int.MaxValue)), Chunk(0x34, EndCommands))))));
        AssertEq(page["commands"].AsGodotArray().Count, 0);
        AssertEq(page["move_route"].AsGodotDictionary()["commands"].AsGodotArray().Count, 1);
    }

    public void Test_CommandOperandsRetainTheSignedInt32Range()
    {
        var command = Command(10220, 0, "", int.MinValue, -1, 0, int.MaxValue);
        var page = FirstPage(Parse(Map((1, Struct(Chunk(0x34, Bytes(command, EndCommands)))))));
        var values = page["commands"].AsGodotArray()[0].AsGodotDictionary()["parameters"].AsInt32Array();
        AssertEq(values[0], int.MinValue);
        AssertEq(values[1], -1);
        AssertEq(values[2], 0);
        AssertEq(values[3], int.MaxValue);
    }

    public void Test_MalformedNestedConditionsFailWithoutDiscardingThePage()
    {
        var invalid = new[]
        {
            Chunk(0x02, Ber(7)), // missing structure terminator
            new byte[] { 0, 99 }, // trailing data after terminator
            Struct(Chunk(0x02, new byte[] { 0x80 })), // truncated scalar
            Struct(Chunk(0x02, Ber(1)), Chunk(0x02, Ber(2))), // duplicate field
        };
        foreach (var data in invalid)
        {
            var result = Parse(Map((1, Struct(Chunk(0x02, data)))));
            AssertFalse(result.Success);
            AssertTrue(result.Error != null && result.Error.Message.Contains("condition", StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Test_MalformedCanonicalFieldDoesNotFallBackToGuessedAlias()
    {
        var result = Parse(Map((1, Struct(Chunk(0x21, new byte[] { 0x80 }), Chunk(0x09, Ber(0))))));
        AssertFalse(result.Success);
        AssertTrue(result.Error != null && result.Error.Message.Contains("trigger", StringComparison.OrdinalIgnoreCase));
    }

    public void Test_UnknownPatchFieldIsDataNotACommandListAlias()
    {
        var page = FirstPage(Parse(Map((1, Struct(Chunk(0x0B, new byte[] { 0xFF }))))));
        AssertFalse(page["has_command_list"].AsBool());
        AssertEq(page["commands"].AsGodotArray().Count, 0);
        var unknown = page["unknown_fields"].AsGodotArray();
        AssertEq(unknown.Count, 1);
        AssertEq((int)unknown[0].AsGodotDictionary()["id"], 0x0B);
        AssertEq(unknown[0].AsGodotDictionary()["data"].AsByteArray()[0], (byte)0xFF);
    }

    public void Test_DuplicateOrNonpositivePageIdsAreRefused()
    {
        foreach (var map in new[] { Map((1, Struct()), (1, Struct())), Map((0, Struct())) })
            AssertFalse(Parse(map).Success);
    }

    public void Test_DuplicateCanonicalPageFieldsAreRefused()
    {
        AssertFalse(Parse(Map((1, Struct(Chunk(0x21, Ber(0)), Chunk(0x21, Ber(4)))))).Success);
    }

    public void Test_MalformedCommandOrRoutePayloadsAreNotReportedAsSuccess()
    {
        AssertFalse(Parse(Map((1, Struct(Chunk(0x34, new byte[] { 1, 0 }))))).Success);
        AssertFalse(Parse(Map((1, Struct(Chunk(0x29, Struct(Chunk(0x0C, Ber(99)))))))).Success);
    }

    public void Test_EmptyPageArrayIsValidAndContainsNoInventedPage()
    {
        var result = Parse(Map());
        AssertTrue(result.Success, result.Error?.Describe() ?? "");
        AssertEq(Pages(result).Count, 0);
    }

    public void Test_CommandLimitIncludesItsFollowingTerminator()
    {
        var single = Command(10110, 0, "");
        var stream = new List<byte>();
        for (var i = 0; i < Rm2kEventCommandDecoder.MaxCommands; i++) stream.AddRange(single);
        var atLimit = Rm2kEventCommandDecoder.Decode(Bytes(stream.ToArray(), EndCommands));
        AssertTrue(atLimit.Success, atLimit.Error?.Describe() ?? "");
        AssertEq((int)atLimit.Data["count"], Rm2kEventCommandDecoder.MaxCommands);
        stream.AddRange(single);
        AssertFalse(Rm2kEventCommandDecoder.Decode(Bytes(stream.ToArray(), EndCommands)).Success);
    }

    private Godot.Collections.Dictionary FirstPage(Rm2kParser.ParseResult result)
    {
        AssertTrue(result.Success, result.Error?.Describe() ?? "");
        var pages = Pages(result);
        AssertEq(pages.Count, 1);
        return pages[0].AsGodotDictionary();
    }

    private static Godot.Collections.Array Pages(Rm2kParser.ParseResult result)
    {
        var e = result.Data["events"].AsGodotArray()[0].AsGodotDictionary();
        var pages = e["pages"].AsGodotArray();
        if ((int)e["page_count"] != pages.Count) throw new InvalidOperationException("Page count and materialized data disagree.");
        return pages;
    }

    private static Rm2kParser.ParseResult Parse(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), "urpg-lmu-" + Guid.NewGuid().ToString("N") + ".lmu");
        try
        {
            File.WriteAllBytes(path, bytes);
            using var parser = new Rm2kParser();
            return parser.ParseMap(path);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static readonly byte[] EndCommands = { 0, 0, 0, 0 };

    private static byte[] Map(params (int Id, byte[] Data)[] pages)
    {
        var eventData = Struct(Chunk(0x01, Encoding.ASCII.GetBytes("Fixture")),
            Chunk(0x02, Ber(1)), Chunk(0x03, Ber(1)), Chunk(0x05, StructArray(pages)));
        var header = Encoding.ASCII.GetBytes("LcfMapUnit");
        return Bytes(Ber(header.Length), header, Chunk(0x02, Ber(3)), Chunk(0x03, Ber(3)),
            Chunk(0x51, StructArray((1, eventData))), new byte[] { 0 });
    }

    private static byte[] StructArray(params (int Id, byte[] Data)[] entries)
    {
        var data = new List<byte>(Ber(entries.Length));
        foreach (var entry in entries) { data.AddRange(Ber(entry.Id)); data.AddRange(entry.Data); }
        return data.ToArray();
    }

    private static byte[] Command(int code, int indent, string text, params int[] parameters)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var data = new List<byte>(Bytes(Ber(code), Ber(indent), Ber(bytes.Length), bytes, Ber(parameters.Length)));
        foreach (var value in parameters) data.AddRange(Ber(value));
        return data.ToArray();
    }

    private static byte[] Struct(params byte[][] fields) => Bytes(fields.Concat(new[] { new byte[] { 0 } }).ToArray());
    private static byte[] Chunk(int id, byte[] data) => Bytes(Ber(id), Ber(data.Length), data);
    private static byte[] Bytes(params byte[][] parts) => parts.SelectMany(part => part).ToArray();

    private static byte[] Ber(int value)
    {
        var unsigned = unchecked((uint)value);
        var data = new List<byte> { (byte)(unsigned & 0x7F) };
        while ((unsigned >>= 7) != 0) data.Add((byte)(unsigned & 0x7F));
        data.Reverse();
        for (var i = 0; i < data.Count - 1; i++) data[i] |= 0x80;
        return data.ToArray();
    }
}
