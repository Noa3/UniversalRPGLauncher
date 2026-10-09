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
/// byte-size metadata and 0x0C for the entries, <c>repeat</c> is a flag at 0x15 that
/// defaults to true, and <c>skippable</c> is a flag at 0x16 that defaults to
/// false. The struct itself is LMU chunk 0x29 on an event page.
/// </para>
/// <para>
/// Pinned liblcf RawStruct&lt;vector&lt;MoveCommand&gt;&gt; consumes the0x0C
/// payload to its byte boundary without a command-count prefix. Ordinary
/// commands contain only an opcode. Switch commands32/33 add A; graphic34
/// adds a length-prefixed string and A; sound35 adds a string and A/B/C.
/// The0x0B field is byte-size metadata, not a command count.
/// </para>
/// </remarks>
public static class Rm2kMoveRouteDecoder
{
	/// <summary>liblcf EventPage::move_route, the LMU chunk holding the struct.</summary>
	public const int MoveRouteChunk = 0x29;


	/// <summary>liblcf MoveRoute::move_commands array field.</summary>
	private const int MoveCommandsArray = 0x0C;

	/// <summary>liblcf MoveRoute::repeat.</summary>
	private const int RepeatField = 0x15;

	/// <summary>liblcf MoveRoute::skippable.</summary>
	private const int SkippableField = 0x16;

	/// <summary>
	/// A route with more commands than this is refused while walking its payload.
	/// This is an application budget, not a count declared by the native vector.
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

		if (ReadFlag(pFields, RepeatField, out var repeatValue)) repeat = repeatValue;
		if (ReadFlag(pFields, SkippableField, out var skippableValue)) skippable = skippableValue;
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
		if (!pFields.TryGetValue(MoveCommandsArray, out var rawArray))
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

		if (rawArray.VariantType != Variant.Type.Dictionary)
		{
			return Failure("The move route count or command field is not a chunk");
		}
		var arrayChunk = (Godot.Collections.Dictionary)rawArray;

		// Both chunks are indexed defensively: a real move route can carry a
		// field with no payload, and a direct index would throw instead of
		// letting the page keep its default.
		if (!TryGetPayload(arrayChunk, out var arrayData))
		{
			return Failure("The move route count or command field carries no byte payload");
		}

		// RawStruct<vector<MoveCommand>> is bounded by the chunk's byte length,
		// not an embedded command count. Ordinary commands are opcode-only.
		if (arrayData.Length > LcfBinaryReader.MaxChunkBytes)
			return Failure("Move route payload exceeds the chunk byte limit");
		using var reader = new LcfBinaryReader(arrayData);
		while (!reader.IsEof())
		{
			if (commands.Count >= MaxMoveCommands)
				return Failure($"Move route command count is outside 0 to {MaxMoveCommands}", reader.GetPosition());
			var commandResult = DecodeCommand(reader, commands.Count);
			if (!commandResult.Success)
			{
				return commandResult;
			}
			commands.Add((Godot.Collections.Dictionary)commandResult.Data);
		}


		return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "move_commands", new Godot.Collections.Array<Godot.Collections.Dictionary>(commands) },
			{ "command_count", commands.Count },
			// Compatibility alias: there is no declared command count on wire.
			{ "declared_count", commands.Count },
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

	private static Rm2kParser.ParseResult DecodeCommand(LcfBinaryReader pReader, int pIndex)
	{
		var commandId = pReader.ReadBer();
		if (pReader.HasError())
		{
			return ReaderFailure(pReader);
		}

		var parameterString = "";
		var parameterA = 0;
		var parameterB = 0;
		var parameterC = 0;
		if (commandId is 34 or 35)
		{
			var stringLength = pReader.ReadBer();
			if (pReader.HasError() || stringLength < 0 || stringLength > MaxParameterStringBytes)
				return Failure($"Move command {pIndex} has a string length outside 0 to {MaxParameterStringBytes}", pReader.GetPosition());
			var textBytes = pReader.ReadBytes(stringLength);
			if (pReader.HasError()) return ReaderFailure(pReader);
			parameterString = new LegacyTextDecoder().Decode(textBytes);
		}
		if (commandId is 32 or 33 or 34 or 35) parameterA = pReader.ReadSignedBer();
		if (commandId == 35)
		{
			parameterB = pReader.ReadSignedBer();
			parameterC = pReader.ReadSignedBer();
		}
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
		using var reader = new LcfBinaryReader(data);
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
