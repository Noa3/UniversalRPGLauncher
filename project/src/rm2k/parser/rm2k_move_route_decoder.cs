using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Core;
using UniversalRPG.Rm2k;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Decodes the <c>rpg::MoveRoute</c> struct, the move route of an event page.
/// </summary>
/// <remarks>
/// <para>
/// The fields come from liblcf's own <c>generator/csv/fields.csv</c>:
/// <c>move_commands</c> is a <c>Vector&lt;MoveCommand&gt;</c> with 0x0B for the
/// count and 0x0C for the entries, <c>repeat</c> is a flag at 0x15 that
/// defaults to true, and <c>skippable</c> is a flag at 0x16 that defaults to
/// false. The struct itself is LMU chunk 0x29 on an event page.
/// </para>
/// <para>
/// An LCF array is stored as a count field followed by an array field that also
/// carries the count, then the entries. A move command is
/// <c>command_id</c>, <c>parameter_string</c> and the three integers
/// <c>parameter_a</c>, <c>parameter_b</c> and <c>parameter_c</c>, all read as
/// BER integers with a BER length prefixed string.
/// </para>
/// </remarks>
public static class Rm2kMoveRouteDecoder
{
	/// <summary>liblcf EventPage::move_route, the LMU chunk holding the struct.</summary>
	public const int MoveRouteChunk = 0x29;

	/// <summary>liblcf MoveRoute::move_commands count field.</summary>
	private const int MoveCommandsCount = 0x0B;

	/// <summary>liblcf MoveRoute::move_commands array field.</summary>
	private const int MoveCommandsArray = 0x0C;

	/// <summary>liblcf MoveRoute::repeat.</summary>
	private const int RepeatField = 0x15;

	/// <summary>liblcf MoveRoute::skippable.</summary>
	private const int SkippableField = 0x16;

	/// <summary>
	/// A route with more commands than this is refused rather than allocated.
	/// The format has no such limit, but a corrupt count would otherwise ask for
	/// an arbitrary allocation before a single command is validated.
	/// </summary>
	public const int MaxMoveCommands = 4096;

	/// <summary>Length bound for a command's parameter string.</summary>
	public const int MaxParameterStringBytes = 65535;

	/// <summary>
	/// Decodes the <c>move_route</c> chunk of an event page, which is LMU chunk
	/// 0x29 holding a <c>rpg::MoveRoute</c> struct.
	/// </summary>
	/// <remarks>
	/// The page's own fields are not the route's fields: 0x0B and 0x0C live
	/// inside the 0x29 struct, so the chunk is read as a nested struct first.
	/// Passing the page fields straight in would report every page as having no
	/// route, which looks like a working decoder and is not one.
	/// </remarks>
	/// <param name="pPageFields">The event page's fields, keyed by chunk id.</param>
	public static Rm2kParser.ParseResult Decode(Godot.Collections.Dictionary pPageFields)
	{
		var commands = new List<Godot.Collections.Dictionary>();
		var repeat = true;
		var skippable = false;

		// A page with no route is not an error: liblcf defaults every field, so
		// an absent move_commands means an empty route.
		if (pPageFields == null || !pPageFields.TryGetValue(MoveRouteChunk, out var routeChunk))
		{
			return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary
			{
				{ "move_commands", new Godot.Collections.Array<Godot.Collections.Dictionary>(commands) },
				{ "command_count", 0 },
				{ "declared_count", 0 },
				{ "repeat", repeat },
				{ "skippable", skippable },
			});
		}

		Godot.Collections.Dictionary pFields;
		try
		{
			var fieldsResult = Rm2kParser.ReadNestedStructFieldsForMoveRoute(
				(Godot.Collections.Dictionary)routeChunk);
			if (!fieldsResult.Success)
			{
				return Failure($"The move route struct 0x{MoveRouteChunk:X} is invalid: {fieldsResult.Error!.Message}");
			}
			pFields = fieldsResult.Data;
		}
		catch (Exception e)
		{
			return Failure($"The move route chunk 0x{MoveRouteChunk:X} is not a struct: {e.Message}");
		}

		if (!pFields.ContainsKey(MoveCommandsArray))
		{
			// The chunk exists but carries no move_commands, which is a route
			// with no commands rather than a damaged one.
			return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary
			{
				{ "move_commands", new Godot.Collections.Array<Godot.Collections.Dictionary>(commands) },
				{ "command_count", 0 },
				{ "declared_count", 0 },
				{ "repeat", repeat },
				{ "skippable", skippable },
			});
		}

		// Both fields are fetched defensively. A move route chunk in a real
		// database can be a stub, and a direct index would throw and take the
		// whole map with it; a route that is not a chunk is an empty route with
		// the reason recorded, which is what a page that never saved one looks
		// like anyway.
		if (!pFields.TryGetValue(MoveCommandsCount, out var rawCount)
			|| !pFields.TryGetValue(MoveCommandsArray, out var rawArray))
		{
			return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary
			{
				{ "move_commands", new Godot.Collections.Array<Godot.Collections.Dictionary>(commands) },
				{ "command_count", 0 },
				{ "declared_count", 0 },
				{ "repeat", repeat },
				{ "skippable", skippable },
			});
		}

		if (rawCount.VariantType != Variant.Type.Dictionary
			|| rawArray.VariantType != Variant.Type.Dictionary)
		{
			return Failure("The move route count or command field is not a chunk");
		}
		var countChunk = (Godot.Collections.Dictionary)rawCount;
		var arrayChunk = (Godot.Collections.Dictionary)rawArray;

		// Both chunks are indexed defensively: a real move route can carry a
		// field with no payload, and a direct index would throw instead of
		// letting the page keep its default.
		if (!TryGetPayload(countChunk, out var countData)
			|| !TryGetPayload(arrayChunk, out var arrayData))
		{
			return Failure("The move route count or command field carries no byte payload");
		}

		var countResult = DecodeCountField(countData);
		if (!countResult.Success)
		{
			return Failure($"The move_commands count field 0x{MoveCommandsCount:X} is invalid: {countResult.Error!.Message}");
		}
		var declaredCount = (int)countResult.Data["value"];

		var reader = new LcfBinaryReader(arrayData);
		var arrayCount = reader.ReadBer();
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		if (arrayCount < 0 || arrayCount > MaxMoveCommands)
		{
			return Failure($"The move route declares {arrayCount} commands, which is outside 0 to {MaxMoveCommands}", reader.GetPosition());
		}

		// The count field is checked against the same bound. A declared count
		// past the limit is refused here rather than after the array has been
		// walked, so a corrupt file cannot make the reader look for thousands
		// of commands it will never find.
		if (declaredCount < 0 || declaredCount > MaxMoveCommands)
		{
			return Failure($"The move route count field says {declaredCount}, which is outside 0 to {MaxMoveCommands}");
		}

		for (var index = 0; index < arrayCount; index++)
		{
			var commandResult = DecodeCommand(reader, index, arrayCount);
			if (!commandResult.Success)
			{
				return commandResult;
			}
			commands.Add((Godot.Collections.Dictionary)commandResult.Data);
		}

		if (ReadFlag(pFields, RepeatField, out var repeatValue))
		{
			repeat = repeatValue;
		}
		if (ReadFlag(pFields, SkippableField, out var skippableValue))
		{
			skippable = skippableValue;
		}

		// Both fields carry the count and a route stores no per-command length,
		// so disagreeing counts would shift every following command. That is
		// reported rather than resolved in favour of one of them.
		if (declaredCount != arrayCount)
		{
			return Failure($"The move route count field says {declaredCount} but the array holds {arrayCount}", reader.GetPosition());
		}

		return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "move_commands", new Godot.Collections.Array<Godot.Collections.Dictionary>(commands) },
			{ "command_count", commands.Count },
			{ "declared_count", declaredCount },
			{ "repeat", repeat },
			{ "skippable", skippable },
		});
	}

	/// <summary>
	/// Reads a chunk's byte payload, or reports that it has none. A field with
	/// no payload is a real possibility in a database, so it is a value to check
	/// rather than an exception to catch: a direct index would throw and take the
	/// whole map with it.
	/// </summary>
	private static bool TryGetPayload(Godot.Collections.Dictionary pChunk, out byte[] pData)
	{
		pData = [];
		if (!pChunk.TryGetValue("data", out var raw) || raw.VariantType != Variant.Type.PackedByteArray)
		{
			return false;
		}
		pData = (byte[])raw;
		return true;
	}

	private static Rm2kParser.ParseResult DecodeCommand(LcfBinaryReader pReader, int pIndex, int pCount)
	{
		var commandId = pReader.ReadBer();
		if (pReader.HasError())
		{
			return ReaderFailure(pReader);
		}

		var stringLength = pReader.ReadBer();
		if (pReader.HasError() || stringLength < 0 || stringLength > MaxParameterStringBytes)
		{
			return Failure($"Move command {pIndex} of {pCount} has a string length outside 0 to {MaxParameterStringBytes}", pReader.GetPosition());
		}
		var textBytes = pReader.ReadBytes(stringLength);
		if (pReader.HasError())
		{
			return ReaderFailure(pReader);
		}
		var parameterString = new LegacyTextDecoder().Decode(textBytes);

		var parameterA = pReader.ReadBer();
		var parameterB = pReader.ReadBer();
		var parameterC = pReader.ReadBer();
		if (pReader.HasError())
		{
			return ReaderFailure(pReader);
		}

		// The command travels as a Godot dictionary because ParseResult carries
		// one; the typed Rm2kMap.MoveCommand is rebuilt from these fields by the
		// caller, so nothing depends on the parser's own dictionary flavour.
		var command = new Godot.Collections.Dictionary
		{
			{ "command_id", commandId },
			{ "parameter_string", parameterString },
			{ "parameter_a", parameterA },
			{ "parameter_b", parameterB },
			{ "parameter_c", parameterC },
		};
		return new Rm2kParser.ParseResult(true, null, command);
	}

	private static Rm2kParser.ParseResult DecodeCountField(byte[] pData)
	{
		if (pData.Length == 0)
		{
			return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary { { "value", 0 } });
		}
		var reader = new LcfBinaryReader(pData);
		var value = reader.ReadBer();
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary { { "value", value } });
	}

	/// <summary>
	/// Reads a liblcf flag field, which is a single BER byte. An absent or
	/// malformed flag keeps the liblcf default rather than failing the page,
	/// because a flag carries no framing information.
	/// </summary>
	private static bool ReadFlag(Godot.Collections.Dictionary pFields, int pId, out bool pValue)
	{
		pValue = false;
		if (!pFields.TryGetValue(pId, out var chunk))
		{
			return false;
		}
		byte[] data;
		try
		{
			data = (byte[])((Godot.Collections.Dictionary)chunk)["data"];
		}
		catch
		{
			return false;
		}
		if (data.Length == 0)
		{
			return false;
		}
		var reader = new LcfBinaryReader(data);
		pValue = reader.ReadBer() != 0;
		return !reader.HasError();
	}

	private static Rm2kParser.ParseResult Failure(string pMessage, int pOffset = -1)
	{
		return new Rm2kParser.ParseResult(false, new Rm2kParser.ParseError(pOffset, pMessage), null);
	}

	private static Rm2kParser.ParseResult ReaderFailure(LcfBinaryReader pReader)
	{
		return new Rm2kParser.ParseResult(false, new Rm2kParser.ParseError(pReader.GetPosition(), pReader.ErrorMessage), null);
	}
}
