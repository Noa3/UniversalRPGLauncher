using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 10820, Memorize Location.
/// </summary>
/// <remarks>
/// <para>
/// The command is three variable writes, and <strong>the parameters are the
/// variables to write into, not the position to store</strong>. A reader that
/// read them as a position would write the player's tile into three variables
/// and store nothing at all — which is exactly the failure a three-parameter
/// command invites when the parameters are all integers.
/// </para>
/// <para>
/// Its counterpart <c>10830 Recall to Location</c> is in liblcf and has no
/// method in this build of EasyRPG, so this repository does not implement it.
/// The asymmetry is the reference's and is recorded rather than filled in from
/// imagination.
/// </para>
/// </remarks>
public partial class TestRm2kMemorizeLocation : TestBase
{
	private static Rm2kMap.EventCommand Memorize(
		int pVarMap, int pVarX, int pVarY)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.MemorizeLocation,
			Text = "",
			Parameters = [pVarMap, pVarX, pVarY],
		};
	}

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		var interpreter = new EventInterpreter(state, 1, pCommands);
		return (interpreter, state);
	}

	/// <summary>
	/// Reads a variable, or -1 when the command never grew the array that far.
	/// </summary>
	/// <remarks>
	/// A first draft read <c>Variables[pId - 1]</c> directly and threw on a
	/// command that stored nothing. **That is the test crashing, not the reader
	/// misbehaving** — the point of the assertion is that nothing was written,
	/// and an array that was never grown is the strongest form of that.
	/// </remarks>
	private static int Get(GameSimulationState pState, int pId)
	{
		return pId < 1 || pId > pState.Variables.Count ? -1 : pState.Variables[pId - 1];
	}

	/// <summary>
	/// The three parameters name the variables, and the values come from the
	/// player.
	/// </summary>
	/// <remarks>
	/// **This is the whole command.** A first draft read the parameters as the
	/// position to store, and every assertion below is written to catch that:
	/// the variables must end up holding the map, the x and the y, and the
	/// parameter numbers must not appear as values.
	/// </remarks>
	public void Test_TheParametersAreTheVariablesAndNotThePosition()
	{
		var (interpreter, state) = Run(Memorize(10, 11, 12));
		state.MapId = 7;
		state.MapX = 34;
		state.MapY = 56;

		interpreter.ExecuteFrame();

		AssertEq(
			Get(state, 10), 7,
			"**and variable 10 holds the map id**, because the first parameter"
			+ $" is the variable to write into and not the value; it is {Get(state, 10)}");
		AssertEq(
			Get(state, 11), 34,
			$"and variable 11 holds the player's x; it is {Get(state, 11)}");
		AssertEq(
			Get(state, 12), 56,
			$"and variable 12 holds the player's y; it is {Get(state, 12)}");
	}

	/// <summary>
	/// The parameter numbers must not leak in as values.
	/// </summary>
	/// <remarks>
	/// **The three parameters are 10, 11 and 12 in the fixture above**, and
	/// none of them may appear in any of the three variables. This is the
	/// assertion that a reader which stored the parameters themselves could
	/// not pass, and it is separate from the one above because the two failures
	/// look different in a log and the same in a test file.
	/// </remarks>
	public void Test_TheParameterNumbersDoNotLeakInAsValues()
	{
		var (interpreter, state) = Run(Memorize(10, 11, 12));
		state.MapId = 7;
		state.MapX = 34;
		state.MapY = 56;

		interpreter.ExecuteFrame();

		foreach (var (id, verboten) in new[] { (10, 10), (11, 11), (12, 12) })
		{
			AssertTrue(
				Get(state, id) != verboten,
				$"**and variable {id} does not hold the number {verboten}**, which"
				+ " is the parameter the command was given; it holds"
				+ $" {Get(state, id)}");
		}
	}

	/// <summary>
	/// The three variables are independent, and the order is the reference's.
	/// </summary>
	/// <remarks>
	/// <c>variables->Set(var_map_id, GetMapId())</c>, then x, then y. A reader
	/// that wrote them in a different order would put the y in the x's variable
	/// and the map in neither, and a game would teleport to a nonsense tile.
	/// </remarks>
	public void Test_MapXAndYGoToTheirOwnVariables()
	{
		// Absichtlich gleiche Zahlen fuer x und y, damit ein Tausch
		// unsichtbar bliebe. Also absichtlich verschiedene.
		var (interpreter, state) = Run(Memorize(1, 2, 3));
		state.MapId = 100;
		state.MapX = 11;
		state.MapY = 22;

		interpreter.ExecuteFrame();

		AssertEq(
			Get(state, 1), 100,
			"and variable 1 holds the map, 100 and not a tile; it is"
			+ $" {Get(state, 1)}");
		AssertEq(
			Get(state, 2), 11,
			"**and variable 2 holds the x, 11 and not the y** — which is what"
			+ $" makes a swap visible; it is {Get(state, 2)}");
		AssertEq(
			Get(state, 3), 22,
			$"and variable 3 holds the y, 22; it is {Get(state, 3)}");
	}

	/// <summary>
	/// A variable id of 0 is not a variable, and nothing is written.
	/// </summary>
	/// <remarks>
	/// <strong>All three are checked before any of them is written.</strong> A
	/// reader that wrote as it went would have stored the map and then thrown on
	/// the zero, leaving a game half-memorized — and a half-memorized location
	/// recalls the player to a tile the game never meant.
	/// </remarks>
	public void Test_AZeroVariableStoresNothingAtAll()
	{
		var (interpreter, state) = Run(Memorize(10, 0, 12));
		state.MapId = 7;
		state.MapX = 34;
		state.MapY = 56;

		interpreter.ExecuteFrame();

		AssertEq(
			Get(state, 10), -1,
			"**and the first variable was never even created**, because the"
			+ " check runs before any write and not as it goes; the reader"
			+ $" reports -1 for a variable that does not exist and it is {Get(state, 10)}");
		AssertEq(
			Get(state, 12), -1,
			$"**and so is the third**, for the same reason; it is {Get(state, 12)}");
		AssertTrue(
			ContainsDiagnostic(state, "outside 1 to 50000"),
			"and the diagnostic says which variable was wrong, because"
			+ " \"nothing was stored\" without a number cannot be acted on;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A command shorter than three parameters is refused.
	/// </summary>
	public void Test_ACommandShorterThanThreeIsRefused()
	{
		var (interpreter, state) = Run(
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.MemorizeLocation,
				Text = "",
				Parameters = [10, 11],
			});

		interpreter.ExecuteFrame();

		AssertTrue(
			ContainsDiagnostic(state, "malformed"),
			"**and the diagnostic says the parameters were malformed**;"
			+ " CmdSetup gives the command a minimum width of three;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A new game does not inherit the last one's memorized location.
	/// </summary>
	public void Test_AMemorizedLocationDoesNotSurviveANewGame()
	{
		var (interpreter, state) = Run(Memorize(10, 11, 12));
		state.MapId = 7;
		state.MapX = 34;
		state.MapY = 56;
		interpreter.ExecuteFrame();
		AssertEq(
			Get(state, 10), 7, "sanity: something was stored");

		state.Reset();

		AssertEq(
			state.Variables.Count, 0,
			"**and a new game has no memorized location**, because a game that"
			+ " began by recalling itself to the last one's tile would open"
			+ $" somewhere the player never was; there are {state.Variables.Count} variables");
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
