using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestRm2kParser : TestBase
{
	private const string Dir = "user://rm2k_test";

	private Rm2kParser _parser = null!;

	public override void Setup()
	{
		_parser = new Rm2kParser();
		CreateTestFiles();
	}

	public override void Teardown()
	{
		CleanupTestFiles();
	}

	internal static byte[] Ber(int pValue)
	{
		var value = pValue;
		var groups = new List<byte>();
		while (value >= 0x80)
		{
			groups.Add((byte)(value & 0x7f));
			value >>= 7;
		}
		groups.Add((byte)value);
		var bytes = new List<byte>();
		for (var index = groups.Count - 1; index >= 0; index--)
		{
			var current = groups[index];
			if (index > 0)
			{
				current |= 0x80;
			}
			bytes.Add(current);
		}
		return bytes.ToArray();
	}

	internal static byte[] Chunk(int pId, byte[] pPayload)
	{
		var bytes = new List<byte>();
		bytes.AddRange(Ber(pId));
		bytes.AddRange(Ber(pPayload.Length));
		bytes.AddRange(pPayload);
		return bytes.ToArray();
	}

	internal static byte[] Lcf(string pHeader, List<byte[]> pChunks)
	{
		var bytes = new List<byte>();
		bytes.AddRange(Ber(pHeader.Length));
		bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(pHeader));
		foreach (var chunk in pChunks)
		{
			bytes.AddRange(chunk);
		}
		return bytes.ToArray();
	}

	private static byte[] TileLayer(int pCount, int pStart = 0)
	{
		var bytes = new byte[pCount * 2];
		for (var index = 0; index < pCount; index++)
		{
			var tile = (pStart + index) & 0xFFFF;
			bytes[index * 2] = (byte)(tile & 0xFF);
			bytes[index * 2 + 1] = (byte)((tile >> 8) & 0xFF);
		}
		return bytes;
	}

	private static void WriteFile(string pPath, byte[] pBytes)
	{
		using var file = FileAccess.Open(pPath, FileAccess.ModeFlags.Write);
		file?.StoreBuffer(pBytes);
	}

	private static void CreateTestFiles()
	{
		DirAccess.MakeDirRecursiveAbsolute(Dir);

		WriteFile(Dir.PathJoin("Game.ini"),
			System.Text.Encoding.ASCII.GetBytes("[RPG_RT]\nGameTitle=TestGame\nEngineID=RM2000\nEnginePath=Game.exe\n"));

		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x1a, Ber(259)),
			Chunk(0x0b, Ber(0)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("Data.rdata"), database);

		var map = Lcf("LcfMapUnit", new List<byte[]>
		{
			Chunk(0x01, Ber(1)),
			Chunk(0x02, Ber(20)),
			Chunk(0x03, Ber(15)),
			Chunk(0x47, TileLayer(300)),
			Chunk(0x48, TileLayer(300, 0x8000)),
			Chunk(0x51, Ber(0)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("Map001.rmm"), map);

		var save = Lcf("LcfSaveData", new List<byte[]>
		{
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("TestGame")),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("Save001.rmm"), save);
	}

	private static void CleanupTestFiles()
	{
		if (!DirAccess.DirExistsAbsolute(Dir))
		{
			return;
		}
		using var directory = DirAccess.Open(Dir);
		if (directory == null)
		{
			return;
		}
		foreach (var fileName in directory.GetFiles())
		{
			DirAccess.RemoveAbsolute(Dir.PathJoin(fileName));
		}
		DirAccess.RemoveAbsolute(Dir);
	}

	private static string DescribeError(Rm2kParser.ParseResult pResult)
	{
		return pResult.IsSuccess() ? "" : pResult.GetError()!.Describe();
	}

	// === TESTS: Game.ini Parsing ===

	public void Test_ParseGameIniSuccess()
	{
		var result = _parser.ParseGameIni(Dir.PathJoin("Game.ini"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		AssertEq(result.GetData()["GameTitle"].AsString(), "TestGame");
		AssertEq(result.GetData()["EngineID"].AsString(), "RM2000");
		AssertEq(result.GetData()["section"].AsString(), "RPG_RT");
	}

	public void Test_ParseGameIniNotFound()
	{
		var result = _parser.ParseGameIni(Dir.PathJoin("NonExistent.ini"));
		AssertFalse(result.IsSuccess());
		AssertNe(result.GetError(), null);
		AssertTrue(result.GetError()!.Message.ToLowerInvariant().Contains("not found"));
	}

	public void Test_ParseGameIniWrongHeader()
	{
		WriteFile(Dir.PathJoin("BadGame.ini"),
			System.Text.Encoding.ASCII.GetBytes("[BadHeader]\nTitle=Test\n"));
		var result = _parser.ParseGameIni(Dir.PathJoin("BadGame.ini"));
		AssertFalse(result.IsSuccess());
	}

	public void Test_ParseGameIniEmptyFile()
	{
		WriteFile(Dir.PathJoin("EmptyGame.ini"), System.Array.Empty<byte>());
		var result = _parser.ParseGameIni(Dir.PathJoin("EmptyGame.ini"));
		AssertFalse(result.IsSuccess());
	}

	// === TESTS: Database Parsing ===

	public void Test_ParseDatabaseSuccess()
	{
		var result = _parser.ParseDatabase(Dir.PathJoin("Data.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		var data = result.GetData();
		AssertEq(data["format"].AsString(), "LDB");
		AssertEq(data["header"].AsString(), "LcfDataBase");
		AssertEq(data["version"].AsInt32(), 259);
		AssertEq(data["engine_family"].AsString(), "RPG Maker 2000");
		AssertEq(((Godot.Collections.Dictionary)data["section_counts"])["actors"].AsInt32(), 0);
	}

	public void Test_ParseDatabaseAcceptsEmptyStructArraySection()
	{
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x1f, System.Array.Empty<byte>()),
			Chunk(0x1a, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("EmptyArray.rdata"), database);
		var result = _parser.ParseDatabase(Dir.PathJoin("EmptyArray.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (result.IsSuccess())
		{
			var sectionCounts = (Godot.Collections.Dictionary)result.GetData()["section_counts"];
			AssertEq(sectionCounts.TryGetValue("class_duplicate", out var count) ? count.AsInt32() : -1, 0);
		}
	}

	public void Test_ParseDatabaseRetainsUnknownTopLevelChunks()
	{
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x99, new byte[] { 0x01, 0x02, 0x03 }),
			Chunk(0x1a, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("UnknownChunk.rdata"), database);
		var result = _parser.ParseDatabase(Dir.PathJoin("UnknownChunk.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (result.IsSuccess())
		{
			var unknownChunks = (Godot.Collections.Array)result.GetData()["unknown_chunks"];
			AssertEq(unknownChunks.Count, 1);
			AssertEq(((Godot.Collections.Dictionary)unknownChunks[0])["id"].AsInt32(), 0x99);
		}
	}

	public void Test_ParseDatabaseNotFound()
	{
		var result = _parser.ParseDatabase(Dir.PathJoin("NonExistent.rdata"));
		AssertFalse(result.IsSuccess());
		AssertNe(result.GetError(), null);
	}

	public void Test_ParseDatabaseTooSmall()
	{
		WriteFile(Dir.PathJoin("Tiny.rdata"), System.Text.Encoding.ASCII.GetBytes("abc"));
		var result = _parser.ParseDatabase(Dir.PathJoin("Tiny.rdata"));
		AssertFalse(result.IsSuccess());
	}

	public void Test_ParseDatabaseWrongHeader()
	{
		WriteFile(Dir.PathJoin("Wrong.rdata"),
			Lcf("LcfSomething", new List<byte[]> { new byte[] { 0x00 } }));
		var result = _parser.ParseDatabase(Dir.PathJoin("Wrong.rdata"));
		AssertFalse(result.IsSuccess());
		AssertTrue(result.GetError()!.Message.ToLowerInvariant().Contains("header"));
	}

	public void Test_ParseDatabaseTruncatedChunk()
	{
		var bytes = new List<byte>();
		bytes.AddRange(Ber(11));
		bytes.AddRange(System.Text.Encoding.ASCII.GetBytes("LcfDataBase"));
		bytes.Add(0x1a);
		WriteFile(Dir.PathJoin("Trunc.rdata"), bytes.ToArray());
		var result = _parser.ParseDatabase(Dir.PathJoin("Trunc.rdata"));
		AssertFalse(result.IsSuccess());
	}

	internal static byte[] Struct(params byte[][] pFieldChunks)
	{
		var bytes = new List<byte>();
		foreach (var chunk in pFieldChunks)
		{
			bytes.AddRange(chunk);
		}
		bytes.Add(0x00);
		return bytes.ToArray();
	}

	internal static byte[] StructArray(params byte[][] pObjectFields)
	{
		var bytes = new List<byte>();
		bytes.AddRange(Ber(pObjectFields.Length));
		for (var index = 0; index < pObjectFields.Length; index++)
		{
			bytes.AddRange(Ber(index + 1));
			bytes.AddRange(pObjectFields[index]);
		}
		return bytes.ToArray();
	}

	// === TESTS: Typed LDB Sections ===

	public void Test_ParseDatabaseDecodesTypedActorEntries()
	{
		var actor1 = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Alex")),
			Chunk(0x02, System.Text.Encoding.ASCII.GetBytes("Hero")),
			Chunk(0x07, Ber(5)),
			Chunk(0xF0, new byte[] { 0x09 })
		);
		var actor2 = Struct(Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Brian")));
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x0b, StructArray(actor1, actor2)),
			Chunk(0x1a, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("Actors.rdata"), database);
		var result = _parser.ParseDatabase(Dir.PathJoin("Actors.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		var actors = (Godot.Collections.Array)data["actors"];
		AssertEq(actors.Count, 2, "actor entry count");
		var first = (Godot.Collections.Dictionary)actors[0];
		AssertEq(first["id"].AsInt32(), 1, "first actor id");
		AssertEq(first["name"].AsString(), "Alex", "first actor name");
		AssertEq(first["title"].AsString(), "Hero", "first actor title");
		AssertEq(first["initial_level"].AsInt32(), 5, "first actor level");
		AssertEq(first["final_level"].AsInt32(), -1, "absent final_level default");
		AssertEq(first["critical_hit"].AsInt32(), 1, "absent critical_hit default");
		AssertEq(first["critical_hit_chance"].AsInt32(), 30, "absent critical chance default");
		var unknown = (Godot.Collections.Array)first["unknown_fields"];
		AssertEq(unknown.Count, 1, "unknown actor field retained");
		AssertEq(((Godot.Collections.Dictionary)unknown[0])["id"].AsInt32(), 0xF0, "unknown field id");
		var second = (Godot.Collections.Dictionary)actors[1];
		AssertEq(second["name"].AsString(), "Brian", "second actor name");
		AssertEq(second["initial_level"].AsInt32(), 1, "default initial_level");
		var sectionCounts = (Godot.Collections.Dictionary)data["section_counts"];
		AssertEq(sectionCounts["actors"].AsInt32(), 2, "section count matches entries");
	}

	public void Test_ParseDatabaseDecodesTypedSkillItemStateAndClassEntries()
	{
		var skill = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Fire")),
			Chunk(0x02, System.Text.Encoding.ASCII.GetBytes("Burns one target")),
			Chunk(0x0B, Ber(5)),
			Chunk(0xF0, new byte[] { 0x01 })
		);
		var item = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Potion")),
			Chunk(0x05, Ber(50)),
			Chunk(0x1F, Ber(1)),
			Chunk(0x33, Ber(2))
		);
		var state = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Poison")),
			Chunk(0x17, Ber(20)),
			Chunk(0x33, System.Text.Encoding.ASCII.GetBytes("is poisoned"))
		);
		var cls = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Hero")),
			Chunk(0x15, Ber(1)),
			Chunk(0x29, Ber(30))
		);
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x0C, StructArray(skill)),
			Chunk(0x0D, StructArray(item)),
			Chunk(0x12, StructArray(state)),
			Chunk(0x1E, StructArray(cls)),
			Chunk(0x1A, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("TypedCoreSections.rdata"), database);

		var result = _parser.ParseDatabase(Dir.PathJoin("TypedCoreSections.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		var skills = (Godot.Collections.Array)data["skills"];
		var items = (Godot.Collections.Array)data["items"];
		var states = (Godot.Collections.Array)data["states"];
		var classes = (Godot.Collections.Array)data["classes"];
		AssertEq(skills.Count, 1, "skill entry count");
		AssertEq(items.Count, 1, "item entry count");
		AssertEq(states.Count, 1, "state entry count");
		AssertEq(classes.Count, 1, "class entry count");
		AssertEq(((Godot.Collections.Dictionary)skills[0])["name"].AsString(), "Fire", "skill name");
		AssertEq(((Godot.Collections.Dictionary)skills[0])["sp_cost"].AsInt32(), 5, "skill SP cost");
		AssertEq(((Godot.Collections.Array)((Godot.Collections.Dictionary)skills[0])["unknown_fields"]).Count, 1,
			"skill unknown field retained");
		AssertEq(((Godot.Collections.Dictionary)items[0])["price"].AsInt32(), 50, "item price");
		AssertEq(((Godot.Collections.Dictionary)items[0])["entire_party"].AsInt32(), 1, "item flag");
		AssertEq(((Godot.Collections.Dictionary)items[0])["using_message"].AsInt32(), 2, "item using message");
		AssertEq(((Godot.Collections.Dictionary)states[0])["release_by_damage"].AsInt32(), 20, "state release chance");
		AssertEq(((Godot.Collections.Dictionary)states[0])["message_actor"].AsString(), "is poisoned", "state message");
		AssertEq(((Godot.Collections.Dictionary)classes[0])["name"].AsString(), "Hero", "class name");
		AssertEq(((Godot.Collections.Dictionary)classes[0])["exp_base"].AsInt32(), 30, "class exp base");
		var sectionCounts = (Godot.Collections.Dictionary)data["section_counts"];
		foreach (var section in new[] { "skills", "items", "states", "classes" })
		{
			AssertEq(sectionCounts[section].AsInt32(), 1, section + " section count");
		}
	}

	public void Test_ParseDatabaseDecodesTypedEnemyTerrainAndAttributeEntries()
	{
		var enemy = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Slime")),
			Chunk(0x02, System.Text.Encoding.ASCII.GetBytes("Slime")),
			Chunk(0x04, Ber(120)),
			Chunk(0x0C, Ber(25)),
			Chunk(0x2A, new byte[] { 0x01 })
		);
		var terrain = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Grass")),
			Chunk(0x02, Ber(3)),
			Chunk(0x03, Ber(20)),
			Chunk(0x04, System.Text.Encoding.ASCII.GetBytes("Forest"))
		);
		var attribute = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Fire")),
			Chunk(0x02, Ber(1)),
			Chunk(0x0B, Ber(2)),
			Chunk(0x0F, Ber(4))
		);
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x0E, StructArray(enemy)),
			Chunk(0x10, StructArray(terrain)),
			Chunk(0x11, StructArray(attribute)),
			Chunk(0x1A, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("TypedCombatSections.rdata"), database);

		var result = _parser.ParseDatabase(Dir.PathJoin("TypedCombatSections.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		var enemies = (Godot.Collections.Array)data["enemies"];
		var terrains = (Godot.Collections.Array)data["terrains"];
		var attributes = (Godot.Collections.Array)data["attributes"];
		AssertEq(enemies.Count, 1, "enemy entry count");
		AssertEq(terrains.Count, 1, "terrain entry count");
		AssertEq(attributes.Count, 1, "attribute entry count");
		var enemyEntry = (Godot.Collections.Dictionary)enemies[0];
		AssertEq(enemyEntry["name"].AsString(), "Slime", "enemy name");
		AssertEq(enemyEntry["max_hp"].AsInt32(), 120, "enemy max HP");
		AssertEq(enemyEntry["gold"].AsInt32(), 25, "enemy gold");
		AssertEq(((Godot.Collections.Array)enemyEntry["unknown_fields"]).Count, 1, "enemy unknown field retained");
		var terrainEntry = (Godot.Collections.Dictionary)terrains[0];
		AssertEq(terrainEntry["name"].AsString(), "Grass", "terrain name");
		AssertEq(terrainEntry["damage"].AsInt32(), 3, "terrain damage");
		AssertEq(terrainEntry["background_name"].AsString(), "Forest", "terrain background");
		var attributeEntry = (Godot.Collections.Dictionary)attributes[0];
		AssertEq(attributeEntry["name"].AsString(), "Fire", "attribute name");
		AssertEq(attributeEntry["type"].AsInt32(), 1, "attribute type");
		AssertEq(attributeEntry["a_rate"].AsInt32(), 2, "attribute A rate");
		AssertEq(attributeEntry["e_rate"].AsInt32(), 4, "attribute E rate");
		var sectionCounts = (Godot.Collections.Dictionary)data["section_counts"];
		foreach (var section in new[] { "enemies", "terrains", "attributes" })
		{
			AssertEq(sectionCounts[section].AsInt32(), 1, section + " section count");
		}
	}

	public void Test_ParseDatabaseDecodesTypedTroopAnimationAndChipsetEntries()
	{
		var troop = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Slime Group")),
			Chunk(0x03, Ber(1)),
			Chunk(0x04, Ber(3)),
			Chunk(0x06, Ber(0)),
			Chunk(0x02, new byte[] { 0x01 })
		);
		var animation = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Fire Hit")),
			Chunk(0x02, System.Text.Encoding.ASCII.GetBytes("Fire")),
			Chunk(0x03, Ber(1)),
			Chunk(0x09, Ber(2)),
			Chunk(0x0A, Ber(3)),
			Chunk(0x06, new byte[] { 0x01 })
		);
		var chipset = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Outdoor")),
			Chunk(0x02, System.Text.Encoding.ASCII.GetBytes("Chipset01")),
			Chunk(0x0B, Ber(1)),
			Chunk(0x0C, Ber(2)),
			Chunk(0x03, new byte[] { 0x01 })
		);
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x0F, StructArray(troop)),
			Chunk(0x13, StructArray(animation)),
			Chunk(0x14, StructArray(chipset)),
			Chunk(0x1A, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("TypedPresentationSections.rdata"), database);

		var result = _parser.ParseDatabase(Dir.PathJoin("TypedPresentationSections.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		var troops = (Godot.Collections.Array)data["troops"];
		var animations = (Godot.Collections.Array)data["animations"];
		var chipsets = (Godot.Collections.Array)data["chipsets"];
		AssertEq(troops.Count, 1, "troop entry count");
		AssertEq(animations.Count, 1, "animation entry count");
		AssertEq(chipsets.Count, 1, "chipset entry count");
		var troopEntry = (Godot.Collections.Dictionary)troops[0];
		AssertEq(troopEntry["name"].AsString(), "Slime Group", "troop name");
		AssertEq(troopEntry["auto_alignment"].AsInt32(), 1, "troop auto alignment");
		AssertEq(troopEntry["terrain_set_size"].AsInt32(), 3, "troop terrain set size");
		AssertEq(((Godot.Collections.Array)troopEntry["unknown_fields"]).Count, 1, "troop nested field retained");
		var animationEntry = (Godot.Collections.Dictionary)animations[0];
		AssertEq(animationEntry["name"].AsString(), "Fire Hit", "animation name");
		AssertEq(animationEntry["animation_name"].AsString(), "Fire", "animation asset name");
		AssertEq(animationEntry["scope"].AsInt32(), 2, "animation scope");
		AssertEq(((Godot.Collections.Array)animationEntry["unknown_fields"]).Count, 1, "animation nested field retained");
		var chipsetEntry = (Godot.Collections.Dictionary)chipsets[0];
		AssertEq(chipsetEntry["name"].AsString(), "Outdoor", "chipset name");
		AssertEq(chipsetEntry["chipset_name"].AsString(), "Chipset01", "chipset asset name");
		AssertEq(chipsetEntry["animation_speed"].AsInt32(), 2, "chipset animation speed");
		AssertEq(((Godot.Collections.Array)chipsetEntry["unknown_fields"]).Count, 1, "chipset nested field retained");
		var sectionCounts = (Godot.Collections.Dictionary)data["section_counts"];
		foreach (var section in new[] { "troops", "animations", "chipsets" })
		{
			AssertEq(sectionCounts[section].AsInt32(), 1, section + " section count");
		}
	}

	public void Test_ParseDatabaseDecodesSwitchAndVariableNames()
	{
		var switchA = Struct(
			Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Switch A")),
			Chunk(0x55, new byte[] { 0x01 })
		);
		var switchB = Struct();
		var variable = Struct(Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("Gold")));
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x17, StructArray(switchA, switchB)),
			Chunk(0x18, StructArray(variable)),
			Chunk(0x1a, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("Names.rdata"), database);
		var result = _parser.ParseDatabase(Dir.PathJoin("Names.rdata"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		var switches = (Godot.Collections.Array)data["switches"];
		AssertEq(switches.Count, 2, "switch count");
		var firstSwitch = (Godot.Collections.Dictionary)switches[0];
		AssertEq(firstSwitch["name"].AsString(), "Switch A", "switch name");
		AssertEq(((Godot.Collections.Array)firstSwitch["unknown_fields"]).Count, 1, "switch unknown field");
		AssertEq(((Godot.Collections.Dictionary)switches[1])["name"].AsString(), "", "unnamed switch");
		var variables = (Godot.Collections.Array)data["variables"];
		AssertEq(variables.Count, 1, "variable count");
		AssertEq(((Godot.Collections.Dictionary)variables[0])["name"].AsString(), "Gold", "variable name");
	}

	public void Test_ParseDatabaseRejectsDuplicateNamedEntryIds()
	{
		var fields = Struct(Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("X")));
		var payload = new List<byte>();
		payload.AddRange(Ber(2));
		payload.AddRange(Ber(7));
		payload.AddRange(fields);
		payload.AddRange(Ber(7));
		payload.AddRange(fields);
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x17, payload.ToArray()),
			Chunk(0x1a, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("DupIds.rdata"), database);
		var result = _parser.ParseDatabase(Dir.PathJoin("DupIds.rdata"));
		AssertFalse(result.IsSuccess());
		AssertTrue(result.GetError()!.Message.Contains("Duplicate"), "duplicate id error");
	}

	public void Test_ParseDatabaseRejectsActorStructMissingTerminator()
	{
		var badActor = Chunk(0x01, System.Text.Encoding.ASCII.GetBytes("NoTerm"));
		var payload = new List<byte>();
		payload.AddRange(Ber(1));
		payload.AddRange(Ber(1));
		payload.AddRange(badActor);
		var database = Lcf("LcfDataBase", new List<byte[]>
		{
			Chunk(0x0b, payload.ToArray()),
			Chunk(0x1a, Ber(259)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("BadActor.rdata"), database);
		var result = _parser.ParseDatabase(Dir.PathJoin("BadActor.rdata"));
		AssertFalse(result.IsSuccess());
		AssertNe(result.GetError(), null);
	}

	// === TESTS: Map Parsing ===

	public void Test_ParseMapSuccess()
	{
		var result = _parser.ParseMap(Dir.PathJoin("Map001.rmm"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		var data = result.GetData();
		AssertEq(data["format"].AsString(), "LMU");
		AssertEq(data["width"].AsInt32(), 20);
		AssertEq(data["height"].AsInt32(), 15);
		AssertEq(data["chipset_id"].AsInt32(), 1);
		AssertEq(data["event_count"].AsInt32(), 0);
		var lower = (int[])data["lower_layer"];
		AssertEq(lower.Length, 300);
		AssertEq(lower[0], 0);
		AssertEq(lower[299], 299);
		var upper = (int[])data["upper_layer"];
		AssertEq(upper.Length, 300);
		AssertEq(upper[299], 0x8000 + 299);
	}

	public void Test_ParseMapNotFound()
	{
		var result = _parser.ParseMap(Dir.PathJoin("NonExistent.rmm"));
		AssertFalse(result.IsSuccess());
		AssertNe(result.GetError(), null);
	}

	public void Test_ParseMapTooSmall()
	{
		WriteFile(Dir.PathJoin("TinyMap.rmm"), System.Text.Encoding.ASCII.GetBytes("abc"));
		var result = _parser.ParseMap(Dir.PathJoin("TinyMap.rmm"));
		AssertFalse(result.IsSuccess());
	}

	public void Test_ParseMapBadLayerSize()
	{
		var map = Lcf("LcfMapUnit", new List<byte[]>
		{
			Chunk(0x01, Ber(1)),
			Chunk(0x02, Ber(20)),
			Chunk(0x03, Ber(15)),
			Chunk(0x47, TileLayer(2)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("BadLayer.rmm"), map);
		var result = _parser.ParseMap(Dir.PathJoin("BadLayer.rmm"));
		AssertFalse(result.IsSuccess());
		AssertTrue(result.GetError()!.Message.ToLowerInvariant().Contains("expected"));
	}

	public void Test_ParseMapDimensionLimit()
	{
		var map = Lcf("LcfMapUnit", new List<byte[]>
		{
			Chunk(0x01, Ber(1)),
			Chunk(0x02, Ber(600)),
			Chunk(0x03, Ber(15)),
			new byte[] { 0x00 },
		});
		WriteFile(Dir.PathJoin("Huge.rmm"), map);
		var result = _parser.ParseMap(Dir.PathJoin("Huge.rmm"));
		AssertFalse(result.IsSuccess());
	}

	// === TESTS: Save Parsing ===

	public void Test_ParseSaveSuccess()
	{
		var result = _parser.ParseSave(Dir.PathJoin("Save001.rmm"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		var data = result.GetData();
		AssertEq(data["format"].AsString(), "LSD");
		AssertEq(data["header"].AsString(), "LcfSaveData");
		AssertEq(data["chunk_count"].AsInt32(), 1);
		var chunks = (Godot.Collections.Array)data["chunks"];
		AssertEq(((Godot.Collections.Dictionary)chunks[0])["id"].AsInt32(), 1);
	}

	public void Test_ParseSaveNotFound()
	{
		var result = _parser.ParseSave(Dir.PathJoin("NonExistent.rmm"));
		AssertFalse(result.IsSuccess());
		AssertNe(result.GetError(), null);
	}

	public void Test_ParseSaveTooSmall()
	{
		WriteFile(Dir.PathJoin("TinySave.rmm"), System.Text.Encoding.ASCII.GetBytes("abc"));
		var result = _parser.ParseSave(Dir.PathJoin("TinySave.rmm"));
		AssertFalse(result.IsSuccess());
	}

	// === TESTS: Error Handling ===

	public void Test_ParseErrorDetails()
	{
		var result = _parser.ParseGameIni(Dir.PathJoin("NonExistent.ini"));
		var error = result.GetError();
		AssertNe(error, null);
		AssertTrue(error!.Message.ToLowerInvariant().Contains("not found"));
		AssertTrue(error.Message.Contains("NonExistent"));
	}

	public void Test_ParseReturnsEmptyDataOnFailure()
	{
		var result = _parser.ParseGameIni(Dir.PathJoin("NonExistent.ini"));
		AssertFalse(result.IsSuccess());
		AssertEq(result.GetData().Count, 0);
	}
}
