using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 10920 Store Event ID, 11810 Teleport Targets, 12420 Game Over and
/// 12510 Return to Title Screen.
/// </summary>
/// <remarks>
/// <para>
/// <c>10920</c> was on the board as "no method in this EasyRPG build". That was
/// wrong: <c>CommandStoreEventID</c> is there, with a body that does three
/// things a reader has to get right — a value-or-variable mode shared by both
/// coordinates, a zero for an empty tile, and no hold.
/// </para>
/// <para>
/// The other three are about waiting: a game over screen that waits for a
/// message, and a teleport point whose fourth parameter is the opposite of
/// what it looks like.
/// </para>
/// </remarks>
public partial class TestRm2kTeleportAndOutcome : TestBase
{
	private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = "",
			Parameters = [.. pParameters],
		};
	}

	/// <summary>
	/// <c>[mode, x, y, switchFlag, switchId]</c> for a point, plus the leading
	/// add/remove flag.
	/// </summary>
	private static Rm2kMap.EventCommand Point(
		int pMap, int pX, int pY, int pSwitchFlag = 0, int pSwitchId = 0,
		int pRemove = 0)
	{
		return Cmd(EventInterpreter.TeleportTargets,
			pRemove, pMap, pX, pY, pSwitchFlag, pSwitchId);
	}

	private static (EventInterpreter Interpreter, GameSimulationState State)
		Run(
			Func<int, int, int>? pEventAtTile = null,
			PresentationState? pPresentation = null,
			params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		state.ConfigureMap(1, 20, 20, new bool[400]);
		for (var i = 0; i < 10; i++)
		{
			state.Variables.Add(0);
		}
		var interpreter = new EventInterpreter(
			state, 1, pCommands, pPresentation, null, null, null, pEventAtTile);
		return (interpreter, state);
	}

	// ---- 10920 Store Event ID

	/// <summary>
	/// The event id on a tile lands in the variable.
	/// </summary>
	public void Test_TheEventIdOnATileLandsInTheVariable()
	{
		var (interpreter, state) = Run(
			(x, y) => x == 4 && y == 5 ? 7 : 0,
			null,
			Cmd(EventInterpreter.StoreEventID, 0, 4, 5, 3));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Variables[2], 7,
			"**and variable 3 holds 7**, because the tile at 4,5 holds event 7"
			+ $" and the command wrote it; it is {state.Variables[2]}");
	}

	/// <summary>
	/// An empty tile stores a zero and does not hold the page.
	/// </summary>
	/// <remarks>
	/// The reference writes <c>ev ? ev->GetId() : 0</c>. <strong>A reader that
	/// held the page would leave the variable holding whatever it held
	/// before</strong>, and a game that asks "is there anything here?" gets a
	/// zero it can test — that is the whole point of the command.
	/// </remarks>
	public void Test_AnEmptyTileStoresZeroAndDoesNotHold()
	{
		var (interpreter, state) = Run(
			(x, y) => 0,
			null,
			Cmd(EventInterpreter.StoreEventID, 0, 4, 5, 3),
			Cmd(EventInterpreter.TeleportTargets, 0, 1, 1, 1, 0, 0));

		interpreter.ExecuteFrame();
		var weiter = interpreter.ExecuteFrame();

		AssertEq(
			state.Variables[2], 0,
			"**and variable 3 holds zero**, because the reference stores zero for"
			+ $" an empty tile; it is {state.Variables[2]}");
		AssertEq(
			weiter, true,
			"**and the page moved on**, because the command does not hold; the"
			+ $" next command ran and the frame reported {weiter}");
		AssertTrue(
			state.TeleportTargets.ContainsKey(1),
			"and the second command did run, which is the same fact from the"
			+ " other side");
	}

	/// <summary>
	/// Both coordinates come from variables when the mode says so.
	/// </summary>
	/// <remarks>
	/// <strong>One mode for both coordinates, and that is the reference's
	/// shape.</strong> A reader that read them as constants could only ever ask
	/// about one tile, and a game that follows a variable — "the tile the hero
	/// just walked off" — would store the wrong event or none.
	/// </remarks>
	public void Test_BothCoordinatesCanComeFromVariables()
	{
		var (interpreter, state) = Run(
			(x, y) => x * 100 + y,
			null,
			Cmd(EventInterpreter.StoreEventID, 1, 3, 4, 2));
		state.Variables[2] = 11;
		state.Variables[3] = 12;

		interpreter.ExecuteFrame();

		AssertEq(
			state.Variables[1], 1112,
			"**and variable 2 holds 1112**, because the mode sent both"
			+ $" coordinates through variables: x from variable 3, y from 4; it is {state.Variables[1]}");
	}

	/// <summary>
	/// A tile outside the map stores nothing and says which coordinate is
	/// wrong.
	/// </summary>
	/// <remarks>
	/// **A lookup past the edge of a map is not a lookup.** The reference would
	/// get nothing back and store a zero, and a zero reads exactly like "no
	/// event here" — so this reader refuses and names the coordinate, which is
	/// the difference between a bug in a game and a bug in the reader.
	/// </remarks>
	public void Test_ATileOutsideTheMapStoresNothing()
	{
		var (interpreter, state) = Run(
			(x, y) => 99,
			null,
			Cmd(EventInterpreter.StoreEventID, 0, 900, 5, 3));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Variables[2], 0,
			"**and variable 3 is still zero**, because nothing was stored;"
			+ $" it is {state.Variables[2]}");
		AssertTrue(
			ContainsDiagnostic(state, "is outside the map"),
			"and the diagnostic names the map, because 'refused' without the"
			+ " size cannot be acted on;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Without an event lookup the command says so instead of writing a zero.
	/// </summary>
	/// <remarks>
	/// **A zero would be a lie here.** The command means "put the event id here",
	/// and answering zero without having looked says "there is nothing" when the
	/// truth is "nobody asked".
	/// </remarks>
	public void Test_WithoutALookupNothingIsWritten()
	{
		var (interpreter, state) = Run(
			null,
			null,
			Cmd(EventInterpreter.StoreEventID, 0, 4, 5, 3));

		interpreter.ExecuteFrame();

		AssertTrue(
			ContainsDiagnostic(state, "no event lookup is wired"),
			"**and the diagnostic says the lookup is missing**, because a"
			+ $" silent zero would be a lie; the diagnostics are {state.Diagnostics.Count}");
	}

	// ---- 11810 Teleport Targets

	/// <summary>
	/// The fourth parameter says the switch must be on.
	/// </summary>
	/// <remarks>
	/// <strong>Not "use a switch"</strong> — the reference reads
	/// <c>bool switch_on = parameters[4]</c> and pairs it with the switch id. A
	/// reader that read it as the first would make every conditional warp
	/// unconditional, and a secret entrance would open at the start of the
	/// game.
	/// </remarks>
	public void Test_TheFourthParameterIsMustBeOnAndNotUseASwitch()
	{
		var (mit, state1) = Run(
			null, null, Point(1, 3, 3, pSwitchFlag: 1, pSwitchId: 2));
		mit.ExecuteFrame();

		var punkt = state1.TeleportTargets[1][0];
		AssertEq(
			punkt.RequiresSwitchOn, true,
			"**and the point needs its switch on**, from a non-zero fourth"
			+ $" parameter; it is {punkt.RequiresSwitchOn}");
		AssertEq(
			punkt.SwitchId, 2,
			"and it is switch 2, which is a different field from the flag;"
			+ $" it is {punkt.SwitchId}");

		var (ohne, state2) = Run(
			null, null, Point(1, 3, 3, pSwitchFlag: 0, pSwitchId: 2));
		ohne.ExecuteFrame();
		AssertEq(
			state2.TeleportTargets[1][0].RequiresSwitchOn, false,
			"**and a cleared flag means unconditional**, not 'switch 2 is off'");
		AssertEq(
			state2.TeleportTargets[1][0].SwitchId, 0,
			"**and the switch id is dropped**, because an unconditional point"
			+ " that still named a switch would make a reader wonder which of the"
			+ " two decides; it is"
			+ $" {state2.TeleportTargets[1][0].SwitchId}");
	}

	/// <summary>
	/// A non-zero first parameter removes every point on that map.
	/// </summary>
	/// <remarks>
	/// <strong>The mode, not a target id.</strong> A reader that read it as an
	/// index would add a point where the game meant to clear them, and a warp
	/// the player thought was gone would still be there.
	/// </remarks>
	public void Test_ARemovalTakesEveryPointOnThatMap()
	{
		var (interpreter, state) = Run(
			null,
			null,
			Point(1, 3, 3), Point(1, 5, 5), Point(2, 1, 1), Point(1, 7, 7, pRemove: 1));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		AssertEq(state.TeleportTargets[1].Count, 2, "sanity: two points on map 1");

		interpreter.ExecuteFrame();

		AssertEq(
			state.TeleportTargets.ContainsKey(1), false,
			"**and map 1 has no points left**, because the command removes every"
			+ " one of them and not just one; the key is"
			+ $" {state.TeleportTargets.ContainsKey(1)}");
		AssertEq(
			state.TeleportTargets[2].Count, 1,
			"**and map 2 is untouched**, because the removal names one map and"
			+ $" not all of them; it has {state.TeleportTargets[2].Count}");
	}

	/// <summary>
	/// A second point on the same tile replaces the first.
	/// </summary>
	/// <remarks>
	/// The reference appends, and a game that re-declares a point — after a
	/// cutscene moves it — would otherwise have two warps on one tile with no
	/// way to say which one is meant.
	/// </remarks>
	public void Test_ASecondPointOnTheSameTileReplacesTheFirst()
	{
		var (interpreter, state) = Run(
			null, null, Point(1, 3, 3), Point(1, 3, 3, pSwitchFlag: 1, pSwitchId: 2));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.TeleportTargets[1].Count, 1,
			"**and there is one point, not two**, because a game that moves a"
			+ " warp point needs the new one to win; there are"
			+ $" {state.TeleportTargets[1].Count}");
		AssertEq(
			state.TeleportTargets[1][0].RequiresSwitchOn, true,
			"**and it is the second one**, which is the half that proves it was"
			+ " replaced and not kept; it needs a switch:"
			+ $" {state.TeleportTargets[1][0].RequiresSwitchOn}");
	}

	/// <summary>
	/// A point outside the map is refused.
	/// </summary>
	public void Test_APointOutsideTheMapIsRefused()
	{
		var (interpreter, state) = Run(null, null, Point(1, 900, 3));

		interpreter.ExecuteFrame();

		AssertEq(
			state.TeleportTargets.ContainsKey(1), false,
			"**and no point was added**, because a warp to a tile that is not"
			+ $" there is a warp into nothing; the key is {state.TeleportTargets.ContainsKey(1)}");
		AssertTrue(
			ContainsDiagnostic(state, "is outside the map"),
			"and the diagnostic says which tile; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	// ---- 12420 and 12510

	/// <summary>
	/// A game over waits for the message it interrupted.
	/// </summary>
	/// <remarks>
	/// The reference's first two lines are
	/// <c>if (Game_Message::IsMessageActive()) return false;</c>. <strong>A hero
	/// who says their last line and then dies should die after the line is
	/// read</strong>, and a reader that showed the screen on top of the text
	/// would bury the line the game wrote for that moment.
	/// </remarks>
	public void Test_AGameOverWaitsForAnOpenMessage()
	{
		var presentation = new PresentationState();
		presentation.ShowMessage("I cannot go on.");
		var (interpreter, state) = Run(null, presentation, Cmd(EventInterpreter.GameOver));

		interpreter.ExecuteFrame();

		AssertEq(
			state.IsGameOverActive, false,
			"**and the screen is not up**, because a message is open and the"
			+ $" reference waits; it is {state.IsGameOverActive}");
		AssertEq(
			state.WaitingFor, GameSimulationState.WaitReason.MessageOpen,
			"**and the interpreter says what it waits for**, because a wait with"
			+ " no reason looks like a hang;"
			+ $" it waits for {state.WaitingFor}");
	}

	/// <summary>
	/// Once the message is closed the screen comes up and the page holds.
	/// </summary>
	/// <remarks>
	/// <strong>The page holds, and that is the reference's <c>return false</c>.</strong>
	/// Advancing would run the rest of the event behind a screen the player is
	/// still looking at.
	/// </remarks>
	public void Test_TheScreenComesUpAfterTheMessageAndThePageHolds()
	{
		var presentation = new PresentationState();
		presentation.ShowMessage("I cannot go on.");
		var (interpreter, state) = Run(null, presentation, Cmd(EventInterpreter.GameOver));

		interpreter.ExecuteFrame();
		presentation.DismissMessage();
		var gehalten = interpreter.ExecuteFrame();

		AssertEq(
			state.IsGameOverActive, true,
			"**and the screen is up**, because the message is closed; it is"
			+ $" {state.IsGameOverActive}");
		AssertEq(
			state.WaitingFor, GameSimulationState.WaitReason.GameOver,
			"and the interpreter waits for the game over screen;"
			+ $" it waits for {state.WaitingFor}");
		AssertEq(
			gehalten, false,
			"**and the page did not move**, because the reference returns false"
			+ $" and the rest of the event must not run behind the screen; the frame reported {gehalten}");
	}

	/// <summary>
	/// Returning to the title holds the page too.
	/// </summary>
	/// <remarks>
	/// The reference makes this an <strong>asynchronous operation</strong>, so
	/// the page holds until the title screen is actually up — a different
	/// reason than the game over screen and the same visible result.
	/// </remarks>
	public void Test_ReturnToTitleHoldsThePage()
	{
		var (interpreter, state) = Run(
			null, null, Cmd(EventInterpreter.ReturnToTitleScreen));

		var gehalten = interpreter.ExecuteFrame();

		AssertEq(
			state.IsTitleRequested, true,
			"**and the title screen is requested**, from a command that takes no"
			+ $" parameters at all; it is {state.IsTitleRequested}");
		AssertEq(
			gehalten, false,
			"**and the page held**, because the reference makes it an async"
			+ $" operation; the frame reported {gehalten}");
	}

	/// <summary>
	/// The outcome of one game is not the outcome of the next.
	/// </summary>
	/// <remarks>
	/// **A new game that started with the last game over screen still up would
	/// look like a crash**, and one that started with a title request pending
	/// would go straight back to the title.
	/// </remarks>
	public void Test_TheOutcomeDoesNotSurviveANewGame()
	{
		var presentation = new PresentationState();
		var (interpreter, state) = Run(
			null, presentation,
			Cmd(EventInterpreter.GameOver), Point(1, 3, 3));
		interpreter.ExecuteFrame();
		AssertEq(state.IsGameOverActive, true, "sanity: the screen is up");

		state.Reset();

		AssertEq(
			state.IsGameOverActive, false,
			"**and a new game is not a game over screen**, because the outcome"
			+ $" belongs to the game; it is {state.IsGameOverActive}");
		AssertEq(
			state.IsTitleRequested, false,
			$"and no title is pending; it is {state.IsTitleRequested}");
		AssertEq(
			state.WaitingFor, GameSimulationState.WaitReason.None,
			"**and the interpreter waits for nothing**, because a wait that"
			+ " survived a reset would freeze the new game on its first frame;"
			+ $" it waits for {state.WaitingFor}");
		AssertEq(
			state.TeleportTargets.Count, 0,
			"**and the warp points are gone**, because they are the last game's"
			+ $" doors; there are {state.TeleportTargets.Count}");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
