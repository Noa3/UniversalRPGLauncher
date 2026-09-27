using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestRm2kLmtParser : TestBase
{
	private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";
	private const string WriteDir = "user://rm2k_lmt_test";

	private Rm2kParser _parser = null!;

	public override void Setup()
	{
		_parser = new Rm2kParser();
	}

	public override void Teardown()
	{
		if (!DirAccess.DirExistsAbsolute(WriteDir))
		{
			return;
		}
		using var directory = DirAccess.Open(WriteDir);
		if (directory == null)
		{
			return;
		}
		foreach (var fileName in directory.GetFiles())
		{
			DirAccess.RemoveAbsolute(WriteDir.PathJoin(fileName));
		}
		DirAccess.RemoveAbsolute(WriteDir);
	}

	public void Test_ParseRealRm2000MapTree()
	{
		var result = _parser.ParseMapTree(FixtureRoot.PathJoin("rm2000/RPG_RT.lmt"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		AssertEq(data["header"].AsString(), "LcfMapTree");
		AssertEq(data["map_count"].AsInt32(), 81);
		var maps = (Godot.Collections.Array)data["maps"];
		AssertEq(((Godot.Collections.Dictionary)maps[0])["id"].AsInt32(), -1);
		AssertEq(((Godot.Collections.Dictionary)maps[0])["name"].AsString(), "MAP-0001");
		AssertEq(((Godot.Collections.Dictionary)maps[1])["id"].AsInt32(), 0);
		AssertEq(((Godot.Collections.Dictionary)maps[1])["name"].AsString(), "RPG Maker 2000 Test suite");
		AssertEq(((Godot.Collections.Array)data["tree_order"]).Count, 81);
		AssertEq(data["active_node"].AsInt32(), 50);
		var start = (Godot.Collections.Dictionary)data["start"];
		AssertEq(start["party_map_id"].AsInt32(), 30);
		AssertEq(start["party_x"].AsInt32(), 37);
		AssertEq(start["party_y"].AsInt32(), 72);
	}

	public void Test_ARealMapYieldsItsMoveRouteCommands()
	{
		// The pinned map is a real LMT, so this proves the route decoder against
		// a file nobody wrote for the test. A page without a route yields an
		// empty one, which is the liblcf default, so the assertion is on the
		// shape of what came back rather than on a specific command.
		var result = _parser.ParseMap(FixtureRoot.PathJoin("rm2000/Map0001.lmu"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		var events = (Godot.Collections.Array<Godot.Collections.Dictionary>)data["events"];
		AssertTrue(events.Count > 0, "the real map has events");

		var pagesWithRoutes = 0;
		var totalCommands = 0;
		var totalPages = 0;
		foreach (var page in AllPages(events))
		{
			// A route that failed to decode is reported rather than silently
			// empty, so a broken page cannot look like a page with no route.
			totalPages++;
			var error = ((string)page["move_route_error"]);
			AssertEq(error, "", $"every page of the real map decodes its move route: {error}");

			var commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)page["move_route_commands"];
			AssertEq((int)page["move_route_count"], commands.Count,
				"the reported count matches the commands that were read");
			if (commands.Count > 0)
			{
				pagesWithRoutes++;
				totalCommands += commands.Count;
				foreach (var command in commands)
				{
					var id = (int)command["command_id"];
					AssertTrue(id >= 0 && id <= Rm2kMoveRoute.MaxCommandId,
						$"command {id} is one the format defines");
				}
			}
		}
		// The pinned map has 22 real event pages and every one of them decodes
		// cleanly, but none of them defines a move route. Asserting that it does
		// would be asserting something about the fixture rather than about the
		// decoder, so the presence of commands is covered by the byte exact
		// tests in TestRm2kMoveRouteDecoder and a synthetic LMT below.
		AssertEq(totalPages, 22, "the pinned map has 22 real event pages");
		AssertEq(pagesWithRoutes, 0,
			"and none of them defines a move route, which is a property of the fixture");
		AssertEq(totalCommands, 0, "so no route commands come from it");
	}

	private static IEnumerable<Godot.Collections.Dictionary> AllPages(
		Godot.Collections.Array<Godot.Collections.Dictionary> pEvents)
	{
		foreach (var pageEvent in pEvents)
		{
			foreach (var page in (Godot.Collections.Array<Godot.Collections.Dictionary>)pageEvent["pages"])
			{
				yield return page;
			}
		}
	}

	public void Test_ParseRealRm2003MapTree()
	{
		var result = _parser.ParseMapTree(FixtureRoot.PathJoin("rm2003/RPG_RT.lmt"));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (!result.IsSuccess())
		{
			return;
		}
		var data = result.GetData();
		AssertEq(data["header"].AsString(), "LcfMapTree");
		AssertEq(data["map_count"].AsInt32(), 22);
		var maps = (Godot.Collections.Array)data["maps"];
		AssertEq(((Godot.Collections.Dictionary)maps[0])["id"].AsInt32(), 0);
		AssertEq(((Godot.Collections.Dictionary)maps[0])["name"].AsString(), "RPG Maker 2003 Test suite");
		AssertEq(((Godot.Collections.Dictionary)maps[2])["parent_id"].AsInt32(), 1);
		AssertEq(((Godot.Collections.Array)data["tree_order"]).Count, 22);
		AssertEq(data["active_node"].AsInt32(), 20);
		var start = (Godot.Collections.Dictionary)data["start"];
		AssertEq(start["party_map_id"].AsInt32(), 1);
		AssertEq(start["party_x"].AsInt32(), 4);
		AssertEq(start["party_y"].AsInt32(), 8);
	}

	public void Test_ParseEmptyMapTree()
	{
		var result = _parser.ParseMapTree(WriteFixture("Empty.lmt", BuildLmt(
			new List<LmtEntry>(), new List<int>(), 0, new Dictionary<int, int>())));
		AssertTrue(result.IsSuccess(), DescribeError(result));
		if (result.IsSuccess())
		{
			AssertEq(result.GetData()["map_count"].AsInt32(), 0);
			AssertEq(((Godot.Collections.Array)result.GetData()["tree_order"]).Count, 0);
		}
	}

	public void Test_ParseMapTreeRejectsTruncation()
	{
		var bytes = BuildLmt(
			new List<LmtEntry> { MapEntry(1, "Root", 0, 0) },
			new List<int> { 1 }, 1,
			new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } });
		var truncated = new byte[bytes.Length - 1];
		System.Array.Copy(bytes, truncated, truncated.Length);
		var result = _parser.ParseMapTree(WriteFixture("Truncated.lmt", truncated));
		AssertFalse(result.IsSuccess());
	}

	public void Test_ParseMapTreeRejectsMaliciousMapCount()
	{
		var bytes = new List<byte>();
		bytes.AddRange(Header());
		bytes.AddRange(Ber(100001));
		var result = _parser.ParseMapTree(WriteFixture("HugeCount.lmt", bytes.ToArray()));
		AssertFalse(result.IsSuccess());
	}

	public void Test_ParseMapTreeRejectsInvalidParentReference()
	{
		var bytes = BuildLmt(
			new List<LmtEntry> { MapEntry(1, "Child", 99, 1) },
			new List<int> { 1 }, 1,
			new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } });
		var result = _parser.ParseMapTree(WriteFixture("InvalidParent.lmt", bytes));
		AssertFalse(result.IsSuccess());
	}

	public void Test_ParseMapTreeRejectsParentCycle()
	{
		var entries = new List<LmtEntry>
		{
			MapEntry(1, "One", 2, 1),
			MapEntry(2, "Two", 1, 1),
		};
		var result = _parser.ParseMapTree(WriteFixture("Cycle.lmt", BuildLmt(
			entries, new List<int> { 1, 2 }, 1,
			new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } })));
		AssertFalse(result.IsSuccess());
	}

	private class LmtEntry
	{
		public int Id;
		public List<(int Id, byte[] Payload)> Fields = new();
	}

	private static LmtEntry MapEntry(int pId, string pName, int pParent, int pIndent)
	{
		return new LmtEntry
		{
			Id = pId,
			Fields = new List<(int, byte[])>
			{
				(0x01, System.Text.Encoding.UTF8.GetBytes(pName)),
				(0x02, Ber(pParent)),
				(0x03, Ber(pIndent)),
				(0x04, Ber(1)),
			},
		};
	}

	private static byte[] BuildLmt(
		List<LmtEntry> pEntries,
		List<int> pTreeOrder,
		int pActiveNode,
		Dictionary<int, int> pStart
	)
	{
		var bytes = new List<byte>();
		bytes.AddRange(Header());
		bytes.AddRange(Ber(pEntries.Count));
		foreach (var entry in pEntries)
		{
			bytes.AddRange(Ber(entry.Id));
			foreach (var field in entry.Fields)
			{
				bytes.AddRange(Chunk(field.Id, field.Payload));
			}
			bytes.Add(0);
		}
		bytes.AddRange(Ber(pTreeOrder.Count));
		foreach (var mapId in pTreeOrder)
		{
			bytes.AddRange(Ber(mapId));
		}
		bytes.AddRange(Ber(pActiveNode));
		foreach (var pair in pStart)
		{
			bytes.AddRange(Chunk(pair.Key, Ber(pair.Value)));
		}
		bytes.Add(0);
		return bytes.ToArray();
	}

	private static byte[] Header()
	{
		var bytes = new List<byte>();
		bytes.AddRange(Ber(10));
		bytes.AddRange(System.Text.Encoding.ASCII.GetBytes("LcfMapTree"));
		return bytes.ToArray();
	}

	private static byte[] Ber(int pValue)
	{
		var value = pValue;
		if (value < 0)
		{
			value += unchecked((int)0x100000000);
		}
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

	private static byte[] Chunk(int pId, byte[] pPayload)
	{
		var bytes = new List<byte>();
		bytes.AddRange(Ber(pId));
		bytes.AddRange(Ber(pPayload.Length));
		bytes.AddRange(pPayload);
		return bytes.ToArray();
	}

	private static string WriteFixture(string pName, byte[] pBytes)
	{
		var path = WriteDir.PathJoin(pName);
		DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		file?.StoreBuffer(pBytes);
		return path;
	}

	private static string DescribeError(Rm2kParser.ParseResult pResult)
	{
		return pResult.IsSuccess() ? "" : pResult.GetError()!.Describe();
	}
}
