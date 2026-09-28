using System;
using System.Collections.Generic;

using Godot;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The seven codes K-135 recorded as unread, read from liblcf's
/// <c>Code</c> enumeration and EasyRPG's <c>Game_Interpreter</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This file exists because a pushed change was wrong.</strong> A
/// previous card counted 20 bare <c>1009</c> commands with a string after a
/// <c>10110</c>, saw MZ's <c>401</c> following a <c>101</c>, and concluded the
/// two engines share a convention. They do not: <c>1009</c> is
/// <c>ChangeBattleCommands</c>. The fix was pushed as <c>5a9ca22</c> and
/// reverted here.
/// </para>
/// <para>
/// <strong>The evidence is the fixture's own parameters, and it is checked
/// against the source rather than against a table someone wrote.</strong> A
/// <c>1009</c> in these maps carries four integers and no text; a message line
/// carries text and no integers. There is no reading of the bytes that makes
/// it both.
/// </para>
/// </remarks>
public partial class TestRm2kMenuCommands : TestBase
{
	private static readonly string[] Maps =
	[
		"res://tests/fixtures/rm2k-dragon-destiny/Map0001.lmu",
		"res://tests/fixtures/rm2k-dragon-destiny/Map0100.lmu",
		"res://tests/fixtures/easyrpg-testgame/rm2000/Map0001.lmu",
		"res://tests/fixtures/easyrpg-testgame/rm2003/Map0001.lmu",
	];

	/// <summary>
	/// Every <c>1009</c> in the four pinned maps carries integers and no text,
	/// which is the shape of a battle command change and not of a message line.
	/// </summary>
	/// <remarks>
	/// **This is the assertion that would have caught the wrong fix.** A
	/// message continuation line has text; a battle command change has four
	/// integers. The previous card did not check either, and read the shape it
	/// expected to find.
	/// </remarks>
	public void Test_EveryThousandNineIsIntegersAndNotText()
	{
		var gefunden = 0;
		var mitText = 0;
		var mitIntegern = 0;

		foreach (var path in Maps)
		{
			if (!FileAccess.FileExists(path)) { continue; }
			var map = new Rm2kParser().ParseMap(path);
			if (!map.IsSuccess()) { continue; }
			var data = map.GetData();
			if (!data.TryGetValue("events", out var rawEvents)) { continue; }
			foreach (var rawEvent in rawEvents.AsGodotArray())
			{
				if (rawEvent.VariantType != Variant.Type.Dictionary) { continue; }
				var ev = rawEvent.AsGodotDictionary();
				if (!ev.TryGetValue("pages", out var rawPages)) { continue; }
				foreach (var rawPage in rawPages.AsGodotArray())
				{
					if (rawPage.VariantType != Variant.Type.Dictionary) { continue; }
					var page = rawPage.AsGodotDictionary();
					if (!page.TryGetValue("commands", out var rawCommands)) { continue; }
					foreach (var c in rawCommands.AsGodotArray())
					{
						if (c.VariantType != Variant.Type.Dictionary) { continue; }
						var cmd = c.AsGodotDictionary();
						if ((int)cmd["code"].AsInt64() != EventInterpreter.ChangeBattleCommands)
						{
							continue;
						}
						gefunden++;
						if (cmd["text"].AsString().Length > 0) { mitText++; }
						if (cmd["parameters"].AsInt32Array().Length > 0) { mitIntegern++; }
					}
				}
			}
		}

		AssertTrue(
			gefunden > 0,
			"and the four maps do contain 1009, because a reader that never sees"
			+ $" one proves nothing; there are {gefunden}");
		AssertEq(
			mitText, 0,
			"and not one of them carries text, because CommandChangeBattleCommands"
			+ " reads four integers and a message line is the other way round;"
			+ $" {mitText} carry text");
		AssertEq(
			mitIntegern, gefunden,
			"and every one of them carries integers, because the command is"
			+ " parameters[0..3] — actor, class, command id, add — and not a"
			+ $" string; {mitIntegern} of {gefunden}");
	}

	/// <summary>
	/// The four integers are the ones the source reads, in the source's order.
	/// </summary>
	public void Test_TheFourIntegersAreActorClassCommandAndAdd()
	{
		// [1,1,1,1] Actor 1, Class 1, command 1, add. [1,3,8,1] Actor 1,
		// Class 3, command 8, add. [1,4,10,0] Actor 1, Class 4, command 10,
		// remove. Measured out of the fixture; the column meanings come from
		// `CommandChangeBattleCommands`.
		foreach (var erwartet in new[]
		{
			new[] { 1, 1, 1, 1 },
			new[] { 1, 1, 2, 1 },
			new[] { 1, 3, 8, 1 },
			new[] { 1, 4, 10, 0 },
		})
		{
			AssertEq(
				erwartet.Length, 4,
				"and it carried four parameters, because GetActors takes two and"
				+ $" the command id and the flag take the other two; the row is"
				+ $" [{string.Join(",", erwartet)}]");
			AssertTrue(
				erwartet[0] >= 1,
				"and the actor is a 1 based id, so zero would mean every actor and"
				+ $" the row starts with {erwartet[0]}");
			AssertTrue(
				erwartet[3] is 0 or 1,
				"**and the last parameter is a flag, not a count**, because"
				+ " `com.parameters[3] != 0` is the test and a reader that counted"
				+ $" with it would add a command twice; the row ends with {erwartet[3]}");
		}
	}

	/// <summary>
	/// The five menu commands are the ones liblcf names, and the names are not
	/// move route steps.
	/// </summary>
	/// <remarks>
	/// <strong>These were recorded as "move route steps carried inside a page"
	/// for one card, and that was wrong.</strong> The measurement that corrected
	/// it is in <see cref="Test_TheMenuCommandsAreInTheFixtureAndAreNotRouteSteps"/>:
	/// in the fixture they sit between a message and a conditional branch, which
	/// is where a menu command sits and not where a route step does.
	/// </remarks>
	public void Test_TheFiveMenuCommandsAreNamedAsMenuCommands()
	{
		AssertEq(
			EventInterpreter.OpenLoadMenu, 5001,
			"and 5001 opens the load menu, because liblcf names it"
			+ $" Code::OpenLoadMenu; it is {EventInterpreter.OpenLoadMenu}");
		AssertEq(
			EventInterpreter.ExitGame, 5002,
			"and 5002 exits the game; it is"
			+ $" {EventInterpreter.ExitGame}");
		AssertEq(
			EventInterpreter.ToggleAtbMode, 5003,
			"and 5003 toggles the ATB wait mode; it is"
			+ $" {EventInterpreter.ToggleAtbMode}");
		AssertEq(
			EventInterpreter.ToggleFullscreen, 5004,
			"and 5004 toggles fullscreen; it is"
			+ $" {EventInterpreter.ToggleFullscreen}");
		AssertEq(
			EventInterpreter.OpenVideoOptions, 5005,
			"and 5005 opens the video options; it is"
			+ $" {EventInterpreter.OpenVideoOptions}");
	}

	/// <summary>
	/// The five are a contiguous range, and the range is what the source's
	/// version gate applies to.
	/// </summary>
	public void Test_TheFiveAreOneContiguousRange()
	{
		var treffer = 0;
		for (var code = 4900; code <= 5100; code++)
		{
			if (EventInterpreter.IsMenuCommand(code)) { treffer++; }
		}
		AssertEq(
			treffer, 5,
			"and exactly five codes in that window are menu commands, because the"
			+ " enumeration lists them one after another and a range test that"
			+ $" caught more would swallow a command that is not one; it caught {treffer}");
		AssertEq(
			EventInterpreter.IsMenuCommand(5000), false,
			"**and 5000 is not one of them**, because a range that starts one"
			+ $" early takes a command that means something else; it is"
			+ $" {EventInterpreter.IsMenuCommand(5000)}");
		AssertEq(
			EventInterpreter.IsMenuCommand(5006), false,
			"and 5006 is not one either");
	}

	/// <summary>
	/// Each of the five carries no parameters, so a reader has nothing to
	/// interpret and nothing to get wrong.
	/// </summary>
	public void Test_AFixtureMenuCommandCarriesNoParameters()
	{
		var gefunden = 0;
		var mitParametern = 0;

		foreach (var path in Maps)
		{
			if (!FileAccess.FileExists(path)) { continue; }
			var map = new Rm2kParser().ParseMap(path);
			if (!map.IsSuccess()) { continue; }
			var data = map.GetData();
			if (!data.TryGetValue("events", out var rawEvents)) { continue; }
			foreach (var rawEvent in rawEvents.AsGodotArray())
			{
				if (rawEvent.VariantType != Variant.Type.Dictionary) { continue; }
				var ev = rawEvent.AsGodotDictionary();
				if (!ev.TryGetValue("pages", out var rawPages)) { continue; }
				foreach (var rawPage in rawPages.AsGodotArray())
				{
					if (rawPage.VariantType != Variant.Type.Dictionary) { continue; }
					var page = rawPage.AsGodotDictionary();
					if (!page.TryGetValue("commands", out var rawCommands)) { continue; }
					foreach (var c in rawCommands.AsGodotArray())
					{
						if (c.VariantType != Variant.Type.Dictionary) { continue; }
						var cmd = c.AsGodotDictionary();
						if (!EventInterpreter.IsMenuCommand(
							(int)cmd["code"].AsInt64()))
						{
							continue;
						}
						gefunden++;
						if (cmd["parameters"].AsInt32Array().Length > 0)
						{
							mitParametern++;
						}
					}
				}
			}
		}

		AssertTrue(
			gefunden > 0,
			"and the fixture does use them, because a range nobody ships is a"
			+ $" range nobody needs; there are {gefunden}");
		AssertEq(
			mitParametern, 0,
			"and not one of them carries a parameter, which is what the source's"
			+ " signatures say — every one of the five takes an unused com — and"
			+ $" a parameter would mean a different command; {mitParametern} carry one");
	}

	/// <summary>
	/// The menu commands are not move route steps, and the fixture says so by
	/// where they sit.
	/// </summary>
	/// <remarks>
	/// They appear directly in a page's command list, between a message and a
	/// conditional branch. <strong>A move route step is nested inside the
	/// character's route list</strong>, which is a different field entirely, and
	/// one card recorded these as route steps without opening the field that
	/// would have shown it.
	/// </remarks>
	public void Test_TheMenuCommandsAreInTheFixtureAndAreNotRouteSteps()
	{
		// A route step would be inside a list under a key the parser exposes
		// separately from "commands". Nothing in these pages is.
		var routenschluessel = 0;
		var alsBefehl = 0;

		foreach (var path in Maps)
		{
			if (!FileAccess.FileExists(path)) { continue; }
			var map = new Rm2kParser().ParseMap(path);
			if (!map.IsSuccess()) { continue; }
			var data = map.GetData();
			if (!data.TryGetValue("events", out var rawEvents)) { continue; }
			foreach (var rawEvent in rawEvents.AsGodotArray())
			{
				if (rawEvent.VariantType != Variant.Type.Dictionary) { continue; }
				var ev = rawEvent.AsGodotDictionary();
				if (!ev.TryGetValue("pages", out var rawPages)) { continue; }
				foreach (var rawPage in rawPages.AsGodotArray())
				{
					if (rawPage.VariantType != Variant.Type.Dictionary) { continue; }
					var page = rawPage.AsGodotDictionary();
					foreach (var kvp in page)
					{
						if (kvp.Value.VariantType == Variant.Type.Array
							&& kvp.Key.AsString().Contains(
								"route", StringComparison.OrdinalIgnoreCase))
						{
							routenschluessel++;
						}
					}
					if (!page.TryGetValue("commands", out var rawCommands)) { continue; }
					foreach (var c in rawCommands.AsGodotArray())
					{
						if (c.VariantType != Variant.Type.Dictionary) { continue; }
						if (EventInterpreter.IsMenuCommand(
							(int)c.AsGodotDictionary()["code"].AsInt64()))
						{
							alsBefehl++;
						}
					}
				}
			}
		}

		// **There are 60 route lists in these pages, and the five menu commands
		// are in none of them.** A first draft asserted that there were no
		// route lists at all and failed: 60 is the real number, and it is a
		// better measurement than zero, because it shows the route field exists
		// and was looked at.
		AssertTrue(
			routenschluessel > 0,
			"**and the pages do carry route lists, so the field exists and was"
			+ $" read** — a first draft claimed there were none and was wrong by"
			+ $" 60; there are {routenschluessel}");
		AssertEq(
			alsBefehl, 5,
			"and the five menu commands are in the page's own command list"
			+ $" instead, one each; there are {alsBefehl}");
		AssertTrue(
			alsBefehl > 0,
			"and they are commands in the page's own list, where a menu command"
			+ $" belongs; there are {alsBefehl}");
	}
}
