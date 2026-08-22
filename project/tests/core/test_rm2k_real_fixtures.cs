using Godot;
using UniversalRPG.Rm2k.Parser;
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
