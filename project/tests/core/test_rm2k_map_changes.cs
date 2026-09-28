using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11710 Change Map Tileset, 11720 Change PBG, 11740 Change
/// Encounter Steps and 11750 Tile Substitution.
/// </summary>
/// <remarks>
/// <para>
/// <c>11750</c> is the one that was structurally impossible: the substitution
/// tables had two 144-entry arrays and <strong>two readers and no writer</strong>,
/// so the command could be parsed and never run.
/// </para>
/// <para>
/// <c>11720</c> has the trap that reads like a pattern: six flags and two
/// speeds, and <strong>the speeds come from different parameters than the
/// flags</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kMapChanges : TestBase
{
	private static Rm2kMap.EventCommand Cmd(
		int pCode, string pText, params int[] pParameters)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = pText,
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

	// ---- 11710

	/// <summary>
	/// Chipset 0 is a real chipset.
	/// </summary>
	/// <remarks>
	/// The reference compares against the current chipset and returns early
	/// when they match, so a game that sets the chipset it already has pays
	/// nothing. <strong>A reader that treated zero as unset would refuse the
	/// first chipset in the database</strong>, and a database whose first entry is
	/// the most common chipset would refuse the most common chipset.
	/// </remarks>
	public void Test_ChipsetZeroIsARealChipset()
	{
		var (nullSetzen, state1) = Run(
			Cmd(EventInterpreter.ChangeMapTileset, "", 0, 0));
		nullSetzen.ExecuteFrame();

		AssertEq(
			state1.ChipsetId, 0,
			"**and setting zero is not a refusal**, because zero is a chipset and"
			+ $" not \"none\"; it is {state1.ChipsetId}");

		var (echtes, state2) = Run(Cmd(EventInterpreter.ChangeMapTileset, "", 3, 0));
		echtes.ExecuteFrame();
		AssertEq(
			state2.ChipsetId, 3,
			"**and a non-zero one lands too**, from parameters[0]; it is"
			+ $" {state2.ChipsetId}");
	}

	/// <summary>
	/// Setting the chipset that is already there costs nothing.
	/// </summary>
	/// <remarks>
	/// <strong>The reference returns before touching anything</strong>, and the
	/// diagnostic says so — <em>a chipset that changed and nothing redrew looks
	/// exactly like a chipset that did not change</em>, and the diagnostic is the
	/// only thing that tells them apart.
	/// </remarks>
	public void Test_SettingTheSameChipsetIsCheap()
	{
		var (zuerst, state) = Run(
			Cmd(EventInterpreter.ChangeMapTileset, "", 5, 0));
		zuerst.ExecuteFrame();
		AssertEq(state.ChipsetId, 5, "sanity: the first one landed");

		// **A second state with 5 already set**, because the first one only
		// proves the write landed. A test that re-used the first state would
		// see the same chipset twice and prove nothing.
		var (nochmal, state2) = Run(Cmd(EventInterpreter.ChangeMapTileset, "", 5, 0));
		state2.SetChipset(5);
		nochmal.ExecuteFrame();
		AssertTrue(
			ContainsDiagnostic(state2, "already the one this map uses"),
			"**and the second one is recognised as a no-op**, because the"
			+ $" reference returns early; the diagnostics are {state2.Diagnostics.Count}");
	}

	/// <summary>
	/// A chipset outside the bound is refused.
	/// </summary>
	public void Test_AChipsetOutsideTheBoundIsRefused()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeMapTileset, "",
				GameSimulationState.MaxChipsetId + 1, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.ChipsetId, 0,
			"**and nothing changed**, because the database has"
			+ $" {GameSimulationState.MaxChipsetId + 1} chipsets and not more;"
			+ $" the chipset is {state.ChipsetId}");
		AssertTrue(
			ContainsDiagnostic(state, "is outside 0 to"),
			"and the diagnostic names the bound; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	// ---- 11740

	/// <summary>
	/// Zero encounter steps is a real value and it is the one that stops
	/// fighting.
	/// </summary>
	/// <remarks>
	/// <strong>A reader that treated zero as unset could never turn random
	/// encounters off</strong>, and a game that does so — a town, a puzzle room,
	/// the last map — would keep fighting every few steps for the rest of it.
	/// </remarks>
	public void Test_ZeroEncounterStepsStopsFighting()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeEncounterSteps, "", 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.EncounterSteps, 0,
			"**and an encounter now comes never**, because zero is written"
			+ $" through and not treated as unset; the steps are {state.EncounterSteps}");
		AssertTrue(
			ContainsDiagnostic(state, "means never"),
			"and the diagnostic says what zero does, because a reader that"
			+ " refused zero would leave a game fighting in its own town;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A step count outside the bound is refused.
	/// </summary>
	public void Test_EncounterStepsOutsideTheBoundAreRefused()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeEncounterSteps, "",
				GameSimulationState.MaxEncounterSteps + 1));

		interpreter.ExecuteFrame();

		AssertEq(
			state.EncounterSteps, 50,
			"**and the old value stayed**, because the bound is what the save"
			+ $" format holds; the steps are {state.EncounterSteps}");
		AssertTrue(
			ContainsDiagnostic(state, "is outside 0 to"),
			"and the diagnostic names the bound; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	// ---- 11720

	/// <summary>
	/// The six flags and the two speeds come from different parameters.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The flags are 0, 1, 2 and 4; the horizontal speed is 3 and the vertical
	/// is 5. <strong>The fourth flag and the horizontal speed are adjacent</strong>
	/// in the list, which is what makes the mistake easy: a reader that read
	/// the parameters in order would take the fourth flag as a speed and the
	/// fourth flag as the vertical-auto bit.
	/// </para>
	/// <para>
	/// The reference writes <c>scroll_horz_speed</c> from
	/// <c>ValueOrVariableBitfield(com, 6, 4, 3)</c> — shift 4 in the mode,
	/// index 3 for the value — and <c>scroll_vert_speed</c> from
	/// <c>(com, 6, 6, 5)</c>.
	/// </para>
	/// </remarks>
	public void Test_TheFlagsAndSpeedsComeFromDifferentParameters()
	{
		var (interpreter, state) = Run(Cmd(
			EventInterpreter.ChangePBG, "Sky",
			1, 1, 1, 7, 1, 9, 0, 0));
		interpreter.ExecuteFrame();
		var pb = state.MapParallax;

		AssertEq(
			pb.ScrollHorizontally, true,
			"**and it scrolls sideways**, from parameters[0]; it is"
			+ $" {pb.ScrollHorizontally}");
		AssertEq(
			pb.ScrollVertically, true,
			"**and up and down**, from parameters[1]; it is"
			+ $" {pb.ScrollVertically}");
		AssertEq(
			pb.ScrollHorizontallyAutomatic, true,
			"**and sideways on its own**, from parameters[2]; it is"
			+ $" {pb.ScrollHorizontallyAutomatic}");
		AssertEq(
			pb.HorizontalSpeed, 7,
			"**and the horizontal speed is 7, from parameters[3]** and not from"
			+ " the fourth parameter, which is a flag — a reader that read them"
			+ $" in order would have put the flag into the speed; it is {pb.HorizontalSpeed}");
		AssertEq(
			pb.ScrollVerticallyAutomatic, true,
			"**and upwards on its own**, from parameters[4]; it is"
			+ $" {pb.ScrollVerticallyAutomatic}");
		AssertEq(
			pb.VerticalSpeed, 9,
			"**and the vertical speed is 9, from parameters[5]**; it is"
			+ $" {pb.VerticalSpeed}");
	}

	/// <summary>
	/// An empty name is the database panorama and not a missing file.
	/// </summary>
	/// <remarks>
	/// That is what the reference does with <c>if (!params.name.empty())</c>
	/// before it asks for the file. <strong>A reader that treated an empty name
	/// as an error would refuse the one thing the command is for</strong> — going
	/// back to what the database says.
	/// </remarks>
	public void Test_AnEmptyNameIsTheDatabasePanorama()
	{
		var (leer, state1) = Run(Cmd(
			EventInterpreter.ChangePBG, "", 0, 0, 0, 0, 0, 0, 0, 0));
		leer.ExecuteFrame();

		AssertEq(
			state1.MapParallax.Name, "",
			"**and the name is empty**, which is the database panorama; it is"
			+ $" \"{state1.MapParallax.Name}\"");
		AssertTrue(
			ContainsDiagnostic(state1, "the database one"),
			"and the diagnostic says which of the two it is, because an empty"
			+ " name otherwise reads like a mistake;"
			+ $" the diagnostics are {state1.Diagnostics.Count}");
		AssertEq(
			ContainsDiagnostic(state1, "waits for the panorama file"), false,
			"**and it does not claim to be waiting for a file**, because there is"
			+ " no file to wait for");
	}

	/// <summary>
	/// A named panorama is stored, and the missing file is said out loud.
	/// </summary>
	/// <remarks>
	/// The reference makes the interpreter <strong>wait</strong> for the file
	/// through an async yield. This reader has no file system here, so the wait
	/// is a diagnostic — **a reader that waited forever would hang a game whose
	/// panorama is simply missing**, and a missing panorama is a bug in a game,
	/// not a reason to stop the interpreter.
	/// </remarks>
	public void Test_ANamedPanoramaIsStoredAndTheWaitIsSayingSo()
	{
		var (interpreter, state) = Run(Cmd(
			EventInterpreter.ChangePBG, "Sky", 0, 0, 0, 0, 0, 0, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.MapParallax.Name, "Sky",
			"**and the name is Sky**, from the string field; it is"
			+ $" \"{state.MapParallax.Name}\"");
		AssertTrue(
			ContainsDiagnostic(state, "waits for the panorama file"),
			"**and the diagnostic says the reference would have waited**, because"
			+ " a panorama that appeared without a redraw looks like one that"
			+ $" never changed; the diagnostics are {state.Diagnostics.Count}");
	}

	// ---- 11750

	/// <summary>
	/// The substitution writes into the table.
	/// </summary>
	/// <remarks>
	/// <strong>This is the writer the class was missing.</strong> The tables and
	/// their two readers existed, so the command could be parsed and never run —
	/// and a test that only checked the readers would have been green the whole
	/// time.
	/// </remarks>
	public void Test_TheSubstitutionWritesIntoTheTable()
	{
		var (interpreter, state) = Run(Cmd(
			EventInterpreter.TileSubstitution, "", 1, 40, 999));
		interpreter.ExecuteFrame();

		AssertTrue(
			state.TileSubstitution != null,
			"**and there is a substitution table**, because the command creates"
			+ " the identity tables when the map has none; it is"
			+ $" {(state.TileSubstitution == null ? "none" : "set")}");
		AssertEq(
			state.TileSubstitution!.SubstituteUpper(40), 999,
			"**and upper tile 40 now draws chip 999**, which is the write the"
			+ $" class was missing; it is {state.TileSubstitution.SubstituteUpper(40)}");
		AssertEq(
			state.TileSubstitution!.SubstituteLower(40),
			40 + Rm2kChipset.BlockEIndex,
			"**and the lower table is untouched**, because parameter 0 is a"
			+ " boolean and not a layer index, so the two are separate writes —"
			+ " and the identity reads back with the block offset the reader"
			+ $" adds; it is {state.TileSubstitution.SubstituteLower(40)}");
	}

	/// <summary>
	/// Parameter 0 is a boolean and not a layer number.
	/// </summary>
	/// <remarks>
	/// A cleared flag writes the lower table. <strong>A reader that read it as
	/// "0 means lower, 1 means upper" would be right by accident for two values
	/// and wrong for every other</strong> — and any value above one would have
	 /// picked a table no command in the format can name.
	/// </remarks>
	public void Test_AClearedFlagWritesTheLowerTable()
	{
		var (interpreter, state) = Run(Cmd(
			EventInterpreter.TileSubstitution, "", 0, 40, 999));
		interpreter.ExecuteFrame();

		// **SubstituteLower adds BlockEIndex on the way out**, so the read back
		// is 999 plus that offset and not 999. A test that expected the raw
		// number would have "failed" a correct writer.
		AssertEq(
			state.TileSubstitution!.SubstituteLower(40),
			999 + Rm2kChipset.BlockEIndex,
			"**and lower tile 40 now reads back as 999 plus the block offset**,"
			+ " because the reader adds it and a writer that subtracted it"
			+ $" would land BlockEIndex low; it is {state.TileSubstitution.SubstituteLower(40)}");
		AssertEq(
			state.TileSubstitution!.SubstituteUpper(40), 40,
			"**and the upper table is untouched**;"
			+ $" it is {state.TileSubstitution.SubstituteUpper(40)}");
	}

	/// <summary>
	/// An index outside the table is refused.
	/// </summary>
	public void Test_AnIndexOutsideTheTableIsRefused()
	{
		var (interpreter, state) = Run(Cmd(
			EventInterpreter.TileSubstitution, "", 1,
			Rm2kChipset.NumUpperTiles, 5));

		interpreter.ExecuteFrame();

		AssertEq(
			state.TileSubstitution!.SubstituteUpper(
				Rm2kChipset.NumUpperTiles - 1),
			Rm2kChipset.NumUpperTiles - 1,
			"**and the last real entry is untouched**, because the table ends"
			+ $" at {Rm2kChipset.NumUpperTiles} entries; it is"
			+ $" {state.TileSubstitution.SubstituteUpper(Rm2kChipset.NumUpperTiles - 1)}");
		AssertTrue(
			ContainsDiagnostic(state, "is outside"),
			"and the diagnostic says the bound; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	/// <summary>
	/// None of it survives a new game.
	/// </summary>
	/// <remarks>
	/// <strong>A new game that inherited a random encounter rate of zero would
	/// be unwinnable</strong> — no fights, no experience — and one that
	/// inherited another game's panorama would open on a sky that is not its
	/// own.
	/// </remarks>
	public void Test_NoneOfItSurvivesANewGame()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeMapTileset, "", 4, 0),
			Cmd(EventInterpreter.ChangePBG, "Sky", 1, 1, 1, 7, 1, 9, 0, 0),
			Cmd(EventInterpreter.ChangeEncounterSteps, "", 0),
			Cmd(EventInterpreter.TileSubstitution, "", 1, 40, 999));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		AssertEq(state.EncounterSteps, 0, "sanity: encounters are off");

		state.Reset();

		AssertEq(
			state.EncounterSteps, 50,
			"**and a new game fights again**, because a game that inherited zero"
			+ " would never gain experience; the steps are"
			+ $" {state.EncounterSteps}");
		AssertEq(
			state.ChipsetId, 0,
			"**and the chipset is the first one again**, because a new game that"
			+ $" opened in another game's tiles would look like a bug; it is {state.ChipsetId}");
		AssertEq(
			state.MapParallax.Name, "",
			"and the panorama is the database one; it is"
			+ $" \"{state.MapParallax.Name}\"");
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
