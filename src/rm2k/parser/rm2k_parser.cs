using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Parser for RPG Maker 2000/2003 LCF files (LDB database, LMU map, LSD save).
/// Only the safe container layer plus real map base fields are decoded;
/// full event/battle content is not interpreted.
/// </summary>
public partial class Rm2kParser : RefCounted
{
	public const long MaxFileBytes = 64L * 1024 * 1024;
	public const int MaxMapDimension = 500;
	public const int MaxMapTiles = 250_000;

	public const string LdbHeader = "LcfDataBase";
	public const string LmuHeader = "LcfMapUnit";
	public const string LsdHeader = "LcfSaveData";

	public static readonly Dictionary<int, string> LdbSectionNames = new()
	{
		{ 0x0b, "actors" },
		{ 0x0c, "skills" },
		{ 0x0d, "items" },
		{ 0x0e, "enemies" },
		{ 0x0f, "troops" },
		{ 0x10, "terrains" },
		{ 0x11, "attributes" },
		{ 0x12, "states" },
		{ 0x13, "animations" },
		{ 0x14, "chipsets" },
		{ 0x15, "terms" },
		{ 0x16, "system" },
		{ 0x17, "switches" },
		{ 0x18, "variables" },
		{ 0x19, "common_events" },
		{ 0x1a, "version" },
		{ 0x1b, "common_event_duplicate_1" },
		{ 0x1c, "common_event_duplicate_2" },
		{ 0x1d, "battle_commands" },
		{ 0x1e, "classes" },
		{ 0x1f, "class_duplicate" },
		{ 0x20, "battler_animations" },
		{ 0x21, "string_variables" },
	};

	public static readonly int[] LdbArraySections =
	{
		0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13,
		0x14, 0x17, 0x18, 0x19, 0x1e, 0x1f, 0x20, 0x21,
	};

	private readonly LegacyTextDecoder _textDecoder = new();

	public class ParseError
	{
		public int Offset { get; }
		public string Message { get; }

		public ParseError(int pOffset = -1, string pMessage = "")
		{
			Offset = pOffset;
			Message = pMessage;
		}

		public string Describe()
		{
			return Offset >= 0 ? $"Offset 0x{Offset:X}: {Message}" : Message;
		}
	}

	public class ParseResult
	{
		public bool Success { get; }
		public ParseError? Error { get; }
		public Godot.Collections.Dictionary Data { get; }

		public ParseResult(bool pSuccess = false, ParseError? pError = null, Godot.Collections.Dictionary? pData = null)
		{
			Success = pSuccess;
			Error = pError;
			Data = pData ?? [];
		}

		public bool IsSuccess() => Success;

		public Godot.Collections.Dictionary GetData() => Data;

		public ParseError? GetError() => Error;
	}

	public ParseResult ParseGameIni(string pPath)
	{
		var loaded = ReadFile(pPath, 1024 * 1024);
		if (!loaded.Success)
		{
			return loaded;
		}
		var bytes = (byte[])loaded.Data["bytes"];
		var text = _textDecoder.Decode(bytes);
		if (string.IsNullOrEmpty(text) && bytes.Length > 0)
		{
			return Failure("Unable to decode INI text", 0);
		}

		var values = new Godot.Collections.Dictionary();
		var currentSection = "";
		var foundSection = false;
		foreach (var rawLine in text.Split('\n'))
		{
			var line = rawLine.TrimEnd('\r').Trim();
			if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
			{
				continue;
			}
			if (line.StartsWith('[') && line.EndsWith(']'))
			{
				currentSection = line.Substring(1, line.Length - 2);
				if (currentSection.Equals("RPG_RT", StringComparison.OrdinalIgnoreCase)
					|| currentSection.Equals("Game", StringComparison.OrdinalIgnoreCase))
				{
					foundSection = true;
				}
				continue;
			}
			var separator = line.IndexOf('=');
			if (separator < 0 || !foundSection)
			{
				continue;
			}
			var key = line[..separator].Trim();
			var value = line[(separator + 1)..].Trim();
			values[key] = value;
		}

		if (!foundSection)
		{
			return Failure("Expected [RPG_RT] or [Game] section", 0);
		}
		values["section"] = currentSection;
		return new ParseResult(true, null, values);
	}

	public ParseResult ParseDatabase(string pPath)
	{
		var opened = OpenLcf(pPath, LdbHeader);
		if (!opened.Success)
		{
			return opened;
		}
		var reader = (LcfBinaryReader)opened.Data["reader"];
		var top = ReadTopChunks(reader);
		if (!top.Success)
		{
			return top;
		}

		var sections = new Godot.Collections.Dictionary();
		var sectionCounts = new Godot.Collections.Dictionary();
		var unknownChunks = new List<Godot.Collections.Dictionary>();
		var engineFamily = "RPG Maker 2000";
		var version = 0;

		foreach (var chunk in (Godot.Collections.Array<Godot.Collections.Dictionary>)top.Data["chunks"])
		{
			var id = (int)chunk["id"];
			if (!LdbSectionNames.TryGetValue(id, out var sectionName))
			{
				unknownChunks.Add(chunk);
				continue;
			}
			var section = new Godot.Collections.Dictionary
			{
				{ "id", id },
				{ "offset", (int)chunk["offset"] },
				{ "length", (int)chunk["length"] },
			};
			if (Array.IndexOf(LdbArraySections, id) >= 0)
			{
				var arrayResult = ParseStructArray((byte[])chunk["data"], false);
				if (!arrayResult.Success)
				{
					return Failure($"Invalid {sectionName} section: {arrayResult.Error!.Message}",
						(int)chunk["payload_offset"] + Math.Max(arrayResult.Error.Offset, 0));
				}
				section["count"] = (int)arrayResult.Data["count"];
				sectionCounts[sectionName] = (int)arrayResult.Data["count"];
			}
			if (id == 0x1a)
			{
				var integer = DecodeLcfInteger((byte[])chunk["data"]);
				if (!integer.Success)
				{
					return Failure($"Invalid database version: {integer.Error!.Message}", (int)chunk["payload_offset"]);
				}
				version = (int)integer.Data["value"];
				section["value"] = version;
			}
			if (id is 0x1d or 0x1e or 0x1f or 0x20)
			{
				engineFamily = "RPG Maker 2003";
			}
			sections[sectionName] = section;
		}

		return new ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "format", "LDB" },
			{ "header", LdbHeader },
			{ "file_size", (long)opened.Data["file_size"] },
			{ "chunk_count", ((Godot.Collections.Array<Godot.Collections.Dictionary>)top.Data["chunks"]).Count },
			{ "sections", sections },
			{ "section_counts", sectionCounts },
			{ "unknown_chunks", unknownChunks },
			{ "version", version },
			{ "engine_family", engineFamily },
		});
	}

	public ParseResult ParseMap(string pPath)
	{
		var opened = OpenLcf(pPath, LmuHeader);
		if (!opened.Success)
		{
			return opened;
		}
		var reader = (LcfBinaryReader)opened.Data["reader"];
		var top = ReadTopChunks(reader);
		if (!top.Success)
		{
			return top;
		}

		var chunks = (Godot.Collections.Array<Godot.Collections.Dictionary>)top.Data["chunks"];
		var fields = ChunksById(chunks);
		var chipsetResult = IntegerFromFields(fields, 0x01, 1);
		var widthResult = IntegerFromFields(fields, 0x02, 20);
		var heightResult = IntegerFromFields(fields, 0x03, 15);
		foreach (var result in new[] { chipsetResult, widthResult, heightResult })
		{
			if (!result.Success)
			{
				return result;
			}
		}
		var width = (int)widthResult.Data["value"];
		var height = (int)heightResult.Data["value"];
		if (width <= 0 || width > MaxMapDimension || height <= 0 || height > MaxMapDimension)
		{
			return Failure($"Map dimensions {width}x{height} exceed limits", 0);
		}
		var tileCount = width * height;
		if (tileCount > MaxMapTiles)
		{
			return Failure("Map contains too many tiles", 0);
		}

		var lowerResult = DecodeTileLayer(GetField(fields, 0x47), tileCount, "lower");
		if (!lowerResult.Success)
		{
			return lowerResult;
		}
		var upperResult = DecodeTileLayer(GetField(fields, 0x48), tileCount, "upper");
		if (!upperResult.Success)
		{
			return upperResult;
		}

		var events = new List<Godot.Collections.Dictionary>();
		if (fields.TryGetValue(0x51, out var eventChunk))
		{
			var eventArray = ParseStructArray((byte[])eventChunk["data"], true);
			if (!eventArray.Success)
			{
				return Failure($"Invalid map events: {eventArray.Error!.Message}",
					(int)eventChunk["payload_offset"] + Math.Max(eventArray.Error.Offset, 0));
			}
			foreach (var eventObject in (Godot.Collections.Array<Godot.Collections.Dictionary>)eventArray.Data["objects"])
			{
				var eventFields = ChunksById((Godot.Collections.Array<Godot.Collections.Dictionary>)eventObject["fields"]);
				var xResult = IntegerFromFields(eventFields, 0x02, 0);
				var yResult = IntegerFromFields(eventFields, 0x03, 0);
				if (!xResult.Success || !yResult.Success)
				{
					return Failure("Invalid event coordinates", (int)eventChunk["payload_offset"]);
				}
				var pageCount = 0;
				if (eventFields.TryGetValue(0x05, out var pageChunk))
				{
					var pages = ParseStructArray((byte[])pageChunk["data"], false);
					if (!pages.Success)
					{
						return Failure($"Invalid event pages: {pages.Error!.Message}", (int)eventChunk["payload_offset"]);
					}
					pageCount = (int)pages.Data["count"];
				}
				events.Add(new Godot.Collections.Dictionary
				{
					{ "id", (int)eventObject["id"] },
					{ "name", DecodeTextField(eventFields, 0x01) },
					{ "x", (int)xResult.Data["value"] },
					{ "y", (int)yResult.Data["value"] },
					{ "page_count", pageCount },
				});
			}
		}

		int[] knownIds =
		{
			0x01, 0x02, 0x03, 0x0b, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x28, 0x29, 0x2a, 0x30,
			0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x3c, 0x3d, 0x3e, 0x47, 0x48, 0x51, 0x5a, 0x5b,
		};
		var unknownChunks = new List<Godot.Collections.Dictionary>();
		foreach (var chunk in chunks)
		{
			if (Array.IndexOf(knownIds, (int)chunk["id"]) < 0)
			{
				unknownChunks.Add(chunk);
			}
		}

		return new ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "format", "LMU" },
			{ "header", LmuHeader },
			{ "file_size", (long)opened.Data["file_size"] },
			{ "chunk_count", chunks.Count },
			{ "chipset_id", (int)chipsetResult.Data["value"] },
			{ "width", width },
			{ "height", height },
			{ "lower_layer", (int[])lowerResult.Data["tiles"] },
			{ "upper_layer", (int[])upperResult.Data["tiles"] },
			{ "event_count", events.Count },
			{ "events", events },
			{ "unknown_chunks", unknownChunks },
		});
	}

	public ParseResult ParseSave(string pPath)
	{
		var opened = OpenLcf(pPath, LsdHeader);
		if (!opened.Success)
		{
			return opened;
		}
		var reader = (LcfBinaryReader)opened.Data["reader"];
		var top = ReadTopChunks(reader);
		if (!top.Success)
		{
			return top;
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "format", "LSD" },
			{ "header", LsdHeader },
			{ "file_size", (long)opened.Data["file_size"] },
			{ "chunk_count", ((Godot.Collections.Array<Godot.Collections.Dictionary>)top.Data["chunks"]).Count },
			{ "chunks", top.Data["chunks"] },
		});
	}

	private ParseResult OpenLcf(string pPath, string pHeader)
	{
		var loaded = ReadFile(pPath, MaxFileBytes);
		if (!loaded.Success)
		{
			return loaded;
		}
		var reader = new LcfBinaryReader((byte[])loaded.Data["bytes"]);
		reader.ReadHeader(pHeader);
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "reader", reader },
			{ "file_size", ((byte[])loaded.Data["bytes"]).LongLength },
		});
	}

	private static ParseResult ReadFile(string pPath, long pLimit)
	{
		if (!FileAccess.FileExists(pPath))
		{
			return Failure("File not found: " + pPath);
		}
		using var file = FileAccess.Open(pPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			return Failure("Cannot open file: " + pPath);
		}
		var length = file.GetLength();
		if (length > pLimit)
		{
			return Failure($"File exceeds {pLimit}-byte limit");
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "bytes", file.GetBuffer((long)length) } });
	}

	private static ParseResult ReadTopChunks(LcfBinaryReader pReader)
	{
		var chunks = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var terminated = false;
		while (!pReader.IsEof())
		{
			if (chunks.Count >= LcfBinaryReader.MaxChunks)
			{
				return Failure("LCF chunk count exceeds limit", pReader.GetPosition());
			}
			var chunk = pReader.ReadChunk();
			if (pReader.HasError())
			{
				return ReaderFailure(pReader);
			}
			if ((bool)chunk["terminator"])
			{
				terminated = true;
				if (!pReader.IsEof())
				{
					return Failure("Trailing data after LCF terminator", pReader.GetPosition());
				}
				break;
			}
			chunks.Add(chunk);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "chunks", chunks },
			{ "terminated", terminated },
		});
	}

	private static ParseResult ParseStructArray(byte[] pData, bool pCollectFields)
	{
		var reader = new LcfBinaryReader(pData);
		var count = reader.ReadBer();
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		if (count > LcfBinaryReader.MaxArrayItems)
		{
			return Failure($"Array count {count} exceeds limit", 0);
		}
		var objects = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		for (var index = 0; index < count; index++)
		{
			var objectId = reader.ReadBer();
			if (reader.HasError())
			{
				return ReaderFailure(reader);
			}
			var fieldsResult = ReadStructFields(reader, pCollectFields);
			if (!fieldsResult.Success)
			{
				return fieldsResult;
			}
			if (pCollectFields)
			{
				objects.Add(new Godot.Collections.Dictionary
				{
					{ "id", objectId },
					{ "fields", fieldsResult.Data["fields"] },
				});
			}
		}
		if (!reader.IsEof())
		{
			return Failure("Trailing bytes after structure array", reader.GetPosition());
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "count", count }, { "objects", objects } });
	}

	private static ParseResult ReadStructFields(LcfBinaryReader pReader, bool pCollect)
	{
		var fields = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var fieldCount = 0;
		while (!pReader.IsEof())
		{
			if (fieldCount >= LcfBinaryReader.MaxStructFields)
			{
				return Failure("Structure field count exceeds limit", pReader.GetPosition());
			}
			var field = pReader.ReadChunk();
			if (pReader.HasError())
			{
				return ReaderFailure(pReader);
			}
			if ((bool)field["terminator"])
			{
				return new ParseResult(true, null, new Godot.Collections.Dictionary { { "fields", fields } });
			}
			if (pCollect)
			{
				fields.Add(field);
			}
			fieldCount += 1;
		}
		return Failure("Structure is missing terminator", pReader.GetPosition());
	}

	private static Godot.Collections.Dictionary ChunksById(Godot.Collections.Array<Godot.Collections.Dictionary> pChunks)
	{
		var result = new Godot.Collections.Dictionary();
		foreach (var chunk in pChunks)
		{
			result[(int)chunk["id"]] = chunk;
		}
		return result;
	}

	private static ParseResult IntegerFromFields(Godot.Collections.Dictionary pFields, int pId, int pDefault)
	{
		if (!pFields.TryGetValue(pId, out var chunk))
		{
			return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", pDefault } });
		}
		var result = DecodeLcfInteger((byte[])((Godot.Collections.Dictionary)chunk)["data"]);
		if (!result.Success)
		{
			return Failure($"Invalid integer field 0x{pId:X}: {result.Error!.Message}",
				(int)((Godot.Collections.Dictionary)chunk)["payload_offset"]);
		}
		return result;
	}

	private static ParseResult DecodeLcfInteger(byte[] pData)
	{
		if (pData.Length == 0)
		{
			return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", 0 } });
		}
		var reader = new LcfBinaryReader(pData);
		var value = reader.ReadBer();
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		if (!reader.IsEof())
		{
			return Failure("Integer payload has trailing bytes", reader.GetPosition());
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", value } });
	}

	private static ParseResult DecodeTileLayer(Godot.Collections.Dictionary pChunk, int pTileCount, string pName)
	{
		if (pChunk.Count == 0)
		{
			return new ParseResult(true, null, new Godot.Collections.Dictionary { { "tiles", Array.Empty<int>() } });
		}
		var bytes = (byte[])pChunk["data"];
		var expectedSize = pTileCount * 2;
		if (bytes.Length != expectedSize)
		{
			return Failure($"{pName} tile layer has {bytes.Length} bytes, expected {expectedSize}",
				(int)pChunk["payload_offset"]);
		}
		var tiles = new int[pTileCount];
		for (var index = 0; index < pTileCount; index++)
		{
			tiles[index] = bytes[index * 2] | (bytes[index * 2 + 1] << 8);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "tiles", tiles } });
	}

	private string DecodeTextField(Godot.Collections.Dictionary pFields, int pId)
	{
		if (!pFields.TryGetValue(pId, out var chunk))
		{
			return "";
		}
		return _textDecoder.Decode((byte[])((Godot.Collections.Dictionary)chunk)["data"]);
	}

	private static Godot.Collections.Dictionary GetField(Godot.Collections.Dictionary pFields, int pId)
	{
		return pFields.TryGetValue(pId, out var chunk) ? (Godot.Collections.Dictionary)chunk : new Godot.Collections.Dictionary();
	}

	private static ParseResult ReaderFailure(LcfBinaryReader pReader)
	{
		return Failure(pReader.ErrorMessage, pReader.ErrorOffset);
	}

	private static ParseResult Failure(string pMessage, int pOffset = -1)
	{
		return new ParseResult(false, new ParseError(pOffset, pMessage));
	}
}