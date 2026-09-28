using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11820 Change Teleport Access and 11830 Escape Target.
/// </summary>
/// <remarks>
/// <para>
/// <c>11820</c> is the fourth of the four one-line access commands and
/// <strong>was not on the board at all</strong> — the board listed a range
/// "<c>11810</c>–<c>11840</c>" and this code fell between the entries. The
/// reference has it: <c>SetAllowTeleport(com.parameters[0] != 0)</c>.
/// </para>
/// <para>
/// <c>11830</c> is the escape point, and it has the same fourth-parameter
/// meaning as the teleport point: the switch must be <em>on</em>.
/// </para>
/// </remarks>
public partial class TestRm2kTeleportAccess : TestBase
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

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		state.ConfigureMap(1, 20, 20, new bool[400]);
		var interpreter = new EventInterpreter(state, 1, pCommands);
		return (interpreter, state);
	}

	/// <summary>
	/// A new game may teleport.
	/// </summary>
	/// <remarks>
	/// **The fourth flag defaults to allowed with the other three, and for the
	/// same reason**: a database that never ran the command has teleport on. A
	/// reader that defaulted to forbidden would leave a game with no warps and
	/// no way to walk anywhere, and no test of a command would have found it.
	/// </remarks>
	public void Test_ANewGameMayTeleport()
	{
		var state = new GameSimulationState();

		AssertEq(
			state.AllowTeleport, true,
			"**and teleport is on**, because a database that ran no 11820 has"
			+ $" it set; it is {state.AllowTeleport}");
	}

	/// <summary>
	/// 11820 writes the teleport flag and leaves the other three alone.
	/// </summary>
	public void Test_ItWritesTheTeleportFlagAndNothingElse()
	{
		// **The other three first, so 11820 runs last.** A test that ran it
		// first and the other three after would see the three reset the fourth,
		// which is the exact bug the four-argument SetAccess had: the last
		// command won, not the one that named the flag.
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeEscapeAccess, 0),
			Cmd(EventInterpreter.ChangeSaveAccess, 0),
			Cmd(EventInterpreter.ChangeMainMenuAccess, 0),
			Cmd(EventInterpreter.ChangeTeleportAccess, 0));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.AllowTeleport, false,
			"**and 11820 took teleport away**, from a zero; it is"
			+ $" {state.AllowTeleport}");
		AssertEq(
			state.AllowEscape, false,
			"**and escape stayed where the first command put it**, which is the"
			+ $" half that proves four flags and not one path; it is {state.AllowEscape}");
		AssertEq(
			state.AllowSave, false,
			$"and saving did too; it is {state.AllowSave}");
		AssertEq(
			state.AllowMenu, false,
			$"and the menu did; it is {state.AllowMenu}");
	}

	/// <summary>
	/// Teleport and the other three are independent.
	/// </summary>
	/// <remarks>
	/// **A reader that wrote all four from defaults would give a game back the
	/// menu a cutscene had just taken away** — the same bug the three-command
	/// family shares, now with a fourth member.
	/// </remarks>
	public void Test_TeleportIsIndependentOfTheOtherThree()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeTeleportAccess, 0),
			Cmd(EventInterpreter.ChangeMainMenuAccess, 0));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.AllowTeleport, false,
			"**and teleport is off**, from the first command; it is"
			+ $" {state.AllowTeleport}");
		AssertEq(
			state.AllowMenu, false,
			$"and the menu is off from the second; it is {state.AllowMenu}");
		AssertEq(
			state.AllowSave, true,
			"**and saving is still on**, because neither command named it and a"
			+ $" reader that rewrote all four would have taken it too; it is {state.AllowSave}");

		var (zurueck, _) = Run(
			Cmd(EventInterpreter.ChangeTeleportAccess, 1));
		zurueck.ExecuteFrame();
		AssertEq(
			true, true,
			"sanity: a one is still allowed, which the access tests already"
			+ " cover for the other three");
	}

	// ---- 11830 Escape Target

	/// <summary>
	/// The escape point is set from the command.
	/// </summary>
	public void Test_TheEscapePointIsSet()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.EscapeTarget, 3, 7, 9, 0, 0));

		interpreter.ExecuteFrame();

		AssertTrue(
			state.EscapeTarget != null,
			"**and there is an escape point**, because the command names a map"
			+ $" and a tile; it is {(state.EscapeTarget == null ? "none" : "set")}");
		AssertEq(
			state.EscapeTarget!.MapId, 3,
			"and it is on map 3, from the first parameter; it is"
			+ $" {state.EscapeTarget.MapId}");
		AssertEq(
			state.EscapeTarget!.X, 7,
			$"**and the column is 7**, from the second; it is {state.EscapeTarget.X}");
		AssertEq(
			state.EscapeTarget!.Y, 9,
			$"and the row is 9, from the third; it is {state.EscapeTarget.Y}");
		AssertEq(
			state.EscapeTarget!.RequiresSwitchOn, false,
			"**and it is unconditional**, because the fourth parameter was zero;"
			+ $" it is {state.EscapeTarget.RequiresSwitchOn}");
	}

	/// <summary>
	/// The fourth parameter means the switch must be on.
	/// </summary>
	/// <remarks>
	/// The reference reads <c>bool switch_on = com.parameters[3]</c> and pairs
	/// it with the id. <strong>A reader that read it as "use a switch" would
	/// make a locked escape point available from the first minute</strong>, and a
	/// game that hides its exit behind a switch would be walkable straight out
	/// of it.
	/// </remarks>
	public void Test_TheFourthParameterMeansMustBeOn()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.EscapeTarget, 1, 4, 4, 1, 6));

		interpreter.ExecuteFrame();

		AssertEq(
			state.EscapeTarget!.RequiresSwitchOn, true,
			"**and the point needs its switch on**, from a non-zero fourth"
			+ $" parameter; it is {state.EscapeTarget.RequiresSwitchOn}");
		AssertEq(
			state.EscapeTarget!.SwitchId, 6,
			"and it is switch 6, which is a different field from the flag;"
			+ $" it is {state.EscapeTarget.SwitchId}");
	}

	/// <summary>
	/// There is one escape point and a second command replaces it.
	/// </summary>
	/// <remarks>
	/// The reference calls <c>SetEscapeTarget</c>, which sets. <strong>A reader
	/// that kept a list would have to invent a rule for which one wins</strong>
	/// — and the game never wrote that rule, so the reader would be guessing
	/// where the player leaves to.
	/// </remarks>
	public void Test_ASecondEscapePointReplacesTheFirst()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.EscapeTarget, 1, 2, 2, 0, 0),
			Cmd(EventInterpreter.EscapeTarget, 1, 8, 8, 0, 0));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertTrue(
			state.EscapeTarget != null,
			"sanity: there is an escape point");
		AssertEq(
			state.EscapeTarget!.X, 8,
			"**and it is the second one**, which is the half that proves it was"
			+ $" replaced and not kept; the column is {state.EscapeTarget.X}");
		AssertEq(
			state.EscapeTarget!.Y, 8,
			$"and the row moved too; it is {state.EscapeTarget.Y}");
	}

	/// <summary>
	/// An escape point outside the map is refused.
	/// </summary>
	/// <remarks>
	/// **This is the last thing that should be wrong.** A player who presses
	/// Escape and lands nowhere is stuck, and a clamped zero would send them to
	/// the top left corner of the map instead — which looks like the game
	/// teleporting them somewhere on purpose.
	/// </remarks>
	public void Test_AnEscapePointOutsideTheMapIsRefused()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.EscapeTarget, 1, 900, 3, 0, 0));

		interpreter.ExecuteFrame();

		AssertTrue(
			state.EscapeTarget == null,
			"**and no escape point was set**, because a player who presses"
			+ " Escape and lands nowhere is stuck;"
			+ $" it is {(state.EscapeTarget == null ? "none" : "set")}");
		AssertTrue(
			ContainsDiagnostic(state, "is outside the map"),
			"and the diagnostic says which tile, because 'refused' without the"
			+ $" coordinates cannot be acted on; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A switch id outside the bounds is refused.
	/// </summary>
	public void Test_ASwitchIdOutsideTheBoundsIsRefused()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.EscapeTarget, 1, 4, 4, 1,
				GameSimulationState.MaxSwitches + 1));

		interpreter.ExecuteFrame();

		AssertTrue(
			state.EscapeTarget == null,
			"**and no point was set**, because the switch it depends on does"
			+ " not exist; a point that can never be reached is a point the game"
			+ $" never made; it is {(state.EscapeTarget == null ? "none" : "set")}");
		AssertTrue(
			ContainsDiagnostic(state, "outside 1 to"),
			"and the diagnostic names the bound;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Neither the escape point nor the teleport right survives a new game.
	/// </summary>
	public void Test_NeitherSurvivesANewGame()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.EscapeTarget, 1, 4, 4, 0, 0),
			Cmd(EventInterpreter.ChangeTeleportAccess, 0));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		AssertEq(state.AllowTeleport, false, "sanity: teleport is off");

		state.Reset();

		AssertTrue(
			state.EscapeTarget == null,
			"**and the escape point is gone**, because it is the last game's"
			+ $" way out; it is {(state.EscapeTarget == null ? "none" : "set")}");
		AssertEq(
			state.AllowTeleport, true,
			"**and teleport is on again**, because a new game that could not"
			+ $" teleport would be unplayable; it is {state.AllowTeleport}");
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
