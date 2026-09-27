using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the <c>rpg::MoveRoute</c> decoding against liblcf's field list:
/// <c>move_commands</c> is a vector at 0x0B and 0x0C, <c>repeat</c> is a flag
/// at 0x15 defaulting to true, and <c>skippable</c> is a flag at 0x16
/// defaulting to false.
/// </summary>
/// <remarks>
/// The fixtures are byte exact. A move route stores no per-command length, so a
/// single wrong byte shifts every command after it and the route still parses;
/// only the command ids reveal the drift.
/// </remarks>
public partial class TestRm2kMoveRouteDecoder : TestBase
{
	/// <summary>
	/// A BER integer, which is how LCF writes every number. The value is split
	/// into seven bit groups and the highest group is written first, so 4097 is
	/// the two bytes 160 and 1 rather than 129 and 32.
	/// </summary>
	private static byte[] Ber(int pValue)
	{
		var groups = new List<byte>();
		var value = pValue;
		do
		{
			groups.Add((byte)(value & 0x7F));
			value >>= 7;
		}
		while (value != 0);
		groups.Reverse();
		for (var index = 0; index < groups.Count - 1; index++)
		{
			groups[index] |= 0x80;
		}
		return [.. groups];
	}

	private static byte[] Join(params byte[][] pParts)
	{
		var result = new List<byte>();
		foreach (var part in pParts)
		{
			result.AddRange(part);
		}
		return [.. result];
	}

	/// <summary>One move command: id, BER length prefixed string, then a, b, c.</summary>
	private static byte[] Command(int pId, string pString = "", int pA = 0, int pB = 0, int pC = 0)
	{
		return Join(Ber(pId), Ber(pString.Length), System.Text.Encoding.ASCII.GetBytes(pString),
			Ber(pA), Ber(pB), Ber(pC));
	}

	private static Godot.Collections.Dictionary Chunk(byte[] pData)
	{
		return new Godot.Collections.Dictionary { { "data", pData }, { "payload_offset", 0 } };
	}

	private static Godot.Collections.Dictionary Fields(
		int? pCount, byte[]? pArray, bool? pRepeat = null, bool? pSkippable = null)
	{
		// The route fields live inside LMU chunk 0x29, so they are written as a
		// struct payload and then wrapped in that chunk, exactly as liblcf
		// reads them. Wrapping them wrongly would make every fixture look like a
		// page with no route.
		var inner = new List<byte[]>();
		if (pCount != null)
		{
			inner.Add(TestRm2kParser.Chunk(0x0B, Ber(pCount.Value)));
		}
		if (pArray != null)
		{
			inner.Add(TestRm2kParser.Chunk(0x0C, pArray));
		}
		if (pRepeat != null)
		{
			inner.Add(TestRm2kParser.Chunk(0x15, Ber(pRepeat.Value ? 1 : 0)));
		}
		if (pSkippable != null)
		{
			inner.Add(TestRm2kParser.Chunk(0x16, Ber(pSkippable.Value ? 1 : 0)));
		}
		return new Godot.Collections.Dictionary
		{
			{ Rm2kMoveRouteDecoder.MoveRouteChunk, Chunk(TestRm2kParser.Struct(inner.ToArray())) },
		};
	}

	public void Test_ARouteWithNoChunkIsAnEmptyRouteRatherThanAnError()
	{
		// liblcf defaults every field, so a page that never saved a route has no
		// 0x0C at all. Refusing that would make most maps unparseable.
		var result = Rm2kMoveRouteDecoder.Decode(new Godot.Collections.Dictionary());
		AssertTrue(result.Success, "an absent route is an empty route");
		AssertEq((int)result.Data["command_count"], 0, "and it holds no commands");
		AssertEq((bool)result.Data["repeat"], true, "repeat defaults to true");
		AssertEq((bool)result.Data["skippable"], false, "skippable defaults to false");
	}

	public void Test_ASingleCommandDecodesWithItsThreeParameters()
	{
		var array = Join(Ber(1), Command(Rm2kMoveRoute.MoveDown, "", 7, 8, 9));
		var result = Rm2kMoveRouteDecoder.Decode(Fields(1, array));
		AssertTrue(result.Success, $"a one command route parses: {result.Error?.Describe()}");
		AssertEq((int)result.Data["command_count"], 1, "one command");

		var commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)result.Data["move_commands"];
		AssertEq(commands.Count, 1, "one command in the array");
		AssertEq((int)commands[0]["command_id"], 2, "move_down is command 2");
		AssertEq((int)commands[0]["parameter_a"], 7, "parameter_a");
		AssertEq((int)commands[0]["parameter_b"], 8, "parameter_b");
		AssertEq((int)commands[0]["parameter_c"], 9, "parameter_c");
	}

	public void Test_ASequenceOfCommandsKeepsItsOrder()
	{
		// A route has no per-command length, so a reader that miscounts drifts
		// silently. Asserting the whole id sequence is what catches that.
		var array = Join(Ber(4),
			Command(0), Command(14), Command(23), Command(41));
		var result = Rm2kMoveRouteDecoder.Decode(Fields(4, array));
		AssertTrue(result.Success, $"a four command route parses: {result.Error?.Describe()}");
		var commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)result.Data["move_commands"];
		AssertEq(commands.Count, 4, "four commands");
		AssertEq((int)commands[0]["command_id"], 0, "first is move_up");
		AssertEq((int)commands[1]["command_id"], 14, "second is face_down");
		AssertEq((int)commands[2]["command_id"], 23, "third is wait");
		AssertEq((int)commands[3]["command_id"], 41, "fourth is decrease_transp");
	}

	public void Test_AParameterStringIsLengthPrefixedAndDoesNotShiftTheNextCommand()
	{
		var array = Join(Ber(2),
			Command(32, "switch id", 5), Command(33, "other"));
		var result = Rm2kMoveRouteDecoder.Decode(Fields(2, array));
		AssertTrue(result.Success, $"a route with strings parses: {result.Error?.Describe()}");
		var commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)result.Data["move_commands"];
		AssertEq((string)commands[0]["parameter_string"], "switch id", "the string is read whole");
		AssertEq((int)commands[0]["command_id"], 32, "the first command is switch_on");
		// The point of the test: the string length did not consume the next
		// command's bytes.
		AssertEq((int)commands[1]["command_id"], 33, "the second command is still switch_off");
		AssertEq((string)commands[1]["parameter_string"], "other", "and its own string is read");
	}

	public void Test_TheFlagsAreReadAndTheDefaultsHoldWhenTheyAreAbsent()
	{
		var array = Join(Ber(1), Command(0));
		var explicitResult = Rm2kMoveRouteDecoder.Decode(Fields(1, array, pRepeat: false, pSkippable: true));
		AssertTrue(explicitResult.Success, "explicit flags parse");
		AssertEq((bool)explicitResult.Data["repeat"], false, "repeat off is read");
		AssertEq((bool)explicitResult.Data["skippable"], true, "skippable on is read");

		var absentResult = Rm2kMoveRouteDecoder.Decode(Fields(1, array));
		AssertTrue(absentResult.Success, "absent flags parse");
		AssertEq((bool)absentResult.Data["repeat"], true, "repeat defaults to true");
		AssertEq((bool)absentResult.Data["skippable"], false, "skippable defaults to false");
	}

	public void Test_ACountThatDisagreesWithTheArrayIsRefused()
	{
		// Both 0x0B and 0x0C carry the count. Picking either one would desync the
		// route, so the disagreement has to be reported.
		var array = Join(Ber(2), Command(0), Command(0));
		var result = Rm2kMoveRouteDecoder.Decode(Fields(3, array));
		AssertTrue(!result.Success, "a mismatched count is refused");
		AssertTrue(result.Error!.Message.Contains("3") && result.Error!.Message.Contains("2"),
			$"the message names both counts: {result.Error!.Message}");
	}

	public void Test_ACountBeyondTheBoundIsRefusedBeforeAnythingIsAllocated()
	{
		// The format has no such limit, so this is our own guard against a
		// corrupt count asking for an arbitrary allocation.
		// The array header has to be long enough to be read before the bound is
		// reached, otherwise the reader reports a short read instead.
		var tooMany = Rm2kMoveRouteDecoder.MaxMoveCommands + 1;
		var result = Rm2kMoveRouteDecoder.Decode(Fields(tooMany, Join(Ber(tooMany))));
		AssertTrue(!result.Success, "a count past the bound is refused");
		AssertTrue(result.Error!.Message.Contains("outside"),
			$"the message says it is out of bounds: {result.Error!.Message}");
	}

	public void Test_ATruncatedArrayIsRefusedRatherThanReadingPastTheEnd()
	{
		// The array says two commands but carries one and a half. Reading on would
		// consume the parameters of the first command as a second id.
		var array = Join(Ber(2), Command(0), Ber(14));
		var result = Rm2kMoveRouteDecoder.Decode(Fields(2, array));
		AssertTrue(!result.Success, "a truncated array is refused");
	}

	public void Test_AnEmptyArrayIsAValidEmptyRoute()
	{
		var result = Rm2kMoveRouteDecoder.Decode(Fields(0, Join(Ber(0))));
		AssertTrue(result.Success, $"an empty array parses: {result.Error?.Describe()}");
		AssertEq((int)result.Data["command_count"], 0, "with no commands");
	}
}
