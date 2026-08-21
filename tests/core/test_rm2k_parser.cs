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
