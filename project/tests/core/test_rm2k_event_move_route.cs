using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.Core;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves that an event move route actually walks, from
/// <c>Game_Character::UpdateMoveRoute</c>.
/// </summary>
/// <remarks>
/// The route is written into a synthetic LMT with the verified field layout, so
/// the whole path is exercised: the LMT field 0x29, the decoder, the runtime
/// wiring and the per update command consumption. The pinned fixture has no
/// routes at all, so it cannot show this and a decoder only test would prove
/// nothing about the runtime.
/// </remarks>
public partial class TestRm2kEventMoveRoute : TestBase
{
	/// <summary>One simulated update at the default frame rate.</summary>
	private const double Tick = 1.0 / 60.0;

	private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";

	/// <summary>
	/// Writes a game whose map has a single event at 20,16 whose page defines a
	/// move route of three commands: down, down, face left. Every tile is
	/// passable so the route is not refused.
	/// </summary>
	/// <summary>
	/// Builds a game whose single event at 20,16 has a move route. The repeat
	/// flag is a parameter because a repeating route runs forever by design, and
	/// a test that wants the route to end has to say so rather than rely on a
	/// default the runtime cannot invent.
	/// </summary>
	/// <summary>
	/// A map with a wall across row 17, so a route walking down from 20,16 is
	/// refused at the first step. The passability mask is written per tile as
	/// the RM2K bit order: 0x0F is passable in all four directions and 0x00 is
	/// a wall.
	/// </summary>
	/// <summary>
	/// The same game as <see cref="CreateRouteGame"/>, with a wall across row
	/// 17 so a route walking down from 20,16 is refused at its first step. The
	/// passability mask is the RM2K bit order: 0x0F is passable in all four
	/// directions and 0x00 is a wall.
	/// </summary>
	private string CreateBlockedRouteGame(string pName, params int[] pCommandIds)
	{
		var ldb = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.ldb"));
		var gameDir = ProjectSettings.GlobalizePath($"user://rm2k-route-{pName}");
		if (DirAccess.DirExistsAbsolute(gameDir))
		{
			DirAccess.RemoveAbsolute(gameDir);
		}
		DirAccess.MakeDirRecursiveAbsolute(gameDir);
		File.Copy(ldb, Path.Combine(gameDir, "RPG_RT.ldb"), true);
		File.Copy(
			ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.lmt")),
			Path.Combine(gameDir, "RPG_RT.lmt"), true);
		var chipset = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/ChipSet/World.png"));
		if (File.Exists(chipset))
		{
			DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "ChipSet"));
			File.Copy(chipset, Path.Combine(gameDir, "ChipSet", "World.png"), true);
		}
		var charset = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/CharSet/Chara1.png"));
		if (File.Exists(charset))
		{
			DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "CharSet"));
			File.Copy(charset, Path.Combine(gameDir, "CharSet", "Chara1.png"), true);
		}
		File.WriteAllBytes(Path.Combine(gameDir, "Map0001.lmu"), BuildMap(pRepeat: false, pCommandIds));
		return gameDir;
	}

	private string CreateRouteGame(string pName, bool pRepeat, params int[] pCommandIds)
	{
		var ldb = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.ldb"));
		var gameDir = ProjectSettings.GlobalizePath($"user://rm2k-route-{pName}");
		if (DirAccess.DirExistsAbsolute(gameDir))
		{
			DirAccess.RemoveAbsolute(gameDir);
		}
		DirAccess.MakeDirRecursiveAbsolute(gameDir);
		File.Copy(ldb, Path.Combine(gameDir, "RPG_RT.ldb"), true);
		File.Copy(
            ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.lmt")),
            Path.Combine(gameDir, "RPG_RT.lmt"), true);

		var chipset = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/ChipSet/World.png"));
		if (File.Exists(chipset))
		{
			DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "ChipSet"));
			File.Copy(chipset, Path.Combine(gameDir, "ChipSet", "World.png"), true);
		}
		var charset = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/CharSet/Chara1.png"));
		if (File.Exists(charset))
		{
			DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "CharSet"));
			File.Copy(charset, Path.Combine(gameDir, "CharSet", "Chara1.png"), true);
		}

		File.WriteAllBytes(Path.Combine(gameDir, "Map0001.lmu"), BuildMap(pRepeat, pCommandIds));
		return gameDir;
	}

	private static byte[] BuildMap(bool pRepeat, int[] pCommandIds)
	{
		const int width = 40;
		const int height = 30;
		var tileCount = width * height;
		var lower = new byte[tileCount * 2];
		for (var index = 0; index < tileCount; index++)
		{
			var tile = 5000;
			lower[index * 2] = (byte)(tile & 0xFF);
			lower[index * 2 + 1] = (byte)((tile >> 8) & 0xFF);
		}

		// The route itself: LMU chunk 0x29, whose move_commands are 0x0B for
		// the byte size and 0x0C for prefixless entries, and repeat at0x15.
		var commands = new List<byte[]>();
		foreach (var id in pCommandIds)
		{
			commands.Add(TestRm2kParser.Ber(id));
		}
		var array = Join(commands.ToArray());
		var moveRoute = TestRm2kParser.Struct(new List<byte[]>
		{
            TestRm2kParser.Chunk(0x0B, TestRm2kParser.Ber(array.Length)),
            TestRm2kParser.Chunk(0x0C, array),
            TestRm2kParser.Chunk(0x15, TestRm2kParser.Ber(pRepeat ? 1 : 0)),
        }.ToArray());

		var page = new List<byte[]>
		{
            TestRm2kParser.Chunk(0x21, TestRm2kParser.Ber(0)),   // trigger action
            TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(1)),   // same as hero
            TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(0)),   // character index
            TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(2)),   // facing down
            TestRm2kParser.Chunk(0x15, System.Text.Encoding.ASCII.GetBytes("Chara1")),
            TestRm2kParser.Chunk(0x29, moveRoute),
        };

		var mapEvent = TestRm2kParser.Struct(new List<byte[]>
		{
            TestRm2kParser.Chunk(0x01, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x02, TestRm2kParser.Ber(20)),
            TestRm2kParser.Chunk(0x03, TestRm2kParser.Ber(16)),
            TestRm2kParser.Chunk(0x05, TestRm2kParser.StructArray(TestRm2kParser.Struct(page.ToArray()))),
		}.ToArray());

		var upper = new byte[tileCount * 2];
		for (var index = 0; index < tileCount; index++)
		{
			var tile = 10000;
			upper[index * 2] = (byte)(tile & 0xFF);
			upper[index * 2 + 1] = (byte)((tile >> 8) & 0xFF);
		}

		var chunks = new List<byte[]>
		{
            TestRm2kParser.Chunk(0x01, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x02, TestRm2kParser.SystemText("Route")),
            TestRm2kParser.Chunk(0x02, TestRm2kParser.Ber(width)),
            TestRm2kParser.Chunk(0x03, TestRm2kParser.Ber(height)),
            TestRm2kParser.Chunk(0x47, lower),
            TestRm2kParser.Chunk(0x48, upper),
            TestRm2kParser.Chunk(0x51, TestRm2kParser.StructArray(mapEvent)),
        };

        // The file is a BER length prefixed ASCII header, then the chunks, then a
        // single zero terminator. Without the header the parser refuses the file
        // by name, so this is part of the format rather than decoration.
        var bytes = new List<byte>();
        var header = "LcfMapUnit";
        bytes.AddRange(TestRm2kParser.Ber(header.Length));
        bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(header));
        bytes.AddRange(Join(chunks.ToArray()));
        bytes.Add(0x00);
        return bytes.ToArray();
        }

	private static byte[] Join(byte[][] pParts)
	{
		var result = new List<byte>();
		foreach (var part in pParts)
		{
			result.AddRange(part);
		}
		return [.. result];
	}

	private (EnginePluginHost Host, Rm2kEngineRuntime Runtime)? Start(string pGameDir)
	{
		var game = new PluginGameInfo
		{
			GameDirectory = pGameDir,
			EngineId = EnginePluginIds.RpgMaker2000,
			Generation = "rm2k",
			DetectorScore = 3,
		};
		var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
		PluginOperationResult started;
		try
		{
			started = host.Start(game);
		}
		catch (Exception e)
		{
			AssertTrue(false, $"The runtime started without an exception: {e.GetType().Name}: {e.Message}");
			return null;
		}
		AssertTrue(started.Success, $"the runtime starts: {started.Error?.Message ?? "no error"}");
		if (host.Runtime is not Rm2kEngineRuntime runtime)
		{
			AssertTrue(false, "the host created an RM2K runtime");
			return null;
		}
		return (host, runtime);
	}

	public void Test_ARouteMovesTheEventOneTileAtATimeOverSeveralUpdates()
	{
		// A route command that starts a step returns immediately in the Player,
		// so the event does not arrive after one update. It takes the whole
		// step budget, which at move speed 3 is sixteen updates per tile.
		int[] pCommandIdsForProbe = { Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown };
		var gameDir = CreateRouteGame(nameof(Test_ARouteMovesTheEventOneTileAtATimeOverSeveralUpdates),
			pRepeat: true, pCommandIdsForProbe);
		var started = Start(gameDir);
		if (started == null)
		{
			return;
		}
		var runtime = started.Value.Runtime;
		var commandCount = runtime.StartEventMoveRouteForTest(1);
		AssertEq(commandCount, 2, "the page defines two route commands");

		var start = runtime.EventPositionForTest(1);
		AssertEq(start!.Value.X, 20, "the event starts at x 20");
		AssertEq(start.Value.Y, 16, "and y 16");

		runtime.Update(Tick);
		// The logical tile changed on the first update, which is what Move does.
		var afterFirst = runtime.EventPositionForTest(1)!.Value;
		AssertEq(afterFirst.Y, 17, "the event has stepped to y 17");
		AssertEq(afterFirst.X, 20, "and stayed at x 20");
		// The step starts full and is not yet spent: the command that started it
		// returned from the Player's loop at once, so the budget is only drawn
		// down from the following update. That is the difference between walking
		// a tile and teleporting onto it.
		AssertEq(runtime.EventRemainingStepForTest(1), Rm2kStepBudget.ScreenTileSize,
			"and the step is still full, because the command that started it returned immediately");

		// Each following update spends the speed's share of the budget, so at
		// move speed 3 a tile takes sixteen updates.
		runtime.Update(Tick);
		AssertEq(runtime.EventRemainingStepForTest(1), Rm2kStepBudget.ScreenTileSize - 16,
			"the next update spends the first share of the step");

		// One more command cannot start while the first step is walking, so the
		// event stays put until the budget is spent.
		runtime.Update(Tick);
		var afterSecond = runtime.EventPositionForTest(1)!.Value;
		AssertEq(afterSecond.Y, 17, "the second command did not start while the first step was walking");

		// Walk the rest of the first step out, then the second command starts.
		for (var tick = 0; tick < 20 && runtime.EventRemainingStepForTest(1) >= 0; tick++)
		{
			runtime.Update(Tick);
		}
		var afterRoute = runtime.EventPositionForTest(1)!.Value;
		AssertEq(afterRoute.Y, 18, "the second command moved the event one more tile");
		AssertEq(afterRoute.X, 20, "and it did not move sideways");

		Cleanup(gameDir);
	}

	public void Test_ARouteThatEndsLeavesTheEventWhereItStopped()
	{
		var gameDir = CreateRouteGame(nameof(Test_ARouteThatEndsLeavesTheEventWhereItStopped),
			pRepeat: false, Rm2kMoveRoute.MoveDown);
		var started = Start(gameDir);
		if (started == null)
		{
			return;
		}
		var runtime = started.Value.Runtime;
		AssertEq(runtime.StartEventMoveRouteForTest(1), 1, "one command");
		for (var tick = 0; tick < 60; tick++)
		{
			runtime.Update(Tick);
		}
		var final = runtime.EventPositionForTest(1)!.Value;
		AssertEq(final.Y, 17, "the event moved exactly one tile");
		AssertEq(final.X, 20, "and no further");
		AssertTrue(runtime.EventRouteFinishedForTest(1),
			"and the route reports itself finished");
		AssertEq(runtime.EventRemainingStepForTest(1), -1,
			$"and the event is no longer mid step (got {runtime.EventRemainingStepForTest(1)})");

		// A finished route must not keep walking, so a long run changes nothing.
		for (var tick = 0; tick < 120; tick++)
		{
			runtime.Update(Tick);
		}
		var after = runtime.EventPositionForTest(1)!.Value;
		AssertEq(after.Y, 17, "a finished route does not keep moving the event");
		AssertEq(after.X, 20, "in either direction");

		Cleanup(gameDir);
	}

	public void Test_ARouteTurnsTheEventWithoutMovingIt()
	{
		// turn_90_degree_right is command 16 and the liblcf order is up 0, right
		// 1, down 2, left 3, so a right turn from down is left, stored as the
		// RPG Maker byte 4.
		var gameDir = CreateRouteGame(nameof(Test_ARouteTurnsTheEventWithoutMovingIt),
			pRepeat: true, Rm2kMoveRoute.Turn90DegreeRight);
		var started = Start(gameDir);
		if (started == null)
		{
			return;
		}
		var runtime = started.Value.Runtime;
		AssertEq(runtime.EventFacingForTest(1), 2, "the event starts facing down");
		runtime.StartEventMoveRouteForTest(1);
		runtime.Update(Tick);
		AssertEq(runtime.EventFacingForTest(1), 4, "a right turn from down faces left");
		var position = runtime.EventPositionForTest(1)!.Value;
		AssertEq(position.X, 20, "and the event did not move");
		AssertEq(position.Y, 16, "in either direction");

		Cleanup(gameDir);
	}

	public void Test_APageWithoutARouteLeavesTheEventAlone()
	{
		// The Player's loop checks the route pointer before it looks at a
		// command, so an event with no route never runs a route at all. This is
		// the case the pinned fixture is in, and it must stay quiet.
		var gameDir = CreateRouteGame(nameof(Test_APageWithoutARouteLeavesTheEventAlone), pRepeat: true);
		var started = Start(gameDir);
		if (started == null)
		{
			return;
		}
		var runtime = started.Value.Runtime;
		AssertEq(runtime.StartEventMoveRouteForTest(1), 0,
			"an event whose page has no route starts nothing");
		for (var tick = 0; tick < 60; tick++)
		{
			runtime.Update(Tick);
		}
		var position = runtime.EventPositionForTest(1)!.Value;
		AssertEq(position.X, 20, "and it never moves");
		AssertEq(position.Y, 16, "in either direction");
		AssertEq(runtime.EventRemainingStepForTest(1), -1, "and it is never mid step");

		Cleanup(gameDir);
	}

	public void Test_AnEventWithNoRouteIsUnaffectedByAnotherEventsRoute()
	{
		// Routes are per event. A route running for one event must not make
		// another event move, which is what a single shared step state would do.
		var gameDir = CreateRouteGame(nameof(Test_AnEventWithNoRouteIsUnaffectedByAnotherEventsRoute),
			pRepeat: true, Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown);
		var started = Start(gameDir);
		if (started == null)
		{
			return;
		}
		var runtime = started.Value.Runtime;
		AssertEq(runtime.StartEventMoveRouteForTest(99), 0, "an unknown event has no route");
		runtime.StartEventMoveRouteForTest(1);
		for (var tick = 0; tick < 20; tick++)
		{
			runtime.Update(Tick);
		}
		AssertEq(runtime.EventPositionForTest(99), null, "an unknown event has no position");
		AssertEq(runtime.EventRemainingStepForTest(99), -1, "and no step state");

		Cleanup(gameDir);
	}

	public void Test_AFinishedRouteStillFinishesTheStepItStarted()
	{
		// The Player sets the stop count and falls through to the index advance,
		// so a route that ends on a step still has a step to walk. Dropping it
		// would leave the event standing on its old tile with a half drawn
		// sprite, or stuck mid step forever.
		var gameDir = CreateRouteGame(nameof(Test_AFinishedRouteStillFinishesTheStepItStarted),
			pRepeat: false, Rm2kMoveRoute.MoveDown);
		var started = Start(gameDir);
		if (started == null)
		{
			return;
		}
		var runtime = started.Value.Runtime;
		AssertEq(runtime.StartEventMoveRouteForTest(1), 1, "one command");
		runtime.Update(Tick);
		AssertEq(runtime.EventRemainingStepForTest(1), Rm2kStepBudget.ScreenTileSize,
			"the step starts full");

		// The route is finished from here on, but the step is not, so the budget
		// still has to run down and clear.
		for (var tick = 0; tick < 40 && runtime.EventRemainingStepForTest(1) >= 0; tick++)
		{
			runtime.Update(Tick);
		}
		AssertEq(runtime.EventRemainingStepForTest(1), -1,
			"a finished route still walks its last step out");
		AssertTrue(runtime.EventRouteFinishedForTest(1), "and it is still finished");
		var position = runtime.EventPositionForTest(1)!.Value;
		AssertEq(position.Y, 17, "and the event arrived at the tile it was sent to");

		// Nothing more happens once the step is done.
		for (var tick = 0; tick < 60; tick++)
		{
			runtime.Update(Tick);
		}
		var after = runtime.EventPositionForTest(1)!.Value;
		AssertEq(after.Y, 17, "and it stays there");
		AssertEq(after.X, 20, "in either direction");

		Cleanup(gameDir);
	}

	public void Test_ARouteHoldsOnTheSameCommandWhenTheWayIsBlockedAndItIsNotSkippable()
	{
		// The Player's own check: a refused step either skips the command or
		// holds the route on it. A route that is not skippable holds, so a
		// character stuck against a wall keeps trying the same step instead of
		// sliding along it. This is the behaviour a missing passability check
		// would hide, because then the event would walk straight through.
		var gameDir = CreateBlockedRouteGame(nameof(Test_ARouteHoldsOnTheSameCommandWhenTheWayIsBlockedAndItIsNotSkippable),
			Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveRight);
		var started = Start(gameDir);
		if (started == null)
		{
			return;
		}
		var runtime = started.Value.Runtime;
		BlockRowBelow(runtime, 17);
		AssertEq(runtime.StartEventMoveRouteForTest(1), 2, "two commands");

		for (var tick = 0; tick < 120; tick++)
		{
			runtime.Update(Tick);
		}
		var position = runtime.EventPositionForTest(1)!.Value;
		AssertEq(position.Y, 16, "the event did not walk through the wall");
		AssertEq(position.X, 20, "and did not slide sideways either, because the route is held");

		Cleanup(gameDir);
	}

	/// <summary>
	/// Makes row 17 a wall and everything else open, in the RM2K passability bit
	/// order where 0x0F is passable in all four directions.
	/// </summary>
	private static void BlockRowBelow(Rm2kEngineRuntime pRuntime, int pRow)
	{
		const int width = 40;
		const int height = 30;
		var masks = new List<byte>();
		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				masks.Add(y == pRow ? (byte)0x00 : (byte)0x0F);
			}
		}
		pRuntime.Simulation.OverridePassabilityMasksForTest(masks);
	}

	private void Cleanup(string pGameDir)
	{
		if (DirAccess.DirExistsAbsolute(pGameDir))
		{
			DirAccess.RemoveAbsolute(pGameDir);
		}
	}
}
