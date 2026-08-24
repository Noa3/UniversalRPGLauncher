using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Core;

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
	public const int MaxLmtMaps = 100_000;
	public const int MaxLmtTreeOrder = 100_000;
	public const int MaxLmtStringBytes = 1024 * 1024;

	public const string LdbHeader = "LcfDataBase";
	public const string LmuHeader = "LcfMapUnit";
	public const string LsdHeader = "LcfSaveData";
	public const string LmtHeader = "LcfMapTree";

	public static readonly Dictionary<int, string> LmtMapFieldNames = new()
	{
		{ 0x01, "name" },
		{ 0x02, "parent_id" },
		{ 0x03, "indentation" },
		{ 0x04, "type" },
	};

	public static readonly Dictionary<int, string> LmtStartFieldNames = new()
	{
		{ 0x01, "party_map_id" },
		{ 0x02, "party_x" },
		{ 0x03, "party_y" },
		{ 0x0b, "boat_map_id" },
		{ 0x0c, "boat_x" },
		{ 0x0d, "boat_y" },
		{ 0x15, "ship_map_id" },
		{ 0x16, "ship_x" },
		{ 0x17, "ship_y" },
		{ 0x1f, "airship_map_id" },
		{ 0x20, "airship_x" },
		{ 0x21, "airship_y" },
	};

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

	public const int MaxLdbStringBytes = 1024 * 1024;

	// Field IDs verified against EasyRPG liblcf src/generated/lcf/ldb/chunks.h
	// (struct ChunkActor). Only scalar header fields are decoded; nested
	// structures (parameters, equipment, skills) stay raw for later cards.
	public static readonly Dictionary<int, string> LdbActorFieldNames = new()
	{
		{ 0x01, "name" },
		{ 0x02, "title" },
		{ 0x03, "character_name" },
		{ 0x04, "character_index" },
		{ 0x05, "transparent" },
		{ 0x07, "initial_level" },
		{ 0x08, "final_level" },
		{ 0x09, "critical_hit" },
		{ 0x0a, "critical_hit_chance" },
		{ 0x0f, "face_name" },
		{ 0x10, "face_index" },
	};

	// struct ChunkSwitch and struct ChunkVariable contain only the name field.
	public static readonly Dictionary<int, string> LdbNamedEntryFieldNames = new()
	{
		{ 0x01, "name" },
	};

	// Scalar fields verified against EasyRPG liblcf's generated LDB contract.
	// Nested arrays/structures are intentionally left in unknown_fields until a
	// dedicated bounded decoder exists for their element type.
	private static readonly Dictionary<int, Dictionary<int, string>> LdbScalarFieldNames = new()
	{
		{
			0x0c,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "description" }, { 0x03, "using_message1" }, { 0x04, "using_message2" },
				{ 0x07, "failure_message" }, { 0x08, "type" }, { 0x09, "sp_type" }, { 0x0a, "sp_percent" },
				{ 0x0b, "sp_cost" }, { 0x0c, "scope" }, { 0x0d, "switch_id" }, { 0x0e, "animation_id" },
				{ 0x12, "occasion_field" }, { 0x13, "occasion_battle" }, { 0x14, "reverse_state_effect" },
				{ 0x15, "physical_rate" }, { 0x16, "magical_rate" }, { 0x17, "variance" }, { 0x18, "power" },
				{ 0x19, "hit" }, { 0x1f, "affect_hp" }, { 0x20, "affect_sp" }, { 0x21, "affect_attack" },
				{ 0x22, "affect_defense" }, { 0x23, "affect_spirit" }, { 0x24, "affect_agility" },
				{ 0x25, "absorb_damage" }, { 0x26, "ignore_defense" }, { 0x2d, "affect_attr_defence" },
			}
		},
		{
			0x0d,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "description" }, { 0x03, "type" }, { 0x05, "price" }, { 0x06, "uses" },
				{ 0x0b, "atk_points1" }, { 0x0c, "def_points1" }, { 0x0d, "spi_points1" }, { 0x0e, "agi_points1" },
				{ 0x0f, "two_handed" }, { 0x10, "sp_cost" }, { 0x11, "hit" }, { 0x12, "critical_hit" },
				{ 0x14, "animation_id" }, { 0x15, "preemptive" }, { 0x16, "dual_attack" }, { 0x17, "attack_all" },
				{ 0x18, "ignore_evasion" }, { 0x19, "prevent_critical" }, { 0x1a, "raise_evasion" },
				{ 0x1b, "half_sp_cost" }, { 0x1c, "no_terrain_damage" }, { 0x1d, "cursed" }, { 0x1f, "entire_party" },
				{ 0x20, "recover_hp_rate" }, { 0x21, "recover_hp" }, { 0x22, "recover_sp_rate" }, { 0x23, "recover_sp" },
				{ 0x25, "occasion_field1" }, { 0x26, "ko_only" }, { 0x29, "max_hp_points" }, { 0x2a, "max_sp_points" },
				{ 0x2b, "atk_points2" }, { 0x2c, "def_points2" }, { 0x2d, "spi_points2" }, { 0x2e, "agi_points2" },
				{ 0x33, "using_message" }, { 0x35, "skill_id" }, { 0x37, "switch_id" }, { 0x39, "occasion_field2" },
				{ 0x3a, "occasion_battle" }, { 0x3d, "actor_set_size" }, { 0x3f, "state_set_size" },
				{ 0x41, "attribute_set_size" }, { 0x43, "state_chance" }, { 0x45, "weapon_animation" },
				{ 0x4b, "ranged_trajectory" }, { 0x4c, "ranged_target" },
			}
		},
		{
			0x0e,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "battler_name" }, { 0x03, "battler_hue" },
				{ 0x04, "max_hp" }, { 0x05, "max_sp" }, { 0x06, "attack" }, { 0x07, "defense" },
				{ 0x08, "spirit" }, { 0x09, "agility" }, { 0x0a, "transparent" }, { 0x0b, "exp" },
				{ 0x0c, "gold" }, { 0x0d, "drop_id" }, { 0x0e, "drop_prob" }, { 0x15, "critical_hit" },
				{ 0x16, "critical_hit_chance" }, { 0x1a, "miss" }, { 0x1c, "levitate" },
				{ 0x1f, "state_ranks_size" }, { 0x21, "attribute_ranks_size" }, { 0x0f, "maniac_unarmed_animation" },
			}
		},
		{
			0x0f,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x03, "auto_alignment" }, { 0x04, "terrain_set_size" },
				{ 0x06, "appear_randomly" },
			}
		},
		{
			0x10,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "damage" }, { 0x03, "encounter_rate" }, { 0x04, "background_name" },
				{ 0x05, "boat_pass" }, { 0x06, "ship_pass" }, { 0x07, "airship_pass" }, { 0x09, "airship_land" },
				{ 0x0b, "bush_depth" }, { 0x10, "on_damage_se" }, { 0x11, "background_type" },
				{ 0x16, "background_a_scrollh" }, { 0x17, "background_a_scrollv" }, { 0x18, "background_a_scrollh_speed" },
				{ 0x19, "background_a_scrollv_speed" }, { 0x1e, "background_b" }, { 0x20, "background_b_scrollh" },
				{ 0x21, "background_b_scrollv" }, { 0x22, "background_b_scrollh_speed" }, { 0x23, "background_b_scrollv_speed" },
				{ 0x28, "special_flags" }, { 0x29, "special_back_party" }, { 0x2a, "special_back_enemies" },
				{ 0x2b, "special_lateral_party" }, { 0x2c, "special_lateral_enemies" }, { 0x2d, "grid_location" },
				{ 0x2e, "grid_top_y" }, { 0x2f, "grid_elongation" }, { 0x30, "grid_inclination" },
			}
		},
		{
			0x11,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "type" }, { 0x0b, "a_rate" }, { 0x0c, "b_rate" },
				{ 0x0d, "c_rate" }, { 0x0e, "d_rate" }, { 0x0f, "e_rate" },
			}
		},
		{
			0x13,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "animation_name" }, { 0x03, "large" },
				{ 0x09, "scope" }, { 0x0a, "position" },
			}
		},
		{
			0x14,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "chipset_name" }, { 0x0b, "animation_type" },
				{ 0x0c, "animation_speed" },
			}
		},
		{
			0x12,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x02, "type" }, { 0x03, "color" }, { 0x04, "priority" }, { 0x05, "restriction" },
				{ 0x0b, "a_rate" }, { 0x0c, "b_rate" }, { 0x0d, "c_rate" }, { 0x0e, "d_rate" }, { 0x0f, "e_rate" },
				{ 0x15, "hold_turn" }, { 0x16, "auto_release_prob" }, { 0x17, "release_by_damage" },
				{ 0x1e, "affect_type" }, { 0x1f, "affect_attack" }, { 0x20, "affect_defense" }, { 0x21, "affect_spirit" },
				{ 0x22, "affect_agility" }, { 0x23, "reduce_hit_ratio" }, { 0x24, "avoid_attacks" }, { 0x25, "reflect_magic" },
				{ 0x26, "cursed" }, { 0x27, "battler_animation_id" }, { 0x29, "restrict_skill" },
				{ 0x2a, "restrict_skill_level" }, { 0x2b, "restrict_magic" }, { 0x2c, "restrict_magic_level" },
				{ 0x2d, "hp_change_type" }, { 0x2e, "sp_change_type" }, { 0x33, "message_actor" }, { 0x34, "message_enemy" },
				{ 0x35, "message_already" }, { 0x36, "message_affected" }, { 0x37, "message_recovery" },
				{ 0x3d, "hp_change_max" }, { 0x3e, "hp_change_val" }, { 0x3f, "hp_change_map_steps" },
				{ 0x40, "hp_change_map_val" }, { 0x41, "sp_change_max" }, { 0x42, "sp_change_val" },
				{ 0x43, "sp_change_map_steps" }, { 0x44, "sp_change_map_val" },
			}
		},
		{
			0x1e,
			new Dictionary<int, string>
			{
				{ 0x01, "name" }, { 0x15, "two_weapon" }, { 0x16, "lock_equipment" }, { 0x17, "auto_battle" },
				{ 0x18, "super_guard" }, { 0x29, "exp_base" }, { 0x2a, "exp_inflation" }, { 0x2b, "exp_correction" },
				{ 0x3e, "battler_animation" }, { 0x47, "state_ranks_size" }, { 0x49, "attribute_ranks_size" },
			}
		},
	};

	private static readonly Dictionary<int, HashSet<int>> LdbScalarStringFields = new()
	{
		{ 0x0c, new HashSet<int> { 0x01, 0x02, 0x03, 0x04 } },
		{ 0x0d, new HashSet<int> { 0x01, 0x02 } },
		{ 0x0e, new HashSet<int> { 0x01, 0x02 } },
		{ 0x0f, new HashSet<int> { 0x01 } },
		{ 0x10, new HashSet<int> { 0x01, 0x04 } },
		{ 0x11, new HashSet<int> { 0x01 } },
		{ 0x12, new HashSet<int> { 0x01, 0x33, 0x34, 0x35, 0x36, 0x37 } },
		{ 0x13, new HashSet<int> { 0x01, 0x02 } },
		{ 0x14, new HashSet<int> { 0x01, 0x02 } },
		{ 0x1e, new HashSet<int> { 0x01 } },
	};

	private static readonly Dictionary<int, string> LdbBattleCommandsFieldNames = new()
	{
		{ 0x02, "placement" }, { 0x04, "death_handler_unused" }, { 0x06, "row" },
		{ 0x07, "battle_type" }, { 0x09, "unused_display_normal_parameters" },
		{ 0x0f, "death_handler" }, { 0x10, "death_event" }, { 0x14, "window_size" },
		{ 0x18, "transparency" }, { 0x19, "death_teleport" }, { 0x1a, "death_teleport_id" },
		{ 0x1b, "death_teleport_x" }, { 0x1c, "death_teleport_y" }, { 0x1d, "death_teleport_face" },
		{ 0xc8, "default_atb_mode" }, { 0xc9, "enable_battle_row_command" },
		{ 0xca, "sequential_order" }, { 0xcb, "disable_row_feature" },
		{ 0xcc, "fixed_actor_facing_direction" }, { 0xcd, "fixed_enemy_facing_direction" },
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
		var unknownChunks = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var actors = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var skills = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var items = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var enemies = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var terrains = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var attributes = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var troops = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var animations = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var chipsets = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var states = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var classes = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var switches = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var variables = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var battleCommands = new Godot.Collections.Dictionary();
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
				var typed = id == 0x0b || id == 0x17 || id == 0x18 || LdbScalarFieldNames.ContainsKey(id);
				var arrayResult = ParseStructArray((byte[])chunk["data"], typed);
				if (!arrayResult.Success)
				{
					return Failure($"Invalid {sectionName} section: {arrayResult.Error!.Message}",
						(int)chunk["payload_offset"] + Math.Max(arrayResult.Error.Offset, 0));
				}
				section["count"] = (int)arrayResult.Data["count"];
				sectionCounts[sectionName] = (int)arrayResult.Data["count"];
				if (typed)
				{
					var decodeResult = DecodeTypedLdbSection(id,
						(Godot.Collections.Array<Godot.Collections.Dictionary>)arrayResult.Data["objects"]);
					if (!decodeResult.Success)
					{
						return Failure($"{sectionName} section: {decodeResult.Error!.Message}",
							(int)chunk["payload_offset"] + Math.Max(decodeResult.Error.Offset, 0));
					}
					if (id == 0x0b)
					{
						actors = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x17)
					{
						switches = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x18)
					{
						variables = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x0c)
					{
						skills = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x0d)
					{
						items = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x0e)
					{
						enemies = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x10)
					{
						terrains = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x11)
					{
						attributes = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x0f)
					{
						troops = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x13)
					{
						animations = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x14)
					{
						chipsets = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x12)
					{
						states = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
					else if (id == 0x1e)
					{
						classes = (Godot.Collections.Array<Godot.Collections.Dictionary>)decodeResult.Data["entries"];
					}
				}
			}
			if (id == 0x1d)
			{
				var battleResult = DecodeLdbBattleCommands((byte[])chunk["data"]);
				if (!battleResult.Success)
				{
					return Failure($"{sectionName} section: {battleResult.Error!.Message}",
						(int)chunk["payload_offset"] + Math.Max(battleResult.Error.Offset, 0));
				}
				battleCommands = (Godot.Collections.Dictionary)battleResult.Data["entry"];
				section["count"] = 1;
				sectionCounts[sectionName] = 1;
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
			{ "actors", actors },
			{ "skills", skills },
			{ "items", items },
			{ "enemies", enemies },
			{ "terrains", terrains },
			{ "attributes", attributes },
			{ "troops", troops },
			{ "animations", animations },
			{ "chipsets", chipsets },
			{ "states", states },
			{ "classes", classes },
			{ "switches", switches },
			{ "variables", variables },
			{ "battle_commands", battleCommands },
			{ "version", version },
			{ "engine_family", engineFamily },
		});
	}

	private ParseResult DecodeTypedLdbSection(
		int pSectionId,
		Godot.Collections.Array<Godot.Collections.Dictionary> pObjects
	)
	{
		if (pSectionId == 0x0b)
		{
			return DecodeLdbActorEntries(pObjects);
		}
		if (pSectionId == 0x17 || pSectionId == 0x18)
		{
			return DecodeLdbNamedEntries(pObjects);
		}
		return DecodeLdbScalarEntries(pSectionId, pObjects);
	}

	private static ParseResult DecodeLdbBattleCommands(byte[] pData)
	{
		var reader = new LcfBinaryReader(pData);
		var fieldsResult = ReadStructFields(reader, true);
		if (!fieldsResult.Success)
		{
			return fieldsResult;
		}
		if (!reader.IsEof())
		{
			return Failure("Battle commands section has trailing data", reader.GetPosition());
		}

		var entry = new Godot.Collections.Dictionary
		{
			{ "unknown_fields", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
		};
		var unknownFields = (Godot.Collections.Array<Godot.Collections.Dictionary>)entry["unknown_fields"];
		foreach (var field in (Godot.Collections.Array<Godot.Collections.Dictionary>)fieldsResult.Data["fields"])
		{
			var fieldId = (int)field["id"];
			if (!LdbBattleCommandsFieldNames.TryGetValue(fieldId, out var fieldName))
			{
				unknownFields.Add(field);
				continue;
			}
			var integerResult = DecodeLdbIntegerField((byte[])field["data"], $"battle command {fieldName}");
			if (!integerResult.Success)
			{
				return integerResult;
			}
			entry[fieldName] = integerResult.Data["value"];
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "entry", entry } });
	}

	private ParseResult DecodeLdbScalarEntries(
		int pSectionId,
		Godot.Collections.Array<Godot.Collections.Dictionary> pObjects
	)
	{
		if (!LdbScalarFieldNames.TryGetValue(pSectionId, out var fieldNames))
		{
			return Failure($"No scalar field contract exists for LDB section 0x{pSectionId:X}");
		}
		var stringFields = LdbScalarStringFields[pSectionId];
		var entries = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var seenIds = new HashSet<int>();
		foreach (var pObject in pObjects)
		{
			var objectId = (int)pObject["id"];
			if (!seenIds.Add(objectId))
			{
				return Failure($"Duplicate structure ID {objectId}", 0);
			}
			var entry = new Godot.Collections.Dictionary
			{
				{ "id", objectId },
				{ "unknown_fields", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
			};
			var unknownFields = (Godot.Collections.Array<Godot.Collections.Dictionary>)entry["unknown_fields"];
			foreach (var field in (Godot.Collections.Array<Godot.Collections.Dictionary>)pObject["fields"])
			{
				var fieldId = (int)field["id"];
				if (!fieldNames.TryGetValue(fieldId, out var fieldName))
				{
					unknownFields.Add(field);
					continue;
				}
				var fieldData = (byte[])field["data"];
				if (stringFields.Contains(fieldId))
				{
					var textResult = DecodeLdbString(fieldData, $"{fieldName} {objectId}");
					if (!textResult.Success)
					{
						return textResult;
					}
					entry[fieldName] = textResult.Data["value"];
				}
				else
				{
					var integerResult = DecodeLdbIntegerField(fieldData, $"{fieldName} {objectId}");
					if (!integerResult.Success)
					{
						return integerResult;
					}
					entry[fieldName] = integerResult.Data["value"];
				}
			}
			entries.Add(entry);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "entries", entries } });
	}

	private ParseResult DecodeLdbActorEntries(
		Godot.Collections.Array<Godot.Collections.Dictionary> pObjects
	)
	{
		var entries = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var pObject in pObjects)
		{
			// Defaults mirror the lcf::rpg::Actor member initializers so absent
			// chunks decode exactly like liblcf would.
			var entry = new Godot.Collections.Dictionary
			{
				{ "id", (int)pObject["id"] },
				{ "name", "" },
				{ "title", "" },
				{ "character_name", "" },
				{ "face_name", "" },
				{ "character_index", 0 },
				{ "transparent", 0 },
				{ "initial_level", 1 },
				{ "final_level", -1 },
				{ "critical_hit", 1 },
				{ "critical_hit_chance", 30 },
				{ "face_index", 0 },
				{ "unknown_fields", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
			};
			var unknownFields = (Godot.Collections.Array<Godot.Collections.Dictionary>)entry["unknown_fields"];
			foreach (var field in (Godot.Collections.Array<Godot.Collections.Dictionary>)pObject["fields"])
			{
				var fieldId = (int)field["id"];
				if (!LdbActorFieldNames.TryGetValue(fieldId, out var fieldName))
				{
					unknownFields.Add(field);
					continue;
				}
				var fieldData = (byte[])field["data"];
				if (fieldId is 0x01 or 0x02 or 0x03 or 0x0f)
				{
					var textResult = DecodeLdbString(fieldData, $"actor {fieldName}");
					if (!textResult.Success)
					{
						return textResult;
					}
					entry[fieldName] = textResult.Data["value"];
				}
				else
				{
					var integerResult = DecodeLdbIntegerField(fieldData, $"actor {fieldName}");
					if (!integerResult.Success)
					{
						return integerResult;
					}
					entry[fieldName] = integerResult.Data["value"];
				}
			}
			entries.Add(entry);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "entries", entries } });
	}

	private ParseResult DecodeLdbNamedEntries(
		Godot.Collections.Array<Godot.Collections.Dictionary> pObjects
	)
	{
		var entries = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var seenIds = new HashSet<int>();
		foreach (var pObject in pObjects)
		{
			var objectId = (int)pObject["id"];
			if (!seenIds.Add(objectId))
			{
				return Failure($"Duplicate structure ID {objectId}", 0);
			}
			var entry = new Godot.Collections.Dictionary
			{
				{ "id", objectId },
				{ "name", "" },
				{ "unknown_fields", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
			};
			var unknownFields = (Godot.Collections.Array<Godot.Collections.Dictionary>)entry["unknown_fields"];
			foreach (var field in (Godot.Collections.Array<Godot.Collections.Dictionary>)pObject["fields"])
			{
				var fieldId = (int)field["id"];
				if (!LdbNamedEntryFieldNames.TryGetValue(fieldId, out var fieldName))
				{
					unknownFields.Add(field);
					continue;
				}
				var textResult = DecodeLdbString((byte[])field["data"], $"{fieldName} {objectId}");
				if (!textResult.Success)
				{
					return textResult;
				}
				entry[fieldName] = textResult.Data["value"];
			}
			entries.Add(entry);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "entries", entries } });
	}

	private ParseResult DecodeLdbString(byte[] pData, string pLabel)
	{
		if (pData.Length > MaxLdbStringBytes)
		{
			return Failure($"LDB {pLabel} exceeds {MaxLdbStringBytes}-byte limit");
		}
		var value = _textDecoder.Decode(pData);
		if (string.IsNullOrEmpty(value) && pData.Length > 0)
		{
			return Failure("Unable to decode LDB " + pLabel);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", value } });
	}

	private static ParseResult DecodeLdbIntegerField(byte[] pData, string pLabel)
	{
		if (pData.Length == 0)
		{
			return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", 0 } });
		}
		var reader = new LcfBinaryReader(pData);
		var value = reader.ReadSignedBer();
		if (reader.HasError())
		{
			return Failure($"Invalid LDB {pLabel}: {reader.ErrorMessage}", reader.ErrorOffset);
		}
		if (!reader.IsEof())
		{
			return Failure($"LDB {pLabel} has trailing bytes", reader.GetPosition());
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", value } });
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

		var events = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		if (fields.TryGetValue(0x51, out var eventChunk))
		{
			var eventChunkData = (Godot.Collections.Dictionary)eventChunk;
			var eventArray = ParseStructArray((byte[])eventChunkData["data"], true);
			if (!eventArray.Success)
			{
				return Failure($"Invalid map events: {eventArray.Error!.Message}",
					(int)eventChunkData["payload_offset"] + Math.Max(eventArray.Error.Offset, 0));
			}
			foreach (var eventObject in (Godot.Collections.Array<Godot.Collections.Dictionary>)eventArray.Data["objects"])
			{
				var eventFields = ChunksById((Godot.Collections.Array<Godot.Collections.Dictionary>)eventObject["fields"]);
				var xResult = IntegerFromFields(eventFields, 0x02, 0);
				var yResult = IntegerFromFields(eventFields, 0x03, 0);
				if (!xResult.Success || !yResult.Success)
				{
					return Failure("Invalid event coordinates", (int)eventChunkData["payload_offset"]);
				}
				var pageCount = 0;
				if (eventFields.TryGetValue(0x05, out var pageChunk))
				{
					var pageChunkData = (Godot.Collections.Dictionary)pageChunk;
					var pages = ParseStructArray((byte[])pageChunkData["data"], false);
					if (!pages.Success)
					{
						return Failure($"Invalid event pages: {pages.Error!.Message}", (int)eventChunkData["payload_offset"]);
					}
					pageCount = (int)pages.Data["count"];

					var pageList = new Godot.Collections.Array<Godot.Collections.Dictionary>();
					foreach (var pageObj in (Godot.Collections.Array<Godot.Collections.Dictionary>)pages.Data["objects"])
					{
						var pageFields = ChunksById((Godot.Collections.Array<Godot.Collections.Dictionary>)pageObj["fields"]);
						var triggerResult = IntegerFromFields(pageFields, 0x21, 0);
						if (!triggerResult.Success) triggerResult = IntegerFromFields(pageFields, 0x09, 0);
						var priorityResult = IntegerFromFields(pageFields, 0x22, 0);
						if (!priorityResult.Success) priorityResult = IntegerFromFields(pageFields, 0x08, 0);
						var freqResult = IntegerFromFields(pageFields, 0x20, 0);
						if (!freqResult.Success) freqResult = IntegerFromFields(pageFields, 0x06, 0);

						if (!triggerResult.Success || !priorityResult.Success || !freqResult.Success)
						{
							return Failure($"Invalid page metadata", (int)pageChunkData["payload_offset"]);
						}

						var conditionData = new Godot.Collections.Dictionary();
						if (pageFields.TryGetValue(0x02, out var conditionChunk))
						{
							var conditionFields = ChunksById((Godot.Collections.Array<Godot.Collections.Dictionary>)((Godot.Collections.Dictionary)conditionChunk)["fields"]);
							var conditionResult = Rm2kEventPageConditionDecoder.Decode(conditionFields);
							if (!conditionResult.Success) return conditionResult;
							conditionData = conditionResult.Data;
						}

						var commandChunk = pageFields.ContainsKey(0x34)
							? (Godot.Collections.Dictionary)pageFields[0x34]
							: pageFields.ContainsKey(0x0b) ? (Godot.Collections.Dictionary)pageFields[0x0b] : null;
						var hasList = commandChunk != null;
						Godot.Collections.Array<Godot.Collections.Dictionary> commands = new();
						if (hasList)
						{
							var pg = commandChunk!;
							var commandResult = Rm2kEventCommandDecoder.Decode((byte[])pg["data"]);
							if (!commandResult.Success)
							{
								return Failure($"Invalid event command list: {commandResult.Error!.Message}", (int)pg["payload_offset"] + Math.Max(commandResult.Error.Offset, 0));
							}
							commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)commandResult.Data["commands"];
						}

						pageList.Add(new Godot.Collections.Dictionary
						{
							{ "trigger", (int)triggerResult.Data["value"] },
							{ "priority", (int)priorityResult.Data["value"] },
							{ "move_frequency", (int)freqResult.Data["value"] },
							{ "conditions", conditionData },
							{ "has_move_list", hasList },
							{ "has_command_list", hasList },
							{ "commands", commands },
						});
					}
					events.Add(new Godot.Collections.Dictionary
					{
						{ "id", (int)eventObject["id"] },
						{ "name", DecodeTextField(eventFields, 0x01) },
						{ "x", (int)xResult.Data["value"] },
						{ "y", (int)yResult.Data["value"] },
						{ "page_count", pageCount },
						{ "pages", pageList },
					});
				}
				else
				{
					events.Add(new Godot.Collections.Dictionary
					{
						{ "id", (int)eventObject["id"] },
						{ "name", DecodeTextField(eventFields, 0x01) },
						{ "x", (int)xResult.Data["value"] },
						{ "y", (int)yResult.Data["value"] },
						{ "page_count", 0 },
						{ "pages", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
					});
				}
			}
		}

		int[] knownIds =
		{
			0x01, 0x02, 0x03, 0x0b, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x28, 0x29, 0x2a, 0x30,
			0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x3c, 0x3d, 0x3e, 0x47, 0x48, 0x51, 0x5a, 0x5b,
		};
		var unknownChunks = new Godot.Collections.Array<Godot.Collections.Dictionary>();
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

	public ParseResult ParseMapTree(string pPath)
	{
		var loaded = ReadFile(pPath, MaxFileBytes);
		if (!loaded.Success)
		{
			return loaded;
		}
		var bytes = (byte[])loaded.Data["bytes"];
		var reader = new LcfBinaryReader(bytes);
		reader.ReadHeader(LmtHeader);
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}

		var mapCount = reader.ReadBer();
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		if (mapCount < 0 || mapCount > MaxLmtMaps)
		{
			return Failure($"LMT map count {mapCount} exceeds limit", reader.GetPosition());
		}

		var maps = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var mapsById = new Dictionary<int, Godot.Collections.Dictionary>();
		for (var index = 0; index < mapCount; index++)
		{
			var mapId = reader.ReadSignedBer();
			if (reader.HasError())
			{
				return ReaderFailure(reader);
			}
			var fieldsResult = ReadStructFields(reader, true);
			if (!fieldsResult.Success)
			{
				return Failure($"Invalid LMT map {index}: {fieldsResult.Error!.Message}", fieldsResult.Error.Offset);
			}
			if (mapsById.ContainsKey(mapId))
			{
				return Failure($"Duplicate LMT map ID {mapId}", reader.GetPosition());
			}
			var mapResult = DecodeLmtMapInfo(mapId,
				(Godot.Collections.Array<Godot.Collections.Dictionary>)fieldsResult.Data["fields"]);
			if (!mapResult.Success)
			{
				return mapResult;
			}
			var mapInfo = mapResult.Data;
			maps.Add(mapInfo);
			mapsById[mapId] = mapInfo;
		}

		var treeOrderCount = reader.ReadBer();
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		if (treeOrderCount < 0 || treeOrderCount > MaxLmtTreeOrder)
		{
			return Failure($"LMT tree order count {treeOrderCount} exceeds limit", reader.GetPosition());
		}
		var treeOrder = new Godot.Collections.Array<int>();
		for (var index = 0; index < treeOrderCount; index++)
		{
			var mapId = reader.ReadSignedBer();
			if (reader.HasError())
			{
				return ReaderFailure(reader);
			}
			treeOrder.Add(mapId);
		}

		var activeNode = reader.ReadSignedBer();
		if (reader.HasError())
		{
			return ReaderFailure(reader);
		}
		var startResult = ReadStructFields(reader, true);
		if (!startResult.Success)
		{
			return Failure($"Invalid LMT start data: {startResult.Error!.Message}", startResult.Error.Offset);
		}
		var startDecoded = DecodeLmtStart(
			(Godot.Collections.Array<Godot.Collections.Dictionary>)startResult.Data["fields"]);
		if (!startDecoded.Success)
		{
			return startDecoded;
		}
		if (!reader.IsEof())
		{
			return Failure("Trailing data after LMT start structure", reader.GetPosition());
		}

		var referenceResult = ValidateLmtReferences(mapsById, treeOrder, activeNode);
		if (!referenceResult.Success)
		{
			return referenceResult;
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "format", "LMT" },
			{ "header", LmtHeader },
			{ "file_size", (long)bytes.Length },
			{ "map_count", maps.Count },
			{ "maps", maps },
			{ "tree_order", treeOrder },
			{ "active_node", activeNode },
			{ "start", startDecoded.Data["start"] },
			{ "unknown_start_fields", startDecoded.Data["unknown_fields"] },
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
		if (length > (ulong)pLimit)
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
		// RM2K/2003 stores some empty sections as a zero-length LCF payload rather
		// than a BER-encoded count of zero. Both forms represent an empty array.
		if (pData.Length == 0)
		{
			return new ParseResult(true, null, new Godot.Collections.Dictionary
			{
				{ "count", 0 },
				{ "objects", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
			});
		}
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

	private ParseResult DecodeLmtMapInfo(int pMapId, Godot.Collections.Array<Godot.Collections.Dictionary> pFields)
	{
		var mapInfo = new Godot.Collections.Dictionary
		{
			{ "id", pMapId },
			{ "name", "" },
			{ "parent_id", 0 },
			{ "indentation", 0 },
			{ "type", 0 },
			{ "fields", pFields },
			{ "unknown_fields", new Godot.Collections.Array<Godot.Collections.Dictionary>() },
		};
		var unknownFields = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var field in pFields)
		{
			var fieldId = (int)field["id"];
			if (!LmtMapFieldNames.TryGetValue(fieldId, out var fieldName))
			{
				unknownFields.Add(field);
				continue;
			}
			var fieldData = (byte[])field["data"];
			if (fieldId == 0x01)
			{
				var textResult = DecodeLmtString(fieldData, "map name");
				if (!textResult.Success)
				{
					return textResult;
				}
				mapInfo["name"] = textResult.Data["value"];
			}
			else
			{
				var integerResult = DecodeLmtInteger(fieldData, fieldName);
				if (!integerResult.Success)
				{
					return integerResult;
				}
				mapInfo[fieldName] = integerResult.Data["value"];
			}
		}
		mapInfo["unknown_fields"] = unknownFields;
		return new ParseResult(true, null, mapInfo);
	}

	private static ParseResult DecodeLmtStart(Godot.Collections.Array<Godot.Collections.Dictionary> pFields)
	{
		var start = new Godot.Collections.Dictionary();
		var unknownFields = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var field in pFields)
		{
			var fieldId = (int)field["id"];
			if (!LmtStartFieldNames.TryGetValue(fieldId, out var fieldName))
			{
				unknownFields.Add(field);
				continue;
			}
			var integerResult = DecodeLmtInteger((byte[])field["data"], fieldName);
			if (!integerResult.Success)
			{
				return integerResult;
			}
			start[fieldName] = integerResult.Data["value"];
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary
		{
			{ "start", start },
			{ "unknown_fields", unknownFields },
		});
	}

	private ParseResult DecodeLmtString(byte[] pData, string pLabel)
	{
		if (pData.Length > MaxLmtStringBytes)
		{
			return Failure($"LMT {pLabel} exceeds {MaxLmtStringBytes}-byte limit");
		}
		var value = _textDecoder.Decode(pData);
		if (string.IsNullOrEmpty(value) && pData.Length > 0)
		{
			return Failure("Unable to decode LMT " + pLabel);
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", value } });
	}

	private static ParseResult DecodeLmtInteger(byte[] pData, string pLabel)
	{
		if (pData.Length == 0)
		{
			return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", 0 } });
		}
		var reader = new LcfBinaryReader(pData);
		var value = reader.ReadSignedBer();
		if (reader.HasError())
		{
			return Failure($"Invalid LMT {pLabel}: {reader.ErrorMessage}", reader.ErrorOffset);
		}
		if (!reader.IsEof())
		{
			return Failure($"LMT {pLabel} has trailing bytes", reader.GetPosition());
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary { { "value", value } });
	}

	private static ParseResult ValidateLmtReferences(
		Dictionary<int, Godot.Collections.Dictionary> pMapsById,
		Godot.Collections.Array<int> pTreeOrder,
		int pActiveNode
	)
	{
		foreach (var pair in pMapsById)
		{
			var parentId = (int)pair.Value["parent_id"];
			if (parentId != 0 && !pMapsById.ContainsKey(parentId))
			{
				return Failure($"LMT map {pair.Key} references missing parent {parentId}");
			}
		}

		foreach (var mapId in pTreeOrder)
		{
			if (!pMapsById.ContainsKey(mapId))
			{
				return Failure($"LMT tree order references missing map {mapId}");
			}
		}

		if (pMapsById.Count == 0)
		{
			if (pActiveNode != 0)
			{
				return Failure($"Empty LMT map tree has active node {pActiveNode}");
			}
		}
		else if (!pMapsById.ContainsKey(pActiveNode))
		{
			return Failure($"LMT active node {pActiveNode} is not present");
		}

		foreach (var startId in pMapsById.Keys)
		{
			var current = startId;
			var visited = new HashSet<int>();
			while (current != 0)
			{
				if (!visited.Add(current))
				{
					return Failure($"LMT parent cycle includes map {current}");
				}
				if (!pMapsById.TryGetValue(current, out var info))
				{
					return Failure($"LMT parent chain references missing map {current}");
				}
				var parentId = (int)info["parent_id"];
				if (parentId == current)
				{
					return Failure($"LMT map {current} is its own parent");
				}
				current = parentId;
			}
		}
		return new ParseResult(true, null, new Godot.Collections.Dictionary());
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