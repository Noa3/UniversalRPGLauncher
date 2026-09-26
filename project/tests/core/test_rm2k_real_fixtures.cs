using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestRm2kRealFixtures : TestBase
{
	private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";

	private Rm2kParser _parser = null!;

	public override void Setup()
	{
		_parser = new Rm2kParser();
	}

	public void Test_Rm2000DatabaseHasValidLcfBoundaries()
	{
		var result = _parser.ParseDatabase(FixtureRoot.PathJoin("rm2000/RPG_RT.ldb"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		AssertEq(data["header"].AsString(), "LcfDataBase");
		AssertTrue((long)data["file_size"] > 0);
		AssertTrue(data["chunk_count"].AsInt32() > 0);
		AssertTrue(data["chunk_count"].AsInt32() < 100000);
	}

	public void Test_Rm2003DatabaseHasValidLcfBoundaries()
	{
		var result = _parser.ParseDatabase(FixtureRoot.PathJoin("rm2003/RPG_RT.ldb"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		AssertEq(data["header"].AsString(), "LcfDataBase");
		AssertTrue((long)data["file_size"] > 0);
		AssertTrue(data["chunk_count"].AsInt32() > 0);
		AssertTrue(data["chunk_count"].AsInt32() < 100000);
	}

	public void Test_Rm2000MapHasValidLcfBoundaries()
	{
		var result = _parser.ParseMap(FixtureRoot.PathJoin("rm2000/Map0001.lmu"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		AssertEq(data["header"].AsString(), "LcfMapUnit");
		AssertTrue(data["width"].AsInt32() > 0);
		AssertTrue(data["height"].AsInt32() > 0);
		AssertTrue(data["chunk_count"].AsInt32() > 0);
	}

	public void Test_Rm2003MapHasValidLcfBoundaries()
	{
		var result = _parser.ParseMap(FixtureRoot.PathJoin("rm2003/Map0001.lmu"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		AssertEq(data["header"].AsString(), "LcfMapUnit");
		AssertTrue(data["width"].AsInt32() > 0);
		AssertTrue(data["height"].AsInt32() > 0);
		AssertTrue(data["chunk_count"].AsInt32() > 0);
	}

	public void Test_RealFixtureFramingConsumesExactFileBoundaries()
	{
		AssertFixtureFraming("rm2000/RPG_RT.ldb", "LcfDataBase", 16, 210227, false);
		AssertFixtureFraming("rm2003/RPG_RT.ldb", "LcfDataBase", 22, 416513, false);
		AssertFixtureFraming("rm2000/Map0001.lmu", "LcfMapUnit", 6, 8544, true);
		AssertFixtureFraming("rm2003/Map0001.lmu", "LcfMapUnit", 11, 8488, true);
	}

	public void Test_RealDatabaseTypedSectionsMatchSectionCounts()
	{
		AssertTypedSectionsMatchCounts("rm2000/RPG_RT.ldb");
		AssertTypedSectionsMatchCounts("rm2003/RPG_RT.ldb");
		var rm2003 = _parser.ParseDatabase(FixtureRoot.PathJoin("rm2003/RPG_RT.ldb"));
		AssertTrue(rm2003.IsSuccess(), DescribeError(rm2003));
		if (rm2003.IsSuccess())
		{
			var battleCommands = (Godot.Collections.Dictionary)rm2003.GetData()["battle_commands"];
			AssertTrue(battleCommands.ContainsKey("death_handler"), "RM2003 battle command metadata");
			AssertTrue(battleCommands.ContainsKey("unknown_fields"), "RM2003 battle command unknown fields");
			AssertEq(((Godot.Collections.Dictionary)rm2003.GetData()["section_counts"])["battle_commands"].AsInt32(), 1,
				"RM2003 battle command section count");
		}
	}

	private void AssertTypedSectionsMatchCounts(string pRelativePath)
	{
		var result = _parser.ParseDatabase(FixtureRoot.PathJoin(pRelativePath));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		var counts = (Godot.Collections.Dictionary)data["section_counts"];
		foreach (var section in new[]
		{
			"actors", "skills", "items", "enemies", "troops", "terrains", "attributes",
			"states", "animations", "chipsets", "classes", "switches", "variables",
		})
		{
			AssertTrue(data.ContainsKey(section), $"{pRelativePath} typed section key: {section}");
			var entries = (Godot.Collections.Array)data[section];
			if (counts.ContainsKey(section))
			{
				AssertEq(entries.Count, counts[section].AsInt32(), $"{pRelativePath} {section} count");
			}
			else
			{
				AssertEq(entries.Count, 0, $"{pRelativePath} absent {section} is empty");
			}
		}
		var actors = (Godot.Collections.Array)data["actors"];
		AssertTrue(actors.Count > 0, pRelativePath + " has actors");
		foreach (Godot.Collections.Dictionary actor in actors)
		{
			AssertTrue(actor.ContainsKey("name"), pRelativePath + " actor name key");
			AssertTrue(actor.ContainsKey("unknown_fields"), pRelativePath + " actor unknown_fields key");
		}
	}
	public void Test_RealMapEventPagesDecodeCommandCountsMatchingLiblcfSizes()
	{
		var verifiedPages = 0;
		var containedErrors = 0;
		foreach (var relativePath in new[] { "rm2000/Map0001.lmu", "rm2003/Map0001.lmu" })
		{
			var result = _parser.ParseMap(FixtureRoot.PathJoin(relativePath));
			AssertTrue(result.IsSuccess(), DescribeError(result));
			if (!result.IsSuccess())
			{
				return;
			}

			foreach (Godot.Collections.Dictionary mapEvent in (Godot.Collections.Array)result.GetData()["events"])
			{
				var pages = (Godot.Collections.Array)mapEvent["pages"];
				AssertEq(pages.Count, mapEvent["page_count"].AsInt32(), relativePath + " event page_count");
				foreach (Godot.Collections.Dictionary page in pages)
				{
					var commands = (Godot.Collections.Array)page["commands"];
					if (!page["has_command_list"].AsBool())
					{
						AssertEq(commands.Count, 0, relativePath + " page without command list");
						continue;
					}

					// A page must either decode fully or report a contained error with
					// its raw payload size; command data is never dropped silently.
					var commandError = page["command_error"].AsString();
					if (commandError.Length > 0)
					{
						AssertTrue(page["event_commands_bytes"].AsInt32() > 0,
							relativePath + " contained command error keeps payload size");
						containedErrors++;
						continue;
					}

					AssertTrue(commands.Count > 0, relativePath + " page with command list has commands");
					var declaredSize = page["event_commands_size"].AsInt32();
					if (declaredSize >= 0)
					{
						AssertTrue(page["event_commands_bytes"].AsInt32() == declaredSize,
							$"{relativePath} bytes={page["event_commands_bytes"].AsInt32()} declared={declaredSize}");
					}
					verifiedPages++;
				}
			}
		}

		AssertTrue(verifiedPages > 0, "real fixtures expose decoded event command lists");
		AssertTrue(containedErrors < verifiedPages, "most real pages decode without contained errors");
	}

	public void Test_Rm2000RealMapPagesDecodeEveryCommandVector()
	{
		var result = _parser.ParseMap(FixtureRoot.PathJoin("rm2000/Map0001.lmu"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}

		var commandPages = 0;
		foreach (Godot.Collections.Dictionary mapEvent in (Godot.Collections.Array)result.GetData()["events"])
		{
			foreach (Godot.Collections.Dictionary page in (Godot.Collections.Array)mapEvent["pages"])
			{
				if (!page["has_command_list"].AsBool())
				{
					continue;
				}
				commandPages++;
				AssertEq(page["command_error"].AsString(), "",
					"RM2000 page command vector decodes without error");
			}
		}

		AssertTrue(commandPages > 0, "RM2000 fixture has pages with command lists");
	}

	public void Test_RealFixtureCommandsUseVerifiedParameterWidths()
	{
		// EasyRPG CmdSetup declares the minimum parameter count per command.
		// Real payloads must satisfy it, otherwise the interpreter would read
		// shifted or missing operands.
		var minimumParameters = new Dictionary<int, int>
		{
			{ 10110, 0 }, { 10210, 4 }, { 10220, 7 }, { 10310, 3 }, { 10320, 5 },
			{ 10330, 3 }, { 10420, 6 }, { 10810, 3 }, { 11410, 1 }, { 12010, 6 },
		};
		var checkedCommands = 0;
		foreach (var relativePath in new[] { "rm2000/Map0001.lmu", "rm2003/Map0001.lmu" })
		{
			var result = _parser.ParseMap(FixtureRoot.PathJoin(relativePath));
			AssertTrue(result.IsSuccess(), DescribeError(result));
			if (!result.IsSuccess())
			{
				return;
			}

			foreach (Godot.Collections.Dictionary mapEvent in (Godot.Collections.Array)result.GetData()["events"])
			{
				foreach (Godot.Collections.Dictionary page in (Godot.Collections.Array)mapEvent["pages"])
				{
					if (!page["has_command_list"].AsBool()) continue;
					foreach (Godot.Collections.Dictionary command in (Godot.Collections.Array)page["commands"])
					{
						var code = command["code"].AsInt32();
						if (!minimumParameters.TryGetValue(code, out var minimum)) continue;
						var actual = ((int[])command["parameters"]).Length;
						AssertTrue(actual >= minimum,
							$"{relativePath} command {code} has {actual} parameters, expected at least {minimum}");
						checkedCommands++;
					}
				}
			}
		}

		AssertTrue(checkedCommands > 0, "real fixtures expose commands with verified widths");
	}

	public void Test_RealMapPageTriggersUseLiblcfEventPageTriggerValues_Values()
	{
		var allowedTriggers = new HashSet<int> { 0, 1, 2, 3, 4 };
		var actionPages = 0;
		var totalPages = 0;
		foreach (var relativePath in new[] { "rm2000/Map0001.lmu", "rm2003/Map0001.lmu" })
		{
			var result = _parser.ParseMap(FixtureRoot.PathJoin(relativePath));
			AssertTrue(result.IsSuccess(), DescribeError(result));
			if (!result.IsSuccess())
			{
				return;
			}

			foreach (Godot.Collections.Dictionary mapEvent in (Godot.Collections.Array)result.GetData()["events"])
			{
				foreach (Godot.Collections.Dictionary page in (Godot.Collections.Array)mapEvent["pages"])
				{
					totalPages++;
					var trigger = page["trigger"].AsInt32();
					AssertTrue(allowedTriggers.Contains(trigger),
						$"{relativePath} trigger {trigger} is a liblcf EventPage::Trigger value");
					if (trigger == 0) actionPages++;
				}
			}
		}

		AssertTrue(totalPages > 0, "real fixtures expose event pages");
		AssertTrue(actionPages > 0,
			"real fixtures expose action-trigger pages reachable by the runtime");
	}

	public void Test_RealChipsetDecodesVerifiedPassabilityArrays()
	{
		foreach (var relativePath in new[] { "rm2000/RPG_RT.ldb", "rm2003/RPG_RT.ldb" })
		{
			var result = _parser.ParseDatabase(FixtureRoot.PathJoin(relativePath));
			AssertTrue(result.IsSuccess(), DescribeError(result));
			if (!result.IsSuccess())
			{
				return;
			}

			var sections = (Godot.Collections.Dictionary)result.GetData()["sections"];
			if (!sections.TryGetValue("chipsets", out var rawChipset))
			{
				AssertTrue(false, $"{relativePath} exposes the chipsets section; keys=[{string.Join(",", sections.Keys)}]");
				continue;
			}
			var chipset = (Godot.Collections.Dictionary)rawChipset;

			// liblcf ChunkChipset sizes: passable_data_lower x 162, upper x 144.
			var lower = (int[])chipset["passable_data_lower"];
			var upper = (int[])chipset["passable_data_upper"];
			AssertEq(lower.Length, 162, $"{relativePath} lower passability length");
			AssertEq(upper.Length, 144, $"{relativePath} upper passability length");

			// Bits 0-3 are the four direction flags (liblcf defaults: lower 15,
			// upper 31). The fixture must contain both a fully passable and a
			// blocked tile, otherwise movement parity cannot be proven.
			var fullyPassable = 0;
			var blocked = 0;
			foreach (var flags in lower)
			{
				if ((flags & 0x0f) == 0x0f) fullyPassable++;
				if ((flags & 0x0f) == 0) blocked++;
			}
			foreach (var flags in upper)
			{
				if ((flags & 0x0f) == 0x0f) fullyPassable++;
				if ((flags & 0x0f) == 0) blocked++;
			}

			AssertTrue(fullyPassable > 0, $"{relativePath} contains fully passable tiles");
			AssertTrue(blocked > 0, $"{relativePath} contains impassable tiles");
		}
	}

	public void Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles()
	{
		foreach (var relativePath in new[] { "rm2000/RPG_RT.ldb", "rm2003/RPG_RT.ldb" })
		{
			var database = _parser.ParseDatabase(FixtureRoot.PathJoin(relativePath));
			AssertTrue(database.IsSuccess(), DescribeError(database));
			if (!database.IsSuccess())
			{
				return;
			}
			var mapPath = relativePath.Replace("RPG_RT.ldb", "Map0001.lmu");
			var map = _parser.ParseMap(FixtureRoot.PathJoin(mapPath));
			AssertTrue(map.IsSuccess(), DescribeError(map));
			if (!map.IsSuccess())
			{
				return;
			}

			var chipset = ReadChipset(database.GetData());
			AssertTrue(chipset != null, $"{relativePath} chipset passability is present");
			if (chipset == null)
			{
				continue;
			}

			var lower = (int[])map.GetData()["lower_layer"];
			var upper = (int[])map.GetData()["upper_layer"];
			var masks = Rm2kChipset.BuildDirectionMasks(lower, upper, chipset.Value.lower, chipset.Value.upper);

			AssertEq(masks.Length, lower.Length, $"{relativePath} one mask per map tile");
			var passableTiles = 0;
			var blockedTiles = 0;
		 foreach (var mask in masks)
			{
				if (mask == Rm2kChipset.AllDirections) passableTiles++;
                if (mask == 0) blockedTiles++;
			}

			AssertTrue(passableTiles > 0, $"{relativePath} map has walkable tiles");
			AssertTrue(blockedTiles > 0, $"{relativePath} map has impassable tiles");

			// Movement must follow the decoded chipset: a fully blocked tile
			// refuses the step, a walkable tile accepts it.
			var blockedIndex = -1;
			var walkableIndex = -1;
			for (var index = 0; index < masks.Length; index++)
			{
				if (blockedIndex < 0 && masks[index] == 0) blockedIndex = index;
                if (walkableIndex < 0 && masks[index] == Rm2kChipset.AllDirections) walkableIndex = index;
			}
			AssertTrue(blockedIndex >= 0 && walkableIndex >= 0, $"{relativePath} has both tile kinds");

			var state = new GameSimulationState();
			var width = map.GetData()["width"].AsInt32();
			var height = map.GetData()["height"].AsInt32();
			state.ConfigureMap(1, width, height, masks);

			// Place the player next to a walkable tile and confirm the step.
			var walkX = walkableIndex % width;
			var walkY = walkableIndex / width;
			if (walkX > 0)
			{
				state.MapX = walkX - 1;
				state.MapY = walkY;
				AssertTrue(state.TryMove(1, 0), $"{relativePath} steps onto a walkable tile");
			}
			if (blockedIndex / width > 0)
			{
				var blockedX = blockedIndex % width;
				var blockedY = blockedIndex / width;
				state.MapX = blockedX;
				state.MapY = blockedY - 1;
				AssertFalse(state.TryMove(0, 1), $"{relativePath} refuses an impassable tile");
			}
		}
	}

	private static (byte[] lower, byte[] upper)? ReadChipset(Godot.Collections.Dictionary pDatabase)
	{
		if (!pDatabase.TryGetValue("sections", out var rawSections)
            || rawSections.VariantType != Godot.Variant.Type.Dictionary)
        {
            return null;
        }
		var sections = rawSections.AsGodotDictionary();
		if (!sections.TryGetValue("chipsets", out var rawChipset)
			|| rawChipset.VariantType != Godot.Variant.Type.Dictionary)
		{
			return null;
		}
		var chipset = rawChipset.AsGodotDictionary();
		if (!chipset.TryGetValue("passable_data_lower", out var rawLower)
			|| !chipset.TryGetValue("passable_data_upper", out var rawUpper))
		{
			return null;
		}
		var lowerValues = ((int[])rawLower);
		var upperValues = ((int[])rawUpper);
		if (lowerValues.Length != Rm2kChipset.PassabilityLowerEntries
			|| upperValues.Length != Rm2kChipset.PassabilityUpperEntries)
		{
			return null;
		}
		var lower = new byte[lowerValues.Length];
		var upper = new byte[upperValues.Length];
		for (var index = 0; index < lower.Length; index++) lower[index] = (byte)lowerValues[index];
		for (var index = 0; index < upper.Length; index++) upper[index] = (byte)upperValues[index];
		return (lower, upper);
	}

	private static string DescribeError(Rm2kParser.ParseResult pResult)
	{
		return pResult.IsSuccess() ? "" : pResult.GetError()!.Describe();
	}

	private void AssertFixtureFraming(
		string pRelativePath,
		string pHeader,
		int pExpectedChunks,
		int pExpectedSize,
		bool pExpectedTerminator
	)
	{
		var path = FixtureRoot.PathJoin(pRelativePath);
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		AssertNe(file, null, "Open fixture " + path);
		if (file == null)
		{
			return;
		}
		var bytes = file.GetBuffer((long)file.GetLength());
		AssertEq(bytes.Length, pExpectedSize, "Fixture size " + pRelativePath);

		var reader = new LcfBinaryReader(bytes);
		AssertEq(reader.ReadHeader(pHeader), pHeader, "Fixture header " + pRelativePath);
		AssertFalse(reader.HasError(), "Fixture header error " + pRelativePath);
		var chunkCount = 0;
		var sawTerminator = false;
		while (!reader.IsEof())
		{
			var chunk = reader.ReadChunk();
			AssertFalse(reader.HasError(), "Fixture chunk error " + pRelativePath);
			if (reader.HasError())
			{
				return;
			}
			chunkCount += 1;
			if ((bool)chunk["terminator"])
			{
				sawTerminator = true;
				break;
			}
		}
		AssertEq(chunkCount, pExpectedChunks, "Fixture chunk count " + pRelativePath);
		AssertEq(sawTerminator, pExpectedTerminator, "Fixture terminator " + pRelativePath);
		AssertEq(reader.GetPosition(), bytes.Length, "Fixture boundary " + pRelativePath);
	}
}
