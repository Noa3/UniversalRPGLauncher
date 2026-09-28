using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11840 Change Escape Access, 11930 Change Save Access and
/// 11960 Change Main Menu Access.
/// </summary>
/// <remarks>
/// <para>
/// The reference has three one-line methods with the same shape. <strong>That
/// is the whole command</strong>, and the interesting part is what a single
/// parameter does in each of them: a zero is not "no change" but a removal.
/// </para>
/// <para>
/// The other half is the default. All three flags start allowed, because a
/// database that never ran one of these commands has all three set.
/// </para>
/// </remarks>
public partial class TestRm2kAccessCommands : TestBase
{
	private static Rm2kMap.EventCommand Access(int pCode, int pValue)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = "",
			Parameters = [pValue],
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
	/// A new game is a game the player may save, escape from and open the menu
	/// in.
	/// </summary>
	/// <remarks>
	/// <strong>All three default to allowed and that is a real default, not a
	/// guess.</strong> A reader that defaulted to forbidden would make every
	/// untouched game unplayable the moment the player pressed Escape — and no
	/// test of a command would ever have found it, because a command-free game
	/// is the case nobody writes a test for.
	/// </remarks>
	public void Test_ANewGameAllowsEverything()
	{
		var state = new GameSimulationState();

		AssertEq(
			state.AllowEscape, true,
			"**and the player may escape a battle**, because a database that ran"
			+ $" no 11840 has the access set; it is {state.AllowEscape}");
		AssertEq(
			state.AllowSave, true,
			"**and may save**, because a database that ran no 11930 has it set;"
			+ $" it is {state.AllowSave}");
		AssertEq(
			state.AllowMenu, true,
			"**and may open the menu**, because a database that ran no 11960 has"
			+ $" it set; it is {state.AllowMenu}");
	}

	/// <summary>
	/// Each command writes its own flag and leaves the other two alone.
	/// </summary>
	/// <remarks>
	/// **A reader that wrote all three from defaults would unlock a cutscene the
	/// game had just locked** — the classic bug of a three-command family
	/// sharing one "set everything" path. Each assertion is separate, because a
	/// reader that collapsed the three would fail only one of them.
	/// </remarks>
	public void Test_EachCommandWritesOnlyItsOwnFlag()
	{
		var (interpreter, state) = Run(
			Access(EventInterpreter.ChangeEscapeAccess, 0),
			Access(EventInterpreter.ChangeSaveAccess, 0),
			Access(EventInterpreter.ChangeMainMenuAccess, 0));

		interpreter.ExecuteFrame();
		AssertEq(
			state.AllowEscape, false,
			"**and 11840 took the escape away**, from a zero; it is"
			+ $" {state.AllowEscape}");
		AssertEq(
			state.AllowSave, true,
			"**and saving is untouched by the escape command**, because 11840"
			+ $" names one flag and not three; it is {state.AllowSave}");
		AssertEq(
			state.AllowMenu, true,
			$"and the menu is untouched too; it is {state.AllowMenu}");

		interpreter.ExecuteFrame();
		AssertEq(
			state.AllowSave, false,
			"**and 11930 took saving away**, from a zero; it is"
			+ $" {state.AllowSave}");
		AssertEq(
			state.AllowMenu, true,
			"**and the menu is still untouched by the save command**, which is"
			+ $" the half that proves the three are not one path; it is {state.AllowMenu}");

		interpreter.ExecuteFrame();
		AssertEq(
			state.AllowMenu, false,
			"and 11960 took the menu away, from a zero; it is"
			+ $" {state.AllowMenu}");
		AssertEq(
			state.AllowEscape, false,
			"and the escape flag stayed where it was, at false; it is"
			+ $" {state.AllowEscape}");
	}

	/// <summary>
	/// A zero is a removal and not "no change", so a cutscene can give the
	/// player their menu back.
	/// </summary>
	/// <remarks>
	/// The reference writes <c>SetAllowMenu(com.parameters[0] != 0)</c>. <strong>A
	/// reader that only ever set the flag to true could never give a player
	/// their menu back</strong>, and a game that locks the menu for one cutscene
	/// and unlocks it for the next would be stuck in a locked menu.
	/// </remarks>
	public void Test_AZeroIsARemovalAndNotNoChange()
	{
		var (interpreter, state) = Run(
			Access(EventInterpreter.ChangeMainMenuAccess, 0),
			Access(EventInterpreter.ChangeMainMenuAccess, 1));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.AllowMenu, true,
			"**and the menu came back**, because a one writes true over the"
			+ " zero that came before it, and the same field is written twice;"
			+ $" it is {state.AllowMenu}");
	}

	/// <summary>
	/// A non-zero other than one is also allowed.
	/// </summary>
	/// <remarks>
	/// The reference tests <c>!= 0</c> and not <c>== 1</c>. **A reader that
	/// compared against one would refuse a game that passed a boolean computed
	/// elsewhere**, and the diagnostic would call a legal command invalid.
	/// </remarks>
	public void Test_AnyNonZeroIsAllowed()
	{
		var (interpreter, state) = Run(
			Access(EventInterpreter.ChangeSaveAccess, 2));

		interpreter.ExecuteFrame();

		AssertEq(
			state.AllowSave, true,
			"**and a two is as good as a one**, because the reference tests for"
			+ $" a zero and not for a one; saving is {state.AllowSave}");
	}

	/// <summary>
	/// The access a cutscene took is not a new game's access.
	/// </summary>
	/// <remarks>
	/// <strong>A reset that left a cutscene's restrictions in place would lock
	/// the next game</strong> — and the player would have no way to know why.
	/// </remarks>
	public void Test_AccessDoesNotSurviveANewGame()
	{
		var (interpreter, state) = Run(
			Access(EventInterpreter.ChangeEscapeAccess, 0),
			Access(EventInterpreter.ChangeSaveAccess, 0),
			Access(EventInterpreter.ChangeMainMenuAccess, 0));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		AssertEq(state.AllowMenu, false, "sanity: the cutscene took the menu");

		state.Reset();

		AssertEq(
			state.AllowEscape, true,
			"**and a new game may escape again**, because the access belongs to"
			+ $" the game and not to the player; it is {state.AllowEscape}");
		AssertEq(
			state.AllowSave, true,
			$"**and may save**, for the same reason; it is {state.AllowSave}");
		AssertEq(
			state.AllowMenu, true,
			"and may open the menu, so a cutscene that locked it does not lock"
			+ $" the next game; it is {state.AllowMenu}");
	}
}
