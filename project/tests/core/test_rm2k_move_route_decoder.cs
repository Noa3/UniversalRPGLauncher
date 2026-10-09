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

	/// <summary>Pinned liblcf command-specific fields, with no vector count prefix.</summary>
	private static byte[] Command(int pId, string pString = "", int pA = 0, int pB = 0, int pC = 0)
	{
		var parts = new List<byte[]> { Ber(pId) };
		if (pId is 34 or 35) { parts.Add(Ber(pString.Length)); parts.Add(System.Text.Encoding.ASCII.GetBytes(pString)); }
		if (pId is 32 or 33 or 34 or 35) parts.Add(Ber(pA));
		if (pId == 35) { parts.Add(Ber(pB)); parts.Add(Ber(pC)); }
		return Join(parts.ToArray());
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
		var array = Command(35, "", 7, 8, 9);
		var result = Rm2kMoveRouteDecoder.Decode(Fields(array.Length, array));
		AssertTrue(result.Success, $"a one command route parses: {result.Error?.Describe()}");
		AssertEq((int)result.Data["command_count"], 1, "one command");

		var commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)result.Data["move_commands"];
		AssertEq(commands.Count, 1, "one command in the array");
		AssertEq((int)commands[0]["command_id"], 35, "Only sound35 carries all three integer parameters");
		AssertEq((int)commands[0]["parameter_a"], 7, "parameter_a");
		AssertEq((int)commands[0]["parameter_b"], 8, "parameter_b");
		AssertEq((int)commands[0]["parameter_c"], 9, "parameter_c");
	}

	public void Test_ASequenceOfCommandsKeepsItsOrder()
	{
		// A route has no per-command length, so a reader that miscounts drifts
		// silently. Asserting the whole id sequence is what catches that.
		var array = Join(
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
		var array = Join(Command(34, "Chara1", 5), Command(35, "Sound1", 80, 100, 50));
		var result = Rm2kMoveRouteDecoder.Decode(Fields(array.Length, array));
		AssertTrue(result.Success, $"a route with strings parses: {result.Error?.Describe()}");
		var commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)result.Data["move_commands"];
		AssertEq((string)commands[0]["parameter_string"], "Chara1", "the string is read whole");
		AssertEq((int)commands[0]["command_id"], 34, "the first command is change_graphic");
		// The point of the test: the string length did not consume the next
		// command's bytes.
		AssertEq((int)commands[1]["command_id"], 35, "the second command is still play_sound_effect");
		AssertEq((string)commands[1]["parameter_string"], "Sound1", "and its own string is read");
	}

	public void Test_TheFlagsAreReadAndTheDefaultsHoldWhenTheyAreAbsent()
	{
		var array = Command(0);
		var explicitResult = Rm2kMoveRouteDecoder.Decode(Fields(1, array, pRepeat: false, pSkippable: true));
		AssertTrue(explicitResult.Success, "explicit flags parse");
		AssertEq((bool)explicitResult.Data["repeat"], false, "repeat off is read");
		AssertEq((bool)explicitResult.Data["skippable"], true, "skippable on is read");

		var absentResult = Rm2kMoveRouteDecoder.Decode(Fields(1, array));
		AssertTrue(absentResult.Success, "absent flags parse");
		AssertEq((bool)absentResult.Data["repeat"], true, "repeat defaults to true");
		AssertEq((bool)absentResult.Data["skippable"], false, "skippable defaults to false");
	}

	public void Test_OuterPayloadLengthIsAuthoritativeInsteadOfInventingACount()
	{
		// 0x0B is size metadata; the RawStruct vector uses the actual0x0C length.
		var array = Join(Command(0), Command(0));
		var result = Rm2kMoveRouteDecoder.Decode(Fields(3, array));
		AssertTrue(result.Success, "Size metadata cannot invent or drop commands");
		AssertEq(result.Data["command_count"].AsInt32(), 2);
	}

	public void Test_ActualCommandBudgetIsEnforcedWhileWalkingThePayload()
	{
		// Each zero byte is a valid opcode-only move_up. The native format has
		// no command count; the application budget limits actual decoded items.
		var tooMany = Rm2kMoveRouteDecoder.MaxMoveCommands + 1;
		var result = Rm2kMoveRouteDecoder.Decode(Fields(tooMany, new byte[tooMany]));
		AssertTrue(!result.Success, "a count past the bound is refused");
		AssertTrue(result.Error!.Message.Contains("outside"),
			$"the message says it is out of bounds: {result.Error!.Message}");
	}

	public void Test_ATruncatedArrayIsRefusedRatherThanReadingPastTheEnd()
	{
		// One complete opcode followed by a graphic string that exceeds the
		// remaining payload must fail, never consume bytes outside the chunk.
		var array = Join(Command(0), Ber(34), Ber(10), new byte[] { 65 });
		var result = Rm2kMoveRouteDecoder.Decode(Fields(array.Length, array));
		AssertTrue(!result.Success, "a truncated array is refused");
	}

	public void Test_AnEmptyArrayIsAValidEmptyRoute()
	{
		var result = Rm2kMoveRouteDecoder.Decode(Fields(0, System.Array.Empty<byte>()));
		AssertTrue(result.Success, $"an empty array parses: {result.Error?.Describe()}");
		AssertEq((int)result.Data["command_count"], 0, "with no commands");
	}
}
