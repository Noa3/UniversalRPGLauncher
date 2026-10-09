using Godot;
using UniversalRPG.Rm2k.Parser;

namespace UniversalRPG.Tests.Core;

public partial class TestRm2kMoveRouteDecoder
{
    public void Test_NativePayloadDoesNotRequireAdvisorySizeField()
    {
        var result = Rm2kMoveRouteDecoder.Decode(Fields(null, new byte[] { 1, 29 }));
        AssertTrue(result.Success);
        if (result.Success) AssertEq(result.Data["command_count"].AsInt32(), 2);
    }

    public void Test_EmptyNativeRouteStillRetainsItsExplicitFlags()
    {
        var result = Rm2kMoveRouteDecoder.Decode(Fields(null, null, pRepeat: false, pSkippable: true));
        AssertTrue(result.Success);
        AssertEq(result.Data["command_count"].AsInt32(), 0);
        AssertFalse(result.Data["repeat"].AsBool());
        AssertTrue(result.Data["skippable"].AsBool());
    }

    public void Test_NativeInt32ParameterKeepsTheWriterBitPattern()
    {
        // writer_lcf.cpp WriteInt casts int32 to uint32 before BER encoding.
        var bytes = new byte[] { 34, 0, 0x8F, 0xFF, 0xFF, 0xFF, 0x7F, 1 };
        var result = Rm2kMoveRouteDecoder.Decode(Fields(bytes.Length, bytes));
        AssertTrue(result.Success, result.Error?.Describe() ?? "Native signed parameter decodes");
        if (!result.Success) return;
        var commands = result.Data["move_commands"].AsGodotArray();
        AssertEq(commands.Count, 2);
        AssertEq(commands[0].AsGodotDictionary()["parameter_a"].AsInt32(), -1);
        AssertEq(commands[1].AsGodotDictionary()["command_id"].AsInt32(), 1);
    }

    public void Test_NativeSparseParametersPreserveFollowingOpcodeBoundaries()
    {
        // switch129, move_right, graphic A/index2, sound empty/100/90/50, move_up.
        var bytes = new byte[] { 32, 0x81, 1, 1, 34, 1, 65, 2, 35, 0, 100, 90, 50, 0 };
        var result = Rm2kMoveRouteDecoder.Decode(Fields(bytes.Length, bytes));
        AssertTrue(result.Success, result.Error?.Describe() ?? "Native sparse commands decode");
        if (!result.Success) return;
        var commands = result.Data["move_commands"].AsGodotArray();
        AssertEq(commands.Count, 5);
        AssertEq(commands[0].AsGodotDictionary()["parameter_a"].AsInt32(), 129);
        AssertEq(commands[1].AsGodotDictionary()["command_id"].AsInt32(), 1);
        AssertEq(commands[2].AsGodotDictionary()["parameter_string"].AsString(), "A");
        AssertEq(commands[2].AsGodotDictionary()["parameter_a"].AsInt32(), 2);
        AssertEq(commands[3].AsGodotDictionary()["parameter_a"].AsInt32(), 100);
        AssertEq(commands[3].AsGodotDictionary()["parameter_b"].AsInt32(), 90);
        AssertEq(commands[3].AsGodotDictionary()["parameter_c"].AsInt32(), 50);
        AssertEq(commands[4].AsGodotDictionary()["command_id"].AsInt32(), 0);
    }

    public void Test_NativeVectorHasNoCountPrefixOrUniversalParameterTuple()
    {
        // Pinned liblcf RawStruct<vector<MoveCommand>> consumes the payload
        // length; ordinary commands contain only their opcode.
        var result = Rm2kMoveRouteDecoder.Decode(Fields(2, new byte[] { 1, 29 }));
        AssertTrue(result.Success, $"Native move_right/decrease_speed bytes decode: {result.Error?.Describe()}");
        if (!result.Success) return;
        var commands = result.Data["move_commands"].AsGodotArray();
        AssertEq(commands.Count, 2);
        AssertEq(commands[0].AsGodotDictionary()["command_id"].AsInt32(), 1);
        AssertEq(commands[1].AsGodotDictionary()["command_id"].AsInt32(), 29);
        AssertEq(commands[0].AsGodotDictionary()["parameter_a"].AsInt32(), 0);
    }
}
